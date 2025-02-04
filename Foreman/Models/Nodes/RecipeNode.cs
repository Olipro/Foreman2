﻿using Foreman.DataCache;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Newtonsoft.Json;
using Foreman.DataCache.DataTypes;

namespace Foreman.Models.Nodes {
	[JsonObject(MemberSerialization.OptIn)]
	public class RecipeNode : BaseNode {
		public enum Errors {
			Clean = 0b_0000_0000_0000,
			RecipeIsMissing = 0b_0000_0000_0001,
			AssemblerIsMissing = 0b_0000_0000_0010,
			BurnerNoFuelSet = 0b_0000_0000_0100,
			FuelIsMissing = 0b_0000_0000_1000,
			InvalidFuel = 0b_0000_0001_0000,
			InvalidFuelRemains = 0b_0000_0010_0000,
			AModuleIsMissing = 0b_0000_0100_0000,
			AModuleLimitExceeded = 0b_0000_1000_0000,
			BeaconIsMissing = 0b_0001_0000_0000,
			BModuleIsMissing = 0b_0010_0000_0000,
			BModuleLimitExceeded = 0b_0100_0000_0000,

			RQualityIsMissing = 0b_1000_0000_0000,
			AQualityIsMissing = 0b_0001_0000_0000_0000,
			BQualityIsMissing = 0b_0010_0000_0000_0000,
			AModuleQualityIsMissing = 0b_0100_0000_0000_0000,
			BModuleQualityIsMissing = 0b_1000_0000_0000_0000,

			InvalidLinks = 0b_1000_0000_000_0000_0000
		}
		public enum Warnings {
			Clean = 0b_0000_0000_0000_0000,
			RecipeIsDisabled = 0b_0000_0000_0000_0001,
			RecipeIsUnavailable = 0b_0000_0000_0000_0010,
			AssemblerIsDisabled = 0b_0000_0000_0000_0100,
			AssemblerIsUnavailable = 0b_0000_0000_0000_1000,
			NoAvailableAssemblers = 0b_0000_0000_0001_0000,
			FuelIsUnavailable = 0b_0000_0000_0010_0000,
			FuelIsUncraftable = 0b_0000_0000_0100_0000,
			NoAvailableFuels = 0b_0000_0000_1000_0000,
			AModuleIsDisabled = 0b_0000_0001_0000_0000,
			AModuleIsUnavailable = 0b_0000_0010_0000_0000,
			BeaconIsDisabled = 0b_0000_0100_0000_0000,
			BeaconIsUnavailable = 0b_0000_1000_0000_0000,
			BModuleIsDisabled = 0b_0001_0000_0000_0000,
			BModuleIsUnavailable = 0b_0010_0000_0000_0000,

			AssemblerQualityIsDisabled = 0b_1000_0000_0000_0000,
			BeaconQualityIsDisabled = 0b_0001_0000_0000_0000_0000,
			AModulesQualityIsDisabled = 0b_0010_0000_0000_0000_0000,
			BModulesQualityIsDisabled = 0b_0100_0000_0000_0000_0000,

			TemeratureFluidBurnerInvalidLinks = 0b_0100_0000_0000_0000,
		}
		public Errors ErrorSet { get; private set; }
		public Warnings WarningSet { get; private set; }

		private readonly RecipeNodeController controller;
		public override BaseNodeController Controller { get { return controller; } }
		public override ReadOnlyBaseNode ReadOnlyNode { get; protected set; }

		[JsonProperty]
		public bool LowPriority { get; set; }

		public bool ShouldSerializeLowPriority() => LowPriority;

		public readonly RecipeQualityPair BaseRecipe;
		private double neighbourCount;
		[JsonProperty("Neighbours")]
		public double NeighbourCount { get { return neighbourCount; } set { if (neighbourCount != value) { neighbourCount = value; ioUpdateRequired = true; UpdateState(); OnNodeValuesChanged(); } } }

		private readonly DCache RecipeOwner;

		private AssemblerQualityPair assembler;
		public AssemblerQualityPair SelectedAssembler {
			get { return assembler; }
			set { if (value && assembler != value) { assembler = value; ioUpdateRequired = true; UpdateState(); OnNodeStateChanged(); } }
		}
		public IItem? Fuel {
			get { return fuel; }
			set { if (fuel != value) { fuel = value; fuelRemainsOverride = null; ioUpdateRequired = true; UpdateState(); OnNodeStateChanged(); } }
		}
		public IItem? FuelRemains {
			get {
				if (fuelRemainsOverride != null) return fuelRemainsOverride;
				if (Fuel != null && Fuel.BurnResult != null) return Fuel.BurnResult;
				return null;
			}
		}
		public void SetBurntOverride(IItem? item) {
			if (Fuel == null || Fuel.BurnResult != item) {
				fuelRemainsOverride = item;
				ioUpdateRequired = true;
				UpdateState();
				OnNodeValuesChanged();
			}
		}
		private IItem? fuel;
		private IItem? fuelRemainsOverride; //returns as BurntItem if set (error import)

		private BeaconQualityPair? selectedBeacon;
		public BeaconQualityPair? SelectedBeacon { get { return selectedBeacon; } set { if (selectedBeacon != value) { selectedBeacon = value; ioUpdateRequired = true; UpdateState(); OnNodeValuesChanged(); } } }
		private double beaconCount;
		[JsonProperty]
		public double BeaconCount { get { return beaconCount; } set { if (beaconCount != value) { beaconCount = value; ioUpdateRequired = true; UpdateState(); OnNodeValuesChanged(); } } }
		private double beaconsPerAssembler;
		[JsonProperty]
		public double BeaconsPerAssembler { get { return beaconsPerAssembler; } set { if (beaconsPerAssembler != value) { beaconsPerAssembler = value; UpdateState(); OnNodeValuesChanged(); } } }
		private double beaconsConst;
		[JsonProperty]
		public double BeaconsConst { get { return beaconsConst; } set { if (beaconsConst != value) { beaconsConst = value; UpdateState(); OnNodeValuesChanged(); } } }

		[JsonProperty]
		public IReadOnlyList<ModuleQualityPair> AssemblerModules { get { return assemblerModules; } }
		[JsonProperty]
		public IReadOnlyList<ModuleQualityPair> BeaconModules { get { return beaconModules; } }
		private readonly List<ModuleQualityPair> assemblerModules;
		private readonly List<ModuleQualityPair> beaconModules;

		//for recipe nodes, the SetValue is 'number of assemblers/entities'
		public override double ActualSetValue { get { return ActualRatePerSec * BaseRecipe.Recipe.Time / (SelectedAssembler.Assembler.GetSpeed(SelectedAssembler.Quality) * GetSpeedMultiplier()); } }
		public override double DesiredSetValue { get; set; }
		public override double MaxDesiredSetValue { get { return ProductionGraph.MaxFactories; } }
		public override string SetValueDescription { get { return "# of Assemblers:"; } }

		public override double DesiredRatePerSec { get { return DesiredSetValue * SelectedAssembler.Assembler.GetSpeed(SelectedAssembler.Quality) * GetSpeedMultiplier() / BaseRecipe.Recipe.Time; } set { Trace.Fail("Desired rate set on a recipe node!"); } }

		private double extraProductivityBonus;
		[JsonProperty("ExtraProductivity")]
		public double ExtraProductivityBonus { get { return extraProductivityBonus; } set { if (extraProductivityBonus != value) { extraProductivityBonus = value; ioUpdateRequired = true; UpdateState(); OnNodeValuesChanged(); } } }

		public uint MaxQualitySteps { get { return maxQualitySteps; } set { if (maxQualitySteps != value) { maxQualitySteps = value; ioUpdateRequired = true; } } } //if quality bonus > 0 then we will take this many extra quality steps for products
		private uint maxQualitySteps;

