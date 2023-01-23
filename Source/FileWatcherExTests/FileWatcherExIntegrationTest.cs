using System.Collections.Concurrent;
using FileWatcherEx;
using FileWatcherExTests.Helper;
using Xunit;
using Xunit.Abstractions;

namespace FileWatcherExTests;

/// <summary>
/// Integration/ Golden master test for FileWatcherEx
/// Note: the scenarios where recorded in C:\temp\fwtest
/// </summary>
public class FileWatcherExIntegrationTest : IDisposable
{
    private readonly ConcurrentQueue<FileChangedEvent> _events;
    private readonly FileSystemWatcherEx _fileWatcher;
    private readonly ReplayFileSystemWatcherFactory _replayFileSystemWatcherFactory;
    private readonly TempDir _tempDir;

    public FileWatcherExIntegrationTest(ITestOutputHelper testOutputHelper)
    {
        // setup before each test run
        _events = new ConcurrentQueue<FileChangedEvent>();
        _replayFileSystemWatcherFactory = new ReplayFileSystemWatcherFactory();
        
        _tempDir = new TempDir();
        _fileWatcher = new FileSystemWatcherEx(_tempDir.FullPath, testOutputHelper.WriteLine);
        _fileWatcher.FileSystemWatcherFactory = () => _replayFileSystemWatcherFactory.Create();
        _fileWatcher.IncludeSubdirectories = true;

        _fileWatcher.OnCreated += (_, ev) => _events.Enqueue(ev);
        _fileWatcher.OnDeleted += (_, ev) => _events.Enqueue(ev);
        _fileWatcher.OnChanged += (_, ev) => _events.Enqueue(ev);
        _fileWatcher.OnRenamed += (_, ev) => _events.Enqueue(ev);
    }

    
    [Fact]
    public void Create_Single_File()
    {
        StartFileWatcherAndReplay(@"scenario/create_file.csv");

        Assert.Single(_events);
        var ev = _events.First();
        Assert.Equal(ChangeType.CREATED, ev.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\a.txt", ev.FullPath);
        Assert.Equal("", ev.OldFullPath);
    }
    

    [Fact]
    public void Create_And_Remove_Single_File()
    {
        StartFileWatcherAndReplay(@"scenario/create_and_remove_file.csv");

        Assert.Equal(2, _events.Count);
        var ev1 = _events.ToList()[0];
        var ev2 = _events.ToList()[1];

        Assert.Equal(ChangeType.CREATED, ev1.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\a.txt", ev1.FullPath);
        Assert.Equal("", ev1.OldFullPath);

        Assert.Equal(ChangeType.DELETED, ev2.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\a.txt", ev2.FullPath);
        Assert.Equal("", ev2.OldFullPath);
    }


    [Fact]
    public void Create_Rename_And_Remove_Single_File()
    {
        StartFileWatcherAndReplay(@"scenario/create_rename_and_remove_file.csv");

        Assert.Equal(3, _events.Count);
        var ev1 = _events.ToList()[0];
        var ev2 = _events.ToList()[1];
        var ev3 = _events.ToList()[2];

        Assert.Equal(ChangeType.CREATED, ev1.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\a.txt", ev1.FullPath);

        Assert.Equal(ChangeType.RENAMED, ev2.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\b.txt", ev2.FullPath);
        AssertEqualNormalized(@"C:\temp\fwtest\a.txt", ev2.OldFullPath);

        Assert.Equal(ChangeType.DELETED, ev3.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\b.txt", ev3.FullPath);
        Assert.Equal("", ev3.OldFullPath);
    }

    
    [Fact]
    // filters out 2nd "changed" event
    public void Create_Single_File_Via_WSL2()
    {
        StartFileWatcherAndReplay(@"scenario/create_file_wsl2.csv");

        Assert.Single(_events);
        var ev = _events.First();
        Assert.Equal(ChangeType.CREATED, ev.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\a.txt", ev.FullPath);
        Assert.Equal("", ev.OldFullPath);
    }
    
    
    [Fact]
    // scenario creates "created" "changed" and "renamed" event.
    // resulting event is just "created" with the filename taken from "renamed"
    public void Create_And_Rename_Single_File_Via_WSL2()
    {
        StartFileWatcherAndReplay(@"scenario/create_and_rename_file_wsl2.csv");

        Assert.Single(_events);
        var ev = _events.First();
        Assert.Equal(ChangeType.CREATED, ev.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\b.txt", ev.FullPath);
        Assert.Null(ev.OldFullPath);
    }

    
    [Fact]
    public void Create_Rename_And_Remove_Single_File_Via_WSL2()
    {
        StartFileWatcherAndReplay(@"scenario/create_rename_and_remove_file_wsl2.csv");
        Assert.Empty(_events);
    }

    
    [Fact]
    public void Create_Rename_And_Remove_Single_File_With_Wait_Time_Via_WSL2()
    {
        StartFileWatcherAndReplay(@"scenario/create_rename_and_remove_file_with_wait_time_wsl2.csv");

        Assert.Equal(3, _events.Count);
        var ev1 = _events.ToList()[0];
        var ev2 = _events.ToList()[1];
        var ev3 = _events.ToList()[2];

        Assert.Equal(ChangeType.CREATED, ev1.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\a.txt", ev1.FullPath);

        Assert.Equal(ChangeType.RENAMED, ev2.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\b.txt", ev2.FullPath);
        AssertEqualNormalized(@"C:\temp\fwtest\a.txt", ev2.OldFullPath);

        Assert.Equal(ChangeType.DELETED, ev3.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\b.txt", ev3.FullPath);
        Assert.Equal("", ev3.OldFullPath);
    }
    
    
    [Fact]
    public void Manually_Create_And_Rename_File_Via_Windows_Explorer()
    {
        StartFileWatcherAndReplay(@"scenario/create_and_rename_file_via_explorer.csv");

        Assert.Equal(2, _events.Count);

        var ev1 = _events.ToList()[0];
        var ev2 = _events.ToList()[1];

        Assert.Equal(ChangeType.CREATED, ev1.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\New Text Document.txt", ev1.FullPath);

        Assert.Equal(ChangeType.RENAMED, ev2.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\foo.txt", ev2.FullPath);
        AssertEqualNormalized(@"C:\temp\fwtest\New Text Document.txt", ev2.OldFullPath);
    }

    [Fact]
    public void Manually_Create_Rename_And_Delete_File_Via_Windows_Explorer()
    {
        StartFileWatcherAndReplay(@"scenario/create_rename_and_delete_file_via_explorer.csv");

        Assert.Equal(3, _events.Count);
        var ev1 = _events.ToList()[0];
        var ev2 = _events.ToList()[1];
        var ev3 = _events.ToList()[2];

        Assert.Equal(ChangeType.CREATED, ev1.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\New Text Document.txt", ev1.FullPath);

        Assert.Equal(ChangeType.RENAMED, ev2.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\foo.txt", ev2.FullPath);
        AssertEqualNormalized(@"C:\temp\fwtest\New Text Document.txt", ev2.OldFullPath);

        Assert.Equal(ChangeType.DELETED, ev3.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\foo.txt", ev3.FullPath);
        Assert.Equal("", ev3.OldFullPath);
    }

    [Fact]
    public void Download_Image_Via_Edge_Browser()
    {
        StartFileWatcherAndReplay(@"scenario/download_image_via_Edge_browser.csv");

        Assert.Equal(2, _events.Count);
        var ev1 = _events.ToList()[0];
        var ev2 = _events.ToList()[1];

        Assert.Equal(ChangeType.CREATED, ev1.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\test.png.crdownload", ev1.FullPath);
        
        Assert.Equal(ChangeType.RENAMED, ev2.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\test.png", ev2.FullPath);
        AssertEqualNormalized(@"C:\temp\fwtest\test.png.crdownload", ev2.OldFullPath);
    }

    // instantly removed file is not in the events list
    [Fact]
    public void Create_Sub_Directory_Add_And_Remove_File()
    {
        StartFileWatcherAndReplay(@"scenario/create_subdirectory_add_and_remove_file.csv");

        Assert.Equal(2, _events.Count);
        var ev1 = _events.ToList()[0];
        var ev2 = _events.ToList()[1];
        
        Assert.Equal(ChangeType.CREATED, ev1.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\subdir", ev1.FullPath);
        
        Assert.Equal(ChangeType.CHANGED, ev2.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\subdir", ev2.FullPath);
        Assert.Equal(@"", ev2.OldFullPath);
    }

    [Fact]
    public void Create_Sub_Directory_Add_And_Remove_File_With_Sleep()
    {
        StartFileWatcherAndReplay(@"scenario/create_subdirectory_add_and_remove_file_with_sleep.csv");

        Assert.Equal(4, _events.Count);
        var ev1 = _events.ToList()[0];
        var ev2 = _events.ToList()[1];
        var ev3 = _events.ToList()[2];
        var ev4 = _events.ToList()[3];
        
        Assert.Equal(ChangeType.CREATED, ev1.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\subdir", ev1.FullPath);
        
        Assert.Equal(ChangeType.CREATED, ev2.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\subdir\a.txt", ev2.FullPath);
        Assert.Equal(@"", ev2.OldFullPath);

        // TODO this could be filtered out
        Assert.Equal(ChangeType.CHANGED, ev3.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\subdir", ev3.FullPath);
        Assert.Equal(@"", ev3.OldFullPath);

        Assert.Equal(ChangeType.DELETED, ev4.ChangeType);
        AssertEqualNormalized(@"C:\temp\fwtest\subdir\a.txt", ev4.FullPath);
        Assert.Equal(@"", ev4.OldFullPath);
    }

    [Fact]
    public void Filter_Settings_Are_Delegated()
    {
        using var dir = new TempDir();
        var watcher = new ReplayFileSystemWatcherWrapper();
        
        var uut = new FileSystemWatcherEx(dir.FullPath);
        uut.FileSystemWatcherFactory = () => watcher;
        uut.Filters.Add("*.foo");
        uut.Filters.Add("*.bar");
        
        uut.Start();
        Assert.Equal(new List<string>{"*.foo", "*.bar"}, watcher.Filters);
    }
    
    [Fact]
    public void Set_Filter()
    {
        using var dir = new TempDir();
        var watcher = new ReplayFileSystemWatcherWrapper();
        
        var uut = new FileSystemWatcherEx(dir.FullPath);
        uut.FileSystemWatcherFactory = () => watcher;
        
        // "all files" by default 
        Assert.Equal("*", uut.Filter);

        uut.Filters.Add("*.foo");
        uut.Filters.Add("*.bar");

        // two filter entries
        Assert.Equal(2, uut.Filters.Count);
        
        // if multiple filters, only first is displayed. TODO Why ? 
        Assert.Equal("*.foo", uut.Filter);

        uut.Filter = "*.baz";
        Assert.Equal("*.baz", uut.Filter);
        Assert.Single(uut.Filters);
    }

    
    
    [Fact(Skip = "requires real (Windows) file system")]
    public void Simple_Real_File_System_Test()
    {
        ConcurrentQueue<FileChangedEvent> events = new();
        var fw = new FileSystemWatcherEx(@"c:\temp\fwtest\");

        fw.OnCreated += (_, ev) => events.Enqueue(ev);
        fw.OnDeleted += (_, ev) => events.Enqueue(ev);
        fw.OnChanged += (_, ev) => events.Enqueue(ev);
        fw.OnRenamed += (_, ev) => events.Enqueue(ev);
        fw.OnRenamed += (_, ev) => events.Enqueue(ev);

        const string testFile = @"c:\temp\fwtest\b.txt";
        if (File.Exists(testFile))
        {
            File.Delete(testFile);
        }

        _fileWatcher.StartForTesting(
            p => FileAttributes.Normal, 
            p => Array.Empty<DirectoryInfo>());
        File.Create(testFile);
        Thread.Sleep(250);
        fw.Stop();

        Assert.Single(events);
        var ev = events.First();
        Assert.Equal(ChangeType.CREATED, ev.ChangeType);
        Assert.Equal(@"c:\temp\fwtest\b.txt", ev.FullPath);
        Assert.Equal("", ev.OldFullPath);
    }
    
    // cleanup
    public void Dispose()
    {
        _fileWatcher.Dispose();
        _tempDir.Dispose();
    }

    private void StartFileWatcherAndReplay(string csvFile)
    {
        _fileWatcher.StartForTesting(
            p => FileAttributes.Normal,
            // only used for FullName
            p => new[] { new DirectoryInfo(p)});
        _replayFileSystemWatcherFactory.RootWatcher.Replay(csvFile);
        _fileWatcher.Stop();
    }
    
    private class ReplayFileSystemWatcherFactory
    {
        private readonly List<ReplayFileSystemWatcherWrapper> _wrappers = new(); 

        public ReplayFileSystemWatcherWrapper Create()
        {
            var watcher = new ReplayFileSystemWatcherWrapper();
            _wrappers.Add(watcher);
            return watcher;
        }

        // At integration test, we're only interested in the root file watcher.
        // This is the one which is registered first and watches the root directory.
        public ReplayFileSystemWatcherWrapper RootWatcher => _wrappers[0];
    }
    
    // little hack to make the path comparision platform independent 
    private static void AssertEqualNormalized(string expected, string? actual)
    {
        actual = actual?.Replace("/", @"\");
        Assert.Equal(expected, actual);
    }
}
