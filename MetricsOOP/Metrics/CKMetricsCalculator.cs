using MetricsOOP.Models;

namespace MetricsOOP.Metrics;

/// <summary>
/// Calculates Chidamber & Kemerer (CK) metrics for classes
/// </summary>
public class CKMetricsCalculator
{
    private readonly List<ClassInfo> _allClasses;
    private readonly Dictionary<string, ClassInfo> _classMap;
    private readonly Dictionary<string, int> _ditCache = new();

    public CKMetricsCalculator(List<ClassInfo> classes)
    {
        _allClasses = classes;
        _classMap = classes.ToDictionary(c => c.FullName, c => c);
        
        // Also map by simple name for lookup
        foreach (var c in classes.Where(c => !_classMap.ContainsKey(c.Name)))
        {
            _classMap[c.Name] = c;
        }
    }

    /// <summary>
    /// Calculate all CK metrics for a class
    /// </summary>
    public void CalculateMetrics(ClassInfo classInfo, ClassMetrics metrics)
    {
        metrics.WMC = CalculateWMC(classInfo);
        metrics.DIT = CalculateDIT(classInfo);
        metrics.NOC = CalculateNOC(classInfo);
        (metrics.CBO, metrics.CBONoInheritance) = CalculateCBO(classInfo);
        metrics.RFC = CalculateRFC(classInfo);
        metrics.MPC = CalculateMPC(classInfo);
        (metrics.LCOM, metrics.LCOMPercent) = CalculateLCOM(classInfo);
        (metrics.TCC, metrics.LCC) = CalculateTCCandLCC(classInfo);
    }

    /// <summary>
    /// WMC - Weighted Methods per Class
    /// Sum of complexities of all methods (or simply method count when weight = 1)
    /// Higher WMC indicates:
    /// - More faults likely
    /// - Limited reusability
    /// - Higher maintenance effort
    /// Recommended: WMC ≤ 20-50
    /// </summary>
    public int CalculateWMC(ClassInfo classInfo)
    {
        // Using cyclomatic complexity as weight
        return classInfo.DefinedMethods.Sum(m => m.CyclomaticComplexity);
    }

    /// <summary>
    /// DIT - Depth of Inheritance Tree
    /// Maximum length of path from class to root (Object)
    /// Higher DIT:
    /// - More inherited methods = more complex behavior
    /// - Greater design complexity
    /// Recommended: DIT ≤ 5
    /// </summary>
    public int CalculateDIT(ClassInfo classInfo)
    {
        if (_ditCache.TryGetValue(classInfo.FullName, out var cachedDit))
            return cachedDit;

        if (string.IsNullOrEmpty(classInfo.BaseClassName))
        {
            _ditCache[classInfo.FullName] = 1; // Directly inherits System.Object
            return 1;
        }

        if (IsSystemObject(classInfo.BaseClassName))
        {
            _ditCache[classInfo.FullName] = 1;
            return 1;
        }

        // Find parent class
        var parent = FindClass(classInfo.BaseClassName);
        if (parent == null)
        {
            // Assume parent is from external library and inherits Object
            _ditCache[classInfo.FullName] = 2;
            return 2;
        }

        var dit = 1 + CalculateDIT(parent);
        _ditCache[classInfo.FullName] = dit;
        return dit;
    }

    /// <summary>
    /// NOC - Number of Children
    /// Number of immediate subclasses
    /// High NOC:
    /// - Indicates high reuse (good)
    /// - May indicate improper abstraction if too high
    /// - Parent class needs thorough testing
    /// </summary>
    public int CalculateNOC(ClassInfo classInfo)
    {
        return _allClasses.Count(c => 
            c.BaseClassName == classInfo.Name || 
            c.BaseClassName == classInfo.FullName);
    }

