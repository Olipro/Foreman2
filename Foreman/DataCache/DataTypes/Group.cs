using System.Collections.Generic;

namespace Foreman.DataCache.DataTypes {
	public interface IGroup : IDataObjectBase {
		IReadOnlyList<ISubgroup> Subgroups { get; }
	}

	public interface ISubgroup : IDataObjectBase {
		IGroup? MyGroup { get; }
		IReadOnlyList<IRecipe> Recipes { get; }
		IReadOnlyList<IItem> Items { get; }
	}


	public class GroupPrototype(DCache dCache, string name, string lname, string order) : DataObjectBasePrototype(dCache, name, lname, order), IGroup {
		public IReadOnlyList<ISubgroup> Subgroups => subgroups;

		internal List<SubgroupPrototype> subgroups = [];

		public void SortSubgroups() { subgroups.Sort(); } //sort them by their order string

		public override string ToString() { return string.Format("Group: {0}", Name); }
	}

	public class SubgroupPrototype(DCache dCache, string name, string order) : DataObjectBasePrototype(dCache, name, name, order), ISubgroup {
		public IGroup? MyGroup => myGroup;

		public IReadOnlyList<IRecipe> Recipes => recipes;
		public IReadOnlyList<IItem> Items => items;

		internal GroupPrototype? myGroup;

		internal List<RecipePrototype> recipes = [];
		internal List<ItemPrototype> items = [];

		public void SortIRs() { recipes.Sort(); items.Sort(); } //sort them by their order string

		public override string ToString() { return string.Format("Subgroup: {0}", Name); }
	}
}
