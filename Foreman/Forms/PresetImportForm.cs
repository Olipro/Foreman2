﻿using Foreman.DataCache;

using System;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

namespace Foreman {
	public partial class PresetImportForm : Form
	{
		private readonly char[] ExtraChars = ['(', ')', '-', '_', '.', ' '];
		private readonly CancellationTokenSource cts;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string NewPresetName { get; private set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool ImportStarted { get; private set; }

		public PresetImportForm()
		{
			NewPresetName = "";
			ImportStarted = false;
			cts = new CancellationTokenSource();
			InitializeComponent();
			PresetNameTextBox.Focus();

			FactorioLocationComboBox.Items.AddRange([.. FactorioPathsProcessor.GetFactorioInstallLocations()]);
			if (FactorioLocationComboBox.Items.Count > 0)
				FactorioLocationComboBox.SelectedIndex = 0;
		}

		private void EnableProgressBar(bool enabled)
		{
			this.SuspendLayout();
			ImportProgressBar.Visible = enabled;
			CancelImportButtonB.Visible = enabled;
			CancelImportButtonB.Focus();

			FactorioLocationGroup.Enabled = !enabled;
			FactorioSettingsGroup.Enabled = !enabled;
			PresetNameGroup.Enabled = !enabled;

			OKButton.Visible = !enabled;
			OKButton.Enabled = !enabled;
			CancelImportButton.Visible = !enabled;
			this.ResumeLayout();
		}

		private void FactorioBrowseButton_Click(object? sender, EventArgs e)
		{
			using FolderBrowserDialog dialog = new();
			if (Directory.Exists(FactorioLocationComboBox.Text))
				dialog.SelectedPath = FactorioLocationComboBox.Text;

			if (dialog.ShowDialog() == DialogResult.OK) {
				if (File.Exists(Path.Combine([dialog.SelectedPath, "bin", "x64", "factorio.exe"])))
					FactorioLocationComboBox.Text = dialog.SelectedPath;
				else if (File.Exists(Path.Combine([dialog.SelectedPath, "x64", "factorio.exe"])))
					FactorioLocationComboBox.Text = Path.GetDirectoryName(dialog.SelectedPath);
				else if (File.Exists(Path.Combine(dialog.SelectedPath, "factorio.exe")))
					FactorioLocationComboBox.Text = Path.GetDirectoryName(Path.GetDirectoryName(dialog.SelectedPath));
				else
					MessageBox.Show("Selected directory doesnt seem to be a factorio install folder (it should at the very least have \"bin\" and \"data\" folders, along with a \"config-path.cfg\" file)");
			}
		}

		private void ModsBrowseButton_Click(object? sender, EventArgs e)
		{
			using FolderBrowserDialog dialog = new();
			if (Directory.Exists(ModsLocationComboBox.Text))
				dialog.SelectedPath = ModsLocationComboBox.Text;

			if (dialog.ShowDialog() == DialogResult.OK) {
				if (File.Exists(Path.Combine(dialog.SelectedPath, "mod-list.json")))
					ModsLocationComboBox.Text = dialog.SelectedPath;
				else
					MessageBox.Show("Selected directory doesnt seem to be a factorio mods folder (it should at the very least have \"mod-list.json\" file)");
			}
		}

		private void CancelButton_Click(object? sender, EventArgs e)
		{
			cts.Cancel();
			DialogResult = DialogResult.Cancel;
			NewPresetName = "";
			Close();
		}

		private async void OKButton_Click(object? sender, EventArgs e)
		{
			NewPresetName = PresetNameTextBox.Text;
			if (!Directory.Exists(FactorioLocationComboBox.Text))
			{
				MessageBox.Show("That directory doesn't seem to exist");
				CleanupFailedImport();
				return;
			}
			if (NewPresetName.Length < 5)
			{
				MessageBox.Show("Preset name has to be longer than 5!");
				CleanupFailedImport();
				return;
			}

			List<Preset> existingPresets = MainForm.GetValidPresetsList();
			if(NewPresetName.Equals(MainForm.DefaultPreset, StringComparison.CurrentCultureIgnoreCase))
			{
				MessageBox.Show("Cant overwrite default preset!", "", MessageBoxButtons.OK);
				CleanupFailedImport();
				return;
			}
			else if (existingPresets.Any(p => p.Name.Equals(NewPresetName, StringComparison.CurrentCultureIgnoreCase)))
			{
				if (MessageBox.Show("This preset name is already in use. Do you wish to overwrite?", "Confirm Overwrite", MessageBoxButtons.YesNo) != DialogResult.Yes)
				{
					CleanupFailedImport();
					return;
				}
			}

			EnableProgressBar(true);

			string installPath = FactorioLocationComboBox.Text;
			//quick check to ensure the install path is correct (and accept a direct path to the factorio.exe folder just in case)
			if (!File.Exists(Path.Combine([installPath, "bin", "x64", "factorio.exe"])))
				if (File.Exists(Path.Combine(installPath, "factorio.exe")) && Path.GetDirectoryName(installPath) is string installDir)
					installPath = Path.Combine(installDir, @"..\\..\\");

			if (!File.Exists(Path.Combine([installPath, "bin", "x64", "factorio.exe"])))
			{
				EnableProgressBar(false);
				MessageBox.Show("Couldnt find factorio.exe (/bin/x64/factorio.exe) - please select a valid Factorio install location");
				CleanupFailedImport();
				return;
			}

			FileVersionInfo factorioVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine([installPath, "bin", "x64", "factorio.exe"]));
			if(factorioVersionInfo.ProductMajorPart < 2)
			{
                EnableProgressBar(false);
                MessageBox.Show("Factorio Version below 2.0 can not be used with this version of Foreman. Please use Factorio 2.0 or newer. Alternatively download dev.13 or under of foreman 2.0 for pre factorio 2.0.");
                ErrorLogging.LogLine(string.Format("Factorio version {0} instead of 2.x - use Foreman dev.13 or below for these factorio installs.", factorioVersionInfo.ProductVersion));
                CleanupFailedImport();
                return;
            } else
			if(factorioVersionInfo.ProductMajorPart > 2)
			{
                EnableProgressBar(false);
                MessageBox.Show("Factorio Version 3.x+ can not be used with this version of Foreman. Sit tight and wait for update...\nYou can also try to msg me on discord (u\\DanielKotes) if for some reason I am not already aware of this.");
                ErrorLogging.LogLine(string.Format("Factorio version {0} isnt supported.", factorioVersionInfo.ProductVersion));
                CleanupFailedImport();
                return;
            }
			else if (factorioVersionInfo.ProductMinorPart < 0 || (factorioVersionInfo.ProductMinorPart == 0 && factorioVersionInfo.ProductBuildPart < 7))
			{
				EnableProgressBar(false);
				MessageBox.Show("Factorio version (" + factorioVersionInfo.ProductVersion + ") can not be used with Foreman. Please use Factorio 2.0.7 or newer.");
				ErrorLogging.LogLine(string.Format("Factorio version was too old. {0} instead of 2.0.7+", factorioVersionInfo.ProductVersion));
				CleanupFailedImport();
				return;
			}

			string modsPath = ModsLocationComboBox.Text;
			if (string.IsNullOrEmpty(modsPath) || !File.Exists(Path.Combine(modsPath, "mod-list.json")))
			{
				string userDataPath = FactorioPathsProcessor.GetFactorioUserPath(installPath, true);
				if (string.IsNullOrEmpty(userDataPath))
				{
					MessageBox.Show("Couldnt auto-locate the mods folder - please manually locate the folder");
					CleanupFailedImport();
					return;
				}
				modsPath = Path.Combine(userDataPath, "mods");
			}

			//we now have the two paths to use - installPath and modsPath. can begin processing Factorio
			var progress = new Progress<KeyValuePair<int, string>>(value =>
			{
				if (value.Key > ImportProgressBar.Value)
					ImportProgressBar.Value = value.Key;
				if (!String.IsNullOrEmpty(value.Value) && value.Value != ImportProgressBar.CustomText)
					ImportProgressBar.CustomText = value.Value;
			}) as IProgress<KeyValuePair<int, string>>;
			var token = cts.Token;

#if DEBUG
			Stopwatch stopwatch = new();
			stopwatch.Start();
#endif
			ImportStarted = true;
			string foremanModName = "foremanexport_"+ factorioVersionInfo.ProductMajorPart + ".0.0";
			NewPresetName = await ProcessPreset(installPath, foremanModName, modsPath, progress, token);
#if DEBUG
			Console.WriteLine(string.Format("Preset import time: {0} seconds.", (stopwatch.ElapsedMilliseconds / 1000).ToString("0.0")));
			ErrorLogging.LogLine(string.Format("Preset import time: {0} seconds.", (stopwatch.ElapsedMilliseconds / 1000).ToString("0.0")));
#endif

			if (!string.IsNullOrEmpty(NewPresetName))
			{
				DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				//CleanupFailedImport(); //should have already been done.
				EnableProgressBar(false);
			}

		}

