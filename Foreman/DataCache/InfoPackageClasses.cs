using System;
using System.Collections.Generic;

namespace Foreman.DataCache {
	public class SaveFileInfo {
		public Dictionary<string, string> Mods { get; private set; }
		public Dictionary<string, bool> Technologies { get; private set; }
		public Dictionary<string, bool> Recipes { get; private set; }
		public SaveFileInfo() {
			Mods = [];
			Technologies = [];
			Recipes = [];
		}
	}

	public struct PresetInfo {
		public Dictionary<string, string>? ModList { get; set; }
		public bool ExpensiveRecipes { get; set; }
		public bool ExpensiveTechnology { get; set; }
		public PresetInfo(Dictionary<string, string>? modList, bool ERecipes, bool ETech) { ModList = modList; ExpensiveRecipes = ERecipes; ExpensiveTechnology = ETech; }
	}

	public class PresetErrorPackage : IComparable<PresetErrorPackage> {
		public Preset Preset;

		public List<string> RequiredMods;
		public List<string> RequiredItems;
		public List<string> RequiredRecipes;
		public List<string> RequiredPlanting;
		public List<string> RequiredQualities;

		public List<string> MissingRecipes;
		public List<string> IncorrectRecipes;
		public List<string> ValidMissingRecipes; //any recipes that were missing previously but have been found to fit in this current preset
		public List<string> MissingItems;
		//we ignore spoiling and burn results as they are part of item data and its not feasable to process them in the same way as recipes & plantResults. In any case, this will effect only the 'error' counter, not actual graph.
		public List<string> MissingPlanting;
		public List<string> ValidMissingPlanting; //any planting processes that were missing previously but have been found to fit in this current preset
		public List<string> IncorrectPlanting;
		public List<string> MissingQualities;
		public List<string> MissingMods;
		public List<string> AddedMods;
		public List<string> WrongVersionMods;

		public int MICount { get { return MissingRecipes.Count + IncorrectRecipes.Count + MissingItems.Count + MissingPlanting.Count + IncorrectPlanting.Count + MissingQualities.Count; } }
		public int ErrorCount { get { return MICount + MissingMods.Count + AddedMods.Count + WrongVersionMods.Count; } }

		public PresetErrorPackage(Preset preset) {
			Preset = preset;
			RequiredMods = [];
			RequiredItems = [];
			RequiredRecipes = [];
			RequiredPlanting = [];
			RequiredQualities = [];

			MissingRecipes = [];
			IncorrectRecipes = [];
			ValidMissingRecipes = [];
			MissingItems = [];
			MissingPlanting = [];
			IncorrectPlanting = [];
			ValidMissingPlanting = [];
			MissingQualities = [];
			MissingMods = []; // in mod-name|version format
			AddedMods = []; //in mod-name|version format
			WrongVersionMods = []; //in mod-name|expected-version|preset-version format
		}

		public int CompareTo(PresetErrorPackage? other) //usefull for sorting the Presets by increasing severity (mods, items/recipes)
		{
			int modErrorComparison = MissingMods.Count.CompareTo(other?.MissingMods.Count);
			if (modErrorComparison != 0)
				return modErrorComparison;
			modErrorComparison = AddedMods.Count.CompareTo(other?.AddedMods.Count);
			if (modErrorComparison != 0)
				return modErrorComparison;
			return MICount.CompareTo(other?.MICount);
		}
	}
}
