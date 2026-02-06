namespace MetricsOOP.Models;

/// <summary>
/// Contains Robert C. Martin's package metrics for namespace/module-level analysis
/// </summary>
public class PackageMetrics
{
    /// <summary>
    /// Package (namespace) name
    /// </summary>
    public string PackageName { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of classes in this package
    /// </summary>
    public int ClassCount { get; set; }
    
    /// <summary>
    /// Number of abstract classes and interfaces in this package
    /// </summary>
    public int AbstractClassCount { get; set; }
    
    // =========== Martin's Metrics ===========
    
    /// <summary>
    /// Ca - Afferent Coupling (incoming dependencies)
    /// Number of external packages that depend on classes in this package
    /// </summary>
    public int AfferentCoupling { get; set; }
    
    /// <summary>
    /// Ce - Efferent Coupling (outgoing dependencies)
    /// Number of external packages that classes in this package depend on
    /// </summary>
    public int EfferentCoupling { get; set; }
    
    /// <summary>
    /// I - Instability = Ce / (Ca + Ce)
    /// Range: 0 (maximally stable) to 1 (maximally unstable)
    /// </summary>
    public double Instability { get; set; }
    
    /// <summary>
    /// A - Abstractness = Na / Nc
    /// Ratio of abstract classes to total classes
    /// Range: 0 (concrete) to 1 (abstract)
    /// </summary>
    public double Abstractness { get; set; }
    
    /// <summary>
    /// D - Distance from Main Sequence = |A + I - 1|
    /// Measures how far package is from ideal balance
    /// Range: 0 (on main sequence) to ~0.707 (worst case)
    /// </summary>
    public double DistanceFromMainSequence { get; set; }
    
    // =========== Additional Package Metrics ===========
    
    /// <summary>
    /// NCP - Number of Classes in Package
    /// Same as ClassCount, aliased for metric naming
    /// </summary>
    public int NCP => ClassCount;
    
    /// <summary>
    /// List of external packages this package depends on
    /// </summary>
    public List<string> DependsOn { get; set; } = new();
    
    /// <summary>
    /// List of external packages that depend on this package
    /// </summary>
    public List<string> DependedOnBy { get; set; } = new();
    
    /// <summary>
    /// List of classes in this package
    /// </summary>
    public List<string> Classes { get; set; } = new();

    /// <summary>
    /// OutC - Number of classes in this package that depend on classes from other packages
    /// </summary>
    public int OutC { get; set; }

    /// <summary>
    /// InC - Number of classes in this package that are depended on by classes from other packages
    /// </summary>
    public int InC { get; set; }

    /// <summary>
    /// HC - Number of classes with both incoming and outgoing inter-package dependencies
    /// </summary>
    public int HC { get; set; }

    /// <summary>
    /// SPC - Total number of intra-package class dependencies (directed)
    /// </summary>
    public int SPC { get; set; }

    /// <summary>
    /// SCC - Total number of inter-package class dependencies from this package to others (directed)
    /// </summary>
    public int SCC { get; set; }
}
