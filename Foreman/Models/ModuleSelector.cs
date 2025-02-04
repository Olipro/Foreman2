﻿using Foreman.DataCache.DataTypes;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Foreman.Models {
	public class ModuleSelector {
		public enum Style { None, Speed, Productivity, ProductivityOnly, Efficiency, EfficiencyOnly }
		public static readonly string[] StyleNames = ["None", "Speed", "Productivity", "Productivity Only", "Efficiency", "Efficiency Only"];

		public Style DefaultSelectionStyle { get; set; }

		public ModuleSelector() { DefaultSelectionStyle = Style.None; }

		public List<IModule> GetModules(IAssembler assembler, IRecipe recipe) { return GetModules(assembler, recipe, DefaultSelectionStyle); }
		public List<IModule> GetModules(IAssembler assembler, IRecipe recipe, Style style) {
			List<IModule> moduleList = [];
			IModule? bestModule = null;
			IQuality quality = assembler.Owner.DefaultQuality;
			if (assembler == null || assembler.ModuleSlots == 0)
				return moduleList;


			switch (style) {
				//speed, productivity, and productivity only have no max limits, so the 'best' option will always involve identical modules. just pick the best module and fill the module slots.
				//efficiency however is capped at -80% consumption bonus, so we have to get a permutation and pick the 'best'
				case Style.Speed:
					bestModule = assembler.Modules.Intersect(recipe.AssemblerModules).Where(m => m.Enabled).OrderBy(m => m.GetSpeedBonus(quality) * 1000 - m.GetConsumptionBonus(quality)).LastOrDefault();
					break;
				case Style.Productivity:
					bestModule = assembler.Modules.Intersect(recipe.AssemblerModules).Where(m => m.Enabled).OrderBy(m => m.GetProductivityBonus(quality) * 1000 + m.GetSpeedBonus(quality)).LastOrDefault();
					break;
				case Style.ProductivityOnly:
					bestModule = assembler.Modules.Intersect(recipe.AssemblerModules).Where(m => m.Enabled && m.GetProductivityBonus(quality) != 0).OrderBy(m => m.GetProductivityBonus(quality) * 1000 + m.GetSpeedBonus(quality)).LastOrDefault();
					break;
				case Style.Efficiency:
					List<IModule> speedModules = assembler.Modules.Intersect(recipe.AssemblerModules).Where(m => m.Enabled && m.GetSpeedBonus(quality) > 0).OrderByDescending(m => m.GetSpeedBonus(quality) * 1000 - m.GetConsumptionBonus(quality)).ToList(); //highest speed is first!
					List<IModule> efficiencyModules = assembler.Modules.Intersect(recipe.AssemblerModules).Where(m => m.Enabled && m.GetConsumptionBonus(quality) < 0).OrderByDescending(m => m.GetConsumptionBonus(quality) * 1000 + m.GetSpeedBonus(quality)).ToList(); //highest consumption is first! (so worst->best effectivity)
					List<ModulePermutator.Permutation> modulePermutations = ModulePermutator.GetOptimalEfficiencyPermutations(speedModules, efficiencyModules, assembler.ModuleSlots);

					//return best module permutation that has the lowest consumption (max -80%), and the highest speed.
					if (modulePermutations.Count > 0)
						return modulePermutations.OrderByDescending(p => p.ConsumptionBonus).ThenBy(p => p.SpeedBonus).ThenByDescending(p => p.SquaredTierValue).Last().Modules.OfType<IModule>().OrderBy(m => m.FriendlyName).ToList();
					return moduleList; //empty
				case Style.EfficiencyOnly:
					List<IModule> moduleOptions = assembler.Modules.Intersect(recipe.AssemblerModules).Where(m => m.Enabled && m.GetConsumptionBonus(quality) < 0).OrderByDescending(m => m.GetConsumptionBonus(quality) * 1000 - m.GetSpeedBonus(quality)).ToList();
					List<ModulePermutator.Permutation> modulePermutationsB = ModulePermutator.GetOptimalEfficiencyPermutations([], moduleOptions, assembler.ModuleSlots);

					//return best module permutation that has the lowest consumption (max -80%), and the lowest tier cost
					if (modulePermutationsB.Count > 0)
						return modulePermutationsB.OrderByDescending(p => p.ConsumptionBonus).ThenByDescending(p => p.SquaredTierValue).Last().Modules.OfType<IModule>().OrderBy(m => m.FriendlyName).ToList();
					return moduleList; //empty
				case Style.None:
				default:
					break; //return the empty module list
			}

			if (bestModule != null)
				for (int i = 0; i < assembler.ModuleSlots; i++)
					moduleList.Add(bestModule);
			return moduleList;
		}
	}

	public static class ModulePermutator {
		public struct Permutation {
			public IModule?[] Modules; //NOTE: a null module is possible! this means that this particular permutation isnt using all slots.
			public double SpeedBonus;
			public double ProductivityBonus;
			public double ConsumptionBonus;
			public double PollutionBonus;
			public int SquaredTierValue; //total of all added modules tiers squared (ex: T1+T2+T3 would have a value of 1+4+9) ->usefull for solving for 'cheapest' option

			public Permutation(IModule?[] modules) {
				Modules = modules.ToArray();
				if (modules[0] is not IModule firstModule)
					throw new InvalidOperationException("modules[0] has no IQuality");
				IQuality quality = firstModule.Owner.DefaultQuality; //ok, this is a bit jank, I admit. Still, quality completely messes up the module selector anyway :/
				SpeedBonus = 0;
				ProductivityBonus = 0;
				ConsumptionBonus = 0;
				PollutionBonus = 0;
				SquaredTierValue = 0;

				foreach (IModule m in modules.OfType<IModule>()) {
					SpeedBonus += m.GetSpeedBonus(quality);
					ProductivityBonus += m.GetProductivityBonus(quality);
					ConsumptionBonus += m.GetConsumptionBonus(quality);
					PollutionBonus += m.GetPolutionBonus(quality);
					SquaredTierValue += m.Tier * m.Tier;
				}

				SpeedBonus = Math.Max(-0.8f, SpeedBonus);
				ProductivityBonus = Math.Max(0f, ProductivityBonus);
				ConsumptionBonus = Math.Max(-0.8f, ConsumptionBonus);
				PollutionBonus = Math.Max(-0.8f, PollutionBonus);
			}

			public override string ToString() {
				string str = "Speed: " + SpeedBonus.ToString("P") + ", Productivity: " + ProductivityBonus.ToString("P") + ", Energy: " + ConsumptionBonus.ToString("P") + ", Pollution: " + PollutionBonus.ToString("P") + ", SqTierValue: " + SquaredTierValue + ", Modules: ";
				foreach (IModule? m in Modules) {
					if (m is not null)
						str += m + ", ";
					else
						str += "---, ";
				}
				return str;
			}
		}

		public static List<Permutation> GetOptimalEfficiencyPermutations(List<IModule> speedModules, List<IModule> efficiencyModules, int moduleSlots) //the original approach of brute forcing things runs into a ceiling when using over 12 modules or 12 slots (I mean, its a combination -> factorials everywhere)
		{
			//doe by a 'smart' approach: fill in the set with i of one type speed and (module slot - i) of one type efficiency, then refine by changing 1 of the speeds to a different module and 1 of the efficiencies to a different module.
			//so assume the ideal solution will be in the form (i - 1)* speedModule A + (1)* speedModuleB + (module slots - i - 1)* efficiencyModule A + (1) * efficiencyModuleB where speedModuleA and speedModuleB can be equal (same for efficiencyA and B)
			List<Permutation> permutations = [];
			IModule?[] permutation = new IModule[moduleSlots];


			//permutation will be in the form [ 1 speedModuleB, n-1 amounts of speedModuleA, moduleslots-n-1 amounts of efficiencyModuleA, 1 efficiencyModuleB ] where n is between 1 and moduleslots - 1.
			//thus 3 speed modules + 1 efficiency is valid. 1 speed module and 3 efficiency is valid. 4 speed only is not (neither is 4 efficiency)
			//will do nothing if there isnt at least 3 slots
			for (int sfA = 0; sfA < speedModules.Count; sfA++) {
				for (int efA = 0; efA < efficiencyModules.Count; efA++) {
					for (int border = 1; border < moduleSlots; border++) {
						for (int i = 1; i < border; i++)
							permutation[i] = speedModules[sfA];
						for (int i = border; i < moduleSlots - 1; i++)
							permutation[i] = efficiencyModules[efA];
						for (int sfB = sfA; sfB < speedModules.Count; sfB++) {
							permutation[0] = speedModules[sfB];
							for (int efB = efA; efB < efficiencyModules.Count; efB++) {
								permutation[moduleSlots - 1] = efficiencyModules[efB];
								permutations.Add(new Permutation(permutation));
							}
						}
					}
				}
			}

			//efficiency only permutations -> in the form of [n efficiency moduleA, x efficiency moduleB, moduleSlots-n-x null modules], with at least 1 of moduleA and 1 of moduleB
			//will do nothing if there arent at least 2 slots
			for (int efA = 0; efA < efficiencyModules.Count; efA++) {
				for (int efB = efA; efB < efficiencyModules.Count; efB++) //prevents double counts where A and B are switched (still double counts at A=B, but thats minor enough)
				{
					for (int n = 1; n < moduleSlots; n++) {
						for (int x = n + 1; x < moduleSlots; x++) {
							for (int i = 0; i < n; i++)
								permutation[i] = efficiencyModules[efA];
							for (int i = n; i < x; i++)
								permutation[i] = efficiencyModules[efB];
							for (int i = x; i < moduleSlots; i++)
								permutation[i] = null;
							permutations.Add(new Permutation(permutation));
						}
					}
				}
			}

			//last efficiency only -> 0 or 1 modules
			//at least 1 slot
			for (int i = 0; i < moduleSlots; i++)
				permutation[i] = null;
			permutations.Add(new Permutation(permutation)); //no modules
			foreach (IModule module in efficiencyModules) {
				permutation[0] = module;
				permutations.Add(new Permutation(permutation)); //1 module
			}

			//0 slots (empty permutation)
			if (moduleSlots == 0)
				permutations.Add(new Permutation(permutation));

			return permutations;
		}
	}
}
