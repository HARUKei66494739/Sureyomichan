using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Haru.Kei.SureyomiChan.Core;

class WebView2Proxy {
	private bool isInitialized = false;
	private readonly Microsoft.Web.WebView2.Wpf.WebView2 webView2;
	private readonly TegakiSaveStore tegakiSaveStore;
	private readonly string localRoot;
	private readonly bool isOpenWebViewDevTool;

	public event EventHandler? Initialized;

	// WebView2Proxyの設定更新は再起動が必要
	public WebView2Proxy(
		Microsoft.Web.WebView2.Wpf.WebView2 webView2,
		TegakiSaveStore tegakiSaveStore,
		Models.Config currentConfig) {

		this.webView2 = webView2;
		this.tegakiSaveStore = tegakiSaveStore;
		this.localRoot = currentConfig.PathLegacyTegakiSaveValue;
		this.isOpenWebViewDevTool = currentConfig.OpenWebViewDevTool;

		// CoreWebView2初期化
		this.webView2.CoreWebView2InitializationCompleted += this.OnWebViewInitialization;
		Observable.Return(0)
			.Delay(TimeSpan.FromMilliseconds(1))
			.ObserveOn(Reactive.Bindings.UIDispatcherScheduler.Default)
			.Subscribe(async _ => {
				var env = await CoreWebView2Environment.CreateAsync();
				await webView2.EnsureCoreWebView2Async(env);
			});
	}

