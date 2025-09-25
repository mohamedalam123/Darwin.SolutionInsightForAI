using Darwin.SolutionInsightForAI.Models;
using Darwin.SolutionInsightForAI.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Exporters
{
    public sealed class AllSolutionFilesExporter
    {
        private readonly GeneratorOptions _opts;
        public AllSolutionFilesExporter(GeneratorOptions opts) => _opts = opts;

        public string Export(List<(string filePath, List<MemberInfoDto> members)> files)
        {
            // JSON
            var outputPath = System.IO.Path.Combine(_opts.Paths.OutputRoot, "All solution files.json");

            var payload = new
            {
                schema = "darwin/all-solution-files",
                schemaVersion = _opts.Export.SchemaVersion,
                generatedAtUtc = DateTime.UtcNow,
                files = files.Select(f => new
                {
                    FilePath = f.filePath,
                    Members = f.members
                })
            };

            var opts = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            Directory.CreateDirectory(_opts.Paths.OutputRoot);
            File.WriteAllText(outputPath, JsonSerializer.Serialize(payload, opts));

            return outputPath;
        }
    }
}
