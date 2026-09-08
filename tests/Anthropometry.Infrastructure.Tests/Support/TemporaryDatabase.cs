namespace Anthropometry.Infrastructure.Tests.Support;

public sealed class TemporaryDatabase : IDisposable
{
    public TemporaryDatabase()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"anthropometry-{Guid.NewGuid():N}.db");
    }

    public string Path { get; }

    public void Dispose()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}
