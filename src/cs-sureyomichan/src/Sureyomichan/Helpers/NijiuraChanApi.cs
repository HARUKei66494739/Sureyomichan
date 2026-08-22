using Haru.Kei.SureyomiChan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Haru.Kei.SureyomiChan.Helpers;
class NijiuraChanApi {
	private readonly HttpClient httpClient;
	private readonly IApiUrl apiUrl;
	private readonly SerialRunner serialRunner = new(1000);
	private static readonly string ApiEntry = "https://api.nijiurachan.net";

	public NijiuraChanApi(HttpClient httpClient, IApiUrl apiUrl) {
		this.httpClient = httpClient;
		this.apiUrl = apiUrl;
	}

	public async Task<Models.NijiuraChanToken> GetToken(Core.__NijiuraChanWebView2Proxy webView) {
		static string url() => $"{ApiEntry}/tokens";

		var s = url();
		var task = webView.__RequestApi(url(), action: "POST");
		task.Wait();
		var json = "";
		try {
			json = task.Result;
			if(JsonSerializer.Deserialize<Models.NijiuraChanToken>(json) is { } obj) {
				return obj;
			} else {
				throw new Exceptions.ApiInvalidJsonException(json);
			}
		}
		catch(JsonException _) {
			throw new Exceptions.ApiInvalidJsonException(json);
		}
	}


	/* 通らない
	public async Task<Models.NijiuraChanToken> GetToken() {
		var json = "";
		try {
			var req = new HttpRequestMessage(HttpMethod.Get, UrlToken);
			using var r = await Utils.Util.Http(() => this.httpClient.SendAsync(req));
			json = await r.Content.ReadAsStringAsync();
			if(JsonSerializer.Deserialize<Models.NijiuraChanToken>(json) is { } obj) {
				return obj;
			} else {
				throw new Exceptions.ApiInvalidJsonException(json);
			}
		}
		catch(JsonException _) {
			throw new Exceptions.ApiInvalidJsonException(json);
		}
	}
	*/

	public async Task<Models.NijiuraChanChunk> __GetThreadInfoWithWebView(Core.__NijiuraChanWebView2Proxy webView, string threadId) {
		static string url(string threadId) => $"{ApiEntry}/threads/{threadId}/chunks/0";

		var json = "";
		try {
			json = await webView.__RequestApi(url(threadId));
			if(!(JsonSerializer.Deserialize<IEnumerable<Models.NijiuraChanChunk>>(json) is { } cs)) {
				throw new Exceptions.ApiInvalidJsonException(json);
			}

			if(!(cs.Where(x => x.Sequence == 0).FirstOrDefault() is { } obj)) {
				throw new Exceptions.ApiInvalidJsonException(json);
			}

			return obj;
		}
		catch(JsonException _) {
			throw new Exceptions.ApiInvalidJsonException(json);
		}
	}


	public async Task<Models.NijiuraChanChunk> GetThreadInfo(string threadId) {
		static string url(string threadId) => $"{ApiEntry}/threads/{threadId}/chunks/0";

		var json = "";
		try {
			using var r = await Utils.Util.Http(() => httpClient.GetAsync(url(threadId)));
			json = await r.Content.ReadAsStringAsync();


			if(!(JsonSerializer.Deserialize<IEnumerable<Models.NijiuraChanChunk>>(json) is { } cs)) {
				throw new Exceptions.ApiInvalidJsonException(json);
			}

			if(!(cs.Where(x => x.Sequence == 0).FirstOrDefault() is { } obj)) {
				throw new Exceptions.ApiInvalidJsonException(json);
			}

			return obj;
		}
		catch(JsonException _) {
			throw new Exceptions.ApiInvalidJsonException(json);
		}
	}

	public async Task<Models.NijiuraChanState> __GetThreadWithWebView(Core.__NijiuraChanWebView2Proxy webView, string threadId, int? latestResSeq = null) {
		static string url(string threadId, int? latestResSeq) {
			var after = latestResSeq ?? 0;
			return $"{ApiEntry}/threads/{threadId}/state?after={after}";
		}

		var json = "";
		try {
			json = await webView.__RequestApi(url(threadId, latestResSeq));
			if(JsonSerializer.Deserialize<Models.NijiuraChanState>(json) is { } obj) {
				return obj;
			} else {
				throw new Exceptions.ApiInvalidJsonException(json);
			}
		}
		catch(JsonException _) {
			throw new Exceptions.ApiInvalidJsonException(json);
		}
	}

	public async Task<Models.NijiuraChanState> GetThread(string threadId, int? latestResSeq = null) {
		static string url(string threadId, int? latestResSeq) {
			var after = latestResSeq ?? 0;
			return $"{ApiEntry}/threads/{threadId}/state?after={after}";
		}

		var json = "";
		try {
			using var r = await Utils.Util.Http(() => httpClient.GetAsync(url(threadId, latestResSeq)));
			json = await r.Content.ReadAsStringAsync();
			if(JsonSerializer.Deserialize<Models.NijiuraChanState>(json) is { } obj) {
				return obj;
			} else {
				throw new Exceptions.ApiInvalidJsonException(json);
			}
		}
		catch(JsonException _) {
			throw new Exceptions.ApiInvalidJsonException(json);
		}
	}

	public IObservable<Models.NijiuraChanChunk> GetThreadInfoSerial(string threadId)
		=> this.serialRunner.Dispatch(async () => await this.GetThreadInfo(threadId));
	public IObservable<Models.NijiuraChanState> GetThreadSerial(string threadId, int? latestResSeq = null)
		=> this.serialRunner.Dispatch(async () => await this.GetThread(threadId, latestResSeq));

}
