using Spectre.Console;
using ChanSentry.Common;

namespace ChanSentry.CLI.Utils;

public static class CliPrint
{
    public static void PrintTitle()
    {
        AnsiConsole.MarkupLine("Welcome to [bold][green]Chan[/]Sentry[/]!");
        AnsiConsole.WriteLine();
    }

    public static void PrintGoodbye()
    {
        AnsiConsole.MarkupLine("[dim]Goodbye![/]");
        AnsiConsole.WriteLine();
    }

    public static string PrintMainMenu()
    {
        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .AddChoices(
                [
                    "Manage Watched Threads",
                    "Start Downloader",
                    "Exit"
                ]));

        switch (selection)
        {
            case "Manage Watched Threads":
                return Constants.Menus.ManageThreads;
            case "Start Downloader":
                return Constants.Menus.Downloader;
            case "Exit":
                return Constants.Menus.Exit;
        }

        throw new InvalidOperationException("Invalid main menu selection.");
    }

    public static string PrintManageMenu()
    {
        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .AddChoices(
                [
                    "Add Watched Thread",
                    "List/Delete Watched Threads",
                    "Back"
                ]));

        switch (selection)
        {
            case "Add Watched Thread":
                return Constants.Menus.AddThread;
            case "List/Delete Watched Threads":
                return Constants.Menus.ListDeleteThreads;
            case "Back":
                return Constants.Menus.Back;
        }

        throw new InvalidOperationException("Invalid manage threads selection.");
    }

    public static void PrintDownloaderMenu()
    {
        AnsiConsole.MarkupLine("Start Downloader");
        AnsiConsole.WriteLine();
    }
}
