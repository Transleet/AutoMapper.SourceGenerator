using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapper.SourceGenerator
{
    internal class PromisedGeneratingPropertyInfo
    {
        public INamedTypeSymbol PropertyParentTypeSymbol { get; set; }
        public TypeDeclarationSyntax PropertyParentTypeDeclarationSyntax { get; set; }
        public PropertyDeclarationSyntax PropertyDeclarationSyntax { get; set; }
        
        public IPropertySymbol PropertySymbol { get; set; }
        public INamedTypeSymbol PropertyTypeSymbol { get; set; }
    }
}
