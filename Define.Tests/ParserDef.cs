using System.Numerics;
using JetBrains.Annotations;

namespace Define.Tests;

public class ParserDef : IDef
{
    public delegate float MethodWithOut(float f, out string str);
    public delegate T DoMath<T>(in T a, ref T b) where T : INumber<T>;
    
    public string ID { get; set; } = null!;
    
    // Delegate:
    public Action? Action;
    public Func<string, bool, int>? SimpleFunc;
    public Func<string, bool, float, int>? SimpleFunc2;
    public MethodWithOut? MethodWithOutArg;
    public DoMath<float>? MethodDoMath;
    
    // Type:
    public List<Type?> Types = [];
}

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithMembers)]
public static class ExampleMethods
{
    public static void SimpleAction() { }
    
    public static int SimpleFunc(string a, bool b) => 5;
    
    public static int SimpleFunc(string a, bool b, float f) => 12;
    
    public static int SimpleFunc(string a, bool b, int f) => 11;

    public static float GenericMath(in float a, ref float b)
    {
        return a * b;
    }

    public static float MethodWithOut(float f, out string str)
    {
        str = "Hello";
        return 123.4f;
    }
}
