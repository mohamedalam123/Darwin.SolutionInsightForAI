using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Models
{
    /// <summary>
    /// Options for the Full Code Extract task.
    /// </summary>
    public sealed class FullCodeExtractOptions
    {
        /// <summary>The root path to extract from.</summary>
        public required string RootPath { get; init; }


        /// <summary>Whether to include subdirectories in the extraction.</summary>
        public bool IncludeSubdirectories { get; init; } = true;
    }
}
