using Microsoft.Extensions.Logging;
using GregModmanager.Localization;
using GregModmanager.Services;
using GregModmanager.Steam;

namespace GregModmanager;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		AppFileLog.StartSession();
		AppDomain.CurrentDomain.ProcessExit += (_, _) => AppFileLog.EndSession();
		AppFileLog.Info("CreateMauiApp entry");
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
			AppFileLog.MarkCrash("AppDomain.UnhandledException", ex);
			AppFileLog.Error($"UnhandledException (terminating={e.IsTerminating})", ex);
			DebugSessionLog.Write("H1", "MauiProgram.UnhandledException", "unhandled", new
			{
				e.IsTerminating,
				message = ex?.Message,
				stack = ex?.StackTrace,
			});
		};
		TaskScheduler.UnobservedTaskException += (_, e) =>
		{
			AppFileLog.MarkCrash("TaskScheduler.UnobservedTaskException", e.Exception);
			AppFileLog.Error("UnobservedTaskException", e.Exception);
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
				try
				{
					var webViewDataDir = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
						"gregModmanager",
						"webview2");
					Directory.CreateDirectory(webViewDataDir);
					Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", webViewDataDir);
					AppFileLog.Info($"WEBVIEW2_USER_DATA_FOLDER={webViewDataDir}");
				}
				catch (Exception ex)
				{
					AppFileLog.Error("Failed to configure WebView2 user data folder.", ex);
				}

				var baseDir = AppContext.BaseDirectory;
				if (!string.IsNullOrEmpty(baseDir))
				{
					if (IsDirectoryWritable(baseDir))
					{
						Directory.SetCurrentDirectory(baseDir);
						AppFileLog.Info($"CurrentDirectory set to: {baseDir}");
					}
					else
					{
						AppFileLog.Warn($"Skip SetCurrentDirectory for non-writable path: {baseDir}");
					}
				}
			}
			catch
			{
				// ignored — Steam init may still work if cwd is already correct
			}

			var steamOk = SteamApiNativeLoader.TryPreload();
			AppFileLog.Info($"SteamApiNativeLoader.TryPreload={steamOk}");
			// #region agent log
			DebugNdjsonSessionLog.Write("H4", "MauiProgram.CreateMauiApp", "after_steam_preload", new { steamOk });
			// #endregion

			if (GregModmanager.Services.Auth.ProtocolSingleInstance.ShouldForwardAndExitAsync(Environment.GetCommandLineArgs()).GetAwaiter().GetResult())
			{
					Environment.Exit(0);
			}

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
			builder.Services.AddSingleton<ReproBundleService>();
			builder.Services.AddSingleton<SteamWorkshopService>();
			GregModmanager.Steam.SteamApiNativeLoader.SetGameRoot(SettingsPage.GetGameRootPath());
			builder.Services.AddSingleton<WorkspaceService>();
			builder.Services.AddSingleton<ModDependencyService>();
			builder.Services.AddSingleton<gregPluginChannelRegistry>(sp =>
			{
				var registry = new gregPluginChannelRegistry();
				registry.Register(new StablePluginSource(sp.GetRequiredService<ModDependencyService>()));
				registry.Register(new BetaPluginSource());
				registry.Register(new GitHubModSource());
				return registry;
			});
			builder.Services.AddSingleton<RalphSyncService>();
			builder.Services.AddSingleton<VdfGeneratorService>();
			builder.Services.AddSingleton<WorkshopDownloadService>();
			builder.Services.AddSingleton<ModsFolderSyncService>();
			builder.Services.AddSingleton<SubscriptionPoller>(sp =>
				new SubscriptionPoller(sp.GetRequiredService<SteamWorkshopService>()));
			builder.Services.AddSingleton<WorkshopSyncOrchestrator>();
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
                        builder.Services.AddSingleton<GitVerificationService>();
                        builder.Services.AddSingleton<BetterAuthService>();

                        // Auth / Session
                        builder.Services.AddSingleton<GregModmanager.Services.Auth.IAuthApiClient, GregModmanager.Services.Auth.AuthApiClient>();
                        builder.Services.AddSingleton<GregModmanager.Services.Auth.ISessionManager, GregModmanager.Services.Auth.SessionManager>();
                        builder.Services.AddSingleton<GregModmanager.Services.Install.IInstallIntentClient, GregModmanager.Services.Install.InstallIntentClient>();



			// #region agent log
			DebugNdjsonSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "before_build", null);
			DebugSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "before_build", null);
			// #endregion

			var app = builder.Build();
			AppFileLog.Info("CreateMauiApp success");

			// #region agent log
			DebugNdjsonSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "after_build", new { ok = true });
			DebugSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "after_build", null);
			// #endregion

			return app;
		}
		catch (Exception ex)
		{
			AppFileLog.Error("CreateMauiApp exception", ex);
			// #region agent log
			DebugNdjsonSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "exception", new { ex.Message, exType = ex.GetType().FullName, ex.StackTrace });
			DebugSessionLog.Write("H1", "MauiProgram.CreateMauiApp", "exception", new { ex.Message, ex.StackTrace });
			// #endregion
			throw;
		}
	}

	private static bool IsDirectoryWritable(string directoryPath)
	{
		try
		{
			var testFile = Path.Combine(directoryPath, $".write-test-{Environment.ProcessId}.tmp");
			File.WriteAllText(testFile, "ok");
			File.Delete(testFile);
			return true;
		}
		catch
		{
			return false;
		}
	}
}


