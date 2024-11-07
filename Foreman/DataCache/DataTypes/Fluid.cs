namespace Foreman.DataCache.DataTypes {
	public interface IFluid : IItem {
		bool IsTemperatureDependent { get; }
		double DefaultTemperature { get; }
		double SpecificHeatCapacity { get; }
		double GasTemperature { get; }
		double MaxTemperature { get; }

		string GetTemperatureRangeFriendlyName(FRange tempRange);
		string GetTemperatureFriendlyName(double temperature);
	}

	public class FluidPrototype(DCache dCache, string name, string friendlyName, SubgroupPrototype subgroup, string order, bool isMissing = false) : ItemPrototype(dCache, name, friendlyName, subgroup, order, isMissing), IFluid {
		public bool IsTemperatureDependent { get; internal set; } = false;
		public double DefaultTemperature { get; internal set; } = 0;
		public double SpecificHeatCapacity { get; internal set; } = 0;
		public double GasTemperature { get; internal set; } = 0;
		public double MaxTemperature { get; internal set; } = 0;

		public string GetTemperatureRangeFriendlyName(FRange tempRange) {
			if (tempRange.Ignore)
				return FriendlyName;

			string name = FriendlyName;
			bool includeMin = tempRange.Min >= double.MinValue;
			bool includeMax = tempRange.Max <= double.MaxValue;

			if (tempRange.Min == tempRange.Max)
				name += string.Format(" ({0}°c)", tempRange.Min.ToString("0"));
			else if (includeMin && includeMax)
				name += string.Format(" ({0}-{1}°c)", tempRange.Min.ToString("0"), tempRange.Max.ToString("0"));
			else if (includeMin)
				name += string.Format(" (min {0}°c)", tempRange.Min.ToString("0"));
			else if (includeMax)
				name += string.Format(" (max {0}°c)", tempRange.Max.ToString("0"));
			else
				name += "(any°)";

			return name;
		}

		public string GetTemperatureFriendlyName(double temperature) {
			return string.Format("{0} ({1}°c)", FriendlyName, temperature.ToString("0"));
		}


		public override string ToString() { return string.Format("Item: {0}", Name); }
	}
}
