namespace MetricsOOP.Models;

/// <summary>
/// Halstead complexity metrics
/// </summary>
public class HalsteadMetrics
{
    /// <summary>
    /// η₁ - Number of distinct operators
    /// </summary>
    public int DistinctOperators { get; set; }
    
    /// <summary>
    /// η₂ - Number of distinct operands
    /// </summary>
    public int DistinctOperands { get; set; }
    
    /// <summary>
    /// N₁ - Total number of operators
    /// </summary>
    public int TotalOperators { get; set; }
    
    /// <summary>
    /// N₂ - Total number of operands
    /// </summary>
    public int TotalOperands { get; set; }
    
    /// <summary>
    /// η - Program vocabulary = η₁ + η₂
    /// </summary>
    public int Vocabulary => DistinctOperators + DistinctOperands;
    
    /// <summary>
    /// N - Program length = N₁ + N₂
    /// </summary>
    public int Length => TotalOperators + TotalOperands;
    
    /// <summary>
    /// N̂ - Calculated program length = η₁ * log₂(η₁) + η₂ * log₂(η₂)
    /// </summary>
    public double CalculatedLength
    {
        get
        {
            if (DistinctOperators == 0 || DistinctOperands == 0)
                return 0;
            return DistinctOperators * Math.Log2(DistinctOperators) + 
                   DistinctOperands * Math.Log2(DistinctOperands);
        }
    }
    
    /// <summary>
    /// V - Volume = N * log₂(η)
    /// </summary>
    public double Volume
    {
        get
        {
            if (Vocabulary == 0)
                return 0;
            return Length * Math.Log2(Vocabulary);
        }
    }
    
    /// <summary>
    /// D - Difficulty = (η₁/2) * (N₂/η₂)
    /// </summary>
    public double Difficulty
    {
        get
        {
            if (DistinctOperands == 0)
                return 0;
            return (DistinctOperators / 2.0) * ((double)TotalOperands / DistinctOperands);
        }
    }
    
    /// <summary>
    /// E - Effort = D * V
    /// </summary>
    public double Effort => Difficulty * Volume;
    
    /// <summary>
    /// T - Time to program (in seconds) = E / 18
    /// </summary>
    public double TimeToProgram => Effort / 18.0;
    
    /// <summary>
    /// B - Estimated bugs = V / 3000
    /// </summary>
    public double EstimatedBugs => Volume / 3000.0;
    
    /// <summary>
    /// Creates a combined HalsteadMetrics from multiple sources
    /// </summary>
    public static HalsteadMetrics Combine(IEnumerable<HalsteadMetrics?> metrics)
    {
        var allOperators = new HashSet<string>();
        var allOperands = new HashSet<string>();
        int totalOps = 0;
        int totalOperands = 0;
        
        foreach (var m in metrics.Where(x => x != null))
        {
            totalOps += m!.TotalOperators;
            totalOperands += m.TotalOperands;
        }
        
        // For combined metrics, we sum the totals and estimate distincts
        var validMetrics = metrics.Where(x => x != null).ToList();
        int distinctOps = validMetrics.Sum(m => m!.DistinctOperators);
        int distinctOperands = validMetrics.Sum(m => m!.DistinctOperands);
        
        // Rough estimation: take max of sums or average * count
        return new HalsteadMetrics
        {
            DistinctOperators = Math.Max(1, distinctOps / Math.Max(1, validMetrics.Count)),
            DistinctOperands = Math.Max(1, distinctOperands / Math.Max(1, validMetrics.Count)),
            TotalOperators = totalOps,
            TotalOperands = totalOperands
        };
    }
}
