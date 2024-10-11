// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using EtsyStats.Helpers;
using EtsyStats.Models;
using EtsyStats.Models.Options;
using EtsyStats.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenQA.Selenium;

namespace EtsyStats;

public static class Program
{
    public static async Task Main()
    {
        try
        {
            var exit = false;

            Startup.SetupLogs();

            // TODO var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var environmentName = "Production";

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, false)
                .AddJsonFile("appsettings.secrets.json", true, false)
                .AddJsonFile($"appsettings.{environmentName}.json", true, false)
                .AddJsonFile($"appsettings.{environmentName}.secrets.json", true, false)
                .AddEnvironmentVariables();

            var configuration = builder.Build();

            var applicationOptions = configuration.GetSection(ApplicationOptions.SectionName).Get<ApplicationOptions>();
            // TODO Use Configuration Options instead of Settings
            // var configurationOptions = configuration.GetSection(ConfigurationOptions.SectionName).Get<ConfigurationOptions>();
            var googleChromeOptions = configuration.GetSection(GoogleChromeOptions.SectionName).Get<GoogleChromeOptions>();
            var googleSheetsOptions = configuration.GetSection(GoogleSheetsOptions.SectionName).Get<GoogleSheetsOptions>();

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            var config = FirstTimeLaunchConfigure(applicationOptions!);
            Console.WriteLine($"Shop: {config.ShopName}");

            var etsyDataUploadService = new EtsyDataUploadService(config);
            var directoryHelper = new DirectoryHelper(); // TODO ann DI
            var etsyParser = new EtsyParser(directoryHelper);
            do
            {
                try
                {
                    Console.WriteLine(
                        "\nPlease select an action that you would like to do:" +
                        "\n1 - Upload listings stats");
                    // "\n0 - Exit the program");
                    var command = Console.ReadLine();
                    switch (command)
                    {
                        case "1":
                        {
                            var dateRange = ProgramHelper.GetDateRangeStats();
                            
                            await Log.InfoAndConsole("Getting listings stats from Etsy...\n");
                            var data = await etsyParser.GetListingsStats(dateRange);

                            await Log.InfoAndConsole("Writing data to Google Sheets...");
                            await Log.Info($"Program googleSheetsOptions sheetId: {(string.IsNullOrWhiteSpace(googleSheetsOptions!.SheetId) ? "is NULL" : "is NOT NULL")}");
                            await etsyDataUploadService.WriteListingsStatsToGoogleSheet(googleSheetsOptions.SheetId, data);

                            await Log.InfoAndConsole("Listings were uploaded successfully.");
                            break;
                        }
                        case "0":
                            exit = true;
                            break;
                    }
                }
                catch (InvalidOperationException e)
                {
                    if (e.Message.Contains("This version of ChromeDriver only supports Chrome version"))
                    {
                        var chromeVersion = FileVersionInfo.GetVersionInfo(googleChromeOptions!.ChromeLocation).FileVersion;
                        await Log.InfoAndConsole($"The version of Google Chrome ({chromeVersion}) is different from the version of EtsyStats.");
                    }
                    else
                    {
                        Console.WriteLine("\nSomething went wrong. Unexpected error has occured :(\n\n");
                        await Log.Debug("See logs/logs.txt.");
                    }

                    await Log.Error($"Invalid Operation Exception\r\n{e}");
                }
                catch (StaleElementReferenceException e)
                {
                    Console.WriteLine("\nThere was an error trying to load listing(s). Please try again.\n\n");
                    await Log.Error($"Stale Element Reference Exception\r\n{e}");
                }
                catch (IOException e)
                {
                    Console.WriteLine("\nThere was an error.\r\nPlease check if your Google Chrome browsed is closed.\n\n");
                    await Log.Error($"IO Exception\r\n{e}");
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nSomething went wrong. Unexpected error has occured :(\n\n");
                    await Log.Error($"Unexpected Exception\r\n{e}");
                }
            } while (!exit);
        }
        catch (Exception e)
        {
            Console.WriteLine("\nSomething went wrong. Unexpected error has occured :(\n\n");
            await Log.Error(e.ToString());
        }

        Console.ReadLine();
    }

    private static Config FirstTimeLaunchConfigure(ApplicationOptions applicationOptions)
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFileName = $"{appDataFolder}/{applicationOptions.UserDataDirectory}/{applicationOptions.ConfigFileName}";

        Config config;
        if (!File.Exists(configFileName))
        {
            Directory.CreateDirectory($"{appDataFolder}/{applicationOptions.UserDataDirectory}");
            ProgramHelper.OriginalOut.WriteLine("\nYou're using EtsyStats for the firs time. " +
                                                "Lets set you up. Please type following data bellow." +
                                                "\nYour shop name:");
            var shopName = Console.ReadLine();

            config = new Config { ShopName = shopName! };
            File.WriteAllText(configFileName, JsonConvert.SerializeObject(config));

            ProgramHelper.OriginalOut.WriteLine("\nYou're all set!");
        }
        else
        {
            var configFileContents = File.ReadAllText(configFileName);
            config = JsonConvert.DeserializeObject<Config>(configFileContents)!;
        }

        return config;
    }
}