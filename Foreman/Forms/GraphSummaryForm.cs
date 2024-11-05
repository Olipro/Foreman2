using Foreman.Controls;
using Foreman.DataCache;
using Foreman.DataCache.DataTypes;
using Foreman.Models;
using Foreman.Models.Nodes;

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Foreman {
	public partial class GraphSummaryForm : Form
	{
		protected class ItemCounter(double i, double iu, double o, double ou, double oo, double p, double c) {
			public double Input { get; set; } = i;
			public double InputUnlinked { get; set; } = iu;
			public double Output { get; set; } = o;
			public double OutputUnlinked { get; set; } = ou;
			public double OutputOverflow { get; set; } = oo;
			public double Production { get; set; } = p;
			public double Consumption { get; set; } = c;
		}


		private readonly List<ListViewItem> unfilteredAssemblerList;
		private readonly List<ListViewItem> unfilteredMinerList;
		private readonly List<ListViewItem> unfilteredPowerList;
		private readonly List<ListViewItem> unfilteredBeaconList;

		private readonly List<ListViewItem> unfilteredItemsList;
		private readonly List<ListViewItem> unfilteredFluidsList;

		private readonly List<ListViewItem> unfilteredKeyNodesList;

		private readonly List<ListViewItem> filteredAssemblerList;
		private readonly List<ListViewItem> filteredMinerList;
		private readonly List<ListViewItem> filteredPowerList;
		private readonly List<ListViewItem> filteredBeaconList;

		private readonly List<ListViewItem> filteredItemsList;
		private readonly List<ListViewItem> filteredFluidsList;

		private readonly List<ListViewItem> filteredKeyNodesList;

		private readonly Dictionary<ListView, int> lastSortOrder; //int is +ve if sorted down, -ve if sorted up, |value| is the column # (starts from 1 due to 0 not having a sign) of the sort.

		private readonly string rateString;

		private static readonly Color AvailableObjectColor = Color.White;
		private static readonly Color UnavailableObjectColor = Color.Pink;

		public GraphSummaryForm(IEnumerable<ReadOnlyBaseNode> nodes, string rateString)
		{
			InitializeComponent();
			MainForm.SetDoubleBuffered(AssemblerListView);
			MainForm.SetDoubleBuffered(MinerListView);
			MainForm.SetDoubleBuffered(PowerListView);
			MainForm.SetDoubleBuffered(BeaconListView);
			MainForm.SetDoubleBuffered(ItemsListView);
			MainForm.SetDoubleBuffered(FluidsListView);
			MainForm.SetDoubleBuffered(KeyNodesListView);

			unfilteredAssemblerList = [];
			unfilteredMinerList = [];
			unfilteredPowerList = [];
			unfilteredBeaconList = [];
			unfilteredItemsList = [];
			unfilteredFluidsList = [];
			unfilteredKeyNodesList = [];

			filteredAssemblerList = [];
			filteredMinerList = [];
			filteredPowerList = [];
			filteredBeaconList = [];
			filteredItemsList = [];
			filteredFluidsList = [];
			filteredKeyNodesList = [];

			lastSortOrder = new Dictionary<ListView, int> {
				{ AssemblerListView, 2 },
				{ MinerListView, 2 },
				{ PowerListView, 2 },
				{ BeaconListView, 2 },
				{ ItemsListView, 1 },
				{ FluidsListView, 1 },
				{ KeyNodesListView, 1 }
			};

			IconList.Images.Clear();
			IconList.Images.Add(IconCache.GetUnknownIcon());

			ItemsTabPage.Text += " ( per " + rateString + ")";
			this.rateString = rateString;

			//lists
			LoadUnfilteredSelectedAssemblerList(nodes.Where(n => n is ReadOnlyRecipeNode rNode && rNode.SelectedAssembler.Assembler?.EntityType == EntityType.Assembler).Select(n => (ReadOnlyRecipeNode)n), unfilteredAssemblerList);
			LoadUnfilteredSelectedAssemblerList(nodes.Where(n => n is ReadOnlyRecipeNode rNode && (rNode.SelectedAssembler.Assembler?.EntityType == EntityType.Miner || rNode.SelectedAssembler.Assembler?.EntityType == EntityType.OffshorePump)).Select(n => (ReadOnlyRecipeNode)n), unfilteredMinerList);
			LoadUnfilteredSelectedAssemblerList(nodes.Where(n => n is ReadOnlyRecipeNode rNode && (rNode.SelectedAssembler.Assembler?.EntityType == EntityType.Boiler || rNode.SelectedAssembler.Assembler?.EntityType == EntityType.BurnerGenerator || rNode.SelectedAssembler.Assembler?.EntityType == EntityType.Generator || rNode.SelectedAssembler.Assembler?.EntityType == EntityType.Reactor)).Select(n => (ReadOnlyRecipeNode)n), unfilteredPowerList);

			LoadUnfilteredBeaconList(nodes.Where(n => n is ReadOnlyRecipeNode rNode && rNode.SelectedBeacon is BeaconQualityPair).Select(n => (ReadOnlyRecipeNode)n), unfilteredBeaconList);

			LoadUnfilteredItemLists(nodes, false, unfilteredItemsList);
			LoadUnfilteredItemLists(nodes, true, unfilteredFluidsList);

			LoadUnfilteredKeyNodesList(nodes.Where(n => n.KeyNode), unfilteredKeyNodesList);

			//building totals
			double buildingTotal = nodes.Where(n => n is ReadOnlyRecipeNode).Sum(n => Math.Ceiling(((ReadOnlyRecipeNode)n).ActualSetValue));
			double beaconTotal = nodes.Where(n => n is ReadOnlyRecipeNode).Sum(n => ((ReadOnlyRecipeNode)n).GetTotalBeacons());
			BuildingCountLabel.Text += GraphicsStuff.DoubleToString(buildingTotal);
			BeaconCountLabel.Text += GraphicsStuff.DoubleToString(beaconTotal);

			//power totals
			double powerConsumption = nodes.Where(n => n is ReadOnlyRecipeNode).Sum(n => ((ReadOnlyRecipeNode)n).GetTotalAssemblerElectricalConsumption() + ((ReadOnlyRecipeNode)n).GetTotalBeaconElectricalConsumption());
			double powerProduction = nodes.Where(n => n is ReadOnlyRecipeNode).Sum(n => ((ReadOnlyRecipeNode)n).GetTotalGeneratorElectricalProduction());
			PowerConsumptionLabel.Text += GraphicsStuff.DoubleToEnergy(powerConsumption, "W");
			PowerProductionLabel.Text += GraphicsStuff.DoubleToEnergy(powerProduction, "W");

			//update filtered
			UpdateFilteredBuildingLists();
			UpdateFilteredItemsLists();
			UpdateFilteredKeyNodesList();
		}

		//-------------------------------------------------------------------------------------------------------Initial list initialization

		private void LoadUnfilteredSelectedAssemblerList(IEnumerable<ReadOnlyRecipeNode> origin, List<ListViewItem> lviList)
		{
			Dictionary<AssemblerQualityPair, int> buildingCounters = [];
			Dictionary<AssemblerQualityPair, Tuple<double, double>> buildingElectricalPower = []; //power for buildings, power for beacons)

			foreach(ReadOnlyRecipeNode rnode in origin)
			{
				if (buildingCounters.TryAdd(rnode.SelectedAssembler, 0))
				{
					buildingElectricalPower.Add(rnode.SelectedAssembler, new Tuple<double, double>(0,0));
				}
				buildingCounters[rnode.SelectedAssembler] += (int)Math.Ceiling(rnode.ActualSetValue); //should probably check the validity of ceiling in case of near correct (ex: 1.0001 assemblers should really be counted as 1 instead of 2)
				Tuple<double, double> oldValues = buildingElectricalPower[rnode.SelectedAssembler];
				buildingElectricalPower[rnode.SelectedAssembler] = new Tuple<double,double>(oldValues.Item1 + rnode.GetTotalGeneratorElectricalProduction() + rnode.GetTotalAssemblerElectricalConsumption(), oldValues.Item2 + rnode.GetTotalBeaconElectricalConsumption());
			}

			foreach (AssemblerQualityPair assembler in buildingCounters.Keys.OrderByDescending(a => a.Assembler?.Available).ThenBy(a => a.Assembler?.FriendlyName).ThenBy(a => a.Quality?.Level).ThenBy(a => a.Quality?.FriendlyName))
			{
				ListViewItem lvItem = new();
				if (assembler.Assembler?.Icon != null)
				{
					IconList.Images.Add(assembler.Assembler.Icon);
					lvItem.ImageIndex = IconList.Images.Count - 1;
				}
				else
				{
					lvItem.ImageIndex = 0;
				}

				lvItem.Text = buildingCounters[assembler] >= 10000000? buildingCounters[assembler].ToString("0.##e0") : buildingCounters[assembler].ToString("N0");
				lvItem.Tag = assembler;
				lvItem.Name = assembler.Assembler?.Name + ":" + assembler.Quality?.Name; //key
				lvItem.BackColor = assembler.Assembler?.Available ?? false ? AvailableObjectColor : UnavailableObjectColor;
				lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = assembler.FriendlyName });
				lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = buildingElectricalPower[assembler].Item1 == 0 ? "-" : GraphicsStuff.DoubleToEnergy(buildingElectricalPower[assembler].Item1, "W"), Tag = buildingElectricalPower[assembler].Item1 });
				lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = buildingElectricalPower[assembler].Item2 == 0 ? "-" : GraphicsStuff.DoubleToEnergy(buildingElectricalPower[assembler].Item2, "W"), Tag = buildingElectricalPower[assembler].Item2 });
				lviList.Add(lvItem);
			}
		}

		private void LoadUnfilteredBeaconList(IEnumerable<ReadOnlyRecipeNode> origin, List<ListViewItem> lviList)
		{
			Dictionary<BeaconQualityPair, int> beaconCounters = [];

			foreach (ReadOnlyRecipeNode rnode in origin)
			{
				if (rnode.SelectedBeacon is not BeaconQualityPair bqp)
					continue;

				beaconCounters.TryAdd(bqp, 0);
				beaconCounters[bqp] += rnode.GetTotalBeacons();
			}

			foreach (BeaconQualityPair beacon in beaconCounters.Keys.OrderByDescending(b => b.Beacon?.Available).ThenBy(b => b.Beacon?.FriendlyName).ThenBy(b => b.Quality?.Level).ThenBy(b => b.Quality?.FriendlyName))
			{
				ListViewItem lvItem = new();
				if (beacon.Icon != null)
				{
					IconList.Images.Add(beacon.Icon);
					lvItem.ImageIndex = IconList.Images.Count - 1;
				}
				else
				{
					lvItem.ImageIndex = 0;
				}

				lvItem.Text = beaconCounters[beacon].ToString();
				lvItem.Tag = beacon;
				lvItem.Name = beacon.Beacon.Name + ":" + beacon.Quality.Name; //key
				lvItem.BackColor = beacon.Beacon.Available ? AvailableObjectColor : UnavailableObjectColor;
				lvItem.SubItems.Add(beacon.FriendlyName);
				double beaconPowerConsumption = beaconCounters[beacon] * (beacon.Beacon.GetEnergyConsumption(beacon.Quality) + beacon.Beacon.GetEnergyDrain());  //QUALITY UPDATE REQUIRED
				lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = beaconCounters[beacon] == 0 ? "-" : GraphicsStuff.DoubleToEnergy(beaconPowerConsumption, "W"), Tag = beaconPowerConsumption });
				lviList.Add(lvItem);
			}
		}

		private void LoadUnfilteredItemLists(IEnumerable<ReadOnlyBaseNode> nodes, bool fluids, List<ListViewItem> lviList)
		{
			//NOTE: throughput is initially calculatated as all non-overflow linked input & output of each recipe node. At the end we will add
			Dictionary<ItemQualityPair, ItemCounter> itemCounters = [];

			foreach (ReadOnlyBaseNode node in nodes)
			{
				if (node is ReadOnlyRecipeNode)
				{
					foreach (ItemQualityPair input in node.Inputs.Where(i => fluids.Equals(i.Item is IFluid)))
					{
						if (!itemCounters.ContainsKey(input))
							itemCounters.Add(input, new ItemCounter(0, 0, 0, 0, 0, 0, 0));

						double consumeRate = node.GetConsumeRate(input);
						if (consumeRate > 0)
						{
							if (!node.InputLinks.Any(l => l.Item == input))
								itemCounters[input].InputUnlinked += consumeRate;
							else
								itemCounters[input].Consumption += consumeRate;
						}
					}

					foreach (ItemQualityPair output in node.Outputs.Where(i => fluids.Equals(i.Item is IFluid)))
					{
						if (!itemCounters.ContainsKey(output))
							itemCounters.Add(output, new ItemCounter(0, 0, 0, 0, 0, 0, 0));

						double supplyRate = node.GetSupplyRate(output);
						bool isOverProduced = node.IsOverproducing(output);
						double supplyUsedRate = isOverProduced ? node.GetSupplyUsedRate(output) : supplyRate;

						if (supplyRate > 0)
						{
							if (!node.OutputLinks.Any(l => l.Item == output))
								itemCounters[output].OutputUnlinked += supplyRate;

							itemCounters[output].Production += supplyRate;
							if (isOverProduced)
								itemCounters[output].OutputOverflow += supplyRate - supplyUsedRate;
						}
					}
				}

				else if(node is ReadOnlySupplierNode sNode && fluids.Equals(sNode.SuppliedItem.Item is IFluid))
				{
					if (!itemCounters.ContainsKey(sNode.SuppliedItem))
						itemCounters.Add(sNode.SuppliedItem, new ItemCounter(0, 0, 0, 0, 0, 0, 0));
					itemCounters[sNode.SuppliedItem].Input += sNode.ActualRate;
				}

				else if(node is ReadOnlyConsumerNode cNode && fluids.Equals(cNode.ConsumedItem.Item is IFluid))
				{
					if (!itemCounters.ContainsKey(cNode.ConsumedItem))
						itemCounters.Add(cNode.ConsumedItem, new ItemCounter(0, 0, 0, 0, 0, 0, 0));
					itemCounters[cNode.ConsumedItem].Output += cNode.ActualRate;
				}
			}

			foreach (ItemQualityPair item in itemCounters.Keys.OrderBy(a => a.Item?.FriendlyName).ThenBy(a => a.Quality?.Level).ThenBy(a => a.Quality?.FriendlyName))
			{
				ListViewItem lvItem = new();
				if (item.Icon != null)
				{
					IconList.Images.Add(item.Icon);
					lvItem.ImageIndex = IconList.Images.Count - 1;
				}
				else
				{
					lvItem.ImageIndex = 0;
				}

				lvItem.Text = item.FriendlyName;
				lvItem.Tag = item;
				lvItem.Name = item.Item?.Name + ":" + item.Quality?.Name; //key
				lvItem.BackColor = item.Item?.Available ?? false ? AvailableObjectColor : UnavailableObjectColor;
				lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = itemCounters[item].Input == 0 ? "-" : GraphicsStuff.DoubleToString(itemCounters[item].Input), Tag = itemCounters[item].Input });
				lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = itemCounters[item].InputUnlinked == 0 ? "-" : GraphicsStuff.DoubleToString(itemCounters[item].InputUnlinked), Tag = itemCounters[item].InputUnlinked});
				lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = itemCounters[item].Output == 0 ? "-" : GraphicsStuff.DoubleToString(itemCounters[item].Output), Tag = itemCounters[item].Output });
				lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = itemCounters[item].OutputUnlinked == 0 ? "-" : GraphicsStuff.DoubleToString(itemCounters[item].OutputUnlinked), Tag = itemCounters[item].OutputUnlinked });
				lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = itemCounters[item].OutputOverflow == 0 ? "-" : GraphicsStuff.DoubleToString(itemCounters[item].OutputOverflow), Tag = itemCounters[item].OutputOverflow });
				lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = itemCounters[item].Production == 0 ? "-" : GraphicsStuff.DoubleToString(itemCounters[item].Production), Tag = itemCounters[item].Production });
				lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = itemCounters[item].Consumption == 0 ? "-" : GraphicsStuff.DoubleToString(itemCounters[item].Consumption), Tag = itemCounters[item].Consumption });
				lviList.Add(lvItem);
			}
		}

		private void LoadUnfilteredKeyNodesList(IEnumerable<ReadOnlyBaseNode> origin, List<ListViewItem> lviList)
		{
			foreach (ReadOnlyBaseNode node in origin)
			{
				ListViewItem lvItem = new();

				Bitmap icon;
				string nodeText;
				string nodeType;
				if (node is ReadOnlyConsumerNode cNode)
				{
					icon = cNode.ConsumedItem.Icon ?? IconCache.GetUnknownIcon();
					nodeText = cNode.ConsumedItem.FriendlyName;
					nodeType = "Consumer";
				}
				else if (node is ReadOnlySupplierNode sNode)
				{
					icon = sNode.SuppliedItem.Icon ?? IconCache.GetUnknownIcon();
					nodeText = sNode.SuppliedItem.FriendlyName;
					nodeType = "Supplier";
				}
				else if (node is ReadOnlyPassthroughNode pNode)
				{
					icon = pNode.PassthroughItem.Icon ?? IconCache.GetUnknownIcon();
					nodeText = pNode.PassthroughItem.FriendlyName;
					nodeType = "Passthrough";
				}
				else if (node is ReadOnlyRecipeNode rNode)
				{
					icon = rNode.BaseRecipe.Icon ?? IconCache.GetUnknownIcon();
					nodeText = rNode.BaseRecipe.FriendlyName;
					nodeType = "Recipe";
				}
				else if (node is ReadOnlySpoilNode spNode)
                {
                    icon = spNode.InputItem.Icon ?? IconCache.GetUnknownIcon();
                    nodeText = spNode.InputItem.FriendlyName + " spoiling";
                    nodeType = "Spoil";
                }
				else if (node is ReadOnlyPlantNode plNode)
                {
                    icon = plNode.Seed.Icon ?? IconCache.GetUnknownIcon();
                    nodeText = plNode.Seed.FriendlyName + " planting";
                    nodeType = "Plant";
                }
				else
					continue;

				if (icon != null)
				{
					IconList.Images.Add(icon);
					lvItem.ImageIndex = IconList.Images.Count - 1;
				}
				else
				{
					lvItem.ImageIndex = 0;
				}

				lvItem.Text = nodeType;
				lvItem.Tag = node;
				lvItem.Name = nodeText; //key
				lvItem.BackColor = AvailableObjectColor;
				lvItem.SubItems.Add(nodeText);
				lvItem.SubItems.Add(node.KeyNodeTitle);

				if(node is ReadOnlyRecipeNode rrNode)
				{
					lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = "-", Tag = (double)0 });
					lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = GraphicsStuff.DoubleToString(rrNode.ActualSetValue), Tag = rrNode.ActualSetValue });
				}
				else
				{
					lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = GraphicsStuff.DoubleToString(node.ActualRate), Tag = node.ActualRate });
					lvItem.SubItems.Add(new ListViewItem.ListViewSubItem() { Text = "-", Tag = (double)0 });
				}
				lviList.Add(lvItem);
			}
		}

		//-------------------------------------------------------------------------------------------------------Filter functions

		private void UpdateFilteredBuildingLists()
		{
			UpdateFilteredBuildingList(unfilteredAssemblerList, filteredAssemblerList, AssemblerListView);
			UpdateFilteredBuildingList(unfilteredMinerList, filteredMinerList, MinerListView);
			UpdateFilteredBuildingList(unfilteredPowerList, filteredPowerList, PowerListView);
			UpdateFilteredBuildingList(unfilteredBeaconList, filteredBeaconList, BeaconListView);
		}

		private void UpdateFilteredBuildingList(List<ListViewItem> unfilteredList, List<ListViewItem> filteredList, ListView owner)
		{
			string filterString = BuildingsFilterTextBox.Text.ToLower();

			filteredList.Clear();

			foreach (ListViewItem lvItem in unfilteredList)
				if (string.IsNullOrEmpty(filterString) || (lvItem.Tag is IDataObjectBase idob && idob.LFriendlyName.Contains(filterString)))
					filteredList.Add(lvItem);

			owner.VirtualListSize = filteredList.Count;
			owner.Invalidate();
		}

		private void UpdateFilteredItemsLists()
		{
			UpdateFilteredItemsList(unfilteredItemsList, filteredItemsList, ItemsListView);
			UpdateFilteredItemsList(unfilteredFluidsList, filteredFluidsList, FluidsListView);
		}

		private void UpdateFilteredItemsList(List<ListViewItem> unfilteredList, List<ListViewItem> filteredList, ListView owner)
		{
			string filterString = ItemsFilterTextBox.Text.ToLower();
			bool includeInputs = ItemFilterInputCheckBox.Checked;
			bool includeInputUnlinked = ItemFilterInputUnlinkedCheckBox.Checked;
			bool includeOutputs = ItemFilterOutputCheckBox.Checked;
			bool includeOutputsUnlinked = ItemFilterOutputUnlinkedCheckBox.Checked;
			bool includeOutputsOverflow = ItemFilterOutputOverproducedCheckBox.Checked;
			bool includeProduced = ItemFilterProductionCheckBox.Checked;
			bool includeConsumed = ItemFilterConsumptionCheckBox.Checked;

			filteredList.Clear();

			foreach (ListViewItem lvItem in unfilteredList)
			{
				if (string.IsNullOrEmpty(filterString) || (lvItem.Tag is IItem iitem && iitem.LFriendlyName.Contains(filterString)))
				{
					if ((includeInputs && lvItem.SubItems[1].Text != "-") ||
						(includeInputUnlinked && lvItem.SubItems[2].Text != "-") ||
						(includeOutputs && lvItem.SubItems[3].Text != "-") ||
						(includeOutputsUnlinked && lvItem.SubItems[4].Text != "-") ||
						(includeOutputsOverflow && lvItem.SubItems[5].Text != "-") ||
						(includeProduced && lvItem.SubItems[6].Text != "-") ||
						(includeConsumed && lvItem.SubItems[7].Text != "-"))
					{
						filteredList.Add(lvItem);
					}
				}
			}

			owner.VirtualListSize = filteredList.Count;
			owner.Invalidate();
		}

		private void UpdateFilteredKeyNodesList()
		{
			string filterString = KeyNodesFilterTextBox.Text.ToLower();
			bool includeSuppliers = SupplierNodeFilterCheckBox.Checked;
			bool includeConsumers = ConsumerNodeFilterCheckBox.Checked;
			bool includePassthrough = PassthroughNodeFilterCheckBox.Checked;
			bool includeRecipe = RecipeNodeFilterCheckBox.Checked;

			filteredKeyNodesList.Clear();
			const StringComparison ccIgnore = StringComparison.CurrentCultureIgnoreCase;

			foreach (ListViewItem lvItem in unfilteredKeyNodesList)
			{
				if (string.IsNullOrEmpty(filterString) || lvItem.Text.Contains(filterString, ccIgnore) || lvItem.SubItems[1].Text.Contains(filterString, ccIgnore) || lvItem.SubItems[2].Text.Contains(filterString, ccIgnore))
				{
					if ((includeSuppliers && (lvItem.Tag is ReadOnlySupplierNode)) ||
						(includeConsumers && (lvItem.Tag is ReadOnlyConsumerNode)) ||
						(includePassthrough && (lvItem.Tag is ReadOnlyPassthroughNode)) ||
						(includeRecipe && (lvItem.Tag is ReadOnlyRecipeNode)))
					{
						filteredKeyNodesList.Add(lvItem);
					}
				}
			}

			KeyNodesListView.VirtualListSize = filteredKeyNodesList.Count;
			KeyNodesListView.Invalidate();
		}

		//-------------------------------------------------------------------------------------------------------Virtual item retrieval for all list views

		private void AssemblerListView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e) { e.Item = filteredAssemblerList[e.ItemIndex]; }
		private void MinerListView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e) { e.Item = filteredMinerList[e.ItemIndex]; }
		private void PowerListView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e) { e.Item = filteredPowerList[e.ItemIndex]; }
		private void BeaconListView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e) { e.Item = filteredBeaconList[e.ItemIndex]; }
		private void ItemsListView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e) { e.Item = filteredItemsList[e.ItemIndex]; }
		private void FluidsListView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e) { e.Item = filteredFluidsList[e.ItemIndex]; }
		private void KeyNodesListView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e) { e.Item = filteredKeyNodesList[e.ItemIndex]; }

		//-------------------------------------------------------------------------------------------------------Filter changed events

		private void BuildingsFilterTextBox_TextChanged(object? sender, EventArgs e) { UpdateFilteredBuildingLists(); }

		private void ItemsFilterTextBox_TextChanged(object? sender, EventArgs e) { UpdateFilteredItemsLists(); }
		private void ItemFilterCheckBox_CheckedChanged(object? sender, EventArgs e) { UpdateFilteredItemsLists(); }

		private void KeyNodesFilterTextBox_TextChanged(object? sender, EventArgs e) { UpdateFilteredKeyNodesList(); }
		private void KeyNodesFilterCheckBox_CheckedChanged(object? sender, EventArgs e) { UpdateFilteredKeyNodesList(); }

		//-------------------------------------------------------------------------------------------------------Column clicked events

		private void AssemblerListView_ColumnClick(object? sender, ColumnClickEventArgs e) { BuildingListView_ColumnSort(unfilteredAssemblerList, filteredAssemblerList, AssemblerListView, e.Column); }
		private void MinerListView_ColumnClick(object? sender, ColumnClickEventArgs e) { BuildingListView_ColumnSort(unfilteredMinerList, filteredMinerList, MinerListView, e.Column); }
		private void PowerListView_ColumnClick(object? sender, ColumnClickEventArgs e) { BuildingListView_ColumnSort(unfilteredPowerList, filteredPowerList, PowerListView, e.Column); }
		private void BeaconListView_ColumnClick(object? sender, ColumnClickEventArgs e) { BuildingListView_ColumnSort(unfilteredBeaconList, filteredBeaconList, BeaconListView, e.Column); }

		private void BuildingListView_ColumnSort(List<ListViewItem> unfilteredList, List<ListViewItem> filteredList, ListView owner, int column)
		{
			int reverseSortLamda = (lastSortOrder[owner] == column + 1) ? -1 : 1; //last sort was this very column -> this is now a reverse sort
			lastSortOrder[owner] = reverseSortLamda * (column + 1);

			unfilteredList.Sort((a, b) =>
			{
				int result;
				if (column == 0)
					result = -double.Parse(a.Text).CompareTo(double.Parse(b.Text));
				else if (column == 1)
					result = StringComparer.CurrentCultureIgnoreCase.Compare(a.SubItems[1].Text, b.SubItems[1].Text);
				else
					result = -(a.SubItems[column].Tag as double? ?? 0.0).CompareTo(b.SubItems[column].Tag as double? ?? 0.0);

				if (result == 0)
					result = (a.Tag as IDataObjectBase ?? throw new InvalidOperationException("a.Tag is not IDataObjectBase")).LFriendlyName.CompareTo((b.Tag as IDataObjectBase ?? throw new InvalidOperationException("a.Tag is not IDataObjectBase")).LFriendlyName);
				if (result == 0)
					result = (a.Tag as IDataObjectBase ?? throw new InvalidOperationException("a.Tag is not IDataObjectBase")).Name.CompareTo((b.Tag as IDataObjectBase ?? throw new InvalidOperationException("a.Tag is not IDataObjectBase")).Name);
				return result * reverseSortLamda;

			});

			UpdateFilteredBuildingList(unfilteredList, filteredList, owner);
			owner.Invalidate();
		}

		private void ItemsListView_ColumnClick(object? sender, ColumnClickEventArgs e) { ItemListView_ColumnSort(unfilteredItemsList, filteredItemsList, ItemsListView, e.Column); }
		private void FluidsListView_ColumnClick(object? sender, ColumnClickEventArgs e) { ItemListView_ColumnSort(unfilteredFluidsList, filteredFluidsList, FluidsListView, e.Column); }

		private void ItemListView_ColumnSort(List<ListViewItem> unfilteredList, List<ListViewItem> filteredList, ListView owner, int column)
		{
			int reverseSortLamda = (lastSortOrder[owner] == column + 1) ? -1 : 1; //last sort was this very column -> this is now a reverse sort
			lastSortOrder[owner] = reverseSortLamda * (column + 1);

			unfilteredList.Sort((a, b) =>
			{
				int result;
				if (column == 0)
					result = StringComparer.CurrentCultureIgnoreCase.Compare(a.SubItems[0].Text, b.SubItems[0].Text);
				else
					result = -(a.SubItems[column].Tag as double? ?? 0.0).CompareTo(b.SubItems[column].Tag as double? ?? 0.0);

				if (result == 0 && a.Tag is IDataObjectBase ado && b.Tag is IDataObjectBase bdo) {
					result = ado.LFriendlyName.CompareTo(bdo.LFriendlyName);
					if (result == 0)
						result = ado.Name.CompareTo(bdo.Name);
				}

				if (result == 0 && a.Tag is ItemQualityPair aqp && b.Tag is ItemQualityPair bqp)
					result = aqp.Quality.Level.CompareTo(bqp.Quality.Level);

				return result * reverseSortLamda;
			});

			UpdateFilteredItemsList(unfilteredList, filteredList, owner);
			owner.Invalidate();
		}

		private void KeyNodesListView_ColumnClick(object? sender, ColumnClickEventArgs e)
		{
			const int maxDigits = 20;
			Regex comparerRegex = NumberMatch();
			Dictionary<string, string> stringComparerProcessedStrings = [];
			int NaturalCompareStrings(string a, string b)
			{
				if (!stringComparerProcessedStrings.ContainsKey(a))
					stringComparerProcessedStrings.Add(a, comparerRegex.Replace(a.ToLower(), matcha => matcha.Value.PadLeft(maxDigits, '0')));
				if (!stringComparerProcessedStrings.ContainsKey(b))
					stringComparerProcessedStrings.Add(b, comparerRegex.Replace(b.ToLower(), matcha => matcha.Value.PadLeft(maxDigits, '0')));

				return stringComparerProcessedStrings[a].CompareTo(stringComparerProcessedStrings[b]);
			}

			int reverseSortLamda = (lastSortOrder[KeyNodesListView] == e.Column + 1) ? -1 : 1; //last sort was this very column -> this is now a reverse sort
			lastSortOrder[KeyNodesListView] = reverseSortLamda * (e.Column + 1);

			unfilteredKeyNodesList.Sort((a, b) =>
			{
				int result;
				if (e.Column == 2)
					result = NaturalCompareStrings(a.SubItems[2].Text, b.SubItems[2].Text);
				else if(e.Column < 3)
					result = StringComparer.CurrentCultureIgnoreCase.Compare(a.SubItems[e.Column].Text, b.SubItems[e.Column].Text);
				else
					result =  -(a.SubItems[e.Column].Tag as double? ?? 0.0).CompareTo(b.SubItems[e.Column].Tag as double? ?? 0.0);

				if(result == 0 && e.Column != 2)
					result = NaturalCompareStrings(a.SubItems[2].Text, b.SubItems[2].Text);
				if(result == 0 && e.Column != 0)
					result = StringComparer.CurrentCultureIgnoreCase.Compare(a.SubItems[0].Text, b.SubItems[0].Text);
				if (result == 0 && e.Column != 1)
					result = StringComparer.CurrentCultureIgnoreCase.Compare(a.SubItems[1].Text, b.SubItems[1].Text);
				if (result == 0)
					result = (a.Tag as ReadOnlyBaseNode ?? throw new InvalidOperationException("a.Tag is not ReadOnlyBaseNode")).NodeID.CompareTo((b.Tag as ReadOnlyBaseNode ?? throw new InvalidOperationException("b.Tag is not ReadOnlyBaseNode")).NodeID);
				return result * reverseSortLamda;
			});

			UpdateFilteredKeyNodesList();
			KeyNodesListView.Invalidate();
		}

		//-------------------------------------------------------------------------------------------------------Export CSV functions

		private void BuildingsExportButton_Click(object? sender, EventArgs e)
		{
			ExportCSV(
				[filteredAssemblerList, filteredMinerList, filteredPowerList, filteredBeaconList],
				[ 
					["#", "Assembler", "Electrical power consumed by assemblers (in W)", "Electrical power consumed by beacons (in W)"], 
					["#", "Miner", "Electrical power consumed by assemblers (in W)", "Electrical power consumed by beacons (in W)"], 
					["#", "Power Building", "Electrical power generated (in W)", "Electrical power consumed (in W)"], 
					["#", "Beacon", "Electrical power consumed by beacons (in W)"]
				]);
		}

		private void ItemsExportButton_Click(object? sender, EventArgs e)
		{
			ExportCSV(
				[filteredItemsList, filteredFluidsList],
				[
					["Item", "Input (per "+rateString+")", "Input through un-linked recipe ingredients (per "+rateString+")", "Output (per " + rateString + ")", "Output through un-linked recipe products (per " + rateString + ")", "Output through overproduction (per " + rateString + ")", "Produced by recipe nodes (per " + rateString + ")", "Consumed by recipe nodes (per " + rateString + ")"],
					["Fluid", "Input (per "+rateString+")", "Input through un-linked recipe ingredients (per "+rateString+")", "Output (per " + rateString + ")", "Output through un-linked recipe products (per " + rateString + ")", "Output through overproduction (per " + rateString + ")", "Produced by recipe nodes (per " + rateString + ")", "Consumed by recipe nodes (per " + rateString + ")"]
				]);
		}

		private void KeyNodesExportButton_Click(object? sender, EventArgs e)
		{
			ExportCSV(
				[filteredKeyNodesList],
				[
					["Node Type", "Node Details (item / recipe name)", "Node Title", "Throughput (for non-recipe nodes) (per " + rateString + ")", "Building Count (for recipe nodes)"]
				]);
		}

		private static void ExportCSV(List<ListViewItem>[] inputList, string[][] columnNames)
		{
			using SaveFileDialog dialog = new();
			dialog.AddExtension = true;
			dialog.Filter = "CSV (*.csv)|*.csv";
			dialog.InitialDirectory = Path.Combine(Application.StartupPath, "Exported CSVs");
			if (!Directory.Exists(dialog.InitialDirectory))
				Directory.CreateDirectory(dialog.InitialDirectory);
			dialog.FileName = "foreman data.csv";
			dialog.ValidateNames = true;
			dialog.OverwritePrompt = true;
			var result = dialog.ShowDialog();

			if (result == DialogResult.OK) {
				List<string[]> csvLines = [];

				for (int i = 0; i < inputList.Length; i++) {
					csvLines.Add(columnNames[i]);
					foreach (ListViewItem lvi in inputList[i]) {
						string[] cLine = new string[columnNames[i].Length];
						for (int j = 0; j < cLine.Length; j++)
							cLine[j] = (lvi.SubItems[j].Tag?.ToString() ?? lvi.SubItems[j].Text).Replace(",", "").Replace("\n", "; ").Replace("\t", "");
						csvLines.Add(cLine);
					}
					csvLines.Add([""]);
				}
				if (csvLines.Count > 0)
					csvLines.RemoveAt(csvLines.Count - 1);

				//export to csv.
				StringBuilder csvBuilder = new();
				csvLines.ForEach(line => { csvBuilder.AppendLine(string.Join(",", line)); });
				File.WriteAllText(dialog.FileName, csvBuilder.ToString());
			}
		}

		[GeneratedRegex(@"\d+", RegexOptions.Compiled)]
		private static partial Regex NumberMatch();
	}
}
