using MongoDB.Driver;
using MongoDB.Bson;

class Program 
{
    static void Main(string[] args)
    {
        var username = Environment.GetEnvironmentVariable("MONGODB_USERNAME");
        var password = Environment.GetEnvironmentVariable("MONGODB_PASSWORD");
        var cluster = Environment.GetEnvironmentVariable("MONGODB_CLUSTER");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(cluster))
        {
            Console.WriteLine("Missing Required environment variables for MongooseDb credentials.Please enter the following line into the Terminal: source ~/.zshrc");
            return;
        }
        string connectionUri = $"mongodb+srv://{username}:{Uri.EscapeDataString(password)}@{cluster}/?retryWrites=true&w=majority&appName=Cluster44";
        var settings = MongoClientSettings.FromConnectionString(connectionUri);

        settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        var client = new MongoClient(settings);

        try
        {
            var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            Console.WriteLine($"Pinged your deployment. You succesfully connected to MongoDB at [{DateTime.Now}]!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}