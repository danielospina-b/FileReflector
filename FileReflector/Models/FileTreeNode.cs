using AntDesign;

namespace FileReflector.Models;

public record FileTreeNode
{
    public string Name { get; set; }
    public bool IsFile { get; set; }
    public string FullPath { get; set; }
    public List<FileTreeNode> Children { get; set; } = new();
    public string DisplayName { get => IsFile ? $"📃 {Name}" : $"📂 {Name}"; }

    public FileTreeNode(string name, string fullPath, bool isFile = false)
    {
        Name = name;
        IsFile = isFile;
        FullPath = fullPath;
    }

    public override string ToString()
    {
        return ToString(0);
    }

    private string ToString(int indentLevel)
    {
        var indent = new string(' ', indentLevel * 2);
        var result = $"{indent}{(IsFile ? "📄" : "📁")} {Name}\n";

        foreach (var child in Children.OrderBy(c => c.IsFile).ThenBy(c => c.Name))
        {
            result += child.ToString(indentLevel + 1);
        }

        return result;
    }

    public List<string> GetFilesList()
    {
        List<string> filesList = [];
        foreach (var child in Children.OrderBy(c => c.IsFile).ThenBy(c => c.Name))
        {
            if (child.Children.Count > 0)
            {
                filesList.AddRange(child.GetFilesList());
            }
            else
            {
                filesList.Add(child.GetPathIfFile());
            }
        }
        return filesList;
    }

    public string GetPathIfFile()
    {
        if (IsFile) return FullPath;
        return string.Empty;
    }
}
