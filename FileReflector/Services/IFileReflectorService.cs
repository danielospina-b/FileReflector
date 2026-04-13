using FileReflector.Models;

namespace FileReflector.Services;

public interface IFileReflectorService
{
    void TestCommand();
    Task<FileTreeNode> GetFilesFromRemote();
    Task<FileTreeNode> GetFilesFromLocal();
    Task SyncFilesToLocal(List<string> fileList);
    event EventHandler<string>? OnRsyncLogs;
}
