using EtsyStats.Attributes;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;

namespace EtsyStats.Services;

public class GoogleSheetService
{
    // TODO put in appsettings.json
    private const string GoogleCredentialsFileName = "google-credentials.json";
    private const string RowsDimension = "ROWS";
    private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

    private SheetsService InitSheetsService()
    {
        using var stream = new FileStream(GoogleCredentialsFileName, FileMode.Open, FileAccess.Read);
        var serviceInitializer = new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromStream(stream).CreateScoped(Scopes)
        };

        return new SheetsService(serviceInitializer);
    }

    public async Task<(int updatedColumns, int updatedRows)> WriteDataToSheet<T>(string spreadsheetId, string tabName, IList<T> data, int firstColumnIndex = 0)
    {
        using var sheetsService = InitSheetsService();
        var valuesResource = sheetsService.Spreadsheets.Values;

        await Log.InfoAsync("Clearing data from sheet");
        var clear = valuesResource.Clear(new ClearValuesRequest(), spreadsheetId, GetAllRange(tabName));
        await clear.ExecuteAsync();

        return await AppendDataToSheet(spreadsheetId, tabName, data, firstColumnIndex, sheetsService);
    }

    public async Task<(int updatedColumns, int updatedRows)> AppendDataToSheet<T>(string spreadsheetId, string tabName, IList<T> data, int firstColumnIndex = 0, SheetsService? sheetsService = null)
    {
        await Log.InfoAsync("Appending data to sheet");
        sheetsService ??= InitSheetsService();

        try
        {
            var valuesResource = sheetsService.Spreadsheets.Values;

            var properties = typeof(T).GetProperties()
                .Select(propertyInfo => new
                {
                    propertyInfo,
                    sheetColumnAttribute = (SheetColumnAttribute?)propertyInfo.GetCustomAttributes(typeof(SheetColumnAttribute), false).FirstOrDefault()
                })
                .Where(p => p.sheetColumnAttribute is not null)
                .OrderBy(p => p.sheetColumnAttribute!.Order)
                .ToList();

            // Headers
            var columns = properties.Select(object (p) => p.sheetColumnAttribute!.ColumnName ?? p.propertyInfo.Name).ToList();
            var valueRange = new ValueRange { Values = new List<IList<object>> { columns }, MajorDimension = RowsDimension };

            // Values
            foreach (var rowData in data)
            {
                var values = properties.Select(p => p.propertyInfo.GetValue(rowData)).ToList();
                valueRange.Values.Add(values);
            }

            var range = GetRange(tabName, columns.Count, data.Count + 1, firstColumnIndex); // data.Count + 1 - for header

            await Log.InfoAsync($"Range: {range}");

            var update = valuesResource.Update(valueRange, spreadsheetId, range);

            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

            var response = await update.ExecuteAsync();

            await Log.InfoAndConsoleAsync($"Updated rows: {response.UpdatedRows}");

            return (updatedColumns: response.UpdatedColumns ?? 0, updatedRows: response.UpdatedRows ?? 0);
        }
        finally
        {
            sheetsService.Dispose();
        }
    }

    public async Task CreateTab(string spreadsheetId, string tabName)
    {
        await Log.InfoAndConsoleAsync($"Creating a new tab: {tabName}");
        using var sheetsService = InitSheetsService();

        var request = new BatchUpdateSpreadsheetRequest
        {
            Requests = new List<Request>
            {
                new() { AddSheet = new AddSheetRequest { Properties = new SheetProperties { Title = tabName, Index = 0 } } }
            }
        };
        var batchUpdate = sheetsService.Spreadsheets.BatchUpdate(request, spreadsheetId);
        await batchUpdate.ExecuteAsync();

        await Log.InfoAndConsoleAsync($"Tab {tabName} was creating successfully");
    }

    public async Task SortSheetsAscending(string spreadsheetId)
    {
        await Log.InfoAsync("Sorting Sheets");
        using var sheetsService = InitSheetsService();
        var spreadsheet = await sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();

        var sortedSheets = spreadsheet.Sheets.OrderBy(sheet => sheet.Properties.Title).ToList();

        var requests = new List<Request>();
        for (var i = 0; i < sortedSheets.Count; i++)
            requests.Add(new Request
            {
                UpdateSheetProperties = new UpdateSheetPropertiesRequest
                {
                    Properties = new SheetProperties
                    {
                        SheetId = sortedSheets[i].Properties.SheetId,
                        Index = i // New index for the sorted sheet
                    },
                    Fields = "index"
                }
            });

        var batchUpdateRequest = new BatchUpdateSpreadsheetRequest { Requests = requests };
        await sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, spreadsheetId).ExecuteAsync();

        await Log.InfoAsync("Sheets sorted successfully.");
    }

    public bool SheetExists(string spreadsheetId, string sheetName)
    {
        using var sheetsService = InitSheetsService();
        var spreadsheet = sheetsService.Spreadsheets.Get(spreadsheetId).Execute();
        return spreadsheet.Sheets.Any(s => s.Properties.Title == sheetName);
    }

    private int GetSheetId(SheetsService service, string spreadsheetId, string sheetName)
    {
        var spreadsheet = service.Spreadsheets.Get(spreadsheetId).Execute();
        var sheet = spreadsheet.Sheets.First(s => s.Properties.Title == sheetName);
        return sheet.Properties.SheetId!.Value;
    }

    private string GetRange(string tabName, int columnsCount, int rowsCount, int firstColumnIndex = 0)
    {
        var firstColumnName = GetGoogleSheetColumnName(firstColumnIndex + 1);
        var lastColumnName = GetGoogleSheetColumnName(columnsCount + firstColumnIndex);

        return $"{tabName}!{firstColumnName}1:{lastColumnName}{rowsCount + 2}";
    }

    private string GetAllRange(string tabName)
    {
        return $"{tabName}!A1:Z";
    }

    private string GetGoogleSheetColumnName(int columnNumber)
    {
        var columnName = string.Empty;

        while (columnNumber > 0)
        {
            var modulo = (columnNumber - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            columnNumber = (columnNumber - modulo) / 26;
        }

        return columnName;
    }
}