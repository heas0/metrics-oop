using MetricsOOP.Models;

namespace MetricsOOP.Metrics;

/// <summary>
/// Calculates size-related metrics for classes and the entire project
/// </summary>
public class SizeMetricsCalculator
{
    /// <summary>
    /// Calculate size metrics for a single class
    /// </summary>
    public void CalculateClassMetrics(ClassInfo classInfo, ClassMetrics metrics)
    {
        metrics.LOC = classInfo.LinesOfCode;
        metrics.SLOC = classInfo.SourceLinesOfCode;
        metrics.CommentLines = classInfo.CommentLines;
        
        metrics.NumberOfMethods = classInfo.DefinedMethods.Count();
        metrics.NOM = classInfo.MethodDeclarationCount +
                      classInfo.ConstructorCount +
                      classInfo.PropertyAccessorCount +
                      classInfo.EventAccessorCount +
                      classInfo.IndexerAccessorCount;
        metrics.NumberOfFields = classInfo.DefinedFields.Count();
        metrics.NumberOfProperties = classInfo.Properties.Count(p => !p.IsInherited);
        
        // Access modifier counts
        metrics.PublicMethodCount = classInfo.DefinedMethods.Count(m => m.AccessModifier == AccessModifier.Public);
        metrics.PrivateMethodCount = classInfo.DefinedMethods.Count(m => m.AccessModifier == AccessModifier.Private);
        metrics.PublicFieldCount = classInfo.DefinedFields.Count(f => f.AccessModifier == AccessModifier.Public);
        metrics.PrivateFieldCount = classInfo.DefinedFields.Count(f => f.AccessModifier == AccessModifier.Private);
        
        // Inheritance counts
        metrics.OverriddenMethodCount = classInfo.DefinedMethods.Count(m => m.IsOverride);
        metrics.InheritedMethodCount = classInfo.InheritedMethods.Count();
        metrics.InheritedFieldCount = classInfo.InheritedFields.Count();
    }

    /// <summary>
    /// Calculate size metrics for the entire project
    /// </summary>
    public void CalculateProjectMetrics(List<ClassInfo> classes, ProjectMetrics metrics)
    {
        metrics.TotalClasses = classes.Count(c => !c.IsInterface);
        metrics.TotalInterfaces = classes.Count(c => c.IsInterface);
        
        metrics.TotalMethods = classes.Sum(c => c.MethodDeclarationCount +
                                              c.ConstructorCount +
                                              c.PropertyAccessorCount +
                                              c.EventAccessorCount +
                                              c.IndexerAccessorCount);
        metrics.TotalFields = classes.Sum(c => c.DefinedFields.Count());
        
        metrics.TotalLOC = classes.Sum(c => c.LinesOfCode);
        metrics.TotalSLOC = classes.Sum(c => c.SourceLinesOfCode);
        metrics.TotalCommentLines = classes.Sum(c => c.CommentLines);
    }

    /// <summary>
    /// Count lines of code in source files
    /// </summary>
    public static (int total, int source, int blank, int comments) CountLinesInFiles(IEnumerable<string> filePaths)
    {
        int total = 0;
        int source = 0;
        int blank = 0;
        int comments = 0;

        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath))
                continue;

            var lines = File.ReadAllLines(filePath);
            bool inBlockComment = false;

            foreach (var line in lines)
            {
                total++;
                var trimmed = line.Trim();

                if (inBlockComment)
                {
                    comments++;
                    if (trimmed.Contains("*/"))
                        inBlockComment = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    blank++;
                    continue;
                }

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

                // Count as source line
                source++;
                
                // Check for inline comments
                if (trimmed.Contains("//") || trimmed.Contains("/*"))
                    comments++;
            }
        }

        return (total, source, blank, comments);
    }
}
