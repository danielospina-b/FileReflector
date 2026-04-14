// Services/UserConfigurationService.cs
using FileReflector.Models;

namespace FileReflector.Services;

public class UserConfigurationService
{
    private readonly string _environmentSourcePath;
    private readonly string _environmentDestinationPath;
    private readonly string _environmentRemoteHost;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<UserConfigurationService> _logger;
    private const string SOURCEPATH_ENV_NAME = "FILEREFLECTOR_REMOTESOURCEPATH";
    private const string DESTINATIONPATH_ENV_NAME = "FILEREFLECTOR_LOCALDESTINATIONPATH";
    private const string REMOTEHOST_ENV_NAME = "FILEREFLECTOR_REMOTEHOST";

    public UserConfigurationService(ILogger<UserConfigurationService> logger, IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _environmentSourcePath = Environment.GetEnvironmentVariable(SOURCEPATH_ENV_NAME) ?? string.Empty;
        _environmentDestinationPath = Environment.GetEnvironmentVariable(DESTINATIONPATH_ENV_NAME) ?? string.Empty;
        _environmentRemoteHost = Environment.GetEnvironmentVariable(REMOTEHOST_ENV_NAME) ?? string.Empty;
        _applicationLifetime = applicationLifetime;
    }

    private void StopApplication(string reason)
    {
        _logger.LogError(reason);
        _applicationLifetime.StopApplication();
    }

    public void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_environmentRemoteHost)) StopApplication($"{REMOTEHOST_ENV_NAME} environment variable not found.");
        _logger.LogInformation("Using Remote Source Host: [{RemoteSourceHost}]", _environmentRemoteHost);
        if (string.IsNullOrWhiteSpace(_environmentSourcePath)) StopApplication($"{SOURCEPATH_ENV_NAME} environment variable not found.");
        _logger.LogInformation("Using Remote Source Path: [{RemoteSourcePath}]", _environmentSourcePath);
        if (string.IsNullOrWhiteSpace(_environmentDestinationPath)) StopApplication($"{DESTINATIONPATH_ENV_NAME} environment variable not found.");
        _logger.LogInformation("Using Remote Source Path: [{RemoteDestinationHost}]", _environmentDestinationPath);
    }

    /// <summary>
    /// Provides the current active settings, prioritizing environment variables,
    /// then user-persisted overrides.
    /// </summary>
    public SyncSettings CurrentSettings
    {
        get
        {
            return new SyncSettings
            {
                SourcePath = _environmentSourcePath,
                DestinationPath = _environmentDestinationPath,
                RemoteHost = _environmentRemoteHost
            };
        }
    }
}