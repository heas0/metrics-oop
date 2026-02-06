namespace MetricsOOP.Models;

/// <summary>
/// Contains aggregated metrics for the entire project (MOOD metrics)
/// </summary>
public class ProjectMetrics
{
    /// <summary>
    /// Project/solution name
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;
    
    /// <summary>
    /// Analysis date and time
    /// </summary>
    public DateTime AnalysisDate { get; set; } = DateTime.Now;
    
    /// <summary>
    /// List of analyzed files
    /// </summary>
    public List<string> AnalyzedFiles { get; set; } = new();
    
    /// <summary>
    /// All class metrics
    /// </summary>
    public List<ClassMetrics> ClassMetrics { get; set; } = new();
    
    /// <summary>
    /// Detailed class information
    /// </summary>
    public List<ClassInfo> Classes { get; set; } = new();
    
    /// <summary>
    /// Package-level metrics (by namespace)
    /// </summary>
    public List<PackageMetrics> PackageMetrics { get; set; } = new();
    
    // =========== MOOD Metrics ===========
    
    /// <summary>
    /// MHF - Method Hiding Factor
    /// Ratio of hidden methods to total methods
    /// </summary>
    public double MHF { get; set; }
    
    /// <summary>
    /// AHF - Attribute Hiding Factor
    /// Ratio of hidden attributes to total attributes
    /// </summary>
    public double AHF { get; set; }
    
    /// <summary>
    /// MIF - Method Inheritance Factor
    /// Ratio of inherited methods to total available methods
    /// </summary>
    public double MIF { get; set; }
    
    /// <summary>
    /// AIF - Attribute Inheritance Factor
    /// Ratio of inherited attributes to total available attributes
    /// </summary>
    public double AIF { get; set; }
    
    /// <summary>
    /// PF - Polymorphism Factor
    /// Degree of method overriding
    /// </summary>
    public double PF { get; set; }
    
    /// <summary>
    /// CF - Coupling Factor
    /// Inter-class coupling (excluding inheritance)
    /// </summary>
    public double CF { get; set; }
    
    // =========== Aggregate Size Metrics ===========
    
    /// <summary>
    /// Total number of classes
    /// </summary>
    public int TotalClasses { get; set; }
    
    /// <summary>
    /// Total number of interfaces
    /// </summary>
    public int TotalInterfaces { get; set; }
    
    /// <summary>
    /// Total number of methods (NOM)
    /// </summary>
    public int TotalMethods { get; set; }

    /// <summary>
    /// Average number of methods (NOM) across classes
    /// </summary>
    public double AverageNOM { get; set; }
    
    /// <summary>
    /// Total number of fields
    /// </summary>
    public int TotalFields { get; set; }
    
    /// <summary>
    /// Total lines of code
    /// </summary>
    public int TotalLOC { get; set; }
    
    /// <summary>
    /// Total source lines of code
    /// </summary>
    public int TotalSLOC { get; set; }
    
    /// <summary>
    /// Total comment lines
    /// </summary>
    public int TotalCommentLines { get; set; }
    
    // =========== Aggregate CK Metrics ===========
    
    /// <summary>
    /// Average WMC across all classes
    /// </summary>
    public double AverageWMC { get; set; }
    
    /// <summary>
    /// Average DIT across all classes
    /// </summary>
    public double AverageDIT { get; set; }
    
    /// <summary>
    /// Average NOC across all classes
    /// </summary>
    public double AverageNOC { get; set; }
    
    /// <summary>
    /// Average CBO across all classes
    /// </summary>
    public double AverageCBO { get; set; }

    /// <summary>
    /// Average CBO without inheritance across all classes
    /// </summary>
    public double AverageCBONoInheritance { get; set; }
    
    /// <summary>
    /// Average RFC across all classes
    /// </summary>
    public double AverageRFC { get; set; }

    /// <summary>
    /// Average MPC across all classes
    /// </summary>
    public double AverageMPC { get; set; }
    
    /// <summary>
    /// Average LCOM across all classes
    /// </summary>
    public double AverageLCOM { get; set; }
    
    /// <summary>
    /// Average TCC across all classes
    /// </summary>
    public double AverageTCC { get; set; }
    
    /// <summary>
    /// Average LCC across all classes
    /// </summary>
    public double AverageLCC { get; set; }
    
    // =========== Aggregate Complexity Metrics ===========
    
    /// <summary>
    /// Average cyclomatic complexity across all methods
    /// </summary>
    public double AverageCyclomaticComplexity { get; set; }
    
    /// <summary>
    /// Maximum cyclomatic complexity found
    /// </summary>
    public int MaxCyclomaticComplexity { get; set; }
    
    /// <summary>
    /// Average cognitive complexity across all methods
    /// </summary>
    public double AverageCognitiveComplexity { get; set; }
    
    /// <summary>
    /// Maximum cognitive complexity found
    /// </summary>
    public int MaxCognitiveComplexity { get; set; }
    
    /// <summary>
    /// Average maintainability index
    /// </summary>
    public double AverageMaintainabilityIndex { get; set; }
    
    /// <summary>
    /// Aggregated Halstead metrics
    /// </summary>
    public HalsteadMetrics? Halstead { get; set; }
}
