using Haru.Kei.SureyomiChan.Core;
using Haru.Kei.SureyomiChan.Models;
using J2N.Threading;
using Livet.Messaging;
using Livet.Messaging.IO;
using MaterialDesignThemes.Wpf;
using Microsoft.Web.WebView2.Wpf;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using BindableConfig = Haru.Kei.SureyomiChan.Models.Bindables.BindableConfig;
using BindableItem = Haru.Kei.SureyomiChan.Models.Bindables.BindableYomiageItem;

namespace Haru.Kei.SureyomiChan.ViewModels;
class MainWindowViewModel : BindableBase {
	enum LayoutMode {
		None,
		Start,
		Main,
		Config
	}
	enum ProcessState {
		Sucess,
		Fail,
		Running
	}
	public InteractionMessenger LivetMessenger { get; } = new();

	public IReadOnlyReactiveProperty<string> BuildString { get; } = new ReactivePropertySlim<string>(initialValue: "v.yyyymmdd..");

	private ReactivePropertySlim<LayoutMode> Layout { get; } = new(initialValue: LayoutMode.None);
	private ReactivePropertySlim<ProcessState> ApiState { get; } = new(initialValue: ProcessState.Sucess);

	public ReadOnlyReactivePropertySlim<Visibility> StartPageVisibility { get; }
	public ReadOnlyReactivePropertySlim<Visibility> MainPageVisibility { get; }
	public ReadOnlyReactivePropertySlim<Visibility> ConfigPageVisibility { get; }
	public ReadOnlyReactivePropertySlim<Visibility> InitialSettingVisibility { get; }

	public ReactiveCollection<BindableItem> Yomiage { get; } = new();
	public ReactivePropertySlim<string> Url { get; } = new(initialValue: "");

	public ReactiveCommandSlim ClickYomiageStartCommand { get; } = new();
	public ReactiveCommandSlim ClickYomiageSkipCommand { get; } = new();

	public ReactiveCommandSlim<RoutedEventArgs> LoadedCommand { get; } = new();
	public ReactiveCommandSlim ClosedCommand { get; } = new();
	public ReactiveCommandSlim<RoutedEventArgs> WebViewLoadedCommand { get; } = new();



	public ReactiveCommandSlim OpenConfigCommand { get; } = new();
	public ReactiveCommandSlim SaveConfigCommand { get; } = new();
	public ReactiveCommandSlim AddUrlSchemeCommand { get; } = new();
	public ReactiveCommandSlim RemoveUrlSchemeCommand { get; } = new();



	public ReactivePropertySlim<BindableConfig> BindableConfig { get; }

	private readonly IDialogService dialogService;

