using Foreman.DataCache.DataTypes;

using System.Drawing;
using System.Windows.Forms;

namespace Foreman.Controls {
	public partial class RecipePanel : UserControl //helper class to draw the recipe in a panel (container)
	{
		private readonly IRecipe[] Recipes;
		public RecipePanel(IRecipe[] recipes) {
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			DoubleBuffered = true;

			BackColor = Color.Black;

			Recipes = recipes;
			Size = RecipePainter.GetSize(Recipes);
			Location = new Point(0, 0);
		}

		protected override void OnPaint(PaintEventArgs e) { RecipePainter.Paint(Recipes, e.Graphics, new Point(0, 0)); }
		protected override void OnPaintBackground(PaintEventArgs e) { }
	}
}
