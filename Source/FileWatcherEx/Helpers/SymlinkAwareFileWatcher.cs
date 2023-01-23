/*---------------------------------------------------------
 * Copyright (C) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------*/

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FileWatcherEx.Helpers;

internal class SymlinkAwareFileWatcher : IDisposable
{
    private readonly string _watchPath;
    private readonly Action<FileChangedEvent>? _eventCallback;
    private readonly Action<ErrorEventArgs>? _onError;
    private Func<string, FileAttributes>? _getFileAttributesFunc;
    private Func<string, DirectoryInfo[]>? _getDirectoryInfosFunc;
    private readonly Func<IFileSystemWatcherWrapper> _watcherFactory;
    private readonly Action<string> _logger;


    internal Func<string, FileAttributes> GetFileAttributesFunc
    {
        get => _getFileAttributesFunc ?? File.GetAttributes;
        set => _getFileAttributesFunc = value;
    }

    internal Func<string, DirectoryInfo[]> GetDirectoryInfosFunc
    {
        get
        {
            DirectoryInfo[] DefaultFunc(string p) => new DirectoryInfo(p).GetDirectories();
            return _getDirectoryInfosFunc ?? DefaultFunc;
        }
        set => _getDirectoryInfosFunc = value;
    }

    internal Dictionary<string, IFileSystemWatcherWrapper> FileWatchers { get; } = new();

    // defaults from:
    // https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.notifyfilter?view=net-7.0#property-value
    public NotifyFilters NotifyFilter { get; set; } = NotifyFilters.LastWrite
                                                      | NotifyFilters.FileName
                                                      | NotifyFilters.DirectoryName;
    public bool EnableRaisingEvents { get; set; }
    
    public bool IncludeSubdirectories { get; set; }
    
    public Collection<string> Filters { get; } = new();

    public ISynchronizeInvoke? SynchronizingObject { get; set; }


    /// <summary>
    /// Create new instance of <see cref="FileSystemWatcherWrapper"/>.
    /// Object creation follows this order:
    /// <list type="table">
    ///   <item>1) create new instance</item>
    ///   <item>2) set properties (optional)</item>
    ///   <item>3) call init() (mandatory)</item>
    /// </list>
    /// </summary>
    /// <param name="path">Full folder path to watcher</param>
    /// <param name="onEvent">onEvent callback</param>
    /// <param name="onError">onError callback</param>
    /// <param name="watcherFactory">how to create a FileSystemWatcher</param>
    /// <param name="logger">logging callback</param>
    public SymlinkAwareFileWatcher(string path, Action<FileChangedEvent> onEvent, Action<ErrorEventArgs> onError,
        Func<IFileSystemWatcherWrapper> watcherFactory, Action<string> logger)
    {
        _watchPath = path;
        _eventCallback = onEvent;
        _onError = onError;
        _watcherFactory = watcherFactory;
        _logger = logger;
    }

    public void Init()
    {
        RegisterFileWatcher(_watchPath);
        RegisterAdditionalFileWatchersForSymLinkDirs(_watchPath);
    }
    
    private void RegisterFileWatcher(string path)
    {
        _logger($"Registering file watcher for {path}");
        var fileWatcher = _watcherFactory();
        SetFileWatcherProperties(fileWatcher, path);
        RegisterFileWatcherEventHandlers(fileWatcher);

        FileWatchers.Add(path, fileWatcher);
    }

    private void SetFileWatcherProperties(IFileSystemWatcherWrapper fileWatcher, string path)
    {
        fileWatcher.Path = path;
        fileWatcher.NotifyFilter = NotifyFilter;
        fileWatcher.IncludeSubdirectories = IncludeSubdirectories;
        fileWatcher.EnableRaisingEvents = EnableRaisingEvents;
        Filters.ToList().ForEach(filter => fileWatcher.Filters.Add(filter));

        // currently the sync object is only registered for the root file watcher.
        // this preserves the old behaviour
        if (IsRootPath(path))
        {
            fileWatcher.SynchronizingObject = SynchronizingObject;
        }

        //changing this to a higher value can lead into issues when watching UNC drives
        fileWatcher.InternalBufferSize = 32768;
    }

        
    private bool IsRootPath(string path)
    {
        return _watchPath == path;
    }

