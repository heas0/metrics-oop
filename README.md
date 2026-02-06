# MetricsOOP

Консольное приложение для оценки показателей качества объектно-ориентированных приложений на C#.

## Описание

MetricsOOP анализирует исходный код C# и вычисляет метрики качества объектно-ориентированного проектирования. Программа использует Microsoft Roslyn для синтаксического и семантического анализа кода.

## Возможности

- Анализ одного файла или целой директории с рекурсивным поиском `.cs` файлов
- Поддержка частичных классов (partial classes) — автоматическое объединение метрик
- Вычисление метрик на уровне класса и на уровне проекта
- Цветная индикация проблемных значений в консоли
- Экспорт результатов в JSON формат
- Рекомендации по улучшению кода

## Установка

### Требования
- .NET 10.0 SDK или выше

### Сборка
```bash
git clone https://github.com/heas0/metrics-oop.git
cd metrics-oop/MetricsOOP
dotnet build
```

## Использование

```bash
# Анализ директории
dotnet run -- <путь> [опции]

# Примеры
dotnet run -- C:\MyProject\src              # Анализ всех .cs файлов в директории
dotnet run -- MyClass.cs -v                 # Анализ одного файла с детальным выводом
dotnet run -- . --output metrics.json -v    # Анализ текущей директории с экспортом в JSON
```

### Опции

| Опция | Описание |
|-------|----------|
| `-h, --help` | Показать справку |
| `-v, --verbose` | Детальный вывод с таблицей метрик по классам |
| `-o, --output <файл>` | Экспорт результатов в JSON файл |
| `-j, --json <файл>` | То же, что `--output` |

## Метрики

### CK Metrics (Chidamber & Kemerer)

Метрики на уровне класса, предложенные Чидамбером и Кемерером в 1994 году.

| Метрика | Описание | Рекомендуемое значение |
|---------|----------|------------------------|
| **WMC** | Weighted Methods per Class — сумма цикломатических сложностей всех методов класса | ≤ 20-50 |
| **DIT** | Depth of Inheritance Tree — глубина в дереве наследования | ≤ 5 |
| **NOC** | Number of Children — количество прямых наследников | Контекстно-зависимо |
| **CBO** | Coupling Between Objects — количество классов, с которыми связан данный класс | ≤ 14 |
| **RFC** | Response for a Class — количество методов + вызываемые методы | Контекстно-зависимо |
| **MPC** | Message Passing Coupling — количество внешних вызовов методов | Контекстно-зависимо |
| **LCOM** | Lack of Cohesion of Methods — недостаток связности методов (LCOM4) | = 1 (идеально) |
| **TCC** | Tight Class Cohesion — плотная связность класса (0-1) | ≥ 0.5 |
| **LCC** | Loose Class Cohesion — слабая связность класса (0-1) | ≥ 0.5 |

Примечание: CBO считается в двух вариантах — `CBO_total` (с наследованием) и `CBO_no_inheritance` (без базового класса и интерфейсов).  
DIT считается как длина пути до `System.Object`; для класса без явного базового класса DIT = 1.

### MOOD Metrics

Системные метрики, предложенные Fernando Brito e Abreu.

| Метрика | Описание | Рекомендуемое значение |
|---------|----------|------------------------|
| **MHF** | Method Hiding Factor — доля скрытых методов | 0.2 – 0.4 |
| **AHF** | Attribute Hiding Factor — доля скрытых атрибутов | ≈ 1.0 |
| **MIF** | Method Inheritance Factor — доля унаследованных методов | 0.2 – 0.8 |
| **AIF** | Attribute Inheritance Factor — доля унаследованных атрибутов | Контекстно-зависимо |
| **PF** | Polymorphism Factor — степень переопределения методов | Контекстно-зависимо |
| **CF** | Coupling Factor — фактор связанности между классами | ≤ 0.12 |

### Метрики размера

| Метрика | Описание |
|---------|----------|
| **LOC** | Lines of Code — общее количество строк |
| **SLOC** | Source Lines of Code — строки кода без комментариев и пустых |
| **NC** | Number of Classes — количество классов |
| **NM** | Number of Methods — количество объявленных методов |
| **NOM** | Number of Methods — методы + конструкторы + accessors |
| **NA** | Number of Attributes — количество полей |

#### NOM (Number of Methods) — правила подсчёта для C#
NOM считается как сумма:
1. `MethodDeclarationSyntax` (обычные методы)
2. `ConstructorDeclarationSyntax` (конструкторы)
3. Accessors свойств `get/set/init`
4. Accessors индексаторов `get/set/init`
5. Accessors событий `add/remove` (включая неявные `add/remove` для event field)

### Метрики сложности

| Метрика | Описание | Рекомендуемое значение |
|---------|----------|------------------------|
| **CC** | Cyclomatic Complexity — цикломатическая сложность McCabe | ≤ 10 |
| **CogC** | Cognitive Complexity — когнитивная сложность (SonarSource) | ≤ 15 |
| **MI** | Maintainability Index — индекс сопровождаемости (0-100) | ≥ 60 |

#### Метрики Холстеда

| Метрика | Формула | Описание |
|---------|---------|----------|
| η (Vocabulary) | η₁ + η₂ | Словарь программы |
| N (Length) | N₁ + N₂ | Длина программы |
| V (Volume) | N × log₂(η) | Объём |
| D (Difficulty) | (η₁/2) × (N₂/η₂) | Сложность |
| E (Effort) | D × V | Усилие |
| T (Time) | E / 18 | Время программирования (сек) |
| B (Bugs) | V / 3000 | Оценка количества ошибок |