	private async void OnBootNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e) {
		this.webView2.NavigationCompleted -= this.OnBootNavigationCompleted;
		foreach(var it in Directory.EnumerateFiles(Path.Combine(localRoot, "plugins"), "*.js", SearchOption.AllDirectories)) {
			var plugin_js = it.AsSpan().Slice(localRoot.Length).ToString();
			Utils.Logger.Instance.Info($"互換JavaScriptプラグインの読み込み => {plugin_js}");
			await this.webView2.CoreWebView2.ExecuteScriptAsync(@$"
{{
	let file = '{plugin_js.Replace('\\', '/')}';
	import(file)
	  .then(m => {{
		loadModule(m, file);
	  }})
}}
");
		}

		await this.webView2.CoreWebView2.ExecuteScriptAsync(@$"
function __fromBase64(b) {{
	return  new TextDecoder()
		.decode(Uint8Array.fromBase64(b));
}}

function __toBase64(s) {{
	return new TextEncoder()
		.encode(s)
		.toBase64();
}}

function __getTegakiSaveJson(param) {{
	if(param.threadId) {{
		return chrome.webview.hostObjects.sync.TegakiSaveObject.GetStoreFromId(param.threadId);
	}}

	if(param.threadNo) {{
		retrun chrome.webview.hostObjects.sync.TegakiSaveObject.GetStoreFromNo(param.threadNo);
	}}

	return null;
}}

function __runNgPlugins(base64) {{
	const param = JSON.parse(__fromBase64(base64));
	const tegaki = JSON.parse(__getTegakiSaveJson(param));
	param.res.__tegaki_res = tegaki.res;
	const pResult = runPlugins({{
		point: 'read',
		execName: 'beforeExecute',
		from: 'tegakiSaveCtrl.read',
		res: param.res,
	}});
	return __toBase64(JSON.stringify(pResult));
}}");
		Initialized?.Invoke(this, EventArgs.Empty);
	}

	private async void OnWebViewInitialization(object? sender, CoreWebView2InitializationCompletedEventArgs e) {
		this.isInitialized = e.IsSuccess;

		Utils.Logger.Instance.Info($"CoreWebView2の初期化が終わりました => IsSuccess={e.IsSuccess}");
		if(this.isInitialized) {
			if(Directory.Exists(localRoot)) {
				this.webView2.CoreWebView2.SetVirtualHostNameToFolderMapping(
					"localmap",
					localRoot,
					Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

				if(this.isOpenWebViewDevTool) {
					this.webView2.CoreWebView2.OpenDevToolsWindow();
				}
				this.webView2.NavigationCompleted += this.OnBootNavigationCompleted;
				this.webView2.CoreWebView2.Navigate(@"https://localmap/interop/boot.html");
				this.webView2.CoreWebView2.AddHostObjectToScript(
					"TegakiSaveObject",
					new HostObject(tegakiSaveStore));
			} else {
				Utils.Logger.Instance.Error($"tegaki_saveプラグイン設定が不正です。連携を停止します。 => {this.localRoot}");
			}
		}
	}

	public async Task<Models.TegakiSavePluginResult?> RunPlugin(Helpers.ThreadId threadId, Models.SureyomiChanModel res, ulong? imageHash) {
		var task = await Utils.Util.AwaitObserver(
			Observable.Return(res)
				.ObserveOn(Reactive.Bindings.UIDispatcherScheduler.Default)
				.Select(async x => {
					var json = new RunPluginParams(threadId) {
						Res = x.ToTegakiSaveModel(
							isNg: false,
							imageHash: imageHash,
							replaceComment: x.Body)
					}.ToString(writeIndented: false);
					var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
					var r = await this.webView2.CoreWebView2.ExecuteScriptAsync($"__runNgPlugins('{base64}')");
					if(r == "null") {
						return null;
					}

					var s = Encoding.UTF8.GetString(
						Convert.FromBase64String(
							FormatScriptResult(r)));
					return JsonSerializer.Deserialize<Models.TegakiSavePluginResult>(s);
				}), default);
		if(task is { }) {
			return await task;
		} else {
			return await Task.FromResult<Models.TegakiSavePluginResult?>(null);
		}
	}

	public async Task<string> __RequestApi(string url) {
		var task = await Utils.Util.AwaitObserver(
			Observable.Return(url)
				.ObserveOn(Reactive.Bindings.UIDispatcherScheduler.Default)
				.Select(async x => {
					var r = await this.webView2.CoreWebView2.ExecuteScriptAsync($@"
{{
    const xmlHttpApi = new XMLHttpRequest();    
    xmlHttpApi.open('GET', '{url}', false);
    xmlHttpApi.send(null);

	__toBase64(xmlHttpApi.responseText);
}}
");
					if(r == "null") {
						return "";
					}

					return Encoding.UTF8.GetString(
						Convert.FromBase64String(
							FormatScriptResult(r)));
				}), default);
		if(task is { }) {
			return await task;
		} else {
			return await Task.FromResult("");
		}
	}

	private static string FormatScriptResult(string s)
		=> new(s.AsSpan().Slice(1, s.Length - 2));
}


// nijiurachan読み上げ用仮置き
class __NijiuraChanWebView2Proxy {
	// CORS迂回のため許可されたドメインを開く
	// private static readonly string NijiuraChanURL = @"https://nijiurachan.net/";
	private static readonly string NijiuraChanURL = @"https://api.nijiurachan.net/";

	private bool isInitialized = false;
	private readonly Microsoft.Web.WebView2.Wpf.WebView2 webView2;

	public event EventHandler? Initialized;

	// WebView2Proxyの設定更新は再起動が必要
	public __NijiuraChanWebView2Proxy(
		Microsoft.Web.WebView2.Wpf.WebView2 webView2,
		Models.Config currentConfig) {

		this.webView2 = webView2;

		// CoreWebView2初期化
		this.webView2.CoreWebView2InitializationCompleted += this.OnWebViewInitialization;
		Observable.Return(0)
			.Delay(TimeSpan.FromMilliseconds(1))
			.ObserveOn(Reactive.Bindings.UIDispatcherScheduler.Default)
			.Subscribe(async _ => {
				var env = await CoreWebView2Environment.CreateAsync();
				await webView2.EnsureCoreWebView2Async(env);
			});
	}

	private async void OnBootNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e) {
		this.webView2.NavigationCompleted -= this.OnBootNavigationCompleted;
		await this.webView2.CoreWebView2.ExecuteScriptAsync(@$"
function __fromBase64(b) {{
	return  new TextDecoder()
		.decode(Uint8Array.fromBase64(b));
}}

function __toBase64(s) {{
	return new TextEncoder()
		.encode(s)
		.toBase64();
}}");
	}

	private async void OnWebViewInitialization(object? sender, CoreWebView2InitializationCompletedEventArgs e) {
		this.isInitialized = e.IsSuccess;

		Utils.Logger.Instance.Info($"CoreWebView2(nijiurachan.net)の初期化が終わりました => IsSuccess={e.IsSuccess}");
		if(this.isInitialized) {
			this.webView2.NavigationCompleted += this.OnBootNavigationCompleted;
			this.webView2.CoreWebView2.Navigate(NijiuraChanURL);
		}
	}


	public async Task<string> __RequestApi(string url, string action="GET") {


		var task = await Utils.Util.AwaitObserver(
			Observable.Return(url)
				.ObserveOn(Reactive.Bindings.UIDispatcherScheduler.Default)
				.Select(async x => {
					var r = await this.webView2.CoreWebView2.ExecuteScriptAsync($@"
{{
    const xmlHttpApi = new XMLHttpRequest();    
    xmlHttpApi.open('{action}', '{url}', false);
    xmlHttpApi.send(null);

	__toBase64(xmlHttpApi.responseText);
}}
");
					if(r == "null") {
						return "";
					}

					return Encoding.UTF8.GetString(
						Convert.FromBase64String(
							FormatScriptResult(r)));
				}), default);
		if(task is { }) {
			return await task;
		} else {
			return await Task.FromResult("");
		}
	}

	private static string FormatScriptResult(string s)
		=> new(s.AsSpan().Slice(1, s.Length - 2));
}

file class RunPluginParams : Models.JsonObject {
	[JsonPropertyName("threadNo")]
	[JsonInclude]
	public int? __No {
		get {
			// 警告出るので迂回処理
			if(field is null) {
				field = this.threadId?.IsInt switch {
					true => this.threadId.ThreadNo,
					_ => null,
				};
			}
			return field;
		}
		private set {
			field = value;
		}
	}

	[JsonPropertyName("threadId")]
	[JsonInclude]
	public string? __Id {
		get {
			// 警告出るので迂回処理
			if(field is null) {
				field = this.threadId?.IsString switch {
					true => this.threadId.ToString(),
					_ => null,
				};
			}
			return field;
		}
		private set {
			field = value;
		}
	}


	[JsonPropertyName("res")]
	[JsonInclude]
	public required Models.TegakiSaveResData Res { get; init; }


	private readonly Helpers.ThreadId? threadId;

	// requiredがあるとJSONシリアライズでエラーが出るのでコンストラクタでとる
	public RunPluginParams(Helpers.ThreadId threadId) {
		this.threadId = threadId;
	}

	// JSONシリアライザ用コンストラクタ
	private RunPluginParams() { }
}


[ComVisible(true)]
[Guid("326DF6C0-B080-4C84-99A7-14DBDF6062B3")]
public class HostObject(ITegakiSaveStore tegakiSaveStore) {
	public string GetStoreFromNo(int resNo) => tegakiSaveStore.GetStore(resNo);
	public string GetStoreFromId(string resId) => tegakiSaveStore.GetStore(resId);
}