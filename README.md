# ChanSentry

ChanSentry is a Node.js CLI for watching 4chan threads and downloading new media files as they appear.

## Requirements

- Node.js 18.17 or newer

No runtime npm dependencies are required.

## Web Dashboard

From the repository root:

```powershell
.\run.cmd
```

or:

```powershell
npm.cmd run web
```

The dashboard starts at:

```text
http://127.0.0.1:3131
```

## CLI

The original terminal CLI remains available:

```powershell
npm.cmd start
```

The app stores watched threads in `watched-threads.json` and downloads media to:

```text
downloads/<board>/<thread id> - <subject>/
```

Existing `watched-threads.json` files from the previous .NET version remain compatible.

## Test

From the repository root:

```powershell
.\test.cmd
```

or:

```powershell
npm.cmd test
```

PowerShell may block `npm.ps1` depending on execution policy. Use `npm.cmd` on Windows if that happens.
