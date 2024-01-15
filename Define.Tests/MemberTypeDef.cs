
using JetBrains.Annotations;
using System.Xml.Serialization;

namespace Define.Tests;

public sealed class MemberTypeDef : IDef
{
    [XmlIgnore]
    public string? Ignored;

    [Xml.XmlInclude, UsedImplicitly]
    private string? included;
    
    public static string? StaticField;
    public static string? StaticProperty { get; set; }
    
    public string ID { get; set; } = null!;
    public string? Property { get; set; }
    public string? PropertyNoSetter { get; }
    public string? PropertyNoGetter
    {
        set => FlagGotData(value);
    }
    
    public string? Field;
    [XmlIgnore]
    public bool DidWritePropertyNoGetter;

    private string? PropertyPrivate { get; set; }

    private void FlagGotData(string? _)
    {
        DidWritePropertyNoGetter = true;
    }

    public string? GetIncluded() => included;
}