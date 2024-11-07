using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Globalization;
using System.Threading.Tasks;

public class Drivers
{
    public readonly IMongoDatabase _database;

    public Drivers(IMongoDatabase database)
    {
        _database = database;
    }

    // Step 2: Extract Data from API
    public async Task<JArray> ExtractData()
    {
        string apiUrl = "http://ergast.com/api/f1/1986/drivers.json";
        using (HttpClient httpClient = new HttpClient())
        {
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                var jsonData = JObject.Parse(data);
                var drivers = (JArray)jsonData["MRData"]["DriverTable"]["Drivers"];
                Console.WriteLine("Drivers data extracted successfully.");
                return drivers;
            }
            else
            {
                Console.WriteLine("Failed to fetch drivers data from API.");
                return null;
            }
        }
    }

    // Step 3: Load Transformed Data into MongoDB
    public async Task LoadDataIntoMongoDB()
    {
        var collection = _database.GetCollection<BsonDocument>("Drivers");
        var drivers = await ExtractData();

        if (drivers != null)
        {
            foreach (var driver in drivers)
            {
                var transformedDocument = new BsonDocument
                {
                    { "Driver", $"{driver["givenName"]} {driver["familyName"]}" },
                    { "Nationality", driver["nationality"]?.ToString() },
                    { "DOB", DateTime.ParseExact(driver["dateOfBirth"]?.ToString() ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy") }
                };
                await collection.InsertOneAsync(transformedDocument);
                Console.WriteLine($"Inserted driver: {transformedDocument["Driver"]}");
            }
            Console.WriteLine("All drivers data loaded into MongoDB successfully!");
        }
        else
        {
            Console.WriteLine("No drivers data to load.");
        }
    }
}
