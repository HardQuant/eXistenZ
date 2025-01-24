using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;

public class RaceSchedule
{
    private readonly IMongoCollection<BsonDocument> _raceScheduleCollection;

    public RaceSchedule(IMongoDatabase database)
    {
        _raceScheduleCollection = database.GetCollection<BsonDocument>("RaceSchedule");
    }

    public async Task LoadDataIntoMongoDB(int startYear, int endYear)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                for (int year = startYear; year <= endYear; year++)
                {
                    string apiUrl = $"http://ergast.com/api/f1/{year}.json";
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JObject data = JObject.Parse(jsonResponse);

                        var races = data["MRData"]["RaceTable"]["Races"];
                        Console.WriteLine($"Processing race schedule for season {year}...");

                        foreach (var race in races)
                        {
                            string season = race["season"].ToString();
                            string raceName = race["raceName"].ToString();
                            string round = race["round"].ToString();

                            string raceScheduleID = $"{season}_{raceName}_Round {round}";

                            var document = new BsonDocument
                            {
                                { "RaceScheduleID", raceScheduleID },
                                { "Race date", race["date"]?.ToString() ?? string.Empty },
                                { "Race time", race["time"]?.ToString() ?? string.Empty },
                                { "circuitId", race["Circuit"]["circuitId"]?.ToString() ?? string.Empty },
                                { "circuitName", race["Circuit"]["circuitName"]?.ToString() ?? string.Empty },
                                { "locality", race["Circuit"]["Location"]["locality"]?.ToString() ?? string.Empty },
                                { "country", race["Circuit"]["Location"]["country"]?.ToString() ?? string.Empty },
                                { "FirstPractice", new BsonDocument
                                    {
                                        { "date", race["FirstPractice"]?["date"]?.ToString() ?? (year < 2022 ? null : string.Empty) },
                                        { "time", race["FirstPractice"]?["time"]?.ToString() ?? (year < 2022 ? null : string.Empty) }
                                    }
                                },
                                { "SecondPractice", new BsonDocument
                                    {
                                        { "date", race["SecondPractice"]?["date"]?.ToString() ?? (year < 2022 ? null : string.Empty) },
                                        { "time", race["SecondPractice"]?["time"]?.ToString() ?? (year < 2022 ? null : string.Empty) }
                                    }
                                },
                                { "ThirdPractice", new BsonDocument
                                    {
                                        { "date", race["ThirdPractice"]?["date"]?.ToString() ?? (year < 2022 ? null : string.Empty) },
                                        { "time", race["ThirdPractice"]?["time"]?.ToString() ?? (year < 2022 ? null : string.Empty) }
                                    }
                                },
                                { "Qualifying", new BsonDocument
                                    {
                                        { "date", race["Qualifying"]?["date"]?.ToString() ?? (year < 2022 ? null : string.Empty) },
                                        { "time", race["Qualifying"]?["time"]?.ToString() ?? (year < 2022 ? null : string.Empty) }
                                    }
                                },
                                { "Sprint", race["Sprint"] != null && race["Sprint"].HasValues 
                                    ? new BsonDocument
                                        {
                                            { "date", race["Sprint"]["date"]?.ToString() ?? string.Empty },
                                            { "time", race["Sprint"]["time"]?.ToString() ?? string.Empty }
                                        }
                                    : BsonNull.Value
                                }
                            };

                            var filter = Builders<BsonDocument>.Filter.Eq("RaceScheduleID", raceScheduleID);
                            var existingRace = await _raceScheduleCollection.Find(filter).FirstOrDefaultAsync();

                            if (existingRace == null)
                            {
                                await _raceScheduleCollection.InsertOneAsync(document);
                                Console.WriteLine($"Inserted: {raceScheduleID}");
                            }
                            else
                            {
                                Console.WriteLine($"Skipped (exists): {raceScheduleID}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to fetch data for season {year}. Status code: {response.StatusCode}");
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
