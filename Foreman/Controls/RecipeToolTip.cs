using Foreman.DataCache.DataTypes;

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Foreman.Controls {
	public partial class CustomToolTip : ToolTip {
		private static readonly Color BackgroundColor = Color.FromArgb(65, 65, 65);
		private static readonly Pen BorderPen = new(new SolidBrush(Color.Black), 2);
		private static readonly Pen BreakerPen = new(new SolidBrush(Color.Black), 10);
		private static readonly Brush TextBrush = new SolidBrush(Color.White);

		private string? displayedString;
		private string? comparedString;

		[AmbientValue(typeof(Font), "Tahoma, 8")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Font TextFont { get; set; }

		public CustomToolTip() {
			AutoPopDelay = 100000;
			InitialDelay = 100000; //we will be manually showing this - so we dont want the auto-show to happen.
			ReshowDelay = 100000;
			TextFont = new Font(FontFamily.GenericSansSerif, 7.8f, FontStyle.Regular);

			OwnerDraw = true;
			BackColor = BackgroundColor;
			ForeColor = Color.White;
			Popup += new PopupEventHandler(OnPopup);
			Draw += new DrawToolTipEventHandler(IGTooltip_Draw);
		}

		public void Show(IWin32Window window, Point location) { Show("-", window, location); }

		public void SetText(string text, string comparedText = "") { displayedString = text; comparedString = comparedText; }

		public Size GetExpectedSize() {
			Size measuredText = TextRenderer.MeasureText(displayedString, TextFont);
			Size comparedMeasuredText = TextRenderer.MeasureText(comparedString, TextFont);
			return new Size(measuredText.Width + 4 + (string.IsNullOrEmpty(comparedString) ? 0 : comparedMeasuredText.Width + 18), Math.Max(measuredText.Height + 4, comparedMeasuredText.Height + 4));
		}

		private void OnPopup(object? sender, PopupEventArgs e) {
			if (string.IsNullOrEmpty(displayedString)) {
				e.Cancel = true;
				return;
			}

			e.ToolTipSize = GetExpectedSize();
		}

		private void IGTooltip_Draw(object? sender, DrawToolTipEventArgs e) {
			e.DrawBackground();
			e.Graphics.DrawRectangle(BorderPen, e.Bounds);

			e.Graphics.DrawString(displayedString, TextFont, TextBrush, new Point(2, 2));
			if (!string.IsNullOrEmpty(comparedString)) {
				int breakpoint = TextRenderer.MeasureText(displayedString, TextFont).Width + 9;
				e.Graphics.DrawLine(BreakerPen, breakpoint, 0, breakpoint, e.Bounds.Height);
				e.Graphics.DrawString(comparedString, TextFont, TextBrush, new Point(9 + breakpoint, 2));
			}
		}

	}

	public partial class RecipeToolTip : ToolTip {
		private IRecipe? displayedRecipe;
		private IRecipe? comparedRecipe; //if given, we will display both displayed and compared as a 'VS' display

		public RecipeToolTip() {
			AutoPopDelay = 100000;
			InitialDelay = 100000; //we will be manually showing this - so we dont want the auto-show to happen.
			ReshowDelay = 100000;

			OwnerDraw = true;
			BackColor = Color.DimGray;
			ForeColor = Color.White;
			Popup += new PopupEventHandler(OnPopup);
			Draw += new DrawToolTipEventHandler(OnDraw);
		}

		public void Show(IWin32Window window, Point location) { Show("-", window, location); }

		public void SetRecipe(IRecipe recipe, IRecipe? comparedRecipe = null) { displayedRecipe = recipe; this.comparedRecipe = comparedRecipe; }

		public Size GetExpectedSize() {
			if (displayedRecipe is null)
				return RecipePainter.GetSize([]);
			IRecipe[] recipes = comparedRecipe == null ? [displayedRecipe] : [displayedRecipe, comparedRecipe];
			return RecipePainter.GetSize(recipes);
		}

		private void OnPopup(object? sender, PopupEventArgs e) {
			if (displayedRecipe == null) {
				e.Cancel = true;
				return;
			}

			e.ToolTipSize = GetExpectedSize();
		}

		private void OnDraw(object? sender, DrawToolTipEventArgs e) {
			if (displayedRecipe is null)
				return;
			using Graphics g = e.Graphics;
			IRecipe[] recipes = comparedRecipe == null ? [displayedRecipe] : [displayedRecipe, comparedRecipe];
			RecipePainter.Paint(recipes, g, new Point(0, 0));
		}

		public static int GetRecipeToolTipHeight(IRecipe recipe) {
			if (recipe == null)
				return 110;
			return RecipePainter.GetSize([recipe]).Height;
		}
	}
}
