using Define;

namespace TestSharedLib;

public class SimpleDef : IDef
{
    public static string? StaticField;
    public static string? StaticProperty { get; set; } = "asd123";
    
    public string ID { get; set; } = null!;

    public string? Data;
    public SimpleDef? Ref;
    public SimpleDef? SelfRef;
}