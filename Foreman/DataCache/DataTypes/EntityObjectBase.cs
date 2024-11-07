using System;
using System.Collections.Generic;
using System.Linq;

namespace Foreman.DataCache.DataTypes {
	//burner & fluid burner types add fuel & burnt remains to the recipe node they are part of.
	//electric types add to the energy consumption (electic calculations) totals
	//heat types add a special 'heat' item to the recipe node they are part of (similar to burner types) -> in fact to simplify things they are handled as a burner with a specific burn item of 'heat'
	//void are considered as electric types with 0 electricity use
	public enum EnergySource { Burner, FluidBurner, Electric, Heat, Void }
	public enum EntityType { Miner, OffshorePump, Assembler, Beacon, Boiler, Generator, BurnerGenerator, Reactor, Rocket, ERROR }

	public interface IEntityObjectBase : IDataObjectBase {
		IReadOnlyCollection<IModule> Modules { get; }
		IReadOnlyCollection<IItem> Fuels { get; }
		IReadOnlyCollection<IItem> AssociatedItems { get; }
		IReadOnlyDictionary<string, double> Pollution { get; }

		EntityType EntityType { get; }
		string GetEntityTypeName(bool plural);
		EnergySource EnergySource { get; }
		bool IsBurner { get; }
		bool IsTemperatureFluidBurner { get; }
		FRange FluidFuelTemperatureRange { get; }

		double GetBaseFuelConsumptionRate(IItem? fuel, IQuality quality, double temperature = double.NaN);

		bool IsMissing { get; }

		double GetSpeed(IQuality quality);

		int ModuleSlots { get; }

		double GetEnergyDrain();
		double GetEnergyConsumption(IQuality quality);
		double GetEnergyProduction(IQuality quality);

		double ConsumptionEffectivity { get; }

		//steam generators
		double OperationTemperature { get; }
		//reactors
		double NeighbourBonus { get; }
	}

	internal class EntityObjectBasePrototype(DCache dCache, string name, string friendlyName, EntityType type, EnergySource source, bool isMissing) : DataObjectBasePrototype(dCache, name, friendlyName, "-"), IEntityObjectBase {
		private bool availableOverride = false;
		public override bool Available { get { return availableOverride || AssociatedItemsInternal.Any(i => i.ProductionRecipesInternal.Any(r => r.Available)); } set { availableOverride = value; } }

		public IReadOnlyCollection<IModule> Modules { get { return ModulesInternal; } }
		public IReadOnlyCollection<IItem> Fuels { get { return FuelsInternal; } }
		public IReadOnlyCollection<IItem> AssociatedItems { get { return AssociatedItemsInternal; } }
		public IReadOnlyDictionary<string, double> Pollution { get { return PollutionInternal; } }

		internal HashSet<ModulePrototype> ModulesInternal { get; private set; } = [];
		internal HashSet<ItemPrototype> FuelsInternal { get; private set; } = [];
		internal List<ItemPrototype> AssociatedItemsInternal { get; private set; } = [];
		internal Dictionary<string, double> PollutionInternal { get; private set; } = [];

		public EntityType EntityType { get; private set; } = type;
		public EnergySource EnergySource { get; internal set; } = source;
		public bool IsMissing { get; internal set; } = isMissing;
		public bool IsBurner { get { return EnergySource == EnergySource.Burner || EnergySource == EnergySource.FluidBurner || EnergySource == EnergySource.Heat; } }
		public bool IsTemperatureFluidBurner { get; set; }
		public FRange FluidFuelTemperatureRange { get; set; } = new FRange(double.MinValue, double.MaxValue);

		internal Dictionary<IQuality, double> Speed { get; private set; } = [];
		internal Dictionary<IQuality, double> EnergyConsumption { get; private set; } = [];
		internal Dictionary<IQuality, double> EnergyProduction { get; private set; } = [];

		public double GetSpeed(IQuality quality) { return Speed.TryGetValue(quality, out double value) ? value > 0 ? value : 1 : 1; }

		public int ModuleSlots { get; internal set; } = 0;
		public double NeighbourBonus { get; internal set; } = 0;

		internal double energyDrain;
		public double GetEnergyDrain() { return energyDrain; }
		public double GetEnergyConsumption(IQuality quality) {
			if (this is BeaconPrototype)
				return quality.BeaconPowerMultiplier * (EnergyConsumption.TryGetValue(quality, out double value) ? value : 1000);
			else
				return EnergyConsumption.TryGetValue(quality, out double value) ? value : 1000;
		}
		public double GetEnergyProduction(IQuality quality) { return EnergyConsumption.ContainsKey(quality) ? EnergyProduction[quality] : 0; }

		public double ConsumptionEffectivity { get; internal set; } = 1f;
		public double OperationTemperature { get; internal set; } = double.MaxValue;

		public double GetBaseFuelConsumptionRate(IItem? fuel, IQuality quality, double temperature = double.NaN) {
			if (IsMissing) return 0.01; //prevents failure from importing a recipe node with set fuel without a valid assembler

			if (EnergySource != EnergySource.Burner && EnergySource != EnergySource.FluidBurner && EnergySource != EnergySource.Heat)
				return 0.01; // Trace.Fail(string.Format("Cant ask for fuel consumption rate on a non-burner! {0}", this));
			else if (fuel is null || !FuelsInternal.Contains(fuel))
				return 0.01; // Trace.Fail(string.Format("Invalid fuel! {0} for entity {1}", fuel, this));
			else if (!IsTemperatureFluidBurner)
				return GetEnergyConsumption(quality) / (fuel.FuelValue * ConsumptionEffectivity);
			else if (!double.IsNaN(temperature) && fuel is IFluid fluidFuel && temperature > fluidFuel.DefaultTemperature && fluidFuel.SpecificHeatCapacity > 0) //temperature burn of liquid
				return GetEnergyConsumption(quality) / ((temperature - fluidFuel.DefaultTemperature) * fluidFuel.SpecificHeatCapacity * ConsumptionEffectivity);
			return 0.01;

			//0.01 is returned in case of error and prevents the solver from crashing. These errors will be noted on the node, so dont have to worry about them here.
		}

		public string GetEntityTypeName(bool plural) {
			if (plural) {
				return EntityType switch {
					EntityType.Assembler => "Assemblers",
					EntityType.Beacon => "Beacons",
					EntityType.Boiler => "Boilers",
					EntityType.BurnerGenerator => "Generators",
					EntityType.Generator => "Generators",
					EntityType.Miner => "Miners",
					EntityType.OffshorePump => "Offshore Pumps",
					EntityType.Reactor => "Reactors",
					EntityType.Rocket => "Rockets",
					_ => "",
				};
			} else {
				return EntityType switch {
					EntityType.Assembler => "Assembler",
					EntityType.Beacon => "Beacon",
					EntityType.Boiler => "Boiler",
					EntityType.BurnerGenerator => "Generator",
					EntityType.Generator => "Generator",
					EntityType.Miner => "Miner",
					EntityType.OffshorePump => "Offshore Pump",
					EntityType.Reactor => "Reactor",
					EntityType.Rocket => "Rocket",
					_ => "",
				};
			}
		}
	}
}
