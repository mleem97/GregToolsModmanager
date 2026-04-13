using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace GregModmanager.Steam;

/// <summary>
/// Loads <c>steam_api64.dll</c> before Facepunch/Steamworks runs so the process uses the same
/// native binary as Data Center: prefer <c>{GameRoot}/Data Center_Data/Plugins/x86_64/</c>, then the
/// copy shipped next to this executable.
/// </summary>
public static class SteamApiNativeLoader
{
	private const string DllFileName = "steam_api64.dll";
	private const string UnityDataFolderName = "Data Center_Data";
	private static IntPtr _module;

	/// <summary>
	/// Idempotent: loads the first existing candidate. Returns true if a module handle was obtained.
	/// </summary>
	public static bool TryPreload()
	{
		if (_module != IntPtr.Zero)
		{
			return true;
		}

		foreach (var path in EnumerateCandidatePaths())
		{
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				continue;
			}

			try
			{
				_module = NativeLibrary.Load(path);
				if (_module != IntPtr.Zero)
				{
					return true;
				}
			}
			catch
			{
				// try next
			}
		}

		try
		{
			_module = NativeLibrary.Load(DllFileName);
			return _module != IntPtr.Zero;
		}
		catch
		{
			return false;
		}
	}

	private static IEnumerable<string> EnumerateCandidatePaths()
	{
		var envRoot = Environment.GetEnvironmentVariable("DATA_CENTER_GAME_DIR")?.Trim();
		if (!string.IsNullOrEmpty(envRoot))
		{
			yield return Path.Combine(envRoot, UnityDataFolderName, "Plugins", "x86_64", DllFileName);
		}

		foreach (var path in EnumerateWalkingUpFrom(AppContext.BaseDirectory))
		{
			yield return path;
		}

		foreach (var gameRoot in EnumerateHeuristicGameRoots())
		{
			yield return Path.Combine(gameRoot, UnityDataFolderName, "Plugins", "x86_64", DllFileName);
		}

		var baseDir = AppContext.BaseDirectory;
		if (!string.IsNullOrEmpty(baseDir))
		{
			yield return Path.Combine(baseDir, DllFileName);
		}
	}

	private static IEnumerable<string> EnumerateWalkingUpFrom(string startDir)
	{
		string? dir;
		try
		{
			dir = Path.GetFullPath(startDir.Trim());
		}
		catch
		{
			yield break;
		}

		for (var i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
		{
			yield return Path.Combine(dir, UnityDataFolderName, "Plugins", "x86_64", DllFileName);
			try
			{
				dir = Path.GetDirectoryName(dir);
			}
			catch
			{
				yield break;
			}
		}
	}

	private static IEnumerable<string> EnumerateHeuristicGameRoots()
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		void Add(string? path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return;
			}

			try
			{
				var full = Path.GetFullPath(path.Trim());
				if (Directory.Exists(full))
				{
					seen.Add(full);
				}
			}
			catch
			{
				// ignored
			}
		}

		try
		{
			using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
			var installPath = key?.GetValue("InstallPath") as string;
			if (!string.IsNullOrEmpty(installPath))
			{
				Add(Path.Combine(installPath, "steamapps", "common", "Data Center"));
			}
		}
		catch
		{
			// ignored
		}

		Add(Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
			"Steam", "steamapps", "common", "Data Center"));

		foreach (var root in seen)
		{
			yield return root;
		}
	}
}