    private void RegisterFileWatcherEventHandlers(IFileSystemWatcherWrapper fileWatcher)
    {
        fileWatcher.Created += (_, e) => ProcessEvent(e, ChangeType.CREATED);
        fileWatcher.Changed += (_, e) => ProcessEvent(e, ChangeType.CHANGED);
        fileWatcher.Deleted += (_, e) => ProcessEvent(e, ChangeType.DELETED);
        fileWatcher.Renamed += (_, e) => ProcessRenamedEvent(e);
        fileWatcher.Error += (_, e) => _onError?.Invoke(e);

        // extra measures to handle symbolic link directories
        fileWatcher.Created += (_, e) => TryRegisterFileWatcherForSymbolicLinkDir(e.FullPath);
        fileWatcher.Deleted += UnregisterFileWatcherForSymbolicLinkDir;
    }
    
    /// <summary>
    /// Recursively find sym link dir and register them.
    /// Background: the native filewatcher does not follow symlinks so they need to be treated separately.
    /// </summary>
    private void RegisterAdditionalFileWatchersForSymLinkDirs(string path)
    {
        TryRegisterFileWatcherForSymbolicLinkDir(path);

        if (Directory.Exists(path))
        {
            foreach (var dirInfo in GetDirectoryInfosFunc(path))
            {
                RegisterAdditionalFileWatchersForSymLinkDirs(dirInfo.FullName);
            }
        }
    }


    /// <summary>
    /// Process event for type = [CHANGED; DELETED; CREATED]
    /// </summary>
    private void ProcessEvent(FileSystemEventArgs e, ChangeType changeType)
    {
        _eventCallback?.Invoke(new()
        {
            ChangeType = changeType,
            FullPath = e.FullPath,
        });
    }


    private void ProcessRenamedEvent(RenamedEventArgs e)
    {
        _eventCallback?.Invoke(new()
        {
            ChangeType = ChangeType.RENAMED,
            FullPath = e.FullPath,
            OldFullPath = e.OldFullPath,
        });
    }


    /// <summary>
    /// Safely register a file watcher for a symbolic link directory. Used at startup as well as callback on file creation.
    /// </summary>
    /// <param name="path"></param>
    internal void TryRegisterFileWatcherForSymbolicLinkDir(string path)
    {
        try
        {
            if (IsSymbolicLinkDirectory(path) && IncludeSubdirectories && !FileWatchers.ContainsKey(path))
            {
                _logger($"Directory {path} is a symbolic link dir. Will register additional file watcher.");
                RegisterFileWatcher(path);
            }
        }
        catch (Exception ex)
        {
            // IG Issue #405: throws exception on Windows 10
            // for "c:\users\user\application data" folder and sub-folders.
            _logger($"Error registering file system watcher for directory '{path}'. Error was: {ex.Message}");
        }
    }


    /// <summary>
    /// Cleanup filewatcher if a symbolic link dir is deleted
    /// </summary>
    internal void UnregisterFileWatcherForSymbolicLinkDir(object? _, FileSystemEventArgs e)
    {
        if (FileWatchers.ContainsKey(e.FullPath))
        {
            FileWatchers[e.FullPath].Dispose();
            FileWatchers.Remove(e.FullPath);
        }
    }


    private bool IsSymbolicLinkDirectory(string path)
    {
        var attrs = GetFileAttributesFunc(path);
        return attrs.HasFlag(FileAttributes.Directory)
               && attrs.HasFlag(FileAttributes.ReparsePoint);
    }

    // for testing
    internal List<IFileSystemWatcherWrapper> GetFileWatchers()
    {
        return FileWatchers.Values.ToList();
    }


    /// <summary>
    /// Stop raising events and Dispose all filewatchers 
    /// </summary>
    public void Dispose()
    {
        foreach (var watcher in FileWatchers.Select(pair => pair.Value))
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }
}