namespace MetricsOOP.Models;

/// <summary>
/// Contains all calculated metrics for a single class
/// </summary>
public class ClassMetrics
{
    /// <summary>
    /// Class name
    /// </summary>
    public string ClassName { get; set; } = string.Empty;
    
    /// <summary>
    /// Full qualified class name
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    // =========== CK Metrics ===========
    
    /// <summary>
    /// WMC - Weighted Methods per Class
    /// Sum of complexities of all methods (or method count)
    /// </summary>
    public int WMC { get; set; }
    
    /// <summary>
    /// DIT - Depth of Inheritance Tree
    /// Maximum length of path from class to root (Object)
    /// </summary>
    public int DIT { get; set; }
    
    /// <summary>
    /// NOC - Number of Children
    /// Number of immediate subclasses
    /// </summary>
    public int NOC { get; set; }
    
    /// <summary>
    /// CBO - Coupling Between Object Classes (Total)
    /// Number of classes this class is coupled to (including inheritance)
    /// </summary>
    public int CBO { get; set; }
    
    /// <summary>
    /// CBO without inheritance - coupling excluding base class and interfaces
    /// </summary>
    public int CBONoInheritance { get; set; }
    
    /// <summary>
    /// RFC - Response for a Class
    /// Number of methods + methods directly called
    /// </summary>
    public int RFC { get; set; }

    /// <summary>
    /// MPC - Message Passing Coupling
    /// Number of external method calls made by this class
    /// </summary>
    public int MPC { get; set; }
    
    /// <summary>
    /// LCOM - Lack of Cohesion of Methods (LCOM4)
    /// Number of connected components in method-field graph
    /// </summary>
    public int LCOM { get; set; }
    
    /// <summary>
    /// LCOM percentage (0-100, higher is worse)
    /// </summary>
    public double LCOMPercent { get; set; }
    
    /// <summary>
    /// TCC - Tight Class Cohesion
    /// Ratio of directly connected method pairs to total pairs (0-1)
    /// </summary>
    public double TCC { get; set; }
    
    /// <summary>
    /// LCC - Loose Class Cohesion
    /// Ratio of directly and indirectly connected method pairs to total pairs (0-1)
    /// </summary>
    public double LCC { get; set; }
    
    // =========== Size Metrics ===========
    
    /// <summary>
    /// Total lines of code
    /// </summary>
    public int LOC { get; set; }
    
    /// <summary>
    /// Source lines of code (excluding comments and blanks)
    /// </summary>
    public int SLOC { get; set; }
    
    /// <summary>
    /// Number of methods
    /// </summary>
    public int NumberOfMethods { get; set; }

    /// <summary>
    /// NOM - Number of Methods (C#-specific: methods + constructors + accessors)
    /// </summary>
    public int NOM { get; set; }
    
    /// <summary>
    /// Number of fields/attributes
    /// </summary>
    public int NumberOfFields { get; set; }
    
    /// <summary>
    /// Number of properties
    /// </summary>
    public int NumberOfProperties { get; set; }
    
    /// <summary>
    /// Comment lines count
    /// </summary>
    public int CommentLines { get; set; }
    
    // =========== Complexity Metrics ===========
    
    /// <summary>
    /// Average cyclomatic complexity of methods
    /// </summary>
    public double AverageCyclomaticComplexity { get; set; }
    
    /// <summary>
    /// Maximum cyclomatic complexity among methods
    /// </summary>
    public int MaxCyclomaticComplexity { get; set; }
    
    /// <summary>
    /// Average cognitive complexity of methods
    /// </summary>
    public double AverageCognitiveComplexity { get; set; }
    
    /// <summary>
    /// Maximum cognitive complexity among methods
    /// </summary>
    public int MaxCognitiveComplexity { get; set; }
    
    /// <summary>
    /// Total cognitive complexity (sum of all methods)
    /// </summary>
    public int TotalCognitiveComplexity { get; set; }
    
    /// <summary>
    /// Total cyclomatic complexity (sum of all methods)
    /// </summary>
    public int TotalCyclomaticComplexity { get; set; }
    
    /// <summary>
    /// Halstead metrics aggregated for the class
    /// </summary>
    public HalsteadMetrics? Halstead { get; set; }
    
    /// <summary>
    /// Maintainability Index (0-100, higher is better)
    /// </summary>
    public double MaintainabilityIndex { get; set; }
    
    // =========== Additional Metrics ===========
    
    /// <summary>
    /// Count of public methods
    /// </summary>
    public int PublicMethodCount { get; set; }
    
    /// <summary>
    /// Count of private methods
    /// </summary>
    public int PrivateMethodCount { get; set; }
    
    /// <summary>
    /// Count of public fields
    /// </summary>
    public int PublicFieldCount { get; set; }
    
    /// <summary>
    /// Count of private fields
    /// </summary>
    public int PrivateFieldCount { get; set; }
    
    /// <summary>
    /// Count of overridden methods
    /// </summary>
    public int OverriddenMethodCount { get; set; }
    
    /// <summary>
    /// Count of inherited methods
    /// </summary>
    public int InheritedMethodCount { get; set; }
    
    /// <summary>
    /// Count of inherited fields
    /// </summary>
    public int InheritedFieldCount { get; set; }
}
