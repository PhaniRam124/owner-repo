using Microsoft.Data.Sqlite;

namespace DirectorFamilyTravelDesk.Data;

public static class SeedDatabase
{
    public static void Create(string databasePath, string seedSqlPath)
    {
        if (!File.Exists(seedSqlPath))
            throw new FileNotFoundException("Seed SQL file not found.", seedSqlPath);

        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys=OFF;\n" + File.ReadAllText(seedSqlPath) + "\nPRAGMA foreign_keys=ON;";
        command.ExecuteNonQuery();
    }
}