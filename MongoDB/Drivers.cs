using MongoDB.Driver;
using  MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System.Globalization;

public class Drivers
{
    private readonly IMongoDatabase _database;

    public Drivers(IMongoDatabase database)
    {
        _database = database;
    }

    private async Task<JArray> ExtractData()
    {
        string apiURL = "http://ergast.com/api/f1/1986/drivers.json";
        using (HttpClient httpClient = new HttpClient())
        {
            HttpResponseMessage response = await httpClient.GetAsync(apiURL);
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

    public async Task LoadDataIntoMongoDB()
    {
        var collection = _database.GetCollection<BsonDocument>("Drivers");
        var drivers = await ExtractData();

        if (drivers != null)
        {
            foreach (var driver in drivers)
            {
                var transformedDocument = TransformData(driver);
                await collection.InsertOneAsync(transformedDocument);
                Console.WriteLine($"Inserted driver: {transformedDocument["Driver"]}");
            }
            Console.WriteLine("No Drivers Data to Load");
        }

        //transformation
        private BsonDocument TransformData(JToken driver)
        {
            string dob = driver[dateOfBirth]?.ToString();
            string formattedDOB = DateTime.ParseExact(dob. "yyyy-MM-dd", CultureInfo.InvariantCulture)
                                            .ToString("MM/DD/YYYY");

            return new BsonDocument
            {
                { "Driver", $"{driver["givenName"]} {driver["familyName"]}" },
                { "Nationality", driver["nationality"]?.ToString() },
                {"DOB", formattedDOB }
            };
        }
    }
}