// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using DotNetEnv;
using EtsyStats.Factories;
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
            SetupLogs();
            var environmentSetResult = await SetEnvironment();
            if (!environmentSetResult)
            {
                await Log.ErrorAsync("Error on setting environment.");
                Console.WriteLine("\nCan't load configurations. Please contact the support.\nPress any key to exit...");
                Console.ReadKey();
                await Log.InfoAndConsoleAsync("Exiting...");
                return;
            }

            var (applicationOptions, configurationOptions, googleChromeOptions, googleSheetsOptions) = BuildConfiguration();

            if (applicationOptions is null
                || configurationOptions is null
                || googleChromeOptions is null
                || googleSheetsOptions is null)
            {
                await Log.ErrorAsync("Error on building configuration. ApplicationOptions, ConfigurationOptions, GoogleChromeOptions or GoogleSheetsOptions are null.");
                Console.WriteLine("\nCan't load configurations. Please contact the support.\nPress any key to exit...");
                Console.ReadKey();
                await Log.InfoAndConsoleAsync("Exiting...");
                return;
            }
            
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            var userConfiguration = FirstTimeLaunchSetUserConfiguration(applicationOptions);
            await Log.InfoAndConsoleAsync($"Shop: {userConfiguration.ShopName}");

            // TODO: add DI
            var googleSheetsService = new GoogleSheetService(googleSheetsOptions);
            var etsyDataUploadService = new EtsyDataUploadService(userConfiguration, googleSheetsService);
            var directoryHelper = new DirectoryHelper();
            var webScrapingServiceFactory = new WebScrapingServiceFactory();
            var etsyParser = new EtsyParser(directoryHelper, webScrapingServiceFactory, configurationOptions);

            var exit = false;
            do
            {
                try
                {
                    Console.WriteLine(
                        "\nPlease select an action that you would like to do:" +
                        "\n1 - Upload listings stats" +
                        "\n0 - Exit the program");
                    var command = Console.ReadLine();
                    switch (command)
                    {
                        case "1":
                        {
                            await UploadListingsStats(etsyParser, etsyDataUploadService, googleSheetsOptions, googleSheetsService);

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
                        await Log.InfoAndConsoleAsync($"The version of Google Chrome ({chromeVersion}) is different from the version of EtsyStats.");
                        await Log.InfoAsync(e.ToString());
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
            Console.WriteLine("\nSomething went wrong. Unexpected error has occured :(\nPress any key to exit...");
            await Log.ErrorAsync(e.ToString());
            Console.ReadKey();
        }

        await Log.InfoAndConsoleAsync("Exiting...");
    }

    #region Set up

    private static void SetupLogs()
    {
        Directory.CreateDirectory("logs");
        File.AppendAllText("logs/logs.txt", $"\r\n------------{DateTime.Now}------------\r\n");
    }

    private static async Task<bool> SetEnvironment()
    {
        var envFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), ".env.*");

        if (!envFiles.Any())
        {
            await Log.ErrorAsync("No environment files found starting with .env.");
            return false;
        }

        var selectedEnvFile = envFiles.First();
        await Log.InfoAsync($"Loading environment file: {selectedEnvFile}");
        Env.Load(selectedEnvFile);

        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        if (environment == null)
        {
            await Log.ErrorAsync("DOTNET_ENVIRONMENT variable was not found in the env file.");
            return false;
        }

        await Log.InfoAsync($"Environment: {environment}");
        return true;
    }

    private static (ApplicationOptions? applicationOptions, ConfigurationOptions? configurationOptions, GoogleChromeOptions? googleChromeOptions, GoogleSheetsOptions? googleSheetsOptions) BuildConfiguration()
    {
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, false)
            .AddJsonFile("appsettings.secrets.json", true, false)
            .AddJsonFile($"appsettings.{environmentName}.json", true, false)
            .AddJsonFile($"appsettings.{environmentName}.secrets.json", true, false)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        var applicationOptions = configuration.GetSection(ApplicationOptions.SectionName).Get<ApplicationOptions>();
        var configurationOptions = configuration.GetSection(ConfigurationOptions.SectionName).Get<ConfigurationOptions>();
        var googleChromeOptions = configuration.GetSection(GoogleChromeOptions.SectionName).Get<GoogleChromeOptions>();
        var googleSheetsOptions = configuration.GetSection(GoogleSheetsOptions.SectionName).Get<GoogleSheetsOptions>();

        if (googleSheetsOptions is not null)
            googleSheetsOptions.GoogleCredentialsFileName = googleSheetsOptions.GoogleCredentialsFileName?.Replace("[ENV]", environmentName?.ToLower());

        return (applicationOptions, configurationOptions, googleChromeOptions, googleSheetsOptions);
    }

    #endregion

    private static UserConfiguration FirstTimeLaunchSetUserConfiguration(ApplicationOptions applicationOptions)
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFileName = $"{appDataFolder}/{applicationOptions.UserDataDirectory}/{applicationOptions.ConfigFileName}";

        UserConfiguration userConfiguration;
        if (!File.Exists(configFileName))
        {
            Directory.CreateDirectory($"{appDataFolder}/{applicationOptions.UserDataDirectory}");
            Console.WriteLine("\nYou're using EtsyStats for the first time. " +
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

    private static async Task<DateRange> GetDateRangeStats()
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
                await Log.InfoAndConsoleAsync("\nGetting data for the last 30 days");
                return DateRange.Last30Days;
            case "2":
                await Log.InfoAndConsoleAsync("\nGetting data for this month");
                return DateRange.ThisMonth;
            case "3":
                await Log.InfoAndConsoleAsync("\nGetting data for this year");
                return DateRange.ThisYear;
            case "4":
                await Log.InfoAndConsoleAsync("\nGetting data for the last year");
                return DateRange.LastYear;
            case "5":
                await Log.InfoAndConsoleAsync("\nGetting data for all time");
                return DateRange.AllTime;
            default:
                await Log.InfoAndConsoleAsync("\nUnknown command, please try again.");
                await GetDateRangeStats();
                break;
        }

        return DateRange.Unknown;
    }

    private static async Task UploadListingsStats(EtsyParser etsyParser, EtsyDataUploadService etsyDataUploadService, GoogleSheetsOptions googleSheetsOptions, GoogleSheetService googleSheetsService)
    {
        var dateRange = await GetDateRangeStats();

        await Log.InfoAndConsoleAsync("Getting listings stats from Etsy...\n");
        var statistic = await etsyParser.GetListingsStatistics(dateRange);

        if (statistic?.ListingStatistics is null)
        {
            Console.WriteLine("\nThere was an error trying to get Listings Statistics.");
            await Log.ErrorAsync("Error on parsing listings stats, statistic?.ListingStatistics is null.");
            return;
        }

        await Log.InfoAndConsoleAsync("Writing Listings Stats to Google Sheets...");
        await etsyDataUploadService.WriteListingsStatisticsToGoogleSheet(googleSheetsOptions.SpreadsheetId!, statistic.ListingStatistics);
        await Log.InfoAndConsoleAsync("Listings Stats were uploaded successfully.");

        if (statistic.SearchTermStatistics is null)
        {
            Console.WriteLine("\nThere was an error trying to calculate Search Terms Summary. Skipping writing Summary to Google Sheets.");
            await Log.ErrorAsync("Error on parsing listings stats, SearchTermsSummary is null.");
            return;
        }

        if (statistic.TagStatistics is null)
        {
            Console.WriteLine("\nThere was an error trying to calculate Tags Summary. Skipping writing Summary to Google Sheets.");
            await Log.ErrorAsync("Error on parsing listings stats, TagsSummary is null.");
            return;
        }

        await Log.InfoAndConsoleAsync("Writing Summary Statistic to Google Sheets...");
        await etsyDataUploadService.WriteStatisticSummaryToGoogleSheet(googleSheetsOptions.SpreadsheetId!, statistic.SearchTermStatistics, statistic.TagStatistics);
        await Log.InfoAndConsoleAsync("Summary Statistic was uploaded successfully.");

        await googleSheetsService.SortSheetsAscending(googleSheetsOptions.SpreadsheetId!);
    }
}