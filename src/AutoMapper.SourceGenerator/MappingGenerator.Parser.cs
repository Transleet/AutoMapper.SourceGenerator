using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapper.SourceGenerator;

internal partial class MappingGenerator
{
    private const string AdaptFromAttribute = "Mapster.SourceGenerator.AdaptFromAttribute";
    private const string AdaptToAttribute = "Mapster.SourceGenerator.AdaptToAttribute";
    private const string AdaptTwoWaysAttribute = "Mapster.SourceGenerator.AdaptTwoWaysAttribute";
    private const string AdaptIgnoreAttribute = "Mapster.SourceGenerator.AdaptIgnoreAttribute";
    private const string PropertyTypeAttribute = "Mapster.SourceGenerator.PropertyTypeAttribute";

    public static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        var attributeSyntax = node as AttributeSyntax;
        return attributeSyntax?.ArgumentList != null && attributeSyntax.ArgumentList.Arguments.Count > 0;
    }

    public static object? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var attributeSyntax = context.Node as AttributeSyntax;
        if (attributeSyntax!.Parent is AttributeListSyntax attributeListSyntax)
        {
            if (attributeListSyntax.Parent is TypeDeclarationSyntax typeDeclarationSyntax)
            {
                var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) as INamedTypeSymbol;
                var attributeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type as INamedTypeSymbol;
                var attributeText = attributeSyntax.ArgumentList!.Arguments.FirstOrDefault()?.Expression.ToString()
                    .Trim('"');
                if (attributeText is not null)
                {
                    switch (attributeSymbol!.ToDisplayString())
                    {
                        case AdaptFromAttribute:
                        case AdaptToAttribute:
                        case AdaptTwoWaysAttribute:
                            break;
                        default:
                            return null;
                    }
                    var generatedTypeName = attributeText.Replace("[name]", typeSymbol!.Name);
                    return new PromisedGeneratingTypeInfo
                    {
                        AttributeSyntax = attributeSyntax,
                        GeneratedTypeName = generatedTypeName,
                        SourceTypeDeclarationSyntax = typeDeclarationSyntax,
                        SourceTypeSymbol = typeSymbol,
                        GeneratedModelPattern = attributeText
                    };
                }
            }
            
            if (attributeListSyntax.Parent is PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                if (propertyDeclarationSyntax.Parent is TypeDeclarationSyntax propertyParentTypeDeclarationSyntax)
                {
                    var propertyParentTypeSymbol = context.SemanticModel.GetDeclaredSymbol(propertyParentTypeDeclarationSyntax) as INamedTypeSymbol;
                    var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax) as IPropertySymbol;
                    var attributeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type as INamedTypeSymbol;
                    switch (attributeSymbol.ToDisplayString())
                    {
                        case AdaptIgnoreAttribute:
                        case PropertyTypeAttribute:
                            break;
                        default:
                            return null;
                    }
                    return new PromisedGeneratingPropertyInfo()
                    {
                        PropertyParentTypeDeclarationSyntax = propertyParentTypeDeclarationSyntax,
                        PropertyParentTypeSymbol = propertyParentTypeSymbol,
                        PropertySymbol = propertySymbol,
                        PropertyDeclarationSyntax = propertyDeclarationSyntax
                    };
                }
            }
        }

        return null;
    }
}