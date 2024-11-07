using Foreman.DataCache;
using Foreman.DataCache.DataTypes;
using Foreman.Models;
using Foreman.Models.Nodes;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Foreman.ProductionGraphView {
	[JsonObject(MemberSerialization.OptIn)]
	public class NodeCopyOptions {
		public readonly AssemblerQualityPair Assembler;
		[JsonProperty("AModules")]
		public readonly IReadOnlyList<ModuleQualityPair> AssemblerModules;
		public readonly IItem? Fuel;
		[JsonProperty("Neighbours")]
		public readonly double NeighbourCount;
		[JsonProperty("ExtraProductivity")]
		public readonly double ExtraProductivityBonus;

		public readonly BeaconQualityPair? Beacon;
		[JsonProperty("BModules")]
		public readonly IReadOnlyList<ModuleQualityPair> BeaconModules;
		[JsonProperty]
		public readonly double BeaconCount;
		[JsonProperty("BeaconsPA")]
		public readonly double BeaconsPerAssembler;
		[JsonProperty("BeaconsC")]
		public readonly double BeaconsConst;

		[JsonProperty]
		public static int Version => Properties.Settings.Default.ForemanVersion;
		[JsonProperty]
		public static string Object => "NodeCopyOptions";
		[JsonProperty(nameof(Assembler))]
		public string JsonAssembler => Assembler.Assembler.Name;
		[JsonProperty]
		public string AssemblerQuality => Assembler.Quality.Name;
		[JsonProperty(nameof(Fuel))]
		public string JsonFuel => Fuel?.Name ?? "";
		[JsonProperty(nameof(Beacon))]
		public string JsonBeacon => Beacon?.Beacon.Name ?? "";
		[JsonProperty]
		public string BeaconQuality => Beacon?.Quality.Name ?? "";

		public bool ShouldSerializeJsonFuel() => Fuel != null;
		public bool ShouldSerializeJsonBeacon() => Beacon is not null;
		public bool ShouldSerializeBeaconQuality() => Beacon is not null;
		public bool ShouldSerializeBeaconCount() => Beacon is not null;
		public bool ShouldSerializeBeaconsPerAssembler() => Beacon is not null;
		public bool ShouldSerializeBeaconsConst() => Beacon is not null;

		public NodeCopyOptions(ReadOnlyRecipeNode node) {
			Assembler = node.SelectedAssembler;
			AssemblerModules = new List<ModuleQualityPair>(node.AssemblerModules);
			Fuel = node.Fuel;
			Beacon = node.SelectedBeacon;
			BeaconModules = new List<ModuleQualityPair>(node.BeaconModules);
			BeaconCount = node.BeaconCount;
			BeaconsPerAssembler = node.BeaconsPerAssembler;
			BeaconsConst = node.BeaconsConst;
			NeighbourCount = node.NeighbourCount;
			ExtraProductivityBonus = node.ExtraProductivity;
		}

		private NodeCopyOptions(AssemblerQualityPair assembler, List<ModuleQualityPair> assemblerModules, double neighbourCount, double extraProductivityBonus, IItem? fuel, BeaconQualityPair? beacon, List<ModuleQualityPair> beaconModules, double beaconCount, double beaconsPerA, double beaconsCont) {
			Assembler = assembler;
			AssemblerModules = assemblerModules;
			Fuel = fuel;
			Beacon = beacon;
			BeaconModules = beaconModules;
			BeaconCount = beaconCount;
			BeaconsPerAssembler = beaconsPerA;
			BeaconsConst = beaconsCont;
			NeighbourCount = neighbourCount;
			ExtraProductivityBonus = extraProductivityBonus;
		}

		public static NodeCopyOptions? GetNodeCopyOptions(string serialized, DCache cache) {
			try {
				return GetNodeCopyOptions(JObject.Parse(serialized), cache);
			} catch {
				return null;
			}
		}

		public static NodeCopyOptions? GetNodeCopyOptions(JToken json, DCache cache) {
			if (json["Version"]?.Value<int>() != Properties.Settings.Default.ForemanVersion || json["Object"]?.Value<string>() != "NodeCopyOptions")
				return null;

			bool beacons = json["Beacon"] != null;
			IAssembler? assembler = (json["Assembler"]?.Value<string>() is string asm && cache.Assemblers.ContainsKey(asm)) ? cache.Assemblers[asm] : null;
			IQuality? assemblerQuality = (json["AssemblerQuality"]?.Value<string>() is string asmQual && cache.Qualities.ContainsKey(asmQual)) ? cache.Qualities[asmQual] : null;
			if (assembler is null)
				throw new InvalidOperationException("assembler is null");
			AssemblerQualityPair assemberQP = new(assembler, assemblerQuality ?? cache.DefaultQuality);

			IBeacon? beacon = beacons && json["Beacon"]?.Value<string>() is string beaconKey && cache.Beacons.ContainsKey(beaconKey) ? cache.Beacons[beaconKey] : null;
			IQuality? beaconQuality = beacons && json["BeaconQuality"]?.Value<string>() is string beaconQual && cache.Qualities.ContainsKey(beaconQual) ? cache.Qualities[beaconQual] : null;
			BeaconQualityPair? beaconQP = beacon != null ? new BeaconQualityPair(beacon, beaconQuality ?? cache.DefaultQuality) : null; //no beacon

			List<ModuleQualityPair> aModules = [];
			foreach (JToken moduleToken in json["AModules"]?.ToList() ?? []) {
				string? moduleName = moduleToken["Name"]?.Value<string>();
				string? moduleQuality = moduleToken["Quality"]?.Value<string>();
				IModule? module = moduleName is not null && cache.Modules.ContainsKey(moduleName) ? cache.Modules[moduleName] : null;
				IQuality quality = moduleQuality is not null && cache.Qualities.ContainsKey(moduleQuality) ? cache.Qualities[moduleQuality] : cache.DefaultQuality;
				if (module is not null)
					aModules.Add(new ModuleQualityPair(module, quality));
			}

			List<ModuleQualityPair> bModules = [];
			foreach (JToken moduleToken in json["BModules"]?.ToList() ?? []) {
				string? moduleName = moduleToken["Name"]?.Value<string>();
				string? moduleQuality = moduleToken["Quality"]?.Value<string>();
				IModule? module = moduleName is not null && cache.Modules.ContainsKey(moduleName) ? cache.Modules[moduleName] : null;
				IQuality quality = moduleQuality is not null && cache.Qualities.ContainsKey(moduleQuality) ? cache.Qualities[moduleQuality] : cache.DefaultQuality;
				if (module != null)
					bModules.Add(new ModuleQualityPair(module, quality));
			}

			IItem? fuel = json["Fuel"]?.Value<string>() is string s && cache.Items.ContainsKey(s) ? cache.Items[s] : null;

			NodeCopyOptions nco = new(
				assemberQP,
				aModules,
				json["Neighbours"]?.Value<double>() ?? 0,
				json["ExtraProductivity"]?.Value<double>() ?? 0,
				fuel,
				beaconQP,
				bModules,
				beacons ? json["BeaconCount"]?.Value<double>() ?? 0 : 0,
				beacons ? json["BeaconsPA"]?.Value<double>() ?? 0 : 0,
				beacons ? json["BeaconsC"]?.Value<double>() ?? 0 : 0);
			return nco;
		}
	}
}