		public override IEnumerable<ItemQualityPair> Inputs { get { if (ioUpdateRequired) { UpdateInputsAndOutputs(); } return inputList; } }
		private readonly Dictionary<ItemQualityPair, double> inputSet;
		private readonly List<ItemQualityPair> inputList;

		public override IEnumerable<ItemQualityPair> Outputs { get { if (ioUpdateRequired) { UpdateInputsAndOutputs(); } return outputList; } }
		private readonly Dictionary<ItemQualityPair, double> outputSet;
		private readonly List<ItemQualityPair> outputList;

		public bool IsFuelPartOfRecipeInputs { get; private set; }
		public bool IsFuelRemainsPartOfRecipeOutputs { get; private set; }

		private bool ioUpdateRequired;

		[JsonProperty]
		public static NodeType NodeType => NodeType.Recipe;
		[JsonProperty]
		public long RecipeID => BaseRecipe.Recipe.RecipeID;
		[JsonProperty]
		public string RecipeQuality => BaseRecipe.Quality.Name;
		[JsonProperty]
		public string Assembler => SelectedAssembler.Assembler.Name;
		[JsonProperty]
		public string AssemblerQuality => SelectedAssembler.Quality.Name;
		[JsonProperty(nameof(Fuel))]
		public string? JsonFuel => Fuel?.Name;
		[JsonProperty("Burnt")]
		public string? JsonBurnt => FuelRemains?.Name;
		[JsonProperty]
		public string? Beacon => SelectedBeacon?.Beacon.Name;
		[JsonProperty]
		public string? BeaconQuality => SelectedBeacon?.Quality.Name;

		public bool ShouldSerializeBeacon() => SelectedBeacon is not null;
		public bool ShouldSerializeBeaconQuality() => SelectedBeacon is not null;
		public bool ShouldSerializeBeaconModules() => SelectedBeacon is not null;
		public bool ShouldSerializeBeaconCount() => SelectedBeacon is not null;
		public bool ShouldSerializeBeaconsPerAssembler() => SelectedBeacon is not null;
		public bool ShouldSerializeBeaconsConst() => SelectedBeacon is not null;

		public bool ShouldSerializeJsonFuel() => Fuel != null;
		public bool ShouldSerializeJsonBurnt() => FuelRemains != null;

		public RecipeNode(ProductionGraph graph, int nodeID, RecipeQualityPair recipe, IQuality assemblerQuality) : base(graph, nodeID) {
			LowPriority = false;
			maxQualitySteps = graph.MaxQualitySteps;
			ioUpdateRequired = false;

			BaseRecipe = recipe;
			RecipeOwner = recipe.Recipe.Owner;

			controller = RecipeNodeController.GetController(this);
			ReadOnlyNode = new ReadOnlyRecipeNode(this);

			inputSet = [];
			inputList = [];
			outputSet = [];
			outputList = [];

			assemblerModules = [];
			beaconModules = [];

			SelectedAssembler = new AssemblerQualityPair(recipe.Recipe.Assemblers.First(), assemblerQuality); //everything here works under the assumption that assember isnt null.
			SelectedBeacon = null; //no beacon selected
			NeighbourCount = 0;

			BeaconCount = 0;
			BeaconsPerAssembler = 0;
			BeaconsConst = 0;

			ExtraProductivityBonus = 0;
		}

		internal override NodeState GetUpdatedState() {
			WarningSet = Warnings.Clean;
			ErrorSet = Errors.Clean;

			//error states:
			if (BaseRecipe.Recipe.IsMissing)
				ErrorSet |= Errors.RecipeIsMissing;
			if (BaseRecipe.Quality.IsMissing)
				ErrorSet |= Errors.RQualityIsMissing;
			if (SelectedAssembler.Assembler.IsMissing)
				ErrorSet |= Errors.AssemblerIsMissing;
			if (SelectedAssembler.Quality.IsMissing)
				ErrorSet |= Errors.AQualityIsMissing;

			if (SelectedAssembler.Assembler.IsBurner) {
				if (Fuel == null)
					ErrorSet |= Errors.BurnerNoFuelSet;
				else {
					if (Fuel.IsMissing)
						ErrorSet |= Errors.FuelIsMissing;
					if (!SelectedAssembler.Assembler.Fuels.Contains(Fuel))
						ErrorSet |= Errors.InvalidFuel;
					if (Fuel.BurnResult != FuelRemains)
						ErrorSet |= Errors.InvalidFuelRemains;
				}
			}

			if (AssemblerModules.Any(m => m.Module.IsMissing))
				ErrorSet |= Errors.AModuleIsMissing;
			if (AssemblerModules.Count > SelectedAssembler.Assembler.ModuleSlots)
				ErrorSet |= Errors.AModuleLimitExceeded;
			if (AssemblerModules.Any(m => m.Quality.IsMissing))
				ErrorSet |= Errors.AModuleQualityIsMissing;

			if (SelectedBeacon is BeaconQualityPair selected) {
				if (selected.Beacon.IsMissing)
					ErrorSet |= Errors.BeaconIsMissing;
				if (selected.Quality.IsMissing)
					ErrorSet |= Errors.BQualityIsMissing;
				if (BeaconModules.Any(m => m.Module.IsMissing))
					ErrorSet |= Errors.BModuleIsMissing;
				if (BeaconModules.Count > selected.Beacon.ModuleSlots)
					ErrorSet |= Errors.BModuleLimitExceeded;
				if (BeaconModules.Any(m => m.Quality.IsMissing))
					ErrorSet |= Errors.AModuleQualityIsMissing;
				if (!selected.Beacon.Enabled)
					WarningSet |= Warnings.BeaconIsDisabled;
				if (!selected.Beacon.Available)
					WarningSet |= Warnings.BeaconIsUnavailable;
				if (!selected.Quality.Enabled)
					WarningSet |= Warnings.BeaconQualityIsDisabled;

			} else if (BeaconModules.Count != 0)
				ErrorSet |= Errors.BModuleLimitExceeded;

			if (!AllLinksValid)
				ErrorSet |= Errors.InvalidLinks;

			if (ErrorSet != Errors.Clean) //warnings are NOT processed if error has been found. This makes sense (as an error is something that trumps warnings), plus guarantees we dont accidentally check statuses of missing objects (which rightfully dont exist in regular cache)
				return NodeState.Error;

			//warning states (either not enabled or not available both throw up warnings)
			if (!BaseRecipe.Recipe.Enabled)
				WarningSet |= Warnings.RecipeIsDisabled;
			if (!BaseRecipe.Recipe.Available)
				WarningSet |= Warnings.RecipeIsUnavailable;

			if (!SelectedAssembler.Assembler.Enabled)
				WarningSet |= Warnings.AssemblerIsDisabled;
			if (!SelectedAssembler.Assembler.Available)
				WarningSet |= Warnings.AssemblerIsUnavailable;
			if (!SelectedAssembler.Quality.Enabled)
				WarningSet |= Warnings.AssemblerQualityIsDisabled;
			if (!BaseRecipe.Recipe.Assemblers.Any(a => a.Enabled))
				WarningSet |= Warnings.NoAvailableAssemblers;

			if (Fuel != null) {
				if (!Fuel.Available)
					WarningSet |= Warnings.FuelIsUnavailable;
				if (!Fuel.ProductionRecipes.Any(r => r.Enabled && r.Assemblers.Any(a => a.Enabled)))
					WarningSet |= Warnings.FuelIsUncraftable;
				if (!SelectedAssembler.Assembler.Fuels.Any(f => f.Enabled && f.ProductionRecipes.Any(r => r.Enabled && r.Assemblers.Any(a => a.Enabled))))
					WarningSet |= Warnings.NoAvailableFuels;
			}

			if (AssemblerModules.Any(m => !m.Module.Enabled))
				WarningSet |= Warnings.AModuleIsDisabled;
			if (AssemblerModules.Any(m => !m.Module.Available))
				WarningSet |= Warnings.AModuleIsUnavailable;
			if (AssemblerModules.Any(m => !m.Quality.Enabled))
				WarningSet |= Warnings.AModulesQualityIsDisabled;

			if (BeaconModules.Any(m => !m.Module.Enabled))
				WarningSet |= Warnings.BModuleIsDisabled;
			if (BeaconModules.Any(m => !m.Module.Available))
				WarningSet |= Warnings.BModuleIsUnavailable;
			if (BeaconModules.Any(m => !m.Quality.Enabled))
				WarningSet |= Warnings.BModulesQualityIsDisabled;

			if (SelectedAssembler.Assembler.IsTemperatureFluidBurner && (Fuel is not IFluid iFluid || !LinkChecker.GetTemperatureRange(iFluid, ReadOnlyNode, LinkType.Output, false).IsPoint()))
				WarningSet |= Warnings.TemeratureFluidBurnerInvalidLinks;

			if (WarningSet != Warnings.Clean)
				return NodeState.Warning;
			if (AllLinksConnected)
				return NodeState.Clean;
			return NodeState.MissingLink;

		}

