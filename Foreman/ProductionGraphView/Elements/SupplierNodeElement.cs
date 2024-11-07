using Foreman.Controls;
using Foreman.Models.Nodes;

using System.Collections.Generic;
using System.Drawing;

namespace Foreman.ProductionGraphView.Elements {
	public class SupplierNodeElement : BaseNodeElement {
		protected override Brush CleanBgBrush { get { return supplierBgBrush; } }
		private static readonly Brush supplierBgBrush = new SolidBrush(Color.FromArgb(231, 214, 224));

		private string ItemName { get { return DisplayedNode.SuppliedItem.FriendlyName; } }

		private new readonly ReadOnlySupplierNode DisplayedNode;

		public SupplierNodeElement(ProductionGraphViewer graphViewer, ReadOnlySupplierNode node) : base(graphViewer, node) {
			Width = MinWidth;
			Height = BaseSimpleHeight;
			DisplayedNode = node;
		}

		protected override Bitmap? NodeIcon() { return DisplayedNode.SuppliedItem.Icon; }

		protected override void DetailsDraw(Graphics graphics, Point trans) {
			int yoffset = DisplayedNode.NodeDirection == NodeDirection.Up ? 32 : 5;
			Rectangle titleSlot = new(trans.X - Width / 2 + 5, trans.Y - Height / 2 + yoffset, Width - 10, 20);
			Rectangle textSlot = new(titleSlot.X, titleSlot.Y + 20, titleSlot.Width, Height / 2 - 5);
			//graphics.DrawRectangle(devPen, textSlot);
			//graphics.DrawRectangle(devPen, titleSlot);

			graphics.DrawString(DisplayedNode.RateType == RateType.Auto ? "Infinite Source:" : "Exact Input:", TitleFont, TextBrush, titleSlot, TitleFormat);
			GraphicsStuff.DrawText(graphics, TextBrush, TextFormat, ItemName, BaseFont, textSlot);
		}

		protected override List<TooltipInfo> GetMyToolTips(Point graph_point, bool exclusive) {
			List<TooltipInfo> tooltips = [];

			if (exclusive) {
				TooltipInfo helpToolTipInfo = new() {
					Text = string.Format("Left click on this node to edit quantity of {0} produced.\nRight click for options.", ItemName),
					Direction = Direction.None,
					ScreenLocation = new Point(10, 10)
				};
				tooltips.Add(helpToolTipInfo);
			}

			return tooltips;
		}
	}
}
