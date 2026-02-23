using D2Phap.FileWatcherEx;
using D2Phap.FileWatcherEx.Helpers;
using Xunit;

namespace FileWatcherExTests;

public class EventNormalizerTest
{
    [Fact]
    public void No_Input_Gives_No_Output()
    {
        var events = NormalizeEvents(Array.Empty<FileChangedEvent>());
        Assert.Empty(events);
    }

    [Fact]
    public void Single_Event_Is_Passed_Through()
    {
        var events = NormalizeEvents(
            new FileChangedEvent()
            {
                ChangeType = ChangeType.CREATED,
                FullPath = @"c:\foo",
                OldFullPath = @"c:\bar"
            }
        );

        Assert.Single(events);
        var ev = events.First();
        Assert.Equal(ChangeType.CREATED, ev.ChangeType);
        Assert.Equal(@"c:\foo", ev.FullPath);
        Assert.Equal(@"c:\bar", ev.OldFullPath);
    }

    [Fact]
    public void On_Duplicate_Events_The_Latest_Is_Taken()
    {
        var events = NormalizeEvents(
            new FileChangedEvent
            {
                ChangeType = ChangeType.CREATED,
                FullPath = @"c:\foo",
                OldFullPath = null
            },
            new FileChangedEvent
            {
                ChangeType = ChangeType.RENAMED, // differs
                FullPath = @"c:\foo",
                OldFullPath = @"c:\bar" // differs as well
            }
        );

        Assert.Single(events);
        var ev = events.First();
        Assert.Equal(ChangeType.RENAMED, ev.ChangeType);
        Assert.Equal(@"c:\foo", ev.FullPath);
        Assert.Equal(@"c:\bar", ev.OldFullPath);
    }

    [Fact]
    public void On_Consecutive_Renaming_The_Events_Are_Merged()
    {
        var events = NormalizeEvents(
            new FileChangedEvent
            {
                ChangeType = ChangeType.RENAMED,
                FullPath = @"c:\foo",
                OldFullPath = @"c:\bar"
            },
            new FileChangedEvent
            {
                ChangeType = ChangeType.RENAMED,
                FullPath = @"c:\bazz",
                OldFullPath = @"c:\foo" // refers to previous renamedEvent1.FullPath
            }
        );
        Assert.Single(events);
        var ev = events.First();
        Assert.Equal(ChangeType.RENAMED, ev.ChangeType);
        Assert.Equal(@"c:\bazz", ev.FullPath);
        Assert.Equal(@"c:\bar", ev.OldFullPath);
    }

    [Fact]
    public void Rename_After_Create_Gives_Created_Event_With_Updated_Path()
    {
        var events = NormalizeEvents(
            new FileChangedEvent
            {
                ChangeType = ChangeType.CREATED,
                FullPath = @"c:\foo",
                OldFullPath = @"c:\bar"
            },
            new FileChangedEvent
            {
                ChangeType = ChangeType.RENAMED,
                FullPath = @"c:\bar",
                OldFullPath = @"c:\foo" // refers to previous createdEvent.FullPath
            }
        );

        Assert.Single(events);
        var ev = events.First();
        Assert.Equal(ChangeType.CREATED, ev.ChangeType);
        Assert.Equal(@"c:\bar", ev.FullPath);
        Assert.Null(ev.OldFullPath);
    }

    [Fact]
    // This is a complex case, originally extracted by using test coverage.
    // Scenario:
    // - file foo is created
    // - file bar is deleted
    // - now foo is renamed to the just deleted bar
    // - this results into a bar changed event
    public void Rename_After_Create_Gives_Changed_Event()
    {
        var events = NormalizeEvents(
            new FileChangedEvent
            {
                ChangeType = ChangeType.CREATED,
                FullPath = @"c:\foo",
                OldFullPath = null
            },
            new FileChangedEvent
            {
                ChangeType = ChangeType.DELETED,
                FullPath = @"c:\bar",
                OldFullPath = null
            },
            new FileChangedEvent
            {
                ChangeType = ChangeType.RENAMED,
                FullPath = @"c:\bar",
                OldFullPath = @"c:\foo"
            }
        );

        Assert.Single(events);
        var ev = events.First();
        Assert.Equal(ChangeType.CHANGED, ev.ChangeType);
        Assert.Equal(@"c:\bar", ev.FullPath);
        Assert.Null(ev.OldFullPath);
    }

    [Fact]
    public void Result_Suppressed_If_Delete_After_Create()
    {
        var events = NormalizeEvents(
            new FileChangedEvent
            {
                ChangeType = ChangeType.CREATED,
                FullPath = @"c:\foo",
                OldFullPath = null
            },
            new FileChangedEvent
            {
                ChangeType = ChangeType.DELETED,
                FullPath = @"c:\foo",
                OldFullPath = null
            }
        );

        Assert.Empty(events);
    }

