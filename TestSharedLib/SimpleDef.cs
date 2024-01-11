using Define;

namespace TestSharedLib;

public class SimpleDef : IDef
{
    public string ID { get; set; } = null!;

    public string? Data;
}