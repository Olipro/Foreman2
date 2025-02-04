﻿using Foreman.DataCache;

using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Foreman.DataCache.DataTypes;

namespace Foreman {
	public partial class SaveFileLoadForm : Form
	{
		private readonly DCache DCache;
		private readonly HashSet<IDataObjectBase> EnabledObjects;
		public SaveFileInfo? SaveFileInfo;

		private readonly CancellationTokenSource cts;

		private readonly string DefaultSaveFileLocation;
		private string saveFilePath;
		private string factorioPath;

		public SaveFileLoadForm(DCache cache, HashSet<IDataObjectBase> enabledObjects)
		{
			DCache = cache;
			EnabledObjects = enabledObjects;
			SaveFileInfo = null;

			cts = new CancellationTokenSource();

			factorioPath = "";
			saveFilePath = "";

			InitializeComponent();

			//check for previous save file location and its validity (or set to "")
			DefaultSaveFileLocation = Properties.Settings.Default.LastSaveFileLocation;
			if (string.IsNullOrEmpty(DefaultSaveFileLocation))
				DefaultSaveFileLocation = "";
			string? tempUDirectory = DefaultSaveFileLocation;
			while (!string.IsNullOrEmpty(tempUDirectory) && !Path.GetFileName(tempUDirectory).Equals("saves", StringComparison.CurrentCultureIgnoreCase))
				tempUDirectory = Path.GetDirectoryName(tempUDirectory) ?? tempUDirectory;
			if (!string.IsNullOrEmpty(tempUDirectory))
				tempUDirectory = Path.GetDirectoryName(tempUDirectory) ?? tempUDirectory; //done one more time to get the actual user directory, not the saves folder
			if (!File.Exists(Path.Combine(tempUDirectory, "factorio-current.log")))
				DefaultSaveFileLocation = "";

			//check default folders for a factorio installation (to fill in the path as the 'default')
			//program files install
			if (string.IsNullOrEmpty(DefaultSaveFileLocation))
			{
				List<string> factorioInstallLocations = FactorioPathsProcessor.GetFactorioInstallLocations();
				if (factorioInstallLocations.Count > 0)
				{
					string userPath = FactorioPathsProcessor.GetFactorioUserPath(factorioInstallLocations[0], false);
					if (!string.IsNullOrEmpty(userPath))
						DefaultSaveFileLocation = Path.Combine(userPath, "saves");
				}
			}
		}

		private async void ProgressForm_Load(object? sender, EventArgs e)
		{
#if DEBUG
			DateTime startTime = DateTime.Now;
#endif
			using (OpenFileDialog dialog = new())
			{
				dialog.InitialDirectory = DefaultSaveFileLocation;
				dialog.Filter = "factorio saves (*.zip)|*.zip";
				dialog.FilterIndex = 1;
				dialog.RestoreDirectory = true;
				dialog.Multiselect = false;

				if (dialog.ShowDialog() == DialogResult.OK)
					saveFilePath = dialog.FileName;
				else
				{
					DialogResult = DialogResult.Cancel;
					SaveFileInfo = null;
					Close();
					return;
				}
			}

			var token = cts.Token;
			DialogResult = await LoadSaveFile(token); //OK: all good, data loaded, ABORT: error during loading, display error message, CANCEL: local error prior to load (message already displayed)
			if (DialogResult == DialogResult.OK)
				ProcessSaveData();
			Close();

#if DEBUG
			TimeSpan diff = DateTime.Now.Subtract(startTime);
			Console.WriteLine("Save file load time: " + Math.Round(diff.TotalSeconds, 2) + " seconds.");
			ErrorLogging.LogLine("Save file load time: " + Math.Round(diff.TotalSeconds, 2) + " seconds.");
#endif
		}

