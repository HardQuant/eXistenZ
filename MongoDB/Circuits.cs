using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;

public class Circuits
{
    private readonly IMongoCollection<BsonDocument> _circuitsCollection;

    public Circuits(IMongoDatabase database)
    {
        _circuitsCollection = database.GetCollection<BsonDocument>("Circuits");
    }

    public async Task LoadDataIntoMongoDB(int startYear, int endYear)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                for (int year = startYear; year <= endYear; year++)
                {
                    string apiUrl = $"http://ergast.com/api/f1/{year}/circuits.json";
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JObject data = JObject.Parse(jsonResponse);

                        var circuits = data["MRData"]["CircuitTable"]["Circuits"];
                        Console.WriteLine($"Processing circuits for season {year}...");

                        foreach (var circuit in circuits)
                        {
                            string circuitId = circuit["circuitId"].ToString();
                            string circuitName = circuit["circuitName"].ToString();
                            string locality = circuit["Location"]["locality"].ToString();
                            string country = circuit["Location"]["country"].ToString();

                            // Check if the circuit already exists in the database
                            var filter = Builders<BsonDocument>.Filter.Eq("circuitId", circuitId);
                            var existingCircuit = await _circuitsCollection.Find(filter).FirstOrDefaultAsync();

                            if (existingCircuit == null)
                            {
                                // Insert new circuit with the season year
                                var document = new BsonDocument
                                {
                                    { "circuitId", circuitId },
                                    { "circuitName", circuitName },
                                    { "locality", locality },
                                    { "country", country },
                                    { "seasons", new BsonArray { year } }
                                };

                                await _circuitsCollection.InsertOneAsync(document);
                                Console.WriteLine($"Inserted: {circuitId} - {circuitName} (Season {year})");
                            }
                            else
                            {
                                // Update existing circuit to add the year to the seasons array if not already present
                                var update = Builders<BsonDocument>.Update.AddToSet("seasons", year);
                                await _circuitsCollection.UpdateOneAsync(filter, update);
                                Console.WriteLine($"Updated: {circuitId} - {circuitName} (Added Season {year})");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to fetch data for year {year}. Status code: {response.StatusCode}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in LoadDataIntoMongoDB: {ex.Message}");
        }
    }
}