		private async Task<string> ProcessPreset(string installPath, string foremanModName, string modsPath, IProgress<KeyValuePair<int, string>> progress, CancellationToken token)
		{
			return await Task.Run(() =>
			{
				//prepare for running factorio
				string exePath = Path.Combine([installPath, "bin", "x64", "factorio.exe"]);
				string presetPath = Path.Combine([Application.StartupPath, "Presets", NewPresetName]);
				if (!File.Exists(exePath))
				{
					MessageBox.Show("factorio.exe not found..."); //considering that we got here with factorio.exe checks, this is a bit redundant. but whatevs.
					CleanupFailedImport();
					return "";
				}
				//ensure mod path exists and doesnt have the foreman export mod in it
				try
				{
					if (!Directory.Exists(modsPath))
						Directory.CreateDirectory(modsPath);
					if (Directory.Exists(Path.Combine(modsPath, foremanModName)))
						Directory.Delete(Path.Combine(modsPath, foremanModName));
				}
				catch (Exception e)
				{
					if (e is UnauthorizedAccessException)
					{
						MessageBox.Show("Insufficient access to the factorio mods folder. Please ensure factorio mods are in an accessible folder, or launch Foreman with Administrator privileges.");
						ErrorLogging.LogLine("insufficient access to factorio mods folder E: " + e.ToString());
					}
					else
					{
						MessageBox.Show("Unknown error trying to access factorio mods folder. Sorry");
						ErrorLogging.LogLine("Error while accessing factorio mods folder E:" + e.ToString());
					}
					CleanupFailedImport(modsPath);
					return "";
				}

				//launch factorio to create the temporary save we will use for export (LAUNCH #1)
				Process process = new();
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.StartInfo.FileName = exePath;

				progress.Report(new KeyValuePair<int, string>(10, "Running Factorio - creating test save."));
				process.StartInfo.Arguments = string.Format("--mod-directory \"{0}\" --create temp-save.zip", modsPath);
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
						CleanupFailedImport(modsPath);
						return "";
					}
					Thread.Sleep(100);
				}

				if (resultString.Contains("Is another instance already running?", StringComparison.CurrentCulture))
				{
					MessageBox.Show("Foreman export could not be completed because this instance of Factorio is currently running. Please stop expanding the factory for just a brief moment and let the export commence in peace!");
					CleanupFailedImport(modsPath);
					return "";
				}

				//ensure that the foreman export mod is correctly added to the mod-list and is enabled
				SetStateForemanExportMod(modsPath, true);

				//copy the files as necessary
				try
				{
					Directory.CreateDirectory(Path.Combine(modsPath, foremanModName));

					File.Copy(Path.Combine(["Mods", foremanModName, "info.json"]), Path.Combine([modsPath, foremanModName, "info.json"]));
					File.Copy(Path.Combine(["Mods", foremanModName, "instrument-after-data.lua"]), Path.Combine([modsPath, foremanModName, "instrument-after-data.lua"]), true);

                    File.Copy(Path.Combine(["Mods", foremanModName, "instrument-control.lua"]), Path.Combine([modsPath, foremanModName, "instrument-control.lua"]), true);
				}
				catch (Exception e)
				{
					if (e is UnauthorizedAccessException)
					{
						MessageBox.Show("Insufficient access to copy foreman export mod files (Mods/"+ foremanModName+"/) to the factorio mods folder. Please ensure factorio mods are in an accessible folder, or launch Foreman with Administrator privileges.");
						ErrorLogging.LogLine("copying of foreman export mod files failed - insufficient access E:" + e.ToString());
					}
					else
					{
						MessageBox.Show("could not copy foreman export mod files (Mods/"+ foremanModName+"/) to the factorio mods folder. Reinstall foreman?");
						ErrorLogging.LogLine("copying of foreman export mod files failed. E:" + e.ToString());
					}
					CleanupFailedImport(modsPath);
					return "";
				}

				//launch factorio again to export the data (LAUNCH #2)
				progress.Report(new KeyValuePair<int, string>(20, "Running Factorio - foreman export scripts."));
				process.StartInfo.Arguments = string.Format("--mod-directory \"{0}\" --instrument-mod foremanexport --benchmark temp-save.zip --benchmark-ticks 1 --benchmark-runs 1", modsPath);
				process.Start();
				resultString = "";
				while (!process.HasExited)
				{
					resultString += process.StandardOutput.ReadToEnd();
					if (token.IsCancellationRequested)
					{
						process.Close();
						CleanupFailedImport(modsPath);
						return "";
					}
					Thread.Sleep(100);
				}

				if (File.Exists("temp-save.zip"))
					File.Delete("temp-save.zip");
				if (Directory.Exists(Path.Combine(modsPath, foremanModName)))
					Directory.Delete(Path.Combine(modsPath, foremanModName), true);

				progress.Report(new KeyValuePair<int, string>(25, "Processing mod files."));

				if (resultString.Contains("Is another instance already running?", StringComparison.CurrentCulture))
				{
					MessageBox.Show("Foreman export could not be completed because this instance of Factorio is currently running. Please stop expanding the factory for just a brief moment and let the export commence in peace!");
					CleanupFailedImport(modsPath);
					return "";
				}
				else if (!resultString.Contains("<<<END-EXPORT-P1>>>", StringComparison.CurrentCulture) || !resultString.Contains("<<<END-EXPORT-P2>>>", StringComparison.CurrentCulture))
				{
#if DEBUG
					Console.WriteLine(resultString);
#endif
					MessageBox.Show("Foreman export could not be completed - possible mod conflict detected. Please run factorio and ensure it can successfully load to menu before retrying.");
					ErrorLogging.LogLine("Foreman export failed partway. Consult errorExporting.json for full output (and search for <<<END-EXPORT-P1>>> or <<<END-EXPORT-P2>>>, at least one of which is missing)");
					File.WriteAllText(Path.Combine(Application.StartupPath, "errorExporting.json"), resultString);
					CleanupFailedImport(modsPath);
					return "";
				}
#if DEBUG
				File.WriteAllText(Path.Combine(Application.StartupPath, "debugExporting.json"), resultString);
#endif

				string lnamesString = resultString[(resultString.IndexOf("<<<START-EXPORT-LN>>>") + 23)..];
				lnamesString = lnamesString[..(lnamesString.IndexOf("<<<END-EXPORT-LN>>>") - 2)];
				lnamesString = lnamesString.Replace("\n", "").Replace("\r", "").Replace("<#~#>", "\n");

				string iconString = resultString[(resultString.IndexOf("<<<START-EXPORT-P1>>>") + 23)..];
				iconString = iconString[..(iconString.IndexOf("<<<END-EXPORT-P1>>>") - 2)];

				string dataString = resultString[(resultString.IndexOf("<<<START-EXPORT-P2>>>") + 23)..];
				dataString = dataString[..(dataString.IndexOf("<<<END-EXPORT-P2>>>") - 2)];

				string[] lnames = lnamesString.Split('\n'); //keep empties - we know where they are!
				Dictionary<string, string> localisedNames = []; //this is the link between the 'lid' property and the localised names in dataString
				for (int i = 0; i < lnames.Length / 2; i++)
					localisedNames.Add('$' + i.ToString(), lnames[(i * 2) + 1].Replace("Unknown key: \"", "").Replace("\"", ""));

#if DEBUG
				File.WriteAllText(Path.Combine(Application.StartupPath, "_iconJObjectOut.json"), iconString.ToString());
				File.WriteAllText(Path.Combine(Application.StartupPath, "_dataJObjectOut.json"), dataString.ToString());
#endif
				JObject? iconJObject = null;
				JObject? dataJObject = null;
				try
				{
					iconJObject = JObject.Parse(iconString); //this is what needs to be parsed to get all the icons
					dataJObject = JObject.Parse(dataString); //this is pretty much the entire json preset - just need to save it.
				}
				catch
				{
					MessageBox.Show("Foreman export could not be completed - unknown json parsing error.\nSorry");
					ErrorLogging.LogLine("json parsing of output failed. This is clearly an error with the export mod ("+ foremanModName+"). Consult _iconJObjectOut.json and _dataJObjectOut.json and check which one isnt a valid json (and why)");
					File.WriteAllText(Path.Combine(Application.StartupPath, "_iconJObjectOut.json"), iconString.ToString());
					File.WriteAllText(Path.Combine(Application.StartupPath, "_dataJObjectOut.json"), dataString.ToString());
					CleanupFailedImport(modsPath);
					return "";
				}

				//now to trawl over the dataJObject entities and replace any 'lid' with 'localised_name'
				foreach (JToken set in dataJObject.Values().ToList())
				{
					foreach (JToken obj in set.ToList())
					{
						if (obj is JObject jobject && jobject["lid"]?.Value<string>() is string lid)
						{
							JProperty lname = new("localised_name", localisedNames[lid]);
							jobject.Add(lname);
							jobject.Remove("lid");
						}
					}
				}

				//save new preset (data)
				File.WriteAllText(Path.Combine(Application.StartupPath, presetPath + ".pjson"), dataJObject.ToString(Formatting.Indented));
				File.Copy(Path.Combine(Application.StartupPath, "baseCustom.json"), Path.Combine(Application.StartupPath, presetPath + ".json"), true);
#if DEBUG
				File.WriteAllText(Path.Combine(Application.StartupPath, "_iconJObjectOut.json"), iconJObject.ToString(Formatting.Indented));
				File.WriteAllText(Path.Combine(Application.StartupPath, "_dataJObjectOut.json"), dataJObject.ToString(Formatting.Indented));
#endif

				if (token.IsCancellationRequested)
				{
					process.Close();
					CleanupFailedImport(modsPath);
					return "";
				}

				//now we need to process icons. This is done by the IconProcessor.
				Dictionary<string, string> modSet = [];
				foreach (var objJToken in dataJObject["mods"]?.ToList() ?? [])
					modSet.Add((objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key")).ToLower(), objJToken["version"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"));

				using (IconCacheProcessor icProcessor = new())
				{
					if (!icProcessor.PrepareModPaths(modSet, modsPath, Path.Combine(installPath, "data"), token))
					{
						if (!token.IsCancellationRequested)
						{
							MessageBox.Show("Mod inconsistency detected. Try to see if launching Factorio gives an error?");
							ErrorLogging.LogLine("Mod parsing failed - the list of mods provided could not be mapped to the existing mod folders & zip files.");
						}
						CleanupFailedImport(modsPath, presetPath);
						return "";
					}

					if (!icProcessor.CreateIconCache(iconJObject, Path.Combine(Application.StartupPath, presetPath + ".dat"), progress, 30, 100, token))
					{
						if (!token.IsCancellationRequested)
						{
							ErrorLogging.LogLine(string.Format("{0}/{1} images were not found while processing icons.", icProcessor.FailedPathCount, icProcessor.TotalPathCount));
							if (MessageBox.Show(string.Format("{0}/{1} images that were processed for icons were not found and thus some icons are likely wrong/empty. Do you still wish to continue with the preset import?", icProcessor.FailedPathCount, icProcessor.TotalPathCount), "Confirm Preset Import", MessageBoxButtons.YesNo) != DialogResult.Yes)
							{
								CleanupFailedImport(modsPath, presetPath);
								return "";
							}
						}
						else
						{
							CleanupFailedImport(modsPath, presetPath);
							return "";
						}
					}
				}

				SetStateForemanExportMod(modsPath, false);
				return NewPresetName;
			});
		}

		private void CleanupFailedImport(string modsPath = "", string presetPath = "", string foremanModName = "")
		{
			SetStateForemanExportMod(modsPath, false);

			NewPresetName = "";

			if (File.Exists("temp-save.zip"))
				File.Delete("temp-save.zip");

			if (modsPath != "" && foremanModName != "" && Directory.Exists(Path.Combine(modsPath, foremanModName)))
				Directory.Delete(Path.Combine(modsPath, foremanModName), true);

			if (presetPath != "" && foremanModName != "" && File.Exists(Path.Combine(Application.StartupPath, presetPath + ".pjson")))
				File.Delete(Path.Combine(Application.StartupPath, presetPath + ".pjson"));
			if (presetPath != "" && foremanModName != "" && File.Exists(Path.Combine(Application.StartupPath, presetPath + ".json")))
				File.Delete(Path.Combine(Application.StartupPath, presetPath + ".json"));
			if (presetPath != "" && foremanModName != "" && File.Exists(Path.Combine(Application.StartupPath, presetPath + ".dat")))
				File.Delete(Path.Combine(Application.StartupPath, presetPath + ".dat"));
		}

		//Sets the enabled status of the foreman export mod within the mod-list of factorio to be "enabled" (true/false).
		//Needs to be enabled in order to process the preset, but should be disabled otherwise as it adds processing steps to factorio which shouldnt be done any other time (such as while playing the game)
		private static void SetStateForemanExportMod(string modsPath, bool enabled)
		{
			//ensure that the foreman export mod is correctly added to the mod-list and is enabled
			string modListPath = Path.Combine(modsPath, "mod-list.json");
			JObject? modlist = null;
			if (!File.Exists(modListPath))
				modlist = [];
			else
				modlist = JObject.Parse(File.ReadAllText(modListPath));
			if (modlist["mods"] == null)
				modlist.Add("mods", new JArray());

			JToken? foremanModToken = modlist["mods"]?.ToList().FirstOrDefault(t => t["name"]?.Value<string>() is string str && str == "foremanexport");
			if (enabled)
			{
				if (foremanModToken == null)
					(modlist["mods"]?.Value<JArray>() ?? throw new InvalidOperationException("modlist[mods] is not JArray")).Add(new JObject() { { "name", "foremanexport" }, { "enabled", enabled } });
				else
					foremanModToken["enabled"] = enabled;
			}
			else
				foremanModToken?.Remove();

			try
			{
				File.WriteAllText(modListPath, modlist.ToString(Formatting.Indented)); //updated mod list with foreman export disabled
			}
			catch { }
		}

		private void PresetNameTextBox_TextChanged(object? sender, EventArgs e)
		{
			int i = PresetNameTextBox.SelectionStart;
			string filteredText = string.Concat(PresetNameTextBox.Text.Where(c => char.IsLetterOrDigit(c) || ExtraChars.Contains(c)));
			if (filteredText != PresetNameTextBox.Text)
			{
				i = Math.Max(i + filteredText.Length - PresetNameTextBox.Text.Length, 0);
				PresetNameTextBox.Text = filteredText;
				PresetNameTextBox.SelectionStart = i;
			}

			List<Preset> existingPresets = MainForm.GetValidPresetsList();
			if (filteredText.Length < 5)
				PresetNameTextBox.BackColor = Color.Moccasin;
			else if (existingPresets.Any(p => p.Name.Equals(filteredText, StringComparison.CurrentCultureIgnoreCase)))
				PresetNameTextBox.BackColor = Color.Pink;
			else
				PresetNameTextBox.BackColor = Color.LightGreen;
		}
	}
}
