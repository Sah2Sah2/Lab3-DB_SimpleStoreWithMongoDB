using MongoDB.Driver;

public static class DatabaseHelper
{
    private static readonly string connectionString = "mongodb+srv://sarabattistella2:DatabaseMongoDB@cluster2.dpzm7.mongodb.net/GroceryStore?retryWrites=true&w=majority";
    private static readonly string databaseName = "GroceryStore";

    private static readonly MongoClient client = new MongoClient(connectionString);
    private static readonly IMongoDatabase database = client.GetDatabase(databaseName);

    public static IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return database.GetCollection<T>(collectionName);
    }
}
