using Newtonsoft.Json;

using System;
using System.Collections.Generic;

namespace Foreman.DataCache.DataTypes {
	public interface IPlantProcess : IDataObjectBase {

		double GrowTime { get; } //seconds
		long PlantID { get; }
		[JsonProperty("isMissing")]
		bool IsMissing { get; }

		IReadOnlyDictionary<IItem, double> ProductSet { get; }
		IReadOnlyList<IItem> ProductList { get; }

		IItem? Seed { get; }

		public virtual int GetHashCode() {
			return HashCode.Combine(PlantID, GrowTime, ProductSet, ProductList, Seed);
		}
	}

	public class PlantProcessPrototype : DataObjectBasePrototype, IPlantProcess {
		public double GrowTime { get; internal set; }

		public IReadOnlyDictionary<IItem, double> ProductSet => ProductSetInternal;
		public IReadOnlyList<IItem> ProductList => ProductListInternal;

		public IItem? Seed { get; internal set; }

		internal Dictionary<IItem, double> ProductSetInternal { get; private set; }
		internal List<ItemPrototype> ProductListInternal { get; private set; }

		internal HashSet<TechnologyPrototype>? MyUnlockTechnologies { get; private set; }

		[JsonProperty("isMissing")]
		public bool IsMissing { get; private set; }

		private static long lastPlantID = 0;
		public long PlantID { get; private set; }

		public PlantProcessPrototype(DCache dCache, string name, bool isMissing = false) : base(dCache, name, name, "-") {
			PlantID = lastPlantID++;

			GrowTime = 0.5f;
			Enabled = true;
			IsMissing = isMissing;

			ProductSetInternal = [];
			ProductListInternal = [];
		}

		public void InternalOneWayAddProduct(ItemPrototype item, double quantity) {
			if (ProductSetInternal.ContainsKey(item)) {
				ProductSetInternal[item] += quantity;
			} else {
				ProductSetInternal.Add(item, quantity);
				ProductListInternal.Add(item);
			}
		}

		internal void InternalOneWayDeleteProduct(ItemPrototype item) //only from delete calls
		{
			ProductSetInternal.Remove(item);
			ProductListInternal.Remove(item);
		}

		public override string ToString() { return string.Format("Planting process: {0} Id:{1}", Name, PlantID); }
	}

	public class PlantNaInPrComparer : IEqualityComparer<IPlantProcess> //compares by name, ingredient names, and product names (but not exact values!)
	{
		public bool Equals(IPlantProcess? x, IPlantProcess? y) {
			if (x == y)
				return true;
			if (x is null || y is null || x.Name != y.Name || x.ProductList.Count != y.ProductList.Count || x.Seed != y.Seed)
				return false;

			foreach (IItem i in x.ProductList)
				if (!y.ProductSet.ContainsKey(i))
					return false;

			return true;
		}

		public int GetHashCode(IPlantProcess obj) {
			return obj.GetHashCode();
		}
	}
}
