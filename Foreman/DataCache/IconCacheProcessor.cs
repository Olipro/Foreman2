﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Foreman.DataCache {

	public struct IconInfo(string iconPath, int iconSize) {
		public string iconPath = iconPath;
		public int iconSize = iconSize;
		public double iconScale = 1;
		public Point iconOffset = new(0, 0);
		public Color iconTint = IconCacheProcessor.NoTint;

		public void SetIconTint(double a, double r, double g, double b) {
			a = a <= 1 ? a * 255 : a;
			r = r <= 1 ? r * 255 : r;
			g = g <= 1 ? g * 255 : g;
			b = b <= 1 ? b * 255 : b;
			iconTint = Color.FromArgb((int)a, (int)r, (int)g, (int)b);
		}
	}

	public partial class IconCacheProcessor : IDisposable {
		internal static readonly Color NoTint = Color.White;

		public int TotalPathCount { get; private set; }
		public int FailedPathCount { get; private set; }

		private readonly Dictionary<string, IconColorPair> myIconCache;

		private readonly Dictionary<string, string> folderLinks;
		private Dictionary<string, ZipArchiveEntry>? archiveFileLinks;
		private List<ZipArchive>? openedArchives;
		private Dictionary<string, Bitmap?>? bitmapCache; //just so we dont have to load the same file multiple times

		public IconCacheProcessor() {
			TotalPathCount = 0;
			FailedPathCount = 0;

			myIconCache = [];

			folderLinks = [];
			archiveFileLinks = [];
			openedArchives = [];
			bitmapCache = [];
		}

		public bool PrepareModPaths(Dictionary<string, string> modSet, string modsPath, string dataPath, CancellationToken token) {
			folderLinks.Clear();
			archiveFileLinks?.Clear();
			bitmapCache?.Clear();

			//factorio checks for foldeer <name>_<version>, then folder <name> then zip <name>_<version>
			//if zip, then the actual files can either be in the root of zip, or in <name> foler, or in <name>_<version> folder
			//NOTE: versions are of type v1.v2.v3 where each number can have any amount of leading zeros
			foreach (KeyValuePair<string, string> mod in modSet) {
				if (token.IsCancellationRequested)
					return false;

				string versionMatch = string.Join(".", mod.Value.Split('.').Select(s => "0*" + int.Parse(s).ToString()));

				string[] folders = Directory.GetDirectories(modsPath);
				string[] files = Directory.GetFiles(modsPath);
				const StringComparison ccIgnoreCase = StringComparison.CurrentCultureIgnoreCase;

				string? foundFolder = folders.FirstOrDefault(f => Regex.IsMatch((Path.GetFileName(f) ?? throw new InvalidOperationException(f ?? "<null>" + " is invalid")).ToLower(), string.Format("{0}_{1}", mod.Key, versionMatch)));
				foundFolder ??= folders.FirstOrDefault(f => Path.GetFileName(f).Equals(mod.Key, ccIgnoreCase));

				if (foundFolder != null)
					folderLinks.Add("__" + mod.Key.ToLower() + "__", foundFolder);
				else {
					string? foundFile = files.FirstOrDefault(f => Regex.IsMatch((Path.GetFileName(f) ?? throw new InvalidOperationException(f ?? "<null>" + " is invalid")).ToLower(), string.Format("{0}_{1}.zip", mod.Key, versionMatch)));
					
					if (foundFile == null) {
						if (!mod.Key.Equals("core", ccIgnoreCase) && !mod.Key.Equals("base", ccIgnoreCase) && !mod.Key.Equals("elevated-rails", ccIgnoreCase) && !mod.Key.Equals("quality", ccIgnoreCase) && !mod.Key.Equals("space-age", ccIgnoreCase))
							return false;
						continue;
					}

					//for zip files, since we have to iterate through them for each file we might as well make a full link of every possible filepath to given entry
					ZipArchive zip = ZipFile.Open(foundFile, ZipArchiveMode.Read);
					openedArchives?.Add(zip);
					foreach (ZipArchiveEntry zentity in zip.Entries) {
						if (zentity.Name == "")
							continue; //folder

						LinkedList<string> brokenPath = new();
						string? filePath = zentity.FullName;
						while (filePath is not null && filePath != "") {
							brokenPath.AddFirst(Path.GetFileName(filePath));
							filePath = Path.GetDirectoryName(filePath);
						}
						if (brokenPath.First is not null)
							brokenPath.First.Value = "__" + mod.Key.ToLower() + "__";
						archiveFileLinks?.Add(Path.Combine([.. brokenPath]).ToLower(), zentity);
					}
				}
			}
			folderLinks.Add("__core__", Path.Combine(dataPath, "core"));
			folderLinks.Add("__base__", Path.Combine(dataPath, "base"));
			folderLinks.Add("__elevated-rails__", Path.Combine(dataPath, "elevated-rails"));
			folderLinks.Add("__quality__", Path.Combine(dataPath, "quality"));
			folderLinks.Add("__space-age__", Path.Combine(dataPath, "space-age"));

			return true;
		}

		public bool CreateIconCache(JObject iconJObject, string cachePath, IProgress<KeyValuePair<int, string>> progress, int startingPercent, int endingPercent, CancellationToken token) {
			TotalPathCount = 0;
			FailedPathCount = 0;

			myIconCache.Clear();
			bitmapCache?.Clear();

			int totalCount =
				iconJObject["technologies"]?.Count() ?? 0 +
				iconJObject["recipes"]?.Count() ?? 0 +
				iconJObject["items"]?.Count() ?? 0 +
				iconJObject["fluids"]?.Count() ?? 0 +
				iconJObject["entities"]?.Count() ?? 0 +
				iconJObject["groups"]?.Count() ?? 0 +
				iconJObject["qualities"]?.Count() ?? 0;

			progress.Report(new KeyValuePair<int, string>(startingPercent, "Creating icons."));
			int counter = 0;
			foreach (var iconJToken in iconJObject["technologies"]?.ToList() ?? []) {
				if (token.IsCancellationRequested) return false;
				progress.Report(new KeyValuePair<int, string>(startingPercent + (endingPercent - startingPercent) * counter++ / totalCount, ""));
				ProcessIcon(iconJToken, 256);
			}
			foreach (var iconJToken in iconJObject["recipes"]?.ToList() ?? []) {
				if (token.IsCancellationRequested) return false;
				progress.Report(new KeyValuePair<int, string>(startingPercent + (endingPercent - startingPercent) * counter++ / totalCount, ""));
				ProcessIcon(iconJToken, 32);
			}
			foreach (var iconJToken in iconJObject["items"]?.ToList() ?? []) {
				if (token.IsCancellationRequested) return false;
				progress.Report(new KeyValuePair<int, string>(startingPercent + (endingPercent - startingPercent) * counter++ / totalCount, ""));
				ProcessIcon(iconJToken, 32);
			}
			foreach (var iconJToken in iconJObject["fluids"]?.ToList() ?? []) {
				if (token.IsCancellationRequested) return false;
				progress.Report(new KeyValuePair<int, string>(startingPercent + (endingPercent - startingPercent) * counter++ / totalCount, ""));
				ProcessIcon(iconJToken, 32);
			}
			foreach (var iconJToken in iconJObject["entities"]?.ToList() ?? []) {
				if (token.IsCancellationRequested) return false;
				progress.Report(new KeyValuePair<int, string>(startingPercent + (endingPercent - startingPercent) * counter++ / totalCount, ""));
				ProcessIcon(iconJToken, 64);
			}
			foreach (var iconJToken in iconJObject["groups"]?.ToList() ?? []) {
				if (token.IsCancellationRequested) return false;
				progress.Report(new KeyValuePair<int, string>(startingPercent + (endingPercent - startingPercent) * counter++ / totalCount, ""));
				ProcessIcon(iconJToken, 64);
			}
			foreach (var iconJToken in iconJObject["qualities"]?.ToList() ?? []) {
				if (token.IsCancellationRequested) return false;
				progress.Report(new KeyValuePair<int, string>(startingPercent + (endingPercent - startingPercent) * counter++ / totalCount, ""));
				ProcessIcon(iconJToken, 32);
			}

			IconCache.SaveIconCache(cachePath, myIconCache);

			return FailedPathCount == 0;
		}

		private void ProcessIcon(JToken objJToken, int defaultIconSize) {
			if (objJToken["icon_data"]?.Type != JTokenType.Null) {
				string iconName = objJToken["icon_name"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key");

				JToken iconDataJToken = objJToken["icon_data"] ?? throw new InvalidOperationException("Missing JSON key");

				string mainIconPath = iconDataJToken["icon"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key");
				int baseIconSize = iconDataJToken["icon_size"]?.Value<int>() ?? 32;

				IconInfo iicon = new(mainIconPath, baseIconSize);
				iicon.iconScale = defaultIconSize / iicon.iconSize;

				List<IconInfo> iicons = [];
				List<JToken> iconJTokens = [.. iconDataJToken["icons"]];
				foreach (var iconJToken in iconJTokens) {
					IconInfo picon = new(iconJToken["icon"]?.Value<string>() ?? throw new InvalidOperationException("Missing JSON key"), iconJToken["icon_size"]?.Value<int>() ?? baseIconSize);
					picon.iconScale = iconJToken["scale"]?.Value<double>() ?? defaultIconSize / picon.iconSize;

					picon.iconOffset = new Point(iconJToken["shift"]?[0]?.Value<int>() ?? throw new InvalidOperationException("Missing JSON key"), iconJToken["shift"]?[1]?.Value<int>() ?? throw new InvalidOperationException("Missing JSON key"));
					picon.SetIconTint(iconJToken["tint"]?[3]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key"), iconJToken["tint"]?[0]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key"), iconJToken["tint"]?[1]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key"), iconJToken["tint"]?[2]?.Value<double>() ?? throw new InvalidOperationException("Missing JSON key"));
					iicons.Add(picon);
				}
				if (!myIconCache.ContainsKey(iconName))
					myIconCache.Add(iconName, GetIconAndColor(iicon, iicons, defaultIconSize));
			}
		}


		public IconColorPair GetIconAndColor(IconInfo iinfo, List<IconInfo> iinfos, int defaultCanvasSize) {
			iinfos ??= [];
			double IconCanvasScale = defaultCanvasSize == 32 ? 2 : 1; //just some upscailing for icons (item icons are set at 32x32, but they look better at 64x64)
			int IconCanvasSize = (int)(defaultCanvasSize * IconCanvasScale);

			if (iinfos.Count == 0) //if there are no icons, use the single icon
				iinfos.Add(iinfo);

			//quick check to ensure it isnt a null icon
			bool empty = true;
			foreach (IconInfo ii in iinfos) {
				if (!string.IsNullOrEmpty(ii.iconPath))
					empty = false;
			}
			if (empty)
				return new IconColorPair(null, Color.Black);

			//prepare the canvas - we will add each successive icon/layer on top of it
			Bitmap canvas = new(IconCanvasSize, IconCanvasSize, PixelFormat.Format32bppPArgb);
			BitmapData canvasData = canvas.LockBits(new Rectangle(0, 0, canvas.Width, canvas.Height), ImageLockMode.ReadWrite, canvas.PixelFormat);
			int cBPP = Image.GetPixelFormatSize(canvas.PixelFormat) / 8;
			int bCount = canvasData.Stride * canvas.Height;
			byte[] canvasPixels = new byte[bCount];
			nint ptrCanvasFPixel = canvasData.Scan0;
			Marshal.Copy(ptrCanvasFPixel, canvasPixels, 0, canvasPixels.Length);
			int heightInPixels = canvasData.Height;
			int widthInBytes = canvasData.Width * cBPP;

			foreach (IconInfo ii in iinfos) {
				//load the image and prep it for processing
				int iconSize = ii.iconSize > 0 ? ii.iconSize : iinfo.iconSize;
				int iconDrawSize = (int)(iconSize * (ii.iconScale > 0 ? ii.iconScale : (double)defaultCanvasSize / iconSize));
				iconDrawSize = (int)(iconDrawSize * IconCanvasScale);

				Bitmap? iconImage = LoadImageFromMod(ii.iconPath, iconDrawSize);
				if (iconImage == null)
					continue;

				//draw the icon onto a layer (that we will apply tint to and blend with canvas)
				Bitmap layerSlice = new(canvas.Width, canvas.Height, canvas.PixelFormat);
				using (Graphics g = Graphics.FromImage(layerSlice))
					g.DrawImageUnscaled(iconImage, IconCanvasSize / 2 - iconDrawSize / 2 + ii.iconOffset.X, IconCanvasSize / 2 - iconDrawSize / 2 + ii.iconOffset.Y);

				//grab the layer data
				BitmapData layerData = layerSlice.LockBits(new Rectangle(0, 0, canvas.Width, canvas.Height), ImageLockMode.ReadOnly, canvas.PixelFormat);
				byte[] layerPixels = new byte[bCount];
				nint ptrLayerFPixel = layerData.Scan0;
				Marshal.Copy(ptrLayerFPixel, layerPixels, 0, layerPixels.Length);

				//blend -> for each value in 0->1 (so when multiplying, you have to divide by 255 if in 0->255)
				//newCanvas(A/R/G/B) = Layer(A/R/G/B) * tint(A/R/G/B)   +   oldCanvas(A/R/G/B) * (1 - tint(A) * Layer(A))
				//https://www.factorio.com/blog/post/fff-172
				for (int y = 0; y < heightInPixels; y++) {
					int currentLine = y * canvasData.Stride;
					for (int x = 0; x < widthInBytes; x += cBPP) {
						int canvasMulti = 255 - ii.iconTint.A * layerPixels[currentLine + x + 3] / 255;
						canvasPixels[currentLine + x + 0] = (byte)Math.Min(255,
							layerPixels[currentLine + x + 0] * ii.iconTint.B / 255 +
							canvasPixels[currentLine + x + 0] * canvasMulti / 255);
						canvasPixels[currentLine + x + 1] = (byte)Math.Min(255,
							layerPixels[currentLine + x + 1] * ii.iconTint.G / 255 +
							canvasPixels[currentLine + x + 1] * canvasMulti / 255);
						canvasPixels[currentLine + x + 2] = (byte)Math.Min(255,
							layerPixels[currentLine + x + 2] * ii.iconTint.R / 255 +
							canvasPixels[currentLine + x + 2] * canvasMulti / 255);
						canvasPixels[currentLine + x + 3] = (byte)Math.Min(255,
							layerPixels[currentLine + x + 3] * ii.iconTint.A / 255 +
							canvasPixels[currentLine + x + 3] * canvasMulti / 255);

					}
				}
				layerSlice.UnlockBits(layerData);
			}

			//we are done adding all the layers, so copy the canvas data
			Marshal.Copy(canvasPixels, 0, ptrCanvasFPixel, canvasPixels.Length);
			canvas.UnlockBits(canvasData);

			//at this point we need to convert the canvas into a non-alpha multiplied format due to winforms having issues with it
			Bitmap result = new(canvas.Width, canvas.Height, PixelFormat.Format32bppArgb);
			using (Graphics g = Graphics.FromImage(result))
				g.DrawImageUnscaled(canvas, 0, 0);

			//finally, calculate the average color (yes, it comes out a bit different due to inclusion of transparency)
			Color averageColor = GetAverageColor(result);
			if (averageColor.GetBrightness() > 0.9)
				result = AddBorder(result); //if the image is too bright, add a border to it. Honestly, this is never done anymore - it was useful before layer blending was fixed and some icons came out... white.
			if (averageColor.GetBrightness() > 0.7)
				averageColor = Color.FromArgb(255, (int)(averageColor.R * 0.7), (int)(averageColor.G * 0.7), (int)(averageColor.B * 0.7));

			return new IconColorPair(result, averageColor);
		}

		private Bitmap? LoadImageFromMod(string fileName, int resultSize = 32) //NOTE: must make sure we use pre-multiplied alpha
		{
			if (string.IsNullOrEmpty(fileName))
				return null;
			fileName = fileName.ToLower().Replace("/", "\\");
			while (fileName.Contains("\\\\", StringComparison.CurrentCulture)) //found this error in krastorio - apparently factorio ignores multiple slashes in file name
				fileName = fileName.Replace("\\\\", "\\");

			//if the image isnt currently in the cache, process it and add it to cache
			if (bitmapCache is not null && !bitmapCache.ContainsKey(fileName)) {
				TotalPathCount++;
				string origin = fileName[..(fileName.IndexOf("__", 2) + 2)];
				string file = fileName[(fileName.IndexOf("__", 2) + 3)..];

				if (folderLinks.TryGetValue(origin, out string? value)) {

					file = Path.Combine(value, file);
					try { bitmapCache.Add(fileName, new Bitmap(file)); } catch {
						bitmapCache.Add(fileName, null);
						FailedPathCount++;
						ErrorLogging.LogLine("IconCacheProcessor: given fileName not found in mod folders: " + fileName);
					}

				} else if (archiveFileLinks?.TryGetValue(fileName, out ZipArchiveEntry? entry) ?? false) {
					try { bitmapCache.Add(fileName, new Bitmap(entry.Open())); } catch {
						bitmapCache.Add(fileName, null);
						FailedPathCount++;
						ErrorLogging.LogLine("IconCacheProcessor: given fileName not found in mod folders: " + fileName);
					}

				} else {
					FailedPathCount++;
					bitmapCache.Add(fileName, null);
					ErrorLogging.LogLine("IconCacheProcessor: given fileName not found in mod folders: " + fileName);
				}
			}

			if (bitmapCache?[fileName] == null)
				return null;

			//get the requested image from the cache and draw it to correct size.
			Bitmap? image = bitmapCache[fileName];
			Bitmap bmp = new(resultSize, resultSize, PixelFormat.Format32bppPArgb);
			if (image is null)
				throw new InvalidOperationException(fileName + " does not exist in Bitmap cache");
			using (Graphics g = Graphics.FromImage(bmp)) {
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				g.DrawImage(image, new Rectangle(0, 0, resultSize * image.Width / image.Height, resultSize));
			}
			return bmp;
		}

		private static Color GetAverageColor(Bitmap icon) {
			if (icon == null)
				return Color.Black;

			BitmapData iconData = icon.LockBits(new Rectangle(0, 0, icon.Width, icon.Height), ImageLockMode.ReadOnly, icon.PixelFormat);
			int bytesPerPixel = Image.GetPixelFormatSize(icon.PixelFormat) / 8;
			int byteCount = iconData.Stride * icon.Height;
			byte[] iconPixels = new byte[byteCount];
			nint ptrFirstPixel = iconData.Scan0;
			Marshal.Copy(ptrFirstPixel, iconPixels, 0, iconPixels.Length);
			int heightInPixels = iconData.Height;
			int widthInBytes = iconData.Width * bytesPerPixel;

			int[] totalPixel = [0, 0, 0, 0];
			int totalCounter = 1; //just to avoid div by 0 in case of completely empty bitmap
			for (int y = 0; y < heightInPixels; y++) {
				int currentLine = y * iconData.Stride;
				for (int x = 0; x < widthInBytes; x += bytesPerPixel) {
					if (iconPixels[currentLine + x + 3] > 10) //ignore transparent pixels
					{
						totalPixel[3] += iconPixels[currentLine + x];     //B
						totalPixel[2] += iconPixels[currentLine + x + 1]; //G
						totalPixel[1] += iconPixels[currentLine + x + 2]; //R
						totalCounter++;
					}
				}
			}
			for (int i = 1; i < 4; i++) {
				totalPixel[i] /= totalCounter;
				totalPixel[i] = Math.Min(totalPixel[i], 255);
			}
			icon.UnlockBits(iconData);

			return Color.FromArgb(255, totalPixel[1], totalPixel[2], totalPixel[3]);
		}

		private const int iconBorder = 1; //border is drawn on a new layer as 
		private static Bitmap AddBorder(Bitmap icon) {
			Bitmap canvas = new(icon.Width, icon.Height, icon.PixelFormat);
			BitmapData iconData = icon.LockBits(new Rectangle(0, 0, icon.Width, icon.Height), ImageLockMode.ReadOnly, icon.PixelFormat);
			BitmapData canvasData = canvas.LockBits(new Rectangle(0, 0, icon.Width, icon.Height), ImageLockMode.WriteOnly, icon.PixelFormat);
			int bytesPerPixel = Image.GetPixelFormatSize(icon.PixelFormat) / 8; //same for both
			int byteCount = iconData.Stride * icon.Height; //same for both
			byte[] iconPixels = new byte[byteCount];
			byte[] canvasPixels = new byte[byteCount];

			nint ptrFirstPixel = iconData.Scan0;
			Marshal.Copy(ptrFirstPixel, iconPixels, 0, iconPixels.Length);
			int heightInPixels = iconData.Height;
			int widthInBytes = iconData.Width * bytesPerPixel;

			for (int y = iconBorder; y < heightInPixels - iconBorder; y++) {
				int currentLine = y * iconData.Stride;
				for (int x = iconBorder * bytesPerPixel; x < widthInBytes - iconBorder * bytesPerPixel; x += bytesPerPixel) {
					if (iconPixels[currentLine + x + 3] > 11) //check if A >= 10
					{
						for (int iy = -iconBorder; iy <= iconBorder; iy++) {
							for (int ix = -iconBorder * bytesPerPixel; ix <= iconBorder * bytesPerPixel; ix += bytesPerPixel) {
								int currentCanvasIndex = currentLine + iy * iconData.Stride + x + ix;
								canvasPixels[currentCanvasIndex] = 64;
								canvasPixels[currentCanvasIndex + 1] = 64;
								canvasPixels[currentCanvasIndex + 2] = 64;
								canvasPixels[currentCanvasIndex + 3] = 64;
							}
						}
					}
				}
			}
			ptrFirstPixel = canvasData.Scan0;
			Marshal.Copy(canvasPixels, 0, ptrFirstPixel, canvasPixels.Length);
			icon.UnlockBits(iconData);
			canvas.UnlockBits(canvasData);

			//draw the processed icon (singluar) onto the main canvas
			using (Graphics g = Graphics.FromImage(canvas)) {
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				g.DrawImageUnscaled(icon, 0, 0);
			}

			return canvas;
		}

		private bool disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					archiveFileLinks?.Clear();
					archiveFileLinks = null;

					foreach (Bitmap? bitmap in (bitmapCache ?? []).Values)
						bitmap?.Dispose();
					bitmapCache?.Clear();
					bitmapCache = null;

					foreach (ZipArchive zip in openedArchives ?? [])
						zip.Dispose();
					openedArchives?.Clear();
					openedArchives = null;
				}
				disposedValue = true;
			}
		}
		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private static readonly Dictionary<KeyValuePair<Bitmap, Bitmap>, Bitmap> combinedBitmapDictionary = [];
		private const double qualitySizeMultiplier = 0.5;
		public static Bitmap CombinedQualityIcon(Bitmap baseIcon, Bitmap qualityIcon) {
			if (baseIcon == null)
				return IconCache.GetUnknownIcon();

			if (combinedBitmapDictionary.TryGetValue(new KeyValuePair<Bitmap, Bitmap>(baseIcon, qualityIcon), out Bitmap? combinedBitmap))
				return combinedBitmap;

			//combine the two bitmaps
			Bitmap canvas = new(baseIcon.Width, baseIcon.Height, baseIcon.PixelFormat);
			using (Graphics g = Graphics.FromImage(canvas)) {
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				g.DrawImage(baseIcon, new Rectangle(0, 0, baseIcon.Width, baseIcon.Height));
				g.DrawImage(qualityIcon, new Rectangle((int)(baseIcon.Width * (1 - qualitySizeMultiplier)), (int)(baseIcon.Height * (1 - qualitySizeMultiplier)), (int)(baseIcon.Width * qualitySizeMultiplier), (int)(baseIcon.Height * qualitySizeMultiplier)));
			}
			combinedBitmapDictionary.Add(new KeyValuePair<Bitmap, Bitmap>(baseIcon, qualityIcon), canvas);
			return canvas;
		}
	}
}
