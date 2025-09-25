using Darwin.SolutionInsightForAI.Models;
using Darwin.SolutionInsightForAI.Parsing;
using Darwin.SolutionInsightForAI.Utilities;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Darwin.SolutionInsightForAI.Services
{
    /// <summary>
    /// Scans a solution/root folder, lists important files, and for .cs files extracts classes and method signatures.
    /// Writes a single JSON output file named like ProjectMapping_YYYYMMDD.json under the configured output root.
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

        /// <summary>
        /// Executes the mapping and writes JSON output (schema + members per file).
        /// </summary>
        public string Execute(ProjectMappingOptions options)
        {
            // Determine output file name (JSON)
            var outputFile = _writer.BuildDatedFileName("ProjectMapping", "json");

            if (!Directory.Exists(options.RootPath))
            {
                throw new DirectoryNotFoundException($"Root path not found: {options.RootPath}");
            }

            // Build the payload
            var filesPayload = new List<object>();

            var files = Directory.EnumerateFiles(options.RootPath, "*", SearchOption.AllDirectories)
                .Where(f => ImportantExtensions.Contains(Path.GetExtension(f)))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var file in files)
            {
                // Keep Windows-style paths (backslashes). JSON will escape them to \\ as expected.
                var absPath = Path.GetFullPath(file);

                // If not C#, just emit an empty Members array (to align with the sample structure).
                if (!Path.GetExtension(file).Equals(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    filesPayload.Add(new
                    {
                        FilePath = absPath,
                        Members = Array.Empty<object>()
                    });
                    continue;
                }

                // Parse .cs files: classes/records + method/ctor signatures (full line until before '{')
                var code = File.ReadAllText(file);
                var classes = _parser.ExtractClasses(code, options.IncludeClassComments, options.IncludeMethodComments);

                // Flatten to "Members" to mirror sample structure but richer (Signature for methods/ctors)
                var members = new List<object>();
                foreach (var cls in classes)
                {
                    members.Add(new
                    {
                        Name = cls.Name,
                        Kind = "Class",
                        FilePath = absPath,
                        SummaryComment = cls.Comment ?? string.Empty
                    });

                    // Methods (full signature line as requested)
                    foreach (var m in cls.Methods)
                    {
                        members.Add(new
                        {
                            Name = ExtractMethodNameFromSignature(m.SignatureLine),
                            Kind = "Method",
                            FilePath = absPath,
                            SummaryComment = string.Empty, // class-level comments already captured; method doc can be added if IncludeMethodComments was true and you choose to extract it similarly
                            Signature = m.SignatureLine
                        });
                    }
                }

                filesPayload.Add(new
                {
                    FilePath = absPath,
                    Members = members
                });
            }

            var payload = new
            {
                schema = "darwin/project-mapping",
                schemaVersion = "1.2",
                generatedAtUtc = DateTime.UtcNow,
                root = Path.GetFullPath(options.RootPath),
                files = filesPayload
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Write JSON
            var path = _writer.WriteAllText(outputFile, JsonSerializer.Serialize(payload, jsonOptions));
            return path;
        }

        /// <summary>
        /// Extracts method name from a signature line. Best-effort parse: splits by '(' and takes the token before it.
        /// Example: "private static string CondenseToOneLine(string text)" -> "CondenseToOneLine"
        /// </summary>
        private static string ExtractMethodNameFromSignature(string signatureLine)
        {
            // Strip generics/return type tokens by taking the last token before '('
            var beforeParen = signatureLine.Split('(')[0];
            var tokens = beforeParen.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return tokens.Length == 0 ? string.Empty : tokens[^1];
        }
    }
}
