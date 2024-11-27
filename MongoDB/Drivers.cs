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

    // Step 1: Extract Data from API for a specific year
    public async Task<JArray> ExtractData(int year)
    {
        string apiUrl = $"http://ergast.com/api/f1/{year}/drivers.json";
        using (HttpClient httpClient = new HttpClient())
        {
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                var jsonData = JObject.Parse(data);
                var drivers = (JArray)jsonData["MRData"]["DriverTable"]["Drivers"];
                Console.WriteLine($"Drivers data for year {year} extracted successfully.");
                return drivers;
            }
            else
            {
                Console.WriteLine($"Failed to fetch drivers data for year {year}.");
                return null;
            }
        }
    }

    // Step 2: Load Transformed Data into MongoDB for multiple years
    public async Task LoadDataIntoMongoDB(int startYear, int endYear)
    {
        var collection = _database.GetCollection<BsonDocument>("Drivers");

        // Iterate through each year in the range
        for (int year = startYear; year <= endYear; year++)
        {
            Console.WriteLine($"Processing data for year {year}...");
            var drivers = await ExtractData(year);

            if (drivers != null)
            {
                foreach (var driver in drivers)
                {
                    // Ensure driverId is extracted correctly
                    var driverId = driver["driverId"]?.ToString();
                    if (string.IsNullOrEmpty(driverId))
                    {
                        Console.WriteLine($"Skipping driver due to missing driverId for year {year}.");
                        continue;
                    }

                    // Check for an existing document using driverId
                    var filter = Builders<BsonDocument>.Filter.Eq("driverId", driverId);
                    var existingDriver = await collection.Find(filter).FirstOrDefaultAsync();

                    if (existingDriver == null)
                    {
                        // Transform and insert new driver
                        var newDriver = TransformData(driver);
                        await collection.InsertOneAsync(newDriver);
                        Console.WriteLine($"Inserted driver {newDriver["Driver"]} for year {year}.");
                    }
                    else
                    {
                        Console.WriteLine($"Driver {existingDriver["Driver"]} already exists. Skipping...");
                    }
                }
            }
            else
            {
                Console.WriteLine($"No drivers data available for year {year}. Skipping...");
            }
        }

        Console.WriteLine("All driver data loaded successfully!");
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
