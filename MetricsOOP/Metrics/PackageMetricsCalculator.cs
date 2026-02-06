using MetricsOOP.Models;

namespace MetricsOOP.Metrics;

/// <summary>
/// Calculates Robert C. Martin's Package Metrics for namespace-level analysis
/// These metrics help evaluate package stability, abstractness, and dependencies
/// </summary>
public class PackageMetricsCalculator
{
    /// <summary>
    /// Calculate all package metrics for the project
    /// Groups classes by namespace and computes inter-package dependencies
    /// </summary>
    public List<PackageMetrics> CalculateMetrics(List<ClassInfo> classes)
    {
        var result = new List<PackageMetrics>();

        // Group classes by namespace
        var packageGroups = classes
            .GroupBy(c => string.IsNullOrEmpty(c.Namespace) ? "(global)" : c.Namespace)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build a map of class name to class info for dependency resolution
        var classMap = new Dictionary<string, ClassInfo>();
        foreach (var cls in classes)
        {
            classMap[cls.FullName] = cls;
            if (!classMap.ContainsKey(cls.Name))
                classMap[cls.Name] = cls;
        }

        string? ResolveClass(string typeName)
        {
            if (classMap.TryGetValue(typeName, out var cls))
                return cls.FullName;

            // Try suffix match for namespace-qualified names
            var match = classes.FirstOrDefault(c => typeName.EndsWith("." + c.Name));
            return match?.FullName;
        }

        var classToPackage = new Dictionary<string, string>();
        foreach (var cls in classes)
        {
            classToPackage[cls.FullName] = string.IsNullOrEmpty(cls.Namespace) ? "(global)" : cls.Namespace;
        }

        // Build dependency pairs between classes (directed)
        var dependencyPairs = new HashSet<(string from, string to)>();
        foreach (var cls in classes)
        {
            var referenced = new HashSet<string>();

            foreach (var coupledType in cls.CoupledTypes)
            {
                var resolved = ResolveClass(coupledType);
                if (!string.IsNullOrEmpty(resolved))
                    referenced.Add(resolved);
            }

            foreach (var method in cls.Methods)
            {
                foreach (var usedType in method.UsedTypes)
                {
                    var resolved = ResolveClass(usedType);
                    if (!string.IsNullOrEmpty(resolved))
                        referenced.Add(resolved);
                }
            }

            if (!string.IsNullOrEmpty(cls.BaseClassName))
            {
                var resolved = ResolveClass(cls.BaseClassName);
                if (!string.IsNullOrEmpty(resolved))
                    referenced.Add(resolved);
            }

            foreach (var iface in cls.Interfaces)
            {
                var resolved = ResolveClass(iface);
                if (!string.IsNullOrEmpty(resolved))
                    referenced.Add(resolved);
            }

            foreach (var target in referenced)
            {
                if (target != cls.FullName)
                    dependencyPairs.Add((cls.FullName, target));
            }
        }

        // Initialize package metrics entries
        foreach (var (packageName, packageClasses) in packageGroups)
        {
            var metrics = new PackageMetrics
            {
                PackageName = packageName,
                ClassCount = packageClasses.Count,
                AbstractClassCount = packageClasses.Count(c => c.IsAbstract || c.IsInterface),
                Classes = packageClasses.Select(c => c.Name).ToList()
            };

            result.Add(metrics);
        }

        var outgoingPackages = new Dictionary<string, HashSet<string>>();
        var incomingPackages = new Dictionary<string, HashSet<string>>();
        var outgoingClasses = new Dictionary<string, HashSet<string>>();
        var incomingClasses = new Dictionary<string, HashSet<string>>();
        var spcPairs = new Dictionary<string, HashSet<(string from, string to)>>();
        var sccPairs = new Dictionary<string, HashSet<(string from, string to)>>();

        foreach (var (from, to) in dependencyPairs)
        {
            if (!classToPackage.TryGetValue(from, out var fromPackage) ||
                !classToPackage.TryGetValue(to, out var toPackage))
            {
                continue;
            }

            if (fromPackage == toPackage)
            {
                if (!spcPairs.TryGetValue(fromPackage, out var pairs))
                {
                    pairs = new HashSet<(string from, string to)>();
                    spcPairs[fromPackage] = pairs;
                }
                pairs.Add((from, to));
            }
            else
            {
                if (!sccPairs.TryGetValue(fromPackage, out var pairs))
                {
                    pairs = new HashSet<(string from, string to)>();
                    sccPairs[fromPackage] = pairs;
                }
                pairs.Add((from, to));

                if (!outgoingPackages.TryGetValue(fromPackage, out var outPkgs))
                {
                    outPkgs = new HashSet<string>();
                    outgoingPackages[fromPackage] = outPkgs;
                }
                outPkgs.Add(toPackage);

                if (!incomingPackages.TryGetValue(toPackage, out var inPkgs))
                {
                    inPkgs = new HashSet<string>();
                    incomingPackages[toPackage] = inPkgs;
                }
                inPkgs.Add(fromPackage);

                if (!outgoingClasses.TryGetValue(fromPackage, out var outClasses))
                {
                    outClasses = new HashSet<string>();
                    outgoingClasses[fromPackage] = outClasses;
                }
                outClasses.Add(from);

                if (!incomingClasses.TryGetValue(toPackage, out var inClasses))
                {
                    inClasses = new HashSet<string>();
                    incomingClasses[toPackage] = inClasses;
                }
                inClasses.Add(to);
            }
        }

        foreach (var metrics in result)
        {
            var packageName = metrics.PackageName;
            outgoingPackages.TryGetValue(packageName, out var outPkgs);
            incomingPackages.TryGetValue(packageName, out var inPkgs);
            outgoingClasses.TryGetValue(packageName, out var outClasses);
            incomingClasses.TryGetValue(packageName, out var inClasses);
            spcPairs.TryGetValue(packageName, out var spc);
            sccPairs.TryGetValue(packageName, out var scc);

            metrics.EfferentCoupling = outPkgs?.Count ?? 0;
            metrics.AfferentCoupling = inPkgs?.Count ?? 0;
            metrics.DependsOn = outPkgs?.ToList() ?? new List<string>();
            metrics.DependedOnBy = inPkgs?.ToList() ?? new List<string>();

            metrics.OutC = outClasses?.Count ?? 0;
            metrics.InC = inClasses?.Count ?? 0;
            metrics.HC = outClasses != null && inClasses != null
                ? outClasses.Intersect(inClasses).Count()
                : 0;
            metrics.SPC = spc?.Count ?? 0;
            metrics.SCC = scc?.Count ?? 0;

            // Calculate derived metrics
            CalculateDerivedMetrics(metrics);
        }

        return result;
    }
    
