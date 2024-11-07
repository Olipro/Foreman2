using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Foreman.DataCache.DataTypes {
	public class RecipeShort : IEquatable<RecipeShort> {
		public string Name { get; private set; }
		public long RecipeID { get; private set; }
		[JsonProperty("isMissing")]
		public bool IsMissing { get; private set; }
		public Dictionary<string, double> Ingredients { get; private set; }
		public Dictionary<string, double> Products { get; private set; }

		public RecipeShort(string name) {
			Name = name;
			RecipeID = -1;
			IsMissing = false;
			Ingredients = [];
			Products = [];
		}

		public RecipeShort(IRecipe recipe) {
			Name = recipe.Name;
			RecipeID = recipe.RecipeID;
			IsMissing = recipe.IsMissing;

			Ingredients = [];
			foreach (var kvp in recipe.IngredientSet)
				Ingredients.Add(kvp.Key.Name, kvp.Value);
			Products = [];
			foreach (var kvp in recipe.ProductSet)
				Products.Add(kvp.Key.Name, kvp.Value);
		}

		public RecipeShort(JToken recipe) {
			var Name = recipe["Name"]?.Value<string>();
			var RecipeID = recipe["RecipeID"]?.Value<long>();
			var isMissing = recipe["isMissing"]?.Value<bool>();
			var ingredients = recipe["Ingredients"];
			var products = recipe["Products"];

			if (Name is null || RecipeID is null || isMissing is null || ingredients is null || products is null)
				throw new InvalidOperationException("Name, RecipeID, isMissing, ingredients or products field in JSON is null");
			this.Name = Name;
			this.RecipeID = (long)RecipeID;
			this.IsMissing = (bool)isMissing;

			Ingredients = ingredients.Where(token => token is JProperty)
				.Select(token => token as JProperty)
				.OfType<JProperty>()
				.Select(ingredient => (ingredient.Name, ingredient.Value.ToObject<double>()))
				.ToDictionary();
			Products = products.Where(token => token is JProperty)
				.Select(token => token as JProperty)
				.OfType<JProperty>()
				.Select(ingredient => (ingredient.Name, ingredient.Value.ToObject<double>()))
				.ToDictionary();
		}

		public static List<RecipeShort> GetSetFromJson(JToken jdata) {
			List<RecipeShort> resultList = [];
			foreach (JToken recipe in jdata)
				resultList.Add(new RecipeShort(recipe));
			return resultList;
		}

		public bool Equals(RecipeShort? other) {
			return other is not null && Name == other.Name &&
				Ingredients.Count == other.Ingredients.Count && Ingredients.SequenceEqual(other.Ingredients) &&
				Products.Count == other.Products.Count && Products.SequenceEqual(other.Products);
		}

		public override bool Equals(object? obj) => Equals(obj as RecipeShort);

		public override int GetHashCode() => HashCode.Combine(Name, RecipeID, IsMissing, Ingredients, Products);
	}

	public class RecipeShortNaInPrComparer : IEqualityComparer<RecipeShort> //unlike the default recipeshort comparer this one doesnt compare ingredient & product quantities, just names
	{
		public bool Equals(RecipeShort? x, RecipeShort? y) {
			if (x == y)
				return true;

			if (x is null || y is null || x.Name != y.Name)
				return false;
			if (x.Ingredients.Count != y.Ingredients.Count)
				return false;
			if (x.Products.Count != y.Products.Count)
				return false;

			foreach (string i in x.Ingredients.Keys)
				if (!y.Ingredients.ContainsKey(i))
					return false;
			foreach (string i in x.Products.Keys)
				if (!y.Products.ContainsKey(i))
					return false;

			return true;
		}

		public int GetHashCode(RecipeShort obj) {
			return obj.GetHashCode();
		}
	}
}
