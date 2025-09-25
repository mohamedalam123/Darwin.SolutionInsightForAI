using Darwin.SolutionInsightForAI.Models;
using Darwin.SolutionInsightForAI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Services
{
    /// <summary>
    /// Reads all .cs files under a path (optionally recursive) and aggregates their full content into a single file.
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
        /// </summary>
        public string Execute(FullCodeExtractOptions options)
        {
            // Build output name
            var outputFile = _writer.BuildDatedFileName("FullCodeExtract");


            if (!Directory.Exists(options.RootPath))
            {
                throw new DirectoryNotFoundException($"Root path not found: {options.RootPath}");
            }


            var searchOption = options.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.EnumerateFiles(options.RootPath, "*.cs", searchOption)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();


            var sb = new StringBuilder();
            sb.AppendLine($"Root: {options.RootPath}");
            sb.AppendLine($"Include subfolders: {options.IncludeSubdirectories}");
            sb.AppendLine();


            foreach (var file in files)
            {
                var rel = Path.GetRelativePath(options.RootPath, file).Replace('\\', '/');
                sb.AppendLine($"// ====================== FILE: /{rel} ======================");
                sb.AppendLine(File.ReadAllText(file));
                sb.AppendLine();
            }


            var path = _writer.WriteAllText(outputFile, sb.ToString());
            return path;
        }
    }
}