		public void UpdateInputsAndOutputs(bool forceUpdate = false) {
			if (!ioUpdateRequired && !forceUpdate)
				return;
			ioUpdateRequired = false;

			//Inputs:
			inputSet.Clear();
			inputList.Clear();
			foreach (IItem item in BaseRecipe.Recipe.IngredientList) {
				ItemQualityPair inputItem = new(item, item is IFluid ? RecipeOwner.DefaultQuality : BaseRecipe.Quality);
				double inputQuantity = BaseRecipe.Recipe.IngredientSet[item];

				inputList.Add(inputItem);
				inputSet.Add(inputItem, inputQuantity);
			}
			if (Fuel != null) //provide the burner item if it isnt null or already part of recipe ingredients
			{
				ItemQualityPair fuelIQP = new(Fuel, RecipeOwner.DefaultQuality);
				if (!inputSet.ContainsKey(fuelIQP)) {
					IsFuelPartOfRecipeInputs = false;
					inputList.Add(fuelIQP);
					inputSet.Add(fuelIQP, InputRateForFuel());
				} else {
					IsFuelPartOfRecipeInputs = true;
					inputSet[fuelIQP] += InputRateForFuel();
				}
			}

			//Outputs:
			outputSet.Clear();
			outputList.Clear();
			foreach (IItem item in BaseRecipe.Recipe.ProductList) {
				if (SelectedAssembler.Assembler.EntityType == EntityType.Reactor) {
					ItemQualityPair product = new(item, RecipeOwner.DefaultQuality);
					double amount = BaseRecipe.Recipe.ProductSet[item] + 1 * SelectedAssembler.Assembler.NeighbourBonus * NeighbourCount;
					outputList.Add(product);
					outputSet.Add(product, amount);
				} else {
					double amount = BaseRecipe.Recipe.ProductSet[item] + BaseRecipe.Recipe.ProductPSet[item] * GetProductivityBonus();

					if (item is IFluid) {
						ItemQualityPair fluidProduct = new(item, RecipeOwner.DefaultQuality);
						outputList.Add(fluidProduct);
						outputSet.Add(fluidProduct, amount);
					} else {
						ItemQualityPair currentProduct = new(item, BaseRecipe.Quality);
						uint currentStep = 1;
						outputList.Add(currentProduct);
						outputSet.Add(currentProduct, amount);
						double currentMultiplier = GetQualityMultiplier();
						while (currentStep < MaxQualitySteps && currentProduct.Quality.NextQuality != null) {
							currentStep++;
							ItemQualityPair lastProduct = currentProduct;
							currentMultiplier *= currentProduct.Quality.NextProbability;
							currentProduct = new ItemQualityPair(item, currentProduct.Quality.NextQuality);
							if (currentMultiplier == 0)
								break;
							if (!currentProduct.Quality.Enabled || !currentProduct.Quality.Available)
								break;

							outputList.Add(currentProduct);
							outputSet.Add(currentProduct, Math.Min(currentMultiplier, 1.0) * amount);
							outputSet[lastProduct] -= outputSet[currentProduct];

							if (outputSet[lastProduct] <= 0) {
								outputList.Remove(lastProduct);
								outputSet.Remove(lastProduct);
							}

						}
					}
				}
			}
			if (FuelRemains != null) //provide the burnt item if it isnt null or already part of recipe ingredients
			{
				ItemQualityPair fuelRemainsIQP = new(FuelRemains, RecipeOwner.DefaultQuality);
				if (!outputSet.ContainsKey(fuelRemainsIQP)) {
					IsFuelRemainsPartOfRecipeOutputs = false;
					outputList.Add(fuelRemainsIQP);
					outputSet.Add(fuelRemainsIQP, InputRateForFuel());
				} else {
					IsFuelRemainsPartOfRecipeOutputs = true;
					outputSet[fuelRemainsIQP] += InputRateForFuel();
				}
			}

			//links
			foreach (NodeLink link in InputLinks.ToList()) {
				if (!inputSet.ContainsKey(link.Item))
					MyGraph.DeleteLink(link.ReadOnlyLink);
			}
			foreach (NodeLink link in OutputLinks.ToList()) {
				if (!outputSet.ContainsKey(link.Item))
					MyGraph.DeleteLink(link.ReadOnlyLink);
			}

			UpdateState();
		}

		//------------------------------------------------------------------------ assembly/beacon module sets

		public void BeaconModulesAdd(ModuleQualityPair module) { beaconModules.Add(module); ioUpdateRequired = true; }
		public void BeaconModulesAddRange(IEnumerable<ModuleQualityPair> modules) { beaconModules.AddRange(modules); ioUpdateRequired = true; }
		public void BeaconModulesRemoveAt(int index) { beaconModules.RemoveAt(index); ioUpdateRequired = true; }
		public void BeaconModulesRemoveAll(ModuleQualityPair module) { beaconModules.RemoveAll(m => m == module); ioUpdateRequired = true; }
		public void BeaconModulesClear() { beaconModules.Clear(); ioUpdateRequired = true; }

		public void AssemblerModulesAdd(ModuleQualityPair module) { assemblerModules.Add(module); ioUpdateRequired = true; }
		public void AssemblerModulesAddRange(IEnumerable<ModuleQualityPair> modules) { assemblerModules.AddRange(modules); ioUpdateRequired = true; }
		public void AssemblerModulesRemoveAt(int index) { assemblerModules.RemoveAt(index); ioUpdateRequired = true; }
		public void AssemblerModulesRemoveAll(ModuleQualityPair module) { assemblerModules.RemoveAll(m => m == module); ioUpdateRequired = true; }
		public void AssemblerModulesClear() { assemblerModules.Clear(); ioUpdateRequired = true; }



		//------------------------------------------------------------------------ multipliers (speed/productivity/consumption/pollution) & rates

		public double GetSpeedMultiplier() {
			if (SelectedAssembler.Assembler.EntityType == EntityType.Rocket) //this is a bit of a hack - by setting the speed multiplier here like so we get the # of buildings to be the # of rockets launched no matter the time scale.
				return 1 / MyGraph.GetRateMultipler();

			double multiplier = 1.0f;
			foreach (ModuleQualityPair module in AssemblerModules)
				multiplier += module.Module.GetSpeedBonus(module.Quality);
			if (SelectedBeacon is BeaconQualityPair selected)
				foreach (ModuleQualityPair beaconModule in BeaconModules)
					multiplier += beaconModule.Module.GetSpeedBonus(beaconModule.Quality) * selected.Beacon.GetBeaconEffectivity(selected.Quality, BeaconCount) * BeaconCount;
			return Math.Max(0.2f, multiplier);
		}

