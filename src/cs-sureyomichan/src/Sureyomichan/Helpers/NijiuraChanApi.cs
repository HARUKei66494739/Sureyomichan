using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Haru.Kei.SureyomiChan.Helpers;
class NijiuraChanApi {
	private readonly HttpClient httpClient;
	private readonly IApiUrl apiUrl;
	private readonly SerialRunner serialRunner = new(1000);
	private static readonly Random Rand = new();
	private static readonly string ApiEntry = "https://api.nijiurachan.net";
	private static readonly Dictionary<string, string> ExtendHeader = new() {
		{ "X-Api-Version", "2026-11-11"},
		{ "X-Board-Key", "aimg"},
	};

	public NijiuraChanApi(HttpClient httpClient, IApiUrl apiUrl) {
		this.httpClient = httpClient;
		this.apiUrl = apiUrl;
	}

	private HttpRequestMessage GenRequest(HttpMethod method, string url) {
		var request = new HttpRequestMessage(method, url);
		foreach(var it in ExtendHeader) {
			request.Headers.Add(it.Key, it.Value);

		}
		return request;
	}

	public async Task<(
		Models.NijiuraChanAppAuthorizations Request,
		Models.NijiuraChanAppAuthorizationsResponse Response)>
		AppAuth(string deviceUuid, string redirectUri) {

		static string url() => $"{ApiEntry}/app-authorizations";
		static byte[] randomBytes(Random rand, int n) {
			var ret = new StringBuilder();
			for(var i = 0; i < n; i++) {
				ret.Append($"{rand.Next() % 10}");
			}
			return Encoding.UTF8.GetBytes(ret.ToString());
		}

		var json = "";
		try {
			var state = System.Buffers.Text.Base64Url.EncodeToString(randomBytes(Rand, 32));
			var codeVerifier = System.Buffers.Text.Base64Url.EncodeToString(randomBytes(Rand, 32));
			var codeChallenge = System.Buffers.Text.Base64Url.EncodeToString(SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier)));

			var request = this.GenRequest(HttpMethod.Post, url());
			var requestBody = new Models.NijiuraChanAppAuthorizations() {
				RedirectUri = redirectUri,
				InstallationId = deviceUuid,
				State = state,
				CodeVerifier = codeVerifier,
				CodeChallenge = codeChallenge,
			};
			request.Content = new StringContent(
				requestBody.ToString(),
				Encoding.UTF8,
				"application/json");
			using var r = await Utils.Util.Http(() => httpClient.SendAsync(request));
			json = await r.Content.ReadAsStringAsync();
			if(!(JsonSerializer.Deserialize<Models.NijiuraChanAppAuthorizationsResponse>(json) is { } obj)) {
				throw new Exceptions.ApiInvalidJsonException(json);
			}

			return (requestBody, obj);
		}
		catch(JsonException _) {
			throw new Exceptions.ApiInvalidJsonException(json);
		}
	}

	public async Task<Models.NijiuraChanAppCredentialsResponse> AppCredentials(
		Models.NijiuraChanAppAuthorizations authRequest,
		string code) {

		static string url() => $"{ApiEntry}/app-credentials";
		var json = "";
		try {
			var request = this.GenRequest(HttpMethod.Post, url());
			var requestBody = new Models.NijiuraChanAppCredentials() {
				Code = code,
				CodeVerifier = authRequest.CodeVerifier,
				RedirectUri = authRequest.RedirectUri,
			};
			request.Content = new StringContent(
				requestBody.ToString(),
				Encoding.UTF8,
				"application/json");
			using var r = await Utils.Util.Http(() => httpClient.SendAsync(request));
			json = await r.Content.ReadAsStringAsync();
			if(!(JsonSerializer.Deserialize<Models.NijiuraChanAppCredentialsResponse>(json) is { } obj)) {
				throw new Exceptions.ApiInvalidJsonException(json);
			}

			return obj;
		}
		catch(JsonException) {
			throw new Exceptions.ApiInvalidJsonException(json);
		}
	}

	public async Task<Models.NijiuraChanChunk> GetThreadInfo(string threadId) {
		static string url(string threadId) => $"{ApiEntry}/threads/{threadId}/chunks/0";

		var json = "";
		try {
			var request = this.GenRequest(HttpMethod.Get, url(threadId));
			using var r = await Utils.Util.Http(() => httpClient.SendAsync(request));
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
			return $"{ApiEntry}/threads/{threadId}/state?after={after}";
		}

		var json = "";
		try {
			var request = this.GenRequest(HttpMethod.Get, url(threadId, latestResSeq));
			using var r = await Utils.Util.Http(() => httpClient.SendAsync(request));
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

	public async Task<bool> SendDel(string resId, string passwd, string credential) {
		static string url(string resId) => $"{ApiEntry}/posts/{resId}/reactions/del";

		var request = new HttpRequestMessage(HttpMethod.Put, url(resId));
		request.Headers.Add("x-aimg-token", credential);
		request.Headers.Add("x-aimg-delete-key", Uri.EscapeDataString(passwd));

		using var r = await Utils.Util.Http(() => httpClient.SendAsync(request));
		return true;
	}

	public async Task<bool> DeleteRes(string resId, string passwd, string credential) {
		static string url(string resId) => $"{ApiEntry}/posts/{resId}";

		var request = new HttpRequestMessage(HttpMethod.Delete, url(resId));
		request.Headers.Add("x-aimg-token", credential);
		request.Content = new StringContent(
			new RequestDel() {
				DeleteKey = passwd
			}.ToString(),
			Encoding.UTF8,
			"application/json");

		using var r = await Utils.Util.Http(() => httpClient.SendAsync(request));
		return true;
	}


	public IObservable<Models.NijiuraChanChunk> GetThreadInfoSerial(string threadId)
		=> this.serialRunner.Dispatch(async () => await this.GetThreadInfo(threadId));
	public IObservable<Models.NijiuraChanState> GetThreadSerial(string threadId, int? latestResSeq = null)
		=> this.serialRunner.Dispatch(async () => await this.GetThread(threadId, latestResSeq));

}


file class RequestDel : Models.JsonObject {
	[JsonPropertyName("deleteKey")]
	[JsonInclude]
	public required string DeleteKey {  get; init; }
}