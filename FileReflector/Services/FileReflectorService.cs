using CliWrap;
using CliWrap.Buffered;
using FileReflector.Helpers;
using FileReflector.Models;

namespace FileReflector.Services;

public class FileReflectorService : IFileReflectorService
{
    private readonly ILogger<FileReflectorService> _logger;
    private readonly UserConfigurationService _userConfigurationService;
    
    public event EventHandler<List<string>>? OnRsyncLogs;

    private readonly object _rsyncLogLock = new();
    private readonly List<string> _pendingRsyncLogs = new();
    private PeriodicTimer? _rsyncLogTimer;
    private CancellationTokenSource? _rsyncLogCts;
    private Task? _rsyncLogFlushTask;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public FileReflectorService(ILogger<FileReflectorService> logger, UserConfigurationService userConfigurationService)
    {
        _logger = logger;
        _userConfigurationService = userConfigurationService;
    }

    public async void TestCommand()
    {
        var result = await Cli.Wrap("ls")
            .WithArguments(["-al"])
            .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
            .ExecuteAsync();
    }

    public async Task<FileTreeNode> GetFilesFromRemote()
    {
        var remoteHost = _userConfigurationService.CurrentSettings.RemoteHost;
        var remoteFolder = _userConfigurationService.CurrentSettings.SourcePath;
        var findCommand = $"cd {remoteFolder} && " + @"find . -printf ""%P\t%y\n"" | sort | awk -F '\t' '{print $2, $1}'";
        var listOfFilesAndDirectories = new List<string>();
        
        try
        {
            await Cli.Wrap("ssh")
                .WithArguments(args => args
                    .Add(remoteHost)
                    .Add(findCommand)
                )
                .WithStandardOutputPipe(PipeTarget.ToDelegate(listOfFilesAndDirectories.Add))
                .ExecuteBufferedAsync();
                
            _logger.LogInformation("SSH command completed. Retrieved {Count} files/directories.", listOfFilesAndDirectories.Count);
            _logger.LogDebug("SSH command completed successfully. Retrieved the following files and directories:\n{FileList}", string.Join("\n", listOfFilesAndDirectories));
            
            var directoryTree = PathsProcessor.BuildDirectoryTree(listOfFilesAndDirectories);
            _logger.LogDebug("Printing received directory tree: \n{Tree}", directoryTree.ToString());
            
            return directoryTree;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SSH command to retrieve remote files. RemoteHost={RemoteHost}, SourcePath={SourcePath}", remoteHost, remoteFolder);
            throw;
        }
    }

    public async Task<FileTreeNode> GetFilesFromLocal()
    {
        var localFolder = _userConfigurationService.CurrentSettings.DestinationPath;
        var findCommand = $"cd {localFolder} && " + @"find . -printf ""%P\t%y\n"" | sort | awk -F '\t' '{print $2, $1}'";
        var listOfFilesAndDirectories = new List<string>();
        
        try
        {
            await Cli.Wrap("bash")
                .WithArguments(args => args
                    .Add("-c")
                    .Add(findCommand)
                )
                .WithStandardOutputPipe(PipeTarget.ToDelegate(listOfFilesAndDirectories.Add))
                .ExecuteBufferedAsync();
                
            _logger.LogInformation("Local command completed. Retrieved {Count} files/directories.", listOfFilesAndDirectories.Count);
            _logger.LogDebug("Local command completed successfully. Retrieved the following files and directories:\n{FileList}", string.Join("\n", listOfFilesAndDirectories));
            
            var directoryTree = PathsProcessor.BuildDirectoryTree(listOfFilesAndDirectories);
            _logger.LogDebug("Printing received directory tree: \n{Tree}", directoryTree.ToString());
            
            return directoryTree;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command to retrieve local files. LocalDestinationPath={LocalDestinationPath}", localFolder);
            throw;
        }
    }


    public async Task SyncFilesToLocal(List<string> fileList)
    {
        if (!await _syncLock.WaitAsync(0)) return;

        try
        {
            StartRsyncLogBatching();

            var rsyncFileList = PathsProcessor.GetRsyncEscapedPathsWithParents(fileList);
            var localFolder = _userConfigurationService.CurrentSettings.DestinationPath;
            var remoteHost = _userConfigurationService.CurrentSettings.RemoteHost;
            var remoteFolder = _userConfigurationService.CurrentSettings.SourcePath;

            _logger.LogInformation("rsync summary: remoteHost={RemoteHost}, remoteFolder={RemoteFolder}, localFolder={LocalFolder}, include-from=\n{RsyncFileList}", remoteHost, remoteFolder, localFolder, rsyncFileList);

            try
            {
                await Cli.Wrap("rsync")
                    .WithArguments(args => args
                        .Add("-a").Add("-vv")
                        .Add("--human-readable")
                        .Add("--include-from=-")
                        .Add("--exclude=*").Add("--delete-excluded")
                        .Add("--info=progress2")
                        .Add($"{remoteHost}:{remoteFolder}")
                        .Add($"{localFolder}")
                    )
                    .WithStandardInputPipe(PipeSource.FromString(rsyncFileList))
                    .WithStandardOutputPipe(PipeTarget.ToDelegate(LogDebugRsync))
                    .ExecuteBufferedAsync();
            }
            finally
            {
                await StopRsyncLogBatchingAsync();
            }
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private void LogDebugRsync(string log)
    {
        _logger.LogDebug($"rsync> {log}");
        lock (_rsyncLogLock)
        {
            _pendingRsyncLogs.Add(log);
        }
    }

    private void StartRsyncLogBatching()
    {
        _rsyncLogCts = new CancellationTokenSource();
        _rsyncLogTimer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        _rsyncLogFlushTask = Task.Run(async () =>
        {
            try
            {
                while (await _rsyncLogTimer.WaitForNextTickAsync(_rsyncLogCts.Token))
                {
                    FlushPendingRsyncLogs();
                }
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private async Task StopRsyncLogBatchingAsync()
    {
        if (_rsyncLogCts is not null)
            _rsyncLogCts.Cancel();

        if (_rsyncLogFlushTask is not null)
        {
            try
            {
                await _rsyncLogFlushTask;
            }
            catch (OperationCanceledException)
            {}
        }

        FlushPendingRsyncLogs();

        _rsyncLogTimer?.Dispose();
        _rsyncLogCts?.Dispose();
        _rsyncLogTimer = null;
        _rsyncLogCts = null;
        _rsyncLogFlushTask = null;
    }

    private void FlushPendingRsyncLogs()
    {
        List<string> logsToSend;
        lock (_rsyncLogLock)
        {
            if (_pendingRsyncLogs.Count == 0)
                return;

            logsToSend = [.. _pendingRsyncLogs];
            _pendingRsyncLogs.Clear();
        }

        OnRsyncLogs?.Invoke(this, logsToSend);
    }
}