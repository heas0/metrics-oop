using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MetricsOOP.Models;
using MethodInfo = MetricsOOP.Models.MethodInfo;
using PropertyInfo = MetricsOOP.Models.PropertyInfo;

namespace MetricsOOP.Parsers;

/// <summary>
/// Main code analyzer that parses C# source files using Roslyn
/// </summary>
public class CodeAnalyzer
{
    private readonly List<ClassInfo> _classes = new();
    private readonly Dictionary<string, ClassInfo> _classMap = new();
    private readonly Dictionary<string, SyntaxTree> _syntaxTrees = new();
    private CSharpCompilation? _compilation;
    private static readonly HashSet<string> PrimitiveTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "void", "int", "string", "bool", "double", "float", "decimal", "long", "short", "byte",
        "char", "object", "var", "dynamic", "uint", "ulong", "ushort", "sbyte", "nint", "nuint"
    };

    /// <summary>
    /// Analyze multiple C# source files
    /// </summary>
    public List<ClassInfo> AnalyzeFiles(IEnumerable<string> filePaths)
    {
        _classes.Clear();
        _classMap.Clear();
        _syntaxTrees.Clear();

        // Parse all files first
        var trees = new List<SyntaxTree>();
        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath))
                continue;

            var code = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(code, path: filePath);
            trees.Add(tree);
            _syntaxTrees[filePath] = tree;
        }

        // Create compilation for semantic analysis
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        _compilation = CSharpCompilation.Create("Analysis")
            .AddReferences(references)
            .AddSyntaxTrees(trees);

        // First pass: collect all classes
        foreach (var tree in trees)
        {
            var filePath = tree.FilePath;
            var root = tree.GetRoot();
            var semanticModel = _compilation.GetSemanticModel(tree);
            
            CollectClasses(root, filePath, semanticModel);
        }

        // Second pass: resolve inheritance and coupling
        ResolveRelationships();

        return _classes;
    }

    /// <summary>
    /// Analyze a single C# source file
    /// </summary>
    public List<ClassInfo> AnalyzeFile(string filePath)
    {
        return AnalyzeFiles(new[] { filePath });
    }

    /// <summary>
    /// Analyze C# code from string
    /// </summary>
    public List<ClassInfo> AnalyzeCode(string code, string fileName = "code.cs")
    {
        _classes.Clear();
        _classMap.Clear();
        _syntaxTrees.Clear();

        var tree = CSharpSyntaxTree.ParseText(code, path: fileName);
        _syntaxTrees[fileName] = tree;

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        _compilation = CSharpCompilation.Create("Analysis")
            .AddReferences(references)
            .AddSyntaxTrees(tree);

        var root = tree.GetRoot();
        var semanticModel = _compilation.GetSemanticModel(tree);
        
        CollectClasses(root, fileName, semanticModel);
        ResolveRelationships();

        return _classes;
    }

    private void CollectClasses(SyntaxNode root, string filePath, SemanticModel semanticModel)
    {
        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        var interfaceDeclarations = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();

        foreach (var classDecl in classDeclarations)
        {
            var classInfo = ExtractClassInfo(classDecl, filePath, semanticModel);
            
            if (_classMap.TryGetValue(classInfo.FullName, out var existingClass))
            {
                MergeClassInfo(existingClass, classInfo);
            }
            else
            {
                _classes.Add(classInfo);
                _classMap[classInfo.FullName] = classInfo;
            }
        }

        foreach (var interfaceDecl in interfaceDeclarations)
        {
            var classInfo = ExtractInterfaceInfo(interfaceDecl, filePath, semanticModel);

            if (_classMap.TryGetValue(classInfo.FullName, out var existingClass))
            {
                MergeClassInfo(existingClass, classInfo);
            }
            else
            {
                _classes.Add(classInfo);
                _classMap[classInfo.FullName] = classInfo;
            }
        }
    }

    private void MergeClassInfo(ClassInfo target, ClassInfo source)
    {
        // Merge lists
        target.Interfaces = target.Interfaces.Union(source.Interfaces).ToList();
        target.Methods.AddRange(source.Methods);
        target.Fields.AddRange(source.Fields);
        target.Properties.AddRange(source.Properties);
        target.CoupledTypes.UnionWith(source.CoupledTypes);

        // Sum metrics and counts
        target.LinesOfCode += source.LinesOfCode;
        target.SourceLinesOfCode += source.SourceLinesOfCode;
        target.CommentLines += source.CommentLines;
        
        target.MethodDeclarationCount += source.MethodDeclarationCount;
        target.ConstructorCount += source.ConstructorCount;
        target.PropertyAccessorCount += source.PropertyAccessorCount;
        target.IndexerAccessorCount += source.IndexerAccessorCount;
        target.EventAccessorCount += source.EventAccessorCount;

        // Resolve base class - take the one that is specified
        if (string.IsNullOrEmpty(target.BaseClassName) && !string.IsNullOrEmpty(source.BaseClassName))
        {
            target.BaseClassName = source.BaseClassName;
        }
        
        // Also ensure specific flags are persistent (e.g. if one part is abstract, the class is abstract)
        target.IsAbstract |= source.IsAbstract;
        target.IsSealed |= source.IsSealed;
        target.IsStatic |= source.IsStatic;
        
        // Use the most restrictive access modifier or just keep what we have? 
        // In C# partial classes must have the same access modifier.
    }

    private ClassInfo ExtractClassInfo(ClassDeclarationSyntax classDecl, string filePath, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetDeclaredSymbol(classDecl);
        var namespaceName = GetNamespace(classDecl);
        var className = classDecl.Identifier.Text;
        var fullName = string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}";

        var lineSpan = classDecl.GetLocation().GetLineSpan();
        var sourceText = classDecl.SyntaxTree.GetText();
        var classText = classDecl.ToFullString();
        var (loc, sloc, comments) = CountLines(classText);

        var classInfo = new ClassInfo
        {
            Name = className,
            FullName = fullName,
            Namespace = namespaceName,
            FilePath = filePath,
            AccessModifier = GetAccessModifier(classDecl.Modifiers),
            IsAbstract = classDecl.Modifiers.Any(SyntaxKind.AbstractKeyword),
            IsSealed = classDecl.Modifiers.Any(SyntaxKind.SealedKeyword),
            IsStatic = classDecl.Modifiers.Any(SyntaxKind.StaticKeyword),
            IsInterface = false,
            StartLine = lineSpan.StartLinePosition.Line + 1,
            EndLine = lineSpan.EndLinePosition.Line + 1,
            LinesOfCode = loc,
            SourceLinesOfCode = sloc,
            CommentLines = comments
        };

        // Extract base class and interfaces
        if (classDecl.BaseList != null)
        {
            foreach (var baseType in classDecl.BaseList.Types)
            {
                var typeInfo = semanticModel.GetTypeInfo(baseType.Type);
                var typeName = baseType.Type.ToString();
                
                if (typeInfo.Type is INamedTypeSymbol namedType)
                {
                    if (namedType.TypeKind == TypeKind.Interface)
                    {
                        classInfo.Interfaces.Add(typeName);
                    }
                    else
                    {
                        classInfo.BaseClassName = typeName;
                    }
                }
                else if (typeName.StartsWith("I") && char.IsUpper(typeName.ElementAtOrDefault(1)))
                {
                    // Heuristic: interfaces typically start with 'I'
                    classInfo.Interfaces.Add(typeName);
                }
                else if (classInfo.BaseClassName == null)
                {
                    classInfo.BaseClassName = typeName;
                }
            }
        }

        // Extract methods
        var methodDeclarations = classDecl.Members.OfType<MethodDeclarationSyntax>().ToList();
        foreach (var methodDecl in methodDeclarations)
        {
            var methodInfo = ExtractMethodInfo(methodDecl, classInfo, semanticModel);
            classInfo.Methods.Add(methodInfo);
        }

        // Extract constructors as methods
        var constructors = classDecl.Members.OfType<ConstructorDeclarationSyntax>().ToList();
        foreach (var ctorDecl in constructors)
        {
            var methodInfo = ExtractConstructorInfo(ctorDecl, classInfo, semanticModel);
            classInfo.Methods.Add(methodInfo);
        }

        // Extract fields
        var fieldDeclarations = classDecl.Members.OfType<FieldDeclarationSyntax>();
        foreach (var fieldDecl in fieldDeclarations)
        {
            var fields = ExtractFieldInfo(fieldDecl);
            classInfo.Fields.AddRange(fields);
        }

        // Extract properties
        var propertyDeclarations = classDecl.Members.OfType<PropertyDeclarationSyntax>().ToList();
        foreach (var propDecl in propertyDeclarations)
        {
            var propInfo = ExtractPropertyInfo(propDecl);
            classInfo.Properties.Add(propInfo);
        }

        // Indexers and events (for NOM/accessor counts)
        var indexerDeclarations = classDecl.Members.OfType<IndexerDeclarationSyntax>().ToList();
        var eventDeclarations = classDecl.Members.OfType<EventDeclarationSyntax>().ToList();
        var eventFieldDeclarations = classDecl.Members.OfType<EventFieldDeclarationSyntax>().ToList();

        // Store method-like counts for NOM
        classInfo.MethodDeclarationCount = methodDeclarations.Count;
        classInfo.ConstructorCount = constructors.Count;
        classInfo.PropertyAccessorCount = CountPropertyAccessors(propertyDeclarations);
        classInfo.IndexerAccessorCount = CountIndexerAccessors(indexerDeclarations);
        classInfo.EventAccessorCount = CountEventAccessors(eventDeclarations, eventFieldDeclarations);

        // Collect coupled types from method bodies and field types
        CollectCoupledTypes(classDecl, classInfo, semanticModel);

        return classInfo;
    }

    private ClassInfo ExtractInterfaceInfo(InterfaceDeclarationSyntax interfaceDecl, string filePath, SemanticModel semanticModel)
    {
        var namespaceName = GetNamespace(interfaceDecl);
        var interfaceName = interfaceDecl.Identifier.Text;
        var fullName = string.IsNullOrEmpty(namespaceName) ? interfaceName : $"{namespaceName}.{interfaceName}";

        var lineSpan = interfaceDecl.GetLocation().GetLineSpan();
        var interfaceText = interfaceDecl.ToFullString();
        var (loc, sloc, comments) = CountLines(interfaceText);

        var classInfo = new ClassInfo
        {
            Name = interfaceName,
            FullName = fullName,
            Namespace = namespaceName,
            FilePath = filePath,
            AccessModifier = GetAccessModifier(interfaceDecl.Modifiers),
            IsInterface = true,
            StartLine = lineSpan.StartLinePosition.Line + 1,
            EndLine = lineSpan.EndLinePosition.Line + 1,
            LinesOfCode = loc,
            SourceLinesOfCode = sloc,
            CommentLines = comments
        };

        // Extract base interfaces
        if (interfaceDecl.BaseList != null)
        {
            foreach (var baseType in interfaceDecl.BaseList.Types)
            {
                classInfo.Interfaces.Add(baseType.Type.ToString());
            }
        }

        // Extract method signatures (interfaces only have declarations)
        var methodDeclarations = interfaceDecl.Members.OfType<MethodDeclarationSyntax>().ToList();
        foreach (var methodDecl in methodDeclarations)
        {
            var methodInfo = new MethodInfo
            {
                Name = methodDecl.Identifier.Text,
                Signature = methodDecl.ToString(),
                ReturnType = methodDecl.ReturnType.ToString(),
                AccessModifier = AccessModifier.Public,
                IsAbstract = true,
                ParameterCount = methodDecl.ParameterList.Parameters.Count
            };
            classInfo.Methods.Add(methodInfo);
        }

        var propertyDeclarations = interfaceDecl.Members.OfType<PropertyDeclarationSyntax>().ToList();
        var indexerDeclarations = interfaceDecl.Members.OfType<IndexerDeclarationSyntax>().ToList();
        var eventDeclarations = interfaceDecl.Members.OfType<EventDeclarationSyntax>().ToList();
        var eventFieldDeclarations = interfaceDecl.Members.OfType<EventFieldDeclarationSyntax>().ToList();

        classInfo.MethodDeclarationCount = methodDeclarations.Count;
        classInfo.ConstructorCount = 0;
        classInfo.PropertyAccessorCount = CountPropertyAccessors(propertyDeclarations);
        classInfo.IndexerAccessorCount = CountIndexerAccessors(indexerDeclarations);
        classInfo.EventAccessorCount = CountEventAccessors(eventDeclarations, eventFieldDeclarations);

        return classInfo;
    }

    private MethodInfo ExtractMethodInfo(MethodDeclarationSyntax methodDecl, ClassInfo classInfo, SemanticModel semanticModel)
    {
        var methodText = methodDecl.ToFullString();
        var (loc, _, _) = CountLines(methodText);

        var methodInfo = new MethodInfo
        {
            Name = methodDecl.Identifier.Text,
            Signature = $"{methodDecl.ReturnType} {methodDecl.Identifier}{methodDecl.ParameterList}",
            ReturnType = methodDecl.ReturnType.ToString(),
            AccessModifier = GetAccessModifier(methodDecl.Modifiers),
            IsStatic = methodDecl.Modifiers.Any(SyntaxKind.StaticKeyword),
            IsVirtual = methodDecl.Modifiers.Any(SyntaxKind.VirtualKeyword),
            IsAbstract = methodDecl.Modifiers.Any(SyntaxKind.AbstractKeyword),
            IsOverride = methodDecl.Modifiers.Any(SyntaxKind.OverrideKeyword),
            ParameterCount = methodDecl.ParameterList.Parameters.Count,
            LinesOfCode = loc
        };

        // Calculate cyclomatic complexity
        methodInfo.CyclomaticComplexity = CalculateCyclomaticComplexity(methodDecl);
        
        // Calculate cognitive complexity (SonarSource algorithm)
        methodInfo.CognitiveComplexity = CalculateCognitiveComplexity(methodDecl, methodInfo.Name);

        // Find accessed fields
        if (methodDecl.Body != null || methodDecl.ExpressionBody != null)
        {
            var bodyNode = (SyntaxNode?)methodDecl.Body ?? methodDecl.ExpressionBody;
            if (bodyNode != null)
            {
                // Find field accesses
                var identifiers = bodyNode.DescendantNodes().OfType<IdentifierNameSyntax>();
                var fieldNames = classInfo.Fields.Select(f => f.Name).ToHashSet();
                
                foreach (var id in identifiers)
                {
                    if (fieldNames.Contains(id.Identifier.Text))
                    {
                        methodInfo.AccessedFields.Add(id.Identifier.Text);
                    }
                }

                // Find method calls within same class
                var invocations = bodyNode.DescendantNodes().OfType<InvocationExpressionSyntax>();
                foreach (var invocation in invocations)
                {
                    switch (invocation.Expression)
                    {
                        case IdentifierNameSyntax methodId:
                            methodInfo.CalledMethods.Add(methodId.Identifier.Text);
                            break;
                        case MemberAccessExpressionSyntax memberAccess:
                            if (memberAccess.Expression is ThisExpressionSyntax || memberAccess.Expression is BaseExpressionSyntax)
                            {
                                methodInfo.CalledMethods.Add(memberAccess.Name.Identifier.Text);
                            }
                            else
                            {
                                methodInfo.ExternalMethodCalls.Add(memberAccess.ToString());
                                var typeName = TryGetTypeName(semanticModel, memberAccess.Expression) ?? memberAccess.Expression.ToString();
                                if (!string.IsNullOrEmpty(typeName))
                                    methodInfo.UsedTypes.Add(typeName);
                            }
                            break;
                        case MemberBindingExpressionSyntax memberBinding when invocation.Parent is ConditionalAccessExpressionSyntax conditionalAccess:
                            methodInfo.ExternalMethodCalls.Add($"{conditionalAccess.Expression}.{memberBinding.Name.Identifier.Text}");
                            var conditionalType = TryGetTypeName(semanticModel, conditionalAccess.Expression) ?? conditionalAccess.Expression.ToString();
                            if (!string.IsNullOrEmpty(conditionalType))
                                methodInfo.UsedTypes.Add(conditionalType);
                            break;
                    }
                }

                // Find object creations (new Type())
                var objectCreations = bodyNode.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
                foreach (var creation in objectCreations)
                {
                    var typeName = TryGetTypeName(semanticModel, creation.Type) ?? creation.Type.ToString();
                    if (!string.IsNullOrEmpty(typeName))
                        methodInfo.UsedTypes.Add(typeName);
                }
            }
        }

        // Calculate Halstead metrics
        methodInfo.Halstead = CalculateHalsteadMetrics(methodDecl);

        return methodInfo;
    }

    private MethodInfo ExtractConstructorInfo(ConstructorDeclarationSyntax ctorDecl, ClassInfo classInfo, SemanticModel semanticModel)
    {
        var methodText = ctorDecl.ToFullString();
        var (loc, _, _) = CountLines(methodText);

        var methodInfo = new MethodInfo
        {
            Name = ctorDecl.Identifier.Text,
            Signature = $"{ctorDecl.Identifier}{ctorDecl.ParameterList}",
            ReturnType = "void",
            AccessModifier = GetAccessModifier(ctorDecl.Modifiers),
            IsStatic = ctorDecl.Modifiers.Any(SyntaxKind.StaticKeyword),
            ParameterCount = ctorDecl.ParameterList.Parameters.Count,
            LinesOfCode = loc,
            CyclomaticComplexity = CalculateCyclomaticComplexity(ctorDecl),
            CognitiveComplexity = CalculateCognitiveComplexity(ctorDecl, ctorDecl.Identifier.Text)
        };

        methodInfo.Halstead = CalculateHalsteadMetrics(ctorDecl);
        return methodInfo;
    }

    private List<FieldInfo> ExtractFieldInfo(FieldDeclarationSyntax fieldDecl)
    {
        var fields = new List<FieldInfo>();
        var accessMod = GetAccessModifier(fieldDecl.Modifiers);
        var isStatic = fieldDecl.Modifiers.Any(SyntaxKind.StaticKeyword);
        var isReadOnly = fieldDecl.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);
        var isConst = fieldDecl.Modifiers.Any(SyntaxKind.ConstKeyword);
        var typeName = fieldDecl.Declaration.Type.ToString();

        foreach (var variable in fieldDecl.Declaration.Variables)
        {
            fields.Add(new FieldInfo
            {
                Name = variable.Identifier.Text,
                TypeName = typeName,
                AccessModifier = accessMod,
                IsStatic = isStatic,
                IsReadOnly = isReadOnly,
                IsConst = isConst
            });
        }

        return fields;
    }

    private PropertyInfo ExtractPropertyInfo(PropertyDeclarationSyntax propDecl)
    {
        return new PropertyInfo
        {
            Name = propDecl.Identifier.Text,
            TypeName = propDecl.Type.ToString(),
            AccessModifier = GetAccessModifier(propDecl.Modifiers),
            HasGetter = propDecl.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? propDecl.ExpressionBody != null,
            HasSetter = propDecl.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false,
            IsStatic = propDecl.Modifiers.Any(SyntaxKind.StaticKeyword),
            IsVirtual = propDecl.Modifiers.Any(SyntaxKind.VirtualKeyword),
            IsOverride = propDecl.Modifiers.Any(SyntaxKind.OverrideKeyword)
        };
    }

    private void CollectCoupledTypes(ClassDeclarationSyntax classDecl, ClassInfo classInfo, SemanticModel semanticModel)
    {
        // Collect types from field declarations
        foreach (var field in classDecl.Members.OfType<FieldDeclarationSyntax>())
        {
            AddCoupledType(classInfo, field.Declaration.Type.ToString());
        }

        // Collect types from method parameters and return types
        foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
        {
            AddCoupledType(classInfo, method.ReturnType.ToString());
            foreach (var param in method.ParameterList.Parameters)
            {
                if (param.Type != null)
                    AddCoupledType(classInfo, param.Type.ToString());
            }
        }

        // Collect types from constructor parameters
        foreach (var ctor in classDecl.Members.OfType<ConstructorDeclarationSyntax>())
        {
            foreach (var param in ctor.ParameterList.Parameters)
            {
                if (param.Type != null)
                    AddCoupledType(classInfo, param.Type.ToString());
            }
        }

        // Collect types from properties, indexers, and events
        foreach (var prop in classDecl.Members.OfType<PropertyDeclarationSyntax>())
        {
            AddCoupledType(classInfo, prop.Type.ToString());
        }

        foreach (var indexer in classDecl.Members.OfType<IndexerDeclarationSyntax>())
        {
            AddCoupledType(classInfo, indexer.Type.ToString());
            foreach (var param in indexer.ParameterList.Parameters)
            {
                if (param.Type != null)
                    AddCoupledType(classInfo, param.Type.ToString());
            }
        }

        foreach (var evt in classDecl.Members.OfType<EventDeclarationSyntax>())
        {
            AddCoupledType(classInfo, evt.Type.ToString());
        }

        foreach (var evt in classDecl.Members.OfType<EventFieldDeclarationSyntax>())
        {
            AddCoupledType(classInfo, evt.Declaration.Type.ToString());
        }

        // Collect types from local variables and object creations in method/constructor bodies
        IEnumerable<SyntaxNode?> bodies = classDecl.Members
            .Where(m => m is MethodDeclarationSyntax || m is ConstructorDeclarationSyntax)
            .Select(m =>
            {
                return m switch
                {
                    MethodDeclarationSyntax md => (SyntaxNode?)md.Body ?? md.ExpressionBody,
                    ConstructorDeclarationSyntax cd => (SyntaxNode?)cd.Body ?? cd.ExpressionBody,
                    _ => null
                };
            });

        foreach (var body in bodies)
        {
            if (body == null)
                continue;

            // Object creations: new Type()
            foreach (var creation in body.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                AddCoupledType(classInfo, creation.Type.ToString());
            }

            // Local variable declarations
            foreach (var localDecl in body.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
            {
                AddCoupledType(classInfo, localDecl.Declaration.Type.ToString());
            }

            // Type casts
            foreach (var cast in body.DescendantNodes().OfType<CastExpressionSyntax>())
            {
                AddCoupledType(classInfo, cast.Type.ToString());
            }
        }
    }

    private void AddCoupledType(ClassInfo classInfo, string typeName)
    {
        typeName = NormalizeTypeName(typeName);
        if (string.IsNullOrWhiteSpace(typeName))
            return;

        // Filter out primitive types and common system types
        if (IsPrimitiveType(typeName) || IsSystemType(typeName))
            return;

        // Handle arrays
        if (typeName.EndsWith("[]"))
        {
            AddCoupledType(classInfo, typeName[..^2]);
            return;
        }

        // Handle generic types
        var genericIndex = typeName.IndexOf('<');
        if (genericIndex >= 0)
        {
            var baseType = typeName.Substring(0, genericIndex);
            if (!IsPrimitiveType(baseType) && !IsSystemType(baseType))
                classInfo.CoupledTypes.Add(baseType);

            var genericArgs = typeName.Substring(genericIndex + 1, typeName.LastIndexOf('>') - genericIndex - 1);
            foreach (var arg in SplitGenericArguments(genericArgs))
            {
                AddCoupledType(classInfo, arg);
            }
            return;
        }

        if (IsPrimitiveType(typeName) || IsSystemType(typeName))
            return;

        classInfo.CoupledTypes.Add(typeName);
    }

    private bool IsSystemType(string typeName)
    {
        var systemTypes = new HashSet<string>
        {
            "List", "Dictionary", "HashSet", "Queue", "Stack", "Array",
            "Task", "Action", "Func", "Predicate", "IEnumerable", "IList",
            "ICollection", "IDictionary", "ISet", "String", "Object",
            "Exception", "EventArgs", "EventHandler", "Nullable"
        };
        if (typeName.StartsWith("System.", StringComparison.Ordinal))
            return true;
        return systemTypes.Contains(typeName);
    }

    private void ResolveRelationships()
    {
        // Build inheritance relationships
        foreach (var classInfo in _classes)
        {
            if (!string.IsNullOrEmpty(classInfo.BaseClassName))
            {
                // Find parent class
                var parentClass = _classes.FirstOrDefault(c => 
                    c.Name == classInfo.BaseClassName || 
                    c.FullName == classInfo.BaseClassName ||
                    c.FullName.EndsWith("." + classInfo.BaseClassName));

                if (parentClass != null)
                {
                    parentClass.DirectChildren.Add(classInfo.Name);
                    
                    // Mark inherited members
                    foreach (var method in parentClass.Methods.Where(m => !m.IsStatic && m.AccessModifier != AccessModifier.Private))
                    {
                        var inheritedMethod = new MethodInfo
                        {
                            Name = method.Name,
                            Signature = method.Signature,
                            ReturnType = method.ReturnType,
                            AccessModifier = method.AccessModifier,
                            IsInherited = true,
                            IsVirtual = method.IsVirtual,
                            ParameterCount = method.ParameterCount
                        };
                        
                        // Don't add if already overridden
                        if (!classInfo.Methods.Any(m => m.Name == inheritedMethod.Name && 
                                                         m.ParameterCount == inheritedMethod.ParameterCount))
                        {
                            classInfo.Methods.Add(inheritedMethod);
                        }
                    }

                    foreach (var field in parentClass.Fields.Where(f => !f.IsStatic && f.AccessModifier != AccessModifier.Private))
                    {
                        if (!classInfo.Fields.Any(f => f.Name == field.Name))
                        {
                            classInfo.Fields.Add(new FieldInfo
                            {
                                Name = field.Name,
                                TypeName = field.TypeName,
                                AccessModifier = field.AccessModifier,
                                IsInherited = true
                            });
                        }
                    }
                }
            }

            // Remove self from coupled types
            classInfo.CoupledTypes.Remove(classInfo.Name);
            classInfo.CoupledTypes.Remove(classInfo.FullName);
        }
    }

    private int CalculateCyclomaticComplexity(SyntaxNode node)
    {
        int complexity = 1; // Base complexity

        // Count decision points
        complexity += node.DescendantNodes().OfType<IfStatementSyntax>().Count();
        complexity += node.DescendantNodes().OfType<ElseClauseSyntax>()
            .Count(e => e.Statement is IfStatementSyntax); // else if
        complexity += node.DescendantNodes().OfType<WhileStatementSyntax>().Count();
        complexity += node.DescendantNodes().OfType<ForStatementSyntax>().Count();
        complexity += node.DescendantNodes().OfType<ForEachStatementSyntax>().Count();
        complexity += node.DescendantNodes().OfType<DoStatementSyntax>().Count();
        complexity += node.DescendantNodes().OfType<CaseSwitchLabelSyntax>().Count();
        complexity += node.DescendantNodes().OfType<CasePatternSwitchLabelSyntax>().Count();
        complexity += node.DescendantNodes().OfType<CatchClauseSyntax>().Count();
        complexity += node.DescendantNodes().OfType<ConditionalExpressionSyntax>().Count(); // ?:
        
        // Count ??, && and || operators
        complexity += node.DescendantNodes().OfType<BinaryExpressionSyntax>()
            .Count(b => b.IsKind(SyntaxKind.LogicalAndExpression) || 
                        b.IsKind(SyntaxKind.LogicalOrExpression) ||
                        b.IsKind(SyntaxKind.CoalesceExpression));

        return complexity;
    }

    /// <summary>
    /// Calculate Cognitive Complexity (SonarSource algorithm)
    /// Unlike Cyclomatic Complexity, this accounts for:
    /// - Nesting depth (nested structures add +nesting penalty)
    /// - Breaks from linear flow (if, loops, catch, switch)
    /// - Boolean operators only counted once per sequence
    /// - Recursion detection
    /// </summary>
    private int CalculateCognitiveComplexity(SyntaxNode node, string? methodName = null)
    {
        int complexity = 0;

        int CountBooleanSequences(ExpressionSyntax? expression)
        {
            if (expression == null)
                return 0;

            var tokens = expression.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.AmpersandAmpersandToken) || t.IsKind(SyntaxKind.BarBarToken))
                .ToList();

            if (tokens.Count == 0)
                return 0;

            int count = 1;
            for (int i = 1; i < tokens.Count; i++)
            {
                if (tokens[i - 1].Kind() != tokens[i].Kind())
                    count++;
            }

            return count;
        }

        void Walk(SyntaxNode current, int nesting)
        {
            foreach (var child in current.ChildNodes())
            {
                switch (child)
                {
                    // Structural increments (add +1 plus nesting penalty)
                    case IfStatementSyntax ifStmt:
                        complexity += 1 + nesting;
                        complexity += CountBooleanSequences(ifStmt.Condition);
                        Walk(ifStmt.Condition, nesting);
                        Walk(ifStmt.Statement, nesting + 1);
                        if (ifStmt.Else != null)
                        {
                            if (ifStmt.Else.Statement is IfStatementSyntax)
                            {
                                // else if doesn't increase nesting, just +1
                                complexity += 1;
                                Walk(ifStmt.Else.Statement, nesting);
                            }
                            else
                            {
                                // Plain else increases complexity
                                complexity += 1;
                                Walk(ifStmt.Else.Statement, nesting + 1);
                            }
                        }
                        break;

                    case ForStatementSyntax forStmt:
                        complexity += 1 + nesting;
                        if (forStmt.Condition != null)
                        {
                            complexity += CountBooleanSequences(forStmt.Condition);
                            Walk(forStmt.Condition, nesting);
                        }
                        Walk(forStmt.Statement, nesting + 1);
                        break;

                    case ForEachStatementSyntax foreachStmt:
                        complexity += 1 + nesting;
                        Walk(foreachStmt.Statement, nesting + 1);
                        break;

                    case WhileStatementSyntax whileStmt:
                        complexity += 1 + nesting;
                        complexity += CountBooleanSequences(whileStmt.Condition);
                        Walk(whileStmt.Condition, nesting);
                        Walk(whileStmt.Statement, nesting + 1);
                        break;

                    case DoStatementSyntax doStmt:
                        complexity += 1 + nesting;
                        Walk(doStmt.Statement, nesting + 1);
                        complexity += CountBooleanSequences(doStmt.Condition);
                        Walk(doStmt.Condition, nesting);
                        break;

                    case SwitchStatementSyntax switchStmt:
                        complexity += 1 + nesting;
                        complexity += CountBooleanSequences(switchStmt.Expression);
                        Walk(switchStmt.Expression, nesting);
                        foreach (var section in switchStmt.Sections)
                        {
                            Walk(section, nesting + 1);
                        }
                        break;

                    case SwitchExpressionSyntax:
                        complexity += 1 + nesting;
                        break;

                    case CatchClauseSyntax catchClause:
                        complexity += 1 + nesting;
                        if (catchClause.Filter != null)
                        {
                            complexity += CountBooleanSequences(catchClause.Filter.FilterExpression);
                            Walk(catchClause.Filter, nesting);
                        }
                        Walk(catchClause.Block, nesting + 1);
                        break;

                    case ConditionalExpressionSyntax ternary:
                        complexity += 1 + nesting;
                        complexity += CountBooleanSequences(ternary.Condition);
                        Walk(ternary.Condition, nesting);
                        Walk(ternary.WhenTrue, nesting + 1);
                        Walk(ternary.WhenFalse, nesting + 1);
                        break;

                    // Goto, break label, continue label (C# uses goto for labels)
                    case GotoStatementSyntax:
                        complexity += 1;
                        break;

                    // Recursion detection
                    case InvocationExpressionSyntax invocation when methodName != null:
                        if (invocation.Expression is IdentifierNameSyntax id &&
                            id.Identifier.Text == methodName)
                        {
                            complexity += 1; // Recursive call
                        }
                        else
                        {
                            Walk(invocation, nesting);
                        }
                        break;

                    // Lambda expressions increase nesting
                    case LambdaExpressionSyntax lambda:
                        Walk(lambda.Body, nesting + 1);
                        break;

                    default:
                        Walk(child, nesting);
                        break;
                }
            }
        }

        Walk(node, 0);
        return complexity;
    }

    private HalsteadMetrics CalculateHalsteadMetrics(SyntaxNode node)
    {
        var operators = new Dictionary<string, int>();
        var operands = new Dictionary<string, int>();

        // Operators
        var operatorKinds = new[] 
        {
            SyntaxKind.PlusToken, SyntaxKind.MinusToken, SyntaxKind.AsteriskToken,
            SyntaxKind.SlashToken, SyntaxKind.PercentToken, SyntaxKind.EqualsToken,
            SyntaxKind.PlusEqualsToken, SyntaxKind.MinusEqualsToken,
            SyntaxKind.AsteriskEqualsToken, SyntaxKind.SlashEqualsToken,
            SyntaxKind.EqualsEqualsToken, SyntaxKind.ExclamationEqualsToken,
            SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken,
            SyntaxKind.LessThanEqualsToken, SyntaxKind.GreaterThanEqualsToken,
            SyntaxKind.AmpersandAmpersandToken, SyntaxKind.BarBarToken,
            SyntaxKind.ExclamationToken, SyntaxKind.PlusPlusToken, SyntaxKind.MinusMinusToken,
            SyntaxKind.AmpersandToken, SyntaxKind.BarToken, SyntaxKind.CaretToken,
            SyntaxKind.TildeToken, SyntaxKind.QuestionToken, SyntaxKind.ColonToken,
            SyntaxKind.DotToken, SyntaxKind.OpenParenToken, SyntaxKind.OpenBracketToken,
            SyntaxKind.NewKeyword, SyntaxKind.ReturnKeyword, SyntaxKind.IfKeyword,
            SyntaxKind.ElseKeyword, SyntaxKind.WhileKeyword, SyntaxKind.ForKeyword,
            SyntaxKind.ForEachKeyword, SyntaxKind.SwitchKeyword, SyntaxKind.CaseKeyword,
            SyntaxKind.BreakKeyword, SyntaxKind.ContinueKeyword, SyntaxKind.ThrowKeyword,
            SyntaxKind.TryKeyword, SyntaxKind.CatchKeyword, SyntaxKind.FinallyKeyword
        };

        foreach (var token in node.DescendantTokens())
        {
            if (operatorKinds.Contains(token.Kind()))
            {
                var text = token.Text;
                operators[text] = operators.GetValueOrDefault(text, 0) + 1;
            }
        }

        // Operands: identifiers, literals
        foreach (var identifier in node.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var name = identifier.Identifier.Text;
            operands[name] = operands.GetValueOrDefault(name, 0) + 1;
        }

        foreach (var literal in node.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            var text = literal.Token.Text;
            operands[text] = operands.GetValueOrDefault(text, 0) + 1;
        }

        return new HalsteadMetrics
        {
            DistinctOperators = operators.Count,
            DistinctOperands = operands.Count,
            TotalOperators = operators.Values.Sum(),
            TotalOperands = operands.Values.Sum()
        };
    }

    private static string? TryGetTypeName(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        var typeSymbol = semanticModel.GetTypeInfo(expression).Type;
        return FormatTypeName(typeSymbol);
    }

    private static string? TryGetTypeName(SemanticModel semanticModel, TypeSyntax typeSyntax)
    {
        var typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;
        return FormatTypeName(typeSymbol);
    }

    private static string? FormatTypeName(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return null;

        var name = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (name.StartsWith("global::", StringComparison.Ordinal))
            name = name["global::".Length..];

        return name;
    }

    private static string NormalizeTypeName(string typeName)
    {
        var normalized = typeName.Trim();
        if (normalized.StartsWith("global::", StringComparison.Ordinal))
            normalized = normalized["global::".Length..];

        if (normalized.EndsWith("?"))
            normalized = normalized[..^1];

        return normalized;
    }

    private static bool IsPrimitiveType(string typeName)
    {
        return PrimitiveTypes.Contains(typeName);
    }

    private static IEnumerable<string> SplitGenericArguments(string arguments)
    {
        var result = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < arguments.Length; i++)
        {
            var ch = arguments[i];
            if (ch == '<')
                depth++;
            else if (ch == '>')
                depth--;
            else if (ch == ',' && depth == 0)
            {
                result.Add(arguments.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }

        if (start < arguments.Length)
            result.Add(arguments.Substring(start).Trim());

        return result;
    }

    private static int CountPropertyAccessors(IEnumerable<PropertyDeclarationSyntax> properties)
    {
        int count = 0;
        foreach (var prop in properties)
        {
            if (prop.ExpressionBody != null)
                count++;

            if (prop.AccessorList != null)
            {
                count += prop.AccessorList.Accessors.Count(a =>
                    a.IsKind(SyntaxKind.GetAccessorDeclaration) ||
                    a.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                    a.IsKind(SyntaxKind.InitAccessorDeclaration));
            }
        }
        return count;
    }

    private static int CountIndexerAccessors(IEnumerable<IndexerDeclarationSyntax> indexers)
    {
        int count = 0;
        foreach (var indexer in indexers)
        {
            if (indexer.ExpressionBody != null)
                count++;

            if (indexer.AccessorList != null)
            {
                count += indexer.AccessorList.Accessors.Count(a =>
                    a.IsKind(SyntaxKind.GetAccessorDeclaration) ||
                    a.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                    a.IsKind(SyntaxKind.InitAccessorDeclaration));
            }
        }
        return count;
    }

    private static int CountEventAccessors(IEnumerable<EventDeclarationSyntax> events, IEnumerable<EventFieldDeclarationSyntax> eventFields)
    {
        int count = 0;
        foreach (var evt in events)
        {
            if (evt.AccessorList != null)
            {
                count += evt.AccessorList.Accessors.Count(a =>
                    a.IsKind(SyntaxKind.AddAccessorDeclaration) ||
                    a.IsKind(SyntaxKind.RemoveAccessorDeclaration));
            }
        }

        foreach (var evt in eventFields)
        {
            count += evt.Declaration.Variables.Count * 2; // implicit add/remove
        }

        return count;
    }

    private (int loc, int sloc, int comments) CountLines(string code)
    {
        var lines = code.Split('\n');
        int loc = lines.Length;
        int sloc = 0;
        int comments = 0;
        bool inBlockComment = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (inBlockComment)
            {
                comments++;
                if (trimmed.Contains("*/"))
                    inBlockComment = false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.StartsWith("//"))
            {
                comments++;
                continue;
            }

            if (trimmed.StartsWith("/*"))
            {
                comments++;
                inBlockComment = !trimmed.Contains("*/");
                continue;
            }

            // Line with code (may also contain comments)
            sloc++;
            if (trimmed.Contains("//") || trimmed.Contains("/*"))
                comments++;
        }

        return (loc, sloc, comments);
    }

    private string GetNamespace(SyntaxNode node)
    {
        var namespaceDecl = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        if (namespaceDecl != null)
            return namespaceDecl.Name.ToString();

        var fileScopedNamespace = node.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScopedNamespace != null)
            return fileScopedNamespace.Name.ToString();

        // Try root level
        var root = node.SyntaxTree.GetRoot();
        var ns = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (ns != null)
            return ns.Name.ToString();

        var blockNs = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        if (blockNs != null && blockNs.DescendantNodes().Contains(node))
            return blockNs.Name.ToString();

        return string.Empty;
    }

    private AccessModifier GetAccessModifier(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(SyntaxKind.PublicKeyword))
            return AccessModifier.Public;
        if (modifiers.Any(SyntaxKind.PrivateKeyword) && modifiers.Any(SyntaxKind.ProtectedKeyword))
            return AccessModifier.PrivateProtected;
        if (modifiers.Any(SyntaxKind.ProtectedKeyword) && modifiers.Any(SyntaxKind.InternalKeyword))
            return AccessModifier.ProtectedInternal;
        if (modifiers.Any(SyntaxKind.ProtectedKeyword))
            return AccessModifier.Protected;
        if (modifiers.Any(SyntaxKind.InternalKeyword))
            return AccessModifier.Internal;
        if (modifiers.Any(SyntaxKind.PrivateKeyword))
            return AccessModifier.Private;
        
        return AccessModifier.Private; // Default for class members
    }
}
