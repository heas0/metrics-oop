namespace MetricsOOP.Models;

/// <summary>
/// Represents information about a class method
/// </summary>
public class MethodInfo
{
    /// <summary>
    /// Method name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Full method signature
    /// </summary>
    public string Signature { get; set; } = string.Empty;
    
    /// <summary>
    /// Return type name
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;
    
    /// <summary>
    /// Access modifier (public, private, protected, internal)
    /// </summary>
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Private;
    
    /// <summary>
    /// Whether the method is static
    /// </summary>
    public bool IsStatic { get; set; }
    
    /// <summary>
    /// Whether the method is virtual
    /// </summary>
    public bool IsVirtual { get; set; }
    
    /// <summary>
    /// Whether the method is abstract
    /// </summary>
    public bool IsAbstract { get; set; }
    
    /// <summary>
    /// Whether the method is override
    /// </summary>
    public bool IsOverride { get; set; }
    
    /// <summary>
    /// Whether this method is inherited from a base class
    /// </summary>
    public bool IsInherited { get; set; }
    
    /// <summary>
    /// Number of parameters
    /// </summary>
    public int ParameterCount { get; set; }
    
    /// <summary>
    /// Cyclomatic complexity of the method
    /// </summary>
    public int CyclomaticComplexity { get; set; } = 1;
    
    /// <summary>
    /// Cognitive complexity of the method (SonarSource algorithm)
    /// Accounts for nesting depth and structural complexity
    /// </summary>
    public int CognitiveComplexity { get; set; }
    
    /// <summary>
    /// Lines of code in the method
    /// </summary>
    public int LinesOfCode { get; set; }
    
    /// <summary>
    /// Names of fields accessed by this method
    /// </summary>
    public HashSet<string> AccessedFields { get; set; } = new();
    
    /// <summary>
    /// Names of methods called by this method (within the same class)
    /// </summary>
    public HashSet<string> CalledMethods { get; set; } = new();
    
    /// <summary>
    /// Names of external methods called (other classes)
    /// </summary>
    public HashSet<string> ExternalMethodCalls { get; set; } = new();
    
    /// <summary>
    /// External types used by this method
    /// </summary>
    public HashSet<string> UsedTypes { get; set; } = new();
    
    /// <summary>
    /// Checks if the method is hidden (non-public)
    /// </summary>
    public bool IsHidden => AccessModifier == AccessModifier.Private || 
                            AccessModifier == AccessModifier.Protected;
    
    /// <summary>
    /// Halstead metrics for the method
    /// </summary>
    public HalsteadMetrics? Halstead { get; set; }
}
