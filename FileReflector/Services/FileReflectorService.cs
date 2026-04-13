using CliWrap;
using CliWrap.Buffered;
using FileReflector.Helpers;
using FileReflector.Models;

namespace FileReflector.Services;

public class FileReflectorService : IFileReflectorService
{
    private readonly ILogger<FileReflectorService> _logger;
    private readonly UserConfigurationService _userConfigurationService;
    
    public event EventHandler<string>? OnRsyncLogs;

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
        var findCommand = $"cd {remoteFolder} && " + @"find . -printf ""%y %P\n""";
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
        var findCommand = $"cd {localFolder} && " + @"find . -printf ""%y %P\n""";
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
        var rsyncFileList = PathsProcessor.GetRsyncEscapedPathsWithParents(fileList);
        var localFolder = _userConfigurationService.CurrentSettings.DestinationPath;
        var remoteHost = _userConfigurationService.CurrentSettings.RemoteHost;
        var remoteFolder = _userConfigurationService.CurrentSettings.SourcePath;

        _logger.LogInformation("rsync summary: remoteHost={RemoteHost}, remoteFolder={RemoteFolder}, localFolder={LocalFolder}, include-from=\n{RsyncFileList}", remoteHost, remoteFolder, localFolder, rsyncFileList);

        await Cli.Wrap("rsync")
            .WithArguments(args => args
                .Add("-a").Add("-vv")
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

    private void LogDebugRsync(string log)
    {
        _logger.LogDebug($"rsync> {log}");
        OnRsyncLogs?.Invoke(this, log);
    }
}
