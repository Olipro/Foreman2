﻿using Foreman.ProductionGraphView.Elements;

using System;
using System.Drawing;

namespace Foreman.ProductionGraphView {
	public class GridManager {
		public int CurrentGridUnit = 0;
		public int CurrentMajorGridUnit = 0;
		public bool ShowGrid = false;
		public bool LockDragToAxis = false;
		public bool ShowZeroAxis = false;
		public Point DragOrigin;

		private static readonly Pen gridPen = new(Color.FromArgb(230, 230, 230), 1);
		private static Pen gridMPen = new(Color.FromArgb(200, 200, 200), 1);
		private static Brush gridBrush = new SolidBrush(Color.FromArgb(240, 240, 240));
		private static readonly Pen zeroAxisPen = new(Color.FromArgb(140, 140, 140), 2);
		private static readonly Pen lockedAxisPen = new(Color.FromArgb(180, 80, 80), 4);
		private const int minGridWidth = 6;

		public GridManager() {
			CurrentGridUnit = Properties.Settings.Default.MinorGridlines;
		}

		public static void SetGridColors(Color bgCol, Color fgCol) {
			gridBrush = new SolidBrush(bgCol);
			gridMPen = new Pen(fgCol);
		}

		public void Paint(Graphics graphics, float viewScale, Rectangle visibleGraphBounds, BaseNodeElement? draggedNode = null) {
			if (ShowGrid) {
				gridPen.Width = 1 / viewScale;
				gridMPen.Width = 1 / viewScale;
				zeroAxisPen.Width = 2 / viewScale;
				lockedAxisPen.Width = 3 / viewScale;

				//minor grid
				if (CurrentGridUnit > 0) {
					if (visibleGraphBounds.Width > CurrentGridUnit && viewScale * CurrentGridUnit > minGridWidth) {
						for (int ix = visibleGraphBounds.X - visibleGraphBounds.X % CurrentGridUnit; ix < visibleGraphBounds.X + visibleGraphBounds.Width; ix += CurrentGridUnit)
							graphics.DrawLine(gridPen, ix, visibleGraphBounds.Y, ix, visibleGraphBounds.Y + visibleGraphBounds.Height);

						for (int iy = visibleGraphBounds.Y - visibleGraphBounds.Y % CurrentGridUnit; iy < visibleGraphBounds.Y + visibleGraphBounds.Height; iy += CurrentGridUnit)
							graphics.DrawLine(gridPen, visibleGraphBounds.X, iy, visibleGraphBounds.X + visibleGraphBounds.Width, iy);
					} else
						graphics.FillRectangle(gridBrush, visibleGraphBounds);
				}

				//major grid
				if (CurrentMajorGridUnit > CurrentGridUnit) {
					if (visibleGraphBounds.Width > CurrentMajorGridUnit && viewScale * CurrentMajorGridUnit > minGridWidth) {
						for (int ix = visibleGraphBounds.X - visibleGraphBounds.X % CurrentMajorGridUnit; ix < visibleGraphBounds.X + visibleGraphBounds.Width; ix += CurrentMajorGridUnit)
							graphics.DrawLine(gridMPen, ix, visibleGraphBounds.Y, ix, visibleGraphBounds.Y + visibleGraphBounds.Height);

						for (int iy = visibleGraphBounds.Y - visibleGraphBounds.Y % CurrentMajorGridUnit; iy < visibleGraphBounds.Y + visibleGraphBounds.Height; iy += CurrentMajorGridUnit)
							graphics.DrawLine(gridMPen, visibleGraphBounds.X, iy, visibleGraphBounds.X + visibleGraphBounds.Width, iy);
					}
				}

				//zero axis
				if (ShowZeroAxis) {
					graphics.DrawLine(zeroAxisPen, 0, visibleGraphBounds.Y, 0, visibleGraphBounds.Y + visibleGraphBounds.Height);
					graphics.DrawLine(zeroAxisPen, visibleGraphBounds.X, 0, visibleGraphBounds.X + visibleGraphBounds.Width, 0);
				}
			}

			//drag axis
			if (LockDragToAxis && draggedNode != null) {
				int xaxis = DragOrigin.X;
				int yaxis = DragOrigin.Y;
				xaxis = AlignToGrid(xaxis);
				yaxis = AlignToGrid(yaxis);

				graphics.DrawLine(lockedAxisPen, xaxis, visibleGraphBounds.Y, xaxis, visibleGraphBounds.Y + visibleGraphBounds.Height);
				graphics.DrawLine(lockedAxisPen, visibleGraphBounds.X, yaxis, visibleGraphBounds.X + visibleGraphBounds.Width, yaxis);
			}

		}

		public Point AlignToGrid(Point original) { return new Point(AlignToGrid(original.X), AlignToGrid(original.Y)); }
		public int AlignToGrid(int original) {
			if (CurrentGridUnit < 1 || !ShowGrid)
				return original;

			original += Math.Sign(original) * CurrentGridUnit / 2;
			original -= original % CurrentGridUnit;
			return original;
		}
	}
}
