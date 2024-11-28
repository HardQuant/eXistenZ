using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

public class Constructors
{
    private readonly IMongoDatabase _database;

    // Constructor: Accepts the database instance from DataPipeline
    public Constructors(IMongoDatabase database)
    {
        _database = database;
    }

    // Main method to load data into MongoDB
    public async Task LoadDataIntoMongoDB(int startYear, int endYear)
    {
        // Define the constructors collection | MongoDB will automatically create the collection if non-existent
        var constructorsCollection = _database.GetCollection<Constructor>("constructors");

        // HTTP client for API calls
        HttpClient httpClient = new HttpClient();

        // Iterate through the specified range of years
        for (int year = startYear; year <= endYear; year++)
        {
            Console.WriteLine($"Fetching data for year: {year}");
            string url = $"http://ergast.com/api/f1/{year}/constructors.json";

            try
            {
                // Fetch data from the API
                var response = await httpClient.GetStringAsync(url);
                var apiData = JsonConvert.DeserializeObject<dynamic>(response);

                // Extract constructors data
                var constructors = apiData.MRData.ConstructorTable.Constructors;

                foreach (var constructor in constructors)
                {
                    string constructorId = constructor.constructorId;
                    string name = constructor.name;
                    string nationality = constructor.nationality;

                    // Check for existing constructor in the database
                    var filter = Builders<Constructor>.Filter.Eq("ConstructorId", constructorId);
                    var existingConstructor = await constructorsCollection.Find(filter).FirstOrDefaultAsync();

                    if (existingConstructor == null)
                    {
                        // Insert new constructor into MongoDB
                        var newConstructor = new Constructor
                        {
                            ConstructorId = constructorId,
                            Name = name,
                            Nationality = nationality
                        };
                        await constructorsCollection.InsertOneAsync(newConstructor);
                        Console.WriteLine($"Inserted: {name} ({constructorId})");
                    }
                    else
                    {
                        Console.WriteLine($"Constructor {constructorId} already exists, skipping.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data for year {year}: {ex.Message}");
            }
        }
    }

    // Corrected private Constructor model class
    private class Constructor
    {
        [BsonElement("constructorId")]
        public string ConstructorId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("nationality")]
        public string Nationality { get; set; }
    }
}
