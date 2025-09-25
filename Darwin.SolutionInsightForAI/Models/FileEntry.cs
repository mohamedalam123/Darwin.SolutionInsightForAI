using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Models
{
    /// <summary>
    /// Represents a discovered file in the mapping.
    /// For .cs files, additional code model info may be collected elsewhere.
    /// </summary>
    public sealed class FileEntry
    {
        public required string FullPath { get; init; }
        public required string RelativePath { get; init; }
        public required string Extension { get; init; }
    }
}
