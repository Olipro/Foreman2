using Newtonsoft.Json;

using System.Collections.Generic;

namespace Foreman.DataCache.DataTypes {
	public interface IRecipe : IDataObjectBase {
		ISubgroup MySubgroup { get; }

		double Time { get; }
		long RecipeID { get; }
		[JsonProperty("isMissing")]
		bool IsMissing { get; }

		bool HasProductivityResearch { get; }

		bool AllowConsumptionBonus { get; }
		bool AllowSpeedBonus { get; }
		bool AllowProductivityBonus { get; }
		bool AllowPollutionBonus { get; }
		bool AllowQualityBonus { get; }

		double MaxProductivityBonus { get; }

		IReadOnlyDictionary<IItem, double> ProductSet { get; }
		IReadOnlyDictionary<IItem, double> ProductPSet { get; } //extra productivity amounts [ actual amount = productSet + (productPSet * productivity bonus) ]
		IReadOnlyList<IItem> ProductList { get; }
		IReadOnlyDictionary<IItem, double> ProductTemperatureMap { get; }

		IReadOnlyDictionary<IItem, double> IngredientSet { get; }
		IReadOnlyList<IItem> IngredientList { get; }
		IReadOnlyDictionary<IItem, FRange> IngredientTemperatureMap { get; }

		IReadOnlyCollection<IAssembler> Assemblers { get; }
		IReadOnlyCollection<IModule> AssemblerModules { get; }
		IReadOnlyCollection<IModule> BeaconModules { get; }

		IReadOnlyCollection<ITechnology> MyUnlockTechnologies { get; }
		IReadOnlyList<IReadOnlyList<IItem>> MyUnlockSciencePacks { get; }

		string GetIngredientFriendlyName(IItem item);
		string GetProductFriendlyName(IItem item);
		bool TestIngredientConnection(IRecipe provider, IItem ingredient);

		//trash items (spoiled items from spoiling of items already inside assembler) are ignored
		//planet conditions are ignored
	}

	public class RecipePrototype : DataObjectBasePrototype, IRecipe {
		public ISubgroup MySubgroup { get { return mySubgroup; } }

		public double Time { get; internal set; }

		public IReadOnlyDictionary<IItem, double> ProductSet { get { return ProductSetInternal; } }
		public IReadOnlyDictionary<IItem, double> ProductPSet { get { return ProductPSetInternal; } }
		public IReadOnlyList<IItem> ProductList { get { return ProductListInternal; } }
		public IReadOnlyDictionary<IItem, double> ProductTemperatureMap { get { return ProductTemperatureMapInternal; } }

		public IReadOnlyDictionary<IItem, double> IngredientSet { get { return IngredientSetInternal; } }
		public IReadOnlyList<IItem> IngredientList { get { return IngredientListInternal; } }
		public IReadOnlyDictionary<IItem, FRange> IngredientTemperatureMap { get { return IngredientTemperatureMapInternal; } }

		public IReadOnlyCollection<IAssembler> Assemblers { get { return AssemblersInternal; } }
		public IReadOnlyCollection<IModule> AssemblerModules { get { return AssemblerModulesInternal; } }
		public IReadOnlyCollection<IModule> BeaconModules { get { return BeaconModulesInternal; } }

		public IReadOnlyCollection<ITechnology> MyUnlockTechnologies { get { return MyUnlockTechnologiesInternal; } }
		public IReadOnlyList<IReadOnlyList<IItem>> MyUnlockSciencePacks { get; set; }

		internal SubgroupPrototype mySubgroup;

		internal Dictionary<IItem, double> ProductSetInternal { get; private set; }
		internal Dictionary<IItem, double> ProductPSetInternal { get; private set; }
		internal Dictionary<IItem, double> ProductTemperatureMapInternal { get; private set; }
		internal List<ItemPrototype> ProductListInternal { get; private set; }

		internal Dictionary<IItem, double> IngredientSetInternal { get; private set; }
		internal Dictionary<IItem, FRange> IngredientTemperatureMapInternal { get; private set; }
		internal List<ItemPrototype> IngredientListInternal { get; private set; }

		internal HashSet<AssemblerPrototype> AssemblersInternal { get; private set; }
		internal HashSet<ModulePrototype> AssemblerModulesInternal { get; private set; }
		internal HashSet<ModulePrototype> BeaconModulesInternal { get; private set; }

		internal HashSet<TechnologyPrototype> MyUnlockTechnologiesInternal { get; private set; }

		public bool IsMissing { get; private set; }

		public bool AllowConsumptionBonus { get; internal set; }
		public bool AllowSpeedBonus { get; internal set; }
		public bool AllowProductivityBonus { get; internal set; }
		public bool AllowPollutionBonus { get; internal set; }
		public bool AllowQualityBonus { get; internal set; }

		public bool HasProductivityResearch { get; internal set; }

		public double MaxProductivityBonus { get; internal set; }

		private static long lastRecipeID = 0;
		public long RecipeID { get; private set; }

		internal bool HideFromPlayerCrafting { get; set; }

		public RecipePrototype(DCache dCache, string name, string friendlyName, SubgroupPrototype subgroup, string order, bool isMissing = false) : base(dCache, name, friendlyName, order) {
			RecipeID = lastRecipeID++;

			mySubgroup = subgroup;
			subgroup.recipes.Add(this);

			Time = 0.5f;
			Enabled = true;
			IsMissing = isMissing;
			HideFromPlayerCrafting = false;
			AllowConsumptionBonus = true;
			AllowSpeedBonus = true;
			AllowProductivityBonus = true;
			AllowPollutionBonus = true;
			AllowQualityBonus = true;
			MaxProductivityBonus = 1000;
			HasProductivityResearch = false;

			IngredientSetInternal = [];
			IngredientListInternal = [];
			IngredientTemperatureMapInternal = [];

			ProductSetInternal = [];
			ProductListInternal = [];
			ProductTemperatureMapInternal = [];
			ProductPSetInternal = [];

			AssemblersInternal = [];
			AssemblerModulesInternal = [];
			BeaconModulesInternal = [];
			MyUnlockTechnologiesInternal = [];
			MyUnlockSciencePacks = new List<List<IItem>>();
		}

