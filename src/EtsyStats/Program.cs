// See https://aka.ms/new-console-template for more information

using EtsyStats;
using EtsyStats.Models;
using EtsyStats.Services;

// Test
// const string sheetId = "1Iyl2B5Es4XvJF24ymgRhm7TDBioIHNP3HG6HE59OMdE"; //My
// const string chromeLocation = @"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"; //Mac

// Prod
const string sheetId = "1AqxmHf7vPOalndZrC6AiCXcqb8WpzzC6t74qURn6c0U"; //Slava
const string chromeLocation = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
const string shop = "MetalHomeLab";
var exit = false;

ProgramHelper.OriginalOut = Console.Out;

// Don't allow other packages to write to console
#if !DEBUG
Console.SetOut(TextWriter.Null);
Console.WriteLine("You should not see this");
#endif

Startup.SetupLogs();

var etsyDataUploadService = new EtsyDataUploadService();
var etsyParser = new EtsyParser(chromeLocation);
do
{
    try
    {
        ProgramHelper.OriginalOut.WriteLine("\nPlease select an action that you would like to do:" +
                                            "\n1 - Upload listings stats" +
                                            "\n2 - Upload search analytics");
        // "\n0 - Exit the program");
        var command = Console.ReadLine();
        ProgramHelper.OriginalOut.WriteLine("\nGood choice!");
        switch (command)
        {
            case "1":
            {
                var dateRange = ProgramHelper.GetDateRange();
                // Prod
                ProgramHelper.OriginalOut.WriteLine("\nGetting listings stats from Etsy...\n");
                var data = await etsyParser.GetListingsStats(shop, dateRange);

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
                await etsyDataUploadService.WriteListingsStatsToGoogleSheet(sheetId, shop, data);

                ProgramHelper.OriginalOut.WriteLine("\nListings were uploaded successfully.");
                break;
            }
            case "2":
            {
                var dateRange = ProgramHelper.GetDateRange();
                ProgramHelper.OriginalOut.WriteLine("\nGetting search analytics from Etsy...\n");
                var data = await etsyParser.GetSearchAnalytics(shop, dateRange);

                ProgramHelper.OriginalOut.WriteLine("\nWriting data to Google Sheets...");
                await etsyDataUploadService.WriteSearchAnalyticsToGoogleSheet(sheetId, shop, data);

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