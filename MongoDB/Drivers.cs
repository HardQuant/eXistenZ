using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System.Globalization;

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
        string apiUrl = "http://ergast.com/api/f1/1986/drivers.json"; // Update year as needed
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

    // Step 2: Transform and Load Data into MongoDB
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
                    var newDriver = TransformData(driver);
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

    // Step 3: Transform Data
    private BsonDocument TransformData(JToken driver)
    {
     // Convert dateOfBirth to MM/DD/YYYY format
     string dob = driver["dateOfBirth"]?.ToString();
     string formattedDOB = DateTime.TryParseExact(
        dob, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate
     ) ? parsedDate.ToString("MM/dd/yyyy") : null;

    // Create the transformed document
    var document = new BsonDocument
     {
        { "driverId", driver["driverId"]?.ToString() },
        { "Driver", $"{driver["givenName"]} {driver["familyName"]}" },
        { "Nationality", driver["nationality"]?.ToString() },
        { "DOB", formattedDOB }
     };

    // Add optional fields with explicit null handling
     document.Add("permanentNumber", driver["permanentNumber"] != null ? driver["permanentNumber"].ToString() : BsonNull.Value);
     document.Add("code", driver["code"] != null ? driver["code"].ToString() : BsonNull.Value);

     return document;
    }
}