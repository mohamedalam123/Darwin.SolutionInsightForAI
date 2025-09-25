using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Models
{
    /// <summary>
    /// Represents a C# class (or record/class-like) and its metadata extracted via Roslyn.
    /// </summary>
    public sealed class ClassInfo
    {
        public required string Name { get; init; }

        /// <summary>Optional leading comment text associated with the class/record (cleaned; XML tags and /// removed).</summary>
        public string? Comment { get; init; }

        /// <summary>Full declaration signature for the class/record (e.g., "public class Foo&lt;T&gt;").</summary>
        public string? Signature { get; init; }

        public List<MethodInfo> Methods { get; } = new();
    }
}
