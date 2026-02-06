using MetricsOOP.Metrics;
using MetricsOOP.Models;
using Newtonsoft.Json;

namespace MetricsOOP.Reports;

/// <summary>
/// Generates reports in various formats (console, JSON)
/// </summary>
public class ReportGenerator
{
    /// <summary>
    /// Threshold values for metrics (for color coding)
    /// </summary>
    public class Thresholds
    {
        public int WMC_Warning { get; set; } = 20;
        public int WMC_Critical { get; set; } = 50;
        
        public int DIT_Warning { get; set; } = 4;
        public int DIT_Critical { get; set; } = 6;
        
        public int NOC_Warning { get; set; } = 10;
        public int NOC_Critical { get; set; } = 20;
        
        public int CBO_Warning { get; set; } = 8;
        public int CBO_Critical { get; set; } = 14;
        
        public int RFC_Warning { get; set; } = 50;
        public int RFC_Critical { get; set; } = 100;
        
        public int LCOM_Warning { get; set; } = 2;
        public int LCOM_Critical { get; set; } = 4;
        
        public int CC_Warning { get; set; } = 10;
        public int CC_Critical { get; set; } = 20;
        
        public double MI_Warning { get; set; } = 60;
        public double MI_Critical { get; set; } = 40;
        
        public double MHF_Low { get; set; } = 0.2;
        public double MHF_High { get; set; } = 0.4;
        
        public double CF_Warning { get; set; } = 0.12;
        public double CF_Critical { get; set; } = 0.2;
    }

    private readonly Thresholds _thresholds;

    public ReportGenerator(Thresholds? thresholds = null)
    {
        _thresholds = thresholds ?? new Thresholds();
    }

