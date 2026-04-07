using Microsoft.Extensions.Logging;
using WorkshopUploader.Localization;
using WorkshopUploader.Services;
using WorkshopUploader.Steam;

namespace WorkshopUploader;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// #region agent log
		DebugNdjsonSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "entry", new
		{
			baseDir = AppContext.BaseDirectory,
			tempLog = DebugNdjsonSessionLog.LogPath,
			args = Environment.GetCommandLineArgs(),
		});
		// #endregion
		S.ApplySavedCulture();
		// #region agent log
		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
		{
			var ex = e.ExceptionObject as Exception;
			DebugSessionLog.Write("H1", "MauiProgram.UnhandledException", "unhandled", new
			{
				e.IsTerminating,
				message = ex?.Message,
				stack = ex?.StackTrace,
			});
		};
		TaskScheduler.UnobservedTaskException += (_, e) =>
		{
			DebugSessionLog.Write("H1", "MauiProgram.UnobservedTaskException", "unobserved", new
			{
				e.Observed,
				message = e.Exception?.Message,
				stack = e.Exception?.StackTrace,
			});
			e.SetObserved();
		};
		// #endregion

		try
		{
			// #region agent log
			DebugSessionLog.Write("META", "MauiProgram.CreateMauiApp", "log_path", new { path = DebugSessionLog.LogFilePath });
			DebugSessionLog.Write("H4", "MauiProgram.CreateMauiApp", "entry", new
			{
				baseDir = AppContext.BaseDirectory,
				args = Environment.GetCommandLineArgs(),
			});
			// #endregion

			try
			{
				var baseDir = AppContext.BaseDirectory;
				if (!string.IsNullOrEmpty(baseDir))
				{
					Directory.SetCurrentDirectory(baseDir);
				}
			}
			catch
			{
				// ignored — Steam init may still work if cwd is already correct
			}

			var steamOk = SteamApiNativeLoader.TryPreload();
			// #region agent log
			DebugNdjsonSessionLog.Write("H4", "MauiProgram.CreateMauiApp", "after_steam_preload", new { steamOk });
			// #endregion

			if (HeadlessRunner.TryHandle(Environment.GetCommandLineArgs(), out var exitCode))
			{
				// #region agent log
				DebugNdjsonSessionLog.Write("H3", "MauiProgram.CreateMauiApp", "headless_exit", new { exitCode });
				DebugSessionLog.Write("H4", "MauiProgram.CreateMauiApp", "headless_exit", new { exitCode });
				// #endregion
				Environment.Exit(exitCode);
				throw new InvalidOperationException("Unreachable: process should have exited.");
			}

			// #region agent log
			DebugSessionLog.Write("H4", "MauiProgram.CreateMauiApp", "gui_path", new { message = "not headless" });
			// #endregion

			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>();

			builder.Services.AddSingleton<AppLogService>();
			builder.Services.AddSingleton<SteamWorkshopService>();
			builder.Services.AddSingleton<WorkspaceService>();
			builder.Services.AddSingleton<ModDependencyService>();
			builder.Services.AddSingleton<FfmPluginChannelRegistry>(sp =>
			{
				var registry = new FfmPluginChannelRegistry();
				registry.Register(new StablePluginSource(sp.GetRequiredService<ModDependencyService>()));
				registry.Register(new BetaPluginSource());
				return registry;
			});
			builder.Services.AddSingleton<RalphSyncService>();
			builder.Services.AddSingleton<VdfGeneratorService>();
			builder.Services.AddSingleton<ProjectsPage>();
			builder.Services.AddSingleton<NewProjectPage>();
			builder.Services.AddSingleton<MyUploadsPage>();
			builder.Services.AddSingleton<ModManagerPage>();
			builder.Services.AddSingleton<SettingsPage>();
			builder.Services.AddTransient<EditorPage>();
			builder.Services.AddTransient<NativeConfigEditorPage>(sp =>
				new NativeConfigEditorPage(sp.GetRequiredService<WorkspaceService>()));
			builder.Services.AddTransient<ItemDetailPage>();
			builder.Services.AddSingleton<AppShell>();

#if DEBUG
			builder.Logging.AddDebug();
#endif

			// #region agent log
			DebugNdjsonSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "before_build", null);
			DebugSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "before_build", null);
			// #endregion

			var app = builder.Build();

			// #region agent log
			DebugNdjsonSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "after_build", new { ok = true });
			DebugSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "after_build", null);
			// #endregion

			return app;
		}
		catch (Exception ex)
		{
			// #region agent log
			DebugNdjsonSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "exception", new { ex.Message, exType = ex.GetType().FullName, ex.StackTrace });
			DebugSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "exception", new { ex.Message, ex.StackTrace });
			// #endregion
			throw;
		}
	}
}
