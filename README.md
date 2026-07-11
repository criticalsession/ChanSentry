# ChanSentry

ChanSentry is a Node.js CLI for watching 4chan threads and downloading new media files as they appear.

## Requirements

- Node.js 18.17 or newer

No runtime npm dependencies are required.

## Usage

From the repository root:

```powershell
.\run.cmd
```

or:

```powershell
npm.cmd start
```

If your shell permits npm scripts directly, this also works:

```bash
npm start
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