### Метрики пакетов (Robert C. Martin)

Метрики для анализа зависимостей между пакетами (namespace).

| Метрика | Описание | Рекомендуемое значение |
|---------|----------|------------------------|
| **Ca** | Afferent Coupling — входящие зависимости | Контекстно-зависимо |
| **Ce** | Efferent Coupling — исходящие зависимости | Контекстно-зависимо |
| **I** | Instability — нестабильность = Ce/(Ca+Ce) | 0 (стабильный) – 1 (нестабильный) |
| **A** | Abstractness — абстрактность = Na/Nc | 0 (конкретный) – 1 (абстрактный) |
| **D** | Distance from Main Sequence — |A+I-1| | ≤ 0.3 |

#### Метрики архитектуры пакетов (NCP/OutC/InC/HC/SPC/SCC)
- **NCP** — число классов в пакете (namespace)
- **OutC** — число классов пакета, зависящих от классов других пакетов
- **InC** — число классов пакета, от которых зависят классы других пакетов
- **HC** — число классов пакета с одновременно входящими и исходящими межпакетными зависимостями
- **SPC** — число внутрипакетных зависимостей между классами (ориентированные связи)
- **SCC** — число межпакетных зависимостей из данного пакета в другие (ориентированные связи)

## Пример вывода

```
═══════════════════════════════════════════════════════════════════════════════
  OOP METRICS ANALYSIS REPORT - MyProject
  Generated: 2026-02-05 18:59:54
═══════════════════════════════════════════════════════════════════════════════

─── SUMMARY ───
  Files analyzed: 16
  Classes:        21
  Interfaces:     1
  Total methods:  92
  Total LOC:      3291

─── MOOD METRICS (System-Level) ───
  MHF (Method Hiding Factor)         : 0.385  (Encapsulation measure)
  AHF (Attribute Hiding Factor)      : 0.103
  CF (Coupling Factor)               : 0.074  (Should be ≤ 0.12)

─── AVERAGE CK METRICS ───
  Avg WMC                            : 16.59
  Avg DIT                            : 0.18
  Avg CBO                            : 3.05
  Avg LCOM                           : 1.86

─── CLASSES NEEDING ATTENTION ───
  ⚠ CodeAnalyzer:
      - Very high WMC (117) - consider splitting class
      - Complex method (CC=16)
```

## Архитектура

```
MetricsOOP/
├── Models/                 # Модели данных
│   ├── ClassInfo.cs           # Информация о классе
│   ├── ClassMetrics.cs        # Метрики класса
│   ├── MethodInfo.cs          # Информация о методе
│   ├── FieldInfo.cs           # Информация о поле
│   ├── HalsteadMetrics.cs     # Метрики Холстеда
│   ├── PackageMetrics.cs      # Метрики пакетов (Мартина)
│   └── ProjectMetrics.cs      # Метрики проекта
├── Parsers/                # Парсеры кода
│   └── CodeAnalyzer.cs        # Roslyn-анализатор C# кода
├── Metrics/                # Калькуляторы метрик
│   ├── CKMetricsCalculator.cs       # CK метрики + TCC/LCC
│   ├── MOODMetricsCalculator.cs     # MOOD метрики
│   ├── SizeMetricsCalculator.cs     # Метрики размера
│   ├── ComplexityMetricsCalculator.cs # Метрики сложности
│   └── PackageMetricsCalculator.cs  # Метрики пакетов
├── Reports/                # Генераторы отчётов
│   └── ReportGenerator.cs     # Консоль + JSON
└── Program.cs              # CLI точка входа
```

## Формулы

### Цикломатическая сложность McCabe
```
V(G) = E - N + 2P
```
Или упрощённо: `CC = количество_точек_решения + 1`

Точки решения: `if`, `else if`, `while`, `for`, `foreach`, `case`, `catch`, `&&`, `||`, `??`, `?:`

### Maintainability Index
```
MI = max(0, 100 × (171 - 5.2×ln(V) - 0.23×G - 16.2×ln(L) + 50×sin(√(2.4×C))) / 171)
```
Где: V — объём Холстеда, G — цикломатическая сложность, L — SLOC, C — процент комментариев

### LCOM4
Количество связных компонентов в графе, где методы соединены, если используют общие поля или вызывают друг друга.

### TCC и LCC
- **TCC** = NDC / NP — отношение напрямую связанных пар методов к общему числу пар
- **LCC** = (NDC + NIC) / NP — включает косвенно связанные пары через цепочки вызовов

### Cognitive Complexity
Алгоритм SonarSource учитывает:
- Базовые инкременты (+1): `if`, `for`, `while`, `catch`, `switch`, `?:`, `goto`
- Штрафы за вложенность: +1 за каждый уровень вложенности
- Булевы последовательности: `&&`/`||` считаются по группам в условиях (1 за последовательность)
- Рекурсия: +1 за рекурсивный вызов

## Технологии

- [.NET 10.0](https://dotnet.microsoft.com/)
- [Microsoft.CodeAnalysis.CSharp (Roslyn)](https://github.com/dotnet/roslyn) — парсинг и анализ C# кода
- [Newtonsoft.Json](https://www.newtonsoft.com/json) — сериализация в JSON

## Литература

1. Chidamber, S.R., Kemerer, C.F. (1994). *A Metrics Suite for Object Oriented Design*
2. Brito e Abreu, F. (1995). *The MOOD Metrics Set*
3. McCabe, T.J. (1976). *A Complexity Measure*
4. Halstead, M.H. (1977). *Elements of Software Science*

## Лицензия

MIT License
