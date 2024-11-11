using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

public class Drivers
{
    private readonly IMongoDatabase _database;

    public Drivers(IMongoDatabase database)
    {
        _database = database;
    }

    // Step 1: Extract Data from API
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

    // Step 2: Load Transformed Data into MongoDB
    public async Task LoadDataIntoMongoDB()
    {
        var collection = _database.GetCollection<BsonDocument>("Drivers");
        var drivers = await ExtractData();

        if (drivers != null)
        {
            foreach (var driver in drivers)
            {
                // Ensure driverId is extracted correctly
                var driverId = driver["driverId"]?.ToString();
                if (string.IsNullOrEmpty(driverId))
                {
                    Console.WriteLine("Skipping driver due to missing driverId.");
                    continue;
                }

                // Check for an existing document using driverId
                var filter = Builders<BsonDocument>.Filter.Eq("driverId", driverId);
                Console.WriteLine($"Checking for driverId = {driverId} in database.");
                var existingDriver = await collection.Find(filter).FirstOrDefaultAsync();

                if (existingDriver == null)
                {
                    // Insert new driver if not found
                    var newDriver = new BsonDocument
                    {
                        { "driverId", driverId },  // Add driverId explicitly
                        { "Driver", $"{driver["givenName"]} {driver["familyName"]}" },
                        { "Nationality", driver["nationality"]?.ToString() },
                        { "DOB", DateTime.ParseExact(driver["dateOfBirth"]?.ToString() ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy") }
                    };

                    Console.WriteLine($"Inserting new driver: {newDriver}");
                    await collection.InsertOneAsync(newDriver);
                }
                else
                {
                    Console.WriteLine($"Driver {driverId} already exists. Skipping...");
                }
            }
            Console.WriteLine("Driver data load completed.");
        }
        else
        {
            Console.WriteLine("No drivers data to load.");
        }
    }
}
