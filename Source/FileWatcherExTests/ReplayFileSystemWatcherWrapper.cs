using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CsvHelper;
using FileWatcherEx;

namespace FileWatcherExTests;

internal record EventRecordWithDiff(
    string Directory,
    string FileName,
    string EventName,
    string? OldFileName,
    long DiffInTicks,
    double DiffInMilliseconds
);

/// <summary>
/// Allows replaying of previously recorded file system events.
/// Used for integration testing.
/// </summary>
public class ReplayFileSystemWatcherWrapper : IFileSystemWatcherWrapper
{
    private Collection<string> _filters = new();

    public void Replay(string csvFile)
    {
        using var reader = new StreamReader(csvFile);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<EventRecordWithDiff>();
        foreach (var record in records)
        {
            // introduce time gap like originally recorded
            Thread.Sleep((int)record.DiffInMilliseconds);

            switch (record.EventName)
            {
                case "created":
                {
                    var ev = new FileSystemEventArgs(WatcherChangeTypes.Created, record.Directory, record.FileName);
                    Created?.Invoke(this, ev);
                    break;
                }
                case "deleted":
                {
                    var ev = new FileSystemEventArgs(WatcherChangeTypes.Deleted, record.Directory, record.FileName);
                    Deleted?.Invoke(this, ev);
                    break;
                }
                case "changed":
                {
                    var ev = new FileSystemEventArgs(WatcherChangeTypes.Changed, record.Directory, record.FileName);
                    Changed?.Invoke(this, ev);
                    break;
                }
                case "renamed":
                {
                    var ev = new RenamedEventArgs(WatcherChangeTypes.Renamed, record.Directory, record.FileName,
                        record.OldFileName);
                    Renamed?.Invoke(this, ev);
                    break;
                }
            }
        }
        // settle down
        Thread.Sleep(250);
    }

    public event FileSystemEventHandler? Created;
    public event FileSystemEventHandler? Deleted;
    public event FileSystemEventHandler? Changed;
    public event RenamedEventHandler? Renamed;

    #pragma warning disable CS8618 // unused in replay implementation
    public string Path { get; set; }

    public Collection<string> Filters => _filters;

    public bool IncludeSubdirectories { get; set; }
    public bool EnableRaisingEvents { get; set; }
    public NotifyFilters NotifyFilter { get; set; }

    #pragma warning disable CS0067 // unused in replay implementation
    public event ErrorEventHandler? Error;
    public int InternalBufferSize { get; set; }
    public ISynchronizeInvoke? SynchronizingObject { get; set; }

    public void Dispose()
    {
    }
}