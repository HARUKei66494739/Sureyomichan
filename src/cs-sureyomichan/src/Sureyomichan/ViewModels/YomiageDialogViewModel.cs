using ControlzEx.Standard;
using Haru.Kei.SureyomiChan.Core;
using Haru.Kei.SureyomiChan.Models.Bindables;
using MaterialDesignThemes.Wpf;
using Prism.Common;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BindableConfig = Haru.Kei.SureyomiChan.Models.Bindables.BindableConfig;
using BindableSureyomi = Haru.Kei.SureyomiChan.Models.Bindables.BindableSureyomiChanModel;

namespace Haru.Kei.SureyomiChan.ViewModels; 
internal class YomiageDialogViewModel : BindableBase, IDialogAware {
	public record class DialogParams(
		string UrlString,
		Helpers.IApiUrl Url,
		Helpers.ThreadId ThreadId,
		bool IsLatest,
		Core.IConfigProxy Config,
		Core.UiMessageMultiDispatcher Dispatcher,
		Core.BouyomiChan Bouyomi,
		Core.AttachmentWriter AttachmentWriter,
		Core.TegakiSaveStore Store,
		Core.WebView2Proxy WebView,
		Core.SureyomiChanNgProcesser Ng);
	enum ProcessState {
		Sucess,
		Fail,
		Running
	}
	internal class BaseCommandMessage { }
	internal class ScrollMessage(BindableSureyomi scrollTarget) : BaseCommandMessage {
		public BindableSureyomi ScrollTarget => scrollTarget;
	}

	public string Title {
		get {
			if(field == null) {
				return "";
			}
			return field;
		}
		set {
			this.RaisePropertyChanged(nameof(Title));
			field = value;
		}
	}


	public DialogCloseListener RequestClose { get; }
	public SnackbarMessageQueue SnackbarMessageQueue { get; } = new();


	private ReactivePropertySlim<bool> TegakiSaveRunning { get; } = new(initialValue: false);
	private ReactivePropertySlim<ProcessState> ApiState { get; } = new(initialValue: ProcessState.Sucess);
	private readonly ReactivePropertySlim<Core.SureyomiChanApiLooper?> api = new(initialValue: null);
	public ReadOnlyReactivePropertySlim<bool> IsYomiageRun { get; }

	public ReactiveCollection<BindableSureyomi> Replies { get; } = [];
	public ReactivePropertySlim<string> ThreadDieText { get; } = new(initialValue: "");
	public ReactivePropertySlim<string> Url { get; } = new(initialValue: "");
	public ReactivePropertySlim<bool> ListBoxAutoScroll { get; } = new(initialValue: true);

	public ReadOnlyReactivePropertySlim<Visibility> YomiageStatusStop { get; }
	public ReadOnlyReactivePropertySlim<Visibility> YomiageStatusRunning { get; }
	public ReadOnlyReactivePropertySlim<Visibility> TegakiSaveStatusStop { get; }
	public ReadOnlyReactivePropertySlim<Visibility> TegakiSaveStatusRunning { get; }
	public ReadOnlyReactivePropertySlim<Visibility> ApiStatusSucess { get; }
	public ReadOnlyReactivePropertySlim<Visibility> ApiStatusFail { get; }
	public ReadOnlyReactivePropertySlim<Visibility> ApiStatusRunning { get; }


	public ReactiveCommandSlim<RoutedEventArgs> LoadedCommand { get; } = new();
	public ReactiveCommandSlim ClickYomiageCommand { get; } = new();
	public ReactiveCommandSlim ClickOpenFolderCommand { get; } = new();
	public ReactiveCommandSlim<RoutedEventArgs> SendDelCommand { get; } = new();
	public ReactiveCommandSlim<RoutedEventArgs> DeleteResCommand { get; } = new();

