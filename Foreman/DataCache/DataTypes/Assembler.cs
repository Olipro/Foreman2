using System.Collections.Generic;

namespace Foreman.DataCache.DataTypes {
	public interface IAssembler : IEntityObjectBase {
		IReadOnlyCollection<IRecipe> Recipes { get; }
		double BaseSpeedBonus { get; }
		double BaseProductivityBonus { get; }
		double BaseConsumptionBonus { get; }
		double BasePollutionBonus { get; }
		double BaseQualityBonus { get; }

		bool AllowBeacons { get; }
		bool AllowModules { get; }
	}

	internal class AssemblerPrototype(DCache dCache, string name, string friendlyName, EntityType type, EnergySource source, bool isMissing = false) : EntityObjectBasePrototype(dCache, name, friendlyName, type, source, isMissing), IAssembler {
		public IReadOnlyCollection<IRecipe> Recipes => RecipesInternal;
		public double BaseSpeedBonus { get; set; } = 0;
		public double BaseProductivityBonus { get; set; } = 0;
		public double BaseConsumptionBonus { get; set; } = 0;
		public double BasePollutionBonus { get; set; } = 0;
		public double BaseQualityBonus { get; set; } = 0;

		public bool AllowBeacons { get; internal set; } = false; //assumed to be default? no info in LUA
		public bool AllowModules { get; internal set; } = false; //assumed to be default? no info in LUA

		internal HashSet<RecipePrototype> RecipesInternal { get; private set; } = [];

		public override string ToString() {
			return string.Format("Assembler: {0}", Name);
		}
	}
}
