using System;
using System.Drawing;
using System.Linq;

namespace Foreman.DataCache.DataTypes {
	public interface IDataObjectBase : IComparable<IDataObjectBase> {
		DCache Owner { get; }

		string Name { get; }
		string LFriendlyName { get; }
		string FriendlyName { get; }

		bool Available { get; }
		bool Enabled { get; set; }

		Bitmap Icon { get; }
		Color AverageColor { get; }
		void SetIconAndColor(IconColorPair icp);
	}

	public abstract class DataObjectBasePrototype(DCache dCache, string name, string friendlyName, string order) : IDataObjectBase {
		private static readonly char[] orderSeparators = ['[', ']'];

		public DCache Owner { get; private set; } = dCache;

		public string Name { get; private set; } = name;
		public string LFriendlyName { get; private set; } = friendlyName.ToLower();
		public string FriendlyName { get; private set; } = friendlyName;

		public virtual bool Available { get; set; } = true;
		public bool Enabled { get; set; } = true;

		private readonly string[] OrderCompareArray = order.Split(orderSeparators).Where(s => !string.IsNullOrEmpty(s)).ToArray();

		public void SetIconAndColor(IconColorPair icp) {
			if (icp.Icon != null)
				Icon = icp.Icon;
			else
				Icon = IconCache.GetUnknownIcon();

			AverageColor = icp.Color;
		}

		public Color AverageColor { get; private set; } = Color.Black;
		public Bitmap Icon { get; private set; } = IconCache.GetUnknownIcon();

		public override bool Equals(object? obj) {
			return obj is DataObjectBasePrototype dobp && dobp == this;
		}

		public static bool operator==(DataObjectBasePrototype doBase1, DataObjectBasePrototype doBase2) {
			if (ReferenceEquals(doBase1, doBase2))
				return true;
			if (doBase1 is null || doBase2 is null || doBase1.GetType() != doBase2.GetType())
				return false;
			return doBase1.Name == doBase2.Name;
		}

		public static bool operator !=(DataObjectBasePrototype recipe1, DataObjectBasePrototype recipe2) {
			return !(recipe1 == recipe2);
		}

		public override int GetHashCode() => Name.GetHashCode();
		public int CompareTo(IDataObjectBase? other) {
			if (other is DataObjectBasePrototype otherP) {

				//order comparison is apparently quite convoluted - any time we have brackets ([ or ]), it signifies a different order part.
				//each part is compared char-by-char, and in the case of the longer string it goes first.
				//in terms of sections, the sorter section goes first (ex: a[0] goes before a[0]-1)
				for (int i = 0; i < OrderCompareArray.Length && i < otherP.OrderCompareArray.Length; i++) {
					for (int j = 0; j < OrderCompareArray[i].Length && j < otherP.OrderCompareArray[i].Length; j++) {
						int result = OrderCompareArray[i][j].CompareTo(otherP.OrderCompareArray[i][j]);
						if (result != 0)
							return result;
					}
					if (OrderCompareArray[i].Length != otherP.OrderCompareArray[i].Length)
						return OrderCompareArray[i].Length > otherP.OrderCompareArray[i].Length ? -1 : 1;
				}
				if (OrderCompareArray.Length != otherP.OrderCompareArray.Length)
					return OrderCompareArray.Length < otherP.OrderCompareArray.Length ? -1 : 1;

				return LFriendlyName.CompareTo(otherP.LFriendlyName);
			}
			return 0;
		}
	}
}
