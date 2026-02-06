# PDF Compliance Report (MetricsOOP)

## Source
- PDF: the only `.pdf` file in repo root (Cyrillic filename)
- Extraction: `markitdown` (local MCP tool) on 2026-02-05

## Metric Spec Table (from PDF, paraphrased)
| Metric | Definition (short) | C#-specific counting notes from PDF |
| --- | --- | --- |
| DIT | Depth of inheritance tree from class to root. | Base type chain ends at `System.Object`.
| NOC | Number of direct children. | Count subclasses of the class.
| NOM | Number of methods. | Count method declarations, constructors, property `get/set`, event `add/remove`.
| WMC | Weighted Methods per Class. | Weight can be `1` or a chosen complexity; must be documented.
| CC | Cyclomatic Complexity. | Standard `L - N + 2P` and decision points.
| Cognitive Complexity | Structural complexity by control flow + nesting. | `switch`, `catch`, `goto`, labeled `break/continue`, boolean sequences.
| CBO | Coupling Between Object classes. | Two versions: total and no-inheritance.
| CF | Coupling Factor. | `CF = Σ is_client(Ci, Cj) / (TC^2 - TC)`.
| MPC | Message Passing Coupling. | External method calls.
| LCOM | Lack of Cohesion of Methods. | Multiple variants listed (LCOM1/2/3/4/5).
| TCC | Tight Class Cohesion. | `TCC = NDC / NP`, `NP = N*(N-1)/2`.
| LCC | Loose Class Cohesion. | `LCC = (NDC + NIC) / NP`.
| NCP | Number of Classes in Package. | Package = module/assembly/namespace.
| OutC | Outgoing class coupling per package. | Count classes in package that depend on external packages.
| InC | Incoming class coupling per package. | Count classes in package depended upon by external packages.
| HC | Hub classes per package. | Classes with both incoming and outgoing inter-package deps.
| SPC | Stable Package Coupling. | Intra-package dependency count (directed).
| SCC | Stable Class Coupling. | Inter-package dependency count (directed).
| Ce | Efferent Coupling. | Number of external packages depended on.
| Ca | Afferent Coupling. | Number of external packages depending on the package.
| A | Abstractness. | `A = #abstract / #total`.
| I | Instability. | `I = Ce / (Ca + Ce)`.
| D | Distance from Main Sequence. | `D = |A + I - 1|`.

## Implementation Mapping and Status
- DIT: `MetricsOOP/Metrics/CKMetricsCalculator.cs` uses `System.Object` as root (DIT=1). Fixed base-type normalization.
- NOC: `MetricsOOP/Metrics/CKMetricsCalculator.cs` counts direct children (unchanged).
- NOM: `MetricsOOP/Parsers/CodeAnalyzer.cs` counts method declarations + constructors + property/indexer accessors + event accessors. `MetricsOOP/Metrics/SizeMetricsCalculator.cs` uses this as `NOM` and `TotalMethods`.
- WMC: `MetricsOOP/Metrics/CKMetricsCalculator.cs` sums Cyclomatic Complexity; choice documented in `README.md`.
- CC: `MetricsOOP/Parsers/CodeAnalyzer.cs` unchanged, still counts standard decision points.
- Cognitive Complexity: `MetricsOOP/Parsers/CodeAnalyzer.cs` updated for nesting + boolean sequences + `switch/catch/goto` per PDF guidance.
- CBO: `MetricsOOP/Metrics/CKMetricsCalculator.cs` now returns unique couplings (no double counting), with `CBO_total` and `CBO_no_inheritance`.
- CF: `MetricsOOP/Metrics/MOODMetricsCalculator.cs` implements formula (unchanged).
- MPC: `MetricsOOP/Metrics/CKMetricsCalculator.cs` now computes `MPC` from unique external calls captured by `CodeAnalyzer`.
- LCOM/TCC/LCC: `MetricsOOP/Metrics/CKMetricsCalculator.cs` keeps LCOM4 + LCOM-HS% and TCC/LCC formulas; PDF lists other variants, which are not implemented.
- Package metrics: `MetricsOOP/Metrics/PackageMetricsCalculator.cs` now computes `NCP`, `OutC`, `InC`, `HC`, `SPC`, `SCC`, plus `Ce/Ca/A/I/D`.
- Reporting/docs: `MetricsOOP/Reports/ReportGenerator.cs` and `README.md` updated to show new metrics and formulas.

## Mismatches Found and Fixed
- DIT root handling: `System.Object` now treated as depth 1.
- NOM rules: accessors (properties/indexers/events) now counted as methods.
- CBO uniqueness: mutual coupling no longer double-counted.
- Cognitive Complexity: boolean operator sequences, `switch`, `catch`, `goto` now counted per PDF cues.
- MPC: added class-level computation and reporting.
- Package metrics: added OutC/InC/HC/SPC/SCC and wiring.
- CBO variants: `CBO_total` and `CBO_no_inheritance` are both computed and reported.

## SonarQube Snippet Analysis
- Pre-fix: 0 issues in `CodeAnalyzer.cs`, `CKMetricsCalculator.cs`, `ComplexityMetricsCalculator.cs`, `MOODMetricsCalculator.cs`, `PackageMetricsCalculator.cs`, `ReportGenerator.cs`.
- Post-fix: 0 issues in the same files.

## Tests Added
- `MetricsOOP.Tests/MetricsMetricsTests.cs` covers DIT, NOM, CBO mutual coupling, MPC, cognitive complexity (`switch` + `goto`), and package metrics (OutC/InC/HC/SPC/SCC).

## Notes / Open Interpretations
- The PDF lists multiple LCOM variants; the implementation uses LCOM4 (plus LCOM-HS percentage) as the reported LCOM value.
- The PDF does not fully specify switch-case contribution to Cognitive Complexity; implementation counts `switch` but not individual `case` labels.
- WMC weight selection remains a documented choice; current implementation uses Cyclomatic Complexity.