		public double GetProductivityBonus() //unlike most of the others, this is the bonus (aka: starts from 0%, not 100%) //also: quality bonus is rounded down to 2 decimal places (1 percent)
		{
			double multiplier = SelectedAssembler.Assembler.BaseProductivityBonus + ExtraProductivityBonus;
			foreach (ModuleQualityPair module in AssemblerModules)
				multiplier += module.Module.GetProductivityBonus(module.Quality);
			if (SelectedBeacon is BeaconQualityPair selected)
				foreach (ModuleQualityPair beaconModule in BeaconModules)
					multiplier += beaconModule.Module.GetProductivityBonus(beaconModule.Quality) * selected.Beacon.GetBeaconEffectivity(selected.Quality, BeaconCount) * BeaconCount;
			return Math.Min(Math.Max(0, multiplier), BaseRecipe.Recipe.MaxProductivityBonus);
		}

		public double GetConsumptionMultiplier() {
			double multiplier = 1.0f;
			foreach (ModuleQualityPair module in AssemblerModules)
				multiplier += module.Module.GetConsumptionBonus(module.Quality);
			if (SelectedBeacon is BeaconQualityPair selected)
				foreach (ModuleQualityPair beaconModule in BeaconModules)
					multiplier += beaconModule.Module.GetConsumptionBonus(beaconModule.Quality) * selected.Beacon.GetBeaconEffectivity(selected.Quality, BeaconCount) * BeaconCount;
			return Math.Max(0.2f, multiplier);
		}

		public double GetPollutionMultiplier() {
			double multiplier = 1.0f;
			foreach (ModuleQualityPair module in AssemblerModules)
				multiplier += module.Module.GetPolutionBonus(module.Quality);
			if (SelectedBeacon is BeaconQualityPair selected)
				foreach (ModuleQualityPair beaconModule in BeaconModules)
					multiplier += beaconModule.Module.GetPolutionBonus(beaconModule.Quality) * selected.Beacon.GetBeaconEffectivity(selected.Quality, BeaconCount) * BeaconCount;
			return Math.Max(0.2f, multiplier);
		}

		public double GetQualityMultiplier() //unlike the rest this one starts at 0 and is a multiplier (not bonus) - so without modules that add quality the chance to get better quality items from a recipe is 0%
		{
			double multiplier = 0.0f;
			foreach (ModuleQualityPair module in AssemblerModules)
				multiplier += module.Module.GetQualityBonus(module.Quality);
			if (SelectedBeacon is BeaconQualityPair selected)
				foreach (ModuleQualityPair beaconModule in BeaconModules)
					multiplier += beaconModule.Module.GetQualityBonus(beaconModule.Quality) * selected.Beacon.GetBeaconEffectivity(selected.Quality, BeaconCount) * BeaconCount;

			return Math.Max(0.0f, multiplier);
		}

		//------------------------------------------------------------------------ graph optimization functions

		public override double GetConsumeRate(ItemQualityPair item) { return inputRateFor(item) * ActualRate; }
		public override double GetSupplyRate(ItemQualityPair item) { return outputRateFor(item) * ActualRate; }

		internal override double inputRateFor(ItemQualityPair item) {
			if (ioUpdateRequired)
				UpdateInputsAndOutputs();
			return inputSet[item];
		}
		internal override double outputRateFor(ItemQualityPair item) {
			if (ioUpdateRequired)
				UpdateInputsAndOutputs();
			return outputSet[item];
		}

		internal double InputRateForFuel() {
			double temperature = double.NaN;
			if (Fuel is IFluid iFluid && SelectedAssembler.Assembler.IsTemperatureFluidBurner)
				temperature = LinkChecker.GetTemperatureRange(iFluid, ReadOnlyNode, LinkType.Output, false).Min;

			//burner rate = recipe time (modified by speed bonus & assembler) * fuel consumption rate of assembler (modified by fuel, temperature, and consumption modifier)
			return BaseRecipe.Recipe.Time / (SelectedAssembler.Assembler.GetSpeed(SelectedAssembler.Quality) * GetSpeedMultiplier()) * SelectedAssembler.Assembler.GetBaseFuelConsumptionRate(Fuel, SelectedAssembler.Quality, temperature) * GetConsumptionMultiplier();
		}

		internal double FactoryRate() {
			return BaseRecipe.Recipe.Time / (SelectedAssembler.Assembler.GetSpeed(SelectedAssembler.Quality) * GetSpeedMultiplier());
		}
		internal double GetMinOutputRatio() {
			double minValue = double.MaxValue;
			foreach (ItemQualityPair item in Outputs)
				minValue = Math.Min(minValue, outputRateFor(item));
			return minValue;
		}
		public override string ToString() { return string.Format("Recipe node for: {0} ({1})", BaseRecipe.Recipe.Name, BaseRecipe.Quality.Name); }
	}

	public class ReadOnlyRecipeNode(RecipeNode node) : ReadOnlyBaseNode(node) {
		public bool LowPriority => MyNode.LowPriority;

		public uint MaxQualitySteps => MyNode.MaxQualitySteps;

		public RecipeQualityPair BaseRecipe => MyNode.BaseRecipe;
		public AssemblerQualityPair SelectedAssembler => MyNode.SelectedAssembler;
		public IItem? Fuel => MyNode.Fuel;
		public IItem? FuelRemains => MyNode.FuelRemains;
		public IReadOnlyList<ModuleQualityPair> AssemblerModules => MyNode.AssemblerModules;

		public BeaconQualityPair? SelectedBeacon => MyNode.SelectedBeacon;
		public IReadOnlyList<ModuleQualityPair> BeaconModules => MyNode.BeaconModules;

		public double NeighbourCount => MyNode.NeighbourCount;
		public double ExtraProductivity => MyNode.ExtraProductivityBonus;
		public double BeaconCount => MyNode.BeaconCount;
		public double BeaconsPerAssembler => MyNode.BeaconsPerAssembler;
		public double BeaconsConst => MyNode.BeaconsConst;

		public double GetConsumptionMultiplier() => MyNode.GetConsumptionMultiplier();
		public double GetSpeedMultiplier() => MyNode.GetSpeedMultiplier();
		public double GetProductivityMultiplier() => MyNode.GetProductivityBonus() + 1;
		public double GetPollutionMultiplier() => MyNode.GetPollutionMultiplier();
		public double GetQualityMultiplier() => MyNode.GetQualityMultiplier();

		//------------------------------------------------------------------------ warning / errors functions

