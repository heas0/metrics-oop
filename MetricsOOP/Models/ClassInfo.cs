namespace MetricsOOP.Models;

/// <summary>
/// Represents information about a class
/// </summary>
public class ClassInfo
{
    /// <summary>
    /// Class name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Full qualified name including namespace
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Namespace
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
    
    /// <summary>
    /// Source file path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Base class name (null if none or Object)
    /// </summary>
    public string? BaseClassName { get; set; }
    
    /// <summary>
    /// Implemented interface names
    /// </summary>
    public List<string> Interfaces { get; set; } = new();
    
    /// <summary>
    /// Access modifier
    /// </summary>
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Internal;
    
    /// <summary>
    /// Whether the class is abstract
    /// </summary>
    public bool IsAbstract { get; set; }
    
    /// <summary>
    /// Whether the class is sealed
    /// </summary>
    public bool IsSealed { get; set; }
    
    /// <summary>
    /// Whether the class is static
    /// </summary>
    public bool IsStatic { get; set; }
    
    /// <summary>
    /// Whether this is an interface (not a class)
    /// </summary>
    public bool IsInterface { get; set; }
    
    /// <summary>
    /// Methods defined in this class
    /// </summary>
    public List<MethodInfo> Methods { get; set; } = new();
    
    /// <summary>
    /// Fields/attributes defined in this class
    /// </summary>
    public List<FieldInfo> Fields { get; set; } = new();
    
    /// <summary>
    /// Properties defined in this class
    /// </summary>
    public List<PropertyInfo> Properties { get; set; } = new();
    
    /// <summary>
    /// Names of classes that this class directly inherits from
    /// </summary>
    public List<string> DirectChildren { get; set; } = new();
    
    /// <summary>
    /// Types that this class is coupled with (uses or is used by)
    /// </summary>
    public HashSet<string> CoupledTypes { get; set; } = new();
    
    /// <summary>
    /// Lines of code in the class
    /// </summary>
    public int LinesOfCode { get; set; }
    
    /// <summary>
    /// Source lines of code (excluding comments and blank lines)
    /// </summary>
    public int SourceLinesOfCode { get; set; }
    
    /// <summary>
    /// Number of comment lines
    /// </summary>
    public int CommentLines { get; set; }

    /// <summary>
    /// Count of method declarations in this class
    /// </summary>
    public int MethodDeclarationCount { get; set; }

    /// <summary>
    /// Count of constructors in this class
    /// </summary>
    public int ConstructorCount { get; set; }

    /// <summary>
    /// Count of property accessors (get/set/init) in this class
    /// </summary>
    public int PropertyAccessorCount { get; set; }

    /// <summary>
    /// Count of event accessors (add/remove) in this class
    /// </summary>
    public int EventAccessorCount { get; set; }

    /// <summary>
    /// Count of indexer accessors (get/set/init) in this class
    /// </summary>
    public int IndexerAccessorCount { get; set; }
    
    /// <summary>
    /// Start line in source file
    /// </summary>
    public int StartLine { get; set; }
    
    /// <summary>
    /// End line in source file
    /// </summary>
    public int EndLine { get; set; }
    
    /// <summary>
    /// Calculated metrics for this class
    /// </summary>
    public ClassMetrics? Metrics { get; set; }
    
    /// <summary>
    /// Gets all defined (non-inherited) methods
    /// </summary>
    public IEnumerable<MethodInfo> DefinedMethods => Methods.Where(m => !m.IsInherited);
    
    /// <summary>
    /// Gets all inherited methods
    /// </summary>
    public IEnumerable<MethodInfo> InheritedMethods => Methods.Where(m => m.IsInherited);
    
    /// <summary>
    /// Gets all defined (non-inherited) fields
    /// </summary>
    public IEnumerable<FieldInfo> DefinedFields => Fields.Where(f => !f.IsInherited);
    
    /// <summary>
    /// Gets all inherited fields
    /// </summary>
    public IEnumerable<FieldInfo> InheritedFields => Fields.Where(f => f.IsInherited);
}
