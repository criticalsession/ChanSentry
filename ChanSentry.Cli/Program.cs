using ChanSentry.Cli.Utils;
using Spectre.Console;
using ChanSentry.Common;
using ChanSentry.CLI.Handlers;

var currentMenu = Constants.Menus.Main;

while (true)
{
    AnsiConsole.Clear();

    CliPrint.PrintTitle();

    if (currentMenu == Constants.Menus.Main)
    {
        var selectedOption = CliPrint.PrintMainMenu();
        switch (selectedOption)
        {
            case Constants.Menus.ManageThreads:
                currentMenu = Constants.Menus.ManageThreads;
                break;
            case Constants.Menus.Downloader:
                currentMenu = Constants.Menus.Downloader;
                break;
            case Constants.Menus.Exit:
                CliPrint.PrintGoodbye();
                return;
            default:
                throw new InvalidOperationException($"Unexpected menu value: {selectedOption}");
        }
    }
    else if (currentMenu == Constants.Menus.ManageThreads)
    {
        var selectedOption = CliPrint.PrintManageMenu();
        switch (selectedOption)
        {
            case Constants.Menus.AddThread:
                AnsiConsole.MarkupLine("Add... (not implemented yet)");
                Thread.Sleep(1000);
                currentMenu = Constants.Menus.ManageThreads;
                break;
            case Constants.Menus.ListDeleteThreads:
                AnsiConsole.MarkupLine("List/delete... (not implemented yet)");
                Thread.Sleep(1000);
                currentMenu = Constants.Menus.ManageThreads;
                break;
            case Constants.Menus.Back:
                currentMenu = Constants.Menus.Main;
                continue;
            default:
                throw new InvalidOperationException($"Unexpected menu value: {selectedOption}");
        }
    } 
    else if (currentMenu == Constants.Menus.Downloader)
    {
        var downloader = new DownloadHandler();
        await downloader.StartAsync();
        currentMenu = Constants.Menus.Main;
    }
    else
    {
        AnsiConsole.MarkupLine($"[red]Error: Unknown menu state '{currentMenu}'[/]");
        currentMenu = Constants.Menus.Main;
        Thread.Sleep(2000);
    }
}