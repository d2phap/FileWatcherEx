namespace FileWatcherExTests.Helper;

public class TempDir : IDisposable
{
    public string FullPath { get; }

    public TempDir() 
    {
        FullPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(FullPath);
    }

    public string CreateSubDir(string path)
    {
        var subDirPath = Path.Combine(FullPath, path);
        Directory.CreateDirectory(subDirPath);
        return subDirPath;
    }
    
    public string CreateSymlink(string target, params string[] symLink)
    {
        var allElements = new[] { FullPath }.Concat(symLink).ToArray();
        var symlinkPath = Path.Combine(allElements);
        Directory.CreateSymbolicLink(symlinkPath, target);
        return symlinkPath;
    }

    public void Dispose()
    {
        Directory.Delete(FullPath, true);
    }
}