		public override List<string> GetErrors() {
			RecipeNode.Errors ErrorSet = MyNode.ErrorSet;

			List<string> output = [];

			if ((ErrorSet & RecipeNode.Errors.RecipeIsMissing) != 0) {
				output.Add(string.Format("> Recipe \"{0}\" doesnt exist in preset!", MyNode.BaseRecipe.Recipe.FriendlyName));
				return output; //missing recipe is an automatic end -> we dont care about any other errors, since the only solution is to delete the node.
			}
			if ((ErrorSet & RecipeNode.Errors.RQualityIsMissing) != 0)
				output.Add(string.Format("> Recipe's Quality \"{0}\" doesnt exist in preset!", MyNode.BaseRecipe.Quality.FriendlyName));

			if ((ErrorSet & RecipeNode.Errors.AssemblerIsMissing) != 0)
				output.Add(string.Format("> Assembler \"{0}\" doesnt exist in preset!", MyNode.SelectedAssembler.Assembler.FriendlyName));
			if ((ErrorSet & RecipeNode.Errors.AQualityIsMissing) != 0)
				output.Add(string.Format("> Assembler's Quality \"{0}\" doesnt exist in preset!", MyNode.SelectedAssembler.Quality.FriendlyName));

			if ((ErrorSet & RecipeNode.Errors.BurnerNoFuelSet) != 0)
				output.Add("> Burner Assembler has no fuel set!");
			if ((ErrorSet & RecipeNode.Errors.FuelIsMissing) != 0)
				output.Add("> Burner Assembler's fuel doesnt exist in preset!");
			if ((ErrorSet & RecipeNode.Errors.InvalidFuel) != 0)
				output.Add("> Burner Assembler has an invalid fuel set!");
			if ((ErrorSet & RecipeNode.Errors.InvalidFuelRemains) != 0)
				output.Add("> Burning result doesnt match fuel's burn result!");
			if ((ErrorSet & RecipeNode.Errors.AModuleIsMissing) != 0)
				output.Add("> Some of the assembler modules dont exist in preset!");
			if ((ErrorSet & RecipeNode.Errors.AModuleLimitExceeded) != 0)
				output.Add(string.Format("> Assembler has too many modules ({0}/{1})!", MyNode.AssemblerModules.Count, MyNode.SelectedAssembler.Assembler.ModuleSlots));
			if ((ErrorSet & RecipeNode.Errors.AModuleQualityIsMissing) != 0)
				output.Add(string.Format("> Assembler's Module's Quality \"{0}\" doesnt exist in preset!", MyNode.AssemblerModules.First(m => m.Quality.IsMissing).Quality.FriendlyName));

			if ((ErrorSet & RecipeNode.Errors.BeaconIsMissing) != 0)
				output.Add(string.Format("> Beacon \"{0}\" doesnt exist in preset!", MyNode.SelectedBeacon?.Beacon.FriendlyName));
			if ((ErrorSet & RecipeNode.Errors.BQualityIsMissing) != 0)
				output.Add(string.Format("> Beacon's Quality \"{0}\" doesnt exist in preset!", MyNode.SelectedBeacon?.Quality.FriendlyName));

			if ((ErrorSet & RecipeNode.Errors.BModuleIsMissing) != 0)
				output.Add("> Some of the beacon modules dont exist in preset!");
			if ((ErrorSet & RecipeNode.Errors.BModuleLimitExceeded) != 0)
				output.Add("> Beacon has too many modules!");
			if ((ErrorSet & RecipeNode.Errors.BModuleQualityIsMissing) != 0)
				output.Add(string.Format("> Beacon's Module's Quality \"{0}\" doesnt exist in preset!", MyNode.BeaconModules.First(m => m.Quality.IsMissing).Quality.FriendlyName));

			if ((ErrorSet & RecipeNode.Errors.InvalidLinks) != 0)
				output.Add("> Some links are invalid!");

			return output;
		}

		public override List<string> GetWarnings() {
			RecipeNode.Warnings WarningSet = MyNode.WarningSet;

			List<string> output = [];

			//recipe
			if ((WarningSet & RecipeNode.Warnings.RecipeIsDisabled) != 0)
				output.Add("X> Selected recipe is disabled.");
			if ((WarningSet & RecipeNode.Warnings.RecipeIsUnavailable) != 0)
				output.Add("X> Selected recipe is unavailable in regular play.");

			if ((WarningSet & RecipeNode.Warnings.NoAvailableAssemblers) != 0)
				output.Add("X> No enabled assemblers for this recipe.");
			else {
				if ((WarningSet & RecipeNode.Warnings.AssemblerIsDisabled) != 0)
					output.Add("> Selected assembler is disabled.");
				if ((WarningSet & RecipeNode.Warnings.AssemblerIsUnavailable) != 0)
					output.Add("> Selected assembler is unavailable in regular play.");
			}

			//fuel
			if ((WarningSet & RecipeNode.Warnings.NoAvailableFuels) != 0)
				output.Add("X> No fuel can be produced.");
			else {
				if ((WarningSet & RecipeNode.Warnings.FuelIsUnavailable) != 0)
					output.Add("> Selected fuel is unavailable in regular play.");
				if ((WarningSet & RecipeNode.Warnings.FuelIsUncraftable) != 0)
					output.Add("> Selected fuel cant be produced.");
			}
			if ((WarningSet & RecipeNode.Warnings.TemeratureFluidBurnerInvalidLinks) != 0)
				output.Add("> Temperature based fuel uses multiple incoming temperatures (fuel use # might be wrong).");

			//modules & beacon modules
			if ((WarningSet & RecipeNode.Warnings.AModuleIsDisabled) != 0)
				output.Add("> Some selected assembler modules are disabled.");
			if ((WarningSet & RecipeNode.Warnings.AModuleIsUnavailable) != 0)
				output.Add("> Some selected assembler modules are unavailable in regular play.");
			if ((WarningSet & RecipeNode.Warnings.BeaconIsDisabled) != 0)
				output.Add("> Selected beacon is disabled.");
			if ((WarningSet & RecipeNode.Warnings.BeaconIsUnavailable) != 0)
				output.Add("> Selected beacon is unavailable in regular play.");
			if ((WarningSet & RecipeNode.Warnings.BModuleIsDisabled) != 0)
				output.Add("> Some selected beacon modules are disabled.");
			if ((WarningSet & RecipeNode.Warnings.BModuleIsUnavailable) != 0)
				output.Add("> Some selected beacon modules are unavailable in regular play.");

			return output;
		}

		//----------------------------------------------------------------------- Get functions (single assembler/beacon info)

		public double GetGeneratorMinimumTemperature() {
			if (SelectedAssembler.Assembler.EntityType == EntityType.Generator) {
				//minimum temperature accepted by generator is the largest of either the default temperature (at which point the power generation is 0 and it actually doesnt consume anything), or the set min temp
				IFluid fluidBase = (IFluid)BaseRecipe.Recipe.IngredientList[0]; //generators have 1 input & 0 output. only input is the fluid being consumed.
				return Math.Max(fluidBase.DefaultTemperature + 0.1, BaseRecipe.Recipe.IngredientTemperatureMap[fluidBase].Min);
			}
			Trace.Fail("Cant ask for minimum generator temperature for a non-generator!");
			return 0;
		}

		public double GetGeneratorMaximumTemperature() {
			if (SelectedAssembler.Assembler.EntityType == EntityType.Generator)
				return BaseRecipe.Recipe.IngredientTemperatureMap[BaseRecipe.Recipe.IngredientList[0]].Max;
			Trace.Fail("Cant ask for maximum generator temperature for a non-generator!");
			return 0;
		}

		public double GetGeneratorAverageTemperature() {
			if (SelectedAssembler.Assembler.EntityType != EntityType.Generator)
				Trace.Fail("Cant ask for average generator temperature for a non-generator!");

			return GetAverageTemperature(this, MyNode.BaseRecipe.Recipe.IngredientList[0]);
			double GetAverageTemperature(ReadOnlyBaseNode node, IItem item) {
				if (node is ReadOnlyPassthroughNode || node == this) {
					double totalFlow = 0;
					double totalTemperatureFlow = 0;
					double totalTemperature = 0;
					foreach (ReadOnlyNodeLink link in node.InputLinks) //Throughput node: all same item. Generator node: only input is the fluid item.
					{
						totalFlow += link.Throughput;
						double temperature = GetAverageTemperature(link.Supplier, item);
						totalTemperatureFlow += temperature * link.Throughput;
						totalTemperature += temperature;
					}
					if (totalFlow == 0) {
						if (!node.InputLinks.Any())
							return SelectedAssembler.Assembler.OperationTemperature;
						else
							return totalTemperature / node.InputLinks.Count();
					}
					return totalTemperatureFlow / totalFlow;
				} else if (node is ReadOnlySupplierNode)
					return SelectedAssembler.Assembler.OperationTemperature; //assume supplier is optimal temperature (cant exactly set to infinity or something as that would just cause the final result to be infinity)
				else if (node is ReadOnlyRecipeNode rnode)
					return rnode.BaseRecipe.Recipe.ProductTemperatureMap[item];
				Trace.Fail("Unexpected node type in generator calculation!");
				return 0;
			}
		}