    /// <summary>
    /// CBO - Coupling Between Object Classes
    /// Number of classes this class is coupled to (uses or is used by)
    /// High CBO:
    /// - Poor modularity
    /// - Difficult reuse
    /// - More fault-prone
    /// Recommended: CBO ≤ 14
    /// Returns: (CBO_Total including inheritance, CBO_NoInheritance)
    /// </summary>
    public (int total, int noInheritance) CalculateCBO(ClassInfo classInfo)
    {
        var totalCouplings = new HashSet<string>();
        var noInheritanceCouplings = new HashSet<string>();

        string? ResolveCoupled(string name)
        {
            var cls = FindClass(name);
            return cls?.FullName;
        }

        // Forward coupling (uses)
        foreach (var coupledType in classInfo.CoupledTypes)
        {
            var resolved = ResolveCoupled(coupledType);
            if (!string.IsNullOrEmpty(resolved))
            {
                totalCouplings.Add(resolved);
                noInheritanceCouplings.Add(resolved);
            }
        }

        foreach (var method in classInfo.Methods)
        {
            foreach (var usedType in method.UsedTypes)
            {
                var resolved = ResolveCoupled(usedType);
                if (!string.IsNullOrEmpty(resolved))
                {
                    totalCouplings.Add(resolved);
                    noInheritanceCouplings.Add(resolved);
                }
            }
        }

        // Inheritance coupling (only for total)
        if (!string.IsNullOrEmpty(classInfo.BaseClassName))
        {
            var resolvedBase = ResolveCoupled(classInfo.BaseClassName);
            if (!string.IsNullOrEmpty(resolvedBase))
                totalCouplings.Add(resolvedBase);
        }

        foreach (var iface in classInfo.Interfaces)
        {
            var resolvedIface = ResolveCoupled(iface);
            if (!string.IsNullOrEmpty(resolvedIface))
                totalCouplings.Add(resolvedIface);
        }

        // Reverse coupling (other classes using this class)
        var nameVariants = new HashSet<string> { classInfo.Name, classInfo.FullName };
        foreach (var other in _allClasses)
        {
            if (other == classInfo)
                continue;

            bool uses = other.CoupledTypes.Any(t => nameVariants.Contains(t)) ||
                        other.Methods.Any(m => m.UsedTypes.Any(t => nameVariants.Contains(t)));

            bool inherits = nameVariants.Contains(other.BaseClassName ?? string.Empty) ||
                            other.Interfaces.Any(i => nameVariants.Contains(i));

            if (uses)
            {
                totalCouplings.Add(other.FullName);
                noInheritanceCouplings.Add(other.FullName);
            }

            if (inherits)
            {
                totalCouplings.Add(other.FullName);
            }
        }

        // Remove self-coupling if present
        totalCouplings.Remove(classInfo.Name);
        totalCouplings.Remove(classInfo.FullName);
        noInheritanceCouplings.Remove(classInfo.Name);
        noInheritanceCouplings.Remove(classInfo.FullName);

        return (totalCouplings.Count, noInheritanceCouplings.Count);
    }

    /// <summary>
    /// RFC - Response for a Class
    /// Number of methods in class + number of methods directly called
    /// High RFC:
    /// - Increased test complexity
    /// - More fault-prone
    /// - Harder to understand
    /// </summary>
    public int CalculateRFC(ClassInfo classInfo)
    {
        // Methods in class
        var methods = classInfo.DefinedMethods.Count();

        // Methods called from all methods (unique)
        var calledMethods = new HashSet<string>();
        foreach (var method in classInfo.Methods)
        {
            calledMethods.UnionWith(method.CalledMethods);
            calledMethods.UnionWith(method.ExternalMethodCalls);
        }

        return methods + calledMethods.Count;
    }

    /// <summary>
    /// MPC - Message Passing Coupling
    /// Number of external method calls made by this class
    /// </summary>
    public int CalculateMPC(ClassInfo classInfo)
    {
        var externalCalls = new HashSet<string>();
        foreach (var method in classInfo.Methods)
        {
            externalCalls.UnionWith(method.ExternalMethodCalls);
        }

        return externalCalls.Count;
    }

    /// <summary>
    /// LCOM - Lack of Cohesion of Methods (LCOM4)
    /// Number of connected components in method-field graph
    /// Methods are connected if they share fields or call each other
    /// LCOM4 = 1: Ideal (cohesive class)
    /// LCOM4 ≥ 2: Class should potentially be split
    /// Returns (LCOM4, percentage)
    /// </summary>
    public (int lcom4, double lcomPercent) CalculateLCOM(ClassInfo classInfo)
    {
        var methods = classInfo.DefinedMethods.ToList();
        if (methods.Count == 0)
            return (0, 0);

        var fields = classInfo.DefinedFields.ToList();
        if (fields.Count == 0 && methods.Count <= 1)
            return (methods.Count, 0);

        // Build adjacency graph
        var methodNames = methods.Select(m => m.Name).ToList();
        var fieldNames = fields.Select(f => f.Name).ToHashSet();
        
        // Union-Find for connected components
        var parent = new Dictionary<string, string>();
        foreach (var m in methodNames)
            parent[m] = m;

        string Find(string x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }

        void Union(string x, string y)
        {
            var px = Find(x);
            var py = Find(y);
            if (px != py)
                parent[px] = py;
        }

        // Connect methods that share fields
        for (int i = 0; i < methods.Count; i++)
        {
            for (int j = i + 1; j < methods.Count; j++)
            {
                var sharedFields = methods[i].AccessedFields.Intersect(methods[j].AccessedFields);
                if (sharedFields.Any())
                {
                    Union(methodNames[i], methodNames[j]);
                }
            }
        }

        // Connect methods that call each other
        foreach (var method in methods)
        {
            foreach (var calledMethod in method.CalledMethods)
            {
                if (methodNames.Contains(calledMethod))
                {
                    Union(method.Name, calledMethod);
                }
            }
        }

        // Count connected components
        var lcom4 = methodNames.Select(m => Find(m)).Distinct().Count();

        // Calculate LCOM percentage (Henderson-Sellers variant for comparison)
        // LCOM-HS = (m - sum(mA)/a) / (m - 1) where m=methods, a=attributes, mA=methods accessing attribute A
        double lcomPercent = 0;
        if (fields.Count > 0 && methods.Count > 1)
        {
            double sumMA = 0;
            foreach (var field in fields)
            {
                sumMA += methods.Count(m => m.AccessedFields.Contains(field.Name));
            }
            lcomPercent = (methods.Count - sumMA / fields.Count) / (methods.Count - 1);
            lcomPercent = Math.Max(0, Math.Min(1, lcomPercent)) * 100;
        }

        return (lcom4, lcomPercent);
    }

