# FileWatcherEx for Windows
A wrapper of `System.IO.FileSystemWatcher` to standardize the events and avoid false change notifications. It has been being used in [ImageGlass - A lightweight, versatile image viewer](https://github.com/d2phap/ImageGlass) project.

This project is based on the *VSCode FileWatcher*: https://github.com/Microsoft/vscode-filewatcher-windows

![Nuget](https://img.shields.io/nuget/dt/FileWatcherEx?color=%2300a8d6&logo=nuget)


## Resource links
- Nuget package: [https://www.nuget.org/packages/FileWatcherEx](https://www.nuget.org/packages/FileWatcherEx/)
- Project url: [https://github.com/d2phap/FileWatcherEx](https://github.com/d2phap/FileWatcherEx)
- Website: [https://imageglass.org](https://imageglass.org)

## Features
- Standardizes the events of `System.IO.FileSystemWatcher`.
- No false change notifications when a file system item is created, deleted, changed or renamed.
- Supports .NET 6.0, 7.0, 8.0, 9.0

## Installation
Run the command:

```bash
# Nuget package
Install-Package FileWatcherEx
```

## Usage
See Demo project for full details!

```cs
using FileWatcherEx;


var _fw = new FileSystemWatcherEx(@"C:\path\to\watch");

// event handlers
_fw.OnRenamed += FW_OnRenamed;
_fw.OnCreated += FW_OnCreated;
_fw.OnDeleted += FW_OnDeleted;
_fw.OnChanged += FW_OnChanged;
_fw.OnError += FW_OnError;

// thread-safe for event handlers
_fw.SynchronizingObject = this;

// start watching
_fw.Start();



void FW_OnRenamed(object sender, FileChangedEvent e)
{
  // do something here
}
...

```

## License
[MIT](LICENSE)

## Support this project
- [GitHub sponsor](https://github.com/sponsors/d2phap)
- [Patreon](https://www.patreon.com/d2phap)
- [PayPal](https://www.paypal.me/d2phap)

Thanks for your gratitude and finance help!