		private async Task<DialogResult> LoadSaveFile(CancellationToken token)
		{
			return await Task.Run(() =>
			{
				string modsPath = "";
				try
				{
					//get factorio path
					string userDataPath = saveFilePath;
					while (!string.IsNullOrEmpty(userDataPath) && !Path.GetFileName(userDataPath).Equals("saves", StringComparison.CurrentCultureIgnoreCase))
						userDataPath = Path.GetDirectoryName(userDataPath) ?? userDataPath;
					userDataPath = Path.GetDirectoryName(userDataPath) ?? userDataPath; //done one more time to get the actual user directory, not the saves folder

					string currentLog = Path.Combine(userDataPath, "factorio-current.log");
					string[] currentLogLines = File.ReadAllLines(currentLog);
					foreach (string line in currentLogLines)
					{
						if (line.Contains("Program arguments"))
						{
							factorioPath = line[(line.IndexOf('\"') + 1)..];
							factorioPath = factorioPath[..factorioPath.IndexOf('\"')];
						}
					}

                    //test factorio version
                    FileVersionInfo factorioVersionInfo = FileVersionInfo.GetVersionInfo(factorioPath);
                    if (factorioVersionInfo.ProductMajorPart < 2)
                    {
                        MessageBox.Show("Factorio Version below 2.0 can not be used with this version of Foreman. Please use Factorio 2.0 or newer. Alternatively download dev.13 or under of foreman 2.0 for pre factorio 2.0.");
                        ErrorLogging.LogLine(string.Format("Factorio version {0} instead of 2.x - use Foreman dev.13 or below for these factorio installs.", factorioVersionInfo.ProductVersion));
                        return DialogResult.Cancel;
                    } else
                    if (factorioVersionInfo.ProductMajorPart > 2)
                    {
                        MessageBox.Show("Factorio Version 3.x+ can not be used with this version of Foreman. Sit tight and wait for update...\nYou can also try to msg me on discord (u\\DanielKotes) if for some reason I am not already aware of this.");
                        ErrorLogging.LogLine(string.Format("Factorio version {0} isnt supported.", factorioVersionInfo.ProductVersion));
                        return DialogResult.Cancel;
                    } else if (factorioVersionInfo.ProductMinorPart < 0 || (factorioVersionInfo.ProductMinorPart == 0 && factorioVersionInfo.ProductBuildPart < 7))
                    {
                        MessageBox.Show("Factorio version (" + factorioVersionInfo.ProductVersion + ") can not be used with Foreman. Please use Factorio 2.0.7 or newer.");
                        ErrorLogging.LogLine(string.Format("Factorio version was too old. {0} instead of 2.0.7+", factorioVersionInfo.ProductVersion));
                        return DialogResult.Cancel;
                    }

					//copy the save reader mod to the mods folder
					modsPath = Path.Combine(userDataPath, "mods");
					if (!Directory.Exists(modsPath))
						Directory.CreateDirectory(modsPath);
					Directory.CreateDirectory(Path.Combine(modsPath, "foremansavereader_2.0.0"));
					try
					{

						File.Copy(Path.Combine(["Mods", "foremansavereader_2.0.0", "info.json"]), Path.Combine([modsPath, "foremansavereader_2.0.0", "info.json"]), true);
						File.Copy(Path.Combine(["Mods", "foremansavereader_2.0.0", "instrument-control.lua"]), Path.Combine([modsPath, "foremansavereader_2.0.0", "instrument-control.lua"]), true);
					}
					catch
					{
						MessageBox.Show("could not copy foreman save reader mod files (Mods/foremansavereader_2.0.0/) to the factorio mods folder. Reinstall foreman?");
						ErrorLogging.LogLine("copying of foreman save reader mod files failed.");
						return DialogResult.Abort;
					}

					//ensure that the foreman save reader mod is correctly added to the mod-list and is enabled
					string modListPath = Path.Combine(modsPath, "mod-list.json");
					JObject? modlist = null;
					if (!File.Exists(modListPath))
						modlist = [];
					else
						modlist = JObject.Parse(File.ReadAllText(modListPath));
					if (modlist["mods"] == null)
						modlist.Add("mods", new JArray());

					JToken? foremansavereaderModToken = modlist["mods"]?.ToList().FirstOrDefault(t => t["name"]?.Value<string>() is string name && name == "foremansavereader");
					if (foremansavereaderModToken == null)
						(modlist["mods"] as JArray)?.Add(new JObject() { { "name", "foremansavereader" }, { "enabled", true } });
					else
						foremansavereaderModToken["enabled"] = true;
					File.WriteAllText(modListPath, modlist.ToString(Formatting.Indented));

					//open the map with factorio and read the save file info (mods, technology, recipes)
					Process process = new();
					process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					process.StartInfo.FileName = factorioPath;
					process.StartInfo.Arguments = string.Format("--instrument-mod foremansavereader --benchmark \"{0}\" --benchmark-ticks 1 --benchmark-runs 1", Path.GetFileName(saveFilePath));
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.CreateNoWindow = true;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.RedirectStandardInput = true;
					process.Start();
					string resultString = "";
					while (!process.HasExited)
					{
						resultString += process.StandardOutput.ReadToEnd();
						if (token.IsCancellationRequested)
						{
							process.Close();
							if (Directory.Exists(Path.Combine(modsPath, "foremansavereader_2.0.0")))
								Directory.Delete(Path.Combine(modsPath, "foremansavereader_2.0.0"), true);
							return DialogResult.Cancel;
						}
						Thread.Sleep(100);
					}

					if (Directory.Exists(Path.Combine(modsPath, "foremansavereader_2.0.0")))
						Directory.Delete(Path.Combine(modsPath, "foremansavereader_2.0.0"), true);

					if (resultString.Contains("Is another instance already running?", StringComparison.CurrentCulture))
					{
						MessageBox.Show("File read could not be completed because this instance of Factorio is currently running. Please stop expanding the factory for just a brief moment...");
						return DialogResult.Cancel;
					}
					else if (!resultString.Contains("<<<END-EXPORT-P0>>>", StringComparison.CurrentCulture))
					{
#if DEBUG
						Console.WriteLine(resultString);
#endif
						ErrorLogging.LogLine("could not process save file due to export not completing. Mod issue?");
						return DialogResult.Abort;
					}
					//parse output
					string exportString = resultString[(resultString.IndexOf("<<<START-EXPORT-P0>>>") + 23)..];
					exportString = exportString[..(exportString.IndexOf("<<<END-EXPORT-P0>>>") - 1)];
					JObject export = JObject.Parse(exportString);

					SaveFileInfo = new SaveFileInfo();
					foreach (var objJToken in export["mods"]?.ToList() ?? [])
						SaveFileInfo.Mods.Add(objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"), objJToken["version"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"));
					foreach (var objJToken in export["technologies"]?.ToList() ?? [])
						SaveFileInfo.Technologies.Add(objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"), objJToken["enabled"]?.Value<bool>() ?? true);
					foreach (var objJToken in export["recipes"]?.ToList() ?? [])
						SaveFileInfo.Recipes.Add(objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"), objJToken["enabled"]?.Value<bool>() ?? true);

					Properties.Settings.Default.LastSaveFileLocation = Path.GetDirectoryName(saveFilePath);
					Properties.Settings.Default.Save();
					return DialogResult.OK;
				}
				catch
				{
					if (!string.IsNullOrEmpty(modsPath) && Directory.Exists(Path.Combine(modsPath, "foremanexport_2.0.0")))
						Directory.Delete(Path.Combine(modsPath, "foremanexport_2.0.0"), true);
					SaveFileInfo = null;
					return DialogResult.Abort;
				}
			});
		}

		private void ProcessSaveData()
		{
			if (SaveFileInfo is null)
				throw new InvalidOperationException("SaveFileInfo is null");
			int totalMods = DCache.IncludedMods.Count;
			string missingMods = "\nMissing Mods: ";
			string wrongVersionMods = "\nWrong Version Mods: ";
			string newMods = "\nAdded Mods: ";

			foreach (KeyValuePair<string, string> mod in DCache.IncludedMods)
			{
				if (mod.Key == "foremanexport" || mod.Key == "foremansavereader" || mod.Key == "core")
					continue;

				if (!SaveFileInfo.Mods.TryGetValue(mod.Key, out string? value))
					missingMods += mod.Key + ", ";
				else if (value != mod.Value)
					wrongVersionMods += mod.Key + ", ";
			}
			foreach (KeyValuePair<string, string> mod in SaveFileInfo.Mods)
			{
				if (mod.Key == "foremanexport" || mod.Key == "foremansavereader" || mod.Key == "core")
					continue;

				if (!DCache.IncludedMods.ContainsKey(mod.Key))
					newMods += mod.Key + ", ";
			}
			missingMods = missingMods[..^2];
			if (missingMods == "\nMissing Mods") missingMods = "";
			wrongVersionMods = wrongVersionMods[..^2];
			if (wrongVersionMods == "\nWrong Version Mods") wrongVersionMods = "";
			newMods = newMods[..^2];
			if (newMods == "\nAdded Mods") newMods = "";

			if (missingMods != "" || wrongVersionMods != "" || newMods != "")
				if (MessageBox.Show("selected save file mods do not match preset mods; out of {0} mods:" + missingMods + wrongVersionMods + newMods + "\nAre you sure you wish to use this save file?", "Save file mod inconsistencies found!", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
					return;

			//we will not be updating technology based on the read data. we will instead be updating the recipes based on their enabled status. This is due to the possibility that a recipe was 'manually' enabled outside of the default technology unlocks. Is this possible? I dont know.
			EnabledObjects.Clear();
			EnabledObjects.Add(DCache.PlayerAssembler);

			foreach (IRecipe recipe in DCache.Recipes.Values)
				if (recipe.Name.StartsWith("§§") || (SaveFileInfo.Recipes.ContainsKey(recipe.Name) && SaveFileInfo.Recipes[recipe.Name]))
					EnabledObjects.Add(recipe);

			//go through all the assemblers, beacons, and modules and add them to the enabled set if at least one of their associated items has at least one production recipe that is in the enabled set.
			foreach (IAssembler assembler in DCache.Assemblers.Values)
			{
				bool enabled = false;
				foreach (IReadOnlyCollection<IRecipe> recipes in assembler.AssociatedItems.Select(item => item.ProductionRecipes))
					foreach (IRecipe recipe in recipes)
						enabled |= EnabledObjects.Contains(recipe);
				if (enabled)
					EnabledObjects.Add(assembler);
			}

			foreach (IBeacon beacon in DCache.Beacons.Values)
			{
				bool enabled = false;
				foreach (IReadOnlyCollection<IRecipe> recipes in beacon.AssociatedItems.Select(item => item.ProductionRecipes))
					foreach (IRecipe recipe in recipes)
						enabled |= EnabledObjects.Contains(recipe);
				if (enabled)
					EnabledObjects.Add(beacon);
			}

			foreach (IModule module in DCache.Modules.Values)
			{
				bool enabled = false;
				foreach (IRecipe recipe in module.AssociatedItem.ProductionRecipes)
					enabled |= EnabledObjects.Contains(recipe);
				if (enabled)
					EnabledObjects.Add(module);
			}

		}

		private void CancellationButton_Click(object? sender, EventArgs e)
		{
			cts.Cancel();
			DialogResult = DialogResult.Cancel;
			SaveFileInfo = null;
			Close();
		}
	}
}
