using Newtonsoft.Json;
using System;
using System.IO;

namespace AnalyticsReportingApi
{
    class Program
    {
        static AppSettings GetAppSettings(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("appsettings.json was not found or is not accesible");

            var appSettingsContent = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(appSettingsContent))
                throw new Exception("Appsettings file is empty");

            return JsonConvert.DeserializeObject<AppSettings>(appSettingsContent);
        }

        static void Main(string[] args)
        {
            var appSettingsFilePath = "appsettings.json";
            var appSettings = GetAppSettings(appSettingsFilePath);

            var popGa = new PopularUrlService(appSettings);

            var res = popGa.GetParsedResult();
            foreach (var item in res)
            {
                Console.WriteLine($"{item.Item2} page views @ {item.Item1}");
            }

            Console.ReadLine();
        }
    }
}
