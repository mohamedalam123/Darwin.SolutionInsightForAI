using Darwin.SolutionInsightForAI.Models;
using Darwin.SolutionInsightForAI.Services;
using Darwin.SolutionInsightForAI.Config;
using Darwin.SolutionInsightForAI.Utilities;
using Microsoft.Extensions.Configuration;

namespace Darwin.SolutionInsightForAI
{
    internal static class Program
    {
        private static async Task<int> Main()
        {
            // Initialize configuration (JSON + environment variables)
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Load strongly-typed options with safe defaults
            var opts = configuration.GetSection("GeneratorOptions").Get<GeneratorOptions>() ?? new GeneratorOptions();

            // Initialize helper utilities
            var prompter = new ConsolePrompter();

            // IMPORTANT: pass OutputRoot so that files are saved under E:\_Projects\Darwin.Files
            var writer = new OutputWriter(opts.Paths.OutputRoot);

            var runner = new TaskRunner(prompter, writer);

            // Present a simple header (English per requirement)
            Console.WriteLine("=============================================");
            Console.WriteLine("  Darwin.Solution Insight for AI (Phase 1)");
            Console.WriteLine("  .NET 9 Console Application");
            Console.WriteLine("=============================================\n");

            try
            {
                // Ask user to pick the task kind
                var task = prompter.AskEnum(
                    title: "Select a Task",
                    description: "Choose what you want the app to do.",
                    defaultValue: TaskKind.ProjectMapping,
                    options: new[]
                    {
                        TaskKind.ProjectMapping,
                        TaskKind.FullCodeExtract
                    });

                switch (task)
                {
                    case TaskKind.ProjectMapping:
                        {
                            // Default path from configuration (E:\_Projects\Darwin), fallback to current if missing
                            var defaultPath = string.IsNullOrWhiteSpace(opts.Paths.SolutionRoot) ? Directory.GetCurrentDirectory() : opts.Paths.SolutionRoot;

                            var solutionPath = prompter.AskPath(
                                prompt: "Enter the path to the .NET solution or root folder",
                                defaultValue: defaultPath);

                            // Ask whether to include class comments (default: true)
                            var includeClassComments = prompter.AskYesNo(
                                prompt: "Also extract class comments?",
                                defaultYes: true);

                            // Ask whether to include method comments (default: false)
                            var includeMethodComments = prompter.AskYesNo(
                                prompt: "Also extract method comments?",
                                defaultYes: false);

                            var options = new ProjectMappingOptions
                            {
                                RootPath = solutionPath,
                                IncludeClassComments = includeClassComments,
                                IncludeMethodComments = includeMethodComments
                            };

                            await runner.RunProjectMappingAsync(options);
                            break;
                        }
                    case TaskKind.FullCodeExtract:
                        {
                            // Default path from configuration (E:\_Projects\Darwin\src\Darwin.Domain), fallback to current if missing
                            var defaultPath = string.IsNullOrWhiteSpace(opts.Paths.DomainRoot) ? Directory.GetCurrentDirectory() : opts.Paths.DomainRoot;

                            var path = prompter.AskPath(
                                prompt: "Enter the path within your project to extract from",
                                defaultValue: defaultPath);

                            var includeSubfolders = prompter.AskYesNo(
                                prompt: "Include subfolders as well?",
                                defaultYes: true);

                            var options = new FullCodeExtractOptions
                            {
                                RootPath = path,
                                IncludeSubdirectories = includeSubfolders
                            };

                            await runner.RunFullCodeExtractAsync(options);
                            break;
                        }
                    default:
                        Console.WriteLine("Unknown task selected.");
                        return 1;
                }

                Console.WriteLine("\nAll done. Press any key to exit.");
                Console.ReadKey();
                return 0;
            }
            catch (Exception ex)
            {
                // Friendly error handling
                Console.WriteLine($"\nAn error occurred: {ex.Message}");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return 2;
            }
        }
    }
}
