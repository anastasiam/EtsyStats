// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using EtsyStats.Helpers;
using EtsyStats.Models;
using EtsyStats.Models.Enums;
using EtsyStats.Models.Options;
using EtsyStats.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenQA.Selenium;

namespace EtsyStats;

public static class Program
{
    // TODO: Add unit tests
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

            if (googleSheetsOptions is null
                || googleChromeOptions is null
                || applicationOptions is null)
            {
                Console.WriteLine("\nCan't load configurations. Please contact the support.\n\n");
                await Log.ErrorAsync("Application options, Google Chrome options or Google Sheets options are null. Exiting.");
                return;
            }

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            var config = FirstTimeLaunchSetUserConfiguration(applicationOptions);
            Console.WriteLine($"Shop: {config.ShopName}");

            // TODO add DI
            var googleSheetsService = new GoogleSheetService();
            var etsyDataUploadService = new EtsyDataUploadService(config, googleSheetsService);
            var directoryHelper = new DirectoryHelper();
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
                            var dateRange = GetDateRangeStats();

                            await Log.InfoAndConsoleAsync("Getting listings stats from Etsy...\n");
                            var statistic = await etsyParser.GetListingsStatistics(dateRange);

                            if (statistic?.ListingStatistics is null)
                            {
                                Console.WriteLine("\nThere was an error trying to get Listings Statistics.");
                                await Log.ErrorAsync("Error on parsing listings stats, statistic?.ListingStatistics is null.");
                                break;
                            }

                            await Log.InfoAndConsoleAsync("Writing Listings Stats to Google Sheets...");
                            await etsyDataUploadService.WriteListingsStatisticsToGoogleSheet(googleSheetsOptions.SpreadsheetId!, statistic.ListingStatistics);
                            await Log.InfoAndConsoleAsync("Listings Stats were uploaded successfully.");

                            if (statistic.SearchTermStatistics is null)
                            {
                                Console.WriteLine("\nThere was an error trying to calculate Search Terms Summary. Skipping writing Summary to Google Sheets.");
                                await Log.ErrorAsync("Error on parsing listings stats, SearchTermsSummary is null.");
                                break;
                            }

                            if (statistic.TagStatistics is null)
                            {
                                Console.WriteLine("\nThere was an error trying to calculate Tags Summary. Skipping writing Summary to Google Sheets.");
                                await Log.ErrorAsync("Error on parsing listings stats, TagsSummary is null.");
                                break;
                            }

                            await Log.InfoAndConsoleAsync("Writing Summary Statistic to Google Sheets...");
                            await etsyDataUploadService.WriteStatisticSummaryToGoogleSheet(googleSheetsOptions.SpreadsheetId!, statistic.SearchTermStatistics, statistic.TagStatistics);
                            await Log.InfoAndConsoleAsync("Summary Statistic was uploaded successfully.");

                            await googleSheetsService.SortSheetsAscending(googleSheetsOptions.SpreadsheetId!);

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
                        var chromeVersion = FileVersionInfo.GetVersionInfo(googleChromeOptions.ChromeLocation!).FileVersion;
                        await Log.InfoAsync(e.ToString());
                        await Log.InfoAndConsoleAsync($"The version of Google Chrome ({chromeVersion}) is different from the version of EtsyStats.");
                    }
                    else
                    {
                        Console.WriteLine("\nSomething went wrong. Unexpected error has occured :(\n\n");
                    }

                    await Log.ErrorAsync($"Invalid Operation Exception\r\n{e}");
                }
                catch (StaleElementReferenceException e)
                {
                    Console.WriteLine("\nThere was an error trying to load listings. Please try again.\n\n");
                    await Log.ErrorAsync($"Stale Element Reference Exception\r\n{e}");
                }
                catch (IOException e)
                {
                    Console.WriteLine("\nThere was an error.\r\nPlease check if your Google Chrome browsed is closed and try again.\n\n");
                    await Log.ErrorAsync($"IO Exception\r\n{e}");
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nSomething went wrong. Unexpected error has occured :(\n\n");
                    await Log.ErrorAsync($"Unexpected Exception\r\n{e}");
                }
            } while (!exit);
        }
        catch (Exception e)
        {
            Console.WriteLine("\nSomething went wrong. Unexpected error has occured :(\n\n");
            await Log.ErrorAsync(e.ToString());
        }

        Console.ReadLine();
    }

    private static UserConfiguration FirstTimeLaunchSetUserConfiguration(ApplicationOptions applicationOptions)
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFileName = $"{appDataFolder}/{applicationOptions.UserDataDirectory}/{applicationOptions.ConfigFileName}";

        UserConfiguration userConfiguration;
        if (!File.Exists(configFileName))
        {
            Directory.CreateDirectory($"{appDataFolder}/{applicationOptions.UserDataDirectory}");
            Console.WriteLine("\nYou're using EtsyStats for the firs time. " +
                              "Lets set you up. Please type following data bellow." +
                              "\nYour shop name:");
            var shopName = Console.ReadLine();

            userConfiguration = new UserConfiguration { ShopName = shopName! };
            File.WriteAllText(configFileName, JsonConvert.SerializeObject(userConfiguration));

            Console.WriteLine("\nYou're all set!");
        }
        else
        {
            var configFileContents = File.ReadAllText(configFileName);
            userConfiguration = JsonConvert.DeserializeObject<UserConfiguration>(configFileContents)!;
        }

        return userConfiguration;
    }

    private static DateRange GetDateRangeStats()
    {
        Console.WriteLine("\nPlease select the date range:");
        Console.WriteLine("1 - Last 30 days");
        Console.WriteLine("2 - This month");
        Console.WriteLine("3 - This year");
        Console.WriteLine("4 - Last year");
        Console.WriteLine("5 - All time");
        var dateRangeCommand = Console.ReadLine();

        switch (dateRangeCommand)
        {
            case "1":
                Console.WriteLine("\nGetting data for the last 30 days");
                return DateRange.Last30Days;
            case "2":
                Console.WriteLine("\nGetting data for this month");
                return DateRange.ThisMonth;
            case "3":
                Console.WriteLine("\nGetting data for this year");
                return DateRange.ThisYear;
            case "4":
                Console.WriteLine("\nGetting data for the last year");
                return DateRange.LastYear;
            case "5":
                Console.WriteLine("\nGetting data for all time");
                return DateRange.AllTime;
            default:
                Console.WriteLine("\nUnknown command, please try again.");
                GetDateRangeStats();
                break;
        }

        return DateRange.Unknown;
    }
}