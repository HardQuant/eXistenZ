using System;
using System.Threading.Tasks;

public class DataPipeline 
{
    public static asynch Task Main(string[] args)
    {
        var pingProgram = new PingProgram();
        MongoClient? client = pingProgram.InitializeMongoClient();

        if (client != null)
        {
            var database = new client.GetDatabase("F_1");

            //Instantiating each collection class to Extract, Transfor, and Load

        }



    }
}