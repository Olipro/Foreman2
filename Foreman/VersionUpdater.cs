using Foreman.DataCache;
using Foreman.DataCache.DataTypes;
using Foreman.Models;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Foreman {
	public static class VersionUpdater
	{
		public const int CurrentVersion = 7;
		private static readonly string[] NoneAssembler = ["###NONE-ASSEMBLER###"];

		//at some point we need to come back here and actuall fill in the version updater from the base foreman to the current version.


		public static JObject UpdateSave(JObject original, DCache cache)
		{
			if (original["Version"] is null || original["Object"]?.Value<string>() is not string obj || obj != "ProductionGraphViewer")
			{
				if (original["Nodes"] != null && original["NodeLinks"] != null && original["ElementLocations"] != null)
				{
					//this is most likely the 'original' foreman graph. At the moment there isnt a conversion in place to bring it up to current standard (Feature will be added later)
					JObject updated = new() {
						["Version"] = 2,
						["Object"] = "ProductionGraphViewer",

						["SavedPresetName"] = cache.PresetName, //we will import into the currently selected preset. Any failures are handled as missings.
						["IncludedMods"] = new JArray(original["EnabledMods"]?.Select(t => t.Value<string>()).OfType<string>().Select(t => t + "|0").ToList() ?? []),

						["Unit"] = original["Unit"], //original is per sec then per min, which maps nicely to our new units 
						["ViewOffset"] = string.Format("{0}, {1}", 0, 0),
						["ViewScale"] = 1,

						["ExtraProdForNonMiners"] = false,
						["AssemblerSelectorStyle"] = (int)AssemblerSelector.Style.Best,
						["ModuleSelectorStyle"] = (int)ModuleSelector.Style.Productivity,
						["FuelPriorityList"] = new JArray(),

						["EnabledRecipes"] = original["EnabledRecipes"],
						["EnabledAssemblers"] = original["EnabledAssemblers"]
					};
					foreach (string miner in original["EnabledMiners"]?.Select(t => t.Value<string>()).OfType<string>() ?? [])
						(updated["EnabledAssemblers"] as JArray)?.Add(miner);

					updated["EnabledModules"] = original["EnabledModules"];
					updated["EnabledBeacons"] = new JArray();

					updated["OldImport"] = true; //special flag for the graph informing it that this is an old save

					JObject updatedGraph = [];
					updated["ProductionGraph"] = updatedGraph;

					updatedGraph["Version"] = 2;
					updatedGraph["Object"] = "ProductionGraph";

					updatedGraph["IncludedAssemblers"] = new JArray(NoneAssembler); //there is no info in old foreman files about assembler status. This will make all assemblers be 'missing', but this can be solved by auto-setting assembler for all nodes after import

					updatedGraph["IncludedModules"] = new JArray(); //no info - thus none
					updatedGraph["IncludedBeacons"] = new JArray(); //no info - thus none

					//item processing
					HashSet<string> includedItems =
					[
						.. original["Nodes"]?.Where(t => t["NodeType"]?.Value<string>() == "PassThrough" || t["NodeType"]?.Value<string>() == "Supply" || t["NodeType"]?.Value<string>() == "Consumer").Select(t => t["ItemName"]?.Value<string>()),
						.. original["NodeLinks"]?.Select(t => t["Item"]?.Value<string>()),
					];
					updatedGraph["IncludedItems"] = new JArray(includedItems);

					//recipe processing
					Dictionary<string, Tuple<HashSet<string>, HashSet<string>>> recipeFossils = [];
					Dictionary<int, string> recipeNames = [];

					JArray includedRecipes = [];
					updatedGraph["IncludedRecipes"] = includedRecipes;
					Dictionary<string, int> recipeIDs = [];

					for(int i = 0; i < original["Nodes"]?.Count(); i++)
					{
						if(original["Nodes"]?[i] is JToken node && node["NodeType"]?.Value<string>() == "Recipe" && node["RecipeName"]?.Value<string>() is string recipeName)
						{
							recipeNames.Add(i, recipeName);
							if(!recipeFossils.ContainsKey(recipeName))
								recipeFossils.Add(recipeName, new Tuple<HashSet<string>, HashSet<string>>([], []));
						}
					}

					foreach(JToken link in original["NodeLinks"]?.ToList() ?? [])
					{
						if (link["Supplier"]?.Value<int>() is not int supplierId || link["Consumer"]?.Value<int>() is not int consumerId || link["Item"]?.Value<string>() is not string item)
							continue;
						if (recipeNames.TryGetValue(consumerId, out string? consumerStr))
							recipeFossils[consumerStr].Item1.Add(item);
						if (recipeNames.TryGetValue(supplierId, out string? supplierStr))
							recipeFossils[supplierStr].Item2.Add(item);
					}

					foreach(var recipeFossil in recipeFossils)
					{
						JObject? includedRecipe = null;

						if(cache.Recipes.ContainsKey(recipeFossil.Key))
						{
							IRecipe recipe = cache.Recipes[recipeFossil.Key];
							bool fits = true;
							foreach (string ingredient in recipeFossil.Value.Item1)
								fits &= cache.Items.ContainsKey(ingredient) && recipe.IngredientSet.ContainsKey(cache.Items[ingredient]);
							foreach (string product in recipeFossil.Value.Item2)
								fits &= cache.Items.ContainsKey(product) && recipe.ProductSet.ContainsKey(cache.Items[product]);
							if(fits)
							{
								JObject ingredients = [];
								foreach (IItem ingredient in recipe.IngredientList)
									ingredients[ingredient.Name] = recipe.IngredientSet[ingredient];
								
								JObject products = [];
								foreach (IItem product in recipe.ProductList)
									products[product.Name] = recipe.ProductSet[product];

								includedRecipe = new JObject
								{
									{"Name", recipe.Name },
									{"RecipeID", includedRecipes.Count },
									{"isMissing", false },
									{"Ingredients", ingredients },
									{"Products", products }
								};
							}
						}

						if(includedRecipe == null)
						{
							JObject ingredients = [];
							foreach (string ingredient in recipeFossil.Value.Item1)
								ingredients[ingredient] = 1;

							JObject products = [];
							foreach (string product in recipeFossil.Value.Item2)
								products[product] = 1;

							includedRecipe = new JObject()
							{
								{"Name", recipeFossil.Key },
								{"RecipeID", includedRecipes.Count },
								{"isMissing", true },
								{"Ingredients", ingredients },
								{"Products", products }
							};
						}

						if (includedRecipe["Name"]?.Value<string>() is not string recipeName || includedRecipe["RecipeID"]?.Value<int>() is not int recipeId)
							throw new InvalidOperationException("Recipe Name/ID is invalid or null");

						recipeIDs.Add(recipeName, recipeId);
						includedRecipes.Add(includedRecipe);
					}

					//node processing
					JArray nodes = [];
					updatedGraph["Nodes"] = nodes;

					List<string> nodeLocations = original["ElementLocations"]?.Select(t => t.Value<string>()).OfType<string>().ToList() ?? [];
					HashSet<int> processedNodeIDs = [];

					for (int i = 0; i < original["Nodes"]?.Count(); i++)
					{
						JToken? originalNode = original["Nodes"]?[i];
						JObject newNode = new() {
							{ "RateType", originalNode?["RateType"]?.Value<int>() ?? throw new InvalidOperationException("RateType is invalid") },
							{"NodeID", i },
							{"Location", nodeLocations[i] }
						};
						if (newNode["RateType"]!.Value<RateType>()! == RateType.Manual)
							newNode["DesiredRate"] = originalNode["DesiredRate"];

						processedNodeIDs.Add(i);
						switch(originalNode["NodeType"]?.Value<string>() ?? throw new InvalidOperationException("NodeType is invalid"))
						{
							case "Consumer":
								newNode["NodeType"] = (int)NodeType.Consumer;
								newNode["Item"] = originalNode["ItemName"];
								break;
							case "PassThrough":
								newNode["NodeType"] = (int)NodeType.Passthrough;
								newNode["Item"] = originalNode["ItemName"];
								break;
							case "Supply":
								newNode["NodeType"] = (int)NodeType.Supplier;
								newNode["Item"] = originalNode["ItemName"];
								break;
							case "Recipe":
								newNode["NodeType"] = (int)NodeType.Recipe;
								newNode["RecipeID"] = recipeIDs[originalNode["RecipeName"]?.Value<string>() ?? throw new InvalidOperationException("RecipeName is invalid")];
								newNode["Neighbours"] = 0;
								newNode["ExtraProductivity"] = 0;

								newNode["RateType"] = (int)RateType.Auto; //we switched to an assembler based approach, which unfortunately cant be carried over

								newNode["Assembler"] = "###NONE-ASSEMBLER###";
								newNode["AssemblerModules"] = new JArray();
								break;
							default:
								processedNodeIDs.Remove(i);
								break;
						}

						nodes.Add(newNode);
					}

					//node link processing
					JArray nodeLinks = [];
					updatedGraph["NodeLinks"] = nodeLinks;

					foreach(JToken link in original["NodeLinks"]?.ToList() ?? [])
					{
						if (link["Supplier"]?.Value<int>() is not int supplierId || link["Consumer"]?.Value<int>() is not int consumerId || link["Item"]?.Value<string>() is not string item)
							continue;

						if (processedNodeIDs.Contains(supplierId) && processedNodeIDs.Contains(consumerId))
							nodeLinks.Add(new JObject
							{
								{"SupplierID", supplierId },
								{"ConsumerID", consumerId },
								{"Item", item }
							});
					}
					original = updated;
				}
				else
				{
					//unknown file format
					MessageBox.Show("Unknown file format.", "Cant load save", MessageBoxButtons.OK);
					return [];
				}
			}

			if (original["Version"]?.Value<int>() == 1)
			{
				//Version update 1 -> 2:
				//	Graph now has the extra productivity for non-miners value 
				original["Version"] = 2;

				original["ExtraProdForNonMiners"] = false;
			}

			if (original["Version"]?.Value<int>() < 5)
			{
				//Version update 2 -> 6:
				//	No changes in main save (all changes are within the graph)
				original["Version"] = 6;
			}
			
			if (original["Version"]?.Value<int>() == 6)
			{
				//Version update 7:
				//  Added EnabledQualities

				JArray qualities = [];
				foreach(IQuality quality in cache.Qualities.Values.Where(q => q.Enabled))
					qualities.Add(quality.Name);
				original["EnabledQualities"] = qualities;

                original["Version"] = 7;
            }

            return original;
		}

		public static JObject UpdateGraph(JObject original, DCache cache)
		{
			if (original["Version"] == null || original["Object"]?.Value<string>() != "ProductionGraph")
			{
				//this is most likely the 'original' foreman graph. At the moment there isnt a conversion in place to bring it up to current standard (Feature will be added later)
				MessageBox.Show("Imported graph could not be updated to current foreman version.\nSorry.", "Cant process import", MessageBoxButtons.OK);
				return [];
			}

			if(original["Version"]?.Value<int>() == 1)
			{
				//Version update 1 -> 2:
				//	recipe node now has "ExtraPoductivity" value added
				original["Version"] = 2;

				foreach (JToken nodeJToken in original["Nodes"]?.Where(jt => jt["NodeType"]?.Value<NodeType>() == NodeType.Recipe).ToList() ?? [])
					nodeJToken["ExtraProductivity"] = 0;
			}

			if (original["Version"]?.Value<int>() == 2)
			{
				//Version update 2 -> 3:
				//	Nodes now have Direction parameter
				original["Version"] = 3;

				foreach (JToken nodeJToken in original["Nodes"]?.ToList() ?? [])
					nodeJToken["Direction"] = (int)NodeDirection.Up;
			}

			if (original["Version"]?.Value<int>() == 3)
			{
				//Version update 3 -> 4:
				//	Passthrough nodes now have SDraw parameter
				original["Version"] = 4;

				foreach (JToken nodeJToken in original["Nodes"]?.Where(n => n["NodeType"]?.Value<NodeType>() == NodeType.Passthrough).ToList() ?? [])
					nodeJToken["SDraw"] = true;
			}

			if (original["Version"]?.Value<int>() == 4)
			{
				//Version update 4 -> 5:
				//	ProductionGraph gained new properties:
				//		EnableExtraProductivityForNonMiners
				//		DefaultNodeDirection
				//		Solver_PullOutputNodes
				//		Solver_PullOutputNodesPower
				//		Solver_LowPriorityPower
				original["Version"] = 5;

				original["EnableExtraProductivityForNonMiners"] = false;
				original["DefaultNodeDirection"] = (int)NodeDirection.Up;
				original["Solver_PullOutputNodes"] = false;
				original["Solver_PullOutputNodesPower"] = 1f;
				original["Solver_LowPriorityPower"] = 2f;
			}

			if (original["Version"]?.Value<int>() == 5)
			{
                //Version update 5 -> 6:
                //  All nodes now feature a unified 'DesiredSetValue' that replaces the "DesiredAssemblers" from recipe nodes and "DesiredRatePerSec" from all other nodes
                //  This value is specific to each node type (ex: recipe = #assemblers, spoil = #stacks, grow = #tiles, most other nodes = #throughput/s)

				//  Also a new group was added to represent plant processes (IncludedPlantProcesses) - old saves will not have anything here, so just a blank node is fine

                foreach(JToken nodeJToken in original["Nodes"]?.ToList() ?? [])
                {
                    if (nodeJToken["DesiredAssemblers"]?.Value<double>() is double desiredAsm)
                        nodeJToken["DesiredSetValue"] = desiredAsm;
                    //if (nodeJToken["DesiredRatePerSec"] != null)
                    //    nodeJToken["DesiredSetValue"] = (double)nodeJToken["DesiredRatePerSec"];
					if (nodeJToken["DesiredRate"]?.Value<double>() is double desiredRate)
						nodeJToken["DesiredSetValue"] = desiredRate;
                }

				original["IncludedPlantProcesses"] = new JArray();

                original["Version"] = 6;
            }

			if (original["Version"]?.Value<int>() == 6)
			{
				//Version update 6 -> 7:
				//  Added 'included qualities'  (list of included qualities set as name = level, include only the 'default' normal quality)
				//  Added 'maxQualityIterations'  (int value representing max number of quality tiers a recipe node will output with quality modules)
				//  Added quality options for recipes, assemblers, beacons, modules, and items

				JArray qualities = [];
				JObject qualityJObject = new() {
                    { "Key", "normal" },
                    { "Value", 0 }
                };
				qualities.Add(qualityJObject);

				original["IncludedQualities"] = qualities;
				original["MaxQualitySteps"] = 5; //5 is the base number of quality modules in factorio, so its a nice value (using the current max length value could cause issues when combined with those '200 quality' mods)
				original["DefaultQulity"] = cache.DefaultQuality.Name;

                foreach (JToken nodeJToken in original["Nodes"]?.ToList() ?? [])
				{
					switch (nodeJToken["NodeType"]?.Value<NodeType>())
					{
						case NodeType.Passthrough:
						case NodeType.Supplier:
						case NodeType.Consumer:
						case NodeType.Spoil:
						case NodeType.Plant:
                            nodeJToken["BaseQuality"] = cache.DefaultQuality.Name;
							break;

						case NodeType.Recipe:
                            nodeJToken["RecipeQuality"] = cache.DefaultQuality.Name;
                            nodeJToken["AssemblerQuality"] = cache.DefaultQuality.Name;

							JArray newAssemblerModules = [];
							foreach (string module in nodeJToken["AssemblerModules"]?.Select(v => v.Value<string>()).OfType<string>() ?? [])
								newAssemblerModules.Add(new JObject { ["Name"] = module, ["Quality"] = cache.DefaultQuality.Name});
							nodeJToken["AssemblerModules"] = newAssemblerModules;

							if (nodeJToken["Beacon"] != null)
							{
								nodeJToken["BeaconQuality"] = cache.DefaultQuality.Name;
								JArray newBeaconModules = [];
								foreach (string module in nodeJToken["BeaconModules"]?.Select(v => v.Value<string>()).OfType<string>() ?? [])
									newBeaconModules.Add(new JObject { ["Name"] = module, ["Quality"] = cache.DefaultQuality.Name });
								nodeJToken["BeaconModules"] = newBeaconModules;
							}

                            break;
					}
				}
				var nodeLinks = original["NodeLinks"];
				if (nodeLinks is not null)
					foreach (JToken linkJToken in nodeLinks)
						linkJToken["Quality"] = cache.DefaultQuality.Name;

                original["Version"] = 7;
            }

            return original;
		}
	}
}