		public double GetGeneratorEffectivity() {
			IFluid fluid = (IFluid)MyNode.BaseRecipe.Recipe.IngredientList[0];
			return Math.Min((GetGeneratorAverageTemperature() - fluid.DefaultTemperature) / (MyNode.SelectedAssembler.Assembler.OperationTemperature - fluid.DefaultTemperature), 1);
		}

		public double GetGeneratorElectricalProduction() //Watts
		{
			if (SelectedAssembler.Assembler.EntityType == EntityType.Generator)
				return SelectedAssembler.Assembler.GetEnergyProduction(SelectedAssembler.Quality) * GetGeneratorEffectivity();
			return SelectedAssembler.Assembler.GetEnergyProduction(SelectedAssembler.Quality); //no consumption multiplier => generators cant have modules / beacon effects
		}


		public double GetAssemblerSpeed() {
			return SelectedAssembler.Assembler.GetSpeed(SelectedAssembler.Quality) * MyNode.GetSpeedMultiplier();
		}

		public double GetAssemblerEnergyConsumption() //Watts
		{
			return SelectedAssembler.Assembler.GetEnergyDrain() + SelectedAssembler.Assembler.GetEnergyConsumption(SelectedAssembler.Quality) * MyNode.GetConsumptionMultiplier();
		}

		public static double GetAssemblerPollutionProduction() //pollution/sec
		{
			// TODO: implement pollution calculation
			return 0;
		}

		public double GetBeaconEnergyConsumption() //Watts
		{
			if (SelectedBeacon is not BeaconQualityPair selected || selected.Beacon.EnergySource != EnergySource.Electric)
				return 0;
			return selected.Beacon.GetEnergyProduction(selected.Quality) + selected.Beacon.GetEnergyDrain();
		}

		public static double GetBeaconPollutionProduction() //pollution/sec
		{
			// TODO: implement pollution calculation
			return 0;
		}

		//----------------------------------------------------------------------- Get functions (totals)

		public double GetTotalCrafts() {
			return GetAssemblerSpeed() * MyNode.MyGraph.GetRateMultipler() / MyNode.BaseRecipe.Recipe.Time;
		}

		public double GetTotalAssemblerFuelConsumption() //fuel items / time unit
		{
			if (MyNode.Fuel == null)
				return 0;
			return MyNode.MyGraph.GetRateMultipler() * MyNode.InputRateForFuel();
		}

		public double GetTotalAssemblerElectricalConsumption() // J/sec (W)
		{
			if (MyNode.SelectedAssembler.Assembler.EnergySource != EnergySource.Electric)
				return 0;

			double partialAssembler = MyNode.ActualSetValue % 1;
			double entireAssemblers = MyNode.ActualSetValue - partialAssembler;

			return (entireAssemblers + (partialAssembler < 0.05 ? 0 : 1)) * SelectedAssembler.Assembler.GetEnergyDrain() + ActualSetValue * SelectedAssembler.Assembler.GetEnergyConsumption(SelectedAssembler.Quality) * GetConsumptionMultiplier(); //if there is more than 5% of an extra assembler, assume there is +1 assembler working x% of the time (full drain, x% uptime)
		}

		public double GetTotalGeneratorElectricalProduction() // J/sec (W) ; this is also when the temperature range of incoming fuel is taken into account
		{
			return GetGeneratorElectricalProduction() * MyNode.ActualSetValue;
		}

		public int GetTotalBeacons() {
			if (MyNode.SelectedBeacon is null)
				return 0;
			return (int)Math.Ceiling((int)(MyNode.ActualSetValue + 0.8) * BeaconsPerAssembler + BeaconsConst); //assume 0.2 assemblers (or more) is enough to warrant an extra 'beacons per assembler' row
		}

		public double GetTotalBeaconElectricalConsumption() // J/sec (W)
		{
			if (MyNode.SelectedBeacon is null)
				return 0;
			return GetTotalBeacons() * GetBeaconEnergyConsumption();
		}

		private readonly RecipeNode MyNode = node;
	}

	public class RecipeNodeController : BaseNodeController {
		private readonly RecipeNode MyNode;

		protected RecipeNodeController(RecipeNode myNode) : base(myNode) { MyNode = myNode; }

		public static RecipeNodeController GetController(RecipeNode node) {
			if (node.Controller != null)
				return (RecipeNodeController)node.Controller;
			return new RecipeNodeController(node);
		}

		//------------------------------------------------------------------------ warning / errors functions

		public override Dictionary<string, Action> GetErrorResolutions() {
			RecipeNode.Errors ErrorSet = MyNode.ErrorSet;

			Dictionary<string, Action> resolutions = [];
			if ((ErrorSet & RecipeNode.Errors.RecipeIsMissing) != 0)
				resolutions.Add("Delete node", new Action(() => { Delete(); }));
			else {
				if ((ErrorSet & (RecipeNode.Errors.AssemblerIsMissing | RecipeNode.Errors.AQualityIsMissing)) != 0)
					resolutions.Add("Auto-select assembler & quality", new Action(() => AutoSetAssembler()));

				if ((ErrorSet & (RecipeNode.Errors.FuelIsMissing | RecipeNode.Errors.InvalidFuel)) != 0 && MyNode.SelectedAssembler.Assembler.Fuels.Any(f => !f.IsMissing))
					resolutions.Add("Auto-select fuel", new Action(() => AutoSetFuel()));

				if ((ErrorSet & RecipeNode.Errors.InvalidFuelRemains) != 0 && MyNode.SelectedAssembler.Assembler.Fuels.Contains(MyNode.Fuel) && MyNode.Fuel is IItem iItem)
					resolutions.Add("Update burn result", new Action(() => SetFuel(iItem)));

				if ((ErrorSet & (RecipeNode.Errors.AModuleIsMissing | RecipeNode.Errors.AModuleLimitExceeded | RecipeNode.Errors.AModuleQualityIsMissing)) != 0)
					resolutions.Add("Fix assembler modules", new Action(() => {
						for (int i = MyNode.AssemblerModules.Count - 1; i >= 0; i--)
							if (MyNode.AssemblerModules[i].Module.IsMissing || !MyNode.SelectedAssembler.Assembler.Modules.Contains(MyNode.AssemblerModules[i].Module) || !MyNode.BaseRecipe.Recipe.AssemblerModules.Contains(MyNode.AssemblerModules[i].Module) || MyNode.AssemblerModules[i].Quality.IsMissing)
								RemoveAssemblerModule(i);
						while (MyNode.AssemblerModules.Count > MyNode.SelectedAssembler.Assembler.ModuleSlots)
							RemoveAssemblerModule(MyNode.AssemblerModules.Count - 1);
					}));

				if ((ErrorSet & (RecipeNode.Errors.BeaconIsMissing | RecipeNode.Errors.BQualityIsMissing)) != 0)
					resolutions.Add("Remove Beacon", new Action(() => ClearBeacon()));

				if ((ErrorSet & (RecipeNode.Errors.BModuleIsMissing | RecipeNode.Errors.BModuleLimitExceeded | RecipeNode.Errors.BModuleQualityIsMissing)) != 0)
					resolutions.Add("Fix beacon modules", new Action(() => {
						for (int i = MyNode.BeaconModules.Count - 1; i >= 0; i--)
							if (MyNode.BeaconModules[i].Module.IsMissing || !MyNode.SelectedAssembler.Assembler.Modules.Contains(MyNode.BeaconModules[i].Module) || !MyNode.BaseRecipe.Recipe.AssemblerModules.Contains(MyNode.BeaconModules[i].Module) || MyNode.SelectedBeacon is not BeaconQualityPair bqp || !bqp.Beacon.Modules.Contains(MyNode.BeaconModules[i].Module) || MyNode.BeaconModules[i].Quality.IsMissing)
								RemoveBeaconModule(i);
						while (MyNode.BeaconModules.Count > MyNode.SelectedBeacon?.Beacon.ModuleSlots)
							RemoveBeaconModule(MyNode.BeaconModules.Count - 1);
					}));

				foreach (KeyValuePair<string, Action> kvp in GetInvalidConnectionResolutions())
					resolutions.Add(kvp.Key, kvp.Value);
			}

			return resolutions;
		}

