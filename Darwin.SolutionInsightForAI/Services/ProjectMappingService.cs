using Darwin.SolutionInsightForAI.Models;
using Darwin.SolutionInsightForAI.Parsing;
using Darwin.SolutionInsightForAI.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

namespace Darwin.SolutionInsightForAI.Services;

/// <summary>
/// Scans a root folder, lists important files, and for .cs files extracts types and member signatures.
/// Writes a single JSON file like ProjectMapping_YYYYMMDD.json under the configured output root.
/// </summary>
public sealed class ProjectMappingService
{
    private static readonly HashSet<string> ImportantExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".cshtml", ".html", ".htm", ".js", ".css"
    };

    private readonly OutputWriter _writer;
    private readonly CSharpCodeParser _parser = new();

    public ProjectMappingService(OutputWriter writer)
    {
        _writer = writer;
    }

    public string Execute(ProjectMappingOptions options)
    {
        var outputFile = _writer.BuildDatedFileName("ProjectMapping", "json");

        if (!Directory.Exists(options.RootPath))
            throw new DirectoryNotFoundException($"Root path not found: {options.RootPath}");

        var filesSection = new List<object>();

        var files = Directory.EnumerateFiles(options.RootPath, "*", SearchOption.AllDirectories)
            .Where(f => ImportantExtensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var file in files)
        {
            var absPath = Path.GetFullPath(file); // Windows-style backslashes (JSON escapes as \\)

            if (!Path.GetExtension(file).Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                filesSection.Add(new
                {
                    filePath = absPath,
                    members = Array.Empty<object>()
                });
                continue;
            }

            var code = File.ReadAllText(file);
            var types = _parser.ExtractClasses(code, options.IncludeClassComments, options.IncludeMethodComments);

            var members = new List<object>();

            foreach (var t in types)
            {
                // Emit the type (class/record/interface/struct)
                members.Add(new
                {
                    name = t.Name,
                    kind = t.Kind,                // "Class", "Record", "Interface", "Struct"
                    signature = t.Signature,      // e.g., "public interface IFoo<T>"
                    summaryComment = t.Comment ?? string.Empty
                });

                // Emit methods/constructors
                if (t.Methods.Count > 0)
                {
                    foreach (var m in t.Methods)
                    {
                        members.Add(new
                        {
                            name = ExtractMethodNameFromSignature(m.SignatureLine),
                            kind = "Method",
                            signature = m.SignatureLine,
                            summaryComment = m.Comment ?? string.Empty
                        });
                    }
                }
            }

            filesSection.Add(new
            {
                filePath = absPath,
                members
            });
        }

        var payload = new
        {
            schema = "darwin/project-mapping",
            schemaVersion = "1.2",
            generatedAtUtc = DateTime.UtcNow,
            root = Path.GetFullPath(options.RootPath),
            files = filesSection
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Preserve '<' and '>' in generics
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var path = _writer.WriteAllText(outputFile, JsonSerializer.Serialize(payload, jsonOptions));
        return path;
    }

    /// <summary>
    /// Extracts method name from a signature line by taking the token before '('.
    /// Example: "public async Task<Guid> HandleAsync(...)" -> "HandleAsync"
    /// </summary>
    private static string ExtractMethodNameFromSignature(string signatureLine)
    {
        var beforeParen = signatureLine.Split('(')[0];
        var tokens = beforeParen.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length == 0 ? string.Empty : tokens[^1];
    }
}
