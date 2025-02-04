﻿using Foreman.DataCache;

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Foreman {
	public partial class PresetSelectionForm : Form
	{
		public Preset? ChosenPreset;

		private readonly List<PresetErrorPackage> PresetErrors;

		public PresetSelectionForm(List<PresetErrorPackage> presetErrors)
		{
			PresetErrors = presetErrors;
			PresetErrors.Sort();
			InitializeComponent();

			int totalColumnWidth = 0;
			PresetSelectionListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
			for (int i = 1; i < PresetSelectionListView.Columns.Count - 1; i++)
				totalColumnWidth += PresetSelectionListView.Columns[i].Width;
			PresetSelectionListView.Columns[0].Width = Math.Max(PresetSelectionListView.Width - totalColumnWidth - 32, PresetSelectionListView.Columns[0].Width);
			PresetSelectionListView.Columns[PresetSelectionListView.Columns.Count - 1].Width = 1;


			foreach (PresetErrorPackage pePackage in presetErrors)
			{
				float[] compatibility =
				[
					((float)(pePackage.RequiredMods.Count - pePackage.MissingMods.Count - pePackage.WrongVersionMods.Count - pePackage.AddedMods.Count) / pePackage.RequiredMods.Count),
					((float)(pePackage.RequiredItems.Count - pePackage.MissingItems.Count) / pePackage.RequiredItems.Count),
					((float)(pePackage.RequiredRecipes.Count - pePackage.MissingRecipes.Count - pePackage.IncorrectRecipes.Count) / pePackage.RequiredRecipes.Count)
				];

				ListViewItem presetItem = new(
				[
					pePackage.Preset.Name,
					compatibility[0].ToString("%00"),
					compatibility[1].ToString("%00"),
					compatibility[2].ToString("%00")
				]);
				PresetSelectionListView.Items.Add(presetItem);
				presetItem.ToolTipText =
					string.Format("Mods:\n") +
					string.Format("     ({0}) Correct\n", (pePackage.RequiredMods.Count - pePackage.MissingMods.Count - pePackage.WrongVersionMods.Count)) +
					string.Format("     ({0}) Missing\n", (pePackage.MissingMods.Count)) +
					string.Format("     ({0}) Extra\n", (pePackage.AddedMods.Count)) +
					string.Format("     ({0}) Wrong Version\n", (pePackage.WrongVersionMods.Count)) +
					string.Format("Items:\n") +
					string.Format("     ({0}) Correct\n", (pePackage.RequiredItems.Count - pePackage.MissingItems.Count)) +
					string.Format("     ({0}) Missing\n", (pePackage.MissingItems.Count)) +
					string.Format("Recipes:\n") +
					string.Format("     ({0}) Correct\n", (pePackage.RequiredRecipes.Count - pePackage.MissingRecipes.Count - pePackage.IncorrectRecipes.Count)) +
					string.Format("     ({0}) Missing\n", (pePackage.MissingRecipes.Count)) +
					string.Format("     ({0}) Incorrect", (pePackage.IncorrectRecipes.Count));
			}
		}

		private void ConfirmationButton_Click(object? sender, EventArgs e)
		{
			if (PresetSelectionListView.SelectedIndices.Count > 0)
			{
				ChosenPreset = PresetErrors[PresetSelectionListView.SelectedIndices[0]].Preset;
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
		}

		private void CancellingButton_Click(object? sender, EventArgs e)
		{
			ChosenPreset = null;
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void PresetSelectionListView_MouseDoubleClick(object? sender, MouseEventArgs e)
		{
			if (PresetSelectionListView.SelectedIndices.Count > 0)
			{
				ChosenPreset = PresetErrors[PresetSelectionListView.SelectedIndices[0]].Preset;
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
		}
	}
}
