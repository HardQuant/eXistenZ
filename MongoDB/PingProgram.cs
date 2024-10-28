using MongoDB.Driver;
using MongoDB.Bson;

public class PingProgram 
{
    // "?" marks this method as nullable
    public MongoClient? /* -> return type of the method */ InitializeMongoClient() /* -> Name of the method */
    {
        var username = Environment.GetEnvironmentVariable("MONGODB_USERNAME");
        var password = Environment.GetEnvironmentVariable("MONGODB_PASSWORD");
        var cluster = Environment.GetEnvironmentVariable("MONGODB_CLUSTER");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(cluster))
        {
            Console.WriteLine("Missing Required environment variables for MongoDB credentials.Please enter the following line into the Terminal: source ~/.zshrc");
            return null;
        }
        string connectionUri = $"mongodb+srv://{username}:{Uri.EscapeDataString(password)}@{cluster}/?retryWrites=true&w=majority&appName=Cluster44";
        var settings = MongoClientSettings.FromConnectionString(connectionUri);

        settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        var client = new MongoClient(settings);

        try
        {
            var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            Console.WriteLine($"Pinged your deployment. You succesfully connected to MongoDB at [{DateTime.Now}]!");

            /* //Initializing Database and collection-COMPLETED
            var DatabaseSetup = new DatabaseSetup(client, "F_1");
            DatabaseSetup.CreateCollection("Drivers");
            DatabaseSetup.SeedInitialData(); */
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        return null;
    }
}