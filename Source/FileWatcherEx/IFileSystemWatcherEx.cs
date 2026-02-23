using System.ComponentModel;

namespace D2Phap.FileWatcherEx;

public interface IFileSystemWatcherEx
{
    /// <summary>
    /// Gets or sets the path of the directory to watch.
    /// </summary>
    string FolderPath { get; set; }

    /// <summary>
    /// Gets the collection of all the filters used to determine what files are monitored in a directory.
    /// </summary>
    System.Collections.ObjectModel.Collection<string> Filters { get; }

    /// <summary>
    /// Gets or sets the filter string used to determine what files are monitored in a directory.
    /// </summary>
    string Filter { get; set; }

    /// <summary>
    /// Gets or sets the type of changes to watch for.
    /// The default is the bitwise OR combination of
    /// <see cref="NotifyFilters.LastWrite"/>,
    /// <see cref="NotifyFilters.FileName"/>,
    /// and <see cref="NotifyFilters.DirectoryName"/>.
    /// </summary>
    NotifyFilters NotifyFilter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether subdirectories within the specified path should be monitored.
    /// </summary>
    bool IncludeSubdirectories { get; set; }

    /// <summary>
    /// Gets or sets the object used to marshal the event handler calls issued as a result of a directory change.
    /// </summary>
    ISynchronizeInvoke? SynchronizingObject { get; set; }

    /// <summary>
    /// Occurs when a file or directory in the specified
    /// <see cref="FileSystemWatcherEx.FolderPath"/> is changed.
    /// </summary>
    event FileSystemWatcherEx.DelegateOnChanged? OnChanged;

    /// <summary>
    /// Occurs when a file or directory in the specified
    /// <see cref="FileSystemWatcherEx.FolderPath"/> is deleted.
    /// </summary>
    event FileSystemWatcherEx.DelegateOnDeleted? OnDeleted;

    /// <summary>
    /// Occurs when a file or directory in the specified
    /// <see cref="FileSystemWatcherEx.FolderPath"/> is created.
    /// </summary>
    event FileSystemWatcherEx.DelegateOnCreated? OnCreated;

    /// <summary>
    /// Occurs when a file or directory in the specified
    /// <see cref="FileSystemWatcherEx.FolderPath"/> is renamed.
    /// </summary>
    event FileSystemWatcherEx.DelegateOnRenamed? OnRenamed;

    /// <summary>
    /// Occurs when the instance of <see cref="FileSystemWatcherEx"/> is unable to continue
    /// monitoring changes or when the internal buffer overflows.
    /// </summary>
    event FileSystemWatcherEx.DelegateOnError? OnError;

    /// <summary>
    /// Start watching files
    /// </summary>
    void Start();

    /// <summary>
    /// Stop watching files
    /// </summary>
    void Stop();

    /// <summary>
    /// Dispose the FileWatcherEx instance
    /// </summary>
    void Dispose();
}
