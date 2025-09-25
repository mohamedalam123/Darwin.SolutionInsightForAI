namespace Darwin.SolutionInsightForAI.Config
{
    public sealed class GeneratorOptions
    {
        public PathsOptions Paths { get; set; } = new();
        public ExportOptions Export { get; set; } = new();

        public sealed class PathsOptions
        {
            public string SolutionRoot { get; set; } = "E:/_Projects/Darwin";
            public string DomainRoot { get; set; } = "E:/_Projects/Darwin/src/Darwin.Domain";
            public string OutputRoot { get; set; } = "E:/_Projects/Darwin.Files";
        }

        public sealed class ExportOptions
        {
            public string AllSolutionFilesFormat { get; set; } = "json";
            public bool NormalizePathsToForwardSlashes { get; set; } = true;
            public bool StripXmlDocSummaryTags { get; set; } = true;
            public bool DecodeEscapedXml { get; set; } = true;
            public string SchemaVersion { get; set; } = "1.1";
        }
    }
}