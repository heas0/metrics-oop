namespace MetricsOOP.Models;

/// <summary>
/// Represents information about a class field/attribute
/// </summary>
public class FieldInfo
{
    /// <summary>
    /// Field name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Field type name
    /// </summary>
    public string TypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Access modifier (public, private, protected, internal)
    /// </summary>
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Private;
    
    /// <summary>
    /// Whether the field is static
    /// </summary>
    public bool IsStatic { get; set; }
    
    /// <summary>
    /// Whether the field is readonly
    /// </summary>
    public bool IsReadOnly { get; set; }
    
    /// <summary>
    /// Whether the field is a constant
    /// </summary>
    public bool IsConst { get; set; }
    
    /// <summary>
    /// Whether this field is inherited from a base class
    /// </summary>
    public bool IsInherited { get; set; }
    
    /// <summary>
    /// Checks if the field is hidden (non-public)
    /// </summary>
    public bool IsHidden => AccessModifier == AccessModifier.Private || 
                            AccessModifier == AccessModifier.Protected;
}
