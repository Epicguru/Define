using Define.Xml;

namespace Define.Tests;

public class ThrowExceptionsDef : IDef, IPostLoad, IConfigErrors, IPostXmlConstruct
{
    public static bool ThrowInConstructor;

    public string ID { get; set; } = null!;

    public string? SomeData;
    
    public ThrowExceptionsDef()
    {
        if (ThrowInConstructor)
            throw new Exception("Exception from constructor");
    }
    
    public void PostLoad()
    {
        throw new Exception("PostLoad exception!");
    }

    public void ConfigErrors(ConfigErrorReporter config)
    {
        config.Warn("This is a warning");
        config.Error("This is an error", new Exception("Some exception"));
        config.Assert(true);
        throw new Exception("ConfigErrors exception!");
    }

    public void PostXmlConstruct(in XmlParseContext context)
    {
        throw new Exception("PostXmlConstruct exception!");
    }
}