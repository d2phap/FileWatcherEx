using static FileWatcherEx.ChangeType;

namespace FileWatcherEx.Helpers;

/// <summary>
/// Tries to fix the real life oddities of the underlying FileSystemWatcher class.
/// The code here got refactored from the original Microsoft sources.
/// For real scenario, see  EventNormalizerTest.cs
/// </summary>
internal class EventNormalizer
{
    private readonly FileEventRepository _eventRepo = new();

    internal IEnumerable<FileChangedEvent> Normalize(FileChangedEvent[] events)
    {
        NormalizeDuplicates(events);
        return FilterDeleted(_eventRepo.Events());
    }

    private void NormalizeDuplicates(FileChangedEvent[] events)
    {
        foreach (var newEvent in events)
        {
            var oldEvent = _eventRepo.Find(newEvent.FullPath);
            // original file event from which we renamed, only applicable for RENAMED event
            var renameFromEvent = newEvent.ChangeType == RENAMED
                ? _eventRepo.Find(newEvent.OldFullPath)
                : null;

            switch (newEvent.ChangeType)
            {
                // CREATED followed by CHANGED => CREATED
                case CHANGED when oldEvent?.ChangeType == CREATED:
                    // Do nothing
                    break;

                // CREATED followed by DELETED => remove
                case DELETED when oldEvent?.ChangeType == CREATED:
                    _eventRepo.Remove(oldEvent);
                    break;

                // DELETED followed by CREATED => CHANGED
                case CREATED when oldEvent?.ChangeType == DELETED:
                    oldEvent.ChangeType = CHANGED;
                    break;

                // Scenario:
                // - file foo is created
                // - file bar is deleted
                // - now foo is renamed to the just deleted bar
                // - this results into a bar changed event
                case RENAMED when oldEvent?.ChangeType == DELETED && renameFromEvent?.ChangeType == CREATED:
                    newEvent.ChangeType = CHANGED;
                    newEvent.OldFullPath = null;
                    _eventRepo.AddOrUpdate(newEvent);

                    // Remove data about the CREATED file 
                    _eventRepo.Remove(renameFromEvent);
                    break;

                // rename from CREATED file, all other cases
                case RENAMED when renameFromEvent?.ChangeType == CREATED:
                    newEvent.ChangeType = CREATED;
                    newEvent.OldFullPath = null;
                    _eventRepo.AddOrUpdate(newEvent);

                    // Remove data about the CREATED file 
                    _eventRepo.Remove(renameFromEvent);
                    break;

                case RENAMED when renameFromEvent?.ChangeType == RENAMED:
                    newEvent.OldFullPath = renameFromEvent.OldFullPath;
                    _eventRepo.AddOrUpdate(newEvent);

                    // Remove data about the RENAMED file 
                    _eventRepo.Remove(renameFromEvent);
                    break;

                // the LOG event is not coming from the filesystem, hence it is ignored.
                // ideally, LOG would disappear completely but unfortunately it is part of the public API of this lib
                case LOG:
                    // ignore
                    break;

                default:
                    _eventRepo.AddOrUpdate(newEvent);
                    break;
            }
        }
    }

    // This algorithm will remove all DELETE events up to the root folder
    // that got deleted if any. This ensures that we are not producing
    // DELETE events for each file inside a folder that gets deleted.
    //
    // 1.) split ADD/CHANGE and DELETED events
    // 2.) sort short deleted paths to the top
    // 3.) for each DELETE, check if there is a deleted parent and ignore the event in that case
    internal static IEnumerable<FileChangedEvent> FilterDeleted(IEnumerable<FileChangedEvent> eventsWithoutDuplicates)
    {
        // Handle deletes
        var deletedPaths = new List<string>();
        return eventsWithoutDuplicates
            .Select((e, n) => new KeyValuePair<int, FileChangedEvent>(n, e)) // store original position value
            .OrderBy(e => e.Value.FullPath.Length) // shortest path first
            .Where(e => IsParent(e.Value, deletedPaths))
            .OrderBy(e => e.Key) // restore original position
            .Select(e => e.Value);
    }

    internal static bool IsParent(FileChangedEvent e, List<string> deletedPaths)
    {
        if (e.ChangeType == DELETED)
        {
            if (deletedPaths.Any(d => IsParent(e.FullPath, d)))
            {
                return false; // DELETE is ignored if parent is deleted already
            }

            // otherwise mark as deleted
            deletedPaths.Add(e.FullPath);
        }

        return true;
    }


    internal static bool IsParent(string path, string candidatePath)
    {
        // if exists, remove trailing "\" for both paths
        candidatePath = candidatePath.TrimEnd('\\');
        path = path.TrimEnd('\\');
        return path.StartsWith(candidatePath + '\\', StringComparison.Ordinal);
    }


    private class FileEventRepository
    {
        private readonly Dictionary<string, FileChangedEvent> _mapPathToEvents = [];

        public void AddOrUpdate(FileChangedEvent newEvent)
        {
            if (_mapPathToEvents.TryGetValue(newEvent.FullPath, out var oldEvent))
            {
                // update existing
                oldEvent.ChangeType = newEvent.ChangeType;
                oldEvent.OldFullPath = newEvent.OldFullPath;
            }
            else
            {
                // add
                _mapPathToEvents[newEvent.FullPath] = newEvent;
            }
        }

        public void Remove(FileChangedEvent ev)
        {
            _mapPathToEvents.Remove(ev.FullPath);
        }

        public FileChangedEvent? Find(string? path)
        {
            _mapPathToEvents.TryGetValue(path ?? "", out var oldEvent);
            return oldEvent;
        }

        public List<FileChangedEvent> Events()
        {
            return [.. _mapPathToEvents.Values];
        }
    }
}
