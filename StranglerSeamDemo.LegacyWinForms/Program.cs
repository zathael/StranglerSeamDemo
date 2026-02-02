using Microsoft.Extensions.Configuration;
using StranglerSeamDemo.LegacyWinForms;
using StranglerSeamDemo.LegacyWinForms.Api;

namespace StranglerSeamDemo.LegacyWinForms;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var useApi = config.GetValue("Migration:UseApiSeam", true);
        var apiBaseUrl = config["Api:BaseUrl"] ?? "http://localhost:5050/";
        var sqlitePath = config["Data:SqlitePath"] ?? "../StranglerSeamDemo.db";

        ICasesGateway gateway = useApi
            ? new ApiCasesGateway(apiBaseUrl)
            : new LegacySqliteCasesGateway(sqlitePath);

        Application.Run(new MainForm(gateway, useApi));
    }
}