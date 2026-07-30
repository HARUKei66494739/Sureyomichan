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

using NjiuraChanThreadResponse = Models.NijiuraChanResponse<Models.NijiuraChanThreadDataV1>;

class NijiuraChanApi {
	private readonly HttpClient httpClient;

	public NijiuraChanApi(HttpClient httpClient, IApiUrl apiUrl) {

		// 現時点でAPIが素直につながらないので保留

		CookieContainer cookies = new CookieContainer();
		HttpClientHandler clientHandler = new HttpClientHandler();
		clientHandler.CookieContainer = cookies;
		clientHandler.UseCookies = true;


		this.httpClient = new(clientHandler);
		this.httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100");
		this.httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
		this.httpClient.DefaultRequestVersion = new Version(2, 0);
	}

	public IObservable<NjiuraChanThreadResponse> GetThread(int threadId, int? latestResId = null) {
		return Observable.Create<NjiuraChanThreadResponse>(async o => {
			string genUrl() => latestResId switch {
				int v => $"https://nijiurachan.net/api/v1/thread/{threadId}/new?after={v}",
				_ => $"https://nijiurachan.net/api/v1/thread/{threadId}"
			};

			var url = genUrl();
			try {
				var r = await httpClient.GetAsync(url);
				r.EnsureSuccessStatusCode();
				var json = await r.Content.ReadAsStringAsync();
				if (JsonSerializer.Deserialize<NjiuraChanThreadResponse>(json) is { } obj) {
					o.OnNext(obj);
				} else {
					o.OnError(new Exceptions.ApiInvalidJsonException(json));
				}
			}
			catch (HttpRequestException ex) {
				o.OnError(new Exceptions.ApiHttpErrorException(url, ex));
			}
		});
	}
}

class NijiuraChanTsApi {
	private readonly HttpClient httpClient;
	private readonly IApiUrl apiUrl;
	private readonly SerialRunner serialRunner = new(1000);
	private static readonly string ApiEntry = "https://api-staging.nijiurachan.net/";
	private static readonly string UrlToken = "https://api-staging.nijiurachan.net/tokens";

	public NijiuraChanTsApi(HttpClient httpClient, IApiUrl apiUrl) {
		this.httpClient = httpClient;
		this.apiUrl = apiUrl;
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

	public async Task<Models.NijiuraChanChunk> GetThreadInfo(string threadId) {
		static string url(string threadId) => $"{ApiEntry}threads/{threadId}/chunks/0";

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


	public async Task<Models.NijiuraChanState> GetThread(string threadId, int? latestResSeq = null) {
		static string url(string threadId, int? latestResSeq) {
			var after = latestResSeq ?? 0;
			return $"{ApiEntry}threads/{threadId}/state?after={after}";
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
