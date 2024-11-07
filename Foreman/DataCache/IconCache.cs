using ProtoBuf;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Foreman.DataCache {
	[Serializable]
	[ProtoContract]
	public partial class IconColorPair : IDisposable {
		[ProtoMember(1)]
		private byte[]? iconBytes;
		[ProtoMember(2)]
		private int color;

		private Bitmap? iconCache;

		public Bitmap? Icon {
			get {
				if (iconCache != null)
					return iconCache;
				if (iconBytes == null)
					return null;
				using (var strm = new MemoryStream(iconBytes))
					iconCache = new Bitmap(strm);
				return iconCache;
			}
			set {
				iconCache = value;
				if (iconCache == null) {
					iconBytes = null;
					return;
				}
				using var strm = new MemoryStream();
				iconCache.Save(strm, ImageFormat.Png);
				iconBytes = strm.ToArray();
			}
		}

		public Color Color {
			get {
				return Color.FromArgb(color);
			}
			set {
				color = value.ToArgb();
			}
		}

		public IconColorPair(Bitmap? icon, Color color) {
			Icon = icon;
			Color = color;
		}

		private IconColorPair() : this(null, Color.White) { }

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				iconCache?.Dispose();
			}
		}
	}
	[Serializable]
	[ProtoContract]
	public class IconBitmapCollection {
		[ProtoMember(1)]
		public Dictionary<string, IconColorPair> Icons;
		public IconBitmapCollection() { Icons = []; }
	}


	public static class IconCache {
		private static Bitmap? unknownIcon;
		public static Bitmap GetUnknownIcon() {
			unknownIcon ??= GetIcon(Path.Combine("Graphics", "UnknownIcon.png"), 32);
			return unknownIcon;
		}
		private static Bitmap? spoilageIcon;
		public static Bitmap GetSpoilageIcon() {
			spoilageIcon ??= GetIcon(Path.Combine("Graphics", "SpoilAssembler.png"), 96);
			return spoilageIcon;

		}
		private static Bitmap? plantingIcon;
		public static Bitmap GetPlantingIcon() {
			plantingIcon ??= GetIcon(Path.Combine("Graphics", "PlantAssembler.png"), 96);
			return plantingIcon;

		}
		public static Bitmap GetIcon(string path, int size) {
			try {
				using Bitmap image = new(path); //If you don't do this, the file is locked for the lifetime of the bitmap
				Bitmap bmp = new(size, size);
				using (Graphics g = Graphics.FromImage(bmp))
					g.DrawImage(image, new Rectangle(0, 0, size * image.Width / image.Height, size));
				return bmp;
			} catch (Exception) { return new Bitmap(size, size); }
		}

		public static Bitmap ConbineIcons(Bitmap aIcon, Bitmap bIcon, int size, bool diagonalSlice = true) {
			Bitmap result = new(size, size);
			using (Graphics g = Graphics.FromImage(result)) {
				using (GraphicsPath tlPath = new()) {
					tlPath.AddLine(0, 0, 0, size);
					tlPath.AddLine(0, size, size, 0);
					tlPath.AddLine(size, 0, 0, 0);
					if (diagonalSlice)
						g.Clip = new Region(tlPath);
					if (aIcon != null)
						g.DrawImage(aIcon, 0, 0, size, size);
				}

				using GraphicsPath trPath = new();
				trPath.AddLine(size, size, 0, size);
				trPath.AddLine(0, size, size, 0);
				trPath.AddLine(size, 0, size, size);
				if (diagonalSlice)
					g.Clip = new Region(trPath);
				if (bIcon != null)
					g.DrawImage(bIcon, 0, 0, size, size);
			}
			return result;
		}


		public static void SaveIconCache(string path, Dictionary<string, IconColorPair> iconCache) {
			IconBitmapCollection iCollection = new();

			foreach (KeyValuePair<string, IconColorPair> iconKVP in iconCache)
				iCollection.Icons.Add(iconKVP.Key, iconKVP.Value);

			if (File.Exists(path))
				File.Delete(path);
			using Stream stream = File.Open(path, FileMode.Create, FileAccess.Write);
			Serializer.Serialize(stream, iCollection);
		}

		public static async Task<Dictionary<string, IconColorPair>> LoadIconCache(string path, IProgress<KeyValuePair<int, string>> progress, int startingPercent, int endingPercent) {
			return await Task.Run(() => {
				Dictionary<string, IconColorPair> iconCache = [];
				try {
					using Stream stream = File.Open(path, FileMode.Open);
					IconBitmapCollection iCollection = Serializer.Deserialize<IconBitmapCollection>(stream);

					int totalCount = iCollection.Icons.Count;
					int counter = 0;
					foreach (KeyValuePair<string, IconColorPair> iconKVP in iCollection.Icons) {
						progress.Report(new KeyValuePair<int, string>(startingPercent + (endingPercent - startingPercent) * counter++ / totalCount, "Loading Icons..."));
						iconCache.Add(iconKVP.Key, iconKVP.Value);
					}
				} catch //there was an error reading the cache. Just ignore it and continue (we will have to load the icons from the files directly)
				  {
					iconCache.Clear();
					MessageBox.Show("Icon cache was corrupted. All icons will be empty.\nRecommendation: delete preset and import new one?");
				}
				return iconCache;
			});
		}
	}
}
