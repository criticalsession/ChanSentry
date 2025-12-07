using ChanSentry.CLI;
using ChanSentry.CLI.Utils;
using Spectre.Console;
using ChanSentry.Common;

var currentMenu = Constants.Menus.Main;

while (true)
{
    AnsiConsole.Clear();

    CliPrint.PrintTitle();

    if (currentMenu == Constants.Menus.Main)
    {
        switch (CliPrint.PrintMainMenu())
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
        }
    }
    else if (currentMenu == Constants.Menus.ManageThreads)
    {
        switch (CliPrint.PrintManageMenu())
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
        }
    } 
    else if (currentMenu == Constants.Menus.Downloader)
    {
        CliPrint.PrintDownloaderMenu();
        Thread.Sleep(1000);
        currentMenu = Constants.Menus.Main;
    }
}