	private readonly Core.UiMessageDispatcher uiMsgDispatcher;
	private object viewToken = new();
	private DialogParams? param;
	public YomiageDialogViewModel() {
		this.uiMsgDispatcher = new() {
			OnBeginApi = () => this.ApiState.Value = ProcessState.Running,
			OnEndApi = (sucessed) => this.ApiState.Value = sucessed switch {
				true => ProcessState.Sucess,
				_ => ProcessState.Fail
			},
			OnNewReplies = (x) => {
				if(x.Any()) {
					this.Replies.AddRange(x);
					if(this.ListBoxAutoScroll.Value) {
						Utils.Singleton.Instance.PrismMessenger.GetEvent<PubSubEvent<Models.ScrollMessage>>()
							.Publish(new(this.viewToken, x.Last()));
					}
				}
			},
			OnUpdateDieTime = (c, d) => {
				var ts = d - c;
				var tt = DateTime.Now.Add(ts); // 消滅時間表示はPCの時計を使用
				this.ThreadDieText.Value = ts switch {
					TimeSpan y when y.TotalSeconds < 0 => $"スレ消滅：{Math.Abs(ts.TotalSeconds):00}秒経過(消滅時間を過ぎました)",
					TimeSpan y when 0 < y.Days => $"スレ消滅：{tt.ToString("MM/dd")}(あと{ts.ToString(@"dd\日hh\時\間")})",
					TimeSpan y when 0 < y.Hours => $"スレ消滅：{tt.ToString("HH:mm")}(あと{ts.ToString(@"hh\時\間mm\分")})",
					TimeSpan y when 0 < y.Minutes => $"スレ消滅：{tt.ToString("HH:mm")}(あと{ts.ToString(@"mm\分ss\秒")})",
					_ => $"スレ消滅：{tt.ToString("HH:mm")}(あと{ts.ToString(@"ss\秒")})",
				};
			},
			OnMaxRes = () => {
				this.ThreadDieText.Value = "最大レス数に到達しました";
				StopYomiage();
			},
			OnThreadDied = () => {
				this.ThreadDieText.Value = "スレッドが落ちました";
				StopYomiage();
			},
			OnBouyomiChanNotFound = () => {
				if(this.api.Value != null) {
					this.EnqueueErrorMessage("棒読みちゃんが見つかりません。読み上げを停止します");
					StopYomiage();
				}
			},
			OnSartDisplayTegaki = () => this.TegakiSaveRunning.Value = true,
			OnEndDisplayTegaki = () => this.TegakiSaveRunning.Value = false,
			OnErrorTegaki = (m) => { },
		};
		this.TegakiSaveStatusStop = this.TegakiSaveRunning.Select(x => x switch {
			false => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.TegakiSaveStatusRunning = this.TegakiSaveRunning.Select(x => x switch {
			true => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();

		this.ApiStatusSucess = this.ApiState.Select(x => x switch {
			ProcessState.Sucess => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.ApiStatusFail = this.ApiState.Select(x => x switch {
			ProcessState.Fail => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.ApiStatusRunning = this.ApiState.Select(x => x switch {
			ProcessState.Running => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.IsYomiageRun = this.api.Select(x => x switch {
			{ } => true,
			_ => false,
		}).ObserveOn(UIDispatcherScheduler.Default)
		.ToReadOnlyReactivePropertySlim();
		this.YomiageStatusStop = this.IsYomiageRun.Select(x => x switch {
			false => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.YomiageStatusRunning = this.IsYomiageRun.Select(x => x switch {
			true => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();

		this.LoadedCommand.Subscribe(x => this.OnLoaded(x));
		this.SendDelCommand.Subscribe(x => this.OnDendDel(x));
		this.DeleteResCommand.Subscribe(x => this.OnDeleteRes(x));
		this.ClickYomiageCommand.Subscribe(_ => this.OnYomiage());
		this.ClickOpenFolderCommand.Subscribe(_ => this.OnOpenFolder());
	}

	public bool CanCloseDialog() {
		return true;
	}

	public void OnDialogOpened(IDialogParameters parameters) {
		this.param = parameters.GetValue<DialogParams>(nameof(DialogParams));
		this.param.Dispatcher.Register(this.uiMsgDispatcher);

		this.Title = this.Url.Value = this.param.UrlString;
		this.StartYomiage(this.param.IsLatest);
	}

	public void OnDialogClosed() {
		if(this.api.Value != null) {
			this.StopYomiage();
			if(this.param is { }) {
				this.param.Store.Clear(this.param.ThreadId);
				Utils.ImageUtil.ImageStore.Remove(
					SureyomiChanEnviroment.GetStaticString(this.param.Url.BoardId),
					this.param.ThreadId);
			}
		}
	}


	private bool StartYomiage(bool isLatest) {
		Task<Models.NgResult> safeIsNgFromBody(int threadNo, Models.SureyomiChanModel model, Models.DifferenceHash? dhash)
			=> this.param.Ng?.IsNgFromBody(threadNo, model, dhash) switch {
				{ } v => v,
				_ => Task.FromResult(Models.NgResult.Default),
			};
		Task<Models.NgResult> safeIsNgFromImage(Models.DifferenceHash? dhash) => dhash switch {
			{ } v => this.param.Ng?.IsNgFromImage(v)!,
			_ => Task.FromResult(Models.NgResult.Default),
		};
		if(this.param == null) {
			Utils.Logger.Instance.Error($"！！整合性エラー読み上げパラメータが初期化されていません！！");
			return false;
		}
		if(this.api.Value != null) {
			Utils.Logger.Instance.Error($"！！整合性エラー読み上げ開始にapiがnullではありません！！");
			return false;
		}

		Utils.Logger.Instance.Info($"読み上げを開始します => {SureyomiChanEnviroment.GetStaticString(this.param.Url.BoardId)}, {this.param.ThreadId}");
		var yomiage = new Core.Yomiage(this.param.Bouyomi, this.param.Config);
		var prevResponse = default(Models.SureyomiChanResponse);
		this.ThreadDieText.Value = "";
		try {
			api.Value = new Core.SureyomiChanApiLooper(
				this.param.UrlString,
				this.param.Url,
				this.param.ThreadId,
				this.uiMsgDispatcher,
				this.param.Config,
				this.param.WebView);
			{
				var c = this.param.Config.Get();
				yomiage.DoYomiageOnce(c.YomiageStarted, nameof(c.YomiageStarted));
			}
			api.Value.Run(
				callBack: async (x, skip) => {
					void yomiSpeak(string m) {
						if(!skip) {
							yomiage.EnqueueSpeak(m);
						}
					}
					void yomiImage(IEnumerable<Models.AttachmentObject> attachments) {
						if(!skip && attachments.Any(x => x.IsUpdatedTegakiPng)) {
							yomiage.SaveImage();
						}
					}
					Task<Models.NgResult> delay(int milisec) => Task.Run(async () => {
						await Task.Delay(milisec);
						return Models.NgResult.Default;
					});

					try {
						var speak = new List<Models.SureyomiChanModel>();
						var disp = new List<BindableSureyomi>();
						var images = new List<byte[]>();
						foreach(var it in x.NewReplies) {
							var attachments = await it.Interaction.DownloadImages();
							var isNg = false;
							var body = it.ToSpeakText();
							var dHash = attachments.FirstOrDefault()?.Hash;
							foreach(var it2 in await Task.WhenAll(
								safeIsNgFromBody(x.ThreadNo, it, dHash),
								safeIsNgFromImage(dHash),
								delay(500))) {
								isNg |= it2.IsNg;
								if(0 < it2.ReplaceBody.Length) {
									body = it2.ReplaceBody;
								}
							}

							if(!isNg) {
								yomiSpeak(
									string.Join('\n',
										body.Replace("\r", "")
											.Split("\n")
											.Select(x => x switch {
												{ } v when v.FirstOrDefault() == '>' => $"{this.param.Config.Get().AppendSpecialTag}{x}",
												{ } v => v,
												_ => "",
											}))
									);

								if(attachments.Count() != 0) {
									yomiImage(attachments);
									await this.param.AttachmentWriter.Save(it, attachments);
								}
								if(this.param.Config.Get().IsEnabledUpFile) {
									_ = this.param.AttachmentWriter.DownloadShio(it);
								}
							}
							foreach(var ao in attachments) {
								if(ao.ImageFileBytes is { }) {
									Utils.ImageUtil.ImageStore.Insert(
										SureyomiChanEnviroment.GetStaticString(this.param.Url.BoardId),
										this.param.ThreadId,
										ao.ImageName,
										ao.ImageFileBytes);
								}
							}
							disp.Add(new(it, attachments, isNg));
							this.param.Store.Add(this.param.ThreadId, it, isNg, attachments);
						}

						await this.param.AttachmentWriter.UpdateThreadNo(x);
						this.ProcessYomiage(x, prevResponse, yomiage);
						if(x.SupportFeature.IsSupportThreadDie && !x.IsAlive) {
							await this.param.AttachmentWriter.DeadThreadNo(x);
						}

						uiMsgDispatcher.DispatchNewRiplies(disp);
					}
					finally {
						prevResponse = x;
					}
				},
				skipToLast: isLatest,
				latestResNo: this.Replies.LastOrDefault()?.Model.No);
			return true;
		}
		catch(NotSupportedException e) {
			// URLが不正
			this.EnqueueErrorMessage($"[{SureyomiChanEnviroment.GetStaticString(this.param.Url.BoardId)}]はサポートされていない読み上げURLです");
			Utils.Logger.Instance.Error(e);

			return false;
		}
	}

	private void StopYomiage() {
		if(this.api.Value == null) {
			Utils.Logger.Instance.Error($"！！整合性エラー読み上げを停止しようとしましたがapiがnullです！！");
			return;
		}

		Utils.Logger.Instance.Info($"読み上げを停止します");

		api.Value.Dispose();
		api.Value = null;
	}

	private void ProcessYomiage(Models.SureyomiChanResponse current, Models.SureyomiChanResponse? prev, Yomiage yomiage) {
		if(this.param?.Config.Get() is { } config) {
			bool isSoudane() => prev?.Soudane switch {
				{ } v when v < current.Soudane => true,
				_ => false,
			};
			bool isOld() => (current.DieTime - current.CurrentTime).TotalMilliseconds < this.param.Config.Get().YomiageOldTime;

			if(current.SupportFeature.IsSupportInspectSoudane && isSoudane()) {
				yomiage.DoYomiage(config.YomiageSoudane);
			}
			if(current.SupportFeature.IsSupportThreadOld && isOld()) {
				yomiage.DoYomiageOnce(config.YomiageOld, nameof(config.YomiageOld));
			}
			if(current.IsMaxRes) {
				yomiage.DoYomiageOnce(config.YomiageMaxRes, nameof(config.YomiageMaxRes));
			}
			if(current.SupportFeature.IsSupportThreadDie && !current.IsAlive) {
				yomiage.DoYomiageOnce(config.YomiageDie, nameof(config.YomiageDie));
			}
		}
	}

	private void EnqueueErrorMessage(string message) => this.SnackbarMessageQueue.Enqueue(message);

	private void OnLoaded(RoutedEventArgs e) {
		this.viewToken = e.Source;
	}

	private async void OnDendDel(RoutedEventArgs e) {
		if(e.Source is FrameworkElement el && el.DataContext is BindableSureyomi m) {
			try {
				Utils.Logger.Instance.Info("delを送信");
				var r = await m.Model.Interaction.SendDelAction();
				Utils.Logger.Instance.Info($"delを送信しました => {r}");
			}
			catch(Exceptions.SureyomiChanException ex) {
				Utils.Logger.Instance.Error(ex);
			}
		}
	}

	private void OnDeleteRes(RoutedEventArgs e) {
		if(this.param == null) {
			Utils.Logger.Instance.Error($"！！整合性エラー読み上げパラメータが初期化されていません！！");
			return;
		}

		if(e.Source is FrameworkElement el && el.DataContext is BindableSureyomi m) {
			m.BeginDelete(async () => {
				var r = false;
				try {
					this.param.Store.MarkNg(m.Model.No);

					Utils.Logger.Instance.Info("レス削除を呼び出します");
					r = await m.Model.Interaction.DeleteResAction();
					Utils.Logger.Instance.Info($"レス削除を呼び出しました => {r}");
					if(!r) {
						this.EnqueueErrorMessage($"レス[{m.Model.No}]の削除に失敗しました");
					}
				}
				catch(Exceptions.SureyomiChanException ex) {
					Utils.Logger.Instance.Error(ex);
				}
				return r;
			});
		}
	}

	private void OnYomiage() {
		if(api.Value == null) {
			this.StartYomiage(false);
		} else {
			this.StopYomiage();
		}
	}

	private void OnOpenFolder() {
		if(this.param is { } && Utils.Util.GetSaveDirectoryPath(
			this.param.Config.Get(),
			this.param.Url.BoardId,
			this.param.ThreadId) is { } d
			&& System.IO.Directory.Exists(d)) {
		
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(d) {
				UseShellExecute = true
			});
		} else {
			this.EnqueueErrorMessage("まだ何も保存されていません");
		}
	}
}
