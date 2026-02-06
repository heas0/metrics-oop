using MetricsOOP.Metrics;
using MetricsOOP.Models;
using MetricsOOP.Parsers;
using MetricsOOP.Reports;

namespace MetricsOOP;

/// <summary>
/// Main program entry point - CLI for OOP Metrics Analyzer
/// </summary>
public class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintHelp();
            return 0;
        }

        // Parse arguments
        string? targetPath = null;
        string? outputJson = null;
        bool verbose = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                        outputJson = args[++i];
                    break;
                case "--verbose":
                case "-v":
                    verbose = true;
                    break;
                case "--json":
                case "-j":
                    if (i + 1 < args.Length)
                        outputJson = args[++i];
                    break;
                default:
                    if (!args[i].StartsWith("-"))
                        targetPath = args[i];
                    break;
            }
        }

        if (string.IsNullOrEmpty(targetPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: No target path specified.");
            Console.ResetColor();
            PrintHelp();
            return 1;
        }

        try
        {
            var metrics = AnalyzePath(targetPath, verbose);
            
            if (metrics == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: No C# files found to analyze.");
                Console.ResetColor();
                return 1;
            }

            // Generate reports
            var reporter = new ReportGenerator();
            reporter.PrintConsoleReport(metrics, verbose);

            if (!string.IsNullOrEmpty(outputJson))
            {
                reporter.ExportToJson(metrics, outputJson);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            Console.ResetColor();
            return 1;
        }
    }

    static ProjectMetrics? AnalyzePath(string targetPath, bool verbose)
    {
        List<string> files;
        string projectName;

        if (File.Exists(targetPath))
        {
            // Single file
            files = new List<string> { Path.GetFullPath(targetPath) };
            projectName = Path.GetFileName(targetPath);
        }
        else if (Directory.Exists(targetPath))
        {
            // Directory - find all .cs files recursively
            files = Directory.GetFiles(targetPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
                .ToList();
            projectName = new DirectoryInfo(targetPath).Name;
        }
        else
        {
            throw new FileNotFoundException($"Path not found: {targetPath}");
        }

        if (files.Count == 0)
            return null;

        if (verbose)
        {
            Console.WriteLine($"Analyzing {files.Count} file(s)...");
        }

        // Parse all files
        var analyzer = new CodeAnalyzer();
        var classes = analyzer.AnalyzeFiles(files);

        if (verbose)
        {
            Console.WriteLine($"Found {classes.Count} classes/interfaces.");
        }

        // Calculate metrics
        var projectMetrics = new ProjectMetrics
        {
            ProjectName = projectName,
            AnalyzedFiles = files,
            Classes = classes
        };

        // Calculate CK metrics for each class
        var ckCalculator = new CKMetricsCalculator(classes);
        var sizeCalculator = new SizeMetricsCalculator();
        var complexityCalculator = new ComplexityMetricsCalculator();

        foreach (var cls in classes)
        {
            var classMetrics = new ClassMetrics
            {
                ClassName = cls.Name,
                FullName = cls.FullName
            };

            ckCalculator.CalculateMetrics(cls, classMetrics);
            sizeCalculator.CalculateClassMetrics(cls, classMetrics);
            complexityCalculator.CalculateClassMetrics(cls, classMetrics);

            cls.Metrics = classMetrics;
            projectMetrics.ClassMetrics.Add(classMetrics);
        }

        // Calculate MOOD metrics (system-level)
        var moodCalculator = new MOODMetricsCalculator();
        moodCalculator.CalculateMetrics(classes, projectMetrics);

        // Calculate project-level size metrics
        sizeCalculator.CalculateProjectMetrics(classes, projectMetrics);

        // Calculate project-level complexity metrics
        complexityCalculator.CalculateProjectMetrics(projectMetrics.ClassMetrics, projectMetrics);

        // Calculate average CK metrics
        if (projectMetrics.ClassMetrics.Any())
        {
            projectMetrics.AverageWMC = projectMetrics.ClassMetrics.Average(c => c.WMC);
            projectMetrics.AverageDIT = projectMetrics.ClassMetrics.Average(c => c.DIT);
            projectMetrics.AverageNOC = projectMetrics.ClassMetrics.Average(c => c.NOC);
            projectMetrics.AverageCBO = projectMetrics.ClassMetrics.Average(c => c.CBO);
            projectMetrics.AverageCBONoInheritance = projectMetrics.ClassMetrics.Average(c => c.CBONoInheritance);
            projectMetrics.AverageRFC = projectMetrics.ClassMetrics.Average(c => c.RFC);
            projectMetrics.AverageMPC = projectMetrics.ClassMetrics.Average(c => c.MPC);
            projectMetrics.AverageLCOM = projectMetrics.ClassMetrics.Average(c => c.LCOM);
            projectMetrics.AverageTCC = projectMetrics.ClassMetrics.Average(c => c.TCC);
            projectMetrics.AverageLCC = projectMetrics.ClassMetrics.Average(c => c.LCC);
            projectMetrics.AverageNOM = projectMetrics.ClassMetrics.Average(c => c.NOM);
        }
        
        // Calculate package metrics (Martin's metrics)
        var packageCalculator = new PackageMetricsCalculator();
        projectMetrics.PackageMetrics = packageCalculator.CalculateMetrics(classes);

        return projectMetrics;
    }

    static void PrintHelp()
    {
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════════════════════╗
║                     OOP METRICS ANALYZER v1.0                                 ║
║                     Object-Oriented Quality Metrics Tool                      ║
╚═══════════════════════════════════════════════════════════════════════════════╝

USAGE:
    MetricsOOP <path> [options]

ARGUMENTS:
    <path>              Path to a C# file or directory to analyze

OPTIONS:
    -h, --help          Show this help message
    -v, --verbose       Show detailed per-class metrics table
    -o, --output <file> Export results to JSON file
    -j, --json <file>   Same as --output

METRICS CALCULATED:

  CK Metrics (per class):
    • WMC  - Weighted Methods per Class (sum of cyclomatic complexities)
    • DIT  - Depth of Inheritance Tree (inheritance depth)
    • NOC  - Number of Children (direct subclasses)
    • CBO  - Coupling Between Objects (class dependencies)
    • RFC  - Response for a Class (methods + called methods)
    • LCOM - Lack of Cohesion of Methods (LCOM4 - connected components)

  MOOD Metrics (system-wide):
    • MHF  - Method Hiding Factor (encapsulation)
    • AHF  - Attribute Hiding Factor (data hiding)
    • MIF  - Method Inheritance Factor (inheritance reuse)
    • AIF  - Attribute Inheritance Factor
    • PF   - Polymorphism Factor (overriding usage)
    • CF   - Coupling Factor (inter-class coupling)

  Size Metrics:
    • LOC  - Lines of Code
    • SLOC - Source Lines of Code (excluding comments/blanks)
    • Number of classes, methods, fields

  Complexity Metrics:
    • Cyclomatic Complexity (McCabe)
    • Halstead Metrics (volume, difficulty, effort, bugs estimate)
    • Maintainability Index

EXAMPLES:
    MetricsOOP MyProject\src
    MetricsOOP MyClass.cs -v
    MetricsOOP . --output metrics.json -v

RECOMMENDED THRESHOLDS:
    WMC  ≤ 20-50    DIT  ≤ 5       CBO  ≤ 14
    CC   ≤ 10       LCOM = 1       MI   ≥ 60

");
    }
}
