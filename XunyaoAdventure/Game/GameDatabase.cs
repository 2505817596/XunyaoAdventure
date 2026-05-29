using LiteDB;

namespace XunyaoAdventure.Game;

public sealed class GameDatabase : IDisposable
{
    private readonly LiteDatabase _database;

    public GameDatabase(IWebHostEnvironment environment)
    {
        string dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "GameData");
        Directory.CreateDirectory(dataDirectory);

        string databasePath = Path.Combine(dataDirectory, "xunyao.db");
        _database = new LiteDatabase(new ConnectionString
        {
            Filename = databasePath,
            Connection = ConnectionType.Shared,
        });
    }

    public ILiteCollection<T> GetCollection<T>(string name)
        => _database.GetCollection<T>(name);

    public void Dispose()
        => _database.Dispose();
}
