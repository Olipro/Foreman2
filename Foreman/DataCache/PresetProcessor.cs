using Foreman.DataCache.DataTypes;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Foreman.DataCache {
	public static class PresetProcessor {
		public static PresetInfo ReadPresetInfo(Preset preset) {
			Dictionary<string, string> mods = [];
			string presetPath = Path.Combine([Application.StartupPath, "Presets", preset.Name + ".pjson"]);
			if (!File.Exists(presetPath))
				return new PresetInfo(null, false, false);

			try {
				JObject jsonData = JObject.Parse(File.ReadAllText(presetPath));
				foreach (var objJToken in jsonData["mods"]?.ToList() ?? [])
					mods.Add(objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"), objJToken["version"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"));
				return new PresetInfo(mods, jsonData["difficulty"]?[0]?.Value<int>() == 1, jsonData["difficulty"]?[1]?.Value<int>() == 1);
			} catch {
				mods.Clear();
				mods.Add("ERROR READING PRESET!", "");
				return new PresetInfo(mods, false, false);
			}

		}

		public static JObject PrepPreset(Preset preset) {
			string presetPath = Path.Combine([Application.StartupPath, "Presets", preset.Name + ".pjson"]);
			string presetCustomPath = Path.Combine([Application.StartupPath, "Presets", preset.Name + ".json"]);

			JObject jsonData = JObject.Parse(File.ReadAllText(presetPath));
			if (File.Exists(presetCustomPath)) {
				JObject cjsonData = JObject.Parse(File.ReadAllText(presetCustomPath));
				foreach (var groupToken in cjsonData) {
					foreach (JObject itemToken in groupToken.Value?.Select(tok => tok as JObject).OfType<JObject>() ?? throw new InvalidOperationException("Missing JSON value")) {
						JObject? presetItemToken = jsonData[groupToken.Key]?.FirstOrDefault(t => t["name"]?.Value<string>() == itemToken["name"]?.Value<string>())?.ToObject<JObject>();
						if (presetItemToken != null)
							foreach (var parameter in itemToken)
								presetItemToken[parameter.Key] = parameter.Value;
						else
							((JArray?)jsonData[groupToken.Key])?.Add(itemToken);
					}
				}
			}
			return jsonData;
		}

		public static async Task<PresetErrorPackage> TestPreset(Preset preset, Dictionary<string, string> modList, List<string> itemList, List<string> qualityList, List<RecipeShort> recipeShorts, List<PlantShort> plantShorts) {
			return await TestPresetStreamlined(preset, modList, itemList, qualityList, recipeShorts, plantShorts);
		}
		//this preset comparer loads a 'light' version of the preset - basically loading the items and entities as strings only (no data), and only the minimal info for recipes (name, ingredients + amounts, products + amounts)
		//this speeds things up such that the comparison takes around 150ms for a large preset like seablock (10x vanilla), instead of 250ms as for a full datacache load.
		//still, this is only really helpful if you are using 10 presets (1.5 sec load inatead of 2.5 sec) or more, but hey; i will keep it.
		//any changes to preset json style have to be reflected here though (unlike for a full data cache loader above, which just incorporates any changes to data cache as long as they dont impact the outputs)
		private static async Task<PresetErrorPackage> TestPresetStreamlined(Preset preset, Dictionary<string, string> modList, List<string> itemList, List<string> qualityList, List<RecipeShort> recipeShorts, List<PlantShort> plantShorts) {
			return await Task.Run(() => {
				JObject jsonData = PrepPreset(preset);

				//parse preset (note: this is preset data, so we are guaranteed to only have one name per item/recipe/mod/etc.)
				HashSet<string> presetItems = [];
				HashSet<string> presetEntities = [];
				Dictionary<string, RecipeShort> presetRecipes = [];
				Dictionary<string, PlantShort> presetPlantProcesses = [];
				Dictionary<string, string> presetMods = [];
				HashSet<string> presetQualities = [];

				//built in items
				presetItems.Add("§§i:heat");
				//built in recipes:
				RecipeShort heatRecipe = new("§§r:h:heat-generation");
				heatRecipe.Products.Add("§§i:heat", 1);
				presetRecipes.Add(heatRecipe.Name, heatRecipe);
				RecipeShort burnerRecipe = new("§§r:h:burner-electicity");
				presetRecipes.Add(burnerRecipe.Name, burnerRecipe);
				//built in assemblers:
				presetEntities.Add("§§a:player-assembler");
				presetEntities.Add("§§a:rocket-assembler");

				//read in mods
				foreach (var objJToken in jsonData["mods"]?.ToList() ?? [])
					presetMods.Add(objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"), objJToken["version"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"));
				//read in items (and their plant results)
				foreach (var objJToken in jsonData["items"]?.ToList() ?? []) {
					var name = objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key");
					presetItems.Add(name);
					if (objJToken["plant_results"] != null) {
						PlantShort plantProcess = new(name);
						foreach (var productJToken in objJToken["plant_results"]?.ToList() ?? []) {
							double amount = productJToken["amount"]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key");
							if (amount > 0) {
								string productName = productJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key");
								if (!plantProcess.Products.TryAdd(productName, amount))
									plantProcess.Products[productName] += amount;
							}
						}
						presetPlantProcesses.Add(plantProcess.Name, plantProcess);
					}
				}
				//read in fluids
				foreach (var objJToken in jsonData["fluids"]?.ToList() ?? [])
					presetItems.Add(objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"));
				//read in entities
				foreach (var objJToken in jsonData["entities"]?.ToList() ?? [])
					presetEntities.Add(objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"));
				//read in quality data
				foreach (var objJToken in jsonData["qualities"]?.ToList() ?? [])
					presetQualities.Add(objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"));

				//read in recipes
				foreach (var objJToken in jsonData["recipes"]?.ToList() ?? []) {

					RecipeShort recipe = new(objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"));
					foreach (var ingredientJToken in objJToken["ingredients"]?.ToList() ?? []) {
						double amount = ingredientJToken["amount"]?.Value<double>() ?? 0.0;
						if (amount > 0) {
							string ingredientName = ingredientJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key");
							if (!recipe.Ingredients.TryAdd(ingredientName, amount))
								recipe.Ingredients[ingredientName] += amount;
						}
					}
					foreach (var productJToken in objJToken["products"]?.ToList() ?? []) {
						double amount = productJToken["amount"]?.Value<double>() ?? 0.0;
						if (amount > 0) {

							string productName = productJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key");
							if (!recipe.Products.TryAdd(productName, amount))
								recipe.Products[productName] += amount;
						}
					}
					presetRecipes.Add(recipe.Name, recipe);
				}

				//have to process mining, generators and boilers (since we convert them to recipes as well)
				foreach (var objJToken in jsonData["resources"]?.Concat(jsonData["water_resources"]?.ToList() ?? []) ?? []) {
					if (objJToken["products"] is not JToken products || !products.Any())
						continue;

					RecipeShort recipe = new("§§r:e:" + objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"));

					foreach (var productJToken in products) {
						double amount = productJToken["amount"]?.Value<double>() ?? 0.0;
						if (amount > 0) {
							string productName = productJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key");
							if (!recipe.Products.TryAdd(productName, amount))
								recipe.Products[productName] += amount;
						}
					}
					if (recipe.Products.Count == 0)
						continue;

					if (objJToken["required_fluid"]?.Value<string>() is string required_fluid && objJToken["fluid_amount"]?.Value<double>() is double fluid_amount && fluid_amount != 0)
						recipe.Ingredients.Add(required_fluid, fluid_amount);

					presetRecipes.Add(recipe.Name, recipe);
				}

				foreach (var objJToken in jsonData["entities"]?.ToList() ?? []) {
					string type = objJToken["type"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key");
					if (type == "boiler") {
						if (objJToken["fluid_ingredient"]?.Value<string>() is not string ingredient || objJToken["fluid_product"]?.Value<string>() is not string product)
							continue;

						double temp = objJToken["target_temperature"]?.Value<double>() ?? 0.0;

						RecipeShort recipe = new(string.Format("§§r:b:{0}:{1}:{2}", ingredient, product, temp.ToString()));
						recipe.Ingredients.Add(ingredient, 60);
						recipe.Products.Add(product, 60);

						presetRecipes.TryAdd(recipe.Name, recipe);
					} else if (type == "generator") {
						if (objJToken["fluid_ingredient"]?.Value<string>() is not string ingredient)
							continue;

						double minTemp = objJToken["minimum_temperature"]?.Value<double>() ?? double.NaN;
						double maxTemp = objJToken["maximum_temperature"]?.Value<double>() ?? double.NaN;
						RecipeShort recipe = new(string.Format("§§r:g:{0}:{1}>{2}", ingredient, minTemp, maxTemp));
						recipe.Ingredients.Add(ingredient, 60);

						presetRecipes.TryAdd(recipe.Name, recipe);
					}
				}

				//process launch product recipes
				if (presetItems.Contains("rocket-part") && presetRecipes.ContainsKey("rocket-part") && presetEntities.Contains("rocket-silo")) {
					foreach (var objJToken in jsonData["items"]?.Concat(jsonData["fluids"]?.ToList() ?? []).Where(t => t["launch_products"] is not null).OfType<JToken>() ?? []) {
						RecipeShort recipe = new(string.Format("§§r:rl:launch-{0}", objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key")));

						int inputSize = objJToken["stack"]?.Value<int>() ?? throw new InvalidOperationException("Missing JSON key");
						foreach (var productJToken in objJToken["launch_products"]?.ToList() ?? []) {
							double amount = productJToken["amount"]?.Value<double>() ?? 0.0;
							int productStack = (int)(jsonData["items"]?.First(t => t["name"]?.Value<string>() == productJToken["name"]?.Value<string>())["stack"] ?? 1);
							if (amount != 0 && inputSize * amount > productStack)
								inputSize = (int)(productStack / amount);
						}
						foreach (var productJToken in objJToken["launch_products"]?.ToList() ?? []) {
							double amount = productJToken["amount"]?.Value<double>() ?? 0.0;
							if (amount != 0)
								recipe.Products.Add(productJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"), amount * inputSize);
						}

						recipe.Ingredients.Add(objJToken["name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"), inputSize);
						recipe.Ingredients.Add("rocket-part", 100);

						presetRecipes.Add(recipe.Name, recipe);
					}
				}

				//compare to provided mod/item/recipe sets (recipes have a chance of existing in multitudes - aka: missing recipes)
				PresetErrorPackage errors = new(preset);
				foreach (var mod in modList) {
					errors.RequiredMods.Add(mod.Key + "|" + mod.Value);

					if (!presetMods.TryGetValue(mod.Key, out string? value))
						errors.MissingMods.Add(mod.Key + "|" + mod.Value);
					else if (value != mod.Value)
						errors.WrongVersionMods.Add(mod.Key + "|" + mod.Value + "|" + value);
				}
				foreach (var mod in presetMods)
					if (!modList.ContainsKey(mod.Key))
						errors.AddedMods.Add(mod.Key + "|" + mod.Value);

				foreach (string itemName in itemList) {
					errors.RequiredItems.Add(itemName);

					if (!presetItems.Contains(itemName))
						errors.MissingItems.Add(itemName);
				}

				foreach (RecipeShort recipeS in recipeShorts) {
					errors.RequiredRecipes.Add(recipeS.Name);
					if (recipeS.IsMissing) {
						if (presetRecipes.TryGetValue(recipeS.Name, out RecipeShort? value) && recipeS.Equals(value))
							errors.ValidMissingRecipes.Add(recipeS.Name);
						else
							errors.IncorrectRecipes.Add(recipeS.Name);
					} else {
						if (!presetRecipes.TryGetValue(recipeS.Name, out RecipeShort? value))
							errors.MissingRecipes.Add(recipeS.Name);
						else if (!recipeS.Equals(value))
							errors.IncorrectRecipes.Add(recipeS.Name);
					}
				}

				foreach (PlantShort plantS in plantShorts) {
					errors.RequiredPlanting.Add(plantS.Name);
					if (plantS.IsMissing) {
						if (presetPlantProcesses.TryGetValue(plantS.Name, out PlantShort? value) && plantS.Equals(value))
							errors.ValidMissingPlanting.Add(plantS.Name);
						else
							errors.IncorrectPlanting.Add(plantS.Name);
					} else {
						if (!presetPlantProcesses.TryGetValue(plantS.Name, out PlantShort? value))
							errors.MissingPlanting.Add(plantS.Name);
						else if (!plantS.Equals(value))
							errors.IncorrectPlanting.Add(plantS.Name);
					}
				}

				foreach (string qualityName in qualityList) {
					errors.RequiredQualities.Add(qualityName);

					if (!presetQualities.Contains(qualityName))
						errors.MissingQualities.Add(qualityName);
				}
				return errors;
			});
		}
	}
}
