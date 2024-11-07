using MongoDB.Driver;
using MongoDB.Bson;
using System;

public class PingProgram 
{
    public MongoClient? InitializeMongoClient()
    {
        var username = Environment.GetEnvironmentVariable("MONGODB_USERNAME");
        var password = Environment.GetEnvironmentVariable("MONGODB_PASSWORD");
        var cluster = Environment.GetEnvironmentVariable("MONGODB_CLUSTER");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(cluster))
        {
            Console.WriteLine("Missing Required environment variables for MongoDB credentials. Please enter the following line into the Terminal: source ~/.zshrc");
            return null;
        }

        string connectionUri = $"mongodb+srv://{username}:{Uri.EscapeDataString(password)}@{cluster}/?retryWrites=true&w=majority&appName=Cluster44";
        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        var client = new MongoClient(settings);

        try
        {
            client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            Console.WriteLine($"Pinged your deployment. You successfully connected to MongoDB at [{DateTime.Now}]!");
            
            // Return the client upon successful connection
            return client;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to MongoDB: {ex.Message}");
            return null; // Return null only if an exception occurs
        }
    }
}