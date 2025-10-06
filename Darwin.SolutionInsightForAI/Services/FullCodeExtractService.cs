using Darwin.SolutionInsightForAI.Models;
using Darwin.SolutionInsightForAI.Utilities;
using System.Text;

namespace Darwin.SolutionInsightForAI.Services;

/// <summary>
/// Reads all .cs and .cshtml files under a path (optionally recursive) and aggregates their full, verbatim content
/// into a single file. Each file is delimited by explicit START/END markers and shows its absolute path.
/// The output file name is derived from the input path by removing the drive and any prefix up to and including '\src\',
/// then replacing directory separators with dots. Example:
/// Input:  E:\_Projects\Darwin\src\Darwin.Web\Areas\Admin
/// Output: Darwin.Web.Areas.Admin.txt
/// </summary>
public sealed class FullCodeExtractService
{
    private readonly OutputWriter _writer;

    public FullCodeExtractService(OutputWriter writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// Executes the extraction and writes a single output file named based on the input path
    /// (e.g., Darwin.Web.Areas.Admin.txt). The content of each file is included verbatim (no transformations).
    /// </summary>
    public string Execute(FullCodeExtractOptions options)
    {
        if (!Directory.Exists(options.RootPath))
        {
            throw new DirectoryNotFoundException($"Root path not found: {options.RootPath}");
        }

        // Build output file name from the provided path
        var outputFile = BuildFileNameFromInputPath(options.RootPath);

        // Collect .cs and .cshtml files
        var searchOption = options.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(options.RootPath, "*", searchOption)
                             .Where(f =>
                             {
                                 var ext = Path.GetExtension(f);
                                 return ext.Equals(".cs", StringComparison.OrdinalIgnoreCase)
                                     || ext.Equals(".cshtml", StringComparison.OrdinalIgnoreCase);
                             })
                             .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                             .ToList();

        var sb = new StringBuilder();

        // ---- Header for AI (format guide) ----
        sb.AppendLine("### Solution Insight for AI – Full Code Extract");
        sb.AppendLine();
        sb.AppendLine("Format:");
        sb.AppendLine("Each file is wrapped by two markers, and the content between them is the verbatim file content:");
        sb.AppendLine("-----8<----- [FILE START] <FULL_PATH> -----");
        sb.AppendLine("-----8<----- [FILE END]   <FULL_PATH> -----");
        sb.AppendLine("Notes:");
        sb.AppendLine("- FULL_PATH is the absolute Windows path of the file.");
        sb.AppendLine("- The code is copied exactly as-is (no normalization or reformatting).");
        sb.AppendLine("- File types included: .cs, .cshtml");
        sb.AppendLine();
        sb.AppendLine($"Root: {Path.GetFullPath(options.RootPath)}");
        sb.AppendLine($"Include subfolders: {options.IncludeSubdirectories}");
        sb.AppendLine();

        foreach (var file in files)
        {
            var fullPath = Path.GetFullPath(file); // absolute path with backslashes

            // Begin marker
            sb.AppendLine($"-----8<----- [FILE START] {fullPath} -----");

            // Read verbatim and append exactly (preserving CRLF/LF as in source).
            var content = File.ReadAllText(file);

            // Append content exactly; do NOT use AppendLine(content) to avoid forcing an extra newline.
            sb.Append(content);

            // Ensure there is a newline BEFORE the END marker (only if the file didn't end with one)
            if (!(content.EndsWith("\n") || content.EndsWith("\r")))
            {
                sb.AppendLine();
            }

            // End marker and a blank line separator
            sb.AppendLine($"-----8<----- [FILE END]   {fullPath} -----");
            sb.AppendLine();
        }

        var path = _writer.WriteAllText(outputFile, sb.ToString());
        return path;
    }

    /// <summary>
    /// Builds the output file name from the given input path by:
    /// - Normalizing to absolute path with backslashes
    /// - Removing drive root and any prefix up to and including '\src\'
    /// - Replacing '\' and '/' with '.'
    /// - Removing invalid filename characters
    /// - Appending '.txt'
    /// </summary>
    private static string BuildFileNameFromInputPath(string inputPath)
    {
        var full = Path.GetFullPath(inputPath)
                       .TrimEnd('\\', '/')
                       .Replace('/', '\\');

        const string srcMarker = "\\src\\";
        string tail;

        var idx = full.IndexOf(srcMarker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            tail = full[(idx + srcMarker.Length)..];
        }
        else
        {
            // Fallback: drop drive root like 'E:\'
            var root = Path.GetPathRoot(full) ?? string.Empty;
            tail = full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full[root.Length..] : full;
        }

        tail = tail.Trim('\\', '/');

        // Replace separators with dots
        var fileStem = tail.Replace('\\', '.').Replace('/', '.');

        // Remove invalid file name characters
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var ch in invalid)
        {
            fileStem = fileStem.Replace(ch, '_');
        }

        if (string.IsNullOrWhiteSpace(fileStem))
            fileStem = "FullCodeExtract";

        return fileStem + ".txt";
    }
}
