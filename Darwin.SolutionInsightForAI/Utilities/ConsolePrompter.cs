using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Utilities
{
    /// <summary>
    /// Provides small helpers to ask the user questions in the console with defaults.
    /// All texts are in English by requirement.
    /// </summary>
    public sealed class ConsolePrompter
    {
        /// <summary>
        /// Asks the user to select from an enum with a default value.
        /// </summary>
        public T AskEnum<T>(string title, string description, T defaultValue, IEnumerable<T> options) where T : Enum
        {
            Console.WriteLine($"{title}");
            Console.WriteLine(description);


            // Print menu options with indices for clarity
            var arr = options.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                Console.WriteLine($" {i + 1}. {arr[i]}");
            }


            Console.Write($"Enter number [default: {defaultValue}]: ");
            var input = Console.ReadLine();


            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }


            if (int.TryParse(input, out int choice))
            {
                var idx = choice - 1;
                if (idx >= 0 && idx < arr.Length)
                {
                    return arr[idx];
                }
            }


            // If parsing fails, fall back to default
            Console.WriteLine("Invalid input; using default.");
            return defaultValue;
        }


        /// <summary>
        /// Asks for a filesystem path with default value.
        /// </summary>
        public string AskPath(string prompt, string defaultValue)
        {
            Console.Write($"{prompt} [default: {defaultValue}]: ");
            var input = Console.ReadLine();
            var path = string.IsNullOrWhiteSpace(input) ? defaultValue : input.Trim();


            return Path.GetFullPath(path);
        }


        /// <summary>
        /// Asks a Yes/No question with a default answer.
        /// </summary>
        public bool AskYesNo(string prompt, bool defaultYes)
        {
            var def = defaultYes ? "Y" : "N";
            Console.Write($"{prompt} (y/n) [default: {def}]: ");
            var input = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();


            if (string.IsNullOrEmpty(input))
                return defaultYes;


            return input switch
            {
                "y" or "yes" or "true" => true,
                "n" or "no" or "false" => false,
                _ => defaultYes
            };
        }
    }
}
