using System.Xml;
using Define;
using Define.Xml;
using FluentAssertions;

namespace TestSharedLib;

public abstract class DefTestBase : IDisposable
{
    protected readonly DefSerializeConfig Config = new DefSerializeConfig();
    protected readonly List<string> ErrorMessages = [];
    protected readonly List<string> WarningMessages = [];
    protected readonly DefDatabase DefDatabase;
    
    protected DefTestBase()
    {
        DefDatabase = new DefDatabase(Config);
        DefDatabase.Debug.OnWarning += OnWarning;
        DefDatabase.Debug.OnError += OnError;
    }

    private void OnWarning(string msg)
    {
        WarningMessages.Add(msg);
        Console.WriteLine($"Def.Warn: {msg}");
    }

    private void OnError(string msg, Exception? e, in XmlParseContext? _)
    {
        ErrorMessages.Add(msg);
        Console.WriteLine($"Def.Prs.Err: {msg}\nException: {e}");
    }
    
    protected virtual void PreLoad(DefDatabase db) {}

    protected void LoadDefFile(string file, bool expectErrors = false, bool expectWarnings = false)
    {
        string fullPath = $"./Defs/{file}.xml";

        string xml = File.ReadAllText(fullPath);
        var doc = new XmlDocument
        {
            PreserveWhitespace = true
        };
        doc.LoadXml(xml);

        PreLoad(DefDatabase);
        DefDatabase.AddDefDocument(doc, fullPath);
        
        DefDatabase.FinishLoading();
        
        if (expectErrors)
            ErrorMessages.Should().NotBeEmpty();
        else
            ErrorMessages.Should().BeEmpty();

        if (expectWarnings)
            WarningMessages.Should().NotBeEmpty();
        else
            WarningMessages.Should().BeEmpty();
    }

    protected T LoadSingleDef<T>(string file, bool expectErrors = false, bool expectWarnings = false) where T : class, IDef
    {
        LoadDefFile(file, expectErrors, expectWarnings);

        T? found = DefDatabase.GetAll<T>().FirstOrDefault();
        found.Should().NotBeNull();

        Console.WriteLine($"Loaded def '{found!.ID}' of type {found.GetType().FullName}");
        return found;
    }
    
    protected T? TryLoadSingleDef<T>(string file, bool expectErrors = false, bool expectWarnings = false) where T : class, IDef
    {
        LoadDefFile(file, expectErrors, expectWarnings);

        T? found = DefDatabase.GetAll<T>().FirstOrDefault();

        Console.WriteLine(found != null
            ? $"Loaded def '{found.ID}' of type {found.GetType().FullName}"
            : $"{file} failed to load...");
        return found;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        
        DefDatabase.Debug.OnWarning -= OnWarning;
        DefDatabase.Debug.OnError -= OnError;
    }
}
