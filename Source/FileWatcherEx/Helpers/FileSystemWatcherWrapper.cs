using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FileWatcherEx.Helpers;

/// <summary>
/// Interface around .NET FileSystemWatcher to be able to replace it with a fake implementation
/// </summary>
public interface IFileSystemWatcherWrapper
{
    string Path { get; set; }

    Collection<string> Filters { get; }
    bool IncludeSubdirectories { get; set; }
    bool EnableRaisingEvents { get; set; }
    NotifyFilters NotifyFilter { get; set; }

    event FileSystemEventHandler Created;
    event FileSystemEventHandler Deleted;
    event FileSystemEventHandler Changed;
    event RenamedEventHandler Renamed;
    event ErrorEventHandler Error;

    int InternalBufferSize { get; set; }

    public ISynchronizeInvoke? SynchronizingObject { get; set; }

    void Dispose();
}

/// <summary>
/// Production implementation of  IFileSystemWrapper interface.
/// Backed by the existing FileSystemWatcher
/// </summary>
public class FileSystemWatcherWrapper : FileSystemWatcher, IFileSystemWatcherWrapper
{
    // intentionally empty
}

