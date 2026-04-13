using System.Text;
using System.Text.RegularExpressions;
using FileReflector.Models;

namespace FileReflector.Helpers;

public class PathsProcessor
{
    public static FileTreeNode BuildDirectoryTree(IEnumerable<string> lines)
    {
        var rootNode = new FileTreeNode(name: ".", fullPath: "");

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var spaceIndex = line.IndexOf(' ');
            if (spaceIndex == -1) continue;

            var type = line[0];
            var fullPath = line.Substring(spaceIndex + 1);
            if (string.IsNullOrEmpty(fullPath)) continue;
            var parts = fullPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var currentNode = rootNode;
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                bool isLast = i == parts.Length - 1;
                bool isFile = isLast && type == 'f';

                if (!currentNode.Children.Any(child => child.Name == part))
                {
                    if (!isFile) currentNode.Children.Insert(0, new FileTreeNode(part, fullPath, isFile));
                    else currentNode.Children.Add(new FileTreeNode(part, fullPath, isFile));
                }

                currentNode = currentNode.Children.FirstOrDefault(child => child.Name == part)!;
            }
        }
        return rootNode;
    }

    public static string GetRsyncEscapedPathsWithParents(List<string> paths)
    {
        var result = new StringBuilder();

        foreach (var path in paths)
        {
            string[] parts = path.Split('/');
            string currentPath = "";

            for (int i = 0; i < parts.Length; i++)
            {
                currentPath = Path.Combine(currentPath, parts[i]);

                if (i < parts.Length - 1)
                {
                    result.AppendLine(EscapeSpecialChars(currentPath + "/"));
                }
                else
                {
                    result.AppendLine(EscapeSpecialChars(currentPath));
                }
            }
        }

        return result.ToString();
    }

    private static string EscapeSpecialChars(string input)
    {
        return Regex.Replace(input, @"([\*\[\]\?])", @"\$1");
    }
}