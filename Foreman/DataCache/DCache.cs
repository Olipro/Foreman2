using Foreman.DataCache.DataTypes;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Foreman.DataCache {
	public partial class DCache
	{
		public static readonly DCache defaultDCache = new(true);
		public string? PresetName { get; private set; }

		public IEnumerable<IGroup> AvailableGroups { get { return groups.Values.Where(g => g.Available); } }
		public IEnumerable<ISubgroup> AvailableSubgroups { get { return subgroups.Values.Where(g => g.Available); } }
		public IEnumerable<IQuality> AvailableQualities { get { return qualities.Values.Where(g => g.Available); } }
		public IEnumerable<IItem> AvailableItems { get { return items.Values.Where(g => g.Available); } }
		public IEnumerable<IRecipe> AvailableRecipes { get { return recipes.Values.Where(g => g.Available); } }
		public IEnumerable<IPlantProcess> AvailablePlantProcesses { get { return plantProcesses.Values.Where(g => g.Available); } }

		//mods: <name, version>
		//others: <name, object>

		public IReadOnlyDictionary<string, string> IncludedMods { get { return includedMods; } }
		public IReadOnlyDictionary<string, ITechnology> Technologies { get { return technologies; } }
		public IReadOnlyDictionary<string, IGroup> Groups { get { return groups; } }
		public IReadOnlyDictionary<string, ISubgroup> Subgroups { get { return subgroups; } }
		public IReadOnlyDictionary<string, IQuality> Qualities { get { return qualities; } }
		public IReadOnlyDictionary<string, IItem> Items { get { return items; } }
		public IReadOnlyDictionary<string, IRecipe> Recipes { get { return recipes; } }
		public IReadOnlyDictionary<string, IPlantProcess> PlantProcesses { get { return PlantProcesses; } }
		public IReadOnlyDictionary<string, IAssembler> Assemblers { get { return assemblers; } }
		public IReadOnlyDictionary<string, IModule> Modules { get { return modules; } }
		public IReadOnlyDictionary<string, IBeacon> Beacons { get { return beacons; } }
		public IReadOnlyList<IItem> SciencePacks { get { return sciencePacks; } }
		public IReadOnlyDictionary<IItem, ICollection<IItem>> SciencePackPrerequisites { get { return sciencePackPrerequisites; } }

		public IAssembler PlayerAssembler => playerAssember;
		public IAssembler RocketAssembler => rocketAssembler;
		public ITechnology StartingTech { get { return startingTech; } }

		//missing objects are not linked properly and just have the minimal values necessary to function. They are just placeholders, and cant actually be added to graph except while importing. They are also not solved for.
		public ISubgroup? MissingSubgroup => missingSubgroup;
		public IReadOnlyDictionary<string, IQuality> MissingQualities { get { return missingQualities; } }
		public IReadOnlyDictionary<string, IItem> MissingItems { get { return missingItems; } }
		public IReadOnlyDictionary<string, IAssembler> MissingAssemblers { get { return missingAssemblers; } }
		public IReadOnlyDictionary<string, IModule> MissingModules { get { return missingModules; } }
		public IReadOnlyDictionary<string, IBeacon> MissingBeacons { get { return missingBeacons; } }
		public IReadOnlyDictionary<RecipeShort, IRecipe> MissingRecipes { get { return missingRecipes; } }
		public IReadOnlyDictionary<PlantShort, IPlantProcess> MissingPlantProcesses { get { return missingPlantProcesses; } }

		public IQuality DefaultQuality { get; private set; }
		public uint QualityMaxChainLength { get; private set; }
		private readonly IQuality ErrorQuality;

		private static Bitmap? noBeaconIcon;
		public static Bitmap NoBeaconIcon { get { noBeaconIcon ??= IconCache.GetIcon(Path.Combine("Graphics", "NoBeacon.png"), 64); return noBeaconIcon; } }

		private readonly Dictionary<string, string> includedMods; //name : version
		private readonly Dictionary<string, ITechnology> technologies;
		private readonly Dictionary<string, IGroup> groups;
		private readonly Dictionary<string, ISubgroup> subgroups;
		private readonly Dictionary<string, IQuality> qualities;
		private readonly Dictionary<string, IItem> items;
		private readonly Dictionary<string, IRecipe> recipes;
		private readonly Dictionary<string, IPlantProcess> plantProcesses;
		private readonly Dictionary<string, IAssembler> assemblers;
		private readonly Dictionary<string, IModule> modules;
		private readonly Dictionary<string, IBeacon> beacons;
		private readonly List<IItem> sciencePacks;
		private readonly Dictionary<IItem, ICollection<IItem>> sciencePackPrerequisites;

		private readonly Dictionary<string, IQuality> missingQualities;
		private readonly Dictionary<string, IItem> missingItems;
		private readonly Dictionary<string, IAssembler> missingAssemblers;
		private readonly Dictionary<string, IModule> missingModules;
		private readonly Dictionary<string, IBeacon> missingBeacons;
		private readonly Dictionary<RecipeShort, IRecipe> missingRecipes;
		private readonly Dictionary<PlantShort, IPlantProcess> missingPlantProcesses;

		private readonly GroupPrototype extraFormanGroup;
		private readonly SubgroupPrototype extractionSubgroupItems;
		private readonly SubgroupPrototype extractionSubgroupFluids;
		private readonly SubgroupPrototype extractionSubgroupFluidsOP; //offshore pumps
		private readonly SubgroupPrototype energySubgroupBoiling; //water to steam (boilers)
		private readonly SubgroupPrototype energySubgroupEnergy; //heat production (heat consumption is processed as 'fuel'), steam consumption, burning to energy
		private readonly SubgroupPrototype rocketLaunchSubgroup; //any rocket launch recipes will go here

		private readonly ItemPrototype HeatItem;
		private readonly RecipePrototype HeatRecipe;
		private readonly RecipePrototype BurnerRecipe; //for burner-generators

		private readonly Bitmap ElectricityIcon;

		private readonly AssemblerPrototype playerAssember; //for hand crafting. Because Fk automation, thats why.
		private readonly AssemblerPrototype rocketAssembler; //for those rocket recipes

		private readonly SubgroupPrototype missingSubgroup;
		private readonly TechnologyPrototype startingTech;
		private readonly AssemblerPrototype missingAssembler; //missing recipes will have this set as their one and only assembler.

		private readonly bool UseRecipeBWLists;
		private static readonly Regex[] recipeWhiteList = [EmptyBarrelRegex()]; //whitelist takes priority over blacklist
		private static readonly Regex[] recipeBlackList = [DashBarrelRegex(), DeadlockPackRecipe(), DeadlockUnpackRecipe(), DeadlockPlasticPackaging()];
		private static readonly KeyValuePair<string, Regex>[] recyclingItemNameBlackList = [new KeyValuePair<string, Regex>("barrel", DashBarrelRegex())];

		private Dictionary<string, IconColorPair>? iconCache;

		private static readonly double MaxTemp = 10000000; //some mods set the temperature ranges as 'way too high' and expect factorio to handle it (it does). Since we prefer to show temperature ranges we will define any temp beyond these as no limit
		private static readonly double MinTemp = -MaxTemp;

		public DCache(bool filterRecipes) //if true then the read recipes will be filtered by the white and black lists above. In most cases this is desirable (why bother with barreling, etc???), but if the user want to use them, then so be it.
		{
			UseRecipeBWLists = filterRecipes;

			includedMods = [];
			technologies = [];
			groups = [];
			subgroups = [];
			qualities = [];
			items = [];
			recipes = [];
			plantProcesses = [];
			assemblers = [];
			modules = [];
			beacons = [];
			sciencePacks = [];
			sciencePackPrerequisites = [];

			missingQualities = [];
			missingItems = [];
			missingAssemblers = [];
			missingModules = [];
			missingBeacons = [];
			missingRecipes = new Dictionary<RecipeShort, IRecipe>(new RecipeShortNaInPrComparer());
			missingPlantProcesses = [];

			startingTech = new TechnologyPrototype(this, "§§t:starting_tech", "Starting Technology") {
				Tier = 0
			};

			extraFormanGroup = new GroupPrototype(this, "§§g:extra_group", "Resource Extraction\nPower Generation\nRocket Launches", "~~~z1");
			extraFormanGroup.SetIconAndColor(new IconColorPair(IconCache.GetIcon(Path.Combine("Graphics", "ExtraGroupIcon.png"), 64), Color.Gray));

			extractionSubgroupItems = new SubgroupPrototype(this, "§§sg:extraction_items", "1") {
				myGroup = extraFormanGroup
			};
			extraFormanGroup.subgroups.Add(extractionSubgroupItems);

			extractionSubgroupFluids = new SubgroupPrototype(this, "§§sg:extraction_fluids", "2") {
				myGroup = extraFormanGroup
			};
			extraFormanGroup.subgroups.Add(extractionSubgroupFluids);

			extractionSubgroupFluidsOP = new SubgroupPrototype(this, "§§sg:extraction_fluids_2", "3") {
				myGroup = extraFormanGroup
			};
			extraFormanGroup.subgroups.Add(extractionSubgroupFluidsOP);

			energySubgroupBoiling = new SubgroupPrototype(this, "§§sg:energy_boiling", "4") {
				myGroup = extraFormanGroup
			};
			extraFormanGroup.subgroups.Add(energySubgroupBoiling);

			energySubgroupEnergy = new SubgroupPrototype(this, "§§sg:energy_heat", "5") {
				myGroup = extraFormanGroup
			};
			extraFormanGroup.subgroups.Add(energySubgroupEnergy);

			rocketLaunchSubgroup = new SubgroupPrototype(this, "§§sg:rocket_launches", "6") {
				myGroup = extraFormanGroup
			};
			extraFormanGroup.subgroups.Add(rocketLaunchSubgroup);

			IconColorPair heatIcon = new(IconCache.GetIcon(Path.Combine("Graphics", "HeatIcon.png"), 64), Color.DarkRed);
			IconColorPair burnerGeneratorIcon = new(IconCache.GetIcon(Path.Combine("Graphics", "BurnerGeneratorIcon.png"), 64), Color.DarkRed);
			IconColorPair playerAssemblerIcon = new(IconCache.GetIcon(Path.Combine("Graphics", "PlayerAssembler.png"), 64), Color.Gray);

			HeatItem = new ItemPrototype(this, "§§i:heat", "Heat (1MJ)", new SubgroupPrototype(this, "-", "-"), "-"); //we dont want heat to appear as an item in the lists, so just give it a blank subgroup.
			HeatItem.SetIconAndColor(heatIcon);
			HeatItem.FuelValue = 1000000; //1MJ - nice amount

			HeatRecipe = new RecipePrototype(this, "§§r:h:heat-generation", "Heat Generation", energySubgroupEnergy, "1");
			HeatRecipe.SetIconAndColor(heatIcon);
			HeatRecipe.InternalOneWayAddProduct(HeatItem, 1, 0);
			HeatItem.ProductionRecipesInternal.Add(HeatRecipe);
			HeatRecipe.Time = 1;

			BurnerRecipe = new RecipePrototype(this, "§§r:h:burner-electicity", "Burner Generator", energySubgroupEnergy, "2");
			BurnerRecipe.SetIconAndColor(burnerGeneratorIcon);
			BurnerRecipe.Time = 1;

			rocketAssembler = new AssemblerPrototype(this, "§§a:rocket-assembler", "Rocket", EntityType.Rocket, EnergySource.Void) {
				energyDrain = 0
			};
			IconColorPair rocketAssemblerIcon = new(IconCache.GetIcon(Path.Combine("Graphics", "RocketAssembler.png"), 64), Color.Gray);
			rocketAssembler.SetIconAndColor(rocketAssemblerIcon);

			playerAssember = new AssemblerPrototype(this, "§§a:player-assembler", "Player", EntityType.Assembler, EnergySource.Void) {
				energyDrain = 0
			};
			playerAssember.SetIconAndColor(playerAssemblerIcon);

			ElectricityIcon = IconCache.GetIcon(Path.Combine("Graphics", "ElectricityIcon.png"), 64);

			missingSubgroup = new SubgroupPrototype(this, "§§MISSING-SG", "") {
				myGroup = new GroupPrototype(this, "§§MISSING-G", "MISSING", "")
			};

			missingAssembler = new AssemblerPrototype(this, "§§a:MISSING-A", "missing assembler", EntityType.Assembler, EnergySource.Void, true);

			ErrorQuality = new QualityPrototype(this, "§§error_quality", "ERROR", "-");
			DefaultQuality = ErrorQuality;
			Clear();
		}

		public async Task LoadAllData(Preset preset, IProgress<KeyValuePair<int, string>> progress, bool loadIcons = true)
		{
			Clear();
			//return;

			Dictionary<string, List<RecipePrototype>> craftingCategories = [];
			Dictionary<string, List<ModulePrototype>> moduleCategories = [];
			Dictionary<string, List<RecipePrototype>> resourceCategories = new() {
				{ "<<foreman_resource_category_water_tile>>", [] } //the water resources
			};
			Dictionary<string, List<ItemPrototype>> fuelCategories = new() {
				{ "§§fc:liquids", [] } //the liquid fuels category
			};
			Dictionary<IItem, string> burnResults = [];
			Dictionary<IItem, string> spoilResults = [];
			Dictionary<IQuality, string> nextQualities = [];
			List<IRecipe> miningWithFluidRecipes = [];

			PresetName = preset.Name;
			JObject jsonData = PresetProcessor.PrepPreset(preset);
			if (jsonData == null)
				return;

			iconCache = loadIcons ? await IconCache.LoadIconCache(Path.Combine([Application.StartupPath, "Presets", preset.Name + ".dat"]), progress, 0, 90) : [];

			await Task.Run(() =>
			{
				progress.Report(new KeyValuePair<int, string>(90, "Processing Data...")); //this is SUPER quick, so we dont need to worry about timing stuff here

				//process each section (order is rather important here)
				foreach (var objJToken in jsonData["mods"]?.ToList() ?? [])
					ProcessMod(objJToken);

				foreach (var objJToken in jsonData["subgroups"]?.ToList() ?? [])
					ProcessSubgroup(objJToken);

				foreach (var objJToken in jsonData["groups"]?.ToList() ?? [])
					ProcessGroup(objJToken, iconCache);

				foreach (var objToken in jsonData["qualities"]?.ToList() ?? [])
					ProcessQuality(objToken, iconCache, nextQualities);
				foreach (QualityPrototype quality in qualities.Values.Cast<QualityPrototype>())
					ProcessQualityLink(quality, nextQualities);
				PostProcessQuality();

				foreach (var objJToken in jsonData["fluids"]?.ToList() ?? [])
					ProcessFluid(objJToken, iconCache, fuelCategories);

				foreach (var objJToken in jsonData["items"]?.ToList() ?? [])
					ProcessItem(objJToken, iconCache, fuelCategories, burnResults, spoilResults); //items after fluids to take care of duplicates (if name exists in fluid and in item set, then only the fluid is counted)
				foreach (ItemPrototype item in items.Values.Cast<ItemPrototype>())
					ProcessBurnItem(item, burnResults); //link up any items with burn remains
                foreach (var objJToken in jsonData["items"]?.ToList() ?? [])
                    ProcessPlantProcess(objJToken); //process items json specifically for plant processes (items should all be populated by now)
                foreach (ItemPrototype item in items.Values.Cast<ItemPrototype>())
                    ProcessSpoilItem(item, spoilResults); //link up any items with spoil remains

				foreach (var objJToken in jsonData["modules"]?.ToList() ?? [])
					ProcessModule(objJToken, iconCache, moduleCategories);

                foreach (var objJToken in jsonData["recipes"]?.ToList() ?? [])
					ProcessRecipe(objJToken, iconCache, craftingCategories, moduleCategories);

				foreach (var objJToken in jsonData["resources"]?.ToList() ?? [])
					ProcessResource(objJToken, resourceCategories, miningWithFluidRecipes);
				foreach (var objToken in jsonData["water_resources"]?.ToList() ?? [])
					ProcessResource(objToken, resourceCategories, miningWithFluidRecipes);

				foreach (var objJToken in jsonData["technologies"]?.ToList() ?? [])
					ProcessTechnology(objJToken, iconCache, miningWithFluidRecipes);
				foreach (var objJToken in jsonData["technologies"]?.ToList() ?? [])
					ProcessTechnologyP2(objJToken); //required to properly link technology prerequisites

				foreach (var objJToken in jsonData["entities"]?.ToList() ?? [])
					ProcessEntity(objJToken, iconCache, craftingCategories, resourceCategories, fuelCategories, miningWithFluidRecipes, moduleCategories);

				//process launch products (empty now - depreciated)
				foreach (var objJToken in jsonData["items"]?.Where(t => t["rocket_launch_products"] != null).ToList() ?? [])
					ProcessRocketLaunch(objJToken);
				foreach (var objJToken in jsonData["fluids"]?.Where(t => t["rocket_launch_products"] != null).ToList() ?? [])
					ProcessRocketLaunch(objJToken);

				//process character
				var entities = jsonData["entities"] ?? throw new InvalidOperationException("jsonData[entities] is null");
				ProcessCharacter(entities.First(a => a["name"]?.Value<string>() == "character"), craftingCategories);

				//add rocket assembler
				assemblers.Add(rocketAssembler.Name, rocketAssembler);

				//remove these temporary dictionaries (no longer necessary)
				craftingCategories.Clear();
				resourceCategories.Clear();
				fuelCategories.Clear();
				burnResults.Clear();
				spoilResults.Clear();


				//sort
				foreach (GroupPrototype g in groups.Values.Cast<GroupPrototype>())
					g.SortSubgroups();
				foreach (SubgroupPrototype sg in subgroups.Values.Cast<SubgroupPrototype>())
					sg.SortIRs();

				//The data read by the dataCache (json preset) includes everything. We need to now process it such that any items/recipes that cant be used dont appear.
				//thus any object that has Unavailable set to true should be ignored. We will leave the option to use them to the user, but in most cases its better without them


				//delete any recipe that has no assembler. This is the only type of deletion that we will do, as we MUST enforce the 'at least 1 assembler' per recipe. The only recipes with no assemblers linked are those added to 'missing' category, and those are handled separately.
				//note that even hand crafting has been handled: there is a player assembler that has been added. So the only recipes removed here are those that literally can not be crafted.
				foreach (RecipePrototype recipe in recipes.Values.Where(r => r.Assemblers.Count == 0).ToList().Cast<RecipePrototype>())
				{
					foreach (ItemPrototype ingredient in recipe.IngredientListInternal)
						ingredient.ConsumptionRecipesInternal.Remove(recipe);
					foreach (ItemPrototype product in recipe.ProductListInternal)
						product.ProductionRecipesInternal.Remove(recipe);
					foreach (TechnologyPrototype tech in recipe.MyUnlockTechnologiesInternal)
						tech.UnlockedRecipesInternal.Remove(recipe);
					foreach (ModulePrototype module in recipe.AssemblerModulesInternal)
						module.RecipesInternal.Remove(recipe);
					recipe.mySubgroup.recipes.Remove(recipe);

					recipes.Remove(recipe.Name);
					ErrorLogging.LogLine(string.Format("Removal of {0} due to having no assemblers associated with it.", recipe));
					Console.WriteLine(string.Format("Removal of {0} due to having no assemblers associated with it.", recipe));
				}

				//calculate the availability of various recipes and entities (based on their unlock technologies + entity place objects' unlock technologies)
				ProcessAvailableStatuses();

				//calculate the science packs for each technology (based on both their listed science packs, the science packs of their prerequisites, and the science packs required to research the science packs)
				ProcessSciencePacks();

				//delete any groups/subgroups without any items/recipes within them, and sort by order
				CleanupGroups();

				//check each fluid to see if all production recipe temperatures can fit within all consumption recipe ranges. if not, then the item / fluid is set to be 'temperature dependent' and requires further processing when checking link validity.
				UpdateFluidTemperatureDependencies();

#if DEBUG
				//PrintDataCache();
#endif

				progress.Report(new KeyValuePair<int, string>(98, "Finalizing..."));
				progress.Report(new KeyValuePair<int, string>(100, "Done!"));
			});
		}

		public void Clear()
		{
			DefaultQuality = ErrorQuality;

			includedMods.Clear();
			technologies.Clear();
			groups.Clear();
			subgroups.Clear();
			items.Clear();
			recipes.Clear();
			plantProcesses.Clear();
			assemblers.Clear();
			modules.Clear();
			beacons.Clear();

			missingItems.Clear();
			missingAssemblers.Clear();
			missingModules.Clear();
			missingBeacons.Clear();
			missingRecipes.Clear();
			missingPlantProcesses.Clear();

			if (iconCache != null)
			{
				foreach (var iconset in iconCache.Values)
					iconset?.Icon?.Dispose();
				iconCache.Clear();
			}

			groups.Add(extraFormanGroup.Name, extraFormanGroup);
			subgroups.Add(extractionSubgroupItems.Name, extractionSubgroupItems);
			subgroups.Add(extractionSubgroupFluids.Name, extractionSubgroupFluids);
			subgroups.Add(extractionSubgroupFluidsOP.Name, extractionSubgroupFluidsOP);
			items.Add(HeatItem.Name, HeatItem);
			recipes.Add(HeatRecipe.Name, HeatRecipe);
			recipes.Add(BurnerRecipe.Name, BurnerRecipe);
			technologies.Add(StartingTech.Name, startingTech);
		}

		//------------------------------------------------------Import processing

		public void ProcessImportedItemsSet(IEnumerable<string> itemNames) //will ensure that all items are now part of the data cache -> existing ones (regular and missing) are skipped, new ones are added to MissingItems
		{
			foreach (string iItem in itemNames)
			{
				if (!items.ContainsKey(iItem) && !missingItems.ContainsKey(iItem)) //want to check for missing items too - in this case dont want duplicates
				{
					ItemPrototype missingItem = new(this, iItem, iItem, missingSubgroup, "", true); //just assume it isnt a fluid. we dont honestly care (no temperatures)
					missingItems.Add(missingItem.Name, missingItem);
				}
			}
		}

        public Dictionary<string, IQuality> ProcessImportedQualitiesSet(IEnumerable<KeyValuePair<string, int>> qualityPairs)
		{
			//check that a quality exists in the set of qualities (missing or otherwise) that has the correct level; if not, make a new one
			Dictionary<string, IQuality> qualityMap = [];

			foreach(var quality in qualityPairs)
			{
				//check quality sets for any direct matches (name & level)
				if (qualities.Values.Any(q => q.Name == quality.Key && q.Level == quality.Value))
				{
					qualityMap.Add(quality.Key, qualities[quality.Key]);
					continue;
				}
				else if (missingQualities.Values.Any(q => q.Name == quality.Key && q.Level == quality.Value))
				{
					qualityMap.Add(quality.Key, missingQualities[quality.Key]);
					continue;
				}

                //check for any matching level quality in the base chain (starting from 'normal' and going until null)
                IQuality? curQuality = DefaultQuality;
                while (curQuality != null)
                {
					if (curQuality.Level == quality.Value)
						break;
                    curQuality = curQuality.NextQuality;
                }
				if (curQuality != null)
				{
					qualityMap.Add(quality.Key, curQuality);
					continue;
				}

                //step 3: check if there is a quality of the same level
                curQuality = Qualities.Values.FirstOrDefault(q => q.Level == quality.Value);
                if (curQuality != null)
				{
					qualityMap.Add(quality.Key, curQuality);
					continue;
				}
                curQuality = MissingQualities.Values.FirstOrDefault(q => q.Level == quality.Value);
                if (curQuality != null)
                {
                    qualityMap.Add(quality.Key, curQuality);
                    continue;
                }

				if (curQuality is null)
					throw new InvalidOperationException("curQuality is null");

				//step 4: no other option, make a new quality and add it to missing qualities
				string missingQualityName = quality.Key;
				while (qualities.ContainsKey(missingQualityName) || missingQualities.ContainsKey(missingQualityName))
					missingQualityName += "_";

				QualityPrototype missingQuality = new(this, missingQualityName, quality.Key, "-", true);
				missingQualities.Add(missingQuality.Name, missingQuality);
				qualityMap.Add(quality.Key, curQuality);
			}

			return qualityMap;
		}

        public void ProcessImportedAssemblersSet(IEnumerable<string> assemblerNames)
		{
			foreach (string iAssembler in assemblerNames)
			{
				if (!assemblers.ContainsKey(iAssembler) && !missingAssemblers.ContainsKey(iAssembler))
				{
					AssemblerPrototype missingAssembler = new(this, iAssembler, iAssembler, EntityType.Assembler, EnergySource.Void, true); //dont know, dont care about entity type we will just treat it as a void-assembler (and let fuel io + recipe figure it out)
					missingAssemblers.Add(missingAssembler.Name, missingAssembler);
				}
			}
		}

		public void ProcessImportedModulesSet(IEnumerable<string> moduleNames)
		{
			foreach (string iModule in moduleNames)
			{
				if (!modules.ContainsKey(iModule) && !missingModules.ContainsKey(iModule))
				{
					ModulePrototype missingModule = new(this, iModule, iModule, true);
					missingModules.Add(missingModule.Name, missingModule);
				}
			}
		}

		public void ProcessImportedBeaconsSet(IEnumerable<string> beaconNames)
		{
			foreach (string iBeacon in beaconNames)
			{
				if (!beacons.ContainsKey(iBeacon) && !missingBeacons.ContainsKey(iBeacon))
				{
					BeaconPrototype missingBeacon = new(this, iBeacon, iBeacon, EnergySource.Void, true);
					missingBeacons.Add(missingBeacon.Name, missingBeacon);
				}
			}
		}

		public Dictionary<long, IRecipe> ProcessImportedRecipesSet(IEnumerable<RecipeShort> recipeShorts) //will ensure all recipes are now part of the data cache -> each one is checked against existing recipes (regular & missing), and if it doesnt exist are added to MissingRecipes. Returns a set of links of original recipeID (NOT! the noew recipeIDs) to the recipe
		{
			Dictionary<long, IRecipe> recipeLinks = [];
			foreach (RecipeShort recipeShort in recipeShorts)
			{
				IRecipe? recipe = null;

				//recipe check #1 : does its name exist in database (note: we dont quite care about extra missing recipes here - so what if we have a couple identical ones? they will combine during save/load anyway)
				bool recipeExists = recipes.ContainsKey(recipeShort.Name);
				if (recipeExists)
				{
					//recipe check #2 : do the number of ingredients & products match?
					recipe = recipes[recipeShort.Name];
					recipeExists &= recipeShort.Ingredients.Count == recipe.IngredientList.Count;
					recipeExists &= recipeShort.Products.Count == recipe.ProductList.Count;
				}
				if (recipeExists)
				{
					//recipe check #3 : do the ingredients & products from the loaded data match the actual recipe? (names, not quantities -> this is to allow some recipes to pass; ex: normal->expensive might change the values, but importing such a recipe should just use the 'correct' quantities and soft-pass the different recipe)
					foreach (string ingredient in recipeShort.Ingredients.Keys)
						recipeExists &= items.ContainsKey(ingredient) && (recipe?.IngredientSet.ContainsKey(items[ingredient]) ?? false);
					foreach (string product in recipeShort.Products.Keys)
						recipeExists &= items.ContainsKey(product) && (recipe?.ProductSet.ContainsKey(items[product]) ?? false);
				}
				if (!recipeExists)
				{
					bool missingRecipeExists = missingRecipes.ContainsKey(recipeShort);

					if (missingRecipeExists)
					{
						recipe = missingRecipes[recipeShort];
					}
					else
					{
						RecipePrototype missingRecipe = new(this, recipeShort.Name, recipeShort.Name, missingSubgroup, "", true);
						foreach (var ingredient in recipeShort.Ingredients)
						{
							if (items.TryGetValue(ingredient.Key, out IItem? value))
								missingRecipe.InternalOneWayAddIngredient((ItemPrototype)value, ingredient.Value);
							else
								missingRecipe.InternalOneWayAddIngredient((ItemPrototype)missingItems[ingredient.Key], ingredient.Value);
						}
						foreach (var product in recipeShort.Products)
						{
							if (items.TryGetValue(product.Key, out IItem? value))
								missingRecipe.InternalOneWayAddProduct((ItemPrototype)value, product.Value, 0);
							else
								missingRecipe.InternalOneWayAddProduct((ItemPrototype)missingItems[product.Key], product.Value, 0);
						}
						missingRecipe.AssemblersInternal.Add(missingAssembler);
						missingAssembler.RecipesInternal.Add(missingRecipe);

						missingRecipes.Add(recipeShort, missingRecipe);
						recipe = missingRecipe;
					}
				}
				if (recipe is not null)
					recipeLinks.TryAdd(recipeShort.RecipeID, recipe);
			}
			return recipeLinks;
		}

		//pretty much a copy of the above, just for plant processes (so no ingredient list, and using different data sets)
		public Dictionary<long, IPlantProcess> ProcessImportedPlantProcessesSet(IEnumerable<PlantShort> plantShorts)
		{
            Dictionary<long, IPlantProcess> plantLinks = [];
            foreach (PlantShort plantShort in plantShorts)
            {
                IPlantProcess? pprocess = null;

                //recipe check #1 : does its name exist in database (note: we dont quite care about extra missing recipes here - so what if we have a couple identical ones? they will combine during save/load anyway)
                bool pprocessExists = plantProcesses.ContainsKey(plantShort.Name);
                if (pprocessExists)
                {
                    //recipe check #2 : do the number of ingredients & products match?
                    pprocess = plantProcesses[plantShort.Name];
                    pprocessExists &= plantShort.Products.Count == pprocess.ProductList.Count;
                }
                if (pprocessExists)
                {
                    //recipe check #3 : do the ingredients & products from the loaded data match the actual recipe? (names, not quantities -> this is to allow some recipes to pass; ex: normal->expensive might change the values, but importing such a recipe should just use the 'correct' quantities and soft-pass the different recipe)
                    foreach (string product in plantShort.Products.Keys)
                        pprocessExists &= items.ContainsKey(product) && pprocess is not null && pprocess.ProductSet.ContainsKey(items[product]);
                }
                if (!pprocessExists)
                {
                    bool missingPProcessExists = missingPlantProcesses.ContainsKey(plantShort);

                    if (missingPProcessExists)
                    {
                        pprocess = missingPlantProcesses[plantShort];
                    }
					else
                    {
                        PlantProcessPrototype missingPProcess = new(this, plantShort.Name, true);
                        foreach (var product in plantShort.Products)
                        {
                            if (items.TryGetValue(product.Key, out IItem? value))
                                missingPProcess.InternalOneWayAddProduct((ItemPrototype)value, product.Value);
                            else
                                missingPProcess.InternalOneWayAddProduct((ItemPrototype)missingItems[product.Key], product.Value);
                        }

                        missingPlantProcesses.Add(plantShort, missingPProcess);
                        pprocess = missingPProcess;
                    }
                }
				if (pprocess is not null)
					plantLinks.TryAdd(plantShort.PlantID, pprocess);
			}
            return plantLinks;
        }

		//------------------------------------------------------Data cache load helper functions (all the process functions from LoadAllData)

		private void ProcessMod(JToken objJToken)
		{
			var name = objJToken["name"]?.Value<string>();
			var version = objJToken["version"]?.Value<string>();
			if (name is not null && version is not null)
				includedMods.Add(name, version);
		}

		private void ProcessSubgroup(JToken objJToken) {
			var name = objJToken["name"]?.Value<string>();
			var order = objJToken["order"]?.Value<string>();
			if (name is null || order is null)
				return;
			SubgroupPrototype subgroup = new(this, name, order);
			subgroups.Add(subgroup.Name, subgroup);
		}

		private void ProcessGroup(JToken objJToken, Dictionary<string, IconColorPair> iconCache)
		{
			var name = objJToken["name"]?.Value<string>();
			var order = objJToken["order"]?.Value<string>();
			var localised_name = objJToken["localised_name"]?.Value<string>();
			var icon_name = objJToken["icon_name"]?.Value<string>();
			var subgroups = objJToken["subgroups"];
			if (name is null || order is null || localised_name is null || icon_name is null || subgroups is null)
				throw new InvalidOperationException("Null keys in JSON");
			GroupPrototype group = new(this, name, localised_name, order);

			if (iconCache.TryGetValue(icon_name, out IconColorPair? value))
				group.SetIconAndColor(value);

			foreach (var subgroupJToken in subgroups)
			{
				if (((string?)subgroupJToken) is not string str)
					continue;
				((SubgroupPrototype)this.subgroups[str]).myGroup = group;
				group.subgroups.Add((SubgroupPrototype)this.subgroups[str]);
			}
			groups.Add(group.Name, group);
		}

		private void ProcessQuality(JToken objJToken, Dictionary<string, IconColorPair> iconCache, Dictionary<IQuality, string> nextQualities)
		{
			var name = objJToken["name"]?.Value<string>();
			var order = objJToken["order"]?.Value<string>();
			var localised_name = objJToken["localised_name"]?.Value<string>();
			var icon_name = objJToken["icon_name"]?.Value<string>();
			var hidden = objJToken["hidden"]?.Value<bool>() ?? false;
			var level = objJToken["level"]?.Value<int>();
			var beacon_power_multiplier = objJToken["beacon_power_multiplier"]?.Value<double>();
			var mining_drill_resource_drain_multiplier = objJToken["mining_drill_resource_drain_multiplier"]?.Value<double>();
			var next_probability = objJToken["next_probability"]?.Value<double>();
			var next = objJToken["next"]?.Value<string>();

			if (name is null || order is null || localised_name is null || icon_name is null || level is null || beacon_power_multiplier is null || mining_drill_resource_drain_multiplier is null)
				throw new InvalidOperationException("Null JSON encountered");

			QualityPrototype quality = new(this, name, localised_name, order);

			if (iconCache.TryGetValue(icon_name, out IconColorPair? value))
				quality.SetIconAndColor(value);

			quality.Available = !hidden;
			quality.Enabled = quality.Available; //can be set via science packs, but this requires modifying datacache... so later

			quality.Level = (int)level;
			quality.BeaconPowerMultiplier = (double)beacon_power_multiplier;
			quality.MiningDrillResourceDrainMultiplier = (double)mining_drill_resource_drain_multiplier;
			quality.NextProbability = next_probability != null ? (double)next_probability : 0.0;

			if (quality.NextProbability != 0 && next is not null)
				nextQualities.Add(quality, next);

			qualities.Add(quality.Name, quality);
		}

		private void ProcessQualityLink(QualityPrototype quality, Dictionary<IQuality, string> nextQualities)
		{

			if (nextQualities.ContainsKey(quality) && qualities.TryGetValue(nextQualities[quality], out IQuality? value))
			{
				quality.NextQuality = value;
				((QualityPrototype)value).PrevQuality = quality;
			}
		}

		private void PostProcessQuality()
		{
			//make sure that the default quality is always enabled & available
			DefaultQuality = qualities.TryGetValue("normal", out IQuality? value) ? value : ErrorQuality;
			DefaultQuality.Enabled = true;
			((QualityPrototype)DefaultQuality).Available = true;

			//make available all qualities that are within the defaultquality chain
			IQuality? cQuality = DefaultQuality;
            while (cQuality != null)
            {
                ((QualityPrototype)cQuality).Available = cQuality.Enabled;
                cQuality = cQuality.NextQuality;
            }

            IQuality currentQuality;
			uint currentChain;
			foreach(IQuality quality in qualities.Values)
			{
				currentChain = 1;
				currentQuality = quality;
				while (currentQuality.NextQuality != null && currentQuality.NextProbability != 0)
				{
					currentChain++;
					currentQuality = currentQuality.NextQuality;
				}
				QualityMaxChainLength = Math.Max(QualityMaxChainLength, currentChain);
			}
		}

		private void ProcessFluid(JToken objJToken, Dictionary<string, IconColorPair> iconCache, Dictionary<string, List<ItemPrototype>> fuelCategories)
		{
			var name = objJToken["name"]?.Value<string>();
			var localised_name = objJToken["localised_name"]?.Value<string>();
			var subgroup = objJToken["subgroup"]?.Value<string>();
			var order = objJToken["order"]?.Value<string>();
			var icon_name = objJToken["icon_name"]?.Value<string>();
			var default_temperature = objJToken["default_temperature"]?.Value<double>();
			var heat_capacity = objJToken["heat_capacity"]?.Value<double>();
			var gas_temperature = objJToken["gas_temperature"]?.ToObject<double>();
			var max_temperature = objJToken["max_temperature"]?.Value<double>();
			var fuel_value = objJToken["fuel_value"]?.Value<double>();
			var emissions_multiplier = objJToken["emissions_multiplier"]?.Value<double>();

			if (name is null || localised_name is null || subgroup is null || order is null || icon_name is null || default_temperature is null || heat_capacity is null || gas_temperature is null || max_temperature is null)
				throw new InvalidOperationException("Encountered missing JSON keys");

			FluidPrototype item = new(this,	name, localised_name, (SubgroupPrototype)subgroups[subgroup], order);

			if (iconCache.TryGetValue(icon_name, out IconColorPair? value))
				item.SetIconAndColor(value);

			item.DefaultTemperature = (double)default_temperature;
			item.SpecificHeatCapacity = (double)heat_capacity;
			item.GasTemperature = (double)gas_temperature;
            item.MaxTemperature = (double)max_temperature;

            if (fuel_value is not null && emissions_multiplier is not null && (double)fuel_value > 0)
			{
				item.FuelValue = (double)fuel_value;
				item.PollutionMultiplier = (double)emissions_multiplier;
				fuelCategories["§§fc:liquids"].Add(item);
			}

			items.Add(item.Name, item);
		}

		private void ProcessItem(JToken objJToken, Dictionary<string, IconColorPair> iconCache, Dictionary<string, List<ItemPrototype>> fuelCategories, Dictionary<IItem, string> burnResults, Dictionary<IItem, string> spoilResults)
		{
			var name = objJToken["name"]?.Value<string>();
			if (name is null || items.ContainsKey(name)) //special handling for fluids which appear in both items & fluid lists (ex: fluid-unknown)
				return;

			var localised_name = objJToken["localised_name"]?.Value<string>();
			var subgroup = objJToken["subgroup"]?.Value<string>();
			var order = objJToken["order"]?.Value<string>();
			var icon_name = objJToken["icon_name"]?.Value<string>();
			var stack_size = objJToken["stack_size"]?.Value<int>();
			var weight = objJToken["weight"]?.Value<double>();
			var ingredient_to_weight_coefficient = objJToken["ingredient_to_weight_coefficient"]?.Value<double>();
			var fuel_category = objJToken["fuel_category"]?.Value<string>();
			var fuel_value = objJToken["fuel_value"]?.Value<double>() ?? 0.0;
			var fuel_emissions_multiplier = objJToken["fuel_emissions_multiplier"]?.Value<double>() ?? 0.0;
			var burnt_result = objJToken["burnt_result"]?.Value<string>();
			var spoil_result = objJToken["spoil_result"]?.Value<string>();

			if (localised_name is null || subgroup is null || order is null || icon_name is null || stack_size is null || weight is null || ingredient_to_weight_coefficient is null)
				throw new InvalidOperationException("Missing JSON keys encountered");

			ItemPrototype item = new(
				this,
				name,
				localised_name,
				(SubgroupPrototype)subgroups[subgroup],
				order);

			if (iconCache.TryGetValue(icon_name, out IconColorPair? value))
				item.SetIconAndColor(value);

			item.StackSize = (int)stack_size;
			item.Weight = (double)weight;
			item.IngredientToWeightCoefficient = (double)ingredient_to_weight_coefficient;

			if (fuel_category != null && fuel_value > 0) //factorio eliminates any 0fuel value fuel from the list (checked)
			{
				item.FuelValue = fuel_value;
				item.PollutionMultiplier = (double)fuel_emissions_multiplier;

				if (!fuelCategories.ContainsKey(fuel_category))
					fuelCategories.Add(fuel_category, []);
				fuelCategories[fuel_category].Add(item);
			}
			if (burnt_result != null)
				burnResults.Add(item, burnt_result);
			if (spoil_result != null)
			{
				spoilResults.Add(item, spoil_result);
				var q_spoil_time = objJToken["q_spoil_time"];
				if (q_spoil_time is not null)
					foreach (JToken spoilToken in q_spoil_time) {
						var quality = spoilToken["quality"]?.Value<string>();
						var val = spoilToken["value"]?.Value<double>();
						if (quality is not null && val is not null)
							item.SpoilageTimes.Add(qualities[quality], (double)val);
					}
            }

            items.Add(item.Name, item);
		}

		private void ProcessBurnItem(ItemPrototype item, Dictionary<IItem, string> burnResults)
		{
			if (burnResults.ContainsKey(item))
			{
				item.BurnResult = items[burnResults[item]];
				((ItemPrototype)items[burnResults[item]]).FuelOrigin = item;
			}
		}
        private void ProcessPlantProcess(JToken objJToken)
        {
			var name = objJToken["name"]?.Value<string>();
			var plant_results = objJToken["plant_results"]?.ToList();

			if (name is null)
				throw new InvalidOperationException("Missing JSON key");

            if (plant_results != null)
			{
				ItemPrototype seed = (ItemPrototype)items[name];
				PlantProcessPrototype plantProcess = new(
					this,
					seed.Name) {
					Seed = seed,
					GrowTime = objJToken["plant_growth_time"]?.Value<double>() ?? 0.0
				};

				foreach (var productJToken in plant_results.ToList())
                {
					if (productJToken["name"]?.Value<string>() is not string prodName)
						throw new InvalidOperationException("Missing JSON key");
					ItemPrototype product = (ItemPrototype)items[prodName];
                    double amount = productJToken["amount"]?.Value<double>() ?? 0.0;
                    if (amount != 0)
                    {
                        plantProcess.InternalOneWayAddProduct(product, amount);
                        product.PlantOriginsInternal.Add(seed);
						seed.PlantResult = plantProcess;
                    }
                }

				plantProcesses.Add(seed.Name, plantProcess); //seed.Name = plantProcess.name, but for clarity: any searches will be done via seed's name
			}
        }
        private void ProcessSpoilItem(ItemPrototype item, Dictionary<IItem, string> spoilResults)
        {
            if (spoilResults.ContainsKey(item))
            {
                item.SpoilResult = items[spoilResults[item]];
                ((ItemPrototype)items[spoilResults[item]]).SpoilOriginsInternal.Add(item);
            }
        }

		private void ProcessModule(JToken objJToken, Dictionary<string, IconColorPair> iconCache, Dictionary<string, List<ModulePrototype>> moduleCategories)
		{
			var name = objJToken["name"]?.Value<string>();
			var localised_name = objJToken["localised_name"]?.Value<string>();
			var icon_name = objJToken["icon_name"]?.Value<string>();
			var icon_alt_name = objJToken["icon_alt_name"]?.Value<string>();
			var module_effects = objJToken["module_effects"];
			var tier = objJToken["tier"]?.Value<int>();
			var category = objJToken["category"]?.Value<string>();

			if (name is null || localised_name is null || icon_name is null || icon_alt_name is null || module_effects is null || tier is null || category is null)
				throw new InvalidOperationException("Missing JSON key");

			ModulePrototype module = new(this, name, localised_name);

			if (iconCache.TryGetValue(icon_name, out IconColorPair? icon))
				module.SetIconAndColor(icon);
			else if (iconCache.TryGetValue(icon_alt_name, out IconColorPair? altIcon))
				module.SetIconAndColor(altIcon);

			if (module_effects["speed"]?.Value<double>() is not double speed ||
				module_effects["productivity"]?.Value<double?>() is not double productivity ||
				module_effects["consumption"]?.Value<double?>() is not double consumption ||
				module_effects["pollution"]?.Value<double?>() is not double pollution ||
				module_effects["quality"]?.Value<double?>() is not double quality)
				throw new InvalidOperationException("Missing JSON key");

			module.SpeedBonus = Math.Round(speed * 1000, 0, MidpointRounding.AwayFromZero) / 1000;
			module.ProductivityBonus = Math.Round(productivity * 1000, 0, MidpointRounding.AwayFromZero) / 1000;
			module.ConsumptionBonus = Math.Round(consumption * 1000, 0, MidpointRounding.AwayFromZero) / 1000;
			module.PollutionBonus = Math.Round(pollution * 1000, 0, MidpointRounding.AwayFromZero) / 1000;
			module.QualityBonus = Math.Round(quality * 1000, 0, MidpointRounding.AwayFromZero) / 1000;

			module.Tier = (int)tier;

			module.Category = category;
			if (!moduleCategories.ContainsKey(module.Category))
				moduleCategories.Add(module.Category, []);
			moduleCategories[module.Category].Add(module);

			modules.Add(module.Name, module);
		}

		private void ProcessRecipe(JToken objJToken, Dictionary<string, IconColorPair> iconCache, Dictionary<string, List<RecipePrototype>> craftingCategories, Dictionary<string, List<ModulePrototype>> moduleCategories) {
			var name = objJToken["name"]?.Value<string>();
			var localised_name = objJToken["localised_name"]?.Value<string>();
			var icon_name = objJToken["icon_name"]?.Value<string>();
			var icon_alt_name = objJToken["icon_alt_name"]?.Value<string>();
			var subgroup = objJToken["subgroup"]?.Value<string>();
			var order = objJToken["order"]?.Value<string>();
			var time = objJToken["energy"]?.Value<double>();
			var enabled = objJToken["enabled"]?.Value<bool>();
			var category = objJToken["category"]?.Value<string>();
			var prod_research = objJToken["prod_research"]?.Value<bool>();
			var maximum_productivity = objJToken["maximum_productivity"]?.Value<double>();
			var products = objJToken["products"]?.ToList();
			var ingredients = objJToken["ingredients"]?.ToList();
			var allowed_effects = objJToken["allowed_effects"];
			var allowed_module_categories = objJToken["allowed_module_categories"];

			if (name is null || localised_name is null || subgroup is null || order is null || time is null || enabled is null || category is null || icon_name is null || icon_alt_name is null || products is null || ingredients is null)
				throw new InvalidOperationException("Missing JSON key");

			RecipePrototype recipe = new(this, name, localised_name, (SubgroupPrototype)subgroups[subgroup], order) {
				Time = (double)time
			};
			if ((bool)enabled) //due to the way the import of presets happens, enabled at this stage means the recipe is available without any research necessary (aka: available at start)
			{
				recipe.MyUnlockTechnologiesInternal.Add(startingTech);
				startingTech.UnlockedRecipesInternal.Add(recipe);
			}

			if (!craftingCategories.ContainsKey(category))
				craftingCategories.Add(category, []);
			craftingCategories[category].Add(recipe);

			if (iconCache.TryGetValue(icon_name, out IconColorPair? icon))
				recipe.SetIconAndColor(icon);
			else if (iconCache.TryGetValue(icon_alt_name, out IconColorPair? altIcon))
				recipe.SetIconAndColor(altIcon);

			recipe.HasProductivityResearch = (prod_research != null && (bool)prod_research);
			recipe.MaxProductivityBonus = maximum_productivity == null ? 1000 : (double)maximum_productivity;

			foreach (var productJToken in products) {
				var pName = productJToken["name"]?.Value<string>();
				var pAmount = productJToken["amount"]?.Value<double>() ?? 0.0;
				var pType = productJToken["type"]?.Value<string>();
				var p_amount = productJToken["p_amount"]?.Value<double>() ?? 0.0;
				var temperature = productJToken["temperature"]?.Value<double>();

				if (pName is null)
					throw new InvalidOperationException("Missing JSON key");

				ItemPrototype product = (ItemPrototype)items[pName];
				double amount = (double)pAmount;
				if (amount != 0)
				{
					if (pType == "fluid")
						recipe.InternalOneWayAddProduct(product, amount, (double)p_amount, temperature == null ? ((FluidPrototype)product).DefaultTemperature : (double)temperature);
					else
						recipe.InternalOneWayAddProduct(product, amount, (double)p_amount);

					product.ProductionRecipesInternal.Add(recipe);
				}
			}

			foreach (var ingredientJToken in ingredients)
			{
				var iName = ingredientJToken["name"]?.Value<string>();
				var iType = ingredientJToken["type"]?.Value<string>();

				if (iName is null)
					throw new InvalidOperationException("Missing JSON key");

				ItemPrototype ingredient = (ItemPrototype)items[iName];
				double amount = ingredientJToken["amount"]?.Value<double>() ?? 0.0;
				if (amount != 0)
				{
					var min_temp = ingredientJToken["minimum_temperature"]?.Value<double>();
					var max_temp = ingredientJToken["maximum_temperature"]?.Value<double>();
					double minTemp = (iType == "fluid" && min_temp != null) ? (double)min_temp : double.NegativeInfinity;
					double maxTemp = (iType == "fluid" && max_temp != null) ? (double)max_temp : double.PositiveInfinity;
					if (minTemp < MinTemp) minTemp = double.NegativeInfinity;
					if (maxTemp > MaxTemp) maxTemp = double.PositiveInfinity;

					recipe.InternalOneWayAddIngredient(ingredient, amount, minTemp, maxTemp);
					ingredient.ConsumptionRecipesInternal.Add(recipe);
				}
			}

			if (allowed_effects != null) {
				var consumption = allowed_effects["consumption"]?.Value<bool>();
				var speed = allowed_effects["speed"]?.Value<bool>();
				var productivity = allowed_effects["productivity"]?.Value<bool>();
				var pollution = allowed_effects["pollution"]?.Value<bool>();
				var quality = allowed_effects["quality"]?.Value<bool>();

				if (consumption is null || speed is null || productivity is null || pollution is null || quality is null)
					throw new InvalidOperationException("Missing JSON key");

				recipe.AllowConsumptionBonus = (bool)consumption;
				recipe.AllowSpeedBonus = (bool)speed;
				recipe.AllowProductivityBonus = (bool)productivity;
				recipe.AllowPollutionBonus = (bool)pollution;
				recipe.AllowQualityBonus = (bool)quality;

                foreach (ModulePrototype module in modules.Values.Cast<ModulePrototype>())
                {
                    bool validModule = (recipe.AllowConsumptionBonus || module.ConsumptionBonus >= 0) &&
                                        (recipe.AllowSpeedBonus || module.SpeedBonus <= 0) &&
                                        (recipe.AllowProductivityBonus || module.ProductivityBonus <= 0) &&
                                        (recipe.AllowPollutionBonus || module.PollutionBonus >= 0) &&
                                        (recipe.AllowQualityBonus || module.QualityBonus <= 0);
                    if (validModule)
                    {
                        recipe.BeaconModulesInternal.Add(module);
                    }
                }

                if (allowed_module_categories == null || !allowed_module_categories.Any())
				{
					foreach (ModulePrototype module in modules.Values.Cast<ModulePrototype>())
					{
						bool validModule = (recipe.AllowConsumptionBonus || module.ConsumptionBonus >= 0) &&
											(recipe.AllowSpeedBonus || module.SpeedBonus <= 0) &&
											(recipe.AllowProductivityBonus || module.ProductivityBonus <= 0) &&
											(recipe.AllowPollutionBonus || module.PollutionBonus >= 0) &&
											(recipe.AllowQualityBonus || module.QualityBonus <= 0);
						if (validModule)
						{
							recipe.AssemblerModulesInternal.Add(module);
							module.RecipesInternal.Add(recipe);
						}
					}
				}
				else
				{
					foreach (string moduleCategory in allowed_module_categories.Select(a => ((JProperty)a).Name))
					{
						if (moduleCategories.TryGetValue(moduleCategory, out List<ModulePrototype>? value))
						{
							foreach (ModulePrototype module in value)
							{
								bool validModule = (recipe.AllowConsumptionBonus || module.ConsumptionBonus >= 0) &&
													(recipe.AllowSpeedBonus || module.SpeedBonus <= 0) &&
													(recipe.AllowProductivityBonus || module.ProductivityBonus <= 0) &&
													(recipe.AllowPollutionBonus || module.PollutionBonus >= 0) &&
													(recipe.AllowQualityBonus || module.QualityBonus <= 0);
								if (validModule)
								{
									recipe.AssemblerModulesInternal.Add(module);
									module.RecipesInternal.Add(recipe);
								}
							}
						}
					}
				}
			}

			recipes.Add(recipe.Name, recipe);
		}

		private static string GetExtractionRecipeName(string itemName) { return "§§r:e:" + itemName; }

		private void ProcessResource(JToken objJToken, Dictionary<string, List<RecipePrototype>> resourceCategories, List<IRecipe> miningWithFluidRecipes) {
			var products = objJToken["products"];
			if (products is null || !products.Any())
				return;

			var name = objJToken["name"]?.Value<string>();
			var localised_name = objJToken["localised_name"]?.Value<string>();
			var mining_time = objJToken["mining_time"]?.Value<double>();
			var product_first_type = products[0]!["type"]?.Value<string>();
			var required_fluid = objJToken["required_fluid"]?.Value<string>();
			var fluid_amount = objJToken["fluid_amount"]?.Value<double>() ?? 0.0;
			var resource_category = objJToken["resource_category"]?.Value<string>();

			if (name is null || localised_name is null || mining_time is null || product_first_type is null || resource_category is null)
				throw new InvalidOperationException("Missing JSON key");

			RecipePrototype recipe = new(
				this,
				GetExtractionRecipeName(name),
				localised_name + " Extraction",
				product_first_type == "fluid" ? extractionSubgroupFluids : extractionSubgroupItems,
				name) {
				Time = (double)mining_time
			};

			foreach (var productJToken in products)
			{
				var pName = productJToken["name"]?.Value<string>();
				var pAmount = productJToken["amount"]?.Value<double>() ?? 0.0;

				if (pName is null)
					throw new InvalidOperationException("Missing JSON key");

				if (!items.ContainsKey(pName) || pAmount <= 0)
					continue;
				ItemPrototype product = (ItemPrototype)items[pName];
				recipe.InternalOneWayAddProduct(product, pAmount, pAmount);
				product.ProductionRecipesInternal.Add(recipe);
			}

			if (recipe.ProductListInternal.Count == 0)
			{
				recipe.mySubgroup.recipes.Remove(recipe);
				return;
			}

			if (required_fluid != null && fluid_amount != 0)
			{
				ItemPrototype reqLiquid = (ItemPrototype)items[required_fluid];
				recipe.InternalOneWayAddIngredient(reqLiquid, fluid_amount);
				reqLiquid.ConsumptionRecipesInternal.Add(recipe);
				miningWithFluidRecipes.Add(recipe);
			}

			foreach (ModulePrototype module in modules.Values.Cast<ModulePrototype>()) //we will let the assembler sort out which module can be used with this recipe
			{
				module.RecipesInternal.Add(recipe);
				recipe.AssemblerModulesInternal.Add(module);
			}

			recipe.SetIconAndColor(new IconColorPair(recipe.ProductListInternal[0].Icon, recipe.ProductListInternal[0].AverageColor));

			if (!resourceCategories.ContainsKey(resource_category))
				resourceCategories.Add(resource_category, []);
			resourceCategories[resource_category].Add(recipe);

			//resource recipe will be processed when adding to miners (each miner that can use this recipe will have its recipe's techs added to unlock tech of the resource recipe)
			//this is for any non-fluid based resource! (fluid based item mining is locked behind research and processed in research function)
			//recipe.myUnlockTechnologies.Add(startingTech);
			//startingTech.unlockedRecipes.Add(recipe);

			recipes.Add(recipe.Name, recipe);
		}

		private void ProcessTechnology(JToken objJToken, Dictionary<string, IconColorPair> iconCache, List<IRecipe> miningWithFluidRecipes)
		{
			var name = objJToken["name"]?.Value<string>();
			var localised_name = objJToken["localised_name"]?.Value<string>();
			var icon_name = objJToken["icon_name"]?.Value<string>();
			var hidden = objJToken["hidden"]?.Value<bool>() ?? false;
			var enabled = objJToken["enabled"]?.Value<bool>() ?? true;
			var jRecipes = objJToken["recipes"]?.Select(recipe => recipe.Value<string>()).OfType<string>();
			var jQualities = objJToken["qualities"]?.ToList() ?? [];
			var research_unit_ingredients = objJToken["research_unit_ingredients"]?.ToList() ?? [];

			if (name is null || localised_name is null || icon_name is null || jRecipes is null)
				throw new InvalidOperationException("Missing JSON key");

			TechnologyPrototype technology = new(this, name, localised_name);

			if (iconCache.TryGetValue(icon_name, out IconColorPair? icon))
				technology.SetIconAndColor(icon);

			technology.Available = !hidden && enabled; //not sure - factorio documentation states 'enabled' means 'available at start', but in this case 'enabled' being false seems to represent the technology not appearing on screen (same as hidden)??? I will just work with what tests show -> tech is available if it is enabled & not hidden.

			foreach (string recipe in jRecipes)
			{
				if (recipes.TryGetValue(recipe, out IRecipe? recipeProto))
				{
					((RecipePrototype)recipeProto).MyUnlockTechnologiesInternal.Add(technology);
					technology.UnlockedRecipesInternal.Add((RecipePrototype)recipeProto);
				}
			}

            foreach (var qualityName in jQualities)
            {
                if (qualityName.Value<string>() is string qName && qualities.TryGetValue(qName, out IQuality? quality))
                {
                    ((QualityPrototype)quality).MyUnlockTechnologiesInternal.Add(technology);
                    technology.UnlockedQualitiesInternal.Add((QualityPrototype)quality);
                }
            }

            if (objJToken["unlocks-mining-with-fluid"] != null)
            {
				foreach(RecipePrototype recipe in miningWithFluidRecipes.Cast<RecipePrototype>())
				{
					recipe.MyUnlockTechnologiesInternal.Add(technology);
					technology.UnlockedRecipesInternal.Add(recipe);
				}
			}

			foreach (var ingredientJToken in research_unit_ingredients)
			{
				var iname = ingredientJToken["name"]?.Value<string>();
				var amount = ingredientJToken["amount"]?.Value<double>() ?? 0.0;

				if (iname is null)
					throw new InvalidOperationException("Missing JSON key");

				if (amount != 0)
				{
					technology.InternalOneWayAddSciPack((ItemPrototype)items[iname], amount);
					((ItemPrototype)items[iname]).ConsumptionTechnologiesInternal.Add(technology);
				}
			}

			technologies.Add(technology.Name, technology);
		}

		private void ProcessTechnologyP2(JToken objJToken)
		{
			var name = objJToken["name"]?.Value<string>();
			var prerequisites = objJToken["prerequisites"]?.ToList() ?? [];

			if (name is null)
				throw new InvalidOperationException("Missing JSON key");

			TechnologyPrototype technology = (TechnologyPrototype)technologies[name];
			foreach (var pKey in prerequisites)
			{
				if (pKey.Value<string>() is string prerequisite && technologies.TryGetValue(prerequisite, out ITechnology? techProto))
				{
					technology.PrerequisitesInternal.Add((TechnologyPrototype)techProto);
					((TechnologyPrototype)techProto).PostTechsInternal.Add(technology);
				}
			}
			if(technology.PrerequisitesInternal.Count == 0) //entire tech tree will stem from teh 'startingTech' node.
			{
				technology.PrerequisitesInternal.Add(startingTech);
				startingTech.PostTechsInternal.Add(technology);
			}
		}

		private void ProcessCharacter(JToken objJtoken, Dictionary<string, List<RecipePrototype>> craftingCategories)
		{
			AssemblerAdditionalProcessing(objJtoken, playerAssember, craftingCategories);
			assemblers.Add(playerAssember.Name, playerAssember);
		}

		private void ProcessEntity(JToken objJToken, Dictionary<string, IconColorPair> iconCache, Dictionary<string, List<RecipePrototype>> craftingCategories, Dictionary<string, List<RecipePrototype>> resourceCategories, Dictionary<string, List<ItemPrototype>> fuelCategories, List<IRecipe> miningWithFluidRecipes, Dictionary<string, List<ModulePrototype>> moduleCategories) {
			var type = objJToken["type"]?.Value<string>();
			var name = objJToken["name"]?.Value<string>();
			var icon_name = objJToken["icon_name"]?.Value<string>();
			var icon_alt_name = objJToken["icon_alt_name"]?.Value<string>();
			var localised_name = objJToken["localised_name"]?.Value<string>();

			if (type is null || name is null || localised_name is null || icon_name is null || icon_alt_name is null)
				throw new InvalidOperationException("Missing JSON key");
			if (type == "character") //character is processed later
				return;

			string fuel_type = objJToken["fuel_type"]?.Value<string>() ?? "";

			EntityObjectBasePrototype entity;
			EnergySource esource =
				(fuel_type == "item") ? EnergySource.Burner :
				(fuel_type == "fluid") ? EnergySource.FluidBurner :
				(fuel_type == "electricity") ? EnergySource.Electric :
				(fuel_type == "heat") ? EnergySource.Heat : EnergySource.Void;
			EntityType etype =
				type == "beacon" ? EntityType.Beacon :
				type == "mining-drill" ? EntityType.Miner :
				type == "offshore-pump" ? EntityType.OffshorePump :           
				type == "furnace" || type == "assembling-machine" || type == "rocket-silo" ? EntityType.Assembler :
				type == "boiler" ? EntityType.Boiler :
				type == "generator" ? EntityType.Generator :
				type == "burner-generator" ? EntityType.BurnerGenerator :
				type == "reactor" ? EntityType.Reactor : EntityType.ERROR;
			if (etype == EntityType.ERROR)
				Trace.Fail(string.Format("Unexpected type of entity ({0} in json data!", type));

			if (etype == EntityType.Beacon)
			{
				entity = new BeaconPrototype(this,
					name,
					localised_name,
					esource,
					false);
			}
			else
			{
				entity = new AssemblerPrototype(this,
					name,
					localised_name,
					etype,
					esource,
					false);
			}

			//icons
			if (iconCache.TryGetValue(icon_name, out IconColorPair? iconColPair))
				entity.SetIconAndColor(iconColPair);
			else if (iconCache.TryGetValue(icon_alt_name, out IconColorPair? altColPair))
				entity.SetIconAndColor(altColPair);

			//associated items
			if (objJToken["items_to_place_this"] is JToken items_to_place_this)
				foreach (var item in items_to_place_this.Select(i => i.Value<string>()).OfType<string>())
					if (items.TryGetValue(item, out IItem? value))
						entity.AssociatedItemsInternal.Add((ItemPrototype)value);

			//base parameters
			if (objJToken["q_speed"] is JToken q_speed)
			{
				foreach (JToken speedToken in q_speed) {
					var quality = speedToken["quality"]?.Value<string>();
					var value = speedToken["value"]?.Value<double>();
					if (quality is null || value is null)
						throw new InvalidOperationException("Missing JSON key");
					entity.Speed.Add(qualities[quality], (double)value);
				}
			}
			else if (objJToken["speed"]?.Value<double>() is double speed)
			{
				foreach (IQuality quality in qualities.Values)
					entity.Speed.Add(quality, speed);
			}

			entity.ModuleSlots = objJToken["module_inventory_size"]?.Value<int>() ?? 0;

			//modules
			if (entity.EntityType == EntityType.Assembler || entity.EntityType == EntityType.Miner || entity.EntityType == EntityType.Rocket || entity.EntityType == EntityType.Beacon)
			{
                if (entity is AssemblerPrototype prototype)
				{
					prototype.BaseConsumptionBonus = objJToken["base_module_effects"]?["consumption"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key");
					prototype.BaseSpeedBonus = objJToken["base_module_effects"]?["speed"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key");
					prototype.BaseProductivityBonus = objJToken["base_module_effects"]?["productivity"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key");
					prototype.BasePollutionBonus = objJToken["base_module_effects"]?["pollution"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key");
					prototype.BaseQualityBonus = objJToken["base_module_effects"]?["quality"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key");
					prototype.AllowModules = objJToken["uses_module_effects"]?.Value<bool>() ?? throw new InvalidOperationException("Missing JSON key");
					prototype.AllowBeacons = objJToken["uses_beacon_effects"]?.Value<bool>() ?? throw new InvalidOperationException("Missing JSON key");
				}

                if (objJToken["allowed_effects"] is JToken allowed_effects)
                {
                    bool allow_consumption = allowed_effects["consumption"]?.Value<bool>() ?? throw new InvalidOperationException("Missing JSON key");
                    bool allow_speed = allowed_effects["speed"]?.Value<bool>() ?? throw new InvalidOperationException("Missing JSON key");
                    bool alllow_productivity = allowed_effects["productivity"]?.Value<bool>() ?? throw new InvalidOperationException("Missing JSON key");
                    bool allow_pollution = allowed_effects["pollution"]?.Value<bool>() ?? throw new InvalidOperationException("Missing JSON key");
                    bool allow_quality = allowed_effects["quality"]?.Value<bool>() ?? throw new InvalidOperationException("Missing JSON key");

                    if (objJToken["allowed_module_categories"] is not JToken allowed_module_categories || !allowed_module_categories.Any())
                    {
                        foreach (ModulePrototype module in modules.Values.Cast<ModulePrototype>())
                        {
                            bool validModule = (allow_consumption || module.ConsumptionBonus >= 0) &&
                                                (allow_speed || module.SpeedBonus <= 0) &&
                                                (alllow_productivity || module.ProductivityBonus <= 0) &&
                                                (allow_pollution || module.PollutionBonus >= 0) &&
                                                (allow_quality || module.QualityBonus <= 0);
                            if (validModule)
                            {
                                entity.ModulesInternal.Add(module);
                                if (entity is AssemblerPrototype aEntity)
                                    module.AssemblersInternal.Add(aEntity);
                                else if (entity is BeaconPrototype bEntity)
                                    module.BeaconsInternal.Add(bEntity);
                            }
                        }
                    } else
                    {
                        foreach (string moduleCategory in allowed_module_categories.Select(a => ((JProperty)a).Name))
                        {
                            if (moduleCategories.TryGetValue(moduleCategory, out List<ModulePrototype>? value))
                            {
                                foreach (ModulePrototype module in value)
                                {
                                    bool validModule = (allow_consumption || module.ConsumptionBonus >= 0) &&
                                                        (allow_speed || module.SpeedBonus <= 0) &&
                                                        (alllow_productivity || module.ProductivityBonus <= 0) &&
                                                        (allow_pollution || module.PollutionBonus >= 0) &&
                                                        (allow_quality || module.QualityBonus <= 0);
                                    if (validModule)
                                    {
                                        entity.ModulesInternal.Add(module);
                                        if (entity is AssemblerPrototype aEntity)
                                            module.AssemblersInternal.Add(aEntity);
                                        else if (entity is BeaconPrototype bEntity)
                                            module.BeaconsInternal.Add(bEntity);
                                    }
                                }
                            }
                        }
                    }
                }
			}

			//energy types
			EntityEnergyFurtherProcessing(objJToken, entity, fuelCategories);

			//assembler / beacon specific parameters
			if (etype == EntityType.Beacon)
			{
				BeaconPrototype bEntity = (BeaconPrototype)entity;

				if (BeaconAdditionalProcessing(objJToken, bEntity))
					beacons.Add(bEntity.Name, bEntity);
			}
			else
			{
				AssemblerPrototype aEntity = (AssemblerPrototype)entity;

				bool success = false;
				switch (etype)
				{
					case EntityType.Assembler:
						success = AssemblerAdditionalProcessing(objJToken, aEntity, craftingCategories);
						break;
					case EntityType.Boiler:
						success = BoilerAdditionalProcessing(objJToken, aEntity);
						break;
					case EntityType.BurnerGenerator:
						success = BurnerGeneratorAdditionalProcessing(aEntity);
						break;
					case EntityType.Generator:
						success = GeneratorAdditionalProcessing(objJToken, aEntity);
						break;
					case EntityType.Miner:
						success = MinerAdditionalProcessing(objJToken, aEntity, resourceCategories, miningWithFluidRecipes);
						break;
					case EntityType.OffshorePump:
						success = OffshorePumpAdditionalProcessing(objJToken, aEntity, resourceCategories["<<foreman_resource_category_water_tile>>"]);
						break;
					case EntityType.Reactor:
						success = ReactorAdditionalProcessing(objJToken, aEntity);
						break;
				}
				if (success)
					assemblers.Add(aEntity.Name, aEntity);
			}
		}

		private void EntityEnergyFurtherProcessing(JToken objJToken, EntityObjectBasePrototype entity, Dictionary<string, List<ItemPrototype>> fuelCategories)
		{
			entity.ConsumptionEffectivity = objJToken["fuel_effectivity"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key");

			//pollution
			if(objJToken is JObject objJObject)
			{
				Dictionary<string, double> pollutions = objJObject["pollution"]?.ToObject<Dictionary<string, double>>() ?? throw new InvalidOperationException("Missing JSON key");
				foreach (KeyValuePair<string, double> pollution in pollutions)
					entity.PollutionInternal.Add(pollution.Key, pollution.Value);
			}

			//energy production
			foreach (JToken speedToken in objJToken["q_energy_production"]?.ToList() ?? [])
				entity.EnergyProduction.Add(qualities[speedToken["quality"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key")], speedToken["value"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key"));

			//energy consumption
			entity.energyDrain = objJToken["drain"]?.Value<double>() ?? 0; //seconds
			foreach (JToken speedToken in objJToken["q_max_energy_usage"]?.ToList() ?? [])
				entity.EnergyConsumption.Add(qualities[speedToken["quality"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key")], speedToken["value"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key"));

			//fuel processing
			switch (entity.EnergySource)
			{
				case EnergySource.Burner:
					foreach (var categoryJToken in objJToken["fuel_categories"]?.ToList() ?? [])
					{
						var category = categoryJToken.Value<string>() ?? throw new InvalidOperationException("Missing JSON key");
						if (fuelCategories.TryGetValue(category, out List<ItemPrototype>? itemProtos))
						{
							foreach (ItemPrototype item in itemProtos)
							{
								entity.FuelsInternal.Add(item);
								item.FuelsEntitiesInternal.Add(entity);
							}
						}
					}
					break;

				case EnergySource.FluidBurner:
					entity.IsTemperatureFluidBurner = !(objJToken["burns_fluid"]?.Value<bool>() ?? false);
					entity.FluidFuelTemperatureRange = new FRange(objJToken["minimum_fuel_temperature"]?.Value<double>() ?? double.NegativeInfinity, objJToken["maximum_fuel_temperature"]?.Value<double>() ?? double.PositiveInfinity);
					string? fuelFilter = objJToken["fuel_filter"]?.Value<string>() ?? null;

					if (fuelFilter != null)
					{
						ItemPrototype fuel = (ItemPrototype)items[fuelFilter];
						if (entity.IsTemperatureFluidBurner || fuelCategories["§§fc:liquids"].Contains(fuel))
						{
							entity.FuelsInternal.Add(fuel);
							fuel.FuelsEntitiesInternal.Add(entity);
						}
						//else
						//	; //there is no valid fuel for this entity. Realistically this means it cant be used. It will thus have an error when placed (no fuel selected -> due to no fuel existing)
					}
					else if(!entity.IsTemperatureFluidBurner)
					{
						//add in all liquid fuels
						foreach (ItemPrototype fluid in fuelCategories["§§fc:liquids"])
						{
							entity.FuelsInternal.Add(fluid);
							fluid.FuelsEntitiesInternal.Add(entity);
						}
					}
					else //ok, this is a bit of a FK U, but this basically means this entity can burn any fluid, and burns it as a temperature range. This is how the old steam generators worked (where you could feed in hot sulfuric acid and it would just burn through it no problem). If you want to use it, fine. Here you go.
					{
						foreach(FluidPrototype fluid in items.Values.Where(i => i is IFluid).Cast<FluidPrototype>())
						{
							entity.FuelsInternal.Add(fluid);
							fluid.FuelsEntitiesInternal.Add(entity);
						}
					}
					break;

				case EnergySource.Heat:
					entity.FuelsInternal.Add(HeatItem);
					HeatItem.FuelsEntitiesInternal.Add(entity);
					break;

				case EnergySource.Electric:
					break;

				case EnergySource.Void:
				default:
					break;
			}
		}

		private static bool BeaconAdditionalProcessing(JToken objJToken, BeaconPrototype bEntity)
		{
			bEntity.DistributionEffectivity = objJToken["distribution_effectivity"]?.Value<double>() ?? 0.5f;
			bEntity.DistributionEffectivityQualityBoost = objJToken["distribution_effectivity_bonus_per_quality_level"]?.Value<double>() ?? 0;

			if (objJToken["profile"] is JToken profile)
			{
				int quantity = 1;
				double lastProfile = 0.5f;
				foreach(var profileJToken in profile)
				{
					lastProfile = (double)profileJToken;
					bEntity.Profile[quantity] = lastProfile;

					quantity++;
					if (quantity >= bEntity.Profile.Length)
						break;
				}
				while(quantity < bEntity.Profile.Length)
				{
					bEntity.Profile[quantity] = lastProfile;
					quantity++;
				}
				bEntity.Profile[0] = bEntity.Profile[1]; //helps with calculating partial beacon values (ex: 0.5 beacons)
			}

			return true;
		}

		private static bool AssemblerAdditionalProcessing(JToken objJToken, AssemblerPrototype aEntity, Dictionary<string, List<RecipePrototype>> craftingCategories) //recipe user
		{
			foreach (var categoryJToken in objJToken["crafting_categories"]?.ToList() ?? [])
			{
				var category = categoryJToken.Value<string>() ?? throw new InvalidOperationException("Missing JSON key");
				if (craftingCategories.TryGetValue(category, out List<RecipePrototype>? recipeProtos))
				{
					foreach (RecipePrototype recipe in recipeProtos)
					{
						if (TestRecipeEntityPipeFit(recipe, objJToken))
						{
							recipe.AssemblersInternal.Add(aEntity);
							aEntity.RecipesInternal.Add(recipe);
						}
					}
				}
			}
			return true;
		}

		private bool MinerAdditionalProcessing(JToken objJToken, AssemblerPrototype aEntity, Dictionary<string, List<RecipePrototype>> resourceCategories, List<IRecipe> miningWithFluidRecipes) //resource provider
		{
			foreach (var categoryJToken in objJToken["resource_categories"]?.ToList() ?? [])
			{
				var category = categoryJToken.Value<string>() ?? throw new InvalidOperationException("Missing JSON key");
				if (resourceCategories.TryGetValue(category, out List<RecipePrototype>? recipeProtos))
				{
					foreach (RecipePrototype recipe in recipeProtos)
					{
						if (TestRecipeEntityPipeFit(recipe, objJToken))
						{
							if(!miningWithFluidRecipes.Contains(recipe))
								ProcessEntityRecipeTechlink(aEntity, recipe);

							recipe.AssemblersInternal.Add(aEntity);
							aEntity.RecipesInternal.Add(recipe);
						}
					}
				}
			}
			return true;
		}

		private bool OffshorePumpAdditionalProcessing(JToken objJToken, AssemblerPrototype aEntity, List<RecipePrototype> waterPumpRecipes)
		{
			//check if the pump has a specified 'output' fluid preset. if yes then only that recipe is added to it; if not then all water tile resource recipes are added
			List<string> outPipeFilters = objJToken["out_pipe_filters"]?.Select(o => o.Value<string>())?.OfType<string>()?.ToList() ?? [];

			if (outPipeFilters.Count != 0)
			{
				if (recipes.TryGetValue(GetExtractionRecipeName(outPipeFilters[0]), out IRecipe? extractionRecipe))
				{
					ProcessEntityRecipeTechlink(aEntity, (RecipePrototype)extractionRecipe);
					((RecipePrototype)extractionRecipe).AssemblersInternal.Add(aEntity);
					aEntity.RecipesInternal.Add((RecipePrototype)extractionRecipe);
				}
				else
				{
					//add new recipe
					if (!items.TryGetValue(outPipeFilters[0], out IItem? extractionFluid))
						return false;

					RecipePrototype recipe = new(
						this,
						GetExtractionRecipeName(outPipeFilters[0]),
						extractionFluid.FriendlyName + " Extraction",
						extractionSubgroupFluids,
						extractionFluid.Name) {
						Time = 1
					};

					recipe.InternalOneWayAddProduct((ItemPrototype)extractionFluid, 60, 60);
					((ItemPrototype)extractionFluid).ProductionRecipesInternal.Add(recipe);

					recipe.SetIconAndColor(new IconColorPair(recipe.ProductListInternal[0].Icon, recipe.ProductListInternal[0].AverageColor));

					recipes.Add(recipe.Name, recipe);
				}
			}
			else
			{
				foreach (RecipePrototype recipe in waterPumpRecipes)
				{
					ProcessEntityRecipeTechlink(aEntity, recipe);
					recipe.AssemblersInternal.Add(aEntity);
					aEntity.RecipesInternal.Add(recipe);
				}
			}

			return true;
		}

		private bool BoilerAdditionalProcessing(JToken objJToken, AssemblerPrototype aEntity) //Uses whatever the default energy source of it is to convert water into steam of a given temperature
		{
			if (objJToken["fluid_ingredient"]?.Value<string>() is not string fluid_ingredient || objJToken["fluid_product"]?.Value<string>() is not string fluid_product)
				return false;
			FluidPrototype ingredient = (FluidPrototype)items[fluid_ingredient];
			FluidPrototype product = (FluidPrototype)items[fluid_product];

			//boiler is a ingredient to product conversion with product coming out at the  target_temperature *C at a rate based on energy efficiency & energy use to bring the INGREDIENT to the given temperature (basically ingredient goes from default temp to target temp, then shifts to product). we will add an extra recipe for this
			double temp = objJToken["target_temperature"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key"); ;

			//I will be honest here. Testing has shown that the actual 'speed' is dependent on the incoming temperature (not the default temperature), as could have likely been expected.
			//this means that if you put in 65* water instead of 15* water to boil it to 165* steam it will result in 1.5x the 'maximum' output as listed in the factorio info menu and calculated below.
			//so if some mod does some wonky things like water pre-heating, or uses boiler to heat other fluids at non-default temperatures (I havent found any such mods, but testing shows it is possible to make such a mod)
			//then the values calculated here will be wrong.
			//Still, for now I will leave it as is.
			if (ingredient.SpecificHeatCapacity == 0)
			{
				foreach (IQuality quality in qualities.Values)
					aEntity.Speed.Add(quality, 0);
			} else
			{
                foreach (IQuality quality in qualities.Values)
                    aEntity.Speed.Add(quality, (double)(aEntity.GetEnergyConsumption(quality) / ((temp - ingredient.DefaultTemperature) * ingredient.SpecificHeatCapacity * 60))); //by placing this here we can keep the recipe as a 1 sec -> 60 production, simplifying recipe comparing for presets.
			}

			RecipePrototype recipe;
			string boilRecipeName = string.Format("§§r:b:{0}:{1}:{2}", ingredient.Name, product.Name, temp.ToString());
			if (!recipes.TryGetValue(boilRecipeName, out IRecipe? value))
			{
				recipe = new RecipePrototype(
					this,
					boilRecipeName,
					ingredient == product ? string.Format("{0} boiling to {1}°c", ingredient.FriendlyName, temp.ToString()) : string.Format("{0} boiling to {1}°c {2}", ingredient.FriendlyName, temp.ToString(), product.FriendlyName),
					energySubgroupBoiling,
					boilRecipeName);

				recipe.SetIconAndColor(new IconColorPair(IconCache.ConbineIcons(ingredient.Icon, product.Icon, ingredient.Icon.Height), product.AverageColor));

				recipe.Time = 1;

				recipe.InternalOneWayAddIngredient(ingredient, 60);
				ingredient.ConsumptionRecipesInternal.Add(recipe);

				double productQuantity = 60 * ingredient.SpecificHeatCapacity / product.SpecificHeatCapacity;
				recipe.InternalOneWayAddProduct(product, productQuantity, productQuantity, temp);
				product.ProductionRecipesInternal.Add(recipe);


				foreach (ModulePrototype module in modules.Values.Cast<ModulePrototype>()) //we will let the assembler sort out which module can be used with this recipe
				{
					module.RecipesInternal.Add(recipe);
					recipe.AssemblerModulesInternal.Add(module);
				}

				recipes.Add(recipe.Name, recipe);
			}
			else
				recipe = (RecipePrototype)value;

			ProcessEntityRecipeTechlink(aEntity, recipe);
			recipe.AssemblersInternal.Add(aEntity);
			aEntity.RecipesInternal.Add(recipe);

			return true;
		}

		private bool GeneratorAdditionalProcessing(JToken objJToken, AssemblerPrototype aEntity) //consumes steam (at the provided temperature up to the given maximum) to generate electricity
		{
			if (objJToken["fluid_ingredient"]?.Value<string>() is not string fluid_ingredient)
				return false;
			FluidPrototype ingredient = (FluidPrototype)items[fluid_ingredient];

			double baseSpeed = (objJToken["fluid_usage_per_sec"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key")) / 60.0; //use 60 multiplier to make recipes easier
			double baseEnergyProduction = objJToken["max_power_output"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key"); //in seconds

			foreach (IQuality quality in qualities.Values)
				aEntity.Speed.Add(quality, baseSpeed * aEntity.GetEnergyProduction(quality) / baseEnergyProduction);

			aEntity.OperationTemperature = objJToken["full_power_temperature"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key"); ;
			double minTemp = objJToken["minimum_temperature"]?.Value<double>() ?? double.NaN;
			double maxTemp = objJToken["maximum_temperature"]?.Value<double>() ?? double.NaN;
			if (!double.IsNaN(minTemp) && minTemp < ingredient.DefaultTemperature) minTemp = ingredient.DefaultTemperature;
			if (!double.IsNaN(maxTemp) && maxTemp > MaxTemp) maxTemp = double.NaN;

			//actual energy production is a bit more complicated here (as it involves actual temperatures), but we will have to handle it in the graph (after all values have been calculated and we know the amounts and temperatures getting passed here, we can calc the energy produced)

			RecipePrototype recipe;
			string generationRecipeName = string.Format("§§r:g:{0}:{1}>{2}", ingredient.Name, minTemp, maxTemp);
			if (!recipes.TryGetValue(generationRecipeName, out IRecipe? value))
			{
				recipe = new RecipePrototype(
					this,
					generationRecipeName,
					string.Format("{0} to Electricity", ingredient.FriendlyName),
					energySubgroupEnergy,
					generationRecipeName);

				recipe.SetIconAndColor(new IconColorPair(IconCache.ConbineIcons(ingredient.Icon, ElectricityIcon, ingredient.Icon.Height, false), ingredient.AverageColor));

				recipe.Time = 1;

				recipe.InternalOneWayAddIngredient(ingredient, 60, double.IsNaN(minTemp) ? double.NegativeInfinity : minTemp, double.IsNaN(maxTemp) ? double.PositiveInfinity : maxTemp);

				ingredient.ConsumptionRecipesInternal.Add(recipe);

				foreach (ModulePrototype module in modules.Values.Cast<ModulePrototype>()) //we will let the assembler sort out which module can be used with this recipe
				{
					module.RecipesInternal.Add(recipe);
					recipe.AssemblerModulesInternal.Add(module);
				}

				recipes.Add(recipe.Name, recipe);
			}
			else
				recipe = (RecipePrototype)value;

			ProcessEntityRecipeTechlink(aEntity, recipe);
			recipe.AssemblersInternal.Add(aEntity);
			aEntity.RecipesInternal.Add(recipe);

			return true;
		}

		private bool BurnerGeneratorAdditionalProcessing(AssemblerPrototype aEntity) //consumes fuel to generate electricity
		{
			aEntity.RecipesInternal.Add(BurnerRecipe);
			BurnerRecipe.AssemblersInternal.Add(aEntity);
			ProcessEntityRecipeTechlink(aEntity, BurnerRecipe);

			foreach (IQuality quality in qualities.Values)
				aEntity.Speed.Add(quality, 1f); //doesnt matter - recipe is empty

			return true;
		}

		private bool ReactorAdditionalProcessing(JToken objJToken, AssemblerPrototype aEntity)
		{
			aEntity.NeighbourBonus = objJToken["neighbour_bonus"]?.Value<double>() ?? 0;
			aEntity.RecipesInternal.Add(HeatRecipe);
			HeatRecipe.AssemblersInternal.Add(aEntity);
			ProcessEntityRecipeTechlink(aEntity, HeatRecipe);

			foreach (IQuality quality in qualities.Values)
				aEntity.Speed.Add(quality, (aEntity.GetEnergyConsumption(quality)) / HeatItem.FuelValue); //the speed of producing 1MJ of energy as heat for this reactor based on quality

			return true;
		}

		private void ProcessEntityRecipeTechlink(EntityObjectBasePrototype entity, RecipePrototype recipe)
		{
			if (entity.AssociatedItemsInternal.Count == 0)
			{
				recipe.MyUnlockTechnologiesInternal.Add(startingTech);
				startingTech.UnlockedRecipesInternal.Add(recipe);
			}
			else
			{
				foreach (IItem placeItem in entity.AssociatedItemsInternal)
				{
					foreach (IRecipe placeItemRecipe in placeItem.ProductionRecipes)
					{
						foreach (TechnologyPrototype tech in placeItemRecipe.MyUnlockTechnologies.Cast<TechnologyPrototype>())
						{
							recipe.MyUnlockTechnologiesInternal.Add(tech);
							tech.UnlockedRecipesInternal.Add(recipe);
						}
					}
				}
			}
		}

		private static bool TestRecipeEntityPipeFit(RecipePrototype recipe, JToken objJToken) //returns true if the fluid boxes of the entity (assembler or miner) can accept the provided recipe (with its in/out fluids)
		{
			int inPipes = objJToken["in_pipes"]?.Value<int>() ?? throw new InvalidOperationException("Missing JSON key");
			List<string> inPipeFilters = objJToken["in_pipe_filters"]?.Select(o => o.Value<string>()).OfType<string>().ToList() ?? throw new InvalidOperationException("Missing JSON key"); ;
			int outPipes = objJToken["out_pipes"]?.Value<int>() ?? throw new InvalidOperationException("Missing JSON key");
			List<string> outPipeFilters = objJToken["out_pipe_filters"]?.Select(o => o.Value<string>()).OfType<string>().ToList() ?? throw new InvalidOperationException("Missing JSON key"); ;
			int ioPipes = objJToken["io_pipes"]?.Value<int>() ?? throw new InvalidOperationException("Missing JSON key");
			List<string> ioPipeFilters = objJToken["io_pipe_filters"]?.Select(o => o.Value<string>()).OfType<string>().ToList() ?? throw new InvalidOperationException("Missing JSON key");

			int inCount = 0; //unfiltered
			int outCount = 0; //unfiltered
			foreach(ItemPrototype inFluid in recipe.IngredientListInternal.Where(i => i is IFluid))
			{
				if (inPipeFilters.Contains(inFluid.Name))
				{
					inPipes--;
					inPipeFilters.Remove(inFluid.Name);
				}
				else if (ioPipeFilters.Contains(inFluid.Name))
				{
					ioPipes--;
					ioPipeFilters.Remove(inFluid.Name);
				}
				else
					inCount++;
			}
			foreach (ItemPrototype outFluid in recipe.ProductListInternal.Where(i => i is IFluid))
			{
				if (outPipeFilters.Contains(outFluid.Name))
				{
					outPipes--;
					outPipeFilters.Remove(outFluid.Name);
				}
				else if (ioPipeFilters.Contains(outFluid.Name))
				{
					ioPipes--;
					ioPipeFilters.Remove(outFluid.Name);
				}
				else
					outCount++;
			}
			//remove any unused filtered pipes from the equation - they cant be used due to the filters.
			inPipes -= inPipeFilters.Count;
			ioPipes -= ioPipeFilters.Count;
			outPipes -= outPipeFilters.Count;

			//return true if the remaining unfiltered ingredients & products (fluids) can fit into the remaining unfiltered pipes
			return (inCount - inPipes <= ioPipes && outCount - outPipes <= ioPipes && inCount + outCount <= inPipes + outPipes + ioPipes); 
		}

		private void ProcessRocketLaunch(JToken objJToken)
		{
			if (!items.TryGetValue("rocket-part", out IItem? rocketPartItem) || !recipes.TryGetValue("rocket-part", out IRecipe? rocketPartRecipe) || !assemblers.ContainsKey("rocket-silo"))
			{
				ErrorLogging.LogLine(string.Format("No Rocket silo / rocket part found! launch product for {0} will be ignored.", objJToken["name"]?.Value<string>() ?? "<JSON KEY MISSING>"));
				return;
			}

			ItemPrototype rocketPartItemProto = (ItemPrototype)rocketPartItem;
			RecipePrototype rocketPartRecipeProto = (RecipePrototype)rocketPartRecipe;
			ItemPrototype launchItem = (ItemPrototype)items[objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key")];

			RecipePrototype recipe = new(
				this,
				string.Format("§§r:rl:launch-{0}", launchItem.Name),
				string.Format("Rocket Launch: {0}", launchItem.FriendlyName),
				rocketLaunchSubgroup,
				launchItem.Name) {
				Time = 1 //placeholder really...
			};

			//process products - have to calculate what the maximum input size of the launch item is so as not to waste any products (ex: you can launch 2000 science packs, but you will only get 100 fish. so input size must be set to 100 -> 100 science packs to 100 fish)
			int inputSize = launchItem.StackSize;
			Dictionary<ItemPrototype, double> products = [];
			Dictionary<ItemPrototype, double> productTemp = [];
			foreach (var productJToken in objJToken["rocket_launch_products"]?.ToList() ?? [])
			{
				ItemPrototype product = (ItemPrototype)items[productJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key")];
				double amount = productJToken["amount"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key");
				if (amount != 0)
				{
					if (inputSize * amount > product.StackSize)
						inputSize = (int)(product.StackSize / amount);

					amount = inputSize * amount;

					if ((productJToken["type"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key")) == "fluid")
						productTemp.Add(product, productJToken["temperature"]?.Value<double>() ?? ((FluidPrototype)product).DefaultTemperature);
					products.Add(product, amount);

					product.ProductionRecipesInternal.Add(recipe);
					recipe.SetIconAndColor(new IconColorPair(product.Icon, Color.DarkGray));
				}
			}
			foreach (ItemPrototype product in products.Keys)
				recipe.InternalOneWayAddProduct(product, inputSize * products[product], 0, productTemp.TryGetValue(product, out double value) ? value : double.NaN);

			recipe.InternalOneWayAddIngredient(launchItem, inputSize);
			launchItem.ConsumptionRecipesInternal.Add(recipe);

			recipe.InternalOneWayAddIngredient(rocketPartItemProto, 100);
			rocketPartItemProto.ConsumptionRecipesInternal.Add(recipe);

			foreach (TechnologyPrototype tech in rocketPartRecipeProto.MyUnlockTechnologiesInternal)
			{
				recipe.MyUnlockTechnologiesInternal.Add(tech);
				tech.UnlockedRecipesInternal.Add(recipe);
			}

			recipe.AssemblersInternal.Add(rocketAssembler);
			rocketAssembler.RecipesInternal.Add(recipe);

			recipes.Add(recipe.Name, recipe);
		}

		//------------------------------------------------------Finalization steps of LoadAllData (cleanup and cyclic checks)

		private void ProcessSciencePacks()
		{
			//DFS for processing the required sci packs of each technology. Basically some research only requires 1 sci pack, but to unlock it requires researching tech with many sci packs. Need to account for that
			Dictionary<TechnologyPrototype, HashSet<IItem>> techRequirements = [];
			HashSet<IItem> sciPacks = [];
			HashSet<IItem> TechRequiredSciPacks(TechnologyPrototype tech)
			{
				if (techRequirements.TryGetValue(tech, out HashSet<IItem>? value))
					return value;

				HashSet<IItem> requiredItems = new(tech.SciPackListInternal);
				foreach (TechnologyPrototype prereq in tech.PrerequisitesInternal)
					foreach (IItem sciPack in TechRequiredSciPacks(prereq))
						requiredItems.Add(sciPack);

				sciPacks.UnionWith(requiredItems);
				techRequirements.Add(tech, requiredItems);

				return requiredItems;
			}

			//tech ordering - set each technology's 'tier' to be its furthest distance from the 'starting tech' node
			HashSet<TechnologyPrototype> visitedTech =
			[
				startingTech, //tier 0, everything starts from here.
			];
			int GetTechnologyTier(TechnologyPrototype tech)
			{
				if (!visitedTech.Contains(tech))
				{
					int maxPrerequisiteTier = 0;
					foreach (TechnologyPrototype prereq in tech.PrerequisitesInternal)
						maxPrerequisiteTier = Math.Max(maxPrerequisiteTier, GetTechnologyTier(prereq));
					tech.Tier = maxPrerequisiteTier + 1;
					visitedTech.Add(tech);
				}
				return tech.Tier;
			}

			//science pack processing - DF again where we want to calculate which science packs are required to get to the given science pack
			HashSet<IItem> visitedPacks = [];
			void UpdateSciencePackPrerequisites(IItem sciPack)
			{
				if (visitedPacks.Contains(sciPack))
					return;

				//for simplicities sake we will only account for prerequisites of the first available production recipe (or first non-available if no available production recipes exist). This means that if (for who knows what reason) there are multiple valid production recipes only the first one will count!
				HashSet<IItem> prerequisites = new(sciPack.ProductionRecipes.OrderByDescending(r => r.Available).FirstOrDefault()?.MyUnlockTechnologies.OrderByDescending(t => t.Available).FirstOrDefault()?.SciPackList ?? []);
				foreach (IRecipe r in sciPack.ProductionRecipes)
					foreach (ITechnology t in r.MyUnlockTechnologies)
						prerequisites.IntersectWith(t.SciPackList);

				//prerequisites now contains all the immediate required sci packs. we will now Update their prerequisites via this function, then add their prerequisites to our own set before finalizing it.
				foreach (IItem prereq in prerequisites.ToList())
				{
					UpdateSciencePackPrerequisites(prereq);
					prerequisites.UnionWith(sciencePackPrerequisites[prereq]);
				}
				sciencePackPrerequisites.Add(sciPack, prerequisites);
				visitedPacks.Add(sciPack);
			}

			//step 1: update tech unlock status & science packs (add a 0 cost pack to the tech if it has no such requirement but its prerequisites do), set tech tier
			foreach (TechnologyPrototype tech in technologies.Values.Cast<TechnologyPrototype>())
			{
				TechRequiredSciPacks(tech);
				GetTechnologyTier(tech);
				foreach (ItemPrototype sciPack in techRequirements[tech].Cast<ItemPrototype>())
					tech.InternalOneWayAddSciPack(sciPack, 0);
			}

			//step 2: further sci pack processing -> for every available science pack we want to build a list of science packs necessary to aquire it. In a situation with multiple (non-equal) research paths (ex: 3 can be aquired through either pack 1&2 or pack 1 alone), take the intersect (1 in this case). These will be added to the sci pack requirement lists
			foreach (IItem sciPack in sciPacks)
				UpdateSciencePackPrerequisites(sciPack);


			//step 2.5: update the technology science packs to account for the science pack prerequisites
			foreach (TechnologyPrototype tech in technologies.Values.Cast<TechnologyPrototype>())
				foreach (IItem sciPack in tech.SciPackList.ToList())
					foreach (ItemPrototype reqSciPack in sciencePackPrerequisites[sciPack].Cast<ItemPrototype>())
						tech.InternalOneWayAddSciPack(reqSciPack, 0);

			//step 3: calculate science pack tier (minimum tier of technology that unlocks the recipe for the given science pack). also make the sciencePacks list.
			Dictionary<IItem, int> sciencePackTiers = [];
			foreach (ItemPrototype sciPack in sciPacks.Cast<ItemPrototype>())
			{
				int minTier = int.MaxValue;
				foreach (IRecipe recipe in sciPack.ProductionRecipesInternal)
					foreach (ITechnology tech in recipe.MyUnlockTechnologies)
						minTier = Math.Min(minTier, tech.Tier);
				if (minTier == int.MaxValue) //there are no recipes for this sci pack. EX: space science pack. We will grant it the same tier as the first tech to require this sci pack. This should sort them relatively correctly (ex - placing space sci pack last, and placing seablock starting tech first)
					minTier = techRequirements.Where(kvp => kvp.Value.Contains(sciPack)).Select(kvp => kvp.Key).Min(t => t.Tier);
				sciencePackTiers.Add(sciPack, minTier);
				sciencePacks.Add(sciPack);
			}

			//step 4: update all science pack lists (main sciencePacks list, plus SciPackList of every technology). Sorting is done by A: if science pack B has science pack A as a prerequisite (in sciPackRequiredPacks), then B goes after A. If neither has the other as a prerequisite, then compare by sciencePack tiers
			sciencePacks.Sort((s1, s2) => sciencePackTiers[s1].CompareTo(sciencePackTiers[s2]) + (sciencePackPrerequisites[s1].Contains(s2) ? 1000 : sciencePackPrerequisites[s2].Contains(s1) ? -1000 : 0));
			foreach (TechnologyPrototype tech in technologies.Values.Cast<TechnologyPrototype>())
				tech.SciPackListInternal.Sort((s1, s2) => sciencePackTiers[s1].CompareTo(sciencePackTiers[s2]) + (sciencePackPrerequisites[s1].Contains(s2) ? 1000 : sciencePackPrerequisites[s2].Contains(s1) ? -1000 : 0));

			//step 5: create science pack lists for each recipe (list of distinct min-pack sets -> ex: if recipe can be aquired through 4 techs with [ A+B, A+B, A+C, A+B+C ] science pack requirements, we will only include A+B and A+C
			foreach (RecipePrototype recipe in recipes.Values.Cast<RecipePrototype>())
			{
				List<List<IItem>> sciPackLists = [];
				foreach (TechnologyPrototype tech in recipe.MyUnlockTechnologiesInternal)
				{
					bool exists = false;
					foreach (List<IItem> sciPackList in sciPackLists.ToList())
					{
						if (!sciPackList.Except(tech.SciPackListInternal).Any()) // sci pack lists already includes a list that is a subset of the technologies sci pack list (ex: already have A+B while tech's is A+B+C)
							exists = true;
						else if (!tech.SciPackListInternal.Except(sciPackList).Any()) //technology sci pack list is a subset of an already included sci pack list. we will add thi to the list and delete the existing one (ex: have A+B while tech's is A -> need to remove A+B and include A)
							sciPackLists.Remove(sciPackList);
					}
					if (!exists)
						sciPackLists.Add(tech.SciPackListInternal);
				}
				recipe.MyUnlockSciencePacks = sciPackLists;
			}
		}


		private void ProcessAvailableStatuses()
		{
			//quick function to depth-first search the tech tree to calculate the availability of the technology. Hashset used to keep track of visited tech and not have to re-check them.
			//NOTE: factorio ensures no cyclic, so we are guaranteed to have a directed acyclic graph (may be disconnected)
			HashSet<TechnologyPrototype> unlockableTechSet = [];
			bool IsUnlockable(TechnologyPrototype tech)
			{
				if (!tech.Available)
					return false;
				else if (unlockableTechSet.Contains(tech))
					return true;
				else if (tech.PrerequisitesInternal.Count == 0)
					return true;
				else
				{
					bool available = true;
					foreach (TechnologyPrototype preTech in tech.PrerequisitesInternal)
						available = available && IsUnlockable(preTech);
					tech.Available = available;

					if (available)
						unlockableTechSet.Add(tech);
					return available;
				}
			}

			//step 0: check availability of technologies
			foreach (TechnologyPrototype tech in technologies.Values.Cast<TechnologyPrototype>())
				IsUnlockable(tech);

			//step 1: update recipe unlock status
			foreach (RecipePrototype recipe in recipes.Values.Cast<RecipePrototype>())
			recipe.Available = recipe.MyUnlockTechnologiesInternal.Any(t => t.Available);

			//step 2: mark any recipe for barelling / crating as unavailable
			if (UseRecipeBWLists)
			{
				foreach (RecipePrototype recipe in recipes.Values.Cast<RecipePrototype>())
				{
					//part 1: make unavailable if recipe fits the black & doesnt fit the white recipe black lists (these should be the 'barelling' and 'unbarelling' recipes)
					if (!recipeWhiteList.Any(white => white.IsMatch(recipe.Name)) && recipeBlackList.Any(black => black.IsMatch(recipe.Name))) //if we dont match a whitelist and match a blacklist...
						recipe.Available = false;
	                //part 2: make unavailable if recipe fits the recyclingItemNameBlackList (should remove any of the barel recycling recipes added by 2.0 SA)
					foreach(KeyValuePair<string, Regex> recycleBL in recyclingItemNameBlackList)
						if (recipe.ProductListInternal.Count == 1 && (IItem)recipe.ProductListInternal[0] == items[recycleBL.Key] && recipe.IngredientListInternal.Count == 1 && recycleBL.Value.IsMatch(recipe.IngredientListInternal[0].Name))
							recipe.Available = false;
				}
            }


            //step 3: mark any recipe with no unlocks, or 0->0 recipes (industrial revolution... what are those aetheric glow recipes?) as unavailable.
            foreach (RecipePrototype recipe in recipes.Values.Cast<RecipePrototype>())
				if (recipe.MyUnlockTechnologiesInternal.Count == 0 || (recipe.ProductListInternal.Count == 0 && recipe.IngredientListInternal.Count == 0 && !recipe.Name.StartsWith("§§"))) //§§ denotes foreman added recipes. ignored during this pass (but not during the assembler check pass)
					recipe.Available = false;

			//step 4 (loop) switch any recipe with no available assemblers to unavailable, switch any useless item to unavailable (no available recipe produces it, it isnt used by any available recipe / only by incineration recipes
			bool clean = false;
			while (!clean)
			{
				clean = true;

				//4.1: mark any recipe with no available assemblers to unavailable.
				foreach (RecipePrototype recipe in recipes.Values.Where(r => r.Available && !r.Assemblers.Any(a => a.Available || (a is AssemblerPrototype ap && (ap == playerAssember || ap == rocketAssembler)))).Cast<RecipePrototype>())
				{
					recipe.Available = false;
					clean = false;
				}

                //4.2: mark any useless items as unavailable (nothing/unavailable recipes produce it, it isnt consumed by anything / only consumed by incineration / only consumed by unavailable recipes, only produced by a itself->itself recipe)
                //this will also update assembler availability status for those whose items become unavailable automatically.
                //note: while this gets rid of those annoying 'burn/incinerate' auto-generated recipes, if the modder decided to have a 'recycle' auto-generated recipe (item->raw ore or something), we will be forced to accept those items as 'available'
                //good example from vanilla: most of the 'garbage' items such as 'item-unknown' and 'electric-energy-interface' are removed as their only recipes are 'recycle to themselves', but 'heat interface' isnt removed as its only recipe is a 'recycle into several parts' (so nothing we can do about it)
                foreach (ItemPrototype item in items.Values.Where(i => i.Available && !i.ProductionRecipes.Any(r => r.Available && !(r.IngredientList.Count == 1 && r.IngredientList[0] == i) )).Cast<ItemPrototype>())
				{


					bool useful = false;

					foreach (RecipePrototype r in item.ConsumptionRecipesInternal.Where(r => r.Available))
						useful |= (r.IngredientListInternal.Count > 1 || r.ProductListInternal.Count > 1 || (r.ProductListInternal.Count == 1 && r.ProductListInternal[0] != item)); //recipe with multiple items coming in or some ingredients coming out (that arent itself) -> not an incineration type

					if (!useful && !item.Name.StartsWith("§§"))
					{
						item.Available = false;
						clean = false;
						foreach (RecipePrototype r in item.ConsumptionRecipesInternal) //from above these recipes are all item->nothing
							r.Available = false;
					}
				}
				//4.3: go over the item list one more time and ensure that if an item that is available has any growth or spoil results then they are also available (edge case: item grows or spoils into something that has no recipes aka: unavailable, but it should be available even though its only 'use' is as a spoil or grow result)
				foreach (ItemPrototype item in items.Values.Where(i => !i.Available).Cast<ItemPrototype>())
				{
					bool useful = false;
					useful |= item.SpoilOriginsInternal.Any(i => i.Available);
					useful |= item.PlantOriginsInternal.Any(i => i.Available);
					item.Available = useful;
				}

			}

			//step 5: set the 'default' enabled statuses of recipes,assemblers,modules & beacons to their available status.
			foreach (var recipe in recipes.Values)
				recipe.Enabled = recipe.Available;
			foreach (var assembler in assemblers.Values)
				assembler.Enabled = assembler.Available;
			foreach (var module in modules.Values)
				module.Enabled = module.Available;
			foreach (var beacon in beacons.Values)
				beacon.Enabled = beacon.Available;
			playerAssember.Enabled = true; //its enabled, so it can theoretically be used, but it is set as 'unavailable' so a warning will be issued if you use it.

			rocketAssembler.Enabled = assemblers["rocket-silo"]?.Enabled?? false; //rocket assembler is set to enabled if rocket silo is enabled
			rocketAssembler.Available = assemblers["rocket-silo"] != null; //override
        }

		private void CleanupGroups()
		{
			//step 6: clean up groups and subgroups (delete any subgroups that have no items/recipes, then delete any groups that have no subgroups)
			foreach (SubgroupPrototype subgroup in subgroups.Values.ToList().Cast<SubgroupPrototype>())
			{
				if (subgroup.items.Count == 0 && subgroup.recipes.Count == 0)
				{
					if (subgroup.MyGroup is null)
						throw new InvalidOperationException("subgroup.MyGroup is null");
					((GroupPrototype)subgroup.MyGroup).subgroups.Remove(subgroup);
					subgroups.Remove(subgroup.Name);
				}
			}
			foreach (GroupPrototype group in groups.Values.ToList().Cast<GroupPrototype>())
				if (group.subgroups.Count == 0)
					groups.Remove(group.Name);

			//step 7: update subgroups and groups to set them to unavailable if they only contain unavailable items/recipes
			foreach (SubgroupPrototype subgroup in subgroups.Values.Cast<SubgroupPrototype>())
				if (!subgroup.items.Any(i => i.Available) && !subgroup.recipes.Any(r => r.Available))
					subgroup.Available = false;
			foreach (GroupPrototype group in groups.Values.Cast<GroupPrototype>())
				if (!group.subgroups.Any(sg => sg.Available))
					group.Available = false;

			//step 8: sort groups/subgroups
			foreach (GroupPrototype group in groups.Values.Cast<GroupPrototype>())
				group.SortSubgroups();
			foreach (SubgroupPrototype sgroup in subgroups.Values.Cast<SubgroupPrototype>())
				sgroup.SortIRs();

		}

		private void UpdateFluidTemperatureDependencies()
		{
			//step 9: update the temperature dependent status of items (fluids)
			foreach (FluidPrototype fluid in items.Values.Where(i => i is IFluid).Cast<FluidPrototype>())
			{
				FRange productionRange = new(double.MaxValue, double.MinValue);
				FRange consumptionRange = new(double.MinValue, double.MaxValue); //a bit different -> the min value is the LARGEST minimum of each consumption recipe, and the max value is the SMALLEST max of each consumption recipe

				foreach (IRecipe recipe in fluid.ProductionRecipesInternal)
				{
					productionRange.Min = Math.Min(productionRange.Min, recipe.ProductTemperatureMap[fluid]);
					productionRange.Max = Math.Max(productionRange.Max, recipe.ProductTemperatureMap[fluid]);
				}
				foreach (IRecipe recipe in fluid.ConsumptionRecipesInternal)
				{
					consumptionRange.Min = Math.Max(consumptionRange.Min, recipe.IngredientTemperatureMap[fluid].Min);
					consumptionRange.Max = Math.Min(consumptionRange.Max, recipe.IngredientTemperatureMap[fluid].Max);
				}
				fluid.IsTemperatureDependent = !(consumptionRange.Contains(productionRange));
			}
		}

		//--------------------------------------------------------------------DEBUG PRINTING FUNCTIONS

#pragma warning disable IDE0051 // Remove unused private members
		private void PrintDataCache()
#pragma warning restore IDE0051 // Remove unused private members
		{
			Console.WriteLine("AVAILABLE: ----------------------------------------------------------------");
			Console.WriteLine("Technologies:");
			foreach (var tech in technologies.Values)
				if (tech.Available) Console.WriteLine("    " + tech);
			Console.WriteLine("Groups:");
			foreach (var group in groups.Values)
				if (group.Available) Console.WriteLine("    " + group);
			Console.WriteLine("Subgroups:");
			foreach (var sgroup in subgroups.Values)
				if (sgroup.Available) Console.WriteLine("    " + sgroup);
			Console.WriteLine("Items:");
			foreach (var item in items.Values)
				if (item.Available) Console.WriteLine("    " + item);
			Console.WriteLine("Assemblers:");
			foreach (var assembler in assemblers.Values)
				if (assembler.Available) Console.WriteLine("    " + assembler);
			Console.WriteLine("Modules:");
			foreach (var module in modules.Values)
				if (module.Available) Console.WriteLine("    " + module);
			Console.WriteLine("Recipes:");
			foreach (var recipe in recipes.Values)
				if (recipe.Available) Console.WriteLine("    " + recipe);
			Console.WriteLine("Beacons:");
			foreach (var beacon in beacons.Values)
				if (beacon.Available) Console.WriteLine("    " + beacon);

			Console.WriteLine("UNAVAILABLE: ----------------------------------------------------------------");
			Console.WriteLine("Technologies:");
			foreach (var tech in technologies.Values)
				if (!tech.Available) Console.WriteLine("    " + tech);
			Console.WriteLine("Groups:");
			foreach (var group in groups.Values)
				if (!group.Available) Console.WriteLine("    " + group);
			Console.WriteLine("Subgroups:");
			foreach (var sgroup in subgroups.Values)
				if (!sgroup.Available) Console.WriteLine("    " + sgroup);
			Console.WriteLine("Items:");
			foreach (var item in items.Values)
				if (!item.Available) Console.WriteLine("    " + item);
			Console.WriteLine("Assemblers:");
			foreach (var assembler in assemblers.Values)
				if (!assembler.Available) Console.WriteLine("    " + assembler);
			Console.WriteLine("Modules:");
			foreach (var module in modules.Values)
				if (!module.Available) Console.WriteLine("    " + module);
			Console.WriteLine("Recipes:");
			foreach (var recipe in recipes.Values)
				if (!recipe.Available) Console.WriteLine("    " + recipe);
			Console.WriteLine("Beacons:");
			foreach (var beacon in beacons.Values)
				if (!beacon.Available) Console.WriteLine("    " + beacon);

			Console.WriteLine("TECHNOLOGIES: ----------------------------------------------------------------");
			Console.WriteLine("Technology tiers:");
			foreach (TechnologyPrototype tech in technologies.Values.OrderBy(t => t.Tier).Cast<TechnologyPrototype>())
			{
				Console.WriteLine("   T:" + tech.Tier.ToString("000") + " : " + tech.Name);
				foreach (TechnologyPrototype prereq in tech.PrerequisitesInternal)
					Console.WriteLine("      > T:" + prereq.Tier.ToString("000" + " : " + prereq.Name));
			}
			Console.WriteLine("Science Pack order:");
			foreach (IItem sciPack in sciencePacks)
				Console.WriteLine("   >" + sciPack.FriendlyName);
			Console.WriteLine("Science Pack prerequisites:");
			foreach (IItem sciPack in sciencePacks)
			{
				Console.WriteLine("   >" + sciPack);
				foreach (IItem i in sciencePackPrerequisites[sciPack])
					Console.WriteLine("      >" + i);
			}

			Console.WriteLine("RECIPES: ----------------------------------------------------------------");
			foreach(RecipePrototype recipe in recipes.Values.Cast<RecipePrototype>())
			{
				Console.WriteLine("R: " + recipe.Name);
				foreach (TechnologyPrototype tech in recipe.MyUnlockTechnologiesInternal)
					Console.WriteLine("  >" + tech.Tier.ToString("000") + ":" + tech.Name);
				foreach(IReadOnlyList<IItem> sciPackList in recipe.MyUnlockSciencePacks)
				{
					Console.Write("    >Science Packs Option: ");
					foreach (IItem sciPack in sciPackList)
						Console.Write(sciPack.Name + ", ");
					Console.WriteLine();
				}
			}

			Console.WriteLine("TEMPERATURE DEPENDENT FLUIDS: ----------------------------------------------------------------");
			foreach (ItemPrototype fluid in items.Values.Where(i => i is IFluid f && f.IsTemperatureDependent).Cast<ItemPrototype>())
			{
				Console.WriteLine(fluid.Name);
				HashSet<double> productionTemps = [];
				foreach (IRecipe recipe in fluid.ProductionRecipesInternal)
					productionTemps.Add(recipe.ProductTemperatureMap[fluid]);
				Console.Write("   Production ranges:          >");
				foreach (double temp in productionTemps.ToList().OrderBy(t => t))
					Console.Write(temp + ", ");
				Console.WriteLine();
				Console.Write("   Failed Consumption ranges:  >");
				foreach (IRecipe recipe in fluid.ConsumptionRecipesInternal.Where(r => productionTemps.Any(t => !r.IngredientTemperatureMap[fluid].Contains(t))))
					Console.Write("(" + recipe.IngredientTemperatureMap[fluid].Min + ">" + recipe.IngredientTemperatureMap[fluid].Max + ": " + recipe.Name + "), ");
				Console.WriteLine();
			}
		}

		[GeneratedRegex("^empty-barrel$")]
		private static partial Regex EmptyBarrelRegex();
		[GeneratedRegex("-barrel$")]
		private static partial Regex DashBarrelRegex();
		[GeneratedRegex("^deadlock-packrecipe-")]
		private static partial Regex DeadlockPackRecipe();
		[GeneratedRegex("^deadlock-unpackrecipe-")]
		private static partial Regex DeadlockUnpackRecipe();
		[GeneratedRegex("^deadlock-plastic-packaging$")]
		private static partial Regex DeadlockPlasticPackaging();
	}
}