    /// <summary>
    /// Print detailed project metrics to console
    /// </summary>
    public void PrintConsoleReport(ProjectMetrics metrics, bool verbose = false)
    {
        Console.WriteLine();
        WriteHeader("═══════════════════════════════════════════════════════════════════════════════");
        WriteHeader($"  OOP METRICS ANALYSIS REPORT - {metrics.ProjectName}");
        WriteHeader($"  Generated: {metrics.AnalysisDate:yyyy-MM-dd HH:mm:ss}");
        WriteHeader("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Summary
        WriteSection("SUMMARY");
        Console.WriteLine($"  Files analyzed: {metrics.AnalyzedFiles.Count}");
        Console.WriteLine($"  Classes:        {metrics.TotalClasses}");
        Console.WriteLine($"  Interfaces:     {metrics.TotalInterfaces}");
        Console.WriteLine($"  Total NOM:      {metrics.TotalMethods}");
        Console.WriteLine($"  Avg NOM:        {metrics.AverageNOM:F2}");
        Console.WriteLine($"  Total fields:   {metrics.TotalFields}");
        Console.WriteLine($"  Total LOC:      {metrics.TotalLOC}");
        Console.WriteLine($"  Source LOC:     {metrics.TotalSLOC}");
        Console.WriteLine($"  Comment lines:  {metrics.TotalCommentLines}");
        Console.WriteLine();

        // MOOD Metrics
        WriteSection("MOOD METRICS (System-Level)");
        PrintMetric("MHF (Method Hiding Factor)", metrics.MHF, 0, 1, false, 
            "Encapsulation measure (0.2-0.4 recommended)");
        PrintMetric("AHF (Attribute Hiding Factor)", metrics.AHF, 0, 1, false, 
            "Should be close to 1.0 (all fields private)");
        PrintMetric("MIF (Method Inheritance Factor)", metrics.MIF, 0, 1, false, 
            "Inheritance reuse (0.2-0.8 recommended)");
        PrintMetric("AIF (Attribute Inheritance Factor)", metrics.AIF, 0, 1, false);
        PrintMetric("PF (Polymorphism Factor)", metrics.PF, 0, 1, false, 
            "Degree of method overriding");
        PrintMetric("CF (Coupling Factor)", metrics.CF, _thresholds.CF_Critical, _thresholds.CF_Warning, true, 
            "Should be ≤ 0.12");
        Console.WriteLine();

        // Average CK Metrics
        WriteSection("AVERAGE CK METRICS");
        PrintMetricInt("Avg WMC", metrics.AverageWMC, _thresholds.WMC_Critical, _thresholds.WMC_Warning, true);
        PrintMetricInt("Avg DIT", metrics.AverageDIT, _thresholds.DIT_Critical, _thresholds.DIT_Warning, true);
        PrintMetricInt("Avg NOC", metrics.AverageNOC, _thresholds.NOC_Critical, _thresholds.NOC_Warning, true);
        PrintMetricInt("Avg CBO", metrics.AverageCBO, _thresholds.CBO_Critical, _thresholds.CBO_Warning, true);
        PrintMetricInt("Avg CBO (No Inh)", metrics.AverageCBONoInheritance, _thresholds.CBO_Critical, _thresholds.CBO_Warning, true);
        PrintMetricInt("Avg RFC", metrics.AverageRFC, _thresholds.RFC_Critical, _thresholds.RFC_Warning, true);
        PrintMetricPlain("Avg MPC", metrics.AverageMPC);
        PrintMetricInt("Avg LCOM", metrics.AverageLCOM, _thresholds.LCOM_Critical, _thresholds.LCOM_Warning, true);
        Console.WriteLine();
        
        // Cohesion Metrics (TCC/LCC)
        WriteSection("COHESION METRICS");
        PrintMetric("Avg TCC (Tight Class Cohesion)", metrics.AverageTCC, 0.3, 0.5, false, 
            "1.0 = ideal, < 0.5 = consider splitting");
        PrintMetric("Avg LCC (Loose Class Cohesion)", metrics.AverageLCC, 0.3, 0.5, false,
            "Includes indirect connections");
        Console.WriteLine();

        // Complexity
        WriteSection("COMPLEXITY METRICS");
        PrintMetricInt("Avg Cyclomatic Complexity", metrics.AverageCyclomaticComplexity, 
            _thresholds.CC_Critical, _thresholds.CC_Warning, true);
        Console.WriteLine($"  Max Cyclomatic Complexity:      {metrics.MaxCyclomaticComplexity}");
        PrintMetricInt("Avg Cognitive Complexity", metrics.AverageCognitiveComplexity,
            15, 25, true);
        Console.WriteLine($"  Max Cognitive Complexity:       {metrics.MaxCognitiveComplexity}");
        PrintMetric("Avg Maintainability Index", metrics.AverageMaintainabilityIndex, 
            _thresholds.MI_Critical, _thresholds.MI_Warning, false, 
            ComplexityMetricsCalculator.GetMaintainabilityLevel(metrics.AverageMaintainabilityIndex));

        if (metrics.Halstead != null)
        {
            Console.WriteLine();
            Console.WriteLine("  Halstead Metrics (Aggregated):");
            Console.WriteLine($"    Vocabulary:        {metrics.Halstead.Vocabulary}");
            Console.WriteLine($"    Length:            {metrics.Halstead.Length}");
            Console.WriteLine($"    Volume:            {metrics.Halstead.Volume:F2}");
            Console.WriteLine($"    Difficulty:        {metrics.Halstead.Difficulty:F2}");
            Console.WriteLine($"    Effort:            {metrics.Halstead.Effort:F2}");
            Console.WriteLine($"    Est. Time (hours): {metrics.Halstead.TimeToProgram / 3600:F2}");
            Console.WriteLine($"    Est. Bugs:         {metrics.Halstead.EstimatedBugs:F2}");
        }
        Console.WriteLine();

        // Per-class metrics table
        if (verbose && metrics.ClassMetrics.Any())
        {
            WriteSection("PER-CLASS METRICS");
            PrintClassTable(metrics.ClassMetrics);
            Console.WriteLine();
            WriteSection("COUPLING DETAILS");
            PrintCouplingTable(metrics.ClassMetrics);
            Console.WriteLine();
        }

        // Package metrics
        if (metrics.PackageMetrics.Any())
        {
            WriteSection("PACKAGE METRICS (Martin's Metrics)");
            PrintPackageTable(metrics.PackageMetrics);
            Console.WriteLine();
            WriteSection("PACKAGE COUPLING DETAILS");
            PrintPackageCouplingTable(metrics.PackageMetrics);
            Console.WriteLine();
        }

        // Classes needing attention
        PrintProblematicClasses(metrics.ClassMetrics);

        WriteHeader("═══════════════════════════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// Export metrics to JSON file
    /// </summary>
    public void ExportToJson(ProjectMetrics metrics, string outputPath)
    {
        var json = JsonConvert.SerializeObject(metrics, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        
        File.WriteAllText(outputPath, json);
        Console.WriteLine($"JSON report exported to: {outputPath}");
    }

    /// <summary>
    /// Get JSON string of metrics
    /// </summary>
    public string GetJson(ProjectMetrics metrics)
    {
        return JsonConvert.SerializeObject(metrics, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
    }

    private void PrintClassTable(List<ClassMetrics> classMetrics)
    {
        Console.WriteLine("  ┌────────────────────────────────┬──────┬─────┬─────┬─────┬──────┬──────┬────────┬────────┐");
        Console.WriteLine("  │ Class                          │ WMC  │ DIT │ NOC │ CBO │ RFC  │ LCOM │ Avg CC │   MI   │");
        Console.WriteLine("  ├────────────────────────────────┼──────┼─────┼─────┼─────┼──────┼──────┼────────┼────────┤");

        foreach (var cm in classMetrics.OrderByDescending(c => c.WMC).Take(20))
        {
            var name = cm.ClassName.Length > 30 ? cm.ClassName[..27] + "..." : cm.ClassName.PadRight(30);
            Console.Write($"  │ {name} │");
            WriteColoredValue(cm.WMC, 6, _thresholds.WMC_Warning, _thresholds.WMC_Critical);
            Console.Write("│");
            WriteColoredValue(cm.DIT, 5, _thresholds.DIT_Warning, _thresholds.DIT_Critical);
            Console.Write("│");
            WriteColoredValue(cm.NOC, 5, _thresholds.NOC_Warning, _thresholds.NOC_Critical);
            Console.Write("│");
            WriteColoredValue(cm.CBO, 5, _thresholds.CBO_Warning, _thresholds.CBO_Critical);
            Console.Write("│");
            WriteColoredValue(cm.RFC, 6, _thresholds.RFC_Warning, _thresholds.RFC_Critical);
            Console.Write("│");
            WriteColoredValue(cm.LCOM, 6, _thresholds.LCOM_Warning, _thresholds.LCOM_Critical);
            Console.Write("│");
            WriteColoredValue(cm.AverageCyclomaticComplexity, 8, _thresholds.CC_Warning, _thresholds.CC_Critical);
            Console.Write("│");
            WriteColoredValueReverse(cm.MaintainabilityIndex, 8, _thresholds.MI_Warning, _thresholds.MI_Critical);
            Console.WriteLine("│");
        }

        Console.WriteLine("  └────────────────────────────────┴──────┴─────┴─────┴─────┴──────┴──────┴────────┴────────┘");
        
        if (classMetrics.Count > 20)
        {
            Console.WriteLine($"  ... and {classMetrics.Count - 20} more classes");
        }
    }

    private void PrintCouplingTable(List<ClassMetrics> classMetrics)
    {
        Console.WriteLine("  ┌────────────────────────────────┬──────┬─────────┬─────┐");
        Console.WriteLine("  │ Class                          │ CBO  │ CBO(no) │ MPC │");
        Console.WriteLine("  ├────────────────────────────────┼──────┼─────────┼─────┤");

        foreach (var cm in classMetrics.OrderByDescending(c => c.CBO).Take(20))
        {
            var name = cm.ClassName.Length > 30 ? cm.ClassName[..27] + "..." : cm.ClassName.PadRight(30);
            Console.Write($"  │ {name} │");
            WriteColoredValue(cm.CBO, 6, _thresholds.CBO_Warning, _thresholds.CBO_Critical);
            Console.Write("│");
            WriteColoredValue(cm.CBONoInheritance, 9, _thresholds.CBO_Warning, _thresholds.CBO_Critical);
            Console.Write($"│ {cm.MPC,3} │");
            Console.WriteLine();
        }

        Console.WriteLine("  └────────────────────────────────┴──────┴─────────┴─────┘");
    }

    private void PrintPackageTable(List<PackageMetrics> packageMetrics)
    {
        Console.WriteLine("  ┌─────────────────────────┬────────┬──────┬──────┬─────────┬────────────┬──────────┐");
        Console.WriteLine("  │ Package (Namespace)     │ Classes│  Ca  │  Ce  │    I    │     A      │    D     │");
        Console.WriteLine("  ├─────────────────────────┼────────┼──────┼──────┼─────────┼────────────┼──────────┤");
        
        foreach (var pm in packageMetrics.OrderByDescending(p => p.ClassCount))
        {
            var name = pm.PackageName.Length > 23 ? pm.PackageName[..20] + "..." : pm.PackageName.PadRight(23);
            Console.Write($"  │ {name} │");
            Console.Write($" {pm.ClassCount,5}  │");
            Console.Write($" {pm.AfferentCoupling,4} │");
            Console.Write($" {pm.EfferentCoupling,4} │");
            
            // Instability (color coded)
            WriteColoredValue(pm.Instability, 9, 0.7, 0.9);
            Console.Write("│");
            
            // Abstractness
            Console.Write($" {pm.Abstractness:F3}      │");
            
            // Distance from main sequence (lower is better)
            WriteColoredValue(pm.DistanceFromMainSequence, 10, 0.3, 0.5);
            Console.WriteLine("│");
        }
        
        Console.WriteLine("  └─────────────────────────┴────────┴──────┴──────┴─────────┴────────────┴──────────┘");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Ca=Afferent Coupling, Ce=Efferent Coupling, I=Instability, A=Abstractness, D=Distance");
        Console.ResetColor();
    }

    private void PrintPackageCouplingTable(List<PackageMetrics> packageMetrics)
    {
        Console.WriteLine("  ┌─────────────────────────┬─────┬─────┬─────┬─────┬─────┬─────┐");
        Console.WriteLine("  │ Package (Namespace)     │ NCP │ OutC│ InC │ HC  │ SPC │ SCC │");
        Console.WriteLine("  ├─────────────────────────┼─────┼─────┼─────┼─────┼─────┼─────┤");

        foreach (var pm in packageMetrics.OrderByDescending(p => p.ClassCount))
        {
            var name = pm.PackageName.Length > 23 ? pm.PackageName[..20] + "..." : pm.PackageName.PadRight(23);
            Console.Write($"  │ {name} │");
            Console.Write($" {pm.NCP,3} │");
            Console.Write($" {pm.OutC,3} │");
            Console.Write($" {pm.InC,3} │");
            Console.Write($" {pm.HC,3} │");
            Console.Write($" {pm.SPC,3} │");
            Console.WriteLine($" {pm.SCC,3} │");
        }

        Console.WriteLine("  └─────────────────────────┴─────┴─────┴─────┴─────┴─────┴─────┘");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  NCP=Classes, OutC/InC=Inter-package class deps, HC=Both in/out, SPC=Intra, SCC=Inter");
        Console.ResetColor();
    }

    private void PrintProblematicClasses(List<ClassMetrics> classMetrics)
    {
        var problematic = classMetrics.Where(c =>
            c.WMC > _thresholds.WMC_Warning ||
            c.DIT > _thresholds.DIT_Warning ||
            c.CBO > _thresholds.CBO_Warning ||
            c.LCOM > _thresholds.LCOM_Warning ||
            c.MaxCyclomaticComplexity > _thresholds.CC_Warning ||
            c.MaintainabilityIndex < _thresholds.MI_Warning
        ).ToList();

        if (!problematic.Any())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✓ No classes with concerning metric values detected.");
            Console.ResetColor();
            Console.WriteLine();
            return;
        }

        WriteSection("CLASSES NEEDING ATTENTION");
        
        foreach (var cm in problematic.OrderByDescending(c => c.WMC).Take(10))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ {cm.ClassName}:");
            Console.ResetColor();
            
            var issues = new List<string>();
            if (cm.WMC > _thresholds.WMC_Critical)
                issues.Add($"Very high WMC ({cm.WMC}) - consider splitting class");
            else if (cm.WMC > _thresholds.WMC_Warning)
                issues.Add($"High WMC ({cm.WMC}) - class may be too complex");
            
            if (cm.DIT > _thresholds.DIT_Critical)
                issues.Add($"Very deep inheritance ({cm.DIT}) - consider flattening hierarchy");
            else if (cm.DIT > _thresholds.DIT_Warning)
                issues.Add($"Deep inheritance ({cm.DIT})");
            
            if (cm.CBO > _thresholds.CBO_Critical)
                issues.Add($"Very high coupling ({cm.CBO}) - reduce dependencies");
            else if (cm.CBO > _thresholds.CBO_Warning)
                issues.Add($"High coupling ({cm.CBO})");
            
            if (cm.LCOM > _thresholds.LCOM_Critical)
                issues.Add($"Very low cohesion (LCOM={cm.LCOM}) - class should be split");
            else if (cm.LCOM > _thresholds.LCOM_Warning)
                issues.Add($"Low cohesion (LCOM={cm.LCOM}) - review class responsibilities");
            
            if (cm.MaxCyclomaticComplexity > _thresholds.CC_Critical)
                issues.Add($"Very complex method (CC={cm.MaxCyclomaticComplexity}) - refactor");
            else if (cm.MaxCyclomaticComplexity > _thresholds.CC_Warning)
                issues.Add($"Complex method (CC={cm.MaxCyclomaticComplexity})");
            
            if (cm.MaintainabilityIndex < _thresholds.MI_Critical)
                issues.Add($"Very low maintainability ({cm.MaintainabilityIndex:F0}%)");
            else if (cm.MaintainabilityIndex < _thresholds.MI_Warning)
                issues.Add($"Low maintainability ({cm.MaintainabilityIndex:F0}%)");

            foreach (var issue in issues)
            {
                Console.WriteLine($"      - {issue}");
            }
            Console.WriteLine();
        }
    }

    private void WriteHeader(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    private void WriteSection(string text)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"─── {text} ───");
        Console.ResetColor();
    }

    private void PrintMetric(string name, double value, double critical, double warning, bool higherIsBad, string? description = null)
    {
        Console.Write($"  {name.PadRight(35)}: ");
        
        bool isBad = higherIsBad ? value >= critical : value <= critical;
        bool isWarning = higherIsBad ? value >= warning : value <= warning;
        
        Console.ForegroundColor = isBad ? ConsoleColor.Red : (isWarning ? ConsoleColor.Yellow : ConsoleColor.Green);
        Console.Write($"{value:F3}");
        Console.ResetColor();
        
        if (description != null)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  ({description})");
            Console.ResetColor();
        }
        Console.WriteLine();
    }

    private void PrintMetricInt(string name, double value, double critical, double warning, bool higherIsBad)
    {
        Console.Write($"  {name.PadRight(35)}: ");
        
        bool isBad = higherIsBad ? value >= critical : value <= critical;
        bool isWarning = higherIsBad ? value >= warning : value <= warning;
        
        Console.ForegroundColor = isBad ? ConsoleColor.Red : (isWarning ? ConsoleColor.Yellow : ConsoleColor.Green);
        Console.Write($"{value:F2}");
        Console.ResetColor();
        Console.WriteLine();
    }

    private void PrintMetricPlain(string name, double value, string? description = null)
    {
        Console.Write($"  {name.PadRight(35)}: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{value:F2}");
        Console.ResetColor();

        if (description != null)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  ({description})");
            Console.ResetColor();
        }
        Console.WriteLine();
    }

    private void WriteColoredValue(double value, int width, double warning, double critical)
    {
        var str = value.ToString("F1").PadLeft(width - 1) + " ";
        
        Console.ForegroundColor = value >= critical ? ConsoleColor.Red : 
                                  value >= warning ? ConsoleColor.Yellow : ConsoleColor.Green;
        Console.Write(str);
        Console.ResetColor();
    }

    private void WriteColoredValue(int value, int width, int warning, int critical)
    {
        var str = value.ToString().PadLeft(width - 1) + " ";
        
        Console.ForegroundColor = value >= critical ? ConsoleColor.Red : 
                                  value >= warning ? ConsoleColor.Yellow : ConsoleColor.Green;
        Console.Write(str);
        Console.ResetColor();
    }

    private void WriteColoredValueReverse(double value, int width, double warning, double critical)
    {
        var str = value.ToString("F1").PadLeft(width - 1) + " ";
        
        // For MI, lower is worse
        Console.ForegroundColor = value <= critical ? ConsoleColor.Red : 
                                  value <= warning ? ConsoleColor.Yellow : ConsoleColor.Green;
        Console.Write(str);
        Console.ResetColor();
    }
}
