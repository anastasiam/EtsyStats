using EtsyStats.Attributes;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace EtsyStats.Services;

public class GoogleSheetService
{
    // TODO put in appsettings.json
    private const string GoogleCredentialsFileName = "google-credentials.json";
    private const string RowsDimension = "ROWS";
    private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

    private readonly SheetsService _sheetsService;

    public GoogleSheetService()
    {
        using var stream = new FileStream(GoogleCredentialsFileName, FileMode.Open, FileAccess.Read);
        var serviceInitializer = new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromStream(stream).CreateScoped(Scopes)
        };

        _sheetsService = new SheetsService(serviceInitializer);
    }

    public async Task WriteDataToSheet<T>(string sheetId, string tabName, List<T> data)
    {
        var valuesResource = _sheetsService.Spreadsheets.Values;

        await Log.InfoAndConsole($"WriteDataToSheet sheetId: {(string.IsNullOrWhiteSpace(sheetId) ? "is NULL" : "is NOT NULL")}");
        var clear = valuesResource.Clear(new ClearValuesRequest(), sheetId, GetAllRange(tabName));
        await clear.ExecuteAsync();

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
        var columns = properties.Select(p => (object)(p.sheetColumnAttribute!.ColumnName ?? p.propertyInfo.Name)).ToList();
        var valueRange = new ValueRange { Values = new List<IList<object>> { columns }, MajorDimension = RowsDimension };

        // Values
        foreach (var rowData in data)
        {
            var values = properties.Select(p => p.propertyInfo.GetValue(rowData)).ToList();
            valueRange.Values.Add(values);
        }

        var range = GetRange(tabName, columns.Count, data.Count + 1); // +1 - for header
        var update = valuesResource.Update(valueRange, sheetId, range);

        update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

        var response = await update.ExecuteAsync();
        await Log.InfoAndConsole($"Updated rows:{response.UpdatedRows}");
    }

    public async Task CreateTab(string sheetId, string tabName)
    {
        await Log.InfoAndConsole($"Creating a new tab: {tabName}");
        await Log.Info($"CreateTab sheetId: {(string.IsNullOrWhiteSpace(sheetId) ? "is NULL" : "is NOT NULL")}");
        
        var request = new BatchUpdateSpreadsheetRequest
        {
            Requests = new List<Request>
            {
                new() { AddSheet = new AddSheetRequest { Properties = new SheetProperties { Title = tabName, Index = 0 } } }
            }
        };
        var batchUpdate = _sheetsService.Spreadsheets.BatchUpdate(request, sheetId);
        await batchUpdate.ExecuteAsync();

        await Log.InfoAndConsole($"Tab {tabName} was creating successfully");
    }

    private string GetRange(string tabName, int columnsCount, int rowsCount)
    {
        var lastColumnName = GetGoogleSheetColumnName(columnsCount);

        return $"{tabName}!A1:{lastColumnName}{rowsCount + 2}";
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