	public MainWindowViewModel(IDialogService dialogService) {
		this.dialogService = dialogService;
		this.FutabaUrl = Utils.Singleton.Instance.FutabaUrl;
		this.NijiuraChanUrl = Utils.Singleton.Instance.NijiuraChanUrl;
		this.NijiuraChanTsUrl = Utils.Singleton.Instance.NijiuraChanTsUrl;
		this.attachmentWriter = new(config: this.config, uiDispatcher: this.uiDispatcher);
		this.bouyomi = new(Utils.Singleton.Instance.HttpClient, this.uiDispatcher, this.config);

		this.StartPageVisibility = this.Layout.Select(x => x switch {
			LayoutMode.Start => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.MainPageVisibility = this.Layout.Select(x => x switch {
			LayoutMode.Main => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.ConfigPageVisibility = this.Layout.Select(x => x switch {
			LayoutMode.Config => Visibility.Visible,
			_ => Visibility.Collapsed,
		}).ToReadOnlyReactivePropertySlim();
		this.InitialSettingVisibility = this.initConfig.Select(x => x switch {
			true => Visibility.Collapsed,
			_ => Visibility.Visible,
		}).ToReadOnlyReactivePropertySlim();


		this.ClickYomiageStartCommand.Subscribe(_ => this.OnYomiage_Start());
		this.ClickYomiageSkipCommand.Subscribe(_ => this.OnYomiage_StartSkip());

		this.LoadedCommand.Subscribe(x => this.OnLoaded(x));
		this.ClosedCommand.Subscribe(_ => this.OnClosed());
		this.WebViewLoadedCommand.Subscribe(x => this.OnWebViewLoaded(x));

		this.OpenConfigCommand.Subscribe(_ => this.OnOpenConfig());
		this.SaveConfigCommand.Subscribe(_ => this.OnSaveConfig());

		this.AddUrlSchemeCommand.Subscribe(_ => this.OnAddUrlScheme());
		this.RemoveUrlSchemeCommand.Subscribe(_ => this.OnRemoveUrlScheme());

		this.LoadConfig();
		this.BindableConfig = new(initialValue: new(this.config.Get()));
	}

	private const string SchemeName = "sureyomichan";
	private readonly ReactivePropertySlim<bool> initConfig = new(initialValue: false);
	private readonly Core.ConfigProxy config = new();
	private readonly Core.UiMessageMultiDispatcher uiDispatcher = new();
	private readonly Core.AttachmentWriter attachmentWriter;
	private readonly Core.BouyomiChan bouyomi;
	private readonly Core.TegakiSaveStore tegakiSaveStore = new();
	private readonly Helpers.IApiUrl FutabaUrl;
	private readonly Helpers.IApiUrl NijiuraChanUrl;
	private readonly Helpers.IApiUrl NijiuraChanTsUrl;

	public SnackbarMessageQueue SnackbarMessageQueue { get; } = new();
	private Core.SureyomiChanNgProcesser? ng;
	private Core.WebView2Proxy? edge;
	private string prevUrl = "";
	private nint hwnd;
	private object viewToken = new();

	private readonly ReactivePropertySlim<Core.SureyomiChanApiLooper?> api = new (initialValue: null);

	private void LoadConfig() {
		if(new Helpers.ConfigLoader().Load() is { } config) {
			this.config.Update(config);
			this.initConfig.Value = true;
		}
	}

	private void OnLoaded(RoutedEventArgs e) {
		Utils.Logger.Instance.Init();
		Utils.Logger.Instance.Info("メインウインドウが初期化されました");
		this.viewToken = e.Source;
		if(e.Source is System.Windows.Window window) {
			var helper = new System.Windows.Interop.WindowInteropHelper(window);
			hwnd = helper.EnsureHandle();
			//hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
			System.Windows.Interop.HwndSource.FromHwnd(hwnd)
				.AddHook(new HwndSourceHook(WndProc));
		} else {
			Utils.Logger.Instance.Error("メインウインドウハンドルが取得できません");
		}
		Utils.Singleton.Instance.StartupSequence.End(hwnd);
		_ =  this.attachmentWriter.InitThreadNo();

		if(!this.initConfig.Value) {
			this.Layout.Value = LayoutMode.Config;
		} else {
			this.Layout.Value = LayoutMode.Start;
		}
	}

	private void OnClosed() {
		App.Current.Shutdown();
	}

	private void OnWebViewLoaded(RoutedEventArgs e) {
		if(e.Source is WebView2 wv2) {
			Utils.Logger.Instance.Info("WebView2が初期化されました");
			this.edge = new(wv2, this.tegakiSaveStore, config.Get());
			this.ng = new(this.edge, this.config);
			this.edge.Initialized += (_, _) => {
				// CoreWebViewが初期化されたタイミングでコマンドライン解析
				if(Utils.Util.ParseCommandLine(Environment.CommandLine) is { } p) {
					if(this.StartYomiage(p.Board, p.ThreadId, p.IsLatest)) {
						Utils.Singleton.Instance.PrismMessenger.GetEvent<PubSubEvent<Models.WindowMinimizeMessage>>()
							.Publish(new(this.viewToken));
					}
				}
			};
		}
	}

	private void OnYomiage_Start() {
		if (this.StartYomiage(this.Url.Value, false)) {
			this.Url.Value = "";
			Utils.Singleton.Instance.PrismMessenger.GetEvent<PubSubEvent<Models.WindowMinimizeMessage>>()
				.Publish(new(this.viewToken));
		}
	}

	private void OnYomiage_StartSkip() {
		if(this.StartYomiage(this.Url.Value, true)) {
			this.Url.Value = "";
			Utils.Singleton.Instance.PrismMessenger.GetEvent<PubSubEvent<Models.WindowMinimizeMessage>>()
				.Publish(new(this.viewToken));
		}
	}


	private void OnOpenConfig() {
		this.Layout.Value = LayoutMode.Config;
	}

	private void OnSaveConfig() { 
		this.config.Update(this.BindableConfig.Value.Save());
		this.Layout.Value = LayoutMode.Start;
		this.initConfig.Value = true;
	}

	private async void OnAddUrlScheme() {
		using var p = System.Diagnostics.Process.GetCurrentProcess();
		int r = 0;
		if(p.MainModule?.FileName is { } path) {
			r = await Utils.Util.AddCustomUrlScheme(
				SchemeName,
				path,
				this.hwnd);
		}
		if(r == 0) {
			this.EnqueueErrorMessage("URLスキーマを登録しました");
		} else {
			this.EnqueueErrorMessage("URLスキーマの登録に失敗しました");
		}
	}

	private async void OnRemoveUrlScheme() {
		var r = await Utils.Util.RemoveCustomUrlScheme(SchemeName, this.hwnd);
		if(r == 0) {
			this.EnqueueErrorMessage("URLスキーマを解除しました");
		} else {
			this.EnqueueErrorMessage("URLスキーマの解除に失敗しました");
		}
	}

	private void EnqueueErrorMessage(string message) => this.SnackbarMessageQueue.Enqueue(message);

	private bool StartYomiage(SureyomiChanBoardId board, Helpers.ThreadId threadId, bool latest) {
		var url = board switch {
			{ } v when v == SureyomiChanBoardId.FutabaImg => Utils.Singleton.Instance.FutabaUrl,
			{ } v when v == SureyomiChanBoardId.NijiuraChanAimg => Utils.Singleton.Instance.NijiuraChanUrl,
			_ => null,
		};
		if(url is { } api) {
			return this.StartYomiage(api, threadId, latest);
		} else {
			return false;
		}
	}

	private bool StartYomiage(string url, bool latest) {
		Helpers.IApiUrl? apiUrl = url switch {
			string s when this.FutabaUrl.IsValidUrl(s) => this.FutabaUrl,
			string s when this.NijiuraChanUrl.IsValidUrl(s) => this.NijiuraChanUrl,
			string s when this.NijiuraChanTsUrl.IsValidUrl(s) => this.NijiuraChanTsUrl,
			_ => null
		};
		if(apiUrl is { } && apiUrl.ParseThreadNo(url) is { } threadId) {
			return this.StartYomiage(apiUrl, threadId, latest);
		} else {
			Utils.Logger.Instance.Info($"サポートされていないURLです url=>{url}");
			return false;
		}
	}

	private bool StartYomiage(Helpers.IApiUrl url, Helpers.ThreadId threadId, bool latest) {
		if(this.edge == null) {
			Utils.Logger.Instance.Error($"！！整合性エラーWebViewが初期化されていません！！");
			return false;
		}
		if(this.ng == null) {
			Utils.Logger.Instance.Error($"！！整合性エラーNG処理が初期化されていません！！");
			return false;
		}
		if(this.api.Value != null) {
			Utils.Logger.Instance.Error($"！！整合性エラー読み上げ開始にapiがnullではありません！！");
			return false;
		}

		var urlString = url.GenUrlThread(threadId);
		if(this.Yomiage.Where(x => x.Url.Value == urlString).Any()) {
			Utils.Logger.Instance.Info($"読み上げ中です=>{urlString}");
			return false;
		}

		var dialogParam = new DialogParameters() {
			{
				nameof(ViewModels.YomiageDialogViewModel.DialogParams),
				new ViewModels.YomiageDialogViewModel.DialogParams(
					UrlString: urlString,
					Url: url,
					ThreadId: threadId,
					IsLatest: latest,
					Config: this.config,
					Dispatcher: this.uiDispatcher,
					Bouyomi: this.bouyomi,
					AttachmentWriter: this.attachmentWriter,
					Store: this.tegakiSaveStore,
					WebView: this.edge,
					Ng: this.ng
				)
			}
		};
		this.dialogService.Show(
			typeof(Views.YomiageDialog).FullName,
			dialogParam,
			(x) => {
				if(this.Yomiage.Where(x => x.Url.Value == urlString).FirstOrDefault() is { } it) {
					this.Yomiage.Remove(it);

					if(this.Yomiage.Count == 0) {
						Utils.Singleton.Instance.PrismMessenger.GetEvent<PubSubEvent<Models.WindowShowMessage>>()
							.Publish(new(this.viewToken));
					}
				}
			},
			nameof(Views.YomiageDialogWindow));
		this.Yomiage.Add(new(url: urlString));
		this.Layout.Value = LayoutMode.Main;

		return true;
	}

	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled) {
		if(msg == Interop.WM_COPYDATA) {
			var d = Marshal.PtrToStructure<Interop.COPYDATASTRUCT>(lParam);
			if(d.dwData == SureyomiChanEnviroment.CopyDataTypeCommandArgs) {
				if(Utils.Util.ParseCommandLine(d.lpData) is { } p) {
					if(this.StartYomiage(p.Board, p.ThreadId, p.IsLatest)) {
						Utils.Singleton.Instance.PrismMessenger.GetEvent<PubSubEvent<Models.WindowMinimizeMessage>>()
							.Publish(new(this.viewToken));
					}
				}
				handled = true;
			}
		}
		return 0;
	}
}
