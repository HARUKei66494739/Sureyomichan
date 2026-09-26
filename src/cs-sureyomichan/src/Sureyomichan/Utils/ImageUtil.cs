
using ImageMagick;
using LibAPNG;
using LibAPNG.WPF;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using WpfAnimatedGif.Decoding;
using GdipBitmap = System.Drawing.Bitmap;
using GdipGraphics = System.Drawing.Graphics;
using GdipImage = System.Drawing.Image;
using ImgObj = Haru.Kei.SureyomiChan.Models.Bindables.ImageObject;

namespace Haru.Kei.SureyomiChan.Utils;

internal class ImageUtil {
	class ImageStoreImpl : Models.IImageStore {
		private const string CacheFile = "image-cache.db";
		private const string CacheTable = "image";
		private static readonly object lockObj = new();
		private string __DbFile => Path.Combine(AppContext.BaseDirectory, CacheFile);

		public ImageStoreImpl() {
			if(File.Exists(this.__DbFile)) {
				try {
					File.Delete(this.__DbFile);
				}
				catch { }
			}
		}

		public byte[]? Get(string board, Helpers.ThreadId threadId, string imageName) {
			lock(lockObj) {
				try {
					using var db = new LiteDatabase(__DbFile);
					var ic = db.GetCollection<CacheObject>(CacheTable)
						.FindOne(x => (x.Board == board)
							&& (x.ThreadId == threadId.ToDbString())
							&& (x.FileName == imageName));
					return ic?.ImageBytes;
				}
				catch(LiteDB.LiteException e) {
					Logger.Instance.Error(e);
					return null;
				}
			}
		}

		public void Insert(string board, Helpers.ThreadId threadId, string imageName, byte[] imageBytes) {
			lock(lockObj) {
				using var db = new LiteDatabase(__DbFile);
				db.BeginTrans();
				try {
					db.GetCollection<CacheObject>(CacheTable)
						.Insert(new CacheObject() {
							Board = board,
							ThreadId = threadId.ToDbString(),
							Time = DateTime.Now,
							ImageBytes = imageBytes,
							FileName = imageName
						});
					db.Commit();
				}
				catch(LiteDB.LiteException e) {
					Logger.Instance.Error(e);
					db.Rollback();
				}
			}
		}

		public void Remove(string board, Helpers.ThreadId threadId) {
			lock(lockObj) {
				using var db = new LiteDatabase(__DbFile);
				db.BeginTrans();
				try {
					db.GetCollection<CacheObject>(CacheTable)
						.DeleteMany(x => (x.Board == board) && (x.ThreadId == threadId.ToDbString()));
					db.Commit();
				}
				catch(LiteDB.LiteException e) {
					Logger.Instance.Error(e);
					db.Rollback();
				}
			}
		}
	}

	public static Models.IImageStore ImageStore {
		get {
			if(field == null) {
				field = new ImageStoreImpl();
			}
			return field;
		}
	}

	public static ImgObj LoadPng(byte[] image) {
		// 8bitインデックスカラーPNGが読めないので対策する
		static Stream fixIndexdPng(byte[] imageBytes) {
			var stream = new MemoryStream();

			using var ms = new MemoryStream(imageBytes);
			using var bmp = new System.Drawing.Bitmap(ms);
			bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

			stream.Seek(0, SeekOrigin.Begin);
			return stream;
		}

		var p = new LibAPNG.APNG(image);

		var img = BitmapFrame.Create(
			((p.IHDRChunk.ColorType == 3) && p.IsSimplePNG) switch {
				true => fixIndexdPng(image),
				false => p.DefaultImage.GetStream(),
			},
			BitmapCreateOptions.None,
			BitmapCacheOption.OnLoad);
		if(p.IsSimplePNG) {
			return new ImgObj(img);
		} else {
			var animation = p.ToAnimation();
			var storyboard = new Storyboard();
			Storyboard.SetTargetProperty(
				animation,
				new PropertyPath(System.Windows.Controls.Image.SourceProperty));
			storyboard.Children.Add(animation);
			return new ImgObj(img, storyboard);
		}
	}

