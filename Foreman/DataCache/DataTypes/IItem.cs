using System.Collections.Generic;

namespace Foreman.DataCache.DataTypes {
	public interface IItem : IDataObjectBase {

		ISubgroup MySubgroup { get; }

		IReadOnlyCollection<IRecipe> ProductionRecipes { get; }
		IReadOnlyCollection<IRecipe> ConsumptionRecipes { get; }
		IReadOnlyCollection<ITechnology> ConsumptionTechnologies { get; }

		bool IsMissing { get; }

		int StackSize { get; }

		double Weight { get; }
		double IngredientToWeightCoefficient { get; }
		double FuelValue { get; }
		double PollutionMultiplier { get; }

		IItem? BurnResult { get; }
		IPlantProcess? PlantResult { get; }
		IItem? SpoilResult { get; }

		IItem? FuelOrigin { get; }
		IReadOnlyCollection<IItem> PlantOrigins { get; }
		IReadOnlyCollection<IItem> SpoilOrigins { get; }

		double GetItemSpoilageTime(IQuality quality); //seconds

		IReadOnlyCollection<IEntityObjectBase> FuelsEntities { get; }

		//spoil ticks are ignored - its assumed that if there is a plant/spoil result then the ticks are at least low enough to make it viable on a world basis
	}

	public class ItemPrototype : DataObjectBasePrototype, IItem {
		public class Test { }

		public ISubgroup MySubgroup { get { return mySubgroup; } }

		public IReadOnlyCollection<IRecipe> ProductionRecipes { get { return ProductionRecipesInternal; } }
		public IReadOnlyCollection<IRecipe> ConsumptionRecipes { get { return ConsumptionRecipesInternal; } }
		public IReadOnlyCollection<ITechnology> ConsumptionTechnologies { get { return ConsumptionTechnologiesInternal; } }

		public bool IsMissing { get; private set; }

		public int StackSize { get; set; }

		public double Weight { get; set; }
		public double IngredientToWeightCoefficient { get; set; }
		public double FuelValue { get; internal set; }
		public double PollutionMultiplier { get; internal set; }

		public IItem? BurnResult { get; internal set; }
		public IPlantProcess? PlantResult { get; internal set; }
		public IItem? SpoilResult { get; internal set; }

		public IItem? FuelOrigin { get; internal set; }
		public IReadOnlyCollection<IItem> PlantOrigins { get { return PlantOriginsInternal; } }
		public IReadOnlyCollection<IItem> SpoilOrigins { get { return SpoilOriginsInternal; } }

		public IReadOnlyCollection<IEntityObjectBase> FuelsEntities { get { return FuelsEntitiesInternal; } }

		public double GetItemSpoilageTime(IQuality quality) { return SpoilageTimes.TryGetValue(quality, out double value) ? value : 1; }

		internal SubgroupPrototype mySubgroup;

		internal HashSet<RecipePrototype> ProductionRecipesInternal { get; private set; }
		internal HashSet<RecipePrototype> ConsumptionRecipesInternal { get; private set; }
		internal HashSet<TechnologyPrototype> ConsumptionTechnologiesInternal { get; private set; }
		internal HashSet<EntityObjectBasePrototype> FuelsEntitiesInternal { get; private set; }
		internal HashSet<ItemPrototype> PlantOriginsInternal { get; private set; }
		internal HashSet<ItemPrototype> SpoilOriginsInternal { get; private set; }

		internal Dictionary<IQuality, double> SpoilageTimes { get; private set; }

		public ItemPrototype(DCache dCache, string name, string friendlyName, SubgroupPrototype subgroup, string order, bool isMissing = false) : base(dCache, name, friendlyName, order) {
			mySubgroup = subgroup;
			subgroup.items.Add(this);

			StackSize = 1;

			ProductionRecipesInternal = [];
			ConsumptionRecipesInternal = [];
			ConsumptionTechnologiesInternal = [];
			FuelsEntitiesInternal = [];
			PlantOriginsInternal = [];
			SpoilOriginsInternal = [];
			SpoilageTimes = [];

			Weight = 0.01f;
			IngredientToWeightCoefficient = 1f;
			FuelValue = 1f; //useful for preventing overlow issues when using missing items / non-fuel items (loading with wrong mods / importing from alt mod group can cause this)
			PollutionMultiplier = 1f;
			IsMissing = isMissing;
		}

		public override string ToString() { return string.Format("Item: {0}", Name); }
	}
}
