using Foreman.Controls;
using Foreman.Models.Nodes;

using System.Collections.Generic;
using System.Drawing;

namespace Foreman.ProductionGraphView.Elements {
	public class ConsumerNodeElement : BaseNodeElement {
		protected override Brush CleanBgBrush { get { return consumerBgBrush; } }
		private static readonly Brush consumerBgBrush = new SolidBrush(Color.FromArgb(249, 237, 195));

		private string ItemName { get { return DisplayedNode.ConsumedItem.FriendlyName; } }

		private new readonly ReadOnlyConsumerNode DisplayedNode;

		public ConsumerNodeElement(ProductionGraphViewer graphViewer, ReadOnlyConsumerNode node) : base(graphViewer, node) {
			Width = MinWidth;
			Height = BaseSimpleHeight;
			DisplayedNode = node;
		}

		protected override Bitmap? NodeIcon() { return DisplayedNode.ConsumedItem.Icon; }

		protected override void DetailsDraw(Graphics graphics, Point trans) {
			int yoffset = DisplayedNode.NodeDirection == NodeDirection.Up ? 5 : 28;
			Rectangle titleSlot = new(trans.X - Width / 2 + 5, trans.Y - Height / 2 + yoffset, Width - 10, 20);
			Rectangle textSlot = new(titleSlot.X, titleSlot.Y + 20, titleSlot.Width, Height / 2 - 5);
			//graphics.DrawRectangle(devPen, textSlot);
			//graphics.DrawRectangle(devPen, titleSlot);

			graphics.DrawString(DisplayedNode.RateType == RateType.Auto ? "Infinite Sink:" : "Required Output:", TitleFont, TextBrush, titleSlot, TitleFormat);
			GraphicsStuff.DrawText(graphics, TextBrush, TextFormat, ItemName, BaseFont, textSlot);
		}

		protected override List<TooltipInfo> GetMyToolTips(Point graph_point, bool exclusive) {
			List<TooltipInfo> tooltips = [];

			if (exclusive) {
				TooltipInfo helpToolTipInfo = new() {
					Text = string.Format("Left click on this node to edit quantity of {0} required.\nRight click for options.", ItemName),
					Direction = Direction.None,
					ScreenLocation = new Point(10, 10)
				};
				tooltips.Add(helpToolTipInfo);
			}

			return tooltips;
		}
	}
}
