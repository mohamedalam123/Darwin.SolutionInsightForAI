using System.Text;

namespace Darwin.SolutionInsightForAI.Utilities
{
    /// <summary>
    /// Provides helpers to build and write output files with UTF-8 encoding under a configured output root.
    /// </summary>
    public sealed class OutputWriter
    {
        private readonly string _outputRoot;

        /// <summary>
        /// Creates a new writer that writes files under the given output root directory.
        /// </summary>
        public OutputWriter(string outputRoot)
        {
            _outputRoot = string.IsNullOrWhiteSpace(outputRoot) ? Directory.GetCurrentDirectory() : outputRoot;
            Directory.CreateDirectory(_outputRoot);
        }

        /// <summary>
        /// Builds a dated file name based on a logical prefix and the current date (yyyyMMdd).
        /// </summary>
        public string BuildDatedFileName(string prefix, string extensionWithoutDot = "txt")
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            return $"{prefix}_{date}.{extensionWithoutDot}";
        }

        /// <summary>
        /// Writes text to a file under the configured output root.
        /// Returns the absolute path written.
        /// </summary>
        public string WriteAllText(string fileName, string content)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_outputRoot, fileName));
            File.WriteAllText(fullPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return fullPath;
        }
    }
}
