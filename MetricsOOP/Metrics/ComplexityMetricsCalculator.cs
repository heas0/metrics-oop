using MetricsOOP.Models;

namespace MetricsOOP.Metrics;

/// <summary>
/// Calculates complexity metrics: Cyclomatic Complexity, Halstead, Maintainability Index
/// </summary>
public class ComplexityMetricsCalculator
{
    /// <summary>
    /// Calculate complexity metrics for a single class
    /// </summary>
    public void CalculateClassMetrics(ClassInfo classInfo, ClassMetrics metrics)
    {
        var methods = classInfo.DefinedMethods.ToList();
        
        if (methods.Count == 0)
        {
            metrics.AverageCyclomaticComplexity = 0;
            metrics.MaxCyclomaticComplexity = 0;
            metrics.TotalCyclomaticComplexity = 0;
            metrics.MaintainabilityIndex = 100;
            return;
        }

        // Cyclomatic complexity metrics
        var complexities = methods.Select(m => m.CyclomaticComplexity).ToList();
        metrics.TotalCyclomaticComplexity = complexities.Sum();
        metrics.MaxCyclomaticComplexity = complexities.Max();
        metrics.AverageCyclomaticComplexity = complexities.Average();
        
        // Cognitive complexity metrics
        var cognitiveComplexities = methods.Select(m => m.CognitiveComplexity).ToList();
        metrics.TotalCognitiveComplexity = cognitiveComplexities.Sum();
        metrics.MaxCognitiveComplexity = cognitiveComplexities.Max();
        metrics.AverageCognitiveComplexity = cognitiveComplexities.Average();

        // Combine Halstead metrics from all methods
        var halsteadMetrics = methods
            .Where(m => m.Halstead != null)
            .Select(m => m.Halstead)
            .ToList();
        
        if (halsteadMetrics.Any())
        {
            metrics.Halstead = CombineHalsteadMetrics(halsteadMetrics!);
        }

        // Calculate Maintainability Index
        metrics.MaintainabilityIndex = CalculateMaintainabilityIndex(
            metrics.Halstead?.Volume ?? 0,
            metrics.AverageCyclomaticComplexity,
            classInfo.SourceLinesOfCode,
            classInfo.CommentLines,
            classInfo.LinesOfCode
        );
    }

    /// <summary>
    /// Calculate complexity metrics for the entire project
    /// </summary>
    public void CalculateProjectMetrics(List<ClassMetrics> classMetrics, ProjectMetrics projectMetrics)
    {
        if (classMetrics.Count == 0)
            return;

        var allCC = classMetrics.Where(c => c.TotalCyclomaticComplexity > 0)
            .Select(c => c.AverageCyclomaticComplexity).ToList();
        
        if (allCC.Any())
        {
            projectMetrics.AverageCyclomaticComplexity = allCC.Average();
            projectMetrics.MaxCyclomaticComplexity = classMetrics.Max(c => c.MaxCyclomaticComplexity);
        }
        
        // Cognitive complexity aggregation
        var allCogCC = classMetrics.Where(c => c.TotalCognitiveComplexity > 0)
            .Select(c => c.AverageCognitiveComplexity).ToList();
        
        if (allCogCC.Any())
        {
            projectMetrics.AverageCognitiveComplexity = allCogCC.Average();
            projectMetrics.MaxCognitiveComplexity = classMetrics.Max(c => c.MaxCognitiveComplexity);
        }

        var allMI = classMetrics.Where(c => c.MaintainabilityIndex > 0)
            .Select(c => c.MaintainabilityIndex).ToList();
        
        if (allMI.Any())
        {
            projectMetrics.AverageMaintainabilityIndex = allMI.Average();
        }

        // Combine all Halstead metrics
        var allHalstead = classMetrics
            .Where(c => c.Halstead != null)
            .Select(c => c.Halstead)
            .ToList();
        
        if (allHalstead.Any())
        {
            projectMetrics.Halstead = CombineHalsteadMetrics(allHalstead!);
        }
    }

    /// <summary>
    /// Calculate Maintainability Index
    /// MI = max(0, 100 × (171 - 5.2×ln(V) - 0.23×G - 16.2×ln(L) + 50×sin(√(2.4×C))) / 171)
    /// Where: V = Halstead Volume, G = Cyclomatic Complexity, L = SLOC, C = comment ratio
    /// </summary>
    public double CalculateMaintainabilityIndex(double halsteadVolume, double cyclomaticComplexity, 
        int sloc, int commentLines, int totalLines)
    {
        if (sloc <= 0)
            return 100;

        // Handle edge cases
        double V = Math.Max(1, halsteadVolume);
        double G = Math.Max(1, cyclomaticComplexity);
        double L = Math.Max(1, sloc);
        
        // Comment percentage (0-100)
        double C = totalLines > 0 ? (double)commentLines / totalLines * 100 : 0;

        // Original formula
        double mi = 171.0 - 5.2 * Math.Log(V) - 0.23 * G - 16.2 * Math.Log(L);
        
        // Comment bonus (optional, included in some variants)
        if (C > 0)
        {
            mi += 50 * Math.Sin(Math.Sqrt(2.4 * C));
        }

        // Normalize to 0-100 scale
        mi = 100.0 * mi / 171.0;
        
        return Math.Max(0, Math.Min(100, mi));
    }

    /// <summary>
    /// Get complexity level description based on cyclomatic complexity
    /// </summary>
    public static string GetComplexityLevel(int cc)
    {
        return cc switch
        {
            <= 10 => "Simple (low risk)",
            <= 20 => "Moderate complexity",
            <= 50 => "Complex (high risk)",
            _ => "Very complex (untestable)"
        };
    }

    /// <summary>
    /// Get maintainability level description
    /// </summary>
    public static string GetMaintainabilityLevel(double mi)
    {
        return mi switch
        {
            >= 80 => "High maintainability",
            >= 60 => "Moderate maintainability",
            >= 40 => "Low maintainability",
            _ => "Very low maintainability"
        };
    }

    private HalsteadMetrics CombineHalsteadMetrics(List<HalsteadMetrics> metrics)
    {
        // Sum operators and operands across all methods
        var allDistinctOperators = new HashSet<int>();
        var allDistinctOperands = new HashSet<int>();
        
        int totalOps = 0;
        int totalOperands = 0;
        int distinctOps = 0;
        int distinctOperands = 0;

        foreach (var m in metrics)
        {
            totalOps += m.TotalOperators;
            totalOperands += m.TotalOperands;
            distinctOps = Math.Max(distinctOps, m.DistinctOperators);
            distinctOperands = Math.Max(distinctOperands, m.DistinctOperands);
        }

        // Use combined distinct (approximation)
        distinctOps = Math.Min(distinctOps * 2, totalOps);
        distinctOperands = Math.Min(distinctOperands * 2, totalOperands);

        return new HalsteadMetrics
        {
            DistinctOperators = Math.Max(1, distinctOps),
            DistinctOperands = Math.Max(1, distinctOperands),
            TotalOperators = totalOps,
            TotalOperands = totalOperands
        };
    }
}
