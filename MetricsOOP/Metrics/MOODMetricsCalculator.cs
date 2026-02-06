using MetricsOOP.Models;

namespace MetricsOOP.Metrics;

/// <summary>
/// Calculates MOOD (Metrics for Object-Oriented Design) metrics
/// These are system-wide metrics proposed by Fernando Brito e Abreu
/// </summary>
public class MOODMetricsCalculator
{
    /// <summary>
    /// Calculate all MOOD metrics for a project
    /// </summary>
    public void CalculateMetrics(List<ClassInfo> classes, ProjectMetrics metrics)
    {
        metrics.MHF = CalculateMHF(classes);
        metrics.AHF = CalculateAHF(classes);
        metrics.MIF = CalculateMIF(classes);
        metrics.AIF = CalculateAIF(classes);
        metrics.PF = CalculatePF(classes);
        metrics.CF = CalculateCF(classes);
    }

    /// <summary>
    /// MHF - Method Hiding Factor
    /// Ratio of hidden (non-public) methods to total methods
    /// Formula: MHF = Σ Mh(Ci) / Σ Md(Ci)
    /// Where: Mh = hidden methods, Md = defined methods
    /// Higher MHF = better encapsulation (0-1 scale)
    /// Recommended: 0.2 ≤ MHF ≤ 0.4
    /// </summary>
    public double CalculateMHF(List<ClassInfo> classes)
    {
        int totalMethods = 0;
        int hiddenMethods = 0;

        foreach (var cls in classes.Where(c => !c.IsInterface))
        {
            var definedMethods = cls.DefinedMethods.ToList();
            totalMethods += definedMethods.Count;
            hiddenMethods += definedMethods.Count(m => m.IsHidden);
        }

        if (totalMethods == 0)
            return 0;

        return (double)hiddenMethods / totalMethods;
    }

    /// <summary>
    /// AHF - Attribute Hiding Factor
    /// Ratio of hidden (non-public) attributes to total attributes
    /// Formula: AHF = Σ Ah(Ci) / Σ Ad(Ci)
    /// Where: Ah = hidden attributes, Ad = defined attributes
    /// AHF should ideally be 1.0 (all attributes hidden)
    /// Recommended: AHF close to 1.0
    /// </summary>
    public double CalculateAHF(List<ClassInfo> classes)
    {
        int totalAttributes = 0;
        int hiddenAttributes = 0;

        foreach (var cls in classes.Where(c => !c.IsInterface))
        {
            var definedFields = cls.DefinedFields.ToList();
            totalAttributes += definedFields.Count;
            hiddenAttributes += definedFields.Count(f => f.IsHidden);

            // Also count properties as attributes
            var definedProperties = cls.Properties.Where(p => !p.IsInherited).ToList();
            totalAttributes += definedProperties.Count;
            hiddenAttributes += definedProperties.Count(p => p.IsHidden);
        }

        if (totalAttributes == 0)
            return 1.0; // No attributes means perfect hiding

        return (double)hiddenAttributes / totalAttributes;
    }

    /// <summary>
    /// MIF - Method Inheritance Factor
    /// Ratio of inherited methods to total available methods
    /// Formula: MIF = Σ Mi(Ci) / Σ Ma(Ci)
    /// Where: Mi = inherited methods, Ma = available methods (inherited + defined)
    /// Measures reuse through inheritance
    /// Recommended: 0.2 ≤ MIF ≤ 0.8
    /// </summary>
    public double CalculateMIF(List<ClassInfo> classes)
    {
        int totalAvailable = 0;
        int totalInherited = 0;

        foreach (var cls in classes.Where(c => !c.IsInterface))
        {
            var allMethods = cls.Methods.Where(m => !m.IsStatic).ToList();
            var inheritedMethods = allMethods.Where(m => m.IsInherited).ToList();
            
            totalAvailable += allMethods.Count;
            totalInherited += inheritedMethods.Count;
        }

        if (totalAvailable == 0)
            return 0;

        return (double)totalInherited / totalAvailable;
    }

