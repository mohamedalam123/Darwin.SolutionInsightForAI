using Darwin.SolutionInsightForAI.Models;
using Darwin.SolutionInsightForAI.Parsing;
using Darwin.SolutionInsightForAI.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

namespace Darwin.SolutionInsightForAI.Services;

/// <summary>
/// Scans a root folder, lists important files, and for .cs files extracts classes/records and method/ctor signatures.
/// Writes a single JSON file like ProjectMapping_YYYYMMDD.json under the configured output root.
/// JSON schema: { schema, schemaVersion, generatedAtUtc, root, files: [ { filePath, members: [ { name, kind, signature, summaryComment } ] } ] }
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
            var absPath = Path.GetFullPath(file); // keep Windows-style backslashes (JSON will escape them as \\)
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
            var classes = _parser.ExtractClasses(code, options.IncludeClassComments, options.IncludeMethodComments);

            var members = new List<object>();

            foreach (var cls in classes)
            {
                // Class/record entry with signature and summary
                members.Add(new
                {
                    name = cls.Name,
                    kind = "Class",
                    signature = cls.Signature,              // e.g., "public class Foo<T>"
                    summaryComment = cls.Comment ?? string.Empty
                });

                if (cls.Methods.Count > 0)
                {
                    foreach (var m in cls.Methods)
                    {
                        members.Add(new
                        {
                            name = ExtractMethodNameFromSignature(m.SignatureLine),
                            kind = "Method",
                            signature = m.SignatureLine,      // full method signature (no '{')
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
            // Keep '<' and '>' as-is in signatures (avoid \u003C and \u003E)
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
