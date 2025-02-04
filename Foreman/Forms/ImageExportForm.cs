﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using Foreman.DataCache;

namespace Foreman {
	public partial class ImageExportForm : Form
	{
		private readonly float[] multipliers = [0.05f, 0.1f, 0.2f, 0.5f, 1f, 2f, 3f];
		private readonly string[] multiplierNames = ["1/20", "1/10", "1/5", "1/2", "1", "2", "3"];
		private readonly int initialIndex = 4;

		private readonly ProductionGraphViewer graphViewer;

		public ImageExportForm(ProductionGraphViewer graphViewer)
		{
			InitializeComponent();
			this.graphViewer = graphViewer;

			ScaleSelectionBox.Items.AddRange(multiplierNames);
			ScaleSelectionBox.SelectedIndex = initialIndex;
			UpdateSizeLabel();
		}

		private void Button1_Click(object? sender, EventArgs e)
		{
			using SaveFileDialog dialog = new();
			dialog.AddExtension = true;
			dialog.Filter = "PNG files (*.png)|*.png";
			dialog.InitialDirectory = Path.Combine(Application.StartupPath, "Exported Graphs");
			if (!Directory.Exists(dialog.InitialDirectory))
				Directory.CreateDirectory(dialog.InitialDirectory);
			dialog.FileName = "Foreman Production Flowchart.png";
			dialog.ValidateNames = true;
			dialog.OverwritePrompt = true;
			var result = dialog.ShowDialog();

			if (result == System.Windows.Forms.DialogResult.OK) {
				fileTextBox.Text = dialog.FileName;
			}
		}

		private void ExportButton_Click(object? sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(fileTextBox.Text) || string.IsNullOrEmpty(Path.GetDirectoryName(fileTextBox.Text)) || !Directory.Exists(Path.GetDirectoryName(fileTextBox.Text)))
			{
				MessageBox.Show("Directory doesn't exist!");
			}
			else
			{
				graphViewer.ClearSelection();

				float scale = multipliers[ScaleSelectionBox.SelectedIndex];

				Bitmap image = ViewLimitCheckBox.Checked? new Bitmap((int)(graphViewer.Width * scale / graphViewer.ViewScale), (int)(graphViewer.Height * scale / graphViewer.ViewScale)) : new Bitmap((int)(graphViewer.Graph.Bounds.Width * scale), (int)(graphViewer.Graph.Bounds.Height * scale));
				using Graphics graphics = Graphics.FromImage(image);
				graphics.ResetTransform();

				if (ViewLimitCheckBox.Checked) {
					graphics.TranslateTransform(graphViewer.Width / (graphViewer.ViewScale * 2), graphViewer.Height / (graphViewer.ViewScale * 2));
					graphics.TranslateTransform(graphViewer.ViewOffset.X, graphViewer.ViewOffset.Y);
					graphics.ScaleTransform(scale, scale);
				} else {
					graphics.ScaleTransform(scale, scale);
					graphics.TranslateTransform(-graphViewer.Graph.Bounds.X, -graphViewer.Graph.Bounds.Y);
				}

				graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				if (!TransparencyCheckBox.Checked)
					graphics.Clear(Color.White);

				graphViewer.Paint(graphics, true);

				try {
					image.Save(fileTextBox.Text, ImageFormat.Png);
					Close();
				} catch (Exception exception) {
					MessageBox.Show("Error saving image: " + exception.Message);
					ErrorLogging.LogLine("Error saving image: " + exception.ToString());
				}
			}
		}

		private void UpdateSizeLabel()
		{
			float scale = multipliers[ScaleSelectionBox.SelectedIndex];
			int x, y;

			if (ViewLimitCheckBox.Checked)
			{
				x = (int)(graphViewer.Width * scale / graphViewer.ViewScale);
				y = (int)(graphViewer.Height * scale / graphViewer.ViewScale);
			}
			else
			{
				x = (int)(graphViewer.Graph.Bounds.Width * scale);
				y = (int)(graphViewer.Graph.Bounds.Height * scale);
			}

			ImageSizeLabel.Text = string.Format("Image Size: {0} x {1}", x.ToString("N0"), y.ToString("N0"));
		}

		private void ViewLimitCheckBox_CheckedChanged(object? sender, EventArgs e)
		{
			UpdateSizeLabel();
		}

		private void ScaleSelectionBox_SelectedIndexChanged(object? sender, EventArgs e)
		{
			UpdateSizeLabel();
		}
	}
}
