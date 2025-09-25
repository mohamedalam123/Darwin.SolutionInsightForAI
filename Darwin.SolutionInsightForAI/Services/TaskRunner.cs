using Darwin.SolutionInsightForAI.Models;
using Darwin.SolutionInsightForAI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Services
{
    /// <summary>
    /// Orchestrates execution of the selected task.
    /// </summary>
    public sealed class TaskRunner
    {
        private readonly ConsolePrompter _prompter;
        private readonly OutputWriter _writer;


        public TaskRunner(ConsolePrompter prompter, OutputWriter writer)
        {
            _prompter = prompter;
            _writer = writer;
        }


        /// <summary>
        /// Executes the Project Mapping workflow end-to-end.
        /// </summary>
        public async Task RunProjectMappingAsync(ProjectMappingOptions options)
        {
            // Instantiate the service responsible for project mapping
            var service = new ProjectMappingService(_writer);


            // Perform the mapping (IO-bound; wrap in Task.Run to avoid blocking if needed)
            var outputPath = await Task.Run(() => service.Execute(options));


            Console.WriteLine($"\nMapping completed. Output written to: {outputPath}");
        }


        /// <summary>
        /// Executes the Full Code Extract workflow end-to-end.
        /// </summary>
        public async Task RunFullCodeExtractAsync(FullCodeExtractOptions options)
        {
            var service = new FullCodeExtractService(_writer);
            var outputPath = await Task.Run(() => service.Execute(options));
            Console.WriteLine($"\nExtraction completed. Output written to: {outputPath}");
        }
    }
}
