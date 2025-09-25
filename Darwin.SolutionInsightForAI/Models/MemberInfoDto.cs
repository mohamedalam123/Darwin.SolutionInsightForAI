using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Models
{
    public sealed class MemberInfoDto
    {
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string Signature { get; set; } = "";
        public string SummaryComment { get; set; } = "";
    }
}
