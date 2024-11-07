using Newtonsoft.Json;

namespace Foreman.Models.Nodes {
	[JsonObject(MemberSerialization.OptIn)]
	public class NodeLink {
		private readonly NodeLinkController controller;
		public NodeLinkController Controller { get { return controller; } }
		public ReadOnlyNodeLink ReadOnlyLink { get; protected set; }

		public ItemQualityPair Item { get; private set; }
		public double ThroughputPerSec { get; internal set; }
		public double Throughput { get { return ThroughputPerSec * MyGraph.GetRateMultipler(); } }
		public bool IsValid { get; private set; }

		public readonly ProductionGraph MyGraph;

		public readonly BaseNode SupplierNode;
		public readonly BaseNode ConsumerNode;

		[JsonProperty]
		public int SupplierID => SupplierNode.NodeID;
		[JsonProperty]
		public int ConsumerID => ConsumerNode.NodeID;
		[JsonProperty(nameof(Item))]
		public string JsonItem => Item.Item?.Name ?? "<ERROR>";
		[JsonProperty]
		public string Quality => Item.Quality?.Name ?? "";

		internal NodeLink(ProductionGraph myGraph, BaseNode supplier, BaseNode consumer, ItemQualityPair item) {
			MyGraph = myGraph;
			SupplierNode = supplier;
			ConsumerNode = consumer;
			Item = item;

			controller = NodeLinkController.GetController(this);
			ReadOnlyLink = new ReadOnlyNodeLink(this);

			IsValid = LinkChecker.IsPossibleConnection(Item, SupplierNode.ReadOnlyNode, ConsumerNode.ReadOnlyNode); //only need to check once -> item & recipe temperatures cant change.
		}

		public override string ToString() => string.Format("NodeLink for {0} ({1}) connecting {2} -> {3}", Item.Item?.Name, Item.Quality?.Name, SupplierNode.NodeID, ConsumerNode.NodeID);
	}

	public class ReadOnlyNodeLink(NodeLink link) {
		public ReadOnlyBaseNode Supplier => MyLink.SupplierNode.ReadOnlyNode;
		public ReadOnlyBaseNode Consumer => MyLink.ConsumerNode.ReadOnlyNode;

		public NodeDirection SupplierDirection => MyLink.SupplierNode.NodeDirection;
		public NodeDirection ConsumerDirection => MyLink.ConsumerNode.NodeDirection;

		public ItemQualityPair Item => MyLink.Item;
		public double Throughput => MyLink.Throughput;
		public bool IsValid => MyLink.IsValid;

		private readonly NodeLink MyLink = link;

		public override string ToString() { return "RO: " + MyLink.ToString(); }
	}

	public class NodeLinkController {
		private readonly NodeLink MyLink;

		protected NodeLinkController(NodeLink link) { MyLink = link; }

		public static NodeLinkController GetController(NodeLink link) {
			if (link.Controller != null)
				return link.Controller;
			return new NodeLinkController(link);
		}

		public void Delete() { MyLink.MyGraph.DeleteLink(MyLink.ReadOnlyLink); }
		public override string ToString() { return "C: " + MyLink.ToString(); }
	}
}
