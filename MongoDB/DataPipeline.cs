using System;
using System.Threading.Tasks;
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
            // Corrected usage of client to get the database
            var database = client.GetDatabase("F_1");

            // Step 2: Instantiate each collection class to Extract, Transform, and Load data
            var drivers = new Drivers(database);
            await drivers.LoadDataIntoMongoDB(1986, 2024);

            // Add more collection classes as needed
        }
        else 
        {
            Console.WriteLine("Failed to initialize MongoDB client. ETL process cannot proceed.");
        }
    }
}