namespace TestSample;

/// <summary>
/// Base class for shapes - demonstrates inheritance
/// </summary>
public abstract class Shape
{
    public string Name { get; set; } = string.Empty;
    public ConsoleColor Color { get; protected set; }
    
    public abstract double CalculateArea();
    public abstract double CalculatePerimeter();
    
    public virtual void Draw()
    {
        Console.WriteLine($"Drawing {Name}");
    }
}

/// <summary>
/// Circle class - demonstrates DIT=1, simple cohesion
/// </summary>
public class Circle : Shape
{
    private double _radius;
    
    public double Radius
    {
        get => _radius;
        set => _radius = value > 0 ? value : throw new ArgumentException("Radius must be positive");
    }
    
    public Circle(double radius)
    {
        _radius = radius;
        Name = "Circle";
        Color = ConsoleColor.Red;
    }
    
    public override double CalculateArea()
    {
        return Math.PI * _radius * _radius;
    }
    
    public override double CalculatePerimeter()
    {
        return 2 * Math.PI * _radius;
    }
    
    public override void Draw()
    {
        base.Draw();
        Console.WriteLine($"  Radius: {_radius}, Area: {CalculateArea():F2}");
    }
}

/// <summary>
/// Rectangle class - demonstrates polymorphism
/// </summary>
public class Rectangle : Shape
{
    private double _width;
    private double _height;
    
    public double Width
    {
        get => _width;
        set => _width = value > 0 ? value : throw new ArgumentException("Width must be positive");
    }
    
    public double Height
    {
        get => _height;
        set => _height = value > 0 ? value : throw new ArgumentException("Height must be positive");
    }
    
    public Rectangle(double width, double height)
    {
        _width = width;
        _height = height;
        Name = "Rectangle";
        Color = ConsoleColor.Blue;
    }
    
    public override double CalculateArea()
    {
        return _width * _height;
    }
    
    public override double CalculatePerimeter()
    {
        return 2 * (_width + _height);
    }
    
    public bool IsSquare()
    {
        return Math.Abs(_width - _height) < 0.001;
    }
}

/// <summary>
/// Square class - demonstrates DIT=2 (Shape -> Rectangle -> Square)
/// </summary>
public class Square : Rectangle
{
    public Square(double side) : base(side, side)
    {
        Name = "Square";
        Color = ConsoleColor.Green;
    }
    
    public double Side
    {
        get => Width;
        set
        {
            Width = value;
            Height = value;
        }
    }
}

/// <summary>
/// Interface for renderable objects
/// </summary>
public interface IRenderable
{
    void Render();
    RenderMode Mode { get; set; }
}

/// <summary>
/// Render mode enumeration
/// </summary>
public enum RenderMode
{
    Wireframe,
    Solid,
    Textured
}

/// <summary>
/// Complex class demonstrating high coupling and complexity
/// </summary>
public class ShapeRenderer : IRenderable
{
    private readonly List<Shape> _shapes;
    private readonly Dictionary<string, int> _renderCount;
    private ConsoleColor _backgroundColor;
    private bool _isInitialized;
    
    public RenderMode Mode { get; set; }
    
    public ShapeRenderer()
    {
        _shapes = new List<Shape>();
        _renderCount = new Dictionary<string, int>();
        _backgroundColor = ConsoleColor.Black;
        _isInitialized = false;
    }
    
    public void AddShape(Shape shape)
    {
        if (shape == null)
            throw new ArgumentNullException(nameof(shape));
        
        _shapes.Add(shape);
        
        if (!_renderCount.ContainsKey(shape.Name))
            _renderCount[shape.Name] = 0;
    }
    
    public void RemoveShape(Shape shape)
    {
        _shapes.Remove(shape);
    }
    
    public void Render()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
        
        Console.BackgroundColor = _backgroundColor;
        
        foreach (var shape in _shapes)
        {
            if (Mode == RenderMode.Wireframe)
            {
                RenderWireframe(shape);
            }
            else if (Mode == RenderMode.Solid)
            {
                RenderSolid(shape);
            }
            else if (Mode == RenderMode.Textured)
            {
                RenderTextured(shape);
            }
            
            _renderCount[shape.Name]++;
        }
        
        Console.ResetColor();
    }
    
    private void Initialize()
    {
        Console.Clear();
        _isInitialized = true;
    }
    
    private void RenderWireframe(Shape shape)
    {
        shape.Draw();
        Console.WriteLine($"  [Wireframe mode] Perimeter: {shape.CalculatePerimeter():F2}");
    }
    
    private void RenderSolid(Shape shape)
    {
        shape.Draw();
        Console.WriteLine($"  [Solid mode] Area: {shape.CalculateArea():F2}");
    }
    
    private void RenderTextured(Shape shape)
    {
        shape.Draw();
        Console.WriteLine($"  [Textured mode] Area: {shape.CalculateArea():F2}, Perimeter: {shape.CalculatePerimeter():F2}");
    }
    
    public int GetRenderCount(string shapeName)
    {
        return _renderCount.TryGetValue(shapeName, out var count) ? count : 0;
    }
    
    public double GetTotalArea()
    {
        double total = 0;
        foreach (var shape in _shapes)
        {
            total += shape.CalculateArea();
        }
        return total;
    }
    
    public List<Shape> GetShapesByType<T>() where T : Shape
    {
        var result = new List<Shape>();
        foreach (var shape in _shapes)
        {
            if (shape is T)
            {
                result.Add(shape);
            }
        }
        return result;
    }
}

/// <summary>
/// Utility class with low cohesion (demonstrates high LCOM)
/// </summary>
public class ShapeUtils
{
    private int _operationCount;
    private string _lastError;
    private DateTime _lastOperation;
    
    // Method using only _operationCount
    public void IncrementOperations()
    {
        _operationCount++;
    }
    
    // Method using only _lastError
    public void SetError(string error)
    {
        _lastError = error;
    }
    
    // Method using only _lastOperation
    public void UpdateTimestamp()
    {
        _lastOperation = DateTime.Now;
    }
    
    // Static method - not related to instance fields
    public static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
    
    // Static method
    public static double RadiansToDegrees(double radians)
    {
        return radians * 180 / Math.PI;
    }
    
    // Complex method with high cyclomatic complexity
    public string AnalyzeShape(Shape shape)
    {
        if (shape == null)
            return "Invalid shape";
        
        double area = shape.CalculateArea();
        double perimeter = shape.CalculatePerimeter();
        
        string sizeCategory;
        if (area < 10)
        {
            sizeCategory = "tiny";
        }
        else if (area < 50)
        {
            sizeCategory = "small";
        }
        else if (area < 100)
        {
            sizeCategory = "medium";
        }
        else if (area < 500)
        {
            sizeCategory = "large";
        }
        else
        {
            sizeCategory = "huge";
        }
        
        string perimeterRatio;
        double ratio = perimeter / area;
        if (ratio < 0.5)
        {
            perimeterRatio = "low";
        }
        else if (ratio < 1.0)
        {
            perimeterRatio = "moderate";
        }
        else if (ratio < 2.0)
        {
            perimeterRatio = "high";
        }
        else
        {
            perimeterRatio = "very high";
        }
        
        _operationCount++;
        _lastOperation = DateTime.Now;
        
        return $"{shape.Name}: {sizeCategory} size, {perimeterRatio} perimeter ratio";
    }
}
