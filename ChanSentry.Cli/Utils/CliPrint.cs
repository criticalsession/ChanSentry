using Spectre.Console;
using ChanSentry.Common;

namespace ChanSentry.Cli.Utils;

/// <summary>
/// Provides static methods for displaying formatted menus and messages in the command-line interface of the
/// application.
/// </summary>
public static class CliPrint
{
    /// <summary>
    /// Writes a formatted welcome title to the console using ANSI markup.
    /// </summary>
    public static void PrintTitle()
    {
        AnsiConsole.MarkupLine("Welcome to [bold][green]Chan[/]Sentry[/]!");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Writes a farewell message to the console using styled output.
    /// </summary>
    public static void PrintGoodbye()
    {
        AnsiConsole.MarkupLine("[dim]Goodbye![/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays the main menu prompt to the user and returns the selected menu action.
    /// </summary>
    /// <returns>A string representing the selected menu action. The value corresponds to one of the predefined menu constants
    /// for managing threads, starting the downloader, or exiting the application.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the user's selection does not match any of the available menu options.</exception>
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
            default:
                throw new InvalidOperationException($"Invalid main menu selection: {selection}");
        }

        throw new InvalidOperationException("Invalid main menu selection.");
    }

    /// <summary>
    /// Displays the manage threads menu and returns the identifier for the selected action.
    /// </summary>
    /// <returns>A string representing the selected menu action. Possible values correspond to adding a watched thread, listing
    /// or deleting watched threads, or returning to the previous menu.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the user's selection does not match any of the available menu options.</exception>
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
            default:
                throw new InvalidOperationException($"Invalid manage threads selection: {selection}");
        }
    }

    /// <summary>
    /// Displays the downloader view to the console.
    /// </summary>
    public static void PrintDownloaderMenu()
    {
        AnsiConsole.MarkupLine("Start Downloader");
        AnsiConsole.WriteLine();
    }
}
