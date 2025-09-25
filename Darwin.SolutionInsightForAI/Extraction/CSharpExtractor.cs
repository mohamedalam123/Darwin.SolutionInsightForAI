using Darwin.SolutionInsightForAI.Models;
using Darwin.SolutionInsightForAI.Config;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Darwin.SolutionInsightForAI.Extraction
{
    /// <summary>
    /// Extracts high-level members from a C# source file using Roslyn.
    /// Builds readable signatures for types, methods, and constructors.
    /// </summary>
    public sealed class CSharpExtractor
    {
        private readonly GeneratorOptions _opts;
        public CSharpExtractor(GeneratorOptions opts) => _opts = opts;

        public (string filePath, List<MemberInfoDto> members) Extract(string filePath, string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetRoot();

            var members = new List<MemberInfoDto>();

            foreach (var node in root.DescendantNodes())
            {
                if (node is ClassDeclarationSyntax c)
                {
                    members.Add(CreateDto(c.Identifier.Text, "Class", c));
                }
                else if (node is InterfaceDeclarationSyntax i)
                {
                    members.Add(CreateDto(i.Identifier.Text, "Interface", i));
                }
                else if (node is StructDeclarationSyntax s)
                {
                    members.Add(CreateDto(s.Identifier.Text, "Struct", s));
                }
                else if (node is RecordDeclarationSyntax r)
                {
                    members.Add(CreateDto(r.Identifier.Text, "Record", r));
                }
                else if (node is MethodDeclarationSyntax m)
                {
                    members.Add(CreateDto(m.Identifier.Text, "Method", m, BuildMethodSignature(m)));
                }
                else if (node is ConstructorDeclarationSyntax ctor)
                {
                    members.Add(CreateDto(ctor.Identifier.Text, "Constructor", ctor, BuildCtorSignature(ctor)));
                }
            }

            // Keep Windows-style backslashes (no normalization to forward slashes)
            var normalizedPath = System.IO.Path.GetFullPath(filePath);

            // Assign file path to all members
            foreach (var m in members) m.FilePath = normalizedPath;

            return (normalizedPath, members);
        }

        private MemberInfoDto CreateDto(string name, string kind, SyntaxNode node, string? signature = null)
        {
            // Extract first XML doc trivia (if any)
            var xml = node.GetLeadingTrivia()
                          .Select(t => t.GetStructure())
                          .OfType<DocumentationCommentTriviaSyntax>()
                          .FirstOrDefault()?.ToFullString();

            // Strip <summary>...</summary> and other xml-ish tags, condense whitespace
            var summary = CleanXmlDoc(xml);

            return new MemberInfoDto
            {
                Name = name,
                Kind = kind,
                Signature = signature ?? BuildTypeSignature(node) ?? string.Empty,
                SummaryComment = summary
            };
        }

        private static string? BuildTypeSignature(SyntaxNode node)
        {
            // Use a classic switch statement to avoid parsing issues on some toolchains.
            string? sig = null;

            if (node is ClassDeclarationSyntax c)
            {
                sig = $"{string.Join(" ", c.Modifiers)} class {c.Identifier}{c.TypeParameterList}";
            }
            else if (node is InterfaceDeclarationSyntax i)
            {
                sig = $"{string.Join(" ", i.Modifiers)} interface {i.Identifier}{i.TypeParameterList}";
            }
            else if (node is StructDeclarationSyntax s)
            {
                sig = $"{string.Join(" ", s.Modifiers)} struct {s.Identifier}{s.TypeParameterList}";
            }
            else if (node is RecordDeclarationSyntax r)
            {
                sig = $"{string.Join(" ", r.Modifiers)} record {r.Identifier}{r.TypeParameterList}";
            }

            return sig?.Trim();
        }

        private static string BuildMethodSignature(MethodDeclarationSyntax m)
        {
            var mods = string.Join(" ", m.Modifiers.Select(x => x.Text)).Trim();
            var ret = m.ReturnType.ToString();
            var name = m.Identifier.Text;
            var tps = m.TypeParameterList?.ToString() ?? "";
            var pars = m.ParameterList.ToString();
            var cons = string.Concat(m.ConstraintClauses.Select(c => " " + c.ToFullString().Trim()));
            return $"{(string.IsNullOrEmpty(mods) ? "" : mods + " ")}{ret} {name}{tps}{pars}{cons}".Trim();
        }

        private static string BuildCtorSignature(ConstructorDeclarationSyntax c)
        {
            var mods = string.Join(" ", c.Modifiers.Select(x => x.Text)).Trim();
            var name = c.Identifier.Text;
            var pars = c.ParameterList.ToString();
            return $"{(string.IsNullOrEmpty(mods) ? "" : mods + " ")}{name}{pars}".Trim();
        }

        private static string CleanXmlDoc(string? xmlish)
        {
            if (string.IsNullOrWhiteSpace(xmlish)) return string.Empty;

            // Remove common XML doc tags and compress whitespace.
            var noTags = System.Text.RegularExpressions.Regex.Replace(
                xmlish, @"</?summary>|</?remarks>|</?para>|</?list.*?>|</?item>|</?c>|</?see.*?/?>(\s*)|</?typeparam.*?>|</?inheritdoc.*?/?>(\s*)",
                " ",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

            var normalized = System.Text.RegularExpressions.Regex.Replace(noTags, @"\s+", " ").Trim();
            return normalized;
        }
    }
}
