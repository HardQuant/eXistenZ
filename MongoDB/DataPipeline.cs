using MongoDB.Driver;

public class DataPipeline
{
    public static async Task Main(string[] args)
    {
        // Step 1: Initialize and execute PingProgram to connect to MongoDB
        var pingProgram = new PingProgram();
        MongoClient? client = pingProgram.InitializeMongoClient();

        if (client != null)
        {
            // Correct usage of client to get the database
            var database = client.GetDatabase("F_1");

            // Step 2: Instantiate Drivers and Constructors collection classes
            var drivers = new Drivers(database);
            await drivers.LoadDataIntoMongoDB(1986, 2024);

            var constructors = new Constructors(database);
            await constructors.LoadDataIntoMongoDB(1986, 2024);

            // Add more collection classes as needed
        }
        else
        {
            Console.WriteLine("Failed to initialize MongoDB client. ETL process cannot proceed.");
        }
    }
}
