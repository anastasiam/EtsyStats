// See https://aka.ms/new-console-template for more information

using EtsyStats;
using EtsyStats.Models;
using EtsyStats.Services;
using Newtonsoft.Json;

// Test
const string sheetId = "1Iyl2B5Es4XvJF24ymgRhm7TDBioIHNP3HG6HE59OMdE"; //My
// const string chromeLocation = @"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"; //Mac

// Prod
// const string sheetId = "1AqxmHf7vPOalndZrC6AiCXcqb8WpzzC6t74qURn6c0U"; //Slava
const string chromeLocation = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
// const string shop = "MetalHomeLab";
const string configFilePath = "config.json";
const string etsyStatsAppDataFolder = "EtsyStats";
var exit = false;

ProgramHelper.OriginalOut = Console.Out;

// Don't allow other packages to write to console
#if !DEBUG
Console.SetOut(TextWriter.Null);
Console.WriteLine("You should not see this");
#endif

Startup.SetupLogs();

var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var configFileName = $"{appDataFolder}/{etsyStatsAppDataFolder}/{configFilePath}";
Config config;
if (!File.Exists(configFileName))
{
    Directory.CreateDirectory($"{appDataFolder}/{etsyStatsAppDataFolder}");
    ProgramHelper.OriginalOut.WriteLine("\nYou're using EtsyStats for the firs time. " +
                                        "Lets set you up. Please type following data bellow" +
                                        "\nGoogle Chrome profile name where you signed in to etsy.com:");
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

var etsyDataUploadService = new EtsyDataUploadService(config);
var etsyParser = new EtsyParser(chromeLocation, config);
do
{
    try
    {
        ProgramHelper.OriginalOut.WriteLine("\nPlease select an action that you would like to do:" +
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
                ProgramHelper.OriginalOut.WriteLine("\nGetting listings stats from Etsy...\n");
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

                ProgramHelper.OriginalOut.WriteLine("\nWriting data to Google Sheets...");
                await etsyDataUploadService.WriteListingsStatsToGoogleSheet(sheetId, data);

                ProgramHelper.OriginalOut.WriteLine("\nListings were uploaded successfully.");
                break;
            }
            case "2":
            {
                var dateRange = ProgramHelper.GetDateRange();
                ProgramHelper.OriginalOut.WriteLine("\nGetting search analytics from Etsy...\n");
                var data = await etsyParser.GetSearchAnalytics(dateRange);

                ProgramHelper.OriginalOut.WriteLine("\nWriting data to Google Sheets...");
                await etsyDataUploadService.WriteSearchAnalyticsToGoogleSheet(sheetId, data);

                break;
            }
            case "0":
                exit = true;
                break;
        }
    }
    catch (Exception e)
    {
        ProgramHelper.OriginalOut.WriteLine("\nAn error occured. See logs/logs.txt.");
        await Log.Error(e.GetBaseException().ToString());

        Console.ReadLine();
    }
} while (!exit);