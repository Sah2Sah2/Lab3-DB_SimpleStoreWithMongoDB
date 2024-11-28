using MongoDB.Driver;

public static class DatabaseHelper
{
    private static readonly string connectionString = "mongodb://localhost:27017/GroceryStore";
    private static readonly string databaseName = "GroceryStore";

    private static readonly MongoClient client = new MongoClient(connectionString);
    private static readonly IMongoDatabase database = client.GetDatabase(databaseName);

    public static IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return database.GetCollection<T>(collectionName);
    }
}
