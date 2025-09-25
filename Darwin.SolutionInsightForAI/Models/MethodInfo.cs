using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Models
{
    /// <summary>
    /// Represents a method signature extracted via Roslyn.
    /// </summary>
    public sealed class MethodInfo
    {
        /// <summary>Full signature line as it appears (trimmed); includes modifiers, return type, name, and parameters.</summary>
        public required string SignatureLine { get; init; }
    }
}
