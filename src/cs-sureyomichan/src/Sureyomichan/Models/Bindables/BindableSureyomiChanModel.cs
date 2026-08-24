using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Haru.Kei.SureyomiChan.Models.Bindables; 

class BindableSureyomiChanModel : INotifyPropertyChanged {
	public class ImageItem(
		SureyomiChanBoardId boardId,
		Helpers.ThreadId threadId,
		AttachmentObject? attachment,
		bool? hasImage = null) : INotifyPropertyChanged {

		public event PropertyChangedEventHandler? PropertyChanged;
		private readonly string imageKey = attachment switch {
			{ } v when v.ImageFileBytes is { } && !string.IsNullOrEmpty(v.ImageName) => v.ImageName,
			_ => ""
		};
		private readonly bool has = hasImage ?? attachment?.ImageFileBytes switch {
			{ } => true,
			_ => false,
		};

		private WeakReference<ImageObject>? image = null;

		public ImageObject? Image {
			get {
				if(!this.has) {
					return null;
				}

				if(this.image?.TryGetTarget(out var io) ?? false) {
					return io;
				}

				var ib = Utils.ImageUtil.ImageStore.Get(
					SureyomiChanEnviroment.GetStaticString(boardId),
					threadId,
					this.imageKey);
				if(ib is null) {
					return null;
				}

				var r = LoadImage(this.imageKey, ib);
				this.image = new WeakReference<ImageObject>(r);
				return r;
			}
		}

		private static ImageObject LoadImage(string imageName, byte[] imageBytes) {
			return Path.GetExtension(imageName).ToLower() switch {
				".png" => Utils.ImageUtil.LoadPng(imageBytes),
				".webp" => Utils.ImageUtil.LoadWebp(imageBytes),
				".gif" => Utils.ImageUtil.LoadGif(imageBytes),
				_ => new ImageObject(BitmapFrame.Create(new MemoryStream(imageBytes)))
			};
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public IReadOnlyReactiveProperty<int> ResIndex { get; }
	public IReadOnlyReactiveProperty<string> No { get; }
	public IReadOnlyReactiveProperty<string> PostTime { get; }
	public IReadOnlyReactiveProperty<Visibility> EmailVisibility { get; }
	public IReadOnlyReactiveProperty<string> Email { get; }
	public IReadOnlyReactiveProperty<string> Body { get; }
	public IReadOnlyReactiveProperty<string?> Id { get; }
	public IReadOnlyReactiveProperty<bool> IsId { get; }
	public IReadOnlyReactiveProperty<string> IdString { get; }

	public IReadOnlyReactiveProperty<Visibility> ImageVisibility { get; }
	public IReadOnlyReactiveProperty<Visibility> ImageErrorVisibility { get; }
	public IReadOnlyReactiveProperty<string?> ImageName { get; }

	private WeakReference<ImageObject>? image = null;
	public ImageObject? Image {
		get {
			if(!this.hasImage) {
				return null;
			}

			if(this.image?.TryGetTarget(out var io) ?? false) {
				return io;
			}

			var ib = Utils.ImageUtil.ImageStore.Get(
				SureyomiChanEnviroment.GetStaticString(this.Model.Interaction.BoardId),
				this.Model.ThreadId,
				this.imageKey);
			if(ib is null) {
				return null;
			}

			var r = LoadImage(this.imageKey, ib);
			this.image = new WeakReference<ImageObject>(r);
			return r;
		}
	}
	public ReactiveCollection<ImageItem> SubImages { get; } = [];


	public SureyomiChanModel Model { get; }

	public ReactivePropertySlim<int> DeleteProgress { get; } = new(initialValue: 0);
	public ReactivePropertySlim<bool> IndeterminateDeleteProgress { get; } = new(initialValue: false);

	public IReadOnlyReactiveProperty<Visibility> ResVisibility { get; }
	public IReadOnlyReactiveProperty<Visibility> NgVisibility { get; }
	public IReadOnlyReactiveProperty<string> NgText { get; }

	public IReadOnlyReactiveProperty<Visibility> SendDelVisibility { get; }
	public IReadOnlyReactiveProperty<Visibility> DeleteResVisibility { get; }

	private IDisposable? deleteSubscriber = null;
	private double __deleteProgress = 0d;
	private const int DeleteGraceTimeMiliSec = 2000;
	private const int DeleteIntervalTimeMiliSec = 10;
	private ReactivePropertySlim<bool> IsNg { get; }
	private ReactivePropertySlim<Models.SureyomiChanDeleteType> DeleteType { get; }
	private readonly bool hasImage;
	private readonly string imageKey;

	public BindableSureyomiChanModel(
		SureyomiChanModel model,
		IEnumerable<AttachmentObject> attachments,
		bool isNg
		) {

		static string getViewFileName(SureyomiChanModel model, AttachmentObject obj) {
			const int left = 6;
			const int right = 6;

			var fileName = obj.FileName;
			if(model.Interaction.BoardId != SureyomiChanBoardId.NijiuraChanAimg) {
				return fileName;
			}

			var name = Path.GetFileNameWithoutExtension(fileName);
			var ext = Path.GetExtension(fileName);
			var span = name.AsSpan();
			return $"{span[..left]}…{span[^right..]}{ext}";
		}


		AttachmentObject? attachment = attachments.FirstOrDefault();
		this.ResIndex = new ReactivePropertySlim<int>(initialValue: model.ResIndex);
		this.No = new ReactivePropertySlim<string>(initialValue: FormatNo(model));
		this.PostTime = new ReactivePropertySlim<string>(initialValue: model.FormatDateTime());
		this.EmailVisibility = new ReactivePropertySlim<Visibility>(initialValue: FormatEmailVisibility(model));
		this.Email = new ReactivePropertySlim<string>(initialValue: FormatEmail(model));
		this.Body = new ReactivePropertySlim<string>(initialValue: FormatBody(model));
		this.Id = new ReactivePropertySlim<string?>(initialValue: model.Id);
		this.ImageName = new ReactivePropertySlim<string?>(initialValue: attachment switch {
			{ } v => getViewFileName(model, v),
			_ => "",
		});
		this.hasImage = !isNg && attachment?.ImageFileBytes != null;
		this.ImageVisibility = new ReactivePropertySlim<Visibility>(initialValue: this.hasImage switch {
			true=> Visibility.Visible,
			_ => Visibility.Collapsed,
		});
		this.ImageErrorVisibility = new ReactivePropertySlim<Visibility>(initialValue: attachment switch {
			{ } v => v.ImageFileBytes switch {
				{ } => Visibility.Collapsed,
				_ => Visibility.Visible,
			},
			_ => Visibility.Collapsed,
		});
		this.imageKey = attachment switch {
			{ } v when v.ImageFileBytes is { } && !string.IsNullOrEmpty(v.ImageName) => v.ImageName,
			_ => ""
		};
		foreach(var it in attachments.Skip(1)) {
			SubImages.Add(new(model.Interaction.BoardId, model.ThreadId, it));
		}

		this.IsNg = new(initialValue: isNg);
		this.DeleteType = new(initialValue: model.DeleteType);

		this.SendDelVisibility = new ReactivePropertySlim<Visibility>(initialValue: model.Interaction.IsSupportSendDel switch {
			true => Visibility.Visible,
			_ => Visibility.Collapsed
		});
		this.DeleteResVisibility = new ReactivePropertySlim<Visibility>(initialValue: model.Interaction.IsSupportDeleteRes switch {
			true => Visibility.Visible,
			_ => Visibility.Collapsed
		});

		this.NgText = this.DeleteType
			.CombineLatest(this.IsNg, (x, ng) => (x, ng) switch {
				{ } v when v.ng => "NGレス",
				{ } v when v.x == Models.SureyomiChanDeleteType.DeleteFromOwner => "スレッドを立てた人によって削除されました",
				{ } v when v.x == Models.SureyomiChanDeleteType.DeleteFromDel => "削除依頼によって隔離されました",
				{ } v when v.x == Models.SureyomiChanDeleteType.SelfDelete => "書き込みをした人によって削除されました",
				_ => "",
			}).ToReadOnlyReactivePropertySlim<string>();
		this.ResVisibility = this.NgText
			.Select(x => x switch {
				{ } v when string.IsNullOrEmpty(v) => Visibility.Visible,
				_ => Visibility.Collapsed,
			}).ToReadOnlyReactivePropertySlim();
		this.NgVisibility = this.NgText
			.Select(x => x switch {
				{ } v when !string.IsNullOrEmpty(v) => Visibility.Visible,
				_ => Visibility.Collapsed,
			}).ToReadOnlyReactivePropertySlim();
		this.IsId = this.Id.Select(x => x is { }).ToReadOnlyReactivePropertySlim();
		this.IdString = this.Id.Select(x => x switch {
			{ } v => v,
			_ => ""
		}).ToReadOnlyReactivePropertySlim<string>();

		this.Model = model;
	}

	public void BeginDelete(Func<Task<bool>> callback) {
		void init() {
			this.deleteSubscriber?.Dispose();
			this.deleteSubscriber = null;
			this.__deleteProgress = 0;
			this.DeleteProgress.Value = 0;
		}

		if(this.deleteSubscriber != null) {
			init();
		} else {
			this.deleteSubscriber = Observable.Interval(TimeSpan.FromMilliseconds(DeleteIntervalTimeMiliSec))
				.ObserveOn(Reactive.Bindings.UIDispatcherScheduler.Default)
				.Subscribe(async _ => {
					this.__deleteProgress += ((double)DeleteIntervalTimeMiliSec / DeleteGraceTimeMiliSec);
					this.DeleteProgress.Value = (int)(Math.Min(1d, this.__deleteProgress) * 100);
					if(1d <= this.__deleteProgress) {
						init();

						this.IndeterminateDeleteProgress.Value = true;
						await callback.Invoke();
						this.IndeterminateDeleteProgress.Value = false;
					}
				});
		}
	}

	private static string FormatNo(SureyomiChanModel model) => $"No.{model.No}";
	private static Visibility FormatEmailVisibility(SureyomiChanModel model) => string.IsNullOrEmpty(model.Email) switch {
		false => Visibility.Visible,
		_ => Visibility.Collapsed,
	};

	private static string FormatEmail(SureyomiChanModel model) => model.Email switch {
		string v when !string.IsNullOrEmpty(v) => $"[{v}]",
		_ => "",
	};

	private static string FormatBody(SureyomiChanModel model) {
		var s1 = Regex.Replace(model.Body, @"<br>", Environment.NewLine,
			RegexOptions.IgnoreCase | RegexOptions.Multiline);
		var s2 = Regex.Replace(s1, @"<[^>]*>", "",
			RegexOptions.IgnoreCase | RegexOptions.Multiline);
		var s3 = System.Net.WebUtility.HtmlDecode(s2);

		return s3;
	}

	private static ImageObject LoadImage(string imageName, byte[] imageBytes) {
		return Path.GetExtension(imageName).ToLower() switch {
			".png" => Utils.ImageUtil.LoadPng(imageBytes),
			".webp" => Utils.ImageUtil.LoadWebp(imageBytes),
			".gif" => Utils.ImageUtil.LoadGif(imageBytes),
			_ => new ImageObject(BitmapFrame.Create(new MemoryStream(imageBytes)))
		};
	}
}

// TODO: 場所をかえる
class ImageObject : INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;

	public BitmapSource ImageSource { get; }
	public Timeline? AnimationSource { get; }

	public ImageObject(BitmapSource image, Timeline? animation = null) {
		this.ImageSource = image;
		this.AnimationSource = animation;
	}
}
