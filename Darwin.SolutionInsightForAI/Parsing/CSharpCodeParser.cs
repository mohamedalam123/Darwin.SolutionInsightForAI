using Darwin.SolutionInsightForAI.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Text.RegularExpressions;

namespace Darwin.SolutionInsightForAI.Parsing;

/// <summary>
/// Parses C# source files via Roslyn and extracts types (class/record/interface/struct)
/// with their cleaned leading comments and member (method/ctor) single-line signatures.
/// </summary>
public sealed class CSharpCodeParser
{
    /// <summary>
    /// Parse a C# file and return extracted types (with methods and optional comments).
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
                var info = BuildTypeInfo(classDecl, "Class", includeClassComments, includeMethodComments);
                results.Add(info);
            }
            else if (node is RecordDeclarationSyntax recordDecl)
            {
                var info = BuildTypeInfo(recordDecl, "Record", includeClassComments, includeMethodComments);
                results.Add(info);
            }
            else if (node is InterfaceDeclarationSyntax interfaceDecl)
            {
                var info = BuildTypeInfo(interfaceDecl, "Interface", includeClassComments, includeMethodComments);
                results.Add(info);
            }
            else if (node is StructDeclarationSyntax structDecl)
            {
                var info = BuildTypeInfo(structDecl, "Struct", includeClassComments, includeMethodComments);
                results.Add(info);
            }
        }

        return results;
    }

    /// <summary>
    /// Builds a ClassInfo for any class-like declaration (class/record/interface/struct).
    /// </summary>
    private static ClassInfo BuildTypeInfo(
        BaseTypeDeclarationSyntax typeDecl,
        string kind,
        bool includeTypeComments,
        bool includeMemberComments)
    {
        // Common name extraction for all type declarations (class/record/interface/struct)
        var name = (typeDecl as TypeDeclarationSyntax)?.Identifier.Text
                   ?? (typeDecl as RecordDeclarationSyntax)?.Identifier.Text
                   ?? string.Empty;

        string? typeComment = null;
        if (includeTypeComments)
            typeComment = ExtractLeadingCommentText(typeDecl);

        var typeSig = BuildTypeSignature(typeDecl);

        var info = new ClassInfo
        {
            Name = name,
            Kind = kind,
            Comment = typeComment,
            Signature = typeSig
        };

        // IMPORTANT: BaseTypeDeclarationSyntax does not have 'Members'.
        // Only TypeDeclarationSyntax (class/interface/struct/record) exposes Members.
        if (typeDecl is TypeDeclarationSyntax tds)
        {
            foreach (var member in tds.Members)
            {
                switch (member)
                {
                    case MethodDeclarationSyntax method:
                        info.Methods.Add(new MethodInfo
                        {
                            SignatureLine = BuildMethodSignature(method),
                            Comment = includeMemberComments ? ExtractLeadingCommentText(method) : null
                        });
                        break;

                    case ConstructorDeclarationSyntax ctor:
                        info.Methods.Add(new MethodInfo
                        {
                            SignatureLine = BuildConstructorSignature(ctor),
                            Comment = includeMemberComments ? ExtractLeadingCommentText(ctor) : null
                        });
                        break;
                }
            }
        }

        return info;
    }

    /// <summary>
    /// Builds a readable single-line signature for a type (class/record/interface/struct).
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
    /// Builds a method signature (modifiers + return type + name + generics + params + constraints), single-line (no '{').
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
    /// Builds a constructor signature (modifiers + name + params), single-line (no '{').
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
    /// Extracts a cleaned one-line leading comment from trivia:
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
                    var text = trivia.ToFullString();
                    if (text.StartsWith("//")) text = text[2..];
                    text = text.Trim();
                    if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
                }
                else if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    var text = trivia.ToFullString()
                        .Replace("/*", string.Empty)
                        .Replace("*/", string.Empty);
                    text = CondenseToOneLine(text);
                    if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
                }
                else if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                         trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    var xml = trivia.ToFullString().Replace("///", string.Empty);
                    var condensed = CondenseXmlDocToSingleLine(xml);
                    if (!string.IsNullOrWhiteSpace(condensed)) sb.AppendLine(condensed);
                }
            }
        }

        // Comments directly attached to the node
        CollectFromTriviaList(node.GetLeadingTrivia());

        // Comments that may attach to attribute lists above the declaration
        foreach (var attrList in node.ChildNodes().OfType<AttributeListSyntax>())
            CollectFromTriviaList(attrList.GetLeadingTrivia());

        var result = CondenseToOneLine(sb.ToString());
        return string.IsNullOrWhiteSpace(result) ? (string?)null : result;
    }

    private static string CondenseXmlDocToSingleLine(string xml)
    {
        var noTags = Regex.Replace(xml, "<.*?>", " ");
        return CondenseToOneLine(noTags);
    }

    /// <summary>
    /// Collapses all whitespace (including CR/LF and tabs) into a single space, then trims.
    /// </summary>
    private static string CondenseToOneLine(string text)
    {
        var normalized = Regex.Replace(text, @"\s+", " ");
        return normalized.Trim();
    }
}
