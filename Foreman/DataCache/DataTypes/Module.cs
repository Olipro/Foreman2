using System;
using System.Linq;
using System.Collections.Generic;

namespace Foreman.DataCache.DataTypes {
	public interface IModule : IDataObjectBase {
		IReadOnlyCollection<IRecipe> Recipes { get; }
		IReadOnlyCollection<IAssembler> Assemblers { get; }
		IReadOnlyCollection<IBeacon> Beacons { get; }
		IReadOnlyCollection<IRecipe>? AvailableRecipes { get; }

		IItem AssociatedItem { get; }

		double GetSpeedBonus(IQuality quality);
		double GetProductivityBonus(IQuality quality);
		double GetConsumptionBonus(IQuality quality);
		double GetPolutionBonus(IQuality quality);
		double GetQualityBonus(IQuality quality);

		double GetSpeedBonus(int qualityLevel = 0);
		double GetProductivityBonus(int qualityLevel = 0);
		double GetConsumptionBonus(int qualityLevel = 0);
		double GetPolutionBonus(int qualityLevel = 0);
		double GetQualityBonus(int qualityLevel = 0);

		string Category { get; }

		int Tier { get; }

		bool IsMissing { get; }
	}

	public class ModulePrototype : DataObjectBasePrototype, IModule {
		public IReadOnlyCollection<IRecipe> Recipes { get { return RecipesInternal; } }
		public IReadOnlyCollection<IAssembler> Assemblers { get { return AssemblersInternal; } }
		public IReadOnlyCollection<IBeacon> Beacons { get { return BeaconsInternal; } }
		public IReadOnlyCollection<IRecipe>? AvailableRecipes { get; private set; }
		public IItem AssociatedItem { get { return Owner.Items[Name]; } }

		public double GetSpeedBonus(IQuality quality) { return GetSpeedBonus(quality.Level); }
		public double GetProductivityBonus(IQuality quality) { return GetProductivityBonus(quality.Level); }
		public double GetConsumptionBonus(IQuality quality) { return GetConsumptionBonus(quality.Level); }
		public double GetPolutionBonus(IQuality quality) { return GetPolutionBonus(quality.Level); }
		public double GetQualityBonus(IQuality quality) { return GetQualityBonus(quality.Level); }

		public double GetSpeedBonus(int qualityLevel = 0) { return SpeedBonus <= 0 || qualityLevel == 0 ? SpeedBonus : Math.Truncate(SpeedBonus * (1 + qualityLevel * 0.3) * 100) / 100; }
		public double GetProductivityBonus(int qualityLevel = 0) { return ProductivityBonus <= 0 || qualityLevel == 0 ? ProductivityBonus : Math.Truncate(ProductivityBonus * (1 + qualityLevel * 0.3) * 100) / 100; }
		public double GetConsumptionBonus(int qualityLevel = 0) { return ConsumptionBonus >= 0 || qualityLevel == 0 ? ConsumptionBonus : Math.Truncate(ConsumptionBonus * (1 + qualityLevel * 0.3) * 100) / 100; }
		public double GetPolutionBonus(int qualityLevel = 0) { return PollutionBonus >= 0 || qualityLevel == 0 ? PollutionBonus : Math.Truncate(PollutionBonus * (1 + qualityLevel * 0.3) * 100) / 100; }
		public double GetQualityBonus(int qualityLevel = 0) { return QualityBonus <= 0 || qualityLevel == 0 ? QualityBonus : Math.Truncate(QualityBonus * (1 + qualityLevel * 0.3) * 100) / 100; }

		public double SpeedBonus { get; internal set; }
		public double ProductivityBonus { get; internal set; }
		public double ConsumptionBonus { get; internal set; }
		public double PollutionBonus { get; internal set; }
		public double QualityBonus { get; internal set; }

		public string Category { get; internal set; }

		public int Tier { get; set; }

		public bool IsMissing { get; private set; }
		public override bool Available { get { return AssociatedItem.Available; } set { } }

		internal HashSet<RecipePrototype> RecipesInternal { get; private set; }
		internal HashSet<AssemblerPrototype> AssemblersInternal { get; private set; }
		internal HashSet<BeaconPrototype> BeaconsInternal { get; private set; }

		public ModulePrototype(DCache dCache, string name, string friendlyName, bool isMissing = false) : base(dCache, name, friendlyName, "-") {
			Enabled = true;
			IsMissing = isMissing;

			SpeedBonus = 0;
			ProductivityBonus = 0;
			ConsumptionBonus = 0;
			PollutionBonus = 0;
			QualityBonus = 0;

			Category = "";

			RecipesInternal = [];
			AssemblersInternal = [];
			BeaconsInternal = [];
		}

		internal void UpdateAvailabilities() {
			AvailableRecipes = new HashSet<IRecipe>(RecipesInternal.Where(r => r.Enabled));
		}

		public override string ToString() { return string.Format("Module: {0}", Name); }
	}
}
