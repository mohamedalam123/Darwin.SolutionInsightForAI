using Darwin.SolutionInsightForAI.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Text.RegularExpressions;

namespace Darwin.SolutionInsightForAI.Parsing;

/// <summary>
/// Parses C# source files via Roslyn and extracts classes/records with their leading comments
/// and member (method/ctor) signatures. Produces single-line signatures and single-line cleaned comments.
/// </summary>
public sealed class CSharpCodeParser
{
    /// <summary>
    /// Parse a C# file and return extracted classes (with methods and optional comments).
    /// </summary>
    public IReadOnlyList<ClassInfo> ExtractClasses(
        string sourceCode,
        bool includeClassComments,
        bool includeMethodComments)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetCompilationUnitRoot();

        var results = new List<ClassInfo>();

        foreach (var node in root.DescendantNodes())
        {
            if (node is ClassDeclarationSyntax classDecl)
            {
                var info = BuildClassInfo(classDecl, includeClassComments, includeMethodComments);
                results.Add(info);
            }
            else if (node is RecordDeclarationSyntax recordDecl)
            {
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
        var className = classDecl.Identifier.Text;

        string? classComment = null;
        if (includeClassComments)
            classComment = ExtractLeadingCommentText(classDecl);

        var classSig = BuildTypeSignature(classDecl);

        var cls = new ClassInfo { Name = className, Comment = classComment, Signature = classSig };

        foreach (var member in classDecl.Members)
        {
            switch (member)
            {
                case MethodDeclarationSyntax method:
                    cls.Methods.Add(new MethodInfo
                    {
                        SignatureLine = BuildMethodSignature(method),
                        Comment = includeMethodComments ? ExtractLeadingCommentText(method) : null
                    });
                    break;

                case ConstructorDeclarationSyntax ctor:
                    cls.Methods.Add(new MethodInfo
                    {
                        SignatureLine = BuildConstructorSignature(ctor),
                        Comment = includeMethodComments ? ExtractLeadingCommentText(ctor) : null
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
            classComment = ExtractLeadingCommentText(recordDecl);

        var classSig = BuildTypeSignature(recordDecl);

        var cls = new ClassInfo { Name = name, Comment = classComment, Signature = classSig };

        foreach (var member in recordDecl.Members)
        {
            switch (member)
            {
                case MethodDeclarationSyntax method:
                    cls.Methods.Add(new MethodInfo
                    {
                        SignatureLine = BuildMethodSignature(method),
                        Comment = includeMethodComments ? ExtractLeadingCommentText(method) : null
                    });
                    break;

                case ConstructorDeclarationSyntax ctor:
                    cls.Methods.Add(new MethodInfo
                    {
                        SignatureLine = BuildConstructorSignature(ctor),
                        Comment = includeMethodComments ? ExtractLeadingCommentText(ctor) : null
                    });
                    break;
            }
        }

        return cls;
    }

    /// <summary>
    /// Builds a readable single-line signature for class/record/struct/interface.
    /// </summary>
    private static string BuildTypeSignature(BaseTypeDeclarationSyntax node)
    {
        var modifiers = string.Join(" ", node.Modifiers.Select(m => m.Text)).Trim();
        var kind = node.Kind() switch
        {
            SyntaxKind.ClassDeclaration => "class",
            SyntaxKind.RecordDeclaration => "record",
            SyntaxKind.StructDeclaration => "struct",
            SyntaxKind.InterfaceDeclaration => "interface",
            _ => "type"
        };

        var id = (node as TypeDeclarationSyntax)?.Identifier.Text
                 ?? (node as RecordDeclarationSyntax)?.Identifier.Text
                 ?? "";

        var tparams = (node as TypeDeclarationSyntax)?.TypeParameterList?.ToString()
                      ?? (node as RecordDeclarationSyntax)?.TypeParameterList?.ToString()
                      ?? "";

        var sig = $"{(string.IsNullOrEmpty(modifiers) ? "" : modifiers + " ")}{kind} {id}{tparams}";
        return CondenseToOneLine(sig);
    }

    /// <summary>
    /// Builds a method signature (modifiers + return type + name + generic args + params + constraints),
    /// single-lined (no '{').
    /// </summary>
    private static string BuildMethodSignature(MethodDeclarationSyntax method)
    {
        var mods = string.Join(" ", method.Modifiers.Select(m => m.Text)).Trim();
        var ret = method.ReturnType.ToString();
        var name = method.Identifier.Text;
        var tps = method.TypeParameterList?.ToString() ?? "";
        var pars = method.ParameterList.ToString();
        var cons = string.Concat(method.ConstraintClauses.Select(c => " " + c.ToFullString().Trim()));

        var raw = $"{(string.IsNullOrEmpty(mods) ? "" : mods + " ")}{ret} {name}{tps}{pars}{cons}";
        return CondenseToOneLine(raw);
    }

    /// <summary>
    /// Builds a constructor signature (modifiers + name + params), single-lined (no '{').
    /// </summary>
    private static string BuildConstructorSignature(ConstructorDeclarationSyntax ctor)
    {
        var mods = string.Join(" ", ctor.Modifiers.Select(m => m.Text)).Trim();
        var name = ctor.Identifier.Text;
        var pars = ctor.ParameterList.ToString();

        var raw = $"{(string.IsNullOrEmpty(mods) ? "" : mods + " ")}{name}{pars}";
        return CondenseToOneLine(raw);
    }

    /// <summary>
    /// Extracts a readable one-line leading comment from trivia:
    /// - XML doc (/// or /** */) -> tags removed, condensed
    /// - Single-line '//' -> markers removed, condensed
    /// - Multi-line '/* ... */' -> markers removed, condensed
    /// Returns null if no meaningful content is found.
    /// </summary>
    private static string? ExtractLeadingCommentText(SyntaxNode node)
    {
        var sb = new StringBuilder();

        void CollectFromTriviaList(SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                {
                    // Strip leading '//' and trim.
                    var text = trivia.ToFullString();
                    if (text.StartsWith("//")) text = text[2..];
                    text = text.Trim();
                    if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
                }
                else if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    // Remove /* */ and condense.
                    var text = trivia.ToFullString()
                        .Replace("/*", string.Empty)
                        .Replace("*/", string.Empty);
                    text = CondenseToOneLine(text);
                    if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
                }
                else if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                         trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    // XML doc: remove '///' occurrences and XML tags, then condense.
                    var xml = trivia.ToFullString().Replace("///", string.Empty);
                    var condensed = CondenseXmlDocToSingleLine(xml);
                    if (!string.IsNullOrWhiteSpace(condensed)) sb.AppendLine(condensed);
                }
            }
        }

        // 1) Comments directly attached to the node (typical case).
        CollectFromTriviaList(node.GetLeadingTrivia());

        // 2) If the declaration has attributes, sometimes comments attach to the attribute list's leading trivia.
        foreach (var attrList in node.ChildNodes().OfType<AttributeListSyntax>())
            CollectFromTriviaList(attrList.GetLeadingTrivia());

        var result = CondenseToOneLine(sb.ToString());
        return string.IsNullOrWhiteSpace(result) ? (string?)null : result;
    }

    /// <summary>
    /// Removes XML tags and collapses whitespace to a single line.
    /// </summary>
    private static string CondenseXmlDocToSingleLine(string xml)
    {
        var noTags = Regex.Replace(xml, "<.*?>", " ");
        return CondenseToOneLine(noTags);
    }

    /// <summary>
    /// Collapses all whitespace (including CR/LF and tabs) into a single space, trims ends.
    /// </summary>
    private static string CondenseToOneLine(string text)
    {
        var normalized = Regex.Replace(text, @"\s+", " ");
        return normalized.Trim();
    }
}
