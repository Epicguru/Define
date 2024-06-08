using Define.Xml;
using JetBrains.Annotations;

namespace Define.Tests;

[UsedImplicitly(ImplicitUseKindFlags.Assign | ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
public class TestDef : IDef, IPostLoad, IConfigErrors, IPostXmlConstruct
{
    public bool PostLoadCalled { get; private set; }
    public bool LatePostLoadCalled { get; private set; }
    public bool ConfigErrorsCalled { get; private set; }
    public bool PostXmlConstructCalled { get; private set; }

    public string ID { get; set; } = null!;

    public InnerData Inner = new InnerData();
    public TestDef? DefRef;
    public IDef? DefRefInterface;
    [Alias("Object")]
    public object? ObjectWithDifferentName = new object();

    public Dictionary<string, InnerData>? Dict;
    public List<TestDef?>? List;
    public List<InnerData?>? InnerDataList;
    public float[] Array = [];
    public float[] ArrayWithExisting = [1, 2, 3];
    public List<float> ListWithExisting = [1, 2, 3];

    public void PostLoad()
    {
        PostLoadCalled = true;
    }

    public void LatePostLoad()
    {
        LatePostLoadCalled = true;
    }

    public void ConfigErrors(ConfigErrorReporter config)
    {
        ConfigErrorsCalled = true;
    }

    public void PostXmlConstruct(in XmlParseContext context)
    {
        PostXmlConstructCalled = true;
    }
}

[UsedImplicitly(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.Members)]
public class InnerData : IPostLoad, IConfigErrors, IPostXmlConstruct
{
    public bool PostLoadCalled { get; private set; }
    public bool LatePostLoadCalled { get; private set; }
    public bool ConfigErrorsCalled { get; private set; }
    public bool PostXmlConstructCalled { get; private set; }

    public string? SomeData;
    public int OtherData;

    public void PostLoad()
    {
        PostLoadCalled = true;
    }

    public void LatePostLoad()
    {
        LatePostLoadCalled = true;
    }

    public void ConfigErrors(ConfigErrorReporter config)
    {
        ConfigErrorsCalled = true;
    }

    public void PostXmlConstruct(in XmlParseContext context)
    {
        PostXmlConstructCalled = true;
    }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public class InnerSub : InnerData
{
    public string? InnerSubData;
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public class InnerGrandSub : InnerSub
{
    public int AnInt;
}

[UsedImplicitly(ImplicitUseKindFlags.Assign | ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithMembers)]
public class SubclassDef : TestDef
{
    public string? SubclassData;
}

[UsedImplicitly(ImplicitUseKindFlags.Assign | ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithMembers)]
public class AltSubclassAbstractDef : TestDef
{
    public string? SubclassData;
}

public class AltSubclassDef : AltSubclassAbstractDef;