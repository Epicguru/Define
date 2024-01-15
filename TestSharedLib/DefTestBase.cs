using System.Xml;
using Define;
using Define.Xml;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace TestSharedLib;

/*
 * Tests needs to run sequentially. Normally multiple threads can work on defs
 * safely if each thread uses its own DefDatabase, which the tests do,
 * however the DefDebugger class and events are static and therefore events are
 * fired from multiple threads at once and so the tests that are listening for the events
 * end up failing.
 * This is a design oversight and in retrospect I would have but the debug events on the
 * def database itself instead.
 */
[Collection("SequentialDefTests")]
public abstract class DefTestBase : IDisposable
{
    protected readonly DefSerializeConfig Config = new DefSerializeConfig();
    protected readonly ITestOutputHelper Output;
    protected readonly List<string> ErrorMessages = new List<string>();
    protected readonly List<string> WarningMessages = new List<string>();
    protected readonly DefDatabase DefDatabase;
    
    protected DefTestBase(ITestOutputHelper output)
    {
        DefDatabase = new DefDatabase();
        Output = output;
        DefDebugger.OnWarning += OnWarning;
        DefDebugger.OnError += OnError;
    }

    private void OnWarning(string msg)
    {
        WarningMessages.Add(msg);
        Output.WriteLine($"Def.Warn: {msg}");
    }

    private void OnError(string msg, Exception? e, in XmlParseContext? _)
    {
        ErrorMessages.Add(msg);
        Output.WriteLine($"Def.Prs.Err: {msg}\nException: {e}");
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

        DefDatabase.StartLoading(Config);
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

        Output.WriteLine($"Loaded def '{found!.ID}' of type {found.GetType().FullName}");
        return found;
    }
    
    protected T? TryLoadSingleDef<T>(string file, bool expectErrors = false, bool expectWarnings = false) where T : class, IDef
    {
        LoadDefFile(file, expectErrors, expectWarnings);

        T? found = DefDatabase.GetAll<T>().FirstOrDefault();

        Output.WriteLine(found != null
            ? $"Loaded def '{found.ID}' of type {found.GetType().FullName}"
            : $"{file} failed to load...");
        return found;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        
        DefDebugger.OnWarning -= OnWarning;
        DefDebugger.OnError -= OnError;
    }
}
