using System.IO;
using System.Windows;
using DirectorFamilyTravelDesk.Data;

namespace DirectorFamilyTravelDesk;

public partial class App : Application
{
    public static TravelRepository Repository { get; private set; } = null!;
    public static string DataDirectory { get; private set; } = string.Empty;
    public static string DatabasePath { get; private set; } = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CPRD", "DirectorFamilyTravelDesk");
        Directory.CreateDirectory(DataDirectory);
        DatabasePath = Path.Combine(DataDirectory, "travel.db");
        if (!File.Exists(DatabasePath))
        {
            var seedSql = Path.Combine(AppContext.BaseDirectory, "Seed", "seed.sql");
            SeedDatabase.Create(DatabasePath, seedSql);
        }
        Repository = new TravelRepository(DatabasePath);
        Repository.Initialize();
        base.OnStartup(e);
    }
}