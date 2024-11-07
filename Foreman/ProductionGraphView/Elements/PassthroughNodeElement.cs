using Foreman.Controls;
using Foreman.Models.Nodes;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Foreman.ProductionGraphView.Elements {
	public partial class PassthroughNodeElement : BaseNodeElement {
		protected override Brush CleanBgBrush { get { return passthroughBGBrush; } }
		private static readonly Brush passthroughBGBrush = new SolidBrush(Color.FromArgb(200, 200, 200));

		private string ItemName { get { return DisplayedNode.PassthroughItem.FriendlyName; } }

		private new readonly ReadOnlyPassthroughNode DisplayedNode;

		public PassthroughNodeElement(ProductionGraphViewer graphViewer, ReadOnlyPassthroughNode node) : base(graphViewer, node) {
			Width = PassthroughNodeWidth;
			Height = BaseSimpleHeight;
			DisplayedNode = node;
		}

		protected override Bitmap? NodeIcon() { return null; }

		protected override void Draw(Graphics graphics, NodeDrawingStyle style) {
			if (style != NodeDrawingStyle.IconsOnly && DisplayedNode.SimpleDraw && DisplayedNode.RateType == RateType.Auto && !DisplayedNode.KeyNode && !DisplayedNode.IsOverproducing() && !DisplayedNode.ManualRateNotMet() && DisplayedNode.InputLinks.Any() && DisplayedNode.OutputLinks.Any()) {
				InputTabs[0].HideItemTab = true;
				OutputTabs[0].HideItemTab = true;

				float maxLineWidth = DisplayedNode.InputLinks.Concat(DisplayedNode.OutputLinks).Select(l => graphViewer.LinkElementDictionary[l].LinkWidth).Max();
				Point inputPoint = InputTabs[0].GetConnectionPoint();
				Point outputPoint = OutputTabs[0].GetConnectionPoint();
				using (Pen pen = new(DisplayedNode.PassthroughItem.Item.AverageColor, maxLineWidth) { EndCap = System.Drawing.Drawing2D.LineCap.Round, StartCap = System.Drawing.Drawing2D.LineCap.Round })
					graphics.DrawLine(pen, inputPoint, outputPoint);
				if (style == NodeDrawingStyle.Regular) {
					using (Brush brush = new SolidBrush(DisplayedNode.PassthroughItem.Item.AverageColor)) {
						graphics.FillEllipse(brush, inputPoint.X - 6, Math.Min(outputPoint.Y, inputPoint.Y) - 6 + ItemTabElement.TabWidth / 2, 12, 12);
						graphics.FillEllipse(brush, inputPoint.X - 6, Math.Max(outputPoint.Y, inputPoint.Y) - 6 - ItemTabElement.TabWidth / 2, 12, 12);
					}
					if (Highlighted)
						using (Pen pen = new(selectionOverlayBrush, Math.Max(30, maxLineWidth + 10)) { EndCap = System.Drawing.Drawing2D.LineCap.Round, StartCap = System.Drawing.Drawing2D.LineCap.Round })
							graphics.DrawLine(pen, inputPoint, outputPoint);
				}
			} else {
				InputTabs[0].HideItemTab = false;
				OutputTabs[0].HideItemTab = false;
				base.Draw(graphics, style);
			}
		}

		protected override void DetailsDraw(Graphics graphics, Point trans) {
			if (DisplayedNode.RateType == RateType.Manual) {
				int yoffset = DisplayedNode.NodeDirection == NodeDirection.Up ? 28 : 32;
				Rectangle titleSlot = new(trans.X - Width / 2 + 5, trans.Y - Height / 2 + yoffset, Width - 10, 18);
				Rectangle textSlot = new(titleSlot.X, titleSlot.Y + 18, titleSlot.Width, 20);
				//graphics.DrawRectangle(devPen, textSlot);
				//graphics.DrawRectangle(devPen, titleSlot);

				graphics.DrawString("-Limit-", TitleFont, TextBrush, titleSlot, TitleFormat);
				GraphicsStuff.DrawText(graphics, TextBrush, TextFormat, GraphicsStuff.DoubleToString(DisplayedNode.DesiredRate), BaseFont, textSlot);
			}
		}

		protected override List<TooltipInfo> GetMyToolTips(Point graph_point, bool exclusive) {
			List<TooltipInfo> tooltips = [];

			if (exclusive) {
				TooltipInfo helpToolTipInfo = new() {
					Text = string.Format("Left click on this node to edit the throughput of {0}.\nRight click for options.", ItemName),
					Direction = Direction.None,
					ScreenLocation = new Point(10, 10)
				};
				tooltips.Add(helpToolTipInfo);
			}

			return tooltips;
		}

		protected override void AddRClickMenuOptions(bool nodeInSelection) {
			RightClickMenu.Items.Add(new ToolStripSeparator());
			if (DisplayedNode.SimpleDraw) {
				RightClickMenu.Items.Add(new ToolStripMenuItem("Dont simple-draw node", null,
					new EventHandler((o, e) => {
						RightClickMenu.Close();
						(graphViewer.Graph.RequestNodeController(DisplayedNode) as PassthroughNodeController)?.SetSimpleDraw(false);
						graphViewer.Invalidate();
					})));
				if (graphViewer.SelectedNodes.Count > 1 && graphViewer.SelectedNodes.Contains(this)) {
					RightClickMenu.Items.Add(new ToolStripMenuItem("Dont simple-draw selected nodes", null,
						new EventHandler((o, e) => {
							RightClickMenu.Close();
							graphViewer.SetSelectedPassthroughNodesSimpleDraw(false);
							graphViewer.Invalidate();
						})));
				}
			} else {
				RightClickMenu.Items.Add(new ToolStripMenuItem("Simple-draw node", null,
					new EventHandler((o, e) => {
						RightClickMenu.Close();
						(graphViewer.Graph.RequestNodeController(DisplayedNode) as PassthroughNodeController)?.SetSimpleDraw(true);
						graphViewer.Invalidate();
					})));
				if (graphViewer.SelectedNodes.Count > 1 && graphViewer.SelectedNodes.Contains(this)) {
					RightClickMenu.Items.Add(new ToolStripMenuItem("Simple-draw selected nodes", null,
						new EventHandler((o, e) => {
							RightClickMenu.Close();
							graphViewer.SetSelectedPassthroughNodesSimpleDraw(true);
							graphViewer.Invalidate();
						})));
				}
			}
		}
	}
}