    [Fact]
    public void Created_Event_After_Deleted_Results_Into_Changed()
    {
        var events = NormalizeEvents(
            new FileChangedEvent
            {
                ChangeType = ChangeType.DELETED,
                FullPath = @"c:\foo",
                OldFullPath = null
            },
            new FileChangedEvent
            {
                ChangeType = ChangeType.CREATED,
                FullPath = @"c:\foo",
                OldFullPath = null
            }
        );

        Assert.Single(events);
        var ev = events.First();
        Assert.Equal(ChangeType.CHANGED, ev.ChangeType);
        Assert.Equal(@"c:\foo", ev.FullPath);
        Assert.Null(ev.OldFullPath);
    }

    [Fact]
    public void Changed_Event_After_Created_Is_Ignored()
    {
        var events = NormalizeEvents(
            new FileChangedEvent
            {
                ChangeType = ChangeType.CREATED,
                FullPath = @"c:\foo",
                OldFullPath = null
            },
            new FileChangedEvent
            {
                ChangeType = ChangeType.CHANGED,
                FullPath = @"c:\foo",
                OldFullPath = null
            }
        );

        Assert.Single(events);
        var ev = events.First();
        Assert.Equal(ChangeType.CREATED, ev.ChangeType);
        Assert.Equal(@"c:\foo", ev.FullPath);
        Assert.Null(ev.OldFullPath);
    }

    [Fact]
    public void Filter_Passes_Events_Through()
    {
        var events = new List<FileChangedEvent>
        {
            new()
            {
                ChangeType = ChangeType.CREATED,
                FullPath = @"c:\foo",
                OldFullPath = null
            },
            new()
            {
                ChangeType = ChangeType.CHANGED,
                FullPath = @"c:\bar",
                OldFullPath = null
            },
            new()
            {
                ChangeType = ChangeType.DELETED,
                FullPath = @"c:\bazz",
                OldFullPath = null
            }
        };

        var filtered = EventNormalizer.FilterDeleted(events);
        Assert.Equal(events, filtered);
    }

    [Fact]
    public void Filter_Out_Deleted_Event_With_Subdirectory()
    {
        var events = new List<FileChangedEvent>
        {
            new()
            {
                ChangeType = ChangeType.DELETED,
                FullPath = @"c:\bar",
                OldFullPath = null
            },
            new()
            {
                ChangeType = ChangeType.CREATED,
                FullPath = @"c:\foo",
                OldFullPath = null
            },
            new()
            {
                ChangeType = ChangeType.DELETED,
                FullPath = @"c:\bar\sub",
                OldFullPath = null
            }
        };

        var filtered = EventNormalizer.FilterDeleted(events).ToList();
        Assert.Equal(2, filtered.Count);
        Assert.Equal(ChangeType.DELETED, filtered[0].ChangeType);
        Assert.Equal(@"c:\bar", filtered[0].FullPath);
        Assert.Equal(ChangeType.CREATED, filtered[1].ChangeType);
        Assert.Equal(@"c:\foo", filtered[1].FullPath);
    }

    [Theory]
    [InlineData(@"c:\a\b", @"c:", true)]
    [InlineData(@"c:\a\b", @"c:\a", true)]
    [InlineData(@"c:\a\b\", @"c:", true)]
    [InlineData(@"c:\a\b\", @"c:\a", true)]
    [InlineData(@"c:\a\b", @"c:\", true)]
    [InlineData(@"c:\a\b", @"c:\a\", true)]
    [InlineData(@"c:\", @"c:\foo", false)]
    [InlineData(@"c:\", @"c:\", false)]
    public void Is_Parent(string path, string candidatePath, bool expectedResult)
    {
        Assert.Equal(expectedResult, EventNormalizer.IsParent(path, candidatePath));
    }

    [Fact]
    public void Parent_Dir_Is_Detected()
    {
        var ev = new FileChangedEvent
        {
            ChangeType = ChangeType.DELETED,
            FullPath = @"c:\foo"
        };

        Assert.True(EventNormalizer.IsParent(ev, new List<string>()));
    }

    [Fact]
    public void Delete_Event_For_Subdirectory_Is_Detected()
    {
        var deletedFiles = new List<string>();

        var parentDirEvent = new FileChangedEvent
        {
            ChangeType = ChangeType.DELETED,
            FullPath = @"c:\foo"
        };

        Assert.True(EventNormalizer.IsParent(parentDirEvent, deletedFiles));


        var subDirEvent = new FileChangedEvent
        {
            ChangeType = ChangeType.DELETED,
            FullPath = @"c:\foo\bar"
        };

        Assert.False(EventNormalizer.IsParent(subDirEvent, deletedFiles));
    }

    private static List<FileChangedEvent> NormalizeEvents(params FileChangedEvent[] events)
    {
        return new EventNormalizer().Normalize(events).ToList();
    }
}