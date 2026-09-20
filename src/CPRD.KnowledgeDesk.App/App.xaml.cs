using System.Windows;
using CPRD.KnowledgeDesk.App.ViewModels;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CPRD.KnowledgeDesk.App;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        try
        {
            var collection = new ServiceCollection();

            collection.AddSingleton<IAppPaths, AppPaths>();
            collection.AddSingleton<KnowledgeDb>();

            collection.AddSingleton<NoteRepository>();
            collection.AddSingleton<FolderRepository>();
            collection.AddSingleton<TagRepository>();
            collection.AddSingleton<SearchRepository>();

            collection.AddSingleton<INoteService, NoteService>();
            collection.AddSingleton<IFolderService, FolderService>();
            collection.AddSingleton<ITagService, TagService>();
            collection.AddSingleton<ISearchService, SearchService>();
            collection.AddSingleton<IDashboardService, DashboardService>();

            collection.AddSingleton<MainWindowViewModel>();
            collection.AddSingleton<MainWindow>();

            _services = collection.BuildServiceProvider();

            var db = _services.GetRequiredService<KnowledgeDb>();
            await db.InitializeAsync(CancellationToken.None);

            var vm = _services.GetRequiredService<MainWindowViewModel>();
            await vm.InitializeAsync();

            var window = _services.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"CPRD Knowledge Desk could not start.\n\n{ex.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
