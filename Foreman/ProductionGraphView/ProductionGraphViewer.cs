﻿using Foreman.Models.Nodes;
using Foreman.Models;
using Foreman.DataCache;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Foreman.DataCache.DataTypes;
using Foreman.ProductionGraphView.Elements;
using Foreman.Controls;
using Foreman.ProductionGraphView;

namespace Foreman {
	public enum NewNodeType { Disconnected, Supplier, Consumer }
	public enum NodeDrawingStyle { Regular, PrintStyle, Simple, IconsOnly } //printstyle is meant for any additional chages (from regular) for exporting to image format, simple will only draw the node boxes (no icons or text) and link lines, iconsonly will draw node icons instead of nodes (for zoomed view)

	[JsonObject(MemberSerialization.OptIn)]
	public partial class ProductionGraphViewer : UserControl
	{
		private enum DragOperation { None, Item, Selection }
		public enum LOD { Low, Medium, High } //low: only names. medium: assemblers, beacons, etc. high: include assembler percentages

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public LOD LevelOfDetail { get; set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool ArrowsOnLinks { get; set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IconsOnly { get; set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int IconsSize { get; set; }
		public int IconsDrawSize { get { return ViewScale > ((double)IconsSize / 96)? 96 : (int)(IconsSize / ViewScale); } }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int NodeCountForSimpleView { get; set; } //if the number of elements to draw is over this amount then the drawing functions will switch to simple view draws (mostly for FPS during zoomed out views)
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool ShowRecipeToolTip { get; set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool TooltipsEnabled { get; set; }
		private bool SubwindowOpen; //used together with tooltip enabled -> if we open up an item/recipe/assembler window, this will halt tooltip show.
		public bool DynamicLinkWidth = false;
		public bool LockedRecipeEditPanelPosition = true;
		public bool FlagOUSuppliedNodes = false; //if true, will add a flag for over or under supplied nodes


		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool SmartNodeDirection { get; set; }

[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DCache DCache { get; set; } = DCache.defaultDCache;
[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[JsonProperty("ProductionGraph")]
		public ProductionGraph Graph { get; private set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public GridManager Grid { get; private set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FloatingTooltipRenderer ToolTipRenderer { get; private set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PointingArrowRenderer ArrowRenderer { get; private set; }

[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IQuality LastAssemblerQuality { get; private set; } = DCache.defaultDCache.DefaultQuality; //quality of the last-edited recipe's assembler (used when placing new recipe nodes)

[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public GraphElement? MouseDownElement { get; set; }

		public IReadOnlyDictionary<ReadOnlyBaseNode, BaseNodeElement> NodeElementDictionary { get { return nodeElementDictionary; } }
		public IReadOnlyDictionary<ReadOnlyNodeLink, LinkElement> LinkElementDictionary { get { return linkElementDictionary; } }

		public IReadOnlyCollection<BaseNodeElement> SelectedNodes { get { return selectedNodes; } }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[JsonProperty]
		public Point ViewOffset { get; private set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[JsonProperty]
		public float ViewScale { get; private set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Rectangle VisibleGraphBounds { get; private set; }

		private const int minDragDiff = 30;
		private const int minLinkWidth = 3;
		private const int maxLinkWidth = 35;

		private static readonly Pen pausedBorders = new(Color.FromArgb(255, 80, 80), 5);
		private static readonly Pen selectionPen = new(Color.FromArgb(100, 100, 200), 2);

		private readonly Dictionary<ReadOnlyBaseNode, BaseNodeElement> nodeElementDictionary;
		private readonly List<BaseNodeElement> nodeElements;
		private readonly Dictionary<ReadOnlyNodeLink, LinkElement> linkElementDictionary;
		private readonly List<LinkElement> linkElements;
		private DraggedLinkElement? draggedLinkElement;

		private Point mouseDownStartScreenPoint;
		private MouseButtons downButtons; //we use this to ensure that any mouse operations only count if they started on this panel

		private Point ViewDragOriginPoint;
		private bool viewBeingDragged = false; //separate from dragOperation due to being able to drag view at all stages of dragOperation

		private DragOperation currentDragOperation = DragOperation.None;

		private Rectangle SelectionZone;
		private Point SelectionZoneOriginPoint;

		private readonly HashSet<BaseNodeElement> selectedNodes; //main list of selected nodes
		private readonly HashSet<BaseNodeElement> currentSelectionNodes; //list of nodes currently under the selection zone (which can be added/removed/replace the full list)

		private readonly ContextMenuStrip rightClickMenu = new();

		[JsonProperty]
		public static int Version => Properties.Settings.Default.ForemanVersion;
		[JsonProperty]
		public static string Object => "ProductionGraphViewer";
		[JsonProperty]
		public string? SavedPresetName => DCache?.PresetName;
		[JsonProperty]
		public IEnumerable<string>? IncludedMods => DCache?.IncludedMods.Select(m => m.Key + "|" + m.Value);
		[JsonProperty]
		public ProductionGraph.RateUnit Unit => Graph.SelectedRateUnit;
		[JsonProperty]
		public bool ExtraProdForNonMiners => Graph.EnableExtraProductivityForNonMiners;
		[JsonProperty]
		public AssemblerSelector.Style AssemblerSelectorStyle => Graph.AssemblerSelector.DefaultSelectionStyle;
		[JsonProperty]
		public ModuleSelector.Style ModuleSelectorStyle => Graph.ModuleSelector.DefaultSelectionStyle;
		[JsonProperty]
		public IEnumerable<string> FuelPriorityList => Graph.FuelSelector.FuelPriority.Select(i => i.Name);
		[JsonProperty]
		public IEnumerable<string>? EnabledRecipes => DCache?.Recipes.Values.Where(r => r.Enabled).Select(r => r.Name);
		[JsonProperty]
		public IEnumerable<string>? EnabledAssemblers => DCache?.Assemblers.Values.Where(a => a.Enabled).Select(a => a.Name);
		[JsonProperty]
		public IEnumerable<string>? EnabledModules => DCache?.Modules.Values.Where(m => m.Enabled).Select(m => m.Name);
		[JsonProperty]
		public IEnumerable<string>? EnabledBeacons => DCache?.Beacons.Values.Where(b => b.Enabled).Select(b => b.Name);

		public ProductionGraphViewer()
		{
			InitializeComponent();
			MouseWheel += new MouseEventHandler(ProductionGraphViewer_MouseWheel);
			Resize += new EventHandler(ProductionGraphViewer_Resized);

			ViewOffset = new Point(Width / -2, Height / -2);
			ViewScale = 1f;
			NodeCountForSimpleView = 200;

			IconsOnly = false;
			IconsSize = 32;

			TooltipsEnabled = true;
			SubwindowOpen = false;

			Graph = new ProductionGraph();
			//Graph.ClearGraph()
			Graph.NodeAdded += Graph_NodeAdded;
			Graph.NodeDeleted += Graph_NodeDeleted;
			Graph.LinkAdded += Graph_LinkAdded;
			Graph.LinkDeleted += Graph_LinkDeleted;
			Graph.NodeValuesUpdated += Graph_NodeValuesUpdated;

			Grid = new GridManager();
			ToolTipRenderer = new FloatingTooltipRenderer(this);
			ArrowRenderer = new PointingArrowRenderer(this);

			nodeElementDictionary = [];
			nodeElements = [];
			linkElementDictionary = [];
			linkElements = [];

			selectedNodes = [];
			currentSelectionNodes = [];

			UpdateGraphBounds();
			Invalidate();
		}

		public void ClearGraph()
		{
			DisposeLinkDrag();
			Graph.ClearGraph();
			//at this point every node element and link element has been removed.

			selectedNodes.Clear();
			currentSelectionNodes.Clear();
		}

		public BaseNodeElement? GetNodeAtPoint(Point point) //returns first such node (in case of stacking)
		{
			//done in a 2 stage process -> first we do a rough check on the point's location (point within a node's area + 50 boundary on all sides), it goes to part 2)
			//							-> then we do a full element.containsPoint check which includes both the node and any added segments (such as item frames)

			for (int i = nodeElements.Count - 1; i >= 0; i--)
			{
				Rectangle roughNodeZone = new(nodeElements[i].X - nodeElements[i].Width / 2 - 50, nodeElements[i].Y - nodeElements[i].Height / 2 - 50, nodeElements[i].Width + 100, nodeElements[i].Height + 100);
				if (roughNodeZone.Contains(point))
					if (nodeElements[i].ContainsPoint(point))
						return nodeElements[i];
			}
			return null;
		}

		//----------------------------------------------Adding new node functions (including link dragging) + Node edit

		public void StartLinkDrag(BaseNodeElement startNode, LinkType linkType, ItemQualityPair item)
		{
			draggedLinkElement?.Dispose();
			draggedLinkElement = new DraggedLinkElement(this, startNode, linkType, item);
			MouseDownElement = draggedLinkElement;
		}

		public void DisposeLinkDrag()
		{
			draggedLinkElement?.Dispose();
			draggedLinkElement = null;
		}

		public void AddItem(Point drawOrigin, Point newLocation)
		{
			if (string.IsNullOrEmpty(DCache?.PresetName))
			{
				MessageBox.Show("The current preset (" + Properties.Settings.Default.CurrentPresetName + ") is corrupt.");
				return;
			}

			SubwindowOpen = true;
			ItemChooserPanel itemChooser = new(this, drawOrigin);
			itemChooser.ItemRequested += (o, itemRequestArgs) =>
			{
				AddNewNode(drawOrigin, itemRequestArgs.Item, newLocation, NewNodeType.Disconnected);
			};
			itemChooser.PanelClosed += (o, e) => { SubwindowOpen = false; };

			itemChooser.Show();
		}

		public void AddNewNode(Point drawOrigin, ItemQualityPair baseItem, Point newLocation, NewNodeType nNodeType, BaseNodeElement? originElement = null, bool offsetLocationToItemTabLevel = false)
		{
			if(string.IsNullOrEmpty(DCache?.PresetName))
			{
				DisposeLinkDrag();
				MessageBox.Show("The current preset (" + Properties.Settings.Default.CurrentPresetName + ") is corrupt.");
				return;
			}

			if ((nNodeType != NewNodeType.Disconnected) && (originElement == null || !baseItem))
				Trace.Fail("Origin element or base item not provided for a new (linked) node");
			
			if (Grid.ShowGrid)
				newLocation = Grid.AlignToGrid(newLocation);

			int lastNodeWidth = 0;
			NodeDirection newNodeDirection = (originElement == null || !SmartNodeDirection) ? Graph.DefaultNodeDirection :
				draggedLinkElement?.Type != BaseLinkElement.LineType.UShape ? originElement.DisplayedNode.NodeDirection :
				originElement.DisplayedNode.NodeDirection == NodeDirection.Up ? NodeDirection.Down : NodeDirection.Up;

            if ((ModifierKeys & Keys.Control) == Keys.Control) //control key pressed -> we are making a passthrough node.
            {
                ProcessNodeRequest(null, new RecipeRequestArgs(NodeType.Passthrough));
                DisposeLinkDrag();
                Graph.UpdateNodeStates(false);
                Invalidate();
            } else
            {
                FRange tempRange = new(0, 0, true);
                if (baseItem.Item is IFluid fluid && fluid.IsTemperatureDependent)
                {
                    if (nNodeType == NewNodeType.Consumer && originElement is not null) //need to check all nodes down to recipes for range of temperatures being produced
                        tempRange = LinkChecker.GetTemperatureRange(fluid, originElement.DisplayedNode, LinkType.Output, true);
                    else if (nNodeType == NewNodeType.Supplier && originElement is not null) //need to check all nodes up to recipes for range of temperatures being consumed (guaranteed to be in a SINGLE [] range)
                        tempRange = LinkChecker.GetTemperatureRange(fluid, originElement.DisplayedNode, LinkType.Input, true);
                }

                RecipeChooserPanel recipeChooser = new(this, drawOrigin, baseItem, tempRange, nNodeType); //QUALITY UPDATE
                recipeChooser.RecipeRequested += ProcessNodeRequest;
                recipeChooser.PanelClosed += (o, e) =>
                {
					if (e.Option != IRChooserPanel.ChooserPanelCloseReason.RequiresItemSelection)
					{
						SubwindowOpen = false;
						DisposeLinkDrag();
						Graph.UpdateNodeStates(false);
						Invalidate();
					}
                };

                SubwindowOpen = true;
                recipeChooser.Show();
            }
			return; //end of this function

			//internal helper funtion: called upon a successfull selection of a recipe-selection screen (opened above)
            void ProcessNodeRequest(object? o, RecipeRequestArgs recipeRequestArgs)
			{
				ReadOnlyBaseNode? newNode = null;
				switch (recipeRequestArgs.NodeType)
				{
					case NodeType.Consumer:
						newNode = Graph.CreateConsumerNode(baseItem, newLocation);
						FinalizeNodePosition(newNode);
						break;
					case NodeType.Supplier:
						newNode = Graph.CreateSupplierNode(baseItem, newLocation);
                        FinalizeNodePosition(newNode);
                        break;
					case NodeType.Passthrough:
						newNode = Graph.CreatePassthroughNode(baseItem, newLocation);
                        FinalizeNodePosition(newNode);
                        break;
					case NodeType.Spoil:
						if (recipeRequestArgs.Direction == NodeDirection.Up)
						{
							if (baseItem.Item.SpoilResult is null)
								throw new InvalidOperationException("SpoilResult is null");
							newNode = Graph.CreateSpoilNode(baseItem, baseItem.Item.SpoilResult, newLocation);
							FinalizeNodePosition(newNode);
						}
						else if (baseItem.Item.SpoilOrigins.Count == 1)
						{
							newNode = Graph.CreateSpoilNode(new ItemQualityPair(baseItem.Item.SpoilOrigins.ElementAt(0), baseItem.Quality), baseItem.Item, newLocation); //QUALITY UPDATE
							FinalizeNodePosition(newNode);
						}
						else
						{
							//need to open up an item selection window to select a given spoil origin
							SubwindowOpen = true;
							ItemChooserPanel itemChooser = new(this, drawOrigin, baseItem.Item.SpoilOrigins);
							itemChooser.ItemRequested += (oo, itemRequestArgs) =>
							{
								newNode = Graph.CreateSpoilNode(new ItemQualityPair(itemRequestArgs.Item.Item, baseItem.Quality), baseItem.Item, newLocation);
                                FinalizeNodePosition(newNode);
                            };
							itemChooser.PanelClosed += (oo, e) => { SubwindowOpen = false; };
							itemChooser.Show();
						}
						break;
					case NodeType.Plant:
                        if (recipeRequestArgs.Direction == NodeDirection.Up)
                        {
                            newNode = Graph.CreatePlantNode(baseItem.Item.PlantResult ?? throw new InvalidOperationException("PlantResult is null"), baseItem.Quality, newLocation);
                            FinalizeNodePosition(newNode);
                        }
						else if (baseItem.Item.PlantOrigins.Count == 1)
                        {
                            newNode = Graph.CreatePlantNode(baseItem.Item.PlantOrigins.ElementAt(0).PlantResult ?? throw new InvalidOperationException("PlantResult is null"), DCache.DefaultQuality, newLocation); //QUALITY UPDATE
                            FinalizeNodePosition(newNode);
                        }
						else
                        {
                            //need to open up an item selection window to select a given spoil origin
                            SubwindowOpen = true;
                            ItemChooserPanel itemChooser = new(this, drawOrigin, baseItem.Item.PlantOrigins);
                            itemChooser.ItemRequested += (oo, itemRequestArgs) =>
                            {
                                newNode = Graph.CreatePlantNode(itemRequestArgs.Item.Item.PlantResult ?? throw new InvalidOperationException("PlantResult is null"), DCache.DefaultQuality, newLocation);
                                FinalizeNodePosition(newNode);
                            };
                            itemChooser.PanelClosed += (oo, e) => { SubwindowOpen = false; };
                            itemChooser.Show();
                        }
                        break;
					case NodeType.Recipe:
						ReadOnlyRecipeNode rNode = Graph.CreateRecipeNode(recipeRequestArgs.Recipe, newLocation);
						newNode = rNode;
						if ((nNodeType == NewNodeType.Consumer && !recipeRequestArgs.Recipe.Recipe.IngredientSet.ContainsKey(baseItem.Item)) || 
							(nNodeType == NewNodeType.Supplier && !recipeRequestArgs.Recipe.Recipe.ProductSet.ContainsKey(baseItem.Item)) ||
							(nNodeType == NewNodeType.Disconnected && baseItem && !recipeRequestArgs.Recipe.Recipe.IngredientSet.ContainsKey(baseItem.Item) && !recipeRequestArgs.Recipe.Recipe.ProductSet.ContainsKey(baseItem.Item)))
						{
							var style = Graph.AssemblerSelector.DefaultSelectionStyle switch {
								AssemblerSelector.Style.Best or
								AssemblerSelector.Style.BestBurner or
								AssemblerSelector.Style.BestNonBurner
								  => AssemblerSelector.Style.BestBurner,
								_ => AssemblerSelector.Style.WorstBurner,
							};
							List<IAssembler> assemblerOptions = AssemblerSelector.GetOrderedAssemblerList(recipeRequestArgs.Recipe.Recipe, style);

							if (Graph.RequestNodeController(rNode) is not RecipeNodeController controller)
								throw new InvalidOperationException("NodeController is not RecipeNodeController");
							if ((nNodeType == NewNodeType.Consumer) || (nNodeType == NewNodeType.Disconnected && assemblerOptions.Any(a => a.Fuels.Contains(baseItem.Item))))
							{
								controller.SetAssembler(new AssemblerQualityPair(assemblerOptions.First(a => a.Fuels.Contains(baseItem.Item)), Graph.DefaultAssemblerQuality));
								controller.SetFuel(baseItem.Item);
							}
							else if(nNodeType == NewNodeType.Supplier || (nNodeType == NewNodeType.Disconnected && assemblerOptions.Any(a => a.Fuels.Contains(baseItem.Item.FuelOrigin))))
                            {
								controller.SetAssembler(new AssemblerQualityPair(assemblerOptions.First(a => a.Fuels.Contains(baseItem.Item.FuelOrigin)), Graph.DefaultAssemblerQuality));
								controller.SetFuel(baseItem.Item.FuelOrigin);
							}
						}
                        FinalizeNodePosition(newNode);
                        break;
				}
			}

			//internal helper funtion: once a node has been created it will be placed where it needs to be and all intermediate states (ex: dragged item line) finalized
			void FinalizeNodePosition(ReadOnlyBaseNode newNode)
			{ 
				//this is the offset to take into account multiple recipe additions (holding shift while selecting recipe). First node isnt shifted, all subsequent ones are 'attempted' to be spaced.
				//should be updated once the node graphics are updated (so that the node size doesnt depend as much on the text)
				BaseNodeElement newNodeElement = NodeElementDictionary[newNode];
				int offsetDistance = lastNodeWidth / 2;
				lastNodeWidth = newNodeElement.Width; //effectively: this recipe width
				if (offsetDistance > 0)
				{
					offsetDistance += (lastNodeWidth / 2);
					int newOffsetDistance = Grid.AlignToGrid(offsetDistance);
					if (newOffsetDistance < offsetDistance)
						newOffsetDistance += Grid.CurrentGridUnit;
					offsetDistance = newOffsetDistance;
				}
				newLocation = new Point(newLocation.X + offsetDistance, newLocation.Y);

				int yoffset = offsetLocationToItemTabLevel ? (nNodeType == NewNodeType.Consumer ? -newNodeElement.Height / 2 : nNodeType == NewNodeType.Supplier ? newNodeElement.Height / 2 : 0) : 0;
				yoffset *= newNodeDirection == NodeDirection.Up ? 1 : -1;
				Graph.RequestNodeController(newNode)?.SetLocation(new Point(newLocation.X, newLocation.Y + yoffset));

				if (originElement is not null)
					Graph.RequestNodeController(newNode)?.SetDirection(newNodeDirection);

				if (nNodeType == NewNodeType.Consumer && originElement is not null)
					Graph.CreateLink(originElement.DisplayedNode, newNode, baseItem);
				else if (nNodeType == NewNodeType.Supplier && originElement is not null)
					Graph.CreateLink(newNode, originElement.DisplayedNode, baseItem);

				DisposeLinkDrag();
				Graph.UpdateNodeValues();
				Graph.UpdateNodeStates(false);
				Invalidate();
			}
		}

		public void AddPassthroughNodesFromSelection(LinkType linkType, Size offset)
		{
			List<BaseNodeElement> newPassthroughNodes = [];
			foreach(PassthroughNodeElement passthroughNode in selectedNodes.Cast<PassthroughNodeElement>())
			{
				NodeDirection newNodeDirection = !SmartNodeDirection ? Graph.DefaultNodeDirection :
					draggedLinkElement?.Type != BaseLinkElement.LineType.UShape ? passthroughNode.DisplayedNode.NodeDirection :
					passthroughNode.DisplayedNode.NodeDirection == NodeDirection.Up ? NodeDirection.Down : NodeDirection.Up;

				ItemQualityPair passthroughItem = ((ReadOnlyPassthroughNode)passthroughNode.DisplayedNode).PassthroughItem;

				int yoffset = linkType == LinkType.Input ? passthroughNode.Height / 2 : -passthroughNode.Height / 2;
				yoffset *= newNodeDirection == NodeDirection.Up ? 1 : -1;
				yoffset += offset.Height;

				ReadOnlyPassthroughNode newNode = Graph.CreatePassthroughNode(passthroughItem, new Point(passthroughNode.Location.X + offset.Width, passthroughNode.Location.Y + yoffset));
				(Graph.RequestNodeController(newNode) as PassthroughNodeController)?.SetDirection(newNodeDirection);

				if (linkType == LinkType.Input)
					Graph.CreateLink(newNode, passthroughNode.DisplayedNode, passthroughItem );
				else
					Graph.CreateLink(passthroughNode.DisplayedNode, newNode, passthroughItem );

				newPassthroughNodes.Add(nodeElementDictionary[newNode]);
			}
			SetSelection(newPassthroughNodes);

			DisposeLinkDrag();
			Graph.UpdateNodeStates(false);
			Invalidate();
		}

		public void TryDeleteSelectedNodes()
		{
			bool proceed = true;
			if (selectedNodes.Count > 10)
				proceed = (MessageBox.Show("You are deleting " + selectedNodes.Count + " nodes. \nAre you sure?", "Confirm delete.", MessageBoxButtons.YesNo) == DialogResult.Yes);
			if (proceed)
			{
				foreach (BaseNodeElement node in selectedNodes.ToList())
					Graph.DeleteNode(node.DisplayedNode);
				selectedNodes.Clear();
				Graph.UpdateNodeValues();
			}
		}

		public void FlipSelectedNodes()
		{
			foreach (BaseNodeElement node in selectedNodes.ToList())
				Graph.RequestNodeController(node.DisplayedNode)?.SetDirection(node.DisplayedNode.NodeDirection == NodeDirection.Up ? NodeDirection.Down : NodeDirection.Up);
			Invalidate();
		}

		public void SetSelectedPassthroughNodesSimpleDraw(bool simpleDraw)
		{
			foreach (PassthroughNodeElement node in selectedNodes.Where(n => n is PassthroughNodeElement).ToList().Cast<PassthroughNodeElement>())
				(Graph.RequestNodeController(node.DisplayedNode) as PassthroughNodeController)?.SetSimpleDraw(simpleDraw);
			Invalidate();
		}

		public void EditNode(BaseNodeElement bNodeElement)
		{
			if (bNodeElement is RecipeNodeElement rNodeElement)
			{
				EditRecipeNode(rNodeElement);
				return;
			}

			SubwindowOpen = true;
			Control editPanel = new EditFlowPanel(bNodeElement.DisplayedNode, this);

            //offset view if necessary to ensure entire window will be seen (with 25 pixels boundary)
            Point screenOriginPoint = GraphToScreen(new Point(bNodeElement.X - (bNodeElement.Width / 2), bNodeElement.Y));
			screenOriginPoint = new Point(screenOriginPoint.X - editPanel.Width, screenOriginPoint.Y - (editPanel.Height / 2));
			Point offset = new(
				(int)(Math.Min(Math.Max(0, 25 - screenOriginPoint.X), this.Width - screenOriginPoint.X - editPanel.Width - bNodeElement.Width - 25)),
				(int)(Math.Min(Math.Max(0, 25 - screenOriginPoint.Y), this.Height - screenOriginPoint.Y - editPanel.Height - 25)));

			ViewOffset = Point.Add(ViewOffset, new Size((int)(offset.X / ViewScale), (int)(offset.Y / ViewScale)));
			UpdateGraphBounds();
			Invalidate();

			//open up the edit panel
			FloatingTooltipControl fttc = new(editPanel, Direction.Right, new Point(bNodeElement.X - (bNodeElement.Width / 2), bNodeElement.Y), this, true, false);
			fttc.Closing += (s, e) =>
			{
				SubwindowOpen = false;
				//bNodeElement.Update();
				Graph.UpdateNodeValues();
			};
		}

		public void EditRecipeNode(RecipeNodeElement rNodeElement)
		{
			SubwindowOpen = true;
			ReadOnlyRecipeNode rNode = (ReadOnlyRecipeNode)rNodeElement.DisplayedNode;
			Control editPanel = new EditRecipePanel(rNode, this);
			RecipePanel recipePanel = new([rNode.BaseRecipe.Recipe]);

			if (LockedRecipeEditPanelPosition)
			{
				editPanel.Location = new Point(15, 15);
				recipePanel.Location = new Point(editPanel.Location.X + editPanel.Width + 5, editPanel.Location.Y);
			}
			else
			{
				//offset view if necessary to ensure entire window will be seen (with 25 pixels boundary). Additionally we want the tooltips to start 100 pixels above the arrow point instead of based on the center of the control (due to the dynamically changing height of the recipe option panel)
				Point recipeEditPanelOriginPoint = FloatingTooltipRenderer.GetTooltipScreenBounds(GraphToScreen(new Point(rNodeElement.X - (rNodeElement.Width / 2), rNodeElement.Y)), editPanel.Size, Direction.Right).Location;
				recipeEditPanelOriginPoint.Y += editPanel.Height / 2 - 125;
				recipeEditPanelOriginPoint.X -= recipePanel.Width + 5;
				Point offset = new(
					(int)(Math.Min(Math.Max(0, 25 - recipeEditPanelOriginPoint.X), this.Width - recipeEditPanelOriginPoint.X - editPanel.Width)),
					(int)(Math.Min(Math.Max(0, 25 - recipeEditPanelOriginPoint.Y), this.Height - recipeEditPanelOriginPoint.Y - editPanel.Height - 25)));

				editPanel.Location = Point.Add(recipeEditPanelOriginPoint, (Size)offset);
				recipePanel.Location = new Point(editPanel.Location.X + editPanel.Width + 5, editPanel.Location.Y);

				ViewOffset = Point.Add(ViewOffset, new Size((int)(offset.X / ViewScale), (int)(offset.Y / ViewScale)));
				UpdateGraphBounds(false);
				Invalidate();

			}

			//add the visible recipe to the right of the node
			_ = new FloatingTooltipControl(recipePanel, Direction.Left, new Point(rNodeElement.X + (rNodeElement.Width / 2), rNodeElement.Y), this, true, true);
			FloatingTooltipControl fttc = new(editPanel, Direction.Right, new Point(rNodeElement.X - (rNodeElement.Width / 2), rNodeElement.Y), this, true, true);
			fttc.Closing += (s, e) => { SubwindowOpen = false; rNodeElement.RequestStateUpdate(); Graph.UpdateNodeValues(); };
		}

		//----------------------------------------------Selection functions

		private void SetSelection(IEnumerable<BaseNodeElement> newSelection)
		{
			foreach (BaseNodeElement element in selectedNodes)
				element.Highlighted = false;

			selectedNodes.Clear();
			selectedNodes.UnionWith(newSelection);

			foreach (BaseNodeElement element in selectedNodes)
				element.Highlighted = true;
		}

		private void UpdateSelection()
		{
			foreach (BaseNodeElement element in nodeElements)
				element.Highlighted = false;

			if ((Control.ModifierKeys & Keys.Alt) != 0) //remove zone
			{
				foreach (BaseNodeElement selectedNode in selectedNodes)
					selectedNode.Highlighted = true;
				foreach (BaseNodeElement newlySelectedNode in currentSelectionNodes)
					newlySelectedNode.Highlighted = false;
			}
			else if ((Control.ModifierKeys & Keys.Control) != 0)  //add zone
			{
				foreach (BaseNodeElement selectedNode in selectedNodes)
					selectedNode.Highlighted = true;
				foreach (BaseNodeElement newlySelectedNode in currentSelectionNodes)
					newlySelectedNode.Highlighted = true;
			}
			else //add zone (additive with ctrl or simple selection)
			{
				foreach (BaseNodeElement newlySelectedNode in currentSelectionNodes)
					newlySelectedNode.Highlighted = true;
			}
		}

		public void ClearSelection()
		{
			foreach (BaseNodeElement element in nodeElements)
				element.Highlighted = false;
			selectedNodes.Clear();
			currentSelectionNodes.Clear();
			Invalidate();
		}

		public void AlignSelected()
		{
			foreach (BaseNodeElement ne in selectedNodes)
				ne.SetLocation(Grid.AlignToGrid(ne.Location));
			Invalidate();
		}

		//----------------------------------------------Paint functions

		protected IEnumerable<GraphElement> GetPaintingOrder()
		{
			if (draggedLinkElement != null)
				yield return draggedLinkElement;
			foreach (LinkElement element in linkElements)
				yield return element;
			foreach (BaseNodeElement element in nodeElements)
				yield return element;
		}

		public void UpdateNodeVisuals()
		{
			try
			{
				foreach (BaseNodeElement node in nodeElements)
					node.RequestStateUpdate();
			}
			catch (OverflowException) { }//Same as when working out node values, there's not really much to do here... Maybe I could show a tooltip saying the numbers are too big or something...
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.ResetTransform();
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			e.Graphics.Clear(this.BackColor);
			e.Graphics.TranslateTransform(Width / 2, Height / 2);
			e.Graphics.ScaleTransform(ViewScale, ViewScale);
			e.Graphics.TranslateTransform(ViewOffset.X, ViewOffset.Y);

			Paint(e.Graphics, false);
		}

		public new void Paint(Graphics graphics, bool FullGraph = false)
		{
			//update visibility of all elements
			if (FullGraph)
				foreach (GraphElement element in GetPaintingOrder())
					element.UpdateVisibility(Graph.Bounds);
			else
				foreach (GraphElement element in GetPaintingOrder())
					element.UpdateVisibility(VisibleGraphBounds);

			//ensure width of selection is correct
			selectionPen.Width = 2 / ViewScale;

			//grid
			if(!FullGraph)
				Grid.Paint(graphics, ViewScale, VisibleGraphBounds, (currentDragOperation == DragOperation.Item) ? MouseDownElement as BaseNodeElement : null);

			//process link element widths
			if (DynamicLinkWidth)
			{
				double itemMax = 0;
				double fluidMax = 0;
				foreach (LinkElement element in linkElements)
				{
					if (element.Item.Item is IFluid && !element.Item.Item.Name.StartsWith("§§")) //§§ is the foreman added special items (currently just §§heat). ignore them
						fluidMax = Math.Max(fluidMax, element.ConsumerElement?.DisplayedNode.GetConsumeRate(element.Item) ?? fluidMax);
					else
						itemMax = Math.Max(itemMax, element.ConsumerElement?.DisplayedNode.GetConsumeRate(element.Item) ?? itemMax);
				}
				itemMax += itemMax == 0 ? 1 : 0;
				fluidMax += fluidMax == 0 ? 1 : 0;

				foreach (LinkElement element in linkElements)
				{
					if (element.Item.Item is IFluid)
						element.LinkWidth = (float)Math.Min((minLinkWidth + (maxLinkWidth - minLinkWidth) * (element.DisplayedLink.Throughput / fluidMax)), maxLinkWidth);
					else
						element.LinkWidth = (float)Math.Min((minLinkWidth + (maxLinkWidth - minLinkWidth) * (element.DisplayedLink.Throughput / itemMax)), maxLinkWidth);
				}
			}
			else
			{
				foreach (LinkElement element in linkElements)
					element.LinkWidth = minLinkWidth;
			}

			//run any pre-paint functions
			foreach (GraphElement elemnent in GetPaintingOrder())
				elemnent.PrePaint();

			//paint all elements (nodes & lines)
			int visibleElements = GetPaintingOrder().Count(e => e.Visible && e is BaseNodeElement);
			foreach (GraphElement element in GetPaintingOrder())
				element.Paint(graphics, FullGraph? NodeDrawingStyle.PrintStyle : IconsOnly? NodeDrawingStyle.IconsOnly : (visibleElements > NodeCountForSimpleView || ViewScale < 0.2)? NodeDrawingStyle.Simple : NodeDrawingStyle.Regular); //if viewscale is 0.2, then the text, images, etc being drawn are ~1/5th the size: aka: ~6x6 pixel images, etc. Use simple draw. Also simple draw if too many objects

			//selection zone
			if (currentDragOperation == DragOperation.Selection && !FullGraph)
			{
				graphics.DrawRectangle(selectionPen, SelectionZone);
				double pConsumption = currentSelectionNodes.Where(n => n.DisplayedNode is ReadOnlyRecipeNode).Sum(n => ((ReadOnlyRecipeNode)n.DisplayedNode).GetTotalAssemblerElectricalConsumption() + ((ReadOnlyRecipeNode)n.DisplayedNode).GetTotalBeaconElectricalConsumption());
				double pProduction = currentSelectionNodes.Where(n => n.DisplayedNode is ReadOnlyRecipeNode).Sum(n => ((ReadOnlyRecipeNode)n.DisplayedNode).GetTotalGeneratorElectricalProduction());
				int recipeNodeCount = currentSelectionNodes.Count(n => n.DisplayedNode is ReadOnlyRecipeNode);
				int buildingCount = (int)Math.Ceiling(currentSelectionNodes.Where(n => n.DisplayedNode is ReadOnlyRecipeNode).Sum(n => ((ReadOnlyRecipeNode)n.DisplayedNode).ActualSetValue));
				int beaconCount = currentSelectionNodes.Where(n => n.DisplayedNode is ReadOnlyRecipeNode).Sum(n => ((ReadOnlyRecipeNode)n.DisplayedNode).GetTotalBeacons());

				ToolTipRenderer.AddExtraToolTip(new TooltipInfo() { Text = string.Format("Power consumption: {0}\nPower production: {1}\nRecipe count: {2}\nBuilding count: {3}\nBeacon count: {4}", GraphicsStuff.DoubleToEnergy(pConsumption, "W"), GraphicsStuff.DoubleToEnergy(pProduction, "W"), recipeNodeCount, buildingCount, beaconCount), Direction = Direction.None, ScreenLocation = new Point(10, 10) });
			}

			//everything below will be drawn directly on the screen instead of scaled/shifted based on graph
			graphics.ResetTransform();

			if (!FullGraph)
			{
				//warning/error arrows
				ArrowRenderer.Paint(graphics, Graph);

				//floating tooltips
				ToolTipRenderer.Paint(graphics, TooltipsEnabled && !SubwindowOpen && currentDragOperation == DragOperation.None && !viewBeingDragged);
				ToolTipRenderer.ClearExtraToolTips();

				//paused border
				if (Graph != null && Graph.PauseUpdates) //graph null check is purely for design view
					graphics.DrawRectangle(pausedBorders, 0, 0, Width - 3, Height - 3);
			}
		}

		//----------------------------------------------Production Graph events

		private void Graph_NodeValuesUpdated(object? sender, EventArgs e)
		{
			UpdateNodeVisuals();
		}

		private void Graph_LinkDeleted(object? sender, NodeLinkEventArgs e)
		{
			BaseNodeElement supplier = nodeElementDictionary[e.nodeLink.Supplier];
			BaseNodeElement consumer = nodeElementDictionary[e.nodeLink.Consumer];

			LinkElement element = linkElementDictionary[e.nodeLink];
			linkElementDictionary.Remove(e.nodeLink);
			linkElements.Remove(element);
			element.Dispose();

			supplier.RequestStateUpdate();
			consumer.RequestStateUpdate();
			Invalidate();
		}

		private void Graph_LinkAdded(object? sender, NodeLinkEventArgs e)
		{
			BaseNodeElement supplier = nodeElementDictionary[e.nodeLink.Supplier];
			BaseNodeElement consumer = nodeElementDictionary[e.nodeLink.Consumer];

			LinkElement element = new(this, e.nodeLink, supplier, consumer);
			linkElementDictionary.Add(e.nodeLink, element);
			linkElements.Add(element);

			supplier.RequestStateUpdate();
			consumer.RequestStateUpdate();
			Invalidate();
		}

		private void Graph_NodeDeleted(object? sender, NodeEventArgs e)
		{
			BaseNodeElement element = nodeElementDictionary[e.node];
			nodeElementDictionary.Remove(e.node);
			nodeElements.Remove(element);
			selectedNodes.Remove(element);
			element.Dispose();
			Invalidate();
		}

		private void Graph_NodeAdded(object? sender, NodeEventArgs e)
		{
			BaseNodeElement? element = null;
			if (e.node is ReadOnlySupplierNode supplierNode)
				element = new SupplierNodeElement(this, supplierNode);
			else if (e.node is ReadOnlyConsumerNode consumerNode)
				element = new ConsumerNodeElement(this, consumerNode);
			else if (e.node is ReadOnlyPassthroughNode passthroughNode)
				element = new PassthroughNodeElement(this, passthroughNode);
			else if (e.node is ReadOnlyRecipeNode recipeNode)
				element = new RecipeNodeElement(this, recipeNode);
			else if (e.node is ReadOnlySpoilNode spoilNode)
				element = new SpoilNodeElement(this, spoilNode);
            else if (e.node is ReadOnlyPlantNode plantNode)
                element = new PlantNodeElement(this, plantNode);
            else
                Trace.Fail("Unexpected node type created in graph.");

			nodeElementDictionary.Add(e.node, element);
			nodeElements.Add(element);
			Invalidate();
		}

		//----------------------------------------------Mouse events

		private void ProductionGraphViewer_MouseDown(object? sender, MouseEventArgs e)
		{
			downButtons |= e.Button;

			ToolTipRenderer.ClearFloatingControls();
			ActiveControl = null; //helps panels like IRChooserPanel (for item/recipe choosing) close when we click on the graph

			mouseDownStartScreenPoint = MousePosition;
			Point graph_location = ScreenToGraph(e.Location);

			GraphElement? clickedElement = draggedLinkElement as GraphElement ?? GetNodeAtPoint(ScreenToGraph(e.Location));
			clickedElement?.MouseDown(graph_location, e.Button);

			if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Right))
			{
				ViewDragOriginPoint = graph_location;
			}
			else if (e.Button == MouseButtons.Left && clickedElement == null) //selection
			{
				SelectionZoneOriginPoint = graph_location;
				SelectionZone = new Rectangle();
				if ((ModifierKeys & Keys.Control) == 0 && (ModifierKeys & Keys.Alt) == 0) //clear all selected nodes if we arent using modifier keys
				{
					foreach (BaseNodeElement ne in selectedNodes)
						ne.Highlighted = false;
					selectedNodes.Clear();
				}
			}
		}

		private void ProductionGraphViewer_MouseUp(object? sender, MouseEventArgs e)
		{
			downButtons &= ~e.Button;

			ToolTipRenderer.ClearFloatingControls();
			Point graph_location = ScreenToGraph(e.Location);
			GraphElement? element = draggedLinkElement as GraphElement ?? GetNodeAtPoint(graph_location);

			switch (e.Button)
			{
				case MouseButtons.Right:
					if (viewBeingDragged)
						viewBeingDragged = false;
					else if (currentDragOperation == DragOperation.None && element == null) //right click on an empty space -> show add item/recipe menu
					{
						Point screenPoint = new(e.Location.X - 150, 15);
						screenPoint.X = Math.Max(15, Math.Min(Width - 650, screenPoint.X)); //want to position the recipe selector such that it is well visible.

						rightClickMenu.Items.Clear();
						rightClickMenu.Items.Add("Add Item", null,
							new EventHandler((o, ee) =>
							{
								AddItem(screenPoint, ScreenToGraph(e.Location));
							}));
						rightClickMenu.Items.Add("Add Recipe", null,
							new EventHandler((o, ee) =>
							{
								AddNewNode(screenPoint, ItemQualityPair.Default, ScreenToGraph(e.Location), NewNodeType.Disconnected);
							}));
						rightClickMenu.Show(this, e.Location);
					}
					else if(currentDragOperation != DragOperation.Selection)
						element?.MouseUp(graph_location, e.Button, (currentDragOperation == DragOperation.Item));
					break;
				case MouseButtons.Middle:
					viewBeingDragged = false;
					break;
				case MouseButtons.Left:
					//finished selecting the given zone (process selected nodes)
					if (currentDragOperation == DragOperation.Selection)
					{
						if ((Control.ModifierKeys & Keys.Alt) != 0) //removal zone processing
							selectedNodes.ExceptWith(currentSelectionNodes);
						else
						{
							if ((Control.ModifierKeys & Keys.Control) == 0) //if we arent using control, then we are just selecting
								selectedNodes.Clear();
							selectedNodes.UnionWith(currentSelectionNodes);
						}
						currentSelectionNodes.Clear();
					}
					//this is a release of a left click (non-drag operation) -> modify selection if clicking on node & using modifier keys
					else if (currentDragOperation == DragOperation.None && MouseDownElement is BaseNodeElement clickedNode)
					{
						if ((Control.ModifierKeys & Keys.Alt) != 0) //remove
						{
							selectedNodes.Remove(clickedNode);
							clickedNode.Highlighted = false;
							MouseDownElement = null;
							Invalidate();
						}
						else if ((Control.ModifierKeys & Keys.Control) != 0) //add if unselected, remove if selected
						{
							if (clickedNode.Highlighted)
								selectedNodes.Remove(clickedNode);
							else
								selectedNodes.Add(clickedNode);

							clickedNode.Highlighted = !clickedNode.Highlighted;
							MouseDownElement = null;
							Invalidate();
						}
						else if (!viewBeingDragged) //left click without modifier keys -> pass click to node
						{
							clickedNode.MouseUp(graph_location, e.Button, false);
						}
					}
					else if (!viewBeingDragged)
						element?.MouseUp(graph_location, e.Button, (currentDragOperation == DragOperation.Item));


					currentDragOperation = DragOperation.None;
					MouseDownElement = null;
					break;
			}
		}

		private void ProductionGraphViewer_MouseMove(object? sender, MouseEventArgs e)
		{
			downButtons &= Control.MouseButtons; //only care about those buttons that were pressed down on this control. This is also the best place to update mouse changes done outside the control (ex: clicking down, dragging outside the window, letting go, moving mouse back into window)

			Point graph_location = ScreenToGraph(e.Location);

			if (currentDragOperation != DragOperation.Selection) //dont care about element mouse move operations during selection operation
			{
				GraphElement? element = draggedLinkElement ?? MouseDownElement;
				element?.MouseMoved(graph_location);
			}

			switch (currentDragOperation)
			{
				case DragOperation.None: //check for minimal distance to be considered a drag operation
					Point dragDiff = Point.Subtract(Control.MousePosition, (Size)mouseDownStartScreenPoint);
					if (dragDiff.X * dragDiff.X + dragDiff.Y * dragDiff.Y > minDragDiff)
					{
						if ((downButtons & MouseButtons.Middle) == MouseButtons.Middle || (downButtons & MouseButtons.Right) == MouseButtons.Right)
							viewBeingDragged = true;

						if (MouseDownElement != null) //there is an item under the mouse during drag
							currentDragOperation = DragOperation.Item;
						else if ((downButtons & MouseButtons.Left) != 0)
							currentDragOperation = DragOperation.Selection;
					}
					break;

				case DragOperation.Item:
					if (MouseDownElement is not null && selectedNodes.Contains(MouseDownElement)) //dragging a group
					{
						Point startPoint = MouseDownElement.Location;
						GraphElement element = MouseDownElement;
						MouseDownElement.Dragged(graph_location);
						if (element == MouseDownElement) //check to ensure that the dragged operation hasnt changed the mousedown element -> as is the case with item tab to dragged link
						{
							Point endPoint = MouseDownElement.Location;
							if (startPoint != endPoint)
								foreach (BaseNodeElement node in selectedNodes.Where(node => node != MouseDownElement))
									node.SetLocation(new Point(node.X + endPoint.X - startPoint.X, node.Y + endPoint.Y - startPoint.Y));
							Invalidate();
						}
					}
					else //dragging single item
					{
						MouseDownElement?.Dragged(graph_location);
						Invalidate();
					}

					//accept middle mouse button for view dragging purposes (while dragging item or selection)
					if ((downButtons & MouseButtons.Middle) == MouseButtons.Middle)
						viewBeingDragged = true;
					break;

				case DragOperation.Selection:
					SelectionZone = new Rectangle(Math.Min(SelectionZoneOriginPoint.X, graph_location.X), Math.Min(SelectionZoneOriginPoint.Y, graph_location.Y), Math.Abs(SelectionZoneOriginPoint.X - graph_location.X), Math.Abs(SelectionZoneOriginPoint.Y - graph_location.Y));
					currentSelectionNodes.Clear();
					currentSelectionNodes.UnionWith(nodeElements.Where(element => element.IntersectsWithZone(SelectionZone, -20, -20)));

					UpdateSelection();

					//accept middle mouse button for view dragging purposes (while dragging item or selection)
					if ((downButtons & MouseButtons.Middle) == MouseButtons.Middle)
						viewBeingDragged = true;
					break;
			}

			//dragging view (can happen during any drag operation)
			if (viewBeingDragged)
			{
				ViewOffset = Point.Add(ViewOffset, (Size)Point.Subtract(graph_location, (Size)ViewDragOriginPoint));// new Point(ViewOffset.X + (int)((graph_location.X - lastMouseDragPoint.X) / ViewScale), ViewOffset.Y + (int)((graph_location.Y - lastMouseDragPoint.Y) / ViewScale));
				UpdateGraphBounds(MouseDownElement == null); //only hard limit the graph bounds if we arent dragging an object
			}

			Invalidate();
		}

		private void ProductionGraphViewer_MouseWheel(object? sender, MouseEventArgs e)
		{
			if (ContainsFocus && !this.Focused) //currently have a control created within this viewer active (ex: recipe chooser) -> dont want to scroll then
				return;

			ToolTipRenderer.ClearFloatingControls();

			Point oldZoomCenter = ScreenToGraph(e.Location);

			if (e.Delta > 0)
				ViewScale *= 1.1f;
			else
				ViewScale /= 1.1f;

			ViewScale = Math.Max(ViewScale, 0.01f);
			ViewScale = Math.Min(ViewScale, 2f);

			Point newZoomCenter = ScreenToGraph(e.Location);
			ViewOffset = new Point(ViewOffset.X + newZoomCenter.X - oldZoomCenter.X, ViewOffset.Y + newZoomCenter.Y - oldZoomCenter.Y);

			UpdateGraphBounds();
			Invalidate();
		}

		private void ProductionGraphViewer_KeyDown(object? sender, KeyEventArgs e)
		{
			if (currentDragOperation == DragOperation.None)
			{
				if ((e.KeyCode == Keys.C || e.KeyCode == Keys.X) && (e.Modifiers & Keys.Control) == Keys.Control) //copy or cut
				{
					StringBuilder stringBuilder = new();
					var writer = new JsonTextWriter(new StringWriter(stringBuilder));

					Graph.SerializeNodeIdSet = [];
					Graph.SerializeNodeIdSet.UnionWith(selectedNodes.Select(n => n.DisplayedNode.NodeID));

					JsonSerializer serialiser = JsonSerializer.Create();
					serialiser.Formatting = Formatting.None;
					serialiser.Serialize(writer, Graph);

					Graph.SerializeNodeIdSet.Clear();
					Graph.SerializeNodeIdSet = null;

					Clipboard.SetText(stringBuilder.ToString());

					if (e.KeyCode == Keys.X) //cut
						foreach (BaseNodeElement node in selectedNodes.ToList())
							Graph.DeleteNode(node.DisplayedNode);
				}
				else if (e.KeyCode == Keys.V && (e.Modifiers & Keys.Control) == Keys.Control) //paste
				{
					try
					{
						JObject json = JObject.Parse(Clipboard.GetText());
						ImportNodesFromJson(json, ScreenToGraph(PointToClient(Cursor.Position)), false);
					}
					catch { Console.WriteLine("Non-Foreman paste detected."); } //clipboard string wasnt a proper json object, or didnt process properly. Likely answer: was a clip NOT from foreman.
				}
			}
			else if (currentDragOperation == DragOperation.Selection) //possible changes to selection type
				UpdateSelection();

			bool lockDragAxis = (Control.ModifierKeys & Keys.Shift) != 0;
			if (Grid.LockDragToAxis != lockDragAxis)
			{
				Grid.LockDragToAxis = lockDragAxis;
				Grid.DragOrigin = Grid.AlignToGrid(MouseDownElement?.Location ?? new Point());
				if (currentDragOperation == DragOperation.Item)
					MouseDownElement?.Dragged(ScreenToGraph(PointToClient(Control.MousePosition)));
			}
			Invalidate();
		}

		private void ProductionGraphViewer_KeyUp(object? sender, KeyEventArgs e)
		{
			if (currentDragOperation == DragOperation.None)
			{
				switch (e.KeyCode)
				{
					case Keys.Delete:
						TryDeleteSelectedNodes();
						e.Handled = true;
						break;
				}
			}
			else if (currentDragOperation == DragOperation.Selection) //possible changes to selection type
				UpdateSelection();

			bool lockDragAxis = (Control.ModifierKeys & Keys.Shift) != 0;
			if (Grid.LockDragToAxis != lockDragAxis)
			{
				Grid.LockDragToAxis = lockDragAxis;
				Grid.DragOrigin = Grid.AlignToGrid(MouseDownElement?.Location ?? new Point());
				if (currentDragOperation == DragOperation.Item)
					MouseDownElement?.Dragged(ScreenToGraph(PointToClient(Control.MousePosition)));
			}
			Invalidate();
		}

		//----------------------------------------------Keyboard events

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) //arrow keys to move the current selection
		{
			bool processed = true;
			int moveUnit = (Grid.CurrentGridUnit > 0) ? Grid.CurrentGridUnit : 6;
			int panUnit = (int)(10 / ViewScale);
			if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) //large move
			{
				moveUnit = (Grid.CurrentMajorGridUnit > Grid.CurrentGridUnit) ? Grid.CurrentMajorGridUnit : moveUnit * 4;
				panUnit *= 5;
			}

			if ((keyData & Keys.KeyCode) == Keys.Left)
			{
				foreach (BaseNodeElement node in selectedNodes)
					node.SetLocation(new Point(node.X - moveUnit, node.Y));
			}
			else if ((keyData & Keys.KeyCode) == Keys.Right)
			{
				foreach (BaseNodeElement node in selectedNodes)
					node.SetLocation(new Point(node.X + moveUnit, node.Y));
			}
			else if ((keyData & Keys.KeyCode) == Keys.Up)
			{
				foreach (BaseNodeElement node in selectedNodes)
					node.SetLocation(new Point(node.X, node.Y - moveUnit));
			}
			else if ((keyData & Keys.KeyCode) == Keys.Down)
			{
				foreach (BaseNodeElement node in selectedNodes)
					node.SetLocation(new Point(node.X, node.Y + moveUnit));
			}
			else if ((keyData & Keys.KeyCode) == Keys.W && !SubwindowOpen)
			{
				ViewOffset += new Size(0, panUnit);
				UpdateGraphBounds();
			}
			else if ((keyData & Keys.KeyCode) == Keys.A && !SubwindowOpen)
			{
				ViewOffset += new Size(panUnit, 0);
				UpdateGraphBounds();
			}
			else if ((keyData & Keys.KeyCode) == Keys.S && !SubwindowOpen)
			{
				ViewOffset += new Size(0, -panUnit);
				UpdateGraphBounds();
			}
			else if ((keyData & Keys.KeyCode) == Keys.D && !SubwindowOpen)
			{
				ViewOffset += new Size(-panUnit, 0);
				UpdateGraphBounds();
			}
			else
				processed = false;

			if (processed)
			{
				Invalidate();
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		//----------------------------------------------Viewpoint events

		private void BGTimer_Tick(object? sender, EventArgs e)
		{
			//if (key)
		}

		private void ProductionGraphViewer_Resized(object? sender, EventArgs e)
		{
			UpdateGraphBounds();
			Invalidate();
		}

		private void ProductionGraphViewer_LostFocus(object? sender, EventArgs e)
		{
			Invalidate();
		}

		public void UpdateGraphBounds(bool limitView = true)
		{
			if (limitView)
			{
				Rectangle bounds = Graph.Bounds;
				Point screenCentre = ScreenToGraph(new Point(Width / 2, Height / 2));
				if (bounds.Width == 0 || bounds.Height == 0)
				{
					ViewOffset = new Point(0, 0);
				}
				else
				{
					int newX = ViewOffset.X;
					int newY = ViewOffset.Y;
					if (screenCentre.X < bounds.X) { newX -= bounds.X - screenCentre.X; }
					if (screenCentre.Y < bounds.Y) { newY -= bounds.Y - screenCentre.Y; }
					if (screenCentre.X > bounds.X + bounds.Width) { newX -= bounds.X + bounds.Width - screenCentre.X; }
					if (screenCentre.Y > bounds.Y + bounds.Height) { newY -= bounds.Y + bounds.Height - screenCentre.Y; }
					ViewOffset = new Point(newX, newY);
				}
			}

			VisibleGraphBounds = new Rectangle(
				(int)(-Width / (2 * ViewScale) - ViewOffset.X),
				(int)(-Height / (2 * ViewScale) - ViewOffset.Y),
				(int)(Width / ViewScale),
				(int)(Height / ViewScale));
		}

		private void ProductionGraphViewer_Resize(object? sender, EventArgs e)
		{
			ToolTipRenderer?.ClearFloatingControls(); //resize can happen before tooltip is created (due to scaling)
		}

		private void ProductionGraphViewer_Leave(object? sender, EventArgs e)
		{
			ToolTipRenderer.ClearFloatingControls();
		}

		//----------------------------------------------Helper functions (point conversions, alignment, etc)

		public Point ScreenToGraph(Point point)
		{
			return new Point(Convert.ToInt32(((point.X - Width / 2) / ViewScale) - ViewOffset.X), Convert.ToInt32(((point.Y - Height / 2) / ViewScale) - ViewOffset.Y));
		}

		public Point GraphToScreen(Point point)
		{
			return new Point(Convert.ToInt32(((point.X + ViewOffset.X) * ViewScale) + Width / 2), Convert.ToInt32(((point.Y + ViewOffset.Y) * ViewScale) + Height / 2));
		}

		//----------------------------------------------Save/Load JSON functions

		public void ImportNodesFromJson(JObject json, Point origin, bool loadSolverValues)
		{
			ProductionGraph.NewNodeCollection newNodeCollection = newNodeCollection = Graph.InsertNodesFromJson(DCache, json, loadSolverValues); //NOTE: missing items & recipes may be added here!
			if (newNodeCollection == null || newNodeCollection.NewNodes.Count == 0)
				return;

			//update the locations of the new nodes to be centered around the mouse position (as opposed to wherever they were before)
			long xAve = 0;
			long yAve = 0;
			foreach (ReadOnlyBaseNode newNode in newNodeCollection.NewNodes)
			{
				xAve += newNode.Location.X;
				yAve += newNode.Location.Y;
			}
			xAve /= newNodeCollection.NewNodes.Count;
			yAve /= newNodeCollection.NewNodes.Count;

			Point importCenter = new((int)xAve, (int)yAve);
			Size offset = (Size)Grid.AlignToGrid(Point.Subtract(origin, (Size)importCenter));
			foreach (ReadOnlyBaseNode newNode in newNodeCollection.NewNodes)
				Graph.RequestNodeController(newNode)?.SetLocation(Point.Add(newNode.Location, offset));

			//update the selection to be just the newly imported nodes
			ClearSelection();
			foreach (BaseNodeElement newNodeElement in newNodeCollection.NewNodes.Select(node => nodeElementDictionary[node]))
			{
				selectedNodes.Add(newNodeElement);
				newNodeElement.Highlighted = true;
			}
			Console.WriteLine(selectedNodes.Count);

			UpdateGraphBounds();
			Graph.UpdateNodeValues();
		}

		public void LoadPreset(Preset preset)
		{
			if (ParentForm is null)
				throw new InvalidOperationException("ParentForm is null");
			using (DataLoadForm form = new(preset))
			{
				form.StartPosition = FormStartPosition.Manual;
				form.Left = ParentForm.Left + 150;
				form.Top = ParentForm.Top + 200;
				DialogResult result = form.ShowDialog(); //LOAD FACTORIO DATA
				DCache?.Clear();
				DCache = form.GetDataCache();
				LastAssemblerQuality = DCache.DefaultQuality; //QUALITY UPDATE
				Graph.DefaultAssemblerQuality = DCache.DefaultQuality;
				Graph.MaxQualitySteps = 5; //DCache.QualityMaxChainLength;

				if (result == DialogResult.Abort)
				{
					MessageBox.Show("The current preset (" + Properties.Settings.Default.CurrentPresetName + ") is corrupt. Switching to the default preset (Factorio 2.0 Vanilla)");
					Properties.Settings.Default.CurrentPresetName = MainForm.DefaultPreset;
					using DataLoadForm form2 = new(new Preset(MainForm.DefaultPreset, false, true));
					form2.StartPosition = FormStartPosition.Manual;
					form2.Left = ParentForm.Left + 150;
					form2.Top = ParentForm.Top + 200;
					DialogResult result2 = form2.ShowDialog(); //LOAD default preset
					DCache?.Clear();
					DCache = form2.GetDataCache();
					if (result2 == DialogResult.Abort)
						MessageBox.Show("The default preset (" + Properties.Settings.Default.CurrentPresetName + ") is corrupt. No Preset is loaded!");
				}
				GC.Collect(); //loaded a new data cache - the old one should be collected (data caches can be over 1gb in size due to icons, plus whatever was in the old graph)
			}
			Invalidate();
		}

		public async Task LoadFromJson(JObject json, bool useFirstPreset, bool setEnablesFromJson)
		{
			if (ParentForm is null)
				throw new InvalidOperationException("ParentForm is null");
			if (json[nameof(Version)]?.Value<int>() != Properties.Settings.Default.ForemanVersion || json[nameof(Object)]?.Value<string>() != Object) // Object == "ProductionGraphViewer"
			{
				json = VersionUpdater.UpdateSave(json, DCache);
				if (json["ProductionGraph"] is not JObject jobj)
					throw new InvalidDataException("ProductionGraph invalid in JSON file");
				VersionUpdater.UpdateGraph(jobj, DCache);
			}

			if (json["ProductionGraph"] is not JObject jProdGraph)
				throw new InvalidDataException("ProductionGraph invalid in JSON file");

			//grab mod list
			Dictionary<string, string> modSet = [];
			foreach (string str in json[nameof(IncludedMods)]?.Select(t => t.Value<string>()).OfType<string>().ToList() ?? [])
			{
				string[] mod = str.Split('|');
				modSet.Add(mod[0], mod[1]);
			}

			//grab include lists
			List<string> itemNames = jProdGraph["IncludedItems"]?.Select(t => t.Value<string>()).OfType<string>().ToList() ?? [];
			List<string> assemblerNames = jProdGraph["IncludedAssemblers"]?.Select(t => t.Value<string>()).OfType<string>().ToList() ?? [];
			List<string> qualityNames = jProdGraph["IncludedQualities"]?.Select(t => t["Key"]?.Value<string>()).OfType<string>().ToList() ?? [];
            List<RecipeShort> recipeShorts = RecipeShort.GetSetFromJson(jProdGraph["IncludedRecipes"] ?? new JObject());
			List<PlantShort> plantShorts = PlantShort.GetSetFromJson(jProdGraph["IncludedPlantProcesses"] ?? new JObject());

			//now - two options:
			// a) we are told to use the first preset (basically, the selected preset) - so that is the only one added to the possible Presets
			// b) we can choose preset - so go through each one and compare mod lists - ask to continue if
			// the preset list will then be checked for compatibility based on recipes, and the one with least errors will be used.
			// any errors will prompt a message box saying that 'incompatibility was found, but proceeding anyways'.
			List<Preset> allPresets = MainForm.GetValidPresetsList();
			List<PresetErrorPackage> presetErrors = [];
			Preset? chosenPreset = null;
			if (useFirstPreset)
				chosenPreset = allPresets[0];
			else
			{
				//test for the preset specified in the json save
				Preset? savedWPreset = allPresets.FirstOrDefault(p => p.Name == json[nameof(SavedPresetName)]?.Value<string>());
				if (savedWPreset != null)
				{
					var errors = await PresetProcessor.TestPreset(savedWPreset, modSet, itemNames, qualityNames, recipeShorts, plantShorts);
					if (errors != null && errors.ErrorCount == 0) //no errors found here. We will then use this exact preset and not search for a different one
						chosenPreset = savedWPreset;
					else
					{
						//errors found. even though the name fits, but the preset seems to be the wrong one. Proceed with searching for best-fit
						if(errors != null)
							presetErrors.Add(errors);
						allPresets.Remove(savedWPreset);
					}
				}

				//havent found the preset, or it returned some errors (not good) -> have to search for best fit (and leave the decision to user if we have multiple)
				if (chosenPreset == null)
				{
					foreach (Preset preset in allPresets)
					{
						PresetErrorPackage errors = await PresetProcessor.TestPreset(preset, modSet, itemNames, qualityNames, recipeShorts, plantShorts);
						if (errors != null)
							presetErrors.Add(errors);
					}

					//show the menu to select the preferred preset
					using PresetSelectionForm form = new(presetErrors);
					form.StartPosition = FormStartPosition.Manual;
					form.Left = ParentForm.Left + 50;
					form.Top = ParentForm.Top + 50;

					if (form.ShowDialog() != DialogResult.OK || form.ChosenPreset == null) //null check is not necessary - if we get an ok dialogresult, we know it will be set
						return;
					chosenPreset = form.ChosenPreset;
					Properties.Settings.Default.CurrentPresetName = chosenPreset.Name;
					Properties.Settings.Default.Save();
				}
				else if (chosenPreset.Name != Properties.Settings.Default.CurrentPresetName) //we had to switch the preset to a new one (without the user having to select a preset from a list)
				{
					MessageBox.Show(string.Format("Loaded graph uses a different Preset.\nPreset switched from \"{0}\" to \"{1}\"", Properties.Settings.Default.CurrentPresetName, chosenPreset.Name));
					Properties.Settings.Default.CurrentPresetName = chosenPreset.Name;
					Properties.Settings.Default.Save();
				}
			}

			//clear graph
			ClearGraph();

			//load new preset
			LoadPreset(chosenPreset);

			//set up graph options
			Graph.SelectedRateUnit = json[nameof(Unit)]?.ToObject<ProductionGraph.RateUnit>() ?? ProductionGraph.RateUnit.Per1Sec;
			Graph.AssemblerSelector.DefaultSelectionStyle = json[nameof(AssemblerSelectorStyle)]?.ToObject<AssemblerSelector.Style>() ?? AssemblerSelector.Style.Worst;
			Graph.ModuleSelector.DefaultSelectionStyle = json[nameof(ModuleSelectorStyle)]?.ToObject<ModuleSelector.Style>() ?? ModuleSelector.Style.None;
			foreach (string fuelType in json[nameof(FuelPriorityList)]?.Select(t => t.Value<string>()).OfType<string>() ?? [])
				if (DCache.Items.ContainsKey(fuelType))
					Graph.FuelSelector.UseFuel(DCache.Items[fuelType]);
			Graph.EnableExtraProductivityForNonMiners = json[nameof(ExtraProdForNonMiners)]?.Value<bool>() ?? false;

			//set up graph view options
			string[] viewOffsetString = (json[nameof(ViewOffset)]?.Value<string>() ?? "0,0").Split(',');
			ViewOffset = new Point(int.Parse(viewOffsetString[0]), int.Parse(viewOffsetString[1]));
			ViewScale = json[nameof(ViewScale)]?.Value<float>() ?? 1;

			//update enabled statuses
			if (setEnablesFromJson)
			{
				foreach (IBeacon beacon in DCache.Beacons.Values)
					beacon.Enabled = false;
				foreach (string beacon in json[nameof(EnabledBeacons)]?.Select(t => t.Value<string>()).OfType<string>().ToList() ?? [])
					if (DCache.Beacons.ContainsKey(beacon))
						DCache.Beacons[beacon].Enabled = true;

				foreach (IAssembler assembler in DCache.Assemblers.Values)
					assembler.Enabled = false;
				foreach (string name in json[nameof(EnabledAssemblers)]?.Select(t => t.Value<string>()).OfType<string>().ToList() ?? [])
					if (DCache.Assemblers.ContainsKey(name))
						DCache.Assemblers[name].Enabled = true;
				DCache.RocketAssembler.Enabled = DCache.Assemblers["rocket-silo"]?.Enabled ?? false;

				foreach (IModule module in DCache.Modules.Values)
					module.Enabled = false;
				foreach (string name in json[nameof(EnabledModules)]?.Select(t => t.Value<string>()).OfType<string>().ToList() ?? [])
					if (DCache.Modules.ContainsKey(name))
						DCache.Modules[name].Enabled = true;

				foreach (IRecipe recipe in DCache.Recipes.Values)
					recipe.Enabled = false;
				foreach (string recipe in json[nameof(EnabledRecipes)]?.Select(t => t.Value<string>()).OfType<string>().ToList() ?? [])
					if (DCache.Recipes.ContainsKey(recipe))
						DCache.Recipes[recipe].Enabled = true;
			}

			//add all nodes
			ProductionGraph.NewNodeCollection collection = Graph.InsertNodesFromJson(DCache, jProdGraph, true);

			//check for old import
			if (json["OldImport"] is not null)
				foreach (var rNode in collection.NewNodes.OfType<ReadOnlyRecipeNode>())
					(Graph.RequestNodeController(rNode) as RecipeNodeController)?.AutoSetAssembler(AssemblerSelector.Style.BestNonBurner);

			//upgrade graph & values
			UpdateGraphBounds();
			Graph.UpdateNodeValues();
			this.Focus();
			Invalidate();
		}

		//Stolen from the designer file
		protected override void Dispose(bool disposing)
		{
			ClearGraph();


			if (disposing && (components != null))
			{
				components.Dispose();
			}

			rightClickMenu.Dispose();

			base.Dispose(disposing);
		}
	}
}
