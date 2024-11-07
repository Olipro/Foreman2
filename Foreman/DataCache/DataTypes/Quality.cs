using Newtonsoft.Json;

using System.Collections.Generic;

namespace Foreman.DataCache.DataTypes {
	public interface IQuality : IDataObjectBase {
		IQuality? NextQuality { get; }
		IQuality? PrevQuality { get; }
		double NextProbability { get; }

		[JsonProperty("isMissing")]
		bool IsMissing { get; }

		int Level { get; } //'power' of the quality
		double BeaconPowerMultiplier { get; }
		double MiningDrillResourceDrainMultiplier { get; }

		IReadOnlyCollection<ITechnology> MyUnlockTechnologies { get; }
		IReadOnlyList<IReadOnlyList<IItem>> MyUnlockSciencePacks { get; }
	}

	public class QualityPrototype : DataObjectBasePrototype, IQuality {
		public IQuality? NextQuality { get; internal set; }
		public IQuality? PrevQuality { get; internal set; }
		public double NextProbability { get; set; }

		public bool IsMissing { get; private set; }

		public int Level { get; internal set; }
		public double BeaconPowerMultiplier { get; set; }
		public double MiningDrillResourceDrainMultiplier { get; set; }

		public IReadOnlyCollection<ITechnology> MyUnlockTechnologies => MyUnlockTechnologiesInternal;
		public IReadOnlyList<IReadOnlyList<IItem>> MyUnlockSciencePacks { get; set; }

		internal HashSet<TechnologyPrototype> MyUnlockTechnologiesInternal { get; private set; }

		public QualityPrototype(DCache dCache, string name, string friendlyName, string order, bool isMissing = false) : base(dCache, name, friendlyName, order) {
			Enabled = true;
			IsMissing = isMissing;

			NextProbability = 0;
			NextQuality = null;
			PrevQuality = null;

			Level = 0;
			BeaconPowerMultiplier = 1;
			MiningDrillResourceDrainMultiplier = 1;

			MyUnlockTechnologiesInternal = [];
			MyUnlockSciencePacks = new List<List<IItem>>();
		}

		public override string ToString() { return string.Format("Quality T{0}: {1}", Level, Name); }
	}
}
