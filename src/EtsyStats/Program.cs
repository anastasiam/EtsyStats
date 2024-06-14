// See https://aka.ms/new-console-template for more information

using System.Globalization;
using EtsyStats.Models;
using EtsyStats.Models.Options;
using EtsyStats.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace EtsyStats;

public static class Program
{
    public static async Task Main()
    {
        try
        {
            ProgramHelper.OriginalOut = Console.Out;

// Don't allow other packages to write to console
#if !DEBUG
Console.SetOut(TextWriter.Null);
Console.WriteLine("You should not see this");
#endif

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
            var configurationOptions = configuration.GetSection(ConfigurationOptions.SectionName).Get<ConfigurationOptions>();
            var googleChromeOptions = configuration.GetSection(GoogleChromeOptions.SectionName).Get<GoogleChromeOptions>();
            var googleSheetsOptions = configuration.GetSection(GoogleSheetsOptions.SectionName).Get<GoogleSheetsOptions>();

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            var config = FirstTimeLaunchConfigure(applicationOptions);

            await ProgramHelper.OriginalOut.WriteLineAsync($"\nGoogle Chrome Profile: {config.ChromeProfile}");

            var etsyDataUploadService = new EtsyDataUploadService(config);
            var etsyParser = new EtsyParser(googleChromeOptions, config);
            do
            {
                try
                {
                    await ProgramHelper.OriginalOut.WriteLineAsync("\nPlease select an action that you would like to do:" +
                                                                   "\n1 - Upload listings stats" +
                                                                   "\n2 - Upload search analytics");
                    // "\n0 - Exit the program");
                    var command = Console.ReadLine();
                    switch (command)
                    {
                        case "1":
                        {
                            var dateRange = ProgramHelper.GetDateRangeStats();
                            // Prod
                            await ProgramHelper.OriginalOut.WriteLineAsync("\nGetting listings stats from Etsy...\n");
                            var data = await etsyParser.GetListingsStats(dateRange);

                            // Test
                            // const string htmlStatsPageFileName = @"/Users/anastasiia/My Projects/EtsyStats.Test/pages/Stats.html";
                            // ProgramHelper.OriginalOut.WriteLine("Reading file...");
                            // var html = etsyParser.GetHtmlFromFile(htmlStatsPageFileName);
                            //
                            // ProgramHelper.OriginalOut.WriteLine("Parsing html...");
                            // var listing = new ListingStats();
                            // etsyParser.LoadListingStatsFromPage(html, string.Empty, listing);
                            // var data = new List<ListingStats> { listing };

                            await ProgramHelper.OriginalOut.WriteLineAsync("\nWriting data to Google Sheets...");
                            await Log.Error($"WriteDataToSheet googleSheetsOptions sheetId: {googleSheetsOptions.SheetId}");
                            await etsyDataUploadService.WriteListingsStatsToGoogleSheet(googleSheetsOptions.SheetId, data);

                            await ProgramHelper.OriginalOut.WriteLineAsync("\nListings were uploaded successfully.");
                            break;
                        }
                        case "2":
                        {
                            var dateRange = ProgramHelper.GetDateRange();
                            await ProgramHelper.OriginalOut.WriteLineAsync("\nGetting search analytics from Etsy...\n");
                            var data = await etsyParser.GetSearchAnalytics(dateRange);

                            await ProgramHelper.OriginalOut.WriteLineAsync("\nWriting data to Google Sheets...");
                            await etsyDataUploadService.WriteSearchAnalyticsToGoogleSheet(googleSheetsOptions.SheetId, data);

                            break;
                        }
                        case "0":
                            exit = true;
                            break;
                    }
                }
                catch (Exception e)
                {
                    await ProgramHelper.OriginalOut.WriteLineAsync("\nSomething went wrong. Unexpected error has occured :(\n\n");
                    await Log.Debug("See logs/logs.txt.");
                    await Log.Error(e.GetBaseException().ToString());
                }
            } while (!exit);
        }
        catch (Exception e)
        {
            await ProgramHelper.OriginalOut.WriteLineAsync("\nSomething went wrong. Unexpected error has occured :(\n\n");
            await Log.Debug("See logs/logs.txt.");
            await Log.Error(e.GetBaseException().ToString());
        }
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
                                                "\n\nGoogle Chrome profile name where you signed in to etsy.com:");
            var chromeProfile = Console.ReadLine();

            ProgramHelper.OriginalOut.WriteLine("\nYour shop name:");
            var shopName = Console.ReadLine();

            config = new Config { ChromeProfile = chromeProfile, ShopName = shopName };
            File.WriteAllText(configFileName, JsonConvert.SerializeObject(config));

            ProgramHelper.OriginalOut.WriteLine("\nYou're all set!");
        }
        else
        {
            var configFileContents = File.ReadAllText(configFileName);
            config = JsonConvert.DeserializeObject<Config>(configFileContents);
        }

        return config;
    }
}