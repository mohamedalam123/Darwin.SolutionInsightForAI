using Darwin.SolutionInsightForAI.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.SolutionInsightForAI.Parsing
{
    /// <summary>
    /// Uses Roslyn to parse C# source files and extract class names, leading comments, and method signatures.
    /// </summary>
    public sealed class CSharpCodeParser
    {
        /// <summary>
        /// Parse a C# file and return the extracted classes (with methods and optional comments).
        /// </summary>
        public IReadOnlyList<ClassInfo> ExtractClasses(
        string sourceCode,
        bool includeClassComments,
        bool includeMethodComments)
        {
            // Parse the source code into a Roslyn SyntaxTree
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetCompilationUnitRoot();


            var results = new List<ClassInfo>();


            // Walk through class-like declarations: classes, records, and potentially structs if needed
            foreach (var node in root.DescendantNodes())
            {
                if (node is ClassDeclarationSyntax classDecl)
                {
                    var info = BuildClassInfo(classDecl, includeClassComments, includeMethodComments);
                    results.Add(info);
                }
                else if (node is RecordDeclarationSyntax recordDecl)
                {
                    // Treat records similarly to classes for mapping purposes
                    var info = BuildRecordInfo(recordDecl, includeClassComments, includeMethodComments);
                    results.Add(info);
                }
            }


            return results;
        }

        private static ClassInfo BuildClassInfo(
            ClassDeclarationSyntax classDecl,
            bool includeClassComments,
            bool includeMethodComments)
        {
            // Extract class name
            var className = classDecl.Identifier.Text;


            // Extract leading comments if requested
            string? classComment = null;
            if (includeClassComments)
            {
                classComment = ExtractLeadingCommentText(classDecl);
            }


            var cls = new ClassInfo { Name = className, Comment = classComment };


            // Extract methods (including constructors)
            foreach (var member in classDecl.Members)
            {
                switch (member)
                {
                    case MethodDeclarationSyntax method:
                        cls.Methods.Add(new MethodInfo
                        {
                            SignatureLine = BuildMethodSignature(method, includeMethodComments)
                        });
                        break;
                    case ConstructorDeclarationSyntax ctor:
                        cls.Methods.Add(new MethodInfo
                        {
                            SignatureLine = BuildConstructorSignature(ctor, includeMethodComments)
                        });
                        break;
                }
            }


            return cls;
        }

        private static ClassInfo BuildRecordInfo(
            RecordDeclarationSyntax recordDecl,
            bool includeClassComments,
            bool includeMethodComments)
        {
            var name = recordDecl.Identifier.Text;
            string? classComment = null;
            if (includeClassComments)
            {
                classComment = ExtractLeadingCommentText(recordDecl);
            }


            var cls = new ClassInfo { Name = name, Comment = classComment };


            foreach (var member in recordDecl.Members)
            {
                switch (member)
                {
                    case MethodDeclarationSyntax method:
                        cls.Methods.Add(new MethodInfo
                        {
                            SignatureLine = BuildMethodSignature(method, includeMethodComments)
                        });
                        break;
                    case ConstructorDeclarationSyntax ctor:
                        cls.Methods.Add(new MethodInfo
                        {
                            SignatureLine = BuildConstructorSignature(ctor, includeMethodComments)
                        });
                        break;
                }
            }


            return cls;
        }

        /// <summary>
        /// Extracts a readable one-line comment text from leading trivia (XML doc or // comments).
        /// </summary>
        private static string? ExtractLeadingCommentText(SyntaxNode node)
        {
            var triviaList = node.GetLeadingTrivia();
            var sb = new StringBuilder();


            foreach (var trivia in triviaList)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                {
                    // Strip leading // and trim
                    var text = trivia.ToFullString().TrimStart('/').Trim();
                    if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
                }
                else if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    // Remove /* */ and condense
                    var text = trivia.ToFullString()
                    .Replace("/*", string.Empty)
                    .Replace("*/", string.Empty)
                    .Trim();
                    if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
                }
                else if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    // Convert XML doc trivia to a simple one-line string
                    var xml = trivia.ToFullString();
                    var condensed = CondenseXmlDocToSingleLine(xml);
                    if (!string.IsNullOrWhiteSpace(condensed)) sb.AppendLine(condensed);
                }
            }


            var result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        /// <summary>
        /// Builds a method signature line including modifiers, return type, name, and parameters.
        /// If method comments are requested, appends an inline comment with condensed doc.
        /// </summary>
        private static string BuildMethodSignature(MethodDeclarationSyntax method, bool includeMethodComments)
        {
            // Collect modifiers (public, private, static, etc.)
            var modifiers = string.Join(" ", method.Modifiers.Select(m => m.Text));
            var returnType = method.ReturnType.ToString();
            var name = method.Identifier.Text;
            var parameters = method.ParameterList.ToString();


            var signature = $"{modifiers} {returnType} {name}{parameters}".Trim();


            if (includeMethodComments)
            {
                var comment = ExtractLeadingCommentText(method);
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    signature += $" // {CondenseToOneLine(comment!)}";
                }
            }


            return signature;
        }

        /// <summary>
        /// Builds a constructor signature line including modifiers and parameters.
        /// </summary>
        private static string BuildConstructorSignature(ConstructorDeclarationSyntax ctor, bool includeMethodComments)
        {
            var modifiers = string.Join(" ", ctor.Modifiers.Select(m => m.Text));
            var name = ctor.Identifier.Text;
            var parameters = ctor.ParameterList.ToString();


            var signature = $"{modifiers} {name}{parameters}".Trim();


            if (includeMethodComments)
            {
                var comment = ExtractLeadingCommentText(ctor);
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    signature += $" // {CondenseToOneLine(comment!)}";
                }
            }


            return signature;
        }

        /// <summary>
        /// Condenses XML documentation to a single readable line by removing tags and normalizing whitespace.
        /// </summary>
        private static string CondenseXmlDocToSingleLine(string xml)
        {
            // This is a lightweight approach that strips angle-bracket tags and collapses whitespace.
            var withoutTags = System.Text.RegularExpressions.Regex.Replace(xml, "<.*?>", " ");
            return CondenseToOneLine(withoutTags);
        }


        /// <summary>
        /// Utility to collapse whitespace and newlines into a single space line.
        /// </summary>
        private static string CondenseToOneLine(string text)
        {
            var normalized = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            return normalized.Trim();
        }
    }
}
