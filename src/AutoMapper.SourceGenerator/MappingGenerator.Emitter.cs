using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AutoMapper.SourceGenerator;

internal partial class MappingGenerator
{
    internal class Emitter
    {
        private static readonly string s_generatedCodeAttribute =
            $"global::System.CodeDom.Compiler.GeneratedCodeAttribute(" +
            $"\"{typeof(Emitter).Assembly.GetName().Name}\", " +
            $"\"{typeof(Emitter).Assembly.GetName().Version}\")";

        private static readonly string s_editorBrowsableAttribute =
            "global::System.ComponentModel.EditorBrowsableAttribute(" +
            "global::System.ComponentModel.EditorBrowsableState.Never)";

        // Generate the Emit Dictionary.
        public Dictionary<string, string> Emit(IEnumerable promisedGeneratingTypeAndPropertyInfos)
        {
            var dict = new Dictionary<string, string>();
            var promisedGeneratingTypeInfos = promisedGeneratingTypeAndPropertyInfos.OfType<PromisedGeneratingTypeInfo>().ToList();
            var promisedGeneratingPropertyInfos= promisedGeneratingTypeAndPropertyInfos.OfType<PromisedGeneratingPropertyInfo>().ToList();
            foreach (var promisedGeneratingTypeInfo in promisedGeneratingTypeInfos)
            {
                var generatedTypeInfo = GenerateType(promisedGeneratingTypeInfo);
                var generatedMapperInfo = GenerateMapper(generatedTypeInfo);
                dict.Add($"{generatedTypeInfo.GeneratedTypeName}.g.cs", generatedTypeInfo.SourceText);
                dict.Add($"{generatedMapperInfo.ParentGeneratingTypeInfo.GeneratedTypeName}.Mapper.g.cs", generatedMapperInfo.SourceText);
            }
            foreach (var group in promisedGeneratingTypeInfos.GroupBy(_ => _.SourceTypeSymbol, SymbolEqualityComparer.Default))
            {
                var typeSymbol = group.Key as INamedTypeSymbol;
                var generatedExtensionInfo = GenerateExtensions(typeSymbol!, group.Select(_ => _.GeneratedTypeName).ToList());
                dict.Add($"{generatedExtensionInfo.ThisTypeSymbol.Name}.Extensions.g.cs", generatedExtensionInfo.SourceText);
            }
            return dict;
        }