	public static ImgObj LoadGif(byte[] image) {
		using var stream = new MemoryStream(image);
		if(BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad) is GifBitmapDecoder decoder) {
			try {
				if(decoder?.Metadata != null) {
					if(!decoder.Frames.Skip(1).Any()) {
						return new(decoder.Frames.First());
					}

					stream.Seek(0, SeekOrigin.Begin);
					var gifFile = GifFile.ReadGifFile(stream, true);
					var width = gifFile.Header.LogicalScreenDescriptor.Width;
					var height = gifFile.Header.LogicalScreenDescriptor.Height;
					int index = 0;
					var keyFrames = new ObjectKeyFrameCollection();
					var totalDuration = TimeSpan.Zero;
					var baseFrame = default(BitmapSource);
					foreach(var rawFrame in decoder.Frames) {
						static bool isFull(GifImageDescriptor metadata, int w, int h) {
							return metadata.Left == 0
								   && metadata.Top == 0
								   && metadata.Width == w
								   && metadata.Height == h;
						}
						static BitmapSource clear(BitmapSource frame, GifImageDescriptor metadata) {
							DrawingVisual visual = new DrawingVisual();
							using(var context = visual.RenderOpen()) {
								var fullRect = new Rect(0, 0, frame.PixelWidth, frame.PixelHeight);
								var clearRect = new Rect(metadata.Left, metadata.Top, metadata.Width, metadata.Height);
								var clip = Geometry.Combine(
									new RectangleGeometry(fullRect),
									new RectangleGeometry(clearRect),
									GeometryCombineMode.Exclude,
									null);
								context.PushClip(clip);
								context.DrawImage(frame, fullRect);
							}

							var bitmap = new RenderTargetBitmap(
									frame.PixelWidth, frame.PixelHeight,
									frame.DpiX, frame.DpiY,
									PixelFormats.Pbgra32);
							bitmap.Render(visual);

							var result = new WriteableBitmap(bitmap);

							if(result.CanFreeze && !result.IsFrozen)
								result.Freeze();
							return result;
						}

						var ggce = gifFile.Frames[index].Extensions.OfType<GifGraphicControlExtension>().FirstOrDefault();
						var metadata = new {
							Descriptor = gifFile.Frames[index].Descriptor,
							Left = gifFile.Frames[index].Descriptor.Left,
							Top = gifFile.Frames[index].Descriptor.Top,
							Width = gifFile.Frames[index].Descriptor.Width,
							Height = gifFile.Frames[index].Descriptor.Height,
							Delay = ggce switch {
								GifGraphicControlExtension v when v.Delay != 0 => TimeSpan.FromMilliseconds(v.Delay),
								_ => TimeSpan.FromMilliseconds(100),
							},
							DisposalMethod = ggce switch {
								GifGraphicControlExtension v => (ImageBehavior.FrameDisposalMethod)v.DisposalMethod,
								_ => ImageBehavior.FrameDisposalMethod.None
							}
						};
						BitmapSource frame;
						if((baseFrame == null) && isFull(metadata.Descriptor, width, height)) {
							frame = rawFrame;
						} else {
							var visual = new DrawingVisual();
							using(var context = visual.RenderOpen()) {
								if(baseFrame != null) {
									context.DrawImage(baseFrame, new Rect(0, 0, width, height));
								}

								var rect = new Rect(metadata.Left, metadata.Top, metadata.Width, metadata.Height);
								context.DrawImage(rawFrame, rect);
							}
							var bitmap = new RenderTargetBitmap(
								width, height,
								96, 96,
								PixelFormats.Pbgra32);
							bitmap.Render(visual);

							frame = new WriteableBitmap(bitmap);
							if(frame.CanFreeze && !frame.IsFrozen) {
								frame.Freeze();
							}
						}

						keyFrames.Add(new DiscreteObjectKeyFrame(frame, totalDuration));
						totalDuration += metadata.Delay;
						baseFrame = metadata switch {
							var v when(v.DisposalMethod == ImageBehavior.FrameDisposalMethod.None) => frame,
							var v when(v.DisposalMethod == ImageBehavior.FrameDisposalMethod.DoNotDispose) => frame,
							var v when(v.DisposalMethod == ImageBehavior.FrameDisposalMethod.RestoreBackground) && isFull(v.Descriptor, width, height) => null,
							var v when(v.DisposalMethod == ImageBehavior.FrameDisposalMethod.RestoreBackground) => clear(frame, v.Descriptor),
							var v when(v.DisposalMethod == ImageBehavior.FrameDisposalMethod.RestorePrevious) => baseFrame,
							var v when((4 <= (int)v.DisposalMethod) && ((int)v.DisposalMethod <= 7)) => frame, // 4-7は仕様で未定義なのでとりあえずnoneとしてふるまう
							_ => throw new ArgumentException(),
						};
						index++;
					}
					// 無限に固定
					//var repeatCount = gifFile.RepeatCount;
					return new(decoder.Frames.First(), new ObjectAnimationUsingKeyFrames {
						KeyFrames = keyFrames,
						Duration = totalDuration,
						RepeatBehavior = RepeatBehavior.Forever,
						SpeedRatio = 1.0
					});
				}
			}
			catch { }
		}
		throw new Exceptions.ImageNotSupportException();
	}

	public static ImgObj LoadWebp(byte[] image) {
		using var ms = new MemoryStream(image);
		var d = BitmapDecoder.Create(
			ms,
			BitmapCreateOptions.None,
			BitmapCacheOption.OnLoad);
		var img = d.Frames[0];
		if(d.Frames.Count == 1) {
			return new ImgObj(img);
		} else {
			var kf = GetAwebpKeyFrame(image);
			if(kf.Count() != d.Frames.Count) {
				Utils.Logger.Instance.Error("フレーム数が一致しません");
			}
			var animation = new ObjectAnimationUsingKeyFrames();
			animation.RepeatBehavior = RepeatBehavior.Forever;
			var keyTime = TimeSpan.Zero;
			for(var i = 0; i < d.Frames.Count; i++) {
				animation.KeyFrames.Add(new DiscreteObjectKeyFrame() {
					KeyTime = keyTime,
					Value = d.Frames[i],
				});
				keyTime += TimeSpan.FromMilliseconds(kf.ElementAt(i));
			}
			animation.Duration = keyTime;

			var storyboard = new Storyboard();
			Storyboard.SetTargetProperty(
				animation,
				new PropertyPath(System.Windows.Controls.Image.SourceProperty));
			storyboard.Children.Add(animation);
			return new ImgObj(img, storyboard);
		}
	}


	public static GdipBitmap? GenBitmap(string fileName, byte[] image) {
		static GdipImage? gen(string fileName, byte[] image)
			=> Path.GetExtension(fileName)?.ToLower() switch {
				".png" => GdipBitmap.FromStream(new MemoryStream(image)),
				".jpg" => GdipBitmap.FromStream(new MemoryStream(image)),
				".jpeg" => GdipBitmap.FromStream(new MemoryStream(image)),
				".gif" => GdipBitmap.FromStream(new MemoryStream(image)),

				_ => null
			};
		return gen(fileName, image) switch {
			{ } v => new GdipBitmap(v),
			_ => null
		};
	}

	public static bool ConvertApng2Gif(byte[] image, string rootPath, string outFileNameWithoutExt) {
		try {
			var apng = new LibAPNG.APNG(image);
			if(!apng.IsSimplePNG) {
				using var mg = new MagickImageCollection();
				foreach(var img in apng.ToBitmapSources().Cast<WriteableBitmap>().Select((x, i) => (Image: x, Index: i))) {
					using var ms = new MemoryStream();
					BitmapEncoder enc = new PngBitmapEncoder();
					enc.Frames.Add(BitmapFrame.Create(img.Image));
					enc.Save(ms);
					ms.Position = 0;
					mg.Add(new MagickImage(ms));
					mg[img.Index].AnimationIterations = 0;
					mg[img.Index].AnimationDelay = (uint)(apng.Frames[img.Index].fcTLChunk.DelayNum switch {
						0 => 100,
						_ => (double)apng.Frames[img.Index].fcTLChunk.DelayNum / apng.Frames[img.Index].fcTLChunk.DelayDen switch {
							0 => 100,
							var v => v + 0.5
						} * 100
					});
				}
				WriteGifFile(mg, Path.Combine(rootPath, $"{outFileNameWithoutExt}.gif"));
				return true;
			} else {
				return false;
			}
		}
		catch(Exception ex) {
			throw new Exceptions.ImageConvertException("APNG2GIFでエラー", ex);
		}
	}


	public static bool ConvertAwebp2Gif(byte[] image, string rootPath, string outFileNameWithoutExt) {
		try {
			using var imageStream = new MemoryStream(image);
			var d = BitmapDecoder.Create(
				imageStream,
				BitmapCreateOptions.None,
				BitmapCacheOption.OnLoad);
			if(1 < d.Frames.Count) {
				var kf = GetAwebpKeyFrame(image);

				if(d.Frames.Count != kf.Count()) {
					throw new InvalidOperationException("フレーム数が一致しません");
				}

				using var mg = new MagickImageCollection();
				foreach(var img in d.Frames.Select((x, i) => (Image: x, Index: i))) {
					using var ms = new MemoryStream();
					BitmapEncoder enc = new PngBitmapEncoder();
					enc.Frames.Add(img.Image);
					enc.Save(ms);
					ms.Position = 0;
					mg.Add(new MagickImage(ms));
					mg[img.Index].AnimationIterations = 0;
					mg[img.Index].AnimationDelay = (uint)((double)kf.ElementAt(img.Index) / 10 + 0.5);
				}
				WriteGifFile(mg, Path.Combine(rootPath, $"{outFileNameWithoutExt}.gif"));
				return true;
			} else {
				using var fs = new FileStream(
					Path.Combine(rootPath, $"{outFileNameWithoutExt}.png"),
					FileMode.OpenOrCreate);
				BitmapEncoder enc = new PngBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create(d.Frames.First()));
				enc.Save(fs);
				return true;
			}
		}
		catch(Exception ex) {
			throw new Exceptions.ImageConvertException("AWEBP2GIFでエラー", ex);
		}
	}

	private static void WriteGifFile(MagickImageCollection mg, string outFilePath) {
		mg.Quantize(new QuantizeSettings() {
			Colors = 256
		});
		// OBSのイメージソースが最適化フォーマットのGIFを意図した描画をしないので最適化しない
		// 32.1.1
		// mg.Optimize();
		mg.Write(outFilePath);
	}

	private static IEnumerable<int> GetAwebpKeyFrame(byte[] image) {
		List<int> delay = new List<int>();

		// WEBP
		if(System.Text.Encoding.ASCII.GetString(image, 0, 4).Equals("RIFF")) {
			for(int i = 12; i < image.Length - 8; i += 8) {
				string chname = System.Text.Encoding.ASCII.GetString(image, i, 4);
				int chsize = BitConverter.ToInt32(image, i + 4);

				if(chname.Equals("ANMF")) {
					int offset = i + 8 + 12;
					byte[] dbuff = { image[offset], image[offset + 1], image[offset + 2], 0 };
					delay.Add(BitConverter.ToInt32(dbuff, 0));
				}
				i += chsize;
			}
		}

		return delay.AsReadOnly();
	}

}

file class CacheObject {
	public string Board { get; set; } = "";
	public string ThreadId { get; set; } = "";
	public DateTime Time { get; set; }
	public string FileName { get; set; } = "";
	public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
}

file static class Extension {
	extension(Helpers.ThreadId source) {
		// #2219対策
		// https://github.com/litedb-org/LiteDB/issues/2219
		public string ToDbString() => Regex.Replace($"{source}", "[{}]", "_");
	}

}