using System;
using Spectre.Console;

namespace ChanSentry.Common.Helpers;

public class LogHelper
{
    public void LogInfo(string message)
    {
        AnsiConsole.MarkupLine($"[cyan]{message}[/]");
    }

    public void LogWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]{message}[/]");
    }

    public void LogError(string message)
    {
        AnsiConsole.MarkupLine($"[bold red]{message}[/]");
    }

    public void LogSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]{message}[/]");
    }
}