		public override Dictionary<string, Action> GetWarningResolutions() {
			RecipeNode.Warnings WarningSet = MyNode.WarningSet;

			Dictionary<string, Action> resolutions = [];

			if ((WarningSet & (RecipeNode.Warnings.AssemblerIsDisabled | RecipeNode.Warnings.AssemblerIsUnavailable | RecipeNode.Warnings.AssemblerQualityIsDisabled)) != 0 && (WarningSet & RecipeNode.Warnings.NoAvailableAssemblers) == 0)
				resolutions.Add("Switch to enabled assembler", new Action(() => AutoSetAssembler()));

			if ((WarningSet & (RecipeNode.Warnings.FuelIsUnavailable | RecipeNode.Warnings.FuelIsUncraftable)) != 0 && (WarningSet & RecipeNode.Warnings.NoAvailableFuels) == 0)
				resolutions.Add("Switch to valid fuel", new Action(() => AutoSetFuel()));

			if ((WarningSet & (RecipeNode.Warnings.AModuleIsDisabled | RecipeNode.Warnings.AModuleIsUnavailable | RecipeNode.Warnings.AModulesQualityIsDisabled)) != 0)
				resolutions.Add("Remove error modules from assembler", new Action(() => {
					for (int i = MyNode.AssemblerModules.Count - 1; i >= 0; i--)
						if (!MyNode.AssemblerModules[i].Module.Enabled || !MyNode.AssemblerModules[i].Module.Available || !MyNode.AssemblerModules[i].Quality.Enabled)
							RemoveAssemblerModule(i);
				}));

			if ((WarningSet & (RecipeNode.Warnings.BeaconIsDisabled | RecipeNode.Warnings.BeaconIsUnavailable)) != 0)
				resolutions.Add("Turn off beacon", new Action(() => ClearBeacon()));

			if ((WarningSet & (RecipeNode.Warnings.BModuleIsDisabled | RecipeNode.Warnings.BModuleIsUnavailable | RecipeNode.Warnings.BModulesQualityIsDisabled)) != 0)
				resolutions.Add("Remove error modules from beacon", new Action(() => {
					for (int i = MyNode.BeaconModules.Count - 1; i >= 0; i--)
						if (!MyNode.BeaconModules[i].Module.Enabled || !MyNode.BeaconModules[i].Module.Available || !MyNode.BeaconModules[i].Quality.Enabled)
							RemoveBeaconModule(i);
				}));

			var iItem = MyNode.Fuel as IItem ?? throw new InvalidOperationException("MyNode.Fuel is not IItem");
			if ((WarningSet & RecipeNode.Warnings.TemeratureFluidBurnerInvalidLinks) != 0)
				resolutions.Add("Remove fuel links", new Action(() => {
					foreach (NodeLink fuelLink in MyNode.InputLinks.Where(l => l.Item == new ItemQualityPair(iItem, iItem.Owner.DefaultQuality)).ToList())
						MyNode.MyGraph.DeleteLink(fuelLink.ReadOnlyLink);
				}));

			return resolutions;
		}

		//-----------------------------------------------------------------------Set functions

		public void SetPriority(bool lowPriority) { MyNode.LowPriority = lowPriority; MyNode.UpdateState(); }

		public void SetNeighbourCount(double count) { if (MyNode.NeighbourCount != count) MyNode.NeighbourCount = count; }
		public void SetExtraProductivityBonus(double bonus) { if (MyNode.ExtraProductivityBonus != bonus) MyNode.ExtraProductivityBonus = bonus; }
		public void SetBeaconCount(double count) { if (MyNode.BeaconCount != count) MyNode.BeaconCount = count; }
		public void SetBeaconsPerAssembler(double beacons) { if (MyNode.BeaconsPerAssembler != beacons) MyNode.BeaconsPerAssembler = beacons; }
		public void SetBeaconsCont(double beacons) { if (MyNode.BeaconsConst != beacons) MyNode.BeaconsConst = beacons; }

		public void SetAssembler(AssemblerQualityPair assembler) {
			MyNode.SelectedAssembler = assembler;

			//fuel
			if (!assembler.Assembler.IsBurner)
				SetFuel(null);
			else if (MyNode.Fuel != null && assembler.Assembler.Fuels.Contains(MyNode.Fuel))
				SetFuel(MyNode.Fuel);
			else
				AutoSetFuel();

			//check for invalid modules
			for (int i = MyNode.AssemblerModules.Count - 1; i >= 0; i--)
				if (MyNode.AssemblerModules[i].Module.IsMissing ||
					!MyNode.SelectedAssembler.Assembler.Modules.Contains(MyNode.AssemblerModules[i].Module) ||
					!MyNode.BaseRecipe.Recipe.AssemblerModules.Contains(MyNode.AssemblerModules[i].Module) ||
					!MyNode.AssemblerModules[i].Quality.Available ||
					MyNode.AssemblerModules[i].Quality.IsMissing) { MyNode.AssemblerModulesRemoveAt(i); }

			//check for too many modules
			while (MyNode.AssemblerModules.Count > MyNode.SelectedAssembler.Assembler.ModuleSlots)
				MyNode.AssemblerModulesRemoveAt(MyNode.AssemblerModules.Count - 1);

			//check if any modules work (if none work, then turn off beacon)
			if (MyNode.SelectedAssembler.Assembler.Modules.Count == 0 || MyNode.BaseRecipe.Recipe.AssemblerModules.Count == 0)
				ClearBeacon();
			else if (MyNode.SelectedBeacon is BeaconQualityPair bqp) //update beacon
				SetBeacon(bqp);

			MyNode.UpdateInputsAndOutputs();
			MyNode.UpdateState();
		}

		public void AutoSetAssembler() {
			IQuality quality = MyNode.SelectedAssembler.Quality.IsMissing || !MyNode.SelectedAssembler.Quality.Enabled ? MyNode.SelectedAssembler.Assembler.Owner.DefaultQuality : MyNode.SelectedAssembler.Quality;
			IAssembler assembler = MyNode.MyGraph.AssemblerSelector.GetAssembler(MyNode.BaseRecipe.Recipe);

			SetAssembler(new AssemblerQualityPair(assembler, quality));
			AutoSetFuel();
		}

