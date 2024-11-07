using Foreman.DataCache;
using Foreman.DataCache.DataTypes;

using Newtonsoft.Json;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Foreman.Models {
	public readonly struct ItemQualityPair : IEquatable<ItemQualityPair> {
		public readonly IItem Item;
		public readonly IQuality Quality;

		public static readonly ItemQualityPair Default = new();

		public ItemQualityPair(IItem item, IQuality quality) {
			Item = item;
			Quality = quality;

			if (Item == null || Quality == null)
				throw new NullReferenceException("null error - Item: " + nameof(Item) + " Quality: " + nameof(Quality));
		}

		public override bool Equals(object? obj) => obj is ItemQualityPair other && Equals(other);
		public bool Equals(ItemQualityPair other) => Item == other.Item && Quality == other.Quality;
		public override int GetHashCode() => HashCode.Combine(Item, Quality);
		public static bool operator ==(ItemQualityPair lhs, ItemQualityPair rhs) => lhs.Equals(rhs);
		public static bool operator !=(ItemQualityPair lhs, ItemQualityPair rhs) => !(lhs == rhs);
		public static implicit operator bool(ItemQualityPair bp) => bp.Item != null && bp.Quality != null;
		public override string ToString() => Item?.ToString() + " (" + Quality?.ToString() + ")";

		public string FriendlyName {
			get {
				if (Quality == Quality?.Owner.DefaultQuality)
					return Item?.FriendlyName + "";
				else
					return Item?.FriendlyName + " (" + Quality?.FriendlyName + ")";
			}
		}
		public Bitmap? Icon {
			get {
				if (Item == null)
					return null;
				return (Quality == Quality?.Owner.DefaultQuality || Quality is null) ? Item.Icon : IconCacheProcessor.CombinedQualityIcon(Item.Icon, Quality.Icon);
			}
		}
	}

	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	public readonly struct ModuleQualityPair {
		public readonly IModule Module;
		public readonly IQuality Quality;

		[JsonProperty]
		public readonly string Name => Module.Name;
		[JsonProperty(nameof(Quality))]
		public readonly string StrQuality => Quality.Name;

		public ModuleQualityPair(IModule module, IQuality quality) {
			Module = module;
			Quality = quality;
		}

		public override bool Equals(object? obj) => obj is ModuleQualityPair other && Equals(other);
		public bool Equals(ModuleQualityPair other) => Module == other.Module && Quality == other.Quality;
		public override int GetHashCode() => HashCode.Combine(Module, Quality);
		public static bool operator ==(ModuleQualityPair lhs, ModuleQualityPair rhs) => lhs.Equals(rhs);
		public static bool operator !=(ModuleQualityPair lhs, ModuleQualityPair rhs) => !(lhs == rhs);
		public static implicit operator bool(ModuleQualityPair bp) => bp.Module != null && bp.Quality != null;
		public override string ToString() { return Module.ToString() + " (" + Quality.ToString() + ")"; }

		public string FriendlyName {
			get {
				if (Quality == Quality.Owner.DefaultQuality)
					return Module.FriendlyName;
				else
					return Module.FriendlyName + " (" + Quality.FriendlyName + ")";
			}
		}
		public Bitmap? Icon {
			get {
				if (Module == null)
					return null;
				return Quality == Quality.Owner.DefaultQuality ? Module.Icon : IconCacheProcessor.CombinedQualityIcon(Module.Icon, Quality.Icon);
			}
		}
	}

	public readonly struct AssemblerQualityPair(IAssembler assembler, IQuality quality) {
		public readonly IAssembler Assembler = assembler;
		public readonly IQuality Quality = quality;

		public override bool Equals(object? obj) => obj is AssemblerQualityPair other && Equals(other);
		public bool Equals(AssemblerQualityPair other) => Assembler == other.Assembler && Quality == other.Quality;
		public override int GetHashCode() => HashCode.Combine(Assembler, Quality);
		public static bool operator ==(AssemblerQualityPair lhs, AssemblerQualityPair rhs) => lhs.Equals(rhs);
		public static bool operator !=(AssemblerQualityPair lhs, AssemblerQualityPair rhs) => !(lhs == rhs);
		public static implicit operator bool(AssemblerQualityPair bp) => bp.Assembler != null && bp.Quality != null;
		public override string ToString() { return Assembler?.ToString() + " (" + Quality?.ToString() + ")"; }

		public string FriendlyName {
			get {
				if (Quality == Quality?.Owner.DefaultQuality)
					return Assembler?.FriendlyName + "";
				else
					return Assembler?.FriendlyName + " (" + Quality?.FriendlyName + ")";
			}
		}
		public Bitmap? Icon {
			get {
				if (Assembler == null)
					return null;
				return Quality == Quality?.Owner.DefaultQuality || Quality is null ? Assembler.Icon : IconCacheProcessor.CombinedQualityIcon(Assembler.Icon, Quality.Icon);
			}
		}
	}

	public readonly struct BeaconQualityPair(IBeacon beacon, IQuality quality) {
		public readonly IBeacon Beacon = beacon;
		public readonly IQuality Quality = quality;

		public override bool Equals(object? obj) => obj is BeaconQualityPair other && Equals(other);
		public bool Equals(BeaconQualityPair other) => Beacon == other.Beacon && Quality == other.Quality;
		public override int GetHashCode() => HashCode.Combine(Beacon, Quality);
		public static bool operator ==(BeaconQualityPair lhs, BeaconQualityPair rhs) => lhs.Equals(rhs);
		public static bool operator !=(BeaconQualityPair lhs, BeaconQualityPair rhs) => !(lhs == rhs);
		public static implicit operator bool(BeaconQualityPair bp) => bp.Beacon != null && bp.Quality != null;
		public override string ToString() => Beacon?.ToString() + " (" + Quality?.ToString() + ")";

		public string FriendlyName {
			get {
				if (Quality == Quality?.Owner.DefaultQuality)
					return Beacon?.FriendlyName + "";
				else
					return Beacon?.FriendlyName + " (" + Quality?.FriendlyName + ")";
			}
		}
		public Bitmap Icon {
			get {
				return Quality == Quality?.Owner.DefaultQuality || Quality is null ? Beacon.Icon : IconCacheProcessor.CombinedQualityIcon(Beacon.Icon, Quality.Icon);
			}
		}
	}

	public readonly struct RecipeQualityPair(IRecipe recipe, IQuality quality) {
		public readonly IRecipe Recipe = recipe;
		public readonly IQuality Quality = quality;

		public override bool Equals(object? obj) => obj is RecipeQualityPair other && Equals(other);
		public bool Equals(RecipeQualityPair other) => Recipe == other.Recipe && Quality == other.Quality;
		public override int GetHashCode() => HashCode.Combine(Recipe, Quality);
		public static bool operator ==(RecipeQualityPair lhs, RecipeQualityPair rhs) => lhs.Equals(rhs);
		public static bool operator !=(RecipeQualityPair lhs, RecipeQualityPair rhs) => !(lhs == rhs);
		public static implicit operator bool(RecipeQualityPair bp) => bp.Recipe != null && bp.Quality != null;
		public override string ToString() => Recipe?.ToString() + " (" + Quality?.ToString() + ")";

		public string FriendlyName {
			get {
				if (Quality == Quality?.Owner.DefaultQuality)
					return Recipe?.FriendlyName + "";
				else
					return Recipe?.FriendlyName + " (" + Quality?.FriendlyName + ")";
			}
		}
		public Bitmap? Icon {
			get {
				if (Recipe == null)
					return null;
				return Quality == Quality?.Owner.DefaultQuality || Quality is null ? Recipe.Icon : IconCacheProcessor.CombinedQualityIcon(Recipe.Icon, Quality.Icon);
			}
		}
	}
}