		public string GetIngredientFriendlyName(IItem item) {
			if (IngredientSet.ContainsKey(item) && item is IFluid fluid && fluid.IsTemperatureDependent)
				return fluid.GetTemperatureRangeFriendlyName(IngredientTemperatureMap[item]);
			return item.FriendlyName;
		}

		public string GetProductFriendlyName(IItem item) {
			if (ProductSetInternal.ContainsKey(item) && item is IFluid fluid && (fluid.IsTemperatureDependent || fluid.DefaultTemperature != ProductTemperatureMap[item]))
				return fluid.GetTemperatureFriendlyName(ProductTemperatureMapInternal[item]);
			return item.FriendlyName;
		}

		public bool TestIngredientConnection(IRecipe provider, IItem ingredient) //checks if the temperature that the ingredient is coming out at fits within the range of temperatures required for this recipe
		{
			if (!IngredientSet.ContainsKey(ingredient) || !provider.ProductSet.ContainsKey(ingredient))
				return false;

			return IngredientTemperatureMap[ingredient].Contains(provider.ProductTemperatureMap[ingredient]);
		}

		public void InternalOneWayAddIngredient(ItemPrototype item, double quantity, double minTemp = double.NaN, double maxTemp = double.NaN) {
			if (IngredientSet.ContainsKey(item))
				IngredientSetInternal[item] += quantity;
			else {
				IngredientSetInternal.Add(item, quantity);
				IngredientListInternal.Add(item);

				minTemp = item is IFluid && double.IsNaN(minTemp) ? double.NegativeInfinity : minTemp;
				maxTemp = item is IFluid && double.IsNaN(maxTemp) ? double.PositiveInfinity : maxTemp;
				IngredientTemperatureMapInternal.Add(item, new FRange(minTemp, maxTemp));
			}
		}

		internal void InternalOneWayDeleteIngredient(ItemPrototype item) //only from delete calls
		{
			IngredientSetInternal.Remove(item);
			IngredientListInternal.Remove(item);
			IngredientTemperatureMapInternal.Remove(item);
		}

		public void InternalOneWayAddProduct(ItemPrototype item, double quantity, double pquantity, double temperature = double.NaN) {
			if (ProductSetInternal.ContainsKey(item)) {
				ProductSetInternal[item] += quantity;
				ProductPSetInternal[item] += pquantity;
			} else {
				ProductSetInternal.Add(item, quantity);
				ProductPSetInternal.Add(item, pquantity);
				ProductListInternal.Add(item);

				temperature = item is IFluid fluid && double.IsNaN(temperature) ? fluid.DefaultTemperature : temperature;
				ProductTemperatureMapInternal.Add(item, temperature);
			}
		}

		internal void InternalOneWayDeleteProduct(ItemPrototype item) //only from delete calls
		{
			ProductSetInternal.Remove(item);
			ProductPSetInternal.Remove(item);
			ProductListInternal.Remove(item);
			ProductTemperatureMapInternal.Remove(item);
		}

		public override string ToString() { return string.Format("Recipe: {0} Id:{1}", Name, RecipeID); }
	}

	public class RecipeNaInPrComparer : IEqualityComparer<IRecipe> //compares by name, ingredient names, and product names
	{
		public bool Equals(IRecipe? x, IRecipe? y) {
			if (x == y)
				return true;

			if (x is null || y is null || x.Name != y.Name)
				return false;
			if (x.IngredientList.Count != y.IngredientList.Count)
				return false;
			if (x.ProductList.Count != y.ProductList.Count)
				return false;

			foreach (IItem i in x.IngredientList)
				if (!y.IngredientSet.ContainsKey(i))
					return false;
			foreach (IItem i in x.ProductList)
				if (!y.ProductSet.ContainsKey(i))
					return false;

			return true;
		}

		public int GetHashCode(IRecipe obj) {
			return obj.GetHashCode();
		}
	}

	public struct FRange(double min, double max, bool ignore = false) {
		//NOTE: there is no check for min to be guaranteed to be less than max, and this is BY DESIGN
		//this means that if your range is for example from 10 to 8, (and it isnt ignored), ANY call to Contains methods will return false
		//ex: 2 recipes, one requiring fluid 0->10 degrees, other requiring fluid 20->30 degrees. A proper summation of ranges will result in a vaild range of 20->10 degrees to satisfy both recipes, aka: NO TEMP WILL SATISFY!
		public double Min = min;
		public double Max = max;
		public bool Ignore = ignore;

		public readonly bool Contains(double value) { return Ignore || double.IsNaN(value) || (double.IsNaN(Min) || value >= Min) && (double.IsNaN(Max) || value <= Max); }
		public readonly bool Contains(FRange range) { return Ignore || range.Ignore || (double.IsNaN(Min) || double.IsNaN(range.Min) || Min <= range.Min) && (double.IsNaN(Max) || double.IsNaN(range.Max) || Max >= range.Max); }
		public readonly bool IsPoint() { return Ignore || Min == Max; } //true if the range is a single point (min is max, and we arent ignoring it)
	}
}
