// Models/SyncSettings.cs
namespace FileReflector.Models
{
    public class SyncSettings
    {
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public string RemoteHost { get; set; } = string.Empty;
    }
}