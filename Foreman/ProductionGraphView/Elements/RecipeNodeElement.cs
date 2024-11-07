using Foreman.Controls;
using Foreman.DataCache;
using Foreman.DataCache.DataTypes;
using Foreman.Models;
using Foreman.Models.Nodes;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Foreman.ProductionGraphView.Elements {
	public partial class RecipeNodeElement : BaseNodeElement {
		protected override Brush CleanBgBrush { get { return recipeBgBrush; } }
		private static readonly Brush recipeBgBrush = new SolidBrush(Color.FromArgb(190, 217, 212));
		private static readonly Pen productivityPen = new(Brushes.DarkRed, 6);
		private static readonly Pen productivityPlusPen = new(productivityPen.Brush, 2);
		private static readonly Pen extraProductivityPen = new(Brushes.Crimson, 6);

		private static readonly StringFormat textFormat = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };

		private readonly AssemblerElement AssemblerElement;
		private readonly BeaconElement BeaconElement;

		private string RecipeName { get { return DisplayedNode.BaseRecipe.FriendlyName; } }

		private new readonly ReadOnlyRecipeNode DisplayedNode;

		private static bool OptionsCopyAssemblerDefault = true;
		private static bool OptionsCopyExtraProductivityMinersDefault = true;
		private static bool OptionsCopyExtraProductivityNonMinersDefault = true;
		private static bool OptionsCopyFuelDefault = true;
		private static bool OptionsCopyModulesDefault = true;
		private static bool OptionsCopyBeaconDefault = true;
		private static bool OptionsCopyBeaconModulesDefault = true;

		public RecipeNodeElement(ProductionGraphViewer graphViewer, ReadOnlyRecipeNode node) : base(graphViewer, node) {
			DisplayedNode = node;

			AssemblerElement = new AssemblerElement(graphViewer, this);
			AssemblerElement.SetVisibility(graphViewer.LevelOfDetail != ProductionGraphViewer.LOD.Low);

			BeaconElement = new BeaconElement(graphViewer, this);
			BeaconElement.SetVisibility(graphViewer.LevelOfDetail != ProductionGraphViewer.LOD.Low);

			UpdateState();
		}

		protected override void UpdateState() {
			//update tabs (necessary now that it is possible that an item was added or removed)... I am looking at you furnaces!!! ... also - with quality added to the game it is possible that the outputs will drastically change based on selected modules (add/remove quality)
			//done by first checking all old tabs and removing any that are no longer part of the displayed node, then looking at the displayed node io and adding any new tabs that are necessary.
			//could potentially be done by just deleting all the old ones and remaking them from scratch, but come on - thats much more intensive than just doing some checks!
			foreach (ItemTabElement oldTab in InputTabs.Where(tab => !DisplayedNode.Inputs.Contains(tab.Item)).ToList()) {
				InputTabs.Remove(oldTab);
				oldTab.Dispose();
			}
			foreach (ItemTabElement oldTab in OutputTabs.Where(tab => !DisplayedNode.Outputs.Contains(tab.Item)).ToList()) {
				OutputTabs.Remove(oldTab);
				oldTab.Dispose();
			}
			foreach (ItemQualityPair item in DisplayedNode.Inputs)
				if (!InputTabs.Any(tab => tab.Item == item))
					InputTabs.Add(new ItemTabElement(item, LinkType.Input, graphViewer, this));
			foreach (ItemQualityPair item in DisplayedNode.Outputs)
				if (!OutputTabs.Any(tab => tab.Item == item))
					OutputTabs.Add(new ItemTabElement(item, LinkType.Output, graphViewer, this));

			//now that the tabs have been updated, update the size and positioning of the node:
			int yOffset = DisplayedNode.NodeDirection == NodeDirection.Up && InputTabs.Count == 0 && OutputTabs.Count != 0 || DisplayedNode.NodeDirection == NodeDirection.Down && OutputTabs.Count == 0 && InputTabs.Count != 0 ? 10 :
						  DisplayedNode.NodeDirection == NodeDirection.Down && InputTabs.Count == 0 && OutputTabs.Count != 0 || DisplayedNode.NodeDirection == NodeDirection.Up && OutputTabs.Count == 0 && InputTabs.Count != 0 ? -10 : 0;
			yOffset += DisplayedNode.NodeDirection == NodeDirection.Up ? 4 : 0;

			AssemblerElement.Location = new Point(-26, -14 + yOffset);
			BeaconElement.Location = new Point(-30, 27 + yOffset);

			AssemblerElement.SetVisibility(graphViewer.LevelOfDetail != ProductionGraphViewer.LOD.Low);
			BeaconElement.SetVisibility(graphViewer.LevelOfDetail != ProductionGraphViewer.LOD.Low);

			Width = Math.Max(MinWidth, Math.Max(GetIconWidths(InputTabs), GetIconWidths(OutputTabs)) + 10);
			if (Width % WidthD != 0) {
				Width += WidthD;
				Width -= Width % WidthD;
			}
			Height = graphViewer.LevelOfDetail == ProductionGraphViewer.LOD.Low ? BaseSimpleHeight : BaseRecipeHeight;

			base.UpdateState();
		}

		protected override Bitmap? NodeIcon() { return DisplayedNode.BaseRecipe.Icon; }

		protected override void DetailsDraw(Graphics graphics, Point trans) {
			if (graphViewer.LevelOfDetail == ProductionGraphViewer.LOD.Low) //text only view
			{
				//text
				bool overproducing = DisplayedNode.IsOverproducing();
				Rectangle textSlot = new(trans.X - Width / 2 + 40, trans.Y - Height / 2 + (overproducing ? 32 : 27), Width - 10 - 40, Height - (overproducing ? 64 : 54));
				//graphics.DrawRectangle(devPen, textSlot);
				int textLength = GraphicsStuff.DrawText(graphics, TextBrush, textFormat, RecipeName, BaseFont, textSlot);

				//assembler icon
				var asm = DisplayedNode.SelectedAssembler;
				graphics.DrawImage(asm && asm.Icon is not null ? asm.Icon : IconCache.GetUnknownIcon(), trans.X - Math.Min(Width / 2 - 10, textLength / 2 + 32), trans.Y - 16, 32, 32);

				//productivity ticks
				int pModules = DisplayedNode.AssemblerModules.Count(m => m.Module.GetProductivityBonus() > 0);
				pModules += (int)(DisplayedNode.BeaconModules.Count(m => m.Module.GetProductivityBonus() > 0) * DisplayedNode.BeaconCount);

				bool extraProductivity = DisplayedNode.ExtraProductivity > 0 && (DisplayedNode.SelectedAssembler.Assembler.EntityType == EntityType.Miner || graphViewer.Graph.EnableExtraProductivityForNonMiners);
				pModules += extraProductivity ? 1 : 0;

				for (int i = 0; i < pModules && i < 6; i++)
					graphics.DrawEllipse(extraProductivity && i == 0 ? extraProductivityPen : productivityPen, trans.X - Width / 2 - 1, trans.Y - Height / 2 + 10 + i * 12, 6, 6);
				if (pModules > 6) {
					graphics.DrawLine(productivityPlusPen, trans.X - Width / 2 - 4, trans.Y - Height / 2 + 84, trans.X - Width / 2 + 8, trans.Y - Height / 2 + 84);
					graphics.DrawLine(productivityPlusPen, trans.X - Width / 2 + 2, trans.Y - Height / 2 + 84 - 6, trans.X - Width / 2 + 2, trans.Y - Height / 2 + 84 + 6);
				}
			} else if (DisplayedNode.ExtraProductivity > 0 && (DisplayedNode.SelectedAssembler.Assembler.EntityType == EntityType.Miner || graphViewer.Graph.EnableExtraProductivityForNonMiners)) {
				graphics.DrawEllipse(extraProductivityPen, trans.X - Width / 2 - 1, trans.Y - Height / 2 + 10, 6, 6);
			}
		}

		protected override void AddRClickMenuOptions(bool nodeInSelection) {
			if (nodeInSelection) {
				List<ReadOnlyRecipeNode> rNodes = new(graphViewer.SelectedNodes.Where(ne => ne is RecipeNodeElement).Select(ne => (ReadOnlyRecipeNode)ne.DisplayedNode));
				if (!rNodes.Contains(DisplayedNode))
					rNodes.Add(DisplayedNode);

				RightClickMenu.Items.Add(new ToolStripSeparator());

				RightClickMenu.Items.Add(new ToolStripMenuItem("Apply default assembler(s)", null,
					new EventHandler((o, e) => {
						RightClickMenu.Close();
						foreach (ReadOnlyRecipeNode rNode in rNodes)
							(graphViewer.Graph.RequestNodeController(rNode) as RecipeNodeController)?.AutoSetAssembler();
					})));
				RightClickMenu.Items.Add(new ToolStripMenuItem("Apply default modules", null,
					new EventHandler((o, e) => {
						RightClickMenu.Close();
						foreach (ReadOnlyRecipeNode rNode in rNodes)
							(graphViewer.Graph.RequestNodeController(rNode) as RecipeNodeController)?.AutoSetAssemblerModules();
					})));
				if (rNodes.Any(rn => rn.AssemblerModules.Count > 0))
					RightClickMenu.Items.Add(new ToolStripMenuItem("Remove modules", null,
						new EventHandler((o, e) => {
							RightClickMenu.Close();
							foreach (ReadOnlyRecipeNode rNode in rNodes)
								(graphViewer.Graph.RequestNodeController(rNode) as RecipeNodeController)?.RemoveAssemblerModules();
						})));
				if (rNodes.Any(rn => rn.SelectedBeacon is BeaconQualityPair))
					RightClickMenu.Items.Add(new ToolStripMenuItem("Remove beacons", null,
						new EventHandler((o, e) => {
							RightClickMenu.Close();
							foreach (ReadOnlyRecipeNode rNode in rNodes)
								(graphViewer.Graph.RequestNodeController(rNode) as RecipeNodeController)?.ClearBeacon();
						})));

				RightClickMenu.Items.Add(new ToolStripSeparator());
				if (NodeCopyOptions.GetNodeCopyOptions(Clipboard.GetText(), graphViewer.DCache) is NodeCopyOptions copiedOptions) {
					bool canPasteAssembler = rNodes.Any(rn => rn.BaseRecipe.Recipe.Assemblers.Contains(copiedOptions.Assembler.Assembler));
					bool canPasteExtraProductivityMiners = rNodes.Any(rn => rn.SelectedAssembler.Assembler.EntityType == EntityType.Miner);
					bool canPasteExtraProductivityNonMiners = graphViewer.Graph.EnableExtraProductivityForNonMiners && rNodes.Any(rn => rn.SelectedAssembler.Assembler.EntityType != EntityType.Miner);
					bool canPasteFuel = copiedOptions.Fuel != null && (canPasteAssembler || rNodes.Any(rn => rn.BaseRecipe.Recipe.Assemblers.Any(a => a.Fuels.Contains(copiedOptions.Fuel))));
					bool canPasteModules = copiedOptions.AssemblerModules.Count > 0 && (canPasteAssembler || rNodes.Any(rn => rn.BaseRecipe.Recipe.AssemblerModules.Count > 0 && rn.SelectedAssembler.Assembler.Modules.Count > 0 && rn.SelectedAssembler.Assembler.ModuleSlots > 0));
					bool canPasteBeacon = copiedOptions.Beacon is not null && (canPasteAssembler || rNodes.Any(rn => rn.BaseRecipe.Recipe.AssemblerModules.Count > 0 && rn.SelectedAssembler.Assembler.Modules.Count > 0));

					if (canPasteAssembler || canPasteFuel || canPasteModules || canPasteBeacon) {
						RightClickMenu.ShowCheckMargin = true;

						ToolStripMenuItem assemblerCheck = new(copiedOptions.Assembler.Assembler.GetEntityTypeName(false)) { CheckOnClick = true, Checked = canPasteAssembler && OptionsCopyAssemblerDefault, Enabled = canPasteAssembler, Tag = "CheckBox" };
						ToolStripMenuItem extraProductivityMinersCheck = new("Bonus Productivity (Miners)") { CheckOnClick = true, Checked = canPasteExtraProductivityMiners && OptionsCopyExtraProductivityMinersDefault, Enabled = canPasteExtraProductivityMiners, Tag = "CheckBox" };
						ToolStripMenuItem extraProductivityNonMinersCheck = new("Bonus Productivity (non-Miners)") { CheckOnClick = true, Checked = canPasteExtraProductivityNonMiners && OptionsCopyExtraProductivityNonMinersDefault, Enabled = canPasteExtraProductivityNonMiners, Tag = "CheckBox" };
						ToolStripMenuItem fuelCheck = new("Fuel") { CheckOnClick = true, Checked = canPasteFuel && OptionsCopyFuelDefault, Enabled = canPasteFuel, Tag = "CheckBox" };
						ToolStripMenuItem modulesCheck = new("Modules") { CheckOnClick = true, Checked = canPasteModules && OptionsCopyModulesDefault, Enabled = canPasteModules, Tag = "CheckBox" };
						ToolStripMenuItem beaconCheck = new("Beacon") { CheckOnClick = true, Checked = canPasteBeacon && OptionsCopyBeaconDefault, Enabled = canPasteBeacon, Tag = "CheckBox" };
						ToolStripMenuItem beaconModuleCheck = new("Beacon Modules") { CheckOnClick = true, Checked = canPasteBeacon && OptionsCopyBeaconModulesDefault, Enabled = canPasteBeacon, Tag = "CheckBox" };

						if (canPasteAssembler) RightClickMenu.Items.Add(assemblerCheck);
						if (canPasteExtraProductivityMiners) RightClickMenu.Items.Add(extraProductivityMinersCheck);
						if (canPasteExtraProductivityNonMiners) RightClickMenu.Items.Add(extraProductivityNonMinersCheck);
						if (canPasteFuel) RightClickMenu.Items.Add(fuelCheck);
						if (canPasteModules) RightClickMenu.Items.Add(modulesCheck);
						if (canPasteBeacon) RightClickMenu.Items.Add(beaconCheck);
						if (canPasteBeacon) RightClickMenu.Items.Add(beaconModuleCheck);
						RightClickMenu.Items.Add(new ToolStripSeparator());
						RightClickMenu.Items.Add(new ToolStripMenuItem("Paste selected options", null,
							new EventHandler((o, e) => {
								RightClickMenu.Close();
								if (canPasteAssembler) OptionsCopyAssemblerDefault = assemblerCheck.Checked;
								if (canPasteExtraProductivityMiners) OptionsCopyExtraProductivityMinersDefault = extraProductivityMinersCheck.Checked;
								if (canPasteExtraProductivityNonMiners) OptionsCopyExtraProductivityNonMinersDefault = extraProductivityNonMinersCheck.Checked;
								if (canPasteFuel) OptionsCopyFuelDefault = fuelCheck.Checked;
								if (canPasteModules) OptionsCopyModulesDefault = modulesCheck.Checked;
								if (canPasteBeacon) OptionsCopyBeaconDefault = beaconCheck.Checked;
								if (canPasteBeacon) OptionsCopyBeaconModulesDefault = beaconCheck.Checked;

								foreach (ReadOnlyRecipeNode rNode in rNodes) {
									if (graphViewer.Graph.RequestNodeController(rNode) is not RecipeNodeController controller)
										throw new InvalidOperationException("RecipeNodeController is null or invalid");

									bool assemblerFilter = !assemblerCheck.Checked; //if we do copy assembler, then all the other options are copied only if the assembler is. If we do not copy assembler, then paste options to everyone
									if (assemblerCheck.Checked && rNode.BaseRecipe.Recipe.Assemblers.Contains(copiedOptions.Assembler.Assembler)) //assembler fits the given recipe
									{
										controller.SetAssembler(copiedOptions.Assembler);
										assemblerFilter = true;
										if (rNode.SelectedAssembler.Assembler.EntityType == EntityType.Reactor)
											controller.SetNeighbourCount(copiedOptions.NeighbourCount);
									}

									if (extraProductivityMinersCheck.Checked && rNode.SelectedAssembler.Assembler.EntityType == EntityType.Miner)
										controller.SetExtraProductivityBonus(copiedOptions.ExtraProductivityBonus);
									if (extraProductivityNonMinersCheck.Checked && rNode.SelectedAssembler.Assembler.EntityType != EntityType.Miner)
										controller.SetExtraProductivityBonus(copiedOptions.ExtraProductivityBonus);


									if (fuelCheck.Checked && rNode.SelectedAssembler.Assembler.Fuels.Contains(copiedOptions.Fuel)) //fuel fits the given recipe node
										controller.SetFuel(copiedOptions.Fuel);

									if (modulesCheck.Checked) {
										HashSet<IModule> acceptableAssemblerModules = new(rNode.BaseRecipe.Recipe.AssemblerModules.Intersect(rNode.SelectedAssembler.Assembler.Modules));
										if (!copiedOptions.AssemblerModules.Any(module => !acceptableAssemblerModules.Contains(module.Module))) //all modules we copied can be added to the selected recipe/assembler
											controller.SetAssemblerModules(copiedOptions.AssemblerModules, true);
									}

									if (beaconCheck.Checked && rNode.BaseRecipe.Recipe.AssemblerModules.Intersect(rNode.SelectedAssembler.Assembler.Modules).Any() && copiedOptions.Beacon is BeaconQualityPair bqp) {
										controller.SetBeacon(bqp);
										controller.SetBeaconCount(copiedOptions.BeaconCount);
										controller.SetBeaconsCont(copiedOptions.BeaconsConst);
										controller.SetBeaconsPerAssembler(copiedOptions.BeaconsPerAssembler);
									}

									if (beaconModuleCheck.Checked && rNode.SelectedBeacon is BeaconQualityPair selectedBqp) {
										HashSet<IModule> acceptableBeaconModules = new(rNode.BaseRecipe.Recipe.AssemblerModules.Intersect(rNode.SelectedAssembler.Assembler.Modules).Intersect(selectedBqp.Beacon.Modules));
										if (!copiedOptions.BeaconModules.Any(module => !acceptableBeaconModules.Contains(module.Module)))
											controller.SetBeaconModules(copiedOptions.BeaconModules, true);
									}
								}

								graphViewer.Graph.UpdateNodeValues();
							})));

						RightClickMenu.Items.Add(new ToolStripSeparator());
					}
				}
			} else
				RightClickMenu.Items.Add(new ToolStripSeparator());

			RightClickMenu.Items.Add(new ToolStripMenuItem("Copy this assembler's options", null,
				new EventHandler((o, e) => {
					RightClickMenu.Close();
					StringBuilder stringBuilder = new();
					var writer = new JsonTextWriter(new StringWriter(stringBuilder));

					JsonSerializer serialiser = JsonSerializer.Create();
					serialiser.Formatting = Formatting.None;
					serialiser.Serialize(writer, new NodeCopyOptions(DisplayedNode as ReadOnlyRecipeNode));

					Clipboard.SetText(stringBuilder.ToString());

				})));
		}

		protected override List<TooltipInfo> GetMyToolTips(Point graph_point, bool exclusive) {
			List<TooltipInfo> tooltips = [];

			if (graphViewer.ShowRecipeToolTip) {
				IRecipe[] Recipes = [DisplayedNode.BaseRecipe.Recipe];
				TooltipInfo ttiRecipe = new() {
					Direction = Direction.Left,
					ScreenLocation = graphViewer.GraphToScreen(LocalToGraph(new Point(Width / 2, 0))),
					ScreenSize = RecipePainter.GetSize(Recipes),
					CustomDraw = new Action<Graphics, Point>((g, offset) => { RecipePainter.Paint(Recipes, g, offset); })
				};
				tooltips.Add(ttiRecipe);
			}

			if (exclusive) {
				TooltipInfo helpToolTipInfo = new() {
					Text = string.Format("Left click on this node to edit its {0}, modules, beacon, etc.\nRight click for options.", DisplayedNode.SelectedAssembler.Assembler.GetEntityTypeName(false).ToLower()),
					Direction = Direction.None,
					ScreenLocation = new Point(10, 10)
				};
				tooltips.Add(helpToolTipInfo);
			}

			return tooltips;
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				AssemblerElement.Dispose();
				BeaconElement.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