        private PromisedGeneratingTypeInfo GenerateType(PromisedGeneratingTypeInfo promisedGeneratingTypeInfo)
        {
            var sw = new StringWriter();
            var writer = new IndentedTextWriter(sw);
            string namespaceName = promisedGeneratingTypeInfo.SourceTypeSymbol.ContainingNamespace.ToDisplayString();

            #region HEADER
            writer.WriteLine("using System;");
            writer.WriteLine($"namespace {namespaceName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"public partial class {promisedGeneratingTypeInfo.GeneratedTypeName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            #endregion
            foreach (var property in promisedGeneratingTypeInfo.GeneratedProperties)
            {
                // Pre process the attribute on the property
                var propertyType = string.Empty;
                foreach (var attributeData in property.PropertySymbol.GetAttributes())
                {
                    // If ignore, pass
                    if (attributeData.AttributeClass!.ToDisplayString() == AdaptIgnoreAttribute)
                    {
                        goto propEnd;
                    }

                    // Get type from attribute
                    if (attributeData.AttributeClass!.ToDisplayString() == PropertyTypeAttribute)
                    {
                        propertyType = attributeData.NamedArguments.FirstOrDefault(kvp => kvp.Key == "Type").Value.ToString();
                    }
                }
                var propertySymbol = property.PropertySymbol;
                var propertySymbolName = propertySymbol.Name;
                var propertySymbolType = string.IsNullOrEmpty(propertyType) ? propertySymbol.Type.ToString() : propertyType;
                writer.WriteLine($"public {propertySymbolType} {propertySymbolName} {{ get; set; }}");

            propEnd:
                writer.WriteLine();
            }
            writer.WriteLine($"public {promisedGeneratingTypeInfo.SourceTypeSymbol} MapTo{promisedGeneratingTypeInfo.SourceTypeSymbol.Name}() => {promisedGeneratingTypeInfo.GeneratedTypeName}.Mapper.Map{promisedGeneratingTypeInfo.GeneratedTypeName}To{promisedGeneratingTypeInfo.SourceTypeSymbol.Name}(this);");
            writer.WriteLine();
            writer.Indent -= 1;
            writer.WriteLine("}");
            writer.Indent -= 1;
            writer.WriteLine("}");
            promisedGeneratingTypeInfo.SourceText = sw.ToString();
            return promisedGeneratingTypeInfo;

        }

        // Generate Mapper
        private GeneratedMapperInfo GenerateMapper(PromisedGeneratingTypeInfo promisedGeneratingTypeInfo)
        {
            var sw = new StringWriter();
            var writer = new IndentedTextWriter(sw);
            string namespaceName = promisedGeneratingTypeInfo.SourceTypeSymbol.ContainingNamespace.ToDisplayString();
            string forwardMethodName =
                $"Map{promisedGeneratingTypeInfo.SourceTypeSymbol.Name}To{promisedGeneratingTypeInfo.GeneratedTypeName}";
            string backwardMethodName =
                $"Map{promisedGeneratingTypeInfo.GeneratedTypeName}To{promisedGeneratingTypeInfo.SourceTypeSymbol.Name}";

            writer.WriteLine("using System;");
            writer.WriteLine($"namespace {namespaceName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            //The start of the type
            writer.WriteLine($"public partial class {promisedGeneratingTypeInfo.GeneratedTypeName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine("public class Mapper");
            writer.WriteLine("{");
            writer.Indent += 1;

            writer.WriteLine(
                "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine(
                $"public static {promisedGeneratingTypeInfo.GeneratedTypeName} {forwardMethodName}({promisedGeneratingTypeInfo.SourceTypeSymbol.ToDisplayString()} obj)");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"var target = new {promisedGeneratingTypeInfo.GeneratedTypeName}();");
            foreach (var property in promisedGeneratingTypeInfo.GeneratedProperties)
            {
                var propertySymbol = property.PropertySymbol;
                writer.WriteLine($"target.{propertySymbol.Name} = obj.{propertySymbol.Name};");
            }
            writer.WriteLine("return target;");
            writer.Indent -= 1;
            writer.WriteLine("}");

            writer.WriteLine();

            writer.WriteLine(
                "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine(
                $"public static {promisedGeneratingTypeInfo.SourceTypeSymbol.ToDisplayString()} {backwardMethodName}({promisedGeneratingTypeInfo.GeneratedTypeName} obj)");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"var target = new {promisedGeneratingTypeInfo.SourceTypeSymbol.ToDisplayString()}();");
            foreach (var property in promisedGeneratingTypeInfo.GeneratedProperties)
            {
                var propertySymbol = property.PropertySymbol;
                writer.WriteLine($"target.{propertySymbol.Name} = obj.{propertySymbol.Name};");
            }
            writer.WriteLine("return target;");
            writer.Indent -= 1;
            writer.WriteLine("}");

            // The end of the type
            writer.Indent -= 1;
            writer.WriteLine("}");
            writer.Indent -= 1;
            writer.WriteLine("}");
            writer.Indent -= 1;
            writer.WriteLine("}");

            return new GeneratedMapperInfo()
            {
                ParentGeneratingTypeInfo = promisedGeneratingTypeInfo,
                SourceText = sw.ToString(),
                ForwardMappingMethodName = forwardMethodName,
                BackwardMappingMethodName = backwardMethodName
            };
        }

        private GeneratedExtensionInfo GenerateExtensions(INamedTypeSymbol thisType, IEnumerable<string> targetTypes)
        {
            var sw = new StringWriter();
            var writer = new IndentedTextWriter(sw);
            var className = $"{thisType.Name}Extensions";
            string namespaceName = thisType.ContainingNamespace.ToDisplayString();
            writer.WriteLine("using System;");
            writer.WriteLine($"namespace {namespaceName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"public static class {className}");
            writer.WriteLine("{");
            writer.Indent += 1;
            foreach (var targetType in targetTypes)
            {
                writer.WriteLine($"public static {targetType} MapTo{targetType}(this {thisType} obj) => {targetType}.Mapper.Map{thisType.Name}To{targetType}(obj);");
                writer.WriteLine();
            }
            writer.Indent -= 1;
            writer.WriteLine("}");
            writer.Indent -= 1;
            writer.WriteLine("}");
            return new GeneratedExtensionInfo()
            {
                SourceText = sw.ToString(),
                ThisTypeSymbol = thisType
            };
        }
    }
}