    /// <summary>
    /// TCC - Tight Class Cohesion and LCC - Loose Class Cohesion
    /// TCC = NDC / NP where NDC = directly connected pairs, NP = total possible pairs
    /// LCC = (NDC + NIC) / NP where NIC = indirectly connected pairs
    /// Methods are "directly connected" if they access the same field
    /// Methods are "indirectly connected" if connected through call chain
    /// TCC = 1.0 is ideal (all methods are cohesive)
    /// Returns (TCC, LCC) both in range 0-1
    /// </summary>
    public (double tcc, double lcc) CalculateTCCandLCC(ClassInfo classInfo)
    {
        var methods = classInfo.DefinedMethods.ToList();
        
        if (methods.Count <= 1)
            return (1.0, 1.0); // Single method or no methods = perfectly cohesive
        
        int n = methods.Count;
        int np = n * (n - 1) / 2; // Total possible pairs
        
        if (np == 0)
            return (1.0, 1.0);
        
        // Build direct connection matrix (methods sharing fields)
        var directlyConnected = new bool[n, n];
        int directPairs = 0;
        
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                // Check if methods share any field access
                var sharedFields = methods[i].AccessedFields.Intersect(methods[j].AccessedFields);
                if (sharedFields.Any())
                {
                    directlyConnected[i, j] = true;
                    directlyConnected[j, i] = true;
                    directPairs++;
                }
            }
        }
        
        // Build method call graph (for indirect connections)
        // Use index-based lookup since method names may be duplicated (overloads)
        var methodNameToIndices = new Dictionary<string, List<int>>();
        for (int i = 0; i < n; i++)
        {
            var name = methods[i].Name;
            if (!methodNameToIndices.ContainsKey(name))
                methodNameToIndices[name] = new List<int>();
            methodNameToIndices[name].Add(i);
        }
        
        var callsTo = new bool[n, n];
        for (int i = 0; i < n; i++)
        {
            foreach (var calledMethod in methods[i].CalledMethods)
            {
                if (methodNameToIndices.TryGetValue(calledMethod, out var indices))
                {
                    foreach (var j in indices)
                    {
                        if (i != j)
                        {
                            callsTo[i, j] = true;
                            callsTo[j, i] = true; // Bidirectional for cohesion purposes
                        }
                    }
                }
            }
        }
        
        // Calculate indirect connections using transitive closure
        // Union of direct field sharing and method calls
        var connected = new bool[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                connected[i, j] = directlyConnected[i, j] || callsTo[i, j];
            }
        }
        
        // Floyd-Warshall for transitive closure
        for (int k = 0; k < n; k++)
        {
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (connected[i, k] && connected[k, j])
                        connected[i, j] = true;
                }
            }
        }
        
        // Count indirectly connected pairs (includes direct pairs)
        int indirectPairs = 0;
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                if (connected[i, j])
                    indirectPairs++;
            }
        }
        
        double tcc = (double)directPairs / np;
        double lcc = (double)indirectPairs / np;
        
        return (tcc, lcc);
    }

    private ClassInfo? FindClass(string name)
    {
        if (_classMap.TryGetValue(name, out var cls))
            return cls;
        
        // Try partial match
        return _allClasses.FirstOrDefault(c => 
            c.Name == name || 
            c.FullName.EndsWith("." + name));
    }

    private static bool IsSystemObject(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return name == "object" ||
               name == "Object" ||
               name == "System.Object" ||
               name == "global::System.Object";
    }
}
