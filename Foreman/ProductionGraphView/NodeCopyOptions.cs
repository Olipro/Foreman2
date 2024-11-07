using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Foreman
{
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	public class NodeCopyOptions
	{
		public readonly AssemblerQualityPair Assembler;
		[JsonProperty("AModules")]
		public readonly IReadOnlyList<ModuleQualityPair> AssemblerModules;
		public readonly Item Fuel;
		[JsonProperty("Neighbours")]
		public readonly double NeighbourCount;
		[JsonProperty("ExtraProductivity")]
		public readonly double ExtraProductivityBonus;

		public readonly BeaconQualityPair Beacon;
		[JsonProperty("BModules")]
		public readonly IReadOnlyList<ModuleQualityPair> BeaconModules;
		[JsonProperty]
		public readonly double BeaconCount;
		[JsonProperty("BeaconsPA")]
		public readonly double BeaconsPerAssembler;
		[JsonProperty("BeaconsC")]
		public readonly double BeaconsConst;

		[JsonProperty]
		public int Version => Properties.Settings.Default.ForemanVersion;
		[JsonProperty]
		public string Object => "NodeCopyOptions";
		[JsonProperty("Assembler")]
		public string jsonAssembler => Assembler.Assembler.Name;
		[JsonProperty]
		public string AssemblerQuality => Assembler.Quality.Name;
		[JsonProperty("Fuel")]
		public string jsonFuel => Fuel.Name;
		[JsonProperty("Beacon")]
		public string jsonBeacon => Beacon.Beacon.Name;
		[JsonProperty]
		public string BeaconQuality => Beacon.Quality.Name;

		public bool ShouldSerializejsonFuel() => Fuel != null;
		public bool ShouldSerializejsonBeacon() => Beacon;
		public bool ShouldSerializeBeaconQuality() => Beacon;
		public bool ShouldSerializeBeaconCount() => Beacon;
		public bool ShouldSerializeBeaconsPerAssembler() => Beacon;
		public bool ShouldSerializeBeaconsConst() => Beacon;

		public NodeCopyOptions(ReadOnlyRecipeNode node)
		{
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

		private NodeCopyOptions(AssemblerQualityPair assembler, List<ModuleQualityPair> assemblerModules, double neighbourCount, double extraProductivityBonus, Item fuel, BeaconQualityPair beacon, List<ModuleQualityPair> beaconModules, double beaconCount, double beaconsPerA, double beaconsCont)
		{
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

		public static NodeCopyOptions GetNodeCopyOptions(string serialized, DataCache cache)
		{
			try { return GetNodeCopyOptions(JObject.Parse(serialized), cache); }
			catch { return null; }
		}

		public static NodeCopyOptions GetNodeCopyOptions(JToken json, DataCache cache)
		{
			if (json["Version"] == null || (int)json["Version"] != Properties.Settings.Default.ForemanVersion || json["Object"] == null || (string)json["Object"] != "NodeCopyOptions")
				return null;

			bool beacons = json["Beacon"] != null;
			Assembler assembler = cache.Assemblers.ContainsKey((string)json["Assembler"]) ? cache.Assemblers[(string)json["Assembler"]] : null;
			Quality assemblerQuality = cache.Qualities.ContainsKey((string)json["AssemblerQuality"]) ? cache.Qualities[(string)json["AssemblerQuality"]] : null;
			AssemblerQualityPair assemberQP = new AssemblerQualityPair(assembler, assemblerQuality ?? cache.DefaultQuality);

			Beacon beacon = (beacons && cache.Beacons.ContainsKey((string)json["Beacon"])) ? cache.Beacons[(string)json["Beacon"]] : null;
			Quality beaconQuality = (beacons && cache.Qualities.ContainsKey((string)json["BeaconQuality"])) ? cache.Qualities[(string)json["BeaconQuality"]] : null;
			BeaconQualityPair beaconQP = beacon != null? new BeaconQualityPair(beacon, beaconQuality ?? cache.DefaultQuality) : new BeaconQualityPair("no beacon");

			List<ModuleQualityPair> aModules = new List<ModuleQualityPair>();
			foreach(JToken moduleToken in json["AModules"])
			{
				string moduleName = (string)moduleToken["Name"];
				string moduleQuality = (string)moduleToken["Quality"];
				Module module = cache.Modules.ContainsKey(moduleName) ? cache.Modules[moduleName] : null;
				Quality quality = cache.Qualities.ContainsKey(moduleQuality) ? cache.Qualities[moduleQuality] : cache.DefaultQuality;
				if (module != null)
					aModules.Add(new ModuleQualityPair(module, quality));
			}

            List<ModuleQualityPair> bModules = new List<ModuleQualityPair>();
            foreach (JToken moduleToken in json["BModules"])
            {
                string moduleName = (string)moduleToken["Name"];
                string moduleQuality = (string)moduleToken["Quality"];
                Module module = cache.Modules.ContainsKey(moduleName) ? cache.Modules[moduleName] : null;
                Quality quality = cache.Qualities.ContainsKey(moduleQuality) ? cache.Qualities[moduleQuality] : cache.DefaultQuality;
                if (module != null)
                    bModules.Add(new ModuleQualityPair(module, quality));
            }

			Item fuel = (json["Fuel"] != null && cache.Items.ContainsKey((string)json["Fuel"])) ? cache.Items[(string)json["Fuel"]] : null;

            NodeCopyOptions nco = new NodeCopyOptions(
				assemberQP,
				aModules,
				(double)json["Neighbours"],
				(double)json["ExtraProductivity"],
				fuel,
				beaconQP,
				bModules,
				beacons ? (double)json["BeaconCount"] : 0,
				beacons ? (double)json["BeaconsPA"] : 0,
				beacons ? (double)json["BeaconsC"] : 0);
			return nco;
		}
	}
}
