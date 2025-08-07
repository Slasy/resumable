using System.Text;

namespace ResumableFunctions;

public interface IStorage
{
    Task Save(string name, string content);
    Task<string> Load(string name);
    Task<bool> Delete(string name);
    IEnumerable<string> EnumerateFiles();
}

public sealed class Storage : IStorage
{
    private readonly string rootFolder;

    public Storage(string rootFolder)
    {
        this.rootFolder = Path.GetFullPath(rootFolder);
        Directory.CreateDirectory(this.rootFolder);
    }

    public Task Save(string name, string content)
    {
        return File.WriteAllTextAsync(Path.Join(rootFolder, name), content, Encoding.UTF8);
    }

    public Task<string> Load(string name)
    {
        return File.ReadAllTextAsync(Path.Join(rootFolder, name), Encoding.UTF8);
    }

    public Task<bool> Delete(string name)
    {
        if (!File.Exists(Path.Join(rootFolder, name)))
        {
            return Task.FromResult(false);
        }
        File.Delete(Path.Join(rootFolder, name));
        return Task.FromResult(true);
    }

    public IEnumerable<string> EnumerateFiles()
    {
        return Directory
            .EnumerateFiles(rootFolder, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OfType<string>();
    }
}