    /// <summary>
    /// Calculate Instability (I), Abstractness (A), and Distance from Main Sequence (D)
    /// </summary>
    private void CalculateDerivedMetrics(PackageMetrics metrics)
    {
        int ca = metrics.AfferentCoupling;
        int ce = metrics.EfferentCoupling;
        
        // Instability = Ce / (Ca + Ce)
        // Range: 0 (stable - depended upon) to 1 (unstable - depends on others)
        if (ca + ce > 0)
        {
            metrics.Instability = (double)ce / (ca + ce);
        }
        else
        {
            metrics.Instability = 0; // No dependencies = stable
        }
        
        // Abstractness = Na / Nc
        // Range: 0 (all concrete) to 1 (all abstract)
        if (metrics.ClassCount > 0)
        {
            metrics.Abstractness = (double)metrics.AbstractClassCount / metrics.ClassCount;
        }
        else
        {
            metrics.Abstractness = 0;
        }
        
        // Distance from Main Sequence = |A + I - 1|
        // Measures "balance" between abstractness and instability
        // 0 = on the main sequence (ideal zone)
        // Higher = either "zone of pain" (too concrete & stable) or "zone of uselessness" (too abstract & unstable)
        metrics.DistanceFromMainSequence = Math.Abs(metrics.Abstractness + metrics.Instability - 1);
    }
    
    /// <summary>
    /// Get a description of the package's position relative to the Main Sequence
    /// </summary>
    public static string GetPackageZoneDescription(PackageMetrics metrics)
    {
        double d = metrics.DistanceFromMainSequence;
        double a = metrics.Abstractness;
        double i = metrics.Instability;
        
        if (d <= 0.15)
        {
            return "Main Sequence (good balance)";
        }
        else if (a < 0.5 && i < 0.5)
        {
            return "Zone of Pain (concrete & stable - hard to change)";
        }
        else if (a > 0.5 && i > 0.5)
        {
            return "Zone of Uselessness (abstract & unstable)";
        }
        else if (d <= 0.3)
        {
            return "Near Main Sequence (acceptable)";
        }
        else
        {
            return "Far from Main Sequence (needs attention)";
        }
    }
    
    /// <summary>
    /// Get instability level description
    /// </summary>
    public static string GetInstabilityLevel(double instability)
    {
        return instability switch
        {
            <= 0.2 => "Very Stable",
            <= 0.4 => "Stable",
            <= 0.6 => "Moderate",
            <= 0.8 => "Unstable",
            _ => "Very Unstable"
        };
    }
}