    /// <summary>
    /// AIF - Attribute Inheritance Factor
    /// Ratio of inherited attributes to total available attributes
    /// Formula: AIF = Σ Ai(Ci) / Σ Aa(Ci)
    /// Where: Ai = inherited attributes, Aa = available attributes
    /// Measures attribute reuse through inheritance
    /// </summary>
    public double CalculateAIF(List<ClassInfo> classes)
    {
        int totalAvailable = 0;
        int totalInherited = 0;

        foreach (var cls in classes.Where(c => !c.IsInterface))
        {
            var allFields = cls.Fields.Where(f => !f.IsStatic).ToList();
            var inheritedFields = allFields.Where(f => f.IsInherited).ToList();
            
            totalAvailable += allFields.Count;
            totalInherited += inheritedFields.Count;
        }

        if (totalAvailable == 0)
            return 0;

        return (double)totalInherited / totalAvailable;
    }

    /// <summary>
    /// PF - Polymorphism Factor
    /// Degree of method overriding (polymorphism usage)
    /// Formula: PF = Σ Mo(Ci) / Σ [Mn(Ci) × DC(Ci)]
    /// Where: Mo = overriding methods, Mn = new methods, DC = descendants count
    /// Higher PF indicates more polymorphism usage
    /// </summary>
    public double CalculatePF(List<ClassInfo> classes)
    {
        int totalOverriding = 0;
        int totalPotential = 0;

        foreach (var cls in classes.Where(c => !c.IsInterface))
        {
            // Count overriding methods in this class
            var overridingMethods = cls.DefinedMethods.Count(m => m.IsOverride);
            totalOverriding += overridingMethods;

            // Count new methods (non-override, non-inherited) × number of descendants
            var newMethods = cls.DefinedMethods.Count(m => !m.IsOverride && m.IsVirtual);
            var descendants = CountAllDescendants(cls, classes);
            totalPotential += newMethods * descendants;
        }

        if (totalPotential == 0)
            return 0;

        return (double)totalOverriding / totalPotential;
    }

    /// <summary>
    /// CF - Coupling Factor
    /// Inter-class coupling excluding inheritance relationships
    /// Formula: CF = Σ is_client(Ci, Cj) / (TC² - TC)
    /// Where: is_client = 1 if Ci uses Cj, TC = total classes
    /// Lower CF is better (less coupling)
    /// Recommended: CF ≤ 0.12
    /// </summary>
    public double CalculateCF(List<ClassInfo> classes)
    {
        var nonInterfaceClasses = classes.Where(c => !c.IsInterface).ToList();
        int tc = nonInterfaceClasses.Count;
        
        if (tc <= 1)
            return 0;

        int couplingCount = 0;
        var classNames = nonInterfaceClasses.Select(c => c.Name).ToHashSet();
        classNames.UnionWith(nonInterfaceClasses.Select(c => c.FullName));

        foreach (var cls in nonInterfaceClasses)
        {
            // Count unique classes that this class uses (excluding inheritance)
            var usedClasses = new HashSet<string>();
            
            foreach (var coupledType in cls.CoupledTypes)
            {
                // Only count if it's a project class and not a parent
                if (classNames.Contains(coupledType) && 
                    coupledType != cls.BaseClassName)
                {
                    usedClasses.Add(coupledType);
                }
            }

            // Also check method usages
            foreach (var method in cls.Methods)
            {
                foreach (var usedType in method.UsedTypes)
                {
                    if (classNames.Contains(usedType) && usedType != cls.BaseClassName)
                    {
                        usedClasses.Add(usedType);
                    }
                }
            }

            couplingCount += usedClasses.Count;
        }

        // Maximum possible couplings = TC * (TC - 1)
        int maxCouplings = tc * (tc - 1);
        
        return (double)couplingCount / maxCouplings;
    }

    /// <summary>
    /// Count all descendants (children, grandchildren, etc.) of a class
    /// </summary>
    private int CountAllDescendants(ClassInfo cls, List<ClassInfo> allClasses)
    {
        int count = 0;
        var directChildren = allClasses.Where(c => 
            c.BaseClassName == cls.Name || 
            c.BaseClassName == cls.FullName).ToList();

        foreach (var child in directChildren)
        {
            count++; // Direct child
            count += CountAllDescendants(child, allClasses); // Grandchildren
        }

        return count;
    }
}
