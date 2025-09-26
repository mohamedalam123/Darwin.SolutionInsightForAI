using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Models
{
    /// <summary>
    /// Represents a C# type (class/record/interface/struct) and its extracted metadata.
    /// </summary>
    public sealed class ClassInfo
    {
        /// <summary>Simple name of the type (identifier text).</summary>
        public required string Name { get; init; }

        /// <summary>High-level kind of the type: "Class", "Record", "Interface", or "Struct".</summary>
        public required string Kind { get; init; }

        /// <summary>Cleaned, single-line leading comment associated with the type (XML tags and /// removed).</summary>
        public string? Comment { get; init; }

        /// <summary>Full type declaration signature (e.g., "public interface IFoo<T>").</summary>
        public string? Signature { get; init; }

        /// <summary>Declared methods/constructors (signatures only) under this type.</summary>
        public List<MethodInfo> Methods { get; } = new();
    }
}
