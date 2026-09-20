using System.Windows;
using CPRD.KnowledgeDesk.App.Services;
using CPRD.KnowledgeDesk.App.ViewModels;
using CPRD.KnowledgeDesk.App.Views;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CPRD.KnowledgeDesk.App;

public partial class App : Application
{
    // Core stability release candidate.
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
            collection.AddSingleton<RevisionRepository>();

            collection.AddSingleton<INoteService, NoteService>();
            collection.AddSingleton<IFolderService, FolderService>();
            collection.AddSingleton<ITagService, TagService>();
            collection.AddSingleton<ISearchService, SearchService>();
            collection.AddSingleton<IDashboardService, DashboardService>();
            collection.AddSingleton<IRevisionService, RevisionService>();
            collection.AddSingleton<IBackupService, BackupService>();
            collection.AddSingleton<IRecoveryService, RecoveryService>();
            collection.AddSingleton<IAppLogService, AppLogService>();
            collection.AddSingleton<IExportImportService, ExportImportService>();
            collection.AddSingleton<IDuplicateDetectionService, DuplicateDetectionService>();

            collection.AddSingleton<IDelayScheduler, SystemDelayScheduler>();
            collection.AddSingleton(sp => new AutoSaveCoordinator(
                sp.GetRequiredService<IDelayScheduler>(),
                TimeSpan.FromMilliseconds(750)));

            collection.AddSingleton<WorkspaceService>();
            collection.AddSingleton<KeyboardShortcutService>();
            collection.AddSingleton<StartupMaintenanceService>();
            collection.AddSingleton<EditorViewModel>();
            collection.AddSingleton<MainWindowViewModel>();
            collection.AddSingleton<MainWindow>();
            collection.AddTransient<QuickCaptureViewModel>();
            collection.AddTransient<QuickCaptureWindow>();

            _services = collection.BuildServiceProvider();
            RegisterGlobalExceptionLogging(_services.GetRequiredService<IAppLogService>());

            var db = _services.GetRequiredService<KnowledgeDb>();
            await db.InitializeAsync(CancellationToken.None);

            var maintenance = _services.GetRequiredService<StartupMaintenanceService>();
            await maintenance.RunAsync(CancellationToken.None);

            var vm = _services.GetRequiredService<MainWindowViewModel>();
            await vm.InitializeAsync();

            var window = _services.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            if (_services is not null)
                TryLog(_services.GetService<IAppLogService>(), AppLogLevel.Error, "Application startup failed.", ex);

            MessageBox.Show(
                $"CPRD Knowledge Desk could not start.\n\n{ex.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void RegisterGlobalExceptionLogging(IAppLogService log)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            TryLog(log, AppLogLevel.Error, "Unhandled UI exception.", args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            TryLog(
                log,
                AppLogLevel.Error,
                "Unhandled application-domain exception.",
                args.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            TryLog(log, AppLogLevel.Error, "Unobserved background task exception.", args.Exception);
            args.SetObserved();
        };
    }

    private static void TryLog(
        IAppLogService? log,
        AppLogLevel level,
        string message,
        Exception? exception)
    {
        if (log is null) return;

        try
        {
            log.LogAsync(level, message, exception, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }
        catch
        {
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
