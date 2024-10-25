using MongoDB.Driver;
using MongoDB.Bson;

public class DatabaseSetup
{
    private readonly IMongoDatabase _database;

    public DatabaseSetup(IMongoClient client, string F_1)
    {
        _database = client.GetDatabase(F_1);
    }

    public void CreateCollection(string Drivers) /* Collection of documents for the DB i.e. similar to tables in RDB */ 
    {
        // Check if the collection already exists
        var collections = _database.ListCollectionNames().ToList();
        if (!collections.Contains(Drivers))
        {
            _database.CreateCollection(Drivers);
            Console.WriteLine($"Collection '{Drivers}' created successfully.");
        }
        else
        {
            Console.WriteLine($"Collection '{Drivers}' already exists.");
        }
    }

    public void SeedInitialData()
    {
        var collection = _database.GetCollection<BsonDocument>("yourCollectionName");

        var sampleDocument = new BsonDocument
        {
            { "name", "Sample Data" },
            { "description", "This is initial data for testing purposes." }
        };

        collection.InsertOne(sampleDocument);
        Console.WriteLine("Sample data seeded into the database.");
    }
}
