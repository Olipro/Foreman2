using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Foreman.DataCache {
	public static class FactorioPathsProcessor {
		public static List<string> GetFactorioInstallLocations() {
			//check default folders for a factorio installation (to fill in the path as the 'default')
			List<string> factorioPaths = [];

			//program files install
			string pfConfigPath = Path.Combine(["c:\\", "Program Files", "Factorio", "config-path.cfg"]);
			if (File.Exists(pfConfigPath) && Path.GetDirectoryName(pfConfigPath) is string dirName)
				factorioPaths.Add(dirName);

			//steam
			object? steamPathA = Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "SteamPath", "");
			object? steamPathB = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Valve\\Steam", "SteamPath", "");
			string steamPath = steamPathA != null && !string.IsNullOrEmpty((string)steamPathA) ? (string)steamPathA : steamPathB != null && !string.IsNullOrEmpty((string)steamPathB) ? (string)steamPathB : "";
			if (!string.IsNullOrEmpty(steamPath)) {
				string libraryFoldersFilePath = Path.Combine([steamPath, "steamapps", "libraryfolders.vdf"]);
				if (File.Exists(libraryFoldersFilePath)) {
					string[] steamLSettings = File.ReadAllLines(libraryFoldersFilePath);
					foreach (string line in steamLSettings) {
						if (line.Contains("\"path\"")) {
							string libraryPath = line[..line.LastIndexOf('\"')];
							libraryPath = libraryPath[(libraryPath.LastIndexOf('\"') + 1)..];
							string factorioConfigPath = Path.Combine([libraryPath, "steamapps", "common", "Factorio", "config-path.cfg"]);
							if (File.Exists(factorioConfigPath) && Path.GetDirectoryName(factorioConfigPath) is string fcDirName)
								factorioPaths.Add(fcDirName);
						}
					}
				}
			}

			return factorioPaths;
		}

		public static string GetFactorioUserPath(string installPath, bool verboseFail = false) {
			//find config-path.cfg, read it, and use it to find config.ini
			string configPath = Path.Combine(installPath, "config-path.cfg");
			if (!File.Exists(configPath)) {
				if (verboseFail)
					MessageBox.Show("config-path.cfg missing from the install location. Maybe run Factorio once to ensure all files are there?\nAlternatively a reinstall might be required.");
				ErrorLogging.LogLine(string.Format("config-path.cfg was not found at {0}. this was supposed to be the install folder", installPath));
				return "";
			}

			string config = File.ReadAllText(configPath);
			string configIniPath = Path.Combine(ProcessPathString(config[12..config.IndexOf('\n')], installPath), "config.ini");

			//read config.ini file
			if (!File.Exists(configIniPath)) {
				if (verboseFail)
					MessageBox.Show("config.ini could not be found. Factorio setup is corrupted?");
				ErrorLogging.LogLine(string.Format("config.ini file was not found at {0}. config-path.cfg was at {1} and linked here.", configIniPath, configPath));
				return "";
			}
			string[] configIni = File.ReadAllLines(configIniPath);
			string writePath = "";
			foreach (string line in configIni)
				if (line.Contains("write-data", StringComparison.CurrentCulture) && !line.StartsWith(';'))
					writePath = line[(line.IndexOf("write-data") + 11)..];

			return ProcessPathString(writePath, installPath);
		}

		private static string ProcessPathString(string input, string installPath) {
			if (input.StartsWith(".factorio")) {
				string path = installPath;
				string folder = input == ".factorio" ? "" : input[9..].Replace("/", "\\");
				if (folder.Length > 0) folder = folder[1..];
				while (folder.Contains("..", StringComparison.CurrentCulture)) {
					path = Path.GetDirectoryName(path) ?? throw new InvalidOperationException(path ?? "<null>" + " is not a valid directory");
					folder = folder[(folder.IndexOf("..") + 2)..];
					if (folder.Length > 0) folder = folder[1..];
				}
				return string.IsNullOrEmpty(folder) ? path : Path.Combine(path, folder);
			} else if (input.StartsWith("__PATH__executable__")) {
				string path = Path.Combine([installPath, "bin", "x64"]);
				string folder = input.Equals("__PATH__executable__") ? "" : input[20..].Replace("/", "\\");
				if (folder.Length > 0) folder = folder[1..];
				while (folder.Contains("..", StringComparison.CurrentCulture)) {
					path = Path.GetDirectoryName(path) ?? throw new InvalidOperationException(path ?? "<null>" + " is not a valid directory");
					folder = folder[(folder.IndexOf("..") + 2)..];
					if (folder.Length > 0) folder = folder[1..];
				}
				return string.IsNullOrEmpty(folder) ? path : Path.Combine(path, folder);
			} else if (input.StartsWith("__PATH__system-write-data__")) {
				string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("/", "\\");
				string folder = input.Equals("__PATH__system-write-data__") ? "" : input[27..].Replace("/", "\\");
				if (folder.Length > 0) folder = folder[1..];
				while (folder.Contains("..", StringComparison.CurrentCulture)) {
					path = Path.GetDirectoryName(path) ?? throw new InvalidOperationException(path ?? "<null>" + " is not a valid directory");
					folder = folder[(folder.IndexOf("..") + 2)..];
					if (folder.Length > 0) folder = folder[1..];
				}
				return string.IsNullOrEmpty(folder) ? Path.Combine(path, "Factorio") : Path.Combine([path, "Factorio", folder]);
			} else
				ErrorLogging.LogLine("path string (from one of the config files) did not start as expected (.factorio || __PATH__executable__ || __PATH__system-write-data__). Path string:" + input);

			return installPath; //something weird must have happened to end up here. Honesty these path conversions are a bit of a mess - not enough examples to be sure its correct (works with all case 'I' have...)
		}

	}
}
