using MetricsOOP.Metrics;
using MetricsOOP.Models;
using MetricsOOP.Parsers;
using Xunit;

namespace MetricsOOP.Tests;

public class MetricsMetricsTests
{
    private static List<ClassInfo> AnalyzeClasses(string code)
    {
        var analyzer = new CodeAnalyzer();
        return analyzer.AnalyzeCode(code);
    }

    private static ClassMetrics AnalyzeClassMetrics(string code, string className)
    {
        var classes = AnalyzeClasses(code);
        var cls = classes.Single(c => c.Name == className);

        var metrics = new ClassMetrics
        {
            ClassName = cls.Name,
            FullName = cls.FullName
        };

        var ck = new CKMetricsCalculator(classes);
        var size = new SizeMetricsCalculator();
        var complexity = new ComplexityMetricsCalculator();

        ck.CalculateMetrics(cls, metrics);
        size.CalculateClassMetrics(cls, metrics);
        complexity.CalculateClassMetrics(cls, metrics);

        return metrics;
    }

    [Fact]
    public void DIT_UsesSystemObjectAsRoot()
    {
        var code = @"
namespace N {
    class A { }
    class B : A { }
    class C : B { }
}";
        var classes = AnalyzeClasses(code);
        var ck = new CKMetricsCalculator(classes);

        var a = classes.Single(c => c.Name == "A");
        var b = classes.Single(c => c.Name == "B");
        var c = classes.Single(c => c.Name == "C");

        Assert.Equal(1, ck.CalculateDIT(a));
        Assert.Equal(2, ck.CalculateDIT(b));
        Assert.Equal(3, ck.CalculateDIT(c));
    }

    [Fact]
    public void NOM_CountsMethodsConstructorsAccessorsAndEvents()
    {
        var code = @"
using System;
namespace N {
    public class NomSample
    {
        public NomSample() { }
        public int M() => 1;
        public int P { get; set; }
        public int this[int i] { get => i; set { } }
        public event EventHandler? E;
        public event EventHandler? F { add { } remove { } }
    }
}";
        var metrics = AnalyzeClassMetrics(code, "NomSample");
        Assert.Equal(10, metrics.NOM);
    }

    [Fact]
    public void CBO_DoesNotDoubleCountMutualCoupling()
    {
        var code = @"
namespace N {
    class A { private B _b = new B(); }
    class B { private A _a = new A(); }
}";
        var aMetrics = AnalyzeClassMetrics(code, "A");
        var bMetrics = AnalyzeClassMetrics(code, "B");

        Assert.Equal(1, aMetrics.CBO);
        Assert.Equal(1, bMetrics.CBO);
        Assert.Equal(1, aMetrics.CBONoInheritance);
        Assert.Equal(1, bMetrics.CBONoInheritance);
    }

    [Fact]
    public void MPC_CountsUniqueExternalCalls()
    {
        var code = @"
namespace N {
    class Helper { public void Do() { } }
    class Uses {
        private readonly Helper _h = new Helper();
        public void M() { _h.Do(); _h.Do(); }
    }
}";
        var metrics = AnalyzeClassMetrics(code, "Uses");
        Assert.Equal(1, metrics.MPC);
    }

    [Fact]
    public void CognitiveComplexity_AccountsForSequencesAndGoto()
    {
        var code = @"
using System;
namespace N {
    class C {
        public void M(int x, bool a, bool b, bool c) {
            if (a && b || c) { }
            switch (x) {
                case 1: break;
                default: goto case 1;
            }
            try { throw new Exception(); }
            catch { }
        }
    }
}";
        var classes = AnalyzeClasses(code);
        var cls = classes.Single(c => c.Name == "C");
        var method = cls.DefinedMethods.Single(m => m.Name == "M");

        Assert.Equal(6, method.CognitiveComplexity);
    }

    [Fact]
    public void PackageMetrics_ComputesOutInAndScc()
    {
        var code = @"
namespace N1 {
    using N2;
    public class A { private B _b = new B(); }
}
namespace N2 {
    public class B { }
}";
        var classes = AnalyzeClasses(code);
        var packageCalc = new PackageMetricsCalculator();
        var packages = packageCalc.CalculateMetrics(classes);

        var n1 = packages.Single(p => p.PackageName == "N1");
        var n2 = packages.Single(p => p.PackageName == "N2");

        Assert.Equal(1, n1.NCP);
        Assert.Equal(1, n2.NCP);

        Assert.Equal(1, n1.OutC);
        Assert.Equal(0, n1.InC);
        Assert.Equal(0, n1.HC);
        Assert.Equal(0, n1.SPC);
        Assert.Equal(1, n1.SCC);

        Assert.Equal(0, n2.OutC);
        Assert.Equal(1, n2.InC);
        Assert.Equal(0, n2.HC);
        Assert.Equal(0, n2.SPC);
        Assert.Equal(0, n2.SCC);

        Assert.Equal(1, n1.EfferentCoupling);
        Assert.Equal(1, n2.AfferentCoupling);
    }
}
