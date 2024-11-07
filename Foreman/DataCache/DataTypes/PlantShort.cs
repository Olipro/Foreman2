using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Foreman.DataCache.DataTypes {
	public class PlantShort : IEquatable<PlantShort> {
		public string Name { get; private set; }
		public long PlantID { get; private set; }
		[JsonProperty("isMissing")]
		public bool IsMissing { get; private set; }
		public Dictionary<string, double> Products { get; private set; }

		public PlantShort(string name) {
			Name = name;
			PlantID = -1;
			IsMissing = false;
			Products = [];
		}

		public PlantShort(IPlantProcess plantProcess) {
			Name = plantProcess.Name;
			PlantID = plantProcess.PlantID;
			IsMissing = plantProcess.IsMissing;

			Products = [];
			foreach (var kvp in plantProcess.ProductSet)
				Products.Add(kvp.Key.Name, kvp.Value);
		}

		public PlantShort(JToken plantProcess) {
			var Name = plantProcess["Name"]?.Value<string>();
			var PlantID = plantProcess["PlantID"]?.Value<long>();
			var isMissing = plantProcess["isMissing"]?.Value<bool>();
			var products = plantProcess["Products"];

			if (Name is null || PlantID is null || isMissing is null || products is null)
				throw new InvalidOperationException("Name, PlantID, isMissing or products field in JSON is null");

			this.Name = Name!;
			this.PlantID = (long)PlantID;
			this.IsMissing = (bool)isMissing;

			Products = products.Where(token => token is JProperty)
				.Select(token => token as JProperty)
				.OfType<JProperty>()
				.Select(ingredient => (ingredient.Name, ingredient.Value.ToObject<double>()))
				.ToDictionary();
		}

		public static List<PlantShort> GetSetFromJson(JToken jdata) {
			List<PlantShort> resultList = [];
			foreach (JToken recipe in jdata)
				resultList.Add(new PlantShort(recipe));
			return resultList;
		}

		public bool Equals(PlantShort? other) {
			return other is not null &&
				Name == other.Name &&
				Products.Count == other.Products.Count && 
				Products.SequenceEqual(other.Products);
		}

		public override bool Equals(object? obj) {
			return Equals(obj as PlantShort);
		}

		public override int GetHashCode() {
			return HashCode.Combine(Name, PlantID, IsMissing, Products);
		}
	}

	public class PlantShortNaInPrComparer : IEqualityComparer<PlantShort> //unlike the default plantshort comparer this one doesnt compare product quantities, just names
	{
		public bool Equals(PlantShort? x, PlantShort? y) {
			if (x == y)
				return true;

			if (x is null || y is null || x.Name != y.Name || x.Products.Count != y.Products.Count)
				return false;

			return x.Products.Keys.All(y.Products.ContainsKey);
		}

		public int GetHashCode(PlantShort obj) {
			return obj.GetHashCode();
		}

	}
}
