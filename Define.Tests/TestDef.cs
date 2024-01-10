using Define.Xml;

namespace Define.Tests;

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

public class InnerSub : InnerData
{
    public string? InnerSubData;
}

public class InnerGrandSub : InnerSub
{
    public int AnInt;
}

public class SubclassDef : TestDef
{
    public string? SubclassData;
}

public class AltSubclassAbstractDef : TestDef
{
    public string? SubclassData;
}

public class AltSubclassDef : AltSubclassAbstractDef;