﻿using Foreman.DataCache;
using Foreman.DataCache.DataTypes;
using Foreman.Models;
using Foreman.Models.Nodes;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;

namespace Foreman {
	public enum NodeType : int { Supplier, Consumer, Passthrough, Recipe, Spoil, Plant }
	public enum LinkType { Input, Output }

	public class NodeEventArgs(ReadOnlyBaseNode node) : EventArgs
	{
		public ReadOnlyBaseNode node = node;
	}
	public class NodeLinkEventArgs(ReadOnlyNodeLink nodeLink) : EventArgs
	{
		public ReadOnlyNodeLink nodeLink = nodeLink;
	}

	[JsonObject(MemberSerialization.OptIn)]
	public partial class ProductionGraph {
		public class NewNodeCollection {
			public List<ReadOnlyBaseNode> NewNodes { get; private set; }
			public List<ReadOnlyNodeLink> NewLinks { get; private set; }
			public NewNodeCollection() { NewNodes = []; NewLinks = []; }
		}

		//public DataCache DCache { get; private set; }

		public enum RateUnit : int { Per1Sec, Per1Min, Per5Min, Per10Min, Per30Min, Per1Hour };//, Per6Hour, Per12Hour, Per24Hour }
		public static readonly string[] RateUnitNames = ["1 sec", "1 min", "5 min", "10 min", "30 min", "1 hour"]; //, "6 hours", "12 hours", "24 hours" };
		private static readonly float[] RateMultiplier = [1f, 60f, 300f, 600f, 1800f, 3600f]; //, 21600f, 43200f, 86400f };

		public RateUnit SelectedRateUnit { get; set; }
		public float GetRateMultipler() { return RateMultiplier[(int)SelectedRateUnit]; } //the amount of assemblers required will be multipled by the rate multipler when displaying.
		public string GetRateName() { return RateUnitNames[(int)SelectedRateUnit]; }

		[JsonProperty]
		public NodeDirection DefaultNodeDirection { get; set; }
		public bool DefaultToSimplePassthroughNodes { get; set; }

		public const double MaxSetFlow = 1e7; //10 million (per second) item flow should be enough for pretty much everything with a generous helping of 'oh god thats way too much!'
		public const double MaxFactories = 1e6; //1 million factories should be good enough as well. NOTE: the auto values can go higher, you just cant set more than 1 million on the manual setting.
		public const double MaxTiles = 1e7; //10 million tiles for planting should be good enough
		public const double MaxInventorySlots = 1e6; // 1 million inventory slots for spoiling should be good enough
		private const int XBorder = 200;
		private const int YBorder = 200;

		public bool PauseUpdates { get; set; }
		[JsonProperty("Solver_PullOutputNodes")]
		public bool PullOutputNodes { get; set; } //if true, the solver will add a 'pull' for output nodes so as to prioritize them over lowering factory count. WARNING: this can lead to '0' solutions if there is any production path that can go to infinity (aka: ensure enough nodes are constrained!)
		[JsonProperty("Solver_PullOutputNodesPower")]
		public double PullOutputNodesPower { get; set; }
		[JsonProperty("Solver_LowPriorityPower")]
		public double LowPriorityPower { get; set; } //this is the multiplier of the factory cost function for low priority nodes. aka: low priority recipes will be picked if the alternative involves this much more factories (10,000 is a nice value here)
		[JsonProperty]
		public bool EnableExtraProductivityForNonMiners { get; set; }

		public AssemblerSelector AssemblerSelector { get; private set; }
		public ModuleSelector ModuleSelector { get; private set; }
		public FuelSelector FuelSelector { get; private set; }

		public IEnumerable<ReadOnlyBaseNode> Nodes { get { return nodes.Select(node => node.ReadOnlyNode); } }
		public IEnumerable<ReadOnlyNodeLink> NodeLinks { get { return nodeLinks.Select(link => link.ReadOnlyLink); } }
		public HashSet<int>? SerializeNodeIdSet { get; set; } //if this isnt null then the serialized production graph will only contain these nodes (and links between them)

		//editing this value will require the entire graph to be updated as any recipe nodes on it will possibly change the number of products and possibly cause a cascade of removed links
		private uint maxQualitySteps;
		[JsonProperty]
		public uint MaxQualitySteps {
			get { return maxQualitySteps; }
			set {
				if (value != maxQualitySteps) {
					maxQualitySteps = value;
					foreach (BaseNode node in nodes) {
						if (node is RecipeNode rnode)
							rnode.MaxQualitySteps = maxQualitySteps;
					}
				}
			}
		}

		public IQuality DefaultAssemblerQuality { get; set; } = DCache.defaultDCache.DefaultQuality;

		public event EventHandler<NodeEventArgs>? NodeAdded;
		public event EventHandler<NodeEventArgs>? NodeDeleted;
		public event EventHandler<NodeLinkEventArgs>? LinkAdded;
		public event EventHandler<NodeLinkEventArgs>? LinkDeleted;
		public event EventHandler<EventArgs>? NodeValuesUpdated;

		public Rectangle Bounds {
			get {
				if (nodes.Count == 0)
					return new Rectangle(0, 0, 0, 0);

				int xMin = int.MaxValue;
				int yMin = int.MaxValue;
				int xMax = int.MinValue;
				int yMax = int.MinValue;
				foreach (BaseNode node in nodes) {
					xMin = Math.Min(xMin, node.Location.X);
					xMax = Math.Max(xMax, node.Location.X);
					yMin = Math.Min(yMin, node.Location.Y);
					yMax = Math.Max(yMax, node.Location.Y);
				}

				return new Rectangle(xMin - XBorder, yMin - YBorder, xMax - xMin + (2 * XBorder), yMax - yMin + (2 * YBorder));
			}
		}

		private readonly HashSet<BaseNode> nodes;
		private readonly HashSet<NodeLink> nodeLinks;
		private readonly Dictionary<ReadOnlyBaseNode, BaseNode> roToNode;
		private readonly Dictionary<ReadOnlyNodeLink, NodeLink> roToLink;
		private int lastNodeID;

		public ProductionGraph() {
			DefaultNodeDirection = NodeDirection.Up;
			PullOutputNodes = false;
			PullOutputNodesPower = 10;
			LowPriorityPower = 1e5;

			nodes = [];
			nodeLinks = [];
			roToNode = [];
			roToLink = [];
			lastNodeID = 0;

			AssemblerSelector = new AssemblerSelector();
			ModuleSelector = new ModuleSelector();
			FuelSelector = new FuelSelector();
		}

		public BaseNodeController? RequestNodeController(ReadOnlyBaseNode node) { if (roToNode.TryGetValue(node, out BaseNode? value)) return value.Controller; return null; }

		public ReadOnlyConsumerNode CreateConsumerNode(ItemQualityPair item, Point location) {
			ConsumerNode node = new(this, lastNodeID++, item) {
				Location = location,
				NodeDirection = DefaultNodeDirection
			};
			nodes.Add(node);
			roToNode.Add(node.ReadOnlyNode, node);
			node.UpdateState();
			NodeAdded?.Invoke(this, new NodeEventArgs(node.ReadOnlyNode));
			return (ReadOnlyConsumerNode)node.ReadOnlyNode;
		}

		public ReadOnlySupplierNode CreateSupplierNode(ItemQualityPair item, Point location) {
			SupplierNode node = new(this, lastNodeID++, item) {
				Location = location,
				NodeDirection = DefaultNodeDirection
			};
			nodes.Add(node);
			roToNode.Add(node.ReadOnlyNode, node);
			node.UpdateState();
			NodeAdded?.Invoke(this, new NodeEventArgs(node.ReadOnlyNode));
			return (ReadOnlySupplierNode)node.ReadOnlyNode;
		}

		public ReadOnlyPassthroughNode CreatePassthroughNode(ItemQualityPair item, Point location) {
			PassthroughNode node = new(this, lastNodeID++, item) {
				Location = location,
				NodeDirection = DefaultNodeDirection,
				SimpleDraw = DefaultToSimplePassthroughNodes
			};
			nodes.Add(node);
			roToNode.Add(node.ReadOnlyNode, node);
			node.UpdateState();
			NodeAdded?.Invoke(this, new NodeEventArgs(node.ReadOnlyNode));
			return (ReadOnlyPassthroughNode)node.ReadOnlyNode;
		}

		public ReadOnlySpoilNode CreateSpoilNode(ItemQualityPair inputItem, IItem outputItem, Point location) {
			SpoilNode node = new(this, lastNodeID++, inputItem, outputItem) {
				Location = location,
				NodeDirection = DefaultNodeDirection
			};
			nodes.Add(node);
			roToNode.Add(node.ReadOnlyNode, node);
			node.UpdateState();
			NodeAdded?.Invoke(this, new NodeEventArgs(node.ReadOnlyNode));
			return (ReadOnlySpoilNode)node.ReadOnlyNode;
		}

		public ReadOnlyPlantNode CreatePlantNode(IPlantProcess plantProcess, IQuality quality, Point location) {
			PlantNode node = new(this, lastNodeID++, plantProcess, quality) {
				Location = location,
				NodeDirection = DefaultNodeDirection
			};
			nodes.Add(node);
			roToNode.Add(node.ReadOnlyNode, node);
			node.UpdateState();
			NodeAdded?.Invoke(this, new NodeEventArgs(node.ReadOnlyNode));
			return (ReadOnlyPlantNode)node.ReadOnlyNode;
		}

		public ReadOnlyRecipeNode CreateRecipeNode(RecipeQualityPair recipe, Point location) { return CreateRecipeNode(recipe, location, null); }
		private ReadOnlyRecipeNode CreateRecipeNode(RecipeQualityPair recipe, Point location, Action<RecipeNode>? nodeSetupAction) //node setup action is used to populate the node prior to informing everyone of its creation
		{
			if (DefaultAssemblerQuality is null)
				throw new InvalidOperationException("DefaultAssemblerQuality is null");
			RecipeNode node = new(this, lastNodeID++, recipe, DefaultAssemblerQuality) {
				Location = location,
				NodeDirection = DefaultNodeDirection
			};
			nodeSetupAction?.Invoke(node);
			if (nodeSetupAction == null) {
				RecipeNodeController rnController = (RecipeNodeController)node.Controller;
				rnController.AutoSetAssembler();
				rnController.AutoSetAssemblerModules();
			}
			nodes.Add(node);
			roToNode.Add(node.ReadOnlyNode, node);
			node.UpdateInputsAndOutputs();
			NodeAdded?.Invoke(this, new NodeEventArgs(node.ReadOnlyNode));
			return (ReadOnlyRecipeNode)node.ReadOnlyNode;
		}

		public ReadOnlyNodeLink CreateLink(ReadOnlyBaseNode supplier, ReadOnlyBaseNode consumer, ItemQualityPair item) {
			if (!roToNode.ContainsKey(supplier) || !roToNode.ContainsKey(consumer) || !supplier.Outputs.Contains(item) || !consumer.Inputs.Contains(item))
				Trace.Fail(string.Format("Node link creation called with invalid parameters! consumer:{0}. supplier:{1}. item:{2}.", consumer.ToString(), supplier.ToString(), item.ToString()));
			if (supplier.OutputLinks.Any(l => l.Item == item && l.Consumer == consumer)) //check for an already existing connection
				return supplier.OutputLinks.First(l => l.Item == item && l.Consumer == consumer);

			BaseNode supplierNode = roToNode[supplier];
			BaseNode consumerNode = roToNode[consumer];

			NodeLink link = new(this, supplierNode, consumerNode, item);
			supplierNode.OutputLinks.Add(link);
			consumerNode.InputLinks.Add(link);
			LinkChangeUpdateImpactedNodeStates(link, LinkType.Input);
			LinkChangeUpdateImpactedNodeStates(link, LinkType.Output);

			nodeLinks.Add(link);
			roToLink.Add(link.ReadOnlyLink, link);
			LinkAdded?.Invoke(this, new NodeLinkEventArgs(link.ReadOnlyLink));
			return link.ReadOnlyLink;
		}

		public void DeleteNode(ReadOnlyBaseNode node) {
			if (!roToNode.ContainsKey(node))
				Trace.Fail(string.Format("Node deletion called on a node ({0}) that isnt part of the graph!", node.ToString()));

			foreach (ReadOnlyNodeLink link in node.InputLinks.ToList())
				DeleteLink(link);
			foreach (ReadOnlyNodeLink link in node.OutputLinks.ToList())
				DeleteLink(link);

			nodes.Remove(roToNode[node]);
			roToNode.Remove(node);
			NodeDeleted?.Invoke(this, new NodeEventArgs(node));
		}

		public void DeleteNodes(IEnumerable<ReadOnlyBaseNode> nodes) {
			foreach (ReadOnlyBaseNode node in nodes)
				DeleteNode(node);
		}

		public void DeleteLink(ReadOnlyNodeLink link) {
			if (!roToLink.ContainsKey(link) || !roToNode.ContainsKey(link.Consumer) || !roToNode.ContainsKey(link.Supplier))
				Trace.Fail(string.Format("Link deletion called with a link ({0}) that isnt part of the graph, or whose node(s) ({1}), ({2}) is/are not part of the graph!", link.ToString(), link.Consumer.ToString(), link.Supplier.ToString()));

			NodeLink nodeLink = roToLink[link];
			nodeLink.ConsumerNode.InputLinks.Remove(nodeLink);
			nodeLink.SupplierNode.OutputLinks.Remove(nodeLink);
			LinkChangeUpdateImpactedNodeStates(nodeLink, LinkType.Input);
			LinkChangeUpdateImpactedNodeStates(nodeLink, LinkType.Output);

			nodeLinks.Remove(nodeLink);
			roToLink.Remove(link);
			LinkDeleted?.Invoke(this, new NodeLinkEventArgs(link));
		}

		public void ClearGraph() {
			foreach (BaseNode node in nodes.ToList())
				DeleteNode(node.ReadOnlyNode);

			SerializeNodeIdSet = null;
			lastNodeID = 0;
		}

		public void UpdateNodeMaxQualities() {
			foreach (RecipeNode rnode in nodes.Where(n => n is RecipeNode).Cast<RecipeNode>()) {
				rnode.UpdateInputsAndOutputs(true);
				rnode.UpdateState();
			}
		}

		public void UpdateNodeStates(bool markAllAsDirty) {
			foreach (BaseNode node in nodes)
				node.UpdateState(markAllAsDirty);
		}

		public IEnumerable<ReadOnlyBaseNode> GetSuppliers(ItemQualityPair item) {
			foreach (ReadOnlyBaseNode node in Nodes)
				if (node.Outputs.Contains(item))
					yield return node;
		}

		public IEnumerable<ReadOnlyBaseNode> GetConsumers(ItemQualityPair item) {
			foreach (ReadOnlyBaseNode node in Nodes)
				if (node.Inputs.Contains(item))
					yield return node;
		}

		public IEnumerable<IEnumerable<ReadOnlyBaseNode>> GetConnectedNodeGroups(bool includeCleanComponents) { foreach (IEnumerable<BaseNode> group in GetConnectedComponents(includeCleanComponents)) yield return group.Select(node => node.ReadOnlyNode); }

		private List<HashSet<BaseNode>> GetConnectedComponents(bool includeCleanComponents) //used to break the graph into groups (in case there are multiple disconnected groups) for simpler solving. Clean components refer to node groups where all the nodes inside the group havent had any changes since last solve operation
		{
			//there is an optimized solution for connected components where we keep track of the various groups and modify them as each node/link is added/removed, but testing shows that this calculation below takes under 1ms even for larg 1000+ node graphs, so why bother.


			HashSet<BaseNode> unvisitedNodes = new(nodes);

			List<HashSet<BaseNode>> connectedComponents = [];

			while (unvisitedNodes.Count != 0) {
				HashSet<BaseNode> newSet = [];
				bool allClean = true;

				HashSet<BaseNode> toVisitNext = [unvisitedNodes.First()];

				while (toVisitNext.Count != 0) {
					BaseNode currentNode = toVisitNext.First();
					allClean &= currentNode.IsClean;

					foreach (NodeLink link in currentNode.InputLinks)
						if (unvisitedNodes.Contains(link.SupplierNode))
							toVisitNext.Add(link.SupplierNode);

					foreach (NodeLink link in currentNode.OutputLinks)
						if (unvisitedNodes.Contains(link.ConsumerNode))
							toVisitNext.Add(link.ConsumerNode);

					newSet.Add(currentNode);
					toVisitNext.Remove(currentNode);
					unvisitedNodes.Remove(currentNode);
				}

				if (!allClean || includeCleanComponents)
					connectedComponents.Add(newSet);
			}
			return connectedComponents;
		}

		public void UpdateNodeValues() {
			if (!PauseUpdates) {
				try { OptimizeGraphNodeValues(); } catch (OverflowException) { } //overflow can theoretically be possible for extremely unbalanced recipes, but with the limit of double and the artificial limit set on max throughput this should never happen.
			}
			NodeValuesUpdated?.Invoke(this, EventArgs.Empty); //called even if no changes have been made in order to re-draw the graph (since something required a node value update - link deletion? node addition? whatever)
		}

		private static void LinkChangeUpdateImpactedNodeStates(NodeLink link, LinkType direction) //helper function to update all the impacted nodes after addition/removal of a given link. Basically we want to update any node connected to this link through passthrough nodes (or directly).
		{
			HashSet<NodeLink> visitedLinks = []; //to prevent a loop
			void Internal_UpdateLinkedNodes(NodeLink ilink) {
				if (visitedLinks.Contains(ilink))
					return;
				visitedLinks.Add(ilink);

				if (direction == LinkType.Output) {
					ilink.ConsumerNode.UpdateState();
					if (ilink.ConsumerNode is PassthroughNode)
						foreach (NodeLink secondaryLink in ilink.ConsumerNode.OutputLinks)
							Internal_UpdateLinkedNodes(secondaryLink);
				} else {
					ilink.SupplierNode.UpdateState();
					if (ilink.SupplierNode is PassthroughNode)
						foreach (NodeLink secondaryLink in ilink.SupplierNode.InputLinks)
							Internal_UpdateLinkedNodes(secondaryLink);

				}
			}

			Internal_UpdateLinkedNodes(link);
		}

		//----------------------------------------------Save/Load JSON functions

		private class JSONSerialisationData(HashSet<BaseNode> nodes, HashSet<NodeLink> nodeLinks) {
			public HashSet<BaseNode> includedNodes = nodes;
			public HashSet<NodeLink> includedLinks = nodeLinks;
			public HashSet<string> includedItems = [];
			public HashSet<string> includedAssemblers = [];
			public HashSet<string> includedModules = [];
			public HashSet<string> includedBeacons = [];
			public HashSet<KeyValuePair<string, int>> includedQualities = []; //name,level
			public List<RecipeShort> includedRecipeShorts = [];
			public List<PlantShort> includedPlantShorts = [];
		}

		[NonSerialized]
		private JSONSerialisationData jData = new([], []);

		[OnSerializing]
		internal void OnSerializingMethod(StreamingContext ctx) {
			if (DefaultAssemblerQuality is null)
				throw new InvalidOperationException("Cannot serialise when DefaultAssemblerQuality is null");
			//collect the set of nodes and links to be saved (either entire set, or only that which is bound by the specified serialized node list)
			jData = new(nodes, nodeLinks);
			if (SerializeNodeIdSet != null) {
				jData.includedNodes = new HashSet<BaseNode>(nodes.Where(node => SerializeNodeIdSet.Contains(node.NodeID)));
				jData.includedLinks = [];
				foreach (NodeLink link in nodeLinks)
					if (jData.includedNodes.Contains(link.ConsumerNode) && jData.includedNodes.Contains(link.SupplierNode))
						jData.includedLinks.Add(link);
			}

			//prepare list of items/assemblers/modules/beacons/recipes that are part of the saved set. Recipes have to include a missing component due to the possibility of different recipes having same name (ex: regular iron.recipe, missing iron.recipe, missing iron.recipe #2)

			HashSet<IRecipe> includedRecipes = [];
			HashSet<IRecipe> includedMissingRecipes = new(new RecipeNaInPrComparer()); //compares by name, ingredients, and products (not amounts, just items)
			HashSet<IPlantProcess> includedPlantProcesses = [];
			HashSet<IPlantProcess> includedMissingPlantProcesses = new(new PlantNaInPrComparer());
			jData.includedQualities.Add(new KeyValuePair<string, int>(DefaultAssemblerQuality.Name, DefaultAssemblerQuality.Level));

			foreach (BaseNode node in jData.includedNodes) {
				switch (node) {
					case RecipeNode rnode:
						if (rnode.BaseRecipe.Recipe.IsMissing)
							includedMissingRecipes.Add(rnode.BaseRecipe.Recipe);
						else
							includedRecipes.Add(rnode.BaseRecipe.Recipe);

						jData.includedAssemblers.Add(rnode.SelectedAssembler.Assembler.Name);

						if (rnode.SelectedBeacon is BeaconQualityPair bqp)
							jData.includedBeacons.Add(bqp.Beacon.Name);

						jData.includedModules.UnionWith(rnode.AssemblerModules.Select(m => m.Module.Name));
						jData.includedModules.UnionWith(rnode.BeaconModules.Select(m => m.Module.Name));

						jData.includedQualities.Add(new KeyValuePair<string, int>(rnode.BaseRecipe.Quality.Name, rnode.BaseRecipe.Quality.Level));
						jData.includedQualities.Add(new KeyValuePair<string, int>(rnode.SelectedAssembler.Quality.Name, rnode.SelectedAssembler.Quality.Level));

						if (rnode.SelectedBeacon is BeaconQualityPair)
							jData.includedQualities.Add(new KeyValuePair<string, int>(rnode.BaseRecipe.Quality.Name, rnode.BaseRecipe.Quality.Level));

						jData.includedQualities.UnionWith(rnode.AssemblerModules.Select(m => new KeyValuePair<string, int>(m.Quality.Name, m.Quality.Level)));
						jData.includedQualities.UnionWith(rnode.BeaconModules.Select(m => new KeyValuePair<string, int>(m.Quality.Name, m.Quality.Level)));
						break;
					case PlantNode pnode:
						if (pnode.BasePlantProcess.IsMissing)
							includedMissingPlantProcesses.Add(pnode.BasePlantProcess);
						else
							includedPlantProcesses.Add(pnode.BasePlantProcess);
						jData.includedQualities.Add(new KeyValuePair<string, int>(pnode.Seed.Quality.Name, pnode.Seed.Quality.Level));
						break;
					case ConsumerNode cnode:
						jData.includedQualities.Add(new KeyValuePair<string, int>(cnode.ConsumedItem.Quality.Name, cnode.ConsumedItem.Quality.Level));
						break;
					case SupplierNode snode:
						jData.includedQualities.Add(new KeyValuePair<string, int>(snode.SuppliedItem.Quality.Name, snode.SuppliedItem.Quality.Level));
						break;
					case PassthroughNode passnode:
						jData.includedQualities.Add(new KeyValuePair<string, int>(passnode.PassthroughItem.Quality.Name, passnode.PassthroughItem.Quality.Level));
						break;
					case SpoilNode spoilnode:
						jData.includedQualities.Add(new KeyValuePair<string, int>(spoilnode.InputItem.Quality.Name, spoilnode.InputItem.Quality.Level));
						break;
				}

				//these will process all inputs/outputs -> so fuel/burnt items are included automatically!
				jData.includedItems.UnionWith(node.Inputs.Select(i => i.Item.Name));
				jData.includedItems.UnionWith(node.Outputs.Select(i => i.Item.Name));
			}
			jData.includedRecipeShorts = includedRecipes.Select(recipe => new RecipeShort(recipe)).ToList();
			jData.includedRecipeShorts.AddRange(includedMissingRecipes.Select(recipe => new RecipeShort(recipe))); //add the missing after the regular, since when we compare saves to preset we will only check 1st recipe of its name (the non-missing kind then)
			jData.includedPlantShorts = includedPlantProcesses.Select(pprocess => new PlantShort(pprocess)).ToList();
			jData.includedPlantShorts.AddRange(includedMissingPlantProcesses.Select(pprocess => new PlantShort(pprocess))); //add the missing after the regular, since when we compare saves to preset we will only check 1st recipe of its name (the non-missing kind then)
		}

		[OnSerialized]
		internal void OnSerialized(StreamingContext ctx) {
			jData = new([], []);
		}

		[JsonProperty]
		public static int Version => Properties.Settings.Default.ForemanVersion;
		[JsonProperty]
		public static string Object => "ProductionGraph";
		[JsonProperty]
		public string DefaultQuality => DefaultAssemblerQuality?.Name ?? "";
		[JsonProperty]
		public HashSet<string> IncludedItems => jData.includedItems;
		[JsonProperty]
		public List<RecipeShort> IncludedRecipes => jData.includedRecipeShorts;
		[JsonProperty]
		public List<PlantShort> IncludedPlantProcesses => jData.includedPlantShorts;
		[JsonProperty]
		public HashSet<string> IncludedAssemblers => jData.includedAssemblers;
		[JsonProperty]
		public HashSet<string> IncludedModules => jData.includedModules;
		[JsonProperty]
		public HashSet<string> IncludedBeacons => jData.includedBeacons;
		[JsonProperty]
		public HashSet<KeyValuePair<string,int>> IncludedQualities => jData.includedQualities;
		[JsonProperty(nameof(Nodes))]
		public HashSet<BaseNode> JsonNodes => jData.includedNodes;
		[JsonProperty(nameof(NodeLinks))]
		public HashSet<NodeLink> JsonNodeLinks => jData.includedLinks;

		public NewNodeCollection InsertNodesFromJson(DCache cache, JObject json, bool loadSolverValues) //cache is necessary since we will possibly be adding to mssing items/recipes
		{
            if (json[nameof(Version)]?.Value<int>() != Properties.Settings.Default.ForemanVersion || json[nameof(Object)]?.Value<string>() != Object) // Object == ProductionGraph
			{
				json = VersionUpdater.UpdateGraph(json, cache);
				if (json == null) //update failed
					return new NewNodeCollection();
			}

			NewNodeCollection newNodeCollection = new();
			Dictionary<int, ReadOnlyBaseNode> oldNodeIndices = []; //the links between the node index (as imported) and the newly created node (which will now have a different index). Used to link up nodes

			try
			{
				//check compliance on all items, assemblers, modules, beacons, and recipes (data-cache will take care of it) - this means add in any missing objects and handle multi-name recipes (there can be multiple versions of a missing recipe, each with identical names)
				cache.ProcessImportedItemsSet(json[nameof(IncludedItems)]?.Select(t => t.Value<string>()).OfType<string>() ?? []);
				var qualityLinks = cache.ProcessImportedQualitiesSet(json[nameof(IncludedQualities)]?.Select(j => new KeyValuePair<string, int>(j["Key"]?.Value<string>() ?? throw new InvalidOperationException("Key is null"), j["Value"]?.Value<int>() ?? throw new InvalidOperationException("Value is null"))) ?? []);
				cache.ProcessImportedAssemblersSet(json[nameof(IncludedAssemblers)]?.Select(t => t.Value<string>()).OfType<string>() ?? []);
				cache.ProcessImportedModulesSet(json[nameof(IncludedModules)]?.Select(t => t.Value<string>()).OfType<string>() ?? []);
				cache.ProcessImportedBeaconsSet(json[nameof(IncludedBeacons)]?.Select(t => t.Value<string>()).OfType<string>() ?? []);
				Dictionary<long, IRecipe> recipeLinks = cache.ProcessImportedRecipesSet(RecipeShort.GetSetFromJson(json[nameof(IncludedRecipes)] ?? new JObject()));
				Dictionary<long, IPlantProcess> plantProcessLinks = cache.ProcessImportedPlantProcessesSet(PlantShort.GetSetFromJson(json[nameof(IncludedPlantProcesses)] ?? new JObject()));

				if (loadSolverValues)
				{
					EnableExtraProductivityForNonMiners = json[nameof(EnableExtraProductivityForNonMiners)]?.Value<bool>() ?? false;
					DefaultNodeDirection = json[nameof(DefaultNodeDirection)]?.ToObject<NodeDirection>() ?? NodeDirection.Up;
					PullOutputNodes = json["Solver_PullOutputNodes"]?.Value<bool>() ?? false;
					PullOutputNodesPower = json["Solver_PullOutputNodesPower"]?.Value<double>() ?? 1.0;
					LowPriorityPower = json["Solver_LowPriorityPower"]?.Value<double>() ?? 2.0;
					MaxQualitySteps = json[nameof(MaxQualitySteps)]?.Value<uint>() ?? 5;
					DefaultAssemblerQuality = qualityLinks[json[nameof(DefaultQuality)]?.Value<string>() ?? "normal"];
				}

				//add in all the graph nodes
				foreach (JToken nodeJToken in json[nameof(Nodes)]?.ToList() ?? [])
				{
					BaseNode? newNode = null;
					string[] locationString = (nodeJToken["Location"]?.Value<string>() ?? "").Split(',');
					Point location = new(int.Parse(locationString[0]), int.Parse(locationString[1]));
					string itemName; //just an early define
					IQuality quality; //early define

					switch (nodeJToken["NodeType"]?.ToObject<NodeType>())
					{
						case NodeType.Consumer:
							itemName = nodeJToken["Item"]?.Value<string>() ?? throw new InvalidOperationException("Item is invalid");
							quality = qualityLinks[nodeJToken["BaseQuality"]?.Value<string>() ?? "normal"];
							if (cache.Items.ContainsKey(itemName))
								newNode = roToNode[CreateConsumerNode(new ItemQualityPair(cache.Items[itemName], quality), location)];
							else
								newNode = roToNode[CreateConsumerNode(new ItemQualityPair(cache.MissingItems[itemName], quality), location)];
							newNodeCollection.NewNodes.Add(newNode.ReadOnlyNode);
							break;
						case NodeType.Supplier:
							itemName = nodeJToken["Item"]?.Value<string>() ?? throw new InvalidOperationException("Item is invalid");
							quality = qualityLinks[nodeJToken["BaseQuality"]?.Value<string>() ?? "normal"];
							if (cache.Items.ContainsKey(itemName))
								newNode = roToNode[CreateSupplierNode(new ItemQualityPair(cache.Items[itemName], quality), location)];
							else
								newNode = roToNode[CreateSupplierNode(new ItemQualityPair(cache.MissingItems[itemName], quality), location)];
							newNodeCollection.NewNodes.Add(newNode.ReadOnlyNode);
							break;
						case NodeType.Passthrough:
							itemName = nodeJToken["Item"]?.Value<string>() ?? throw new InvalidOperationException("Item is invalid");
							quality = qualityLinks[nodeJToken["BaseQuality"]?.Value<string>() ?? "normal"];
							if (cache.Items.ContainsKey(itemName))
								newNode = roToNode[CreatePassthroughNode(new ItemQualityPair(cache.Items[itemName], quality), location)];
							else
								newNode = roToNode[CreatePassthroughNode(new ItemQualityPair(cache.MissingItems[itemName], quality), location)];
							((PassthroughNode)newNode).SimpleDraw = nodeJToken["SDraw"]?.Value<bool>() ?? false;
							newNodeCollection.NewNodes.Add(newNode.ReadOnlyNode);
							break;
						case NodeType.Spoil:
							itemName = nodeJToken["InputItem"]?.Value<string>() ?? throw new InvalidOperationException("InputItem is invalid");
                            string outputItemName = nodeJToken["OutputItem"]?.Value<string>() ?? throw new InvalidOperationException("OutputItem is invalid");
                            quality = qualityLinks[nodeJToken["BaseQuality"]?.Value<string>() ?? "normal"];
							IItem inputItem = cache.Items.ContainsKey(itemName) ? cache.Items[itemName] : cache.MissingItems[itemName];
							IItem outputItem = cache.Items.ContainsKey(outputItemName) ? cache.Items[outputItemName] : cache.MissingItems[outputItemName];
							newNode = roToNode[CreateSpoilNode(new ItemQualityPair(inputItem, quality), outputItem, location)];
							newNodeCollection.NewNodes.Add(newNode.ReadOnlyNode);
							break;
						case NodeType.Plant:
                            long pprocessID = nodeJToken["PlantProcessID"]?.Value<long>() ?? throw new InvalidOperationException("PlantProcessID is invalid");
                            quality = qualityLinks[nodeJToken["BaseQuality"]?.Value<string>() ?? "normal"];
                            newNode = roToNode[CreatePlantNode(plantProcessLinks[pprocessID], quality, location)];
                            newNodeCollection.NewNodes.Add(newNode.ReadOnlyNode);
                            break;
						case NodeType.Recipe:
							long recipeID = nodeJToken["RecipeID"]?.Value<long>() ?? throw new InvalidOperationException("RecipeID is invalid"); ;
							IQuality recipeQuality = qualityLinks[nodeJToken["RecipeQuality"]?.Value<string>() ?? "normal"];
							newNode = roToNode[CreateRecipeNode(new RecipeQualityPair(recipeLinks[recipeID], recipeQuality) , location, (rNode) =>
							{
								RecipeNodeController rNodeController = (RecipeNodeController)rNode.Controller;

								rNode.LowPriority = (nodeJToken["LowPriority"] != null);

								rNode.NeighbourCount = nodeJToken["Neighbours"]?.Value<double>() ?? 0;
								rNode.ExtraProductivityBonus = nodeJToken["ExtraProductivity"]?.Value<double>() ?? 0;

								string assemblerName = nodeJToken["Assembler"]?.Value<string>() ?? throw new InvalidOperationException("Assembler is invalid");
								IQuality assemblerQuality = qualityLinks[nodeJToken["AssemblerQuality"]?.Value<string>() ?? "normal"];
								if (cache.Assemblers.ContainsKey(assemblerName))
									rNodeController.SetAssembler(new AssemblerQualityPair(cache.Assemblers[assemblerName], assemblerQuality));
								else
									rNodeController.SetAssembler(new AssemblerQualityPair(cache.MissingAssemblers[assemblerName], assemblerQuality));

								foreach (JToken module in nodeJToken["AssemblerModules"]?.ToList() ?? [])
								{
									string moduleName = module["Name"]?.Value<string>() ?? throw new InvalidOperationException("Name is invalid");
									IQuality moduleQuality = qualityLinks[module["Quality"]?.Value<string>() ?? "normal"];
									if (cache.Modules.ContainsKey(moduleName))
										rNodeController.AddAssemblerModule(new ModuleQualityPair(cache.Modules[moduleName], moduleQuality));
									else
										rNodeController.AddAssemblerModule(new ModuleQualityPair(cache.MissingModules[moduleName], moduleQuality));
								}

								if (nodeJToken["Fuel"]?.Value<string>() is string fuel)
								{
									if (cache.Items.ContainsKey(fuel))
										rNodeController.SetFuel(cache.Items[fuel]);
									else
										rNodeController.SetFuel(cache.MissingItems[fuel]);
								}
								else if (rNode.SelectedAssembler.Assembler.IsBurner) //and fuel is null... well - its the import. set it as null (and consider it an error)
									rNodeController.SetFuel(null);

								if (nodeJToken["Burnt"]?.Value<string>() is string burnt)
								{
									IItem burntItem;
									if (cache.Items.ContainsKey(burnt))
										burntItem = cache.Items[burnt];
									else
										burntItem = cache.MissingItems[burnt];
									if (rNode.FuelRemains != burntItem)
										rNode.SetBurntOverride(burntItem);
								}
								else if (rNode.Fuel != null && rNode.Fuel.BurnResult != null) //same as above - there should be a burn result, but there isnt...
									rNode.SetBurntOverride(null);

								if (nodeJToken["Beacon"]?.Value<string>() is string beacon)
								{
									string beaconName = beacon;
									IQuality beaconQuality = qualityLinks[nodeJToken["BeaconQuality"]?.Value<string>() ?? "normal"];
									if (cache.Beacons.ContainsKey(beaconName))
										rNodeController.SetBeacon(new BeaconQualityPair(cache.Beacons[beaconName], beaconQuality));
									else
										rNodeController.SetBeacon(new BeaconQualityPair(cache.MissingBeacons[beaconName], beaconQuality));

									foreach (JToken module in nodeJToken["BeaconModules"]?.ToList() ?? [])
									{
                                        string moduleName = module["Name"]?.Value<string>() ?? throw new InvalidOperationException("Name is invalid");
                                        IQuality moduleQuality = qualityLinks[module["Quality"]?.Value<string>() ?? "normal"];
                                        if (cache.Modules.ContainsKey(moduleName))
											rNodeController.AddBeaconModule(new ModuleQualityPair(cache.Modules[moduleName], moduleQuality));
										else
											rNodeController.AddBeaconModule( new ModuleQualityPair(cache.MissingModules[moduleName], moduleQuality));
									}

									rNode.BeaconCount = nodeJToken["BeaconCount"]?.Value<double>() ?? 0;
									rNode.BeaconsPerAssembler = nodeJToken["BeaconsPerAssembler"]?.Value<double>() ?? 0;
									rNode.BeaconsConst = nodeJToken["BeaconsConst"]?.Value<double>() ?? 0;
								}

								newNodeCollection.NewNodes.Add(rNode.ReadOnlyNode); //done last, so as to catch any errors above first.
							})];
							break;
						default:
							throw new Exception(); //we will catch it right away and delete all nodes added in thus far. Error was most likely in json read, in which case we count it as a corrupt json and not import anything.
					}

					newNode.RateType = nodeJToken["RateType"]?.ToObject<RateType>() ?? RateType.Auto;
					if (newNode.RateType == RateType.Manual)
						newNode.DesiredSetValue = nodeJToken["DesiredSetValue"]?.Value<double>() ?? 1;

					newNode.NodeDirection = nodeJToken["Direction"]?.ToObject<NodeDirection>() ?? NodeDirection.Up;

					if(nodeJToken["KeyNode"]?.Value<string>() is string keyNode)
					{
						newNode.KeyNode = true;
						newNode.KeyNodeTitle = keyNode;
					}

					oldNodeIndices.Add(nodeJToken["NodeID"]?.Value<int>() ?? throw new InvalidOperationException("NodeID is invalid"), newNode.ReadOnlyNode);
				}

				//link the new nodes
				foreach (JToken nodeLinkJToken in json[nameof(NodeLinks)]?.ToList() ?? [])
				{
					ReadOnlyBaseNode supplier = oldNodeIndices[nodeLinkJToken["SupplierID"]?.Value<int>() ?? throw new InvalidOperationException("SupplierID is invalid")];
					ReadOnlyBaseNode consumer = oldNodeIndices[nodeLinkJToken["ConsumerID"]?.Value<int>() ?? throw new InvalidOperationException("ConsumerID is invalid")];
					ItemQualityPair item;
					IQuality quality = qualityLinks[nodeLinkJToken["Quality"]?.Value<string>() ?? "normal"];

					string itemName = nodeLinkJToken["Item"]?.Value<string>() ?? throw new InvalidOperationException("Item is invalid");
					if (cache.Items.ContainsKey(itemName))
						item = new ItemQualityPair(cache.Items[itemName], quality);
					else
						item = new ItemQualityPair(cache.MissingItems[itemName], quality);

					if (LinkChecker.IsPossibleConnection(item, supplier, consumer)) //not necessary to test if connection is valid. It must be valid based on json
						newNodeCollection.NewLinks.Add(CreateLink(supplier, consumer, item));
				}
			}
			catch (Exception e) //there was something wrong with the json (probably someone edited it by hand and it didnt link properly). Delete all added nodes and return empty
			{
				ErrorLogging.LogLine(string.Format("Error loading nodes into producton graph! ERROR: {0}", e));
				Console.WriteLine(e);
				DeleteNodes(newNodeCollection.NewNodes);
				return new NewNodeCollection();
			}
			return newNodeCollection;
		}
	}
}