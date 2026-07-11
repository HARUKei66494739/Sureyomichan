using ControlzEx.Standard;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Haru.Kei.SureyomiChan.Helpers;

class FutabaApi {
	private readonly HttpClient httpClient;
	private readonly IApiUrl apiUrl;
	private readonly SerialRunner serialRunner = new(1000);

	public FutabaApi(HttpClient httpClient, IApiUrl apiUrl) {
		this.httpClient = httpClient;
		this.apiUrl = apiUrl;
	}

	public async Task<Models.FutabaResponse> GetThread(int threadId, int? latestResId = null) {
		var json = "";
		try {
			using var r = await Utils.Util.Http(() => httpClient.GetAsync(this.apiUrl.GenApiThread(threadId, latestResId)));
			json = await r.Content.ReadAsStringAsync();
			if (JsonSerializer.Deserialize<Models.FutabaResponse>(json) is { } obj) {
				return obj;
			} else {
				throw new Exceptions.ApiInvalidJsonException(json);
			}
		}
		catch (JsonException _) {
			throw new Exceptions.ApiInvalidJsonException(json);
		}
	}

	public async Task<bool> PostDelete(int resNo, string passwd) {
		var url = this.apiUrl.GenApiDelete();
		var parameters = new Dictionary<string, string>() {
			{ "responsemode", "ajax" },
			{ $"{resNo}", "delete" },
			{ "pwd", passwd },
			{ "mode", "usrdel" },
		};
		// 画像だけ削除する場合
		//parameters.Add("onlyimgdel", "on");
		var content = new FormUrlEncodedContent(parameters);

		using var response = await Utils.Util.Http(() => httpClient.PostAsync(url, content));
		var r = await response.Content.ReadAsStringAsync();
		return r == "ok";
	}

	public async Task<bool> PostDel(string url, int resNo) {
		var board = "b";
		var parameters = new Dictionary<string, string>() {
			{ "mode", "post" },
			{ $"responsemode", "ajax" },
			{ "b", board },
			{ "d", $"{resNo}" },
			{ "reason", $"{110}" },
		};
		var request = new HttpRequestMessage(HttpMethod.Post, this.apiUrl.GenApiDel());
		request.Content = new FormUrlEncodedContent(parameters);
		request.Headers.Add("referer", url);
		using var response = await Utils.Util.Http(() => httpClient.SendAsync(request));
		var r = await response.Content.ReadAsStringAsync();
		return r == "ok";
	}

	public async Task<byte[]> DonwloadImage(Models.IAttachmentData model) {
		using var response = await Utils.Util.Http(() => httpClient.GetAsync(this.apiUrl.GenImage(model)));
		return await response.Content.ReadAsByteArrayAsync();
	}

	public IObservable<Models.FutabaResponse> GetThreadSerial(int threadId, int? latestResId = null)
		=> this.serialRunner.Dispatch(async () => await this.GetThread(threadId, latestResId));

	public IObservable<bool> PostDeleteSerial(int resNo, string passwd)
		=> this.serialRunner.Dispatch(async () => await this.PostDelete(resNo, passwd));

	public IObservable<bool> PostDelSerial(string url, int resNo)
		=> this.serialRunner.Dispatch(async () => await this.PostDel(url, resNo));
}

