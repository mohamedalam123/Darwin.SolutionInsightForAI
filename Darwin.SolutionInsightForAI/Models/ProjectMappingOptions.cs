using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Models
{
    /// <summary>
    /// Options for the Project Mapping task.
    /// </summary>
    public sealed class ProjectMappingOptions
    {
        /// <summary>The root folder (solution root) to scan.</summary>
        public required string RootPath { get; init; }


        /// <summary>Whether to include class-level comments above classes.</summary>
        public bool IncludeClassComments { get; init; } = true;


        /// <summary>Whether to include method-level comments above methods.</summary>
        public bool IncludeMethodComments { get; init; } = false;
    }
}
