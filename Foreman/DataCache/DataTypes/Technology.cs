using System.Collections.Generic;

namespace Foreman.DataCache.DataTypes {
	public interface ITechnology : IDataObjectBase {
		IReadOnlyCollection<ITechnology> Prerequisites { get; }
		IReadOnlyCollection<ITechnology> PostTechs { get; }
		IReadOnlyCollection<IRecipe> UnlockedRecipes { get; }
		IReadOnlyCollection<IQuality> UnlockedQualities { get; }
		IReadOnlyDictionary<IItem, double> SciPackSet { get; }
		IReadOnlyList<IItem> SciPackList { get; }
		double ResearchCost { get; }
		int Tier { get; } //furthest distance from this tech to the starting tech. nice way or ordering technologies
	}

	public class TechnologyPrototype(DCache dCache, string name, string friendlyName) : DataObjectBasePrototype(dCache, name, friendlyName, "-"), ITechnology {
		public IReadOnlyCollection<ITechnology> Prerequisites { get { return PrerequisitesInternal; } }
		public IReadOnlyCollection<ITechnology> PostTechs { get { return PostTechsInternal; } }
		public IReadOnlyCollection<IRecipe> UnlockedRecipes { get { return UnlockedRecipesInternal; } }
		public IReadOnlyCollection<IQuality> UnlockedQualities { get { return UnlockedQualitiesInternal; } }
		public IReadOnlyDictionary<IItem, double> SciPackSet { get { return SciPackSetInternal; } }
		public IReadOnlyList<IItem> SciPackList { get { return SciPackListInternal; } }
		public double ResearchCost { get; set; } = 0;
		public int Tier { get; set; }

		internal HashSet<TechnologyPrototype> PrerequisitesInternal { get; private set; } = [];
		internal HashSet<TechnologyPrototype> PostTechsInternal { get; private set; } = [];
		internal HashSet<RecipePrototype> UnlockedRecipesInternal { get; private set; } = [];
		internal HashSet<QualityPrototype> UnlockedQualitiesInternal { get; private set; } = [];
		internal Dictionary<IItem, double> SciPackSetInternal { get; private set; } = [];
		internal List<IItem> SciPackListInternal { get; private set; } = [];

		public void InternalOneWayAddSciPack(ItemPrototype pack, double quantity) {
			if (SciPackSetInternal.ContainsKey(pack))
				SciPackSetInternal[pack] += quantity;
			else {
				SciPackSetInternal.Add(pack, quantity);
				SciPackListInternal.Add(pack);
			}
		}

		public override int GetHashCode() {
			return Name.GetHashCode();
		}

		public override bool Equals(object? obj) => obj is TechnologyPrototype t && t == this;

		public static bool operator ==(TechnologyPrototype? item1, TechnologyPrototype? item2) {
			return ReferenceEquals(item1, item2) || (item1 != null && item2 != null && item1.Name == item2.Name);
		}

		public static bool operator !=(TechnologyPrototype? item1, TechnologyPrototype? item2) {
			return !(item1 == item2);
		}

		public override string ToString() {
			return string.Format("Technology: {0}", Name);
		}

	}
}