		public void AutoSetAssembler(AssemblerSelector.Style style) {
			IQuality quality = MyNode.SelectedAssembler.Quality.IsMissing || !MyNode.SelectedAssembler.Quality.Enabled ? MyNode.SelectedAssembler.Assembler.Owner.DefaultQuality : MyNode.SelectedAssembler.Quality;
			IAssembler assembler = AssemblerSelector.GetAssembler(MyNode.BaseRecipe.Recipe, style);

			SetAssembler(new AssemblerQualityPair(assembler, quality));
			AutoSetFuel();
		}

		public void SetFuel(IItem? fuel) {
			if (MyNode.Fuel != fuel || MyNode.Fuel == null && MyNode.FuelRemains != null || MyNode.Fuel != null && MyNode.Fuel.BurnResult != MyNode.FuelRemains) {
				//have to remove any links to the burner/burnt item (if they exist) unless the item is also part of the recipe
				if (MyNode.Fuel != null && !MyNode.IsFuelPartOfRecipeInputs) {
					ItemQualityPair fuelIQP = new(MyNode.Fuel, MyNode.Fuel.Owner.DefaultQuality);
					foreach (NodeLink link in MyNode.InputLinks.Where(link => link.Item == fuelIQP).ToList())
						link.Controller.Delete();
				}
				if (MyNode.FuelRemains != null && !MyNode.IsFuelRemainsPartOfRecipeOutputs) {
					ItemQualityPair fuelRemainsIQP = new(MyNode.FuelRemains, MyNode.FuelRemains.Owner.DefaultQuality);
					foreach (NodeLink link in MyNode.OutputLinks.Where(link => link.Item == fuelRemainsIQP).ToList())
						link.Controller.Delete();
				}

				MyNode.Fuel = fuel;
				MyNode.MyGraph.FuelSelector.UseFuel(fuel);
				MyNode.UpdateState();
			}
		}

		public void AutoSetFuel() {
			SetFuel(MyNode.MyGraph.FuelSelector.GetFuel(MyNode.SelectedAssembler.Assembler));
		}

		public void ClearBeacon() {
			MyNode.SelectedBeacon = null; //clearing beacon
			MyNode.BeaconModulesClear();
			MyNode.BeaconCount = 0;
			MyNode.BeaconsPerAssembler = 0;
			MyNode.BeaconsConst = 0;
			MyNode.UpdateState();
		}

		public void SetBeacon(BeaconQualityPair beacon) {
			if (!beacon) { ClearBeacon(); return; } //shouldnt be called - but whatever

			MyNode.SelectedBeacon = beacon;
			//check for invalid modules
			for (int i = MyNode.BeaconModules.Count - 1; i >= 0; i--) {
				if (MyNode.BeaconModules[i].Module.IsMissing ||
					!MyNode.SelectedAssembler.Assembler.Modules.Contains(MyNode.BeaconModules[i].Module) ||
					!MyNode.BaseRecipe.Recipe.AssemblerModules.Contains(MyNode.BeaconModules[i].Module) ||
					MyNode.SelectedBeacon is not BeaconQualityPair bqp ||
					!bqp.Beacon.Modules.Contains(MyNode.BeaconModules[i].Module) ||
					!MyNode.BeaconModules[i].Quality.Available ||
					MyNode.BeaconModules[i].Quality.IsMissing) { MyNode.BeaconModulesRemoveAt(i); }
			}
			//check for too many modules
			while (MyNode.BeaconModules.Count > MyNode.SelectedBeacon?.Beacon.ModuleSlots)
				MyNode.BeaconModulesRemoveAt(MyNode.BeaconModules.Count - 1);

			MyNode.UpdateState();
		}

		public void AddAssemblerModule(ModuleQualityPair module) {
			MyNode.AssemblerModulesAdd(module);
			MyNode.UpdateState();
		}

		public void AddAssemblerModules(ModuleQualityPair module) {
			while (MyNode.AssemblerModules.Count < MyNode.SelectedAssembler.Assembler.ModuleSlots)
				MyNode.AssemblerModulesAdd(module);
			MyNode.UpdateState();
		}

		public void RemoveAssemblerModule(int index) {
			if (index >= 0 && index < MyNode.AssemblerModules.Count)
				MyNode.AssemblerModulesRemoveAt(index);
			MyNode.UpdateState();
		}

		public void RemoveAssemblerModules(ModuleQualityPair module) {
			MyNode.AssemblerModulesRemoveAll(module);
			MyNode.UpdateState();
		}

		public void RemoveAssemblerModules() {
			MyNode.AssemblerModulesClear();
			MyNode.UpdateState();
		}

		public void SetAssemblerModules(IEnumerable<ModuleQualityPair> modules, bool filterModules) {
			MyNode.AssemblerModulesClear();
			if (modules != null) {
				if (filterModules) {
					HashSet<IModule> acceptableModules = new(MyNode.BaseRecipe.Recipe.AssemblerModules.Intersect(MyNode.SelectedAssembler.Assembler.Modules));
					foreach (ModuleQualityPair m in modules)
						if (MyNode.AssemblerModules.Count < MyNode.SelectedAssembler.Assembler.ModuleSlots && acceptableModules.Contains(m.Module))
							MyNode.AssemblerModulesAdd(m);
				} else
					MyNode.AssemblerModulesAddRange(modules);
			}
			MyNode.UpdateState();
		}

		public void AutoSetAssemblerModules() {
			MyNode.AssemblerModulesClear();
			MyNode.AssemblerModulesAddRange(MyNode.MyGraph.ModuleSelector.GetModules(MyNode.SelectedAssembler.Assembler, MyNode.BaseRecipe.Recipe).ConvertAll(i => new ModuleQualityPair(i, i.Owner.DefaultQuality)));
			MyNode.UpdateState();
		}

		public void AutoSetAssemblerModules(ModuleSelector.Style style) {
			MyNode.AssemblerModulesClear();
			MyNode.AssemblerModulesAddRange(MyNode.MyGraph.ModuleSelector.GetModules(MyNode.SelectedAssembler.Assembler, MyNode.BaseRecipe.Recipe, style).ConvertAll(i => new ModuleQualityPair(i, i.Owner.DefaultQuality)));
			MyNode.UpdateState();
		}

		public void AddBeaconModule(ModuleQualityPair module) {
			MyNode.BeaconModulesAdd(module);
			MyNode.UpdateState();
		}

		public void AddBeaconModules(ModuleQualityPair module) {
			while (MyNode.BeaconModules.Count < MyNode.SelectedBeacon?.Beacon.ModuleSlots)
				MyNode.BeaconModulesAdd(module);
			MyNode.UpdateState();
		}

		public void RemoveBeaconModule(int index) {
			if (index >= 0 && index < MyNode.BeaconModules.Count)
				MyNode.BeaconModulesRemoveAt(index);
			MyNode.UpdateState();
		}

		public void RemoveBeaconModules(ModuleQualityPair module) {
			MyNode.BeaconModulesRemoveAll(module);
			MyNode.UpdateState();
		}

		public void SetBeaconModules(IEnumerable<ModuleQualityPair> modules, bool filterModules) {
			MyNode.BeaconModulesClear();
			if (modules != null) {
				if (filterModules && MyNode.SelectedBeacon is BeaconQualityPair bqp) {
					HashSet<IModule> acceptableModules = new(MyNode.BaseRecipe.Recipe.AssemblerModules.Intersect(MyNode.SelectedAssembler.Assembler.Modules).Intersect(bqp.Beacon.Modules));
					foreach (ModuleQualityPair m in modules)
						if (MyNode.BeaconModules.Count < MyNode.SelectedBeacon?.Beacon.ModuleSlots && acceptableModules.Contains(m.Module))
							MyNode.BeaconModulesAdd(m);
				} else
					MyNode.BeaconModulesAddRange(modules);
			}
			MyNode.UpdateState();
		}
	}
}
