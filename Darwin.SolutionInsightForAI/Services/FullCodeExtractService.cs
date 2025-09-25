using Darwin.SolutionInsightForAI.Models;
using Darwin.SolutionInsightForAI.Utilities;
using System.Text;

namespace Darwin.SolutionInsightForAI.Services;

/// <summary>
/// Reads all .cs files under a path (optionally recursive) and aggregates their full, verbatim content
/// into a single file. Each file is delimited by explicit START/END markers and shows its absolute path.
/// </summary>
public sealed class FullCodeExtractService
{
    private readonly OutputWriter _writer;

    public FullCodeExtractService(OutputWriter writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// Executes the extraction and writes a single output file named like FullCodeExtract_YYYYMMDD.txt.
    /// The content of each .cs file is included verbatim (no transformations).
    /// </summary>
    public string Execute(FullCodeExtractOptions options)
    {
        // Output file name
        var outputFile = _writer.BuildDatedFileName("FullCodeExtract", "txt");

        if (!Directory.Exists(options.RootPath))
        {
            throw new DirectoryNotFoundException($"Root path not found: {options.RootPath}");
        }

        // Find .cs files
        var searchOption = options.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(options.RootPath, "*.cs", searchOption)
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

            // Ensure there is a newline BEFORE the END marker (only if the file didn't end with one),
            // so markers do not get glued to the last code line.
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
}
