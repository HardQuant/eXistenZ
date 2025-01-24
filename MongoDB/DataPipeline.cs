using MongoDB.Driver;

public class DataPipeline
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting DataPipeline...");

        var pingProgram = new PingProgram();
        MongoClient? client = pingProgram.InitializeMongoClient();

        if (client != null)
        {
            Console.WriteLine("MongoDB client initialized.");
            var database = client.GetDatabase("F_1");

            Console.WriteLine("Skipping Drivers and Constructors logic...");
            /*
            var drivers = new Drivers(database);
            await drivers.LoadDataIntoMongoDB(1986, 2024);

            var constructors = new Constructors(database);
            await constructors.LoadDataIntoMongoDB(1986, 2024);
            */

            Console.WriteLine("Attempting to load Circuits...");
            var circuits = new Circuits(database);
            await circuits.LoadDataIntoMongoDB(1986, 2024);
            Console.WriteLine("Circuits logic executed successfully.");
        }
        else
        {
            Console.WriteLine("Failed to initialize MongoDB client. ETL process cannot proceed.");
        }
    }
}
