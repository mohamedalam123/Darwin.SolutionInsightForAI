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
        public string? Comment { get; init; }
        public List<MethodInfo> Methods { get; } = new();
    }
}
