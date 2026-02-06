namespace MetricsOOP.Models;

/// <summary>
/// Represents information about a class property
/// </summary>
public class PropertyInfo
{
    /// <summary>
    /// Property name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Property type name
    /// </summary>
    public string TypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Access modifier (public, private, protected, internal)
    /// </summary>
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Private;
    
    /// <summary>
    /// Whether the property has a getter
    /// </summary>
    public bool HasGetter { get; set; }
    
    /// <summary>
    /// Whether the property has a setter
    /// </summary>
    public bool HasSetter { get; set; }
    
    /// <summary>
    /// Whether the property is static
    /// </summary>
    public bool IsStatic { get; set; }
    
    /// <summary>
    /// Whether the property is virtual
    /// </summary>
    public bool IsVirtual { get; set; }
    
    /// <summary>
    /// Whether the property is override
    /// </summary>
    public bool IsOverride { get; set; }
    
    /// <summary>
    /// Whether this property is inherited from a base class
    /// </summary>
    public bool IsInherited { get; set; }
    
    /// <summary>
    /// Checks if the property is hidden (non-public)
    /// </summary>
    public bool IsHidden => AccessModifier == AccessModifier.Private || 
                            AccessModifier == AccessModifier.Protected;
}
