using Haru.Kei.SureyomiChan.Helpers;
using Haru.Kei.SureyomiChan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Security.Policy;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Haru.Kei.SureyomiChan.Core; 
partial class SureyomiChanApiLooper {
	class NijiuraChanApiWorker : IWorker {
		private readonly IConfigProxy config;
		private readonly string urlString;
		private readonly Helpers.ThreadId threadId;
		private readonly WebView2Proxy webView;

		public NijiuraChanApiWorker(string urlString, Helpers.ThreadId threadId, IConfigProxy config, WebView2Proxy webView) {
			this.urlString = urlString;
			this.threadId = threadId;
			this.config = config;

			this.webView = webView;
		}

		public IObservable<SureyomiChanResponse> GetThread(int? latestResNo) {
			var url = this.GenApiThread(this.threadId.ThreadNo, latestResNo);
			return Observable.Return(url)
				.ObserveOn(System.Reactive.Concurrency.ThreadPoolScheduler.Instance)
				.Select(x => {
					var nowTime = DateTime.Now;
					var task = this.webView.__RequestApi(url);
					task.Wait();
					var json = task.Result;

					IEnumerable<Models.NijiuraChanReplyV1> replies;
					if(latestResNo is { }) {
						var o = JsonSerializer.Deserialize<Models.NijiuraChanResponse<Models.NijiuraChanNewThreadDataV1>>(json);
						replies = o?.Data?.Replies ?? Array.Empty<Models.NijiuraChanReplyV1>();
					} else {
						var o = JsonSerializer.Deserialize<Models.NijiuraChanResponse<Models.NijiuraChanThreadDataV1>>(json);
						replies = o?.Data?.Replies ?? Array.Empty<Models.NijiuraChanReplyV1>();
					}

					return new Models.SureyomiChanResponse() {
						BoardId = SureyomiChanBoardId.NijiuraChanAimg,
						ThreadId = this.threadId,
						IsAlive = true,
						IsMaxRes = SureyomiChanEnviroment.NijiuraChanMaxRes <= (replies.LastOrDefault()?.Number ?? 0),
						Soudane = 0,
						CurrentTime = nowTime,
						DieTime = nowTime.AddHours(1),
						NewReplies = replies.Select(x => x.ToSureyomiChanModel(this.threadId, new NijiuraChanInteraction(this.urlString, x, null, this.config))).ToArray() ?? new SureyomiChanModel[0],
						SupportFeature = new NijiuraChanFeature(),
					};
				});
		}
		
		private string GenApiThread(int thread, int? latestNo) {
			return latestNo switch {
				int v => $"https://nijiurachan.net/api/v1/thread/{thread}/new?after={v}",
				_ => $"https://nijiurachan.net/api/v1/thread/{thread}"
			};
		}
	}


	// 非公開API

	class NijiuraChanInternalApiWorker : IWorker {
		private readonly IConfigProxy config;
		private readonly string urlString;
		private readonly Helpers.ThreadId threadId;
		private readonly WebView2Proxy webView;

		public NijiuraChanInternalApiWorker(string urlString, Helpers.ThreadId threadNo, IConfigProxy config, WebView2Proxy webView) {
			this.urlString = urlString;
			this.threadId = threadNo;
			this.config = config;

			this.webView = webView;
		}

		public IObservable<SureyomiChanResponse> GetThread(int? latestResNo) {
			static string getApi(int threadNo, int? latestResNo) {
				return $"https://nijiurachan.net/api/thread/{threadNo}";
			}

			var url = getApi(this.threadId.ThreadNo, latestResNo);
			return Observable.Return(url)
				.ObserveOn(System.Reactive.Concurrency.ThreadPoolScheduler.Instance)
				.Select(x => {
					var json = "";
					try {
						var nowTime = DateTime.Now;
						var task = this.webView.__RequestApi(url);
						task.Wait();
						json = task.Result;
						var o = JsonSerializer.Deserialize<Models.NijiuraChanResponse<Models.NijiuraChanThreadInternalData>>(json);

						IEnumerable<Models.NijiuraChanPostInternal> replies;
						if(latestResNo is { } lno) {
							replies = o?.Data?.Posts.Where(x => lno < x.Id).ToArray() ?? Array.Empty<Models.NijiuraChanPostInternal>();
						} else {
							replies = o?.Data?.Posts.Skip(1).ToArray() ?? Array.Empty<Models.NijiuraChanPostInternal>();
						}

						return new Models.SureyomiChanResponse() {
							BoardId = SureyomiChanBoardId.NijiuraChanAimg,
							ThreadId = threadId,
							IsAlive = !o?.Data?.Thread.IsArchived ?? true,
							IsMaxRes = SureyomiChanEnviroment.NijiuraChanMaxRes <= (replies.LastOrDefault()?.NumberInThread ?? 0),
							Soudane = o?.Data?.Thread.SoudaneCount ?? 0,
							CurrentTime = nowTime,
							DieTime = o?.Data?.Thread.ExpiresAtDateTime ?? nowTime.AddHours(1),
							NewReplies = replies.Select(x => x.ToSureyomiChanModel(this.threadId, new NijiuraChanInternalInteraction(this.urlString, x, null, this.config))).ToArray(),
							SupportFeature = new NijiuraChaninternalFeature(),
						};
					}
					catch(JsonException _) {
						throw new Exceptions.ApiInvalidJsonException(json);
					}
				});
		}
	}


	class NijiuraChanTsApiWorker : IWorker {
		private readonly NijiuraChanTsApi api;
		private readonly IConfigProxy config;
		private readonly string urlString;
		private readonly Helpers.ThreadId threadId;
		private readonly WebView2Proxy webView;

		public NijiuraChanTsApiWorker(string urlString, Helpers.ThreadId threadNo, IConfigProxy config, WebView2Proxy webView) {
			this.urlString = urlString;
			this.threadId = threadNo;
			this.config = config;

			this.api = Utils.Singleton.Instance.NijiuraChanTsApi;
			this.webView = webView;
		}


		public IObservable<Models.SureyomiChanResponse> GetThread(int? latestResNo) {
			var nowTime = DateTime.Now;
			return this.api.GetThreadSerial($"{this.threadId}", latestResNo)
				.Select(x => new Models.SureyomiChanResponse() {
					BoardId = SureyomiChanBoardId.NijiuraChan__Ts,
					ThreadId = threadId,
					IsAlive = x.ArchivedAt is null,
					IsMaxRes = SureyomiChanEnviroment.NijiuraChanMaxRes <= x.ReplyCount,
					Soudane = x.PostStates?.Where(y => y.Sequence == 0).FirstOrDefault()?.Reaction.Up ?? 0,
					CurrentTime = nowTime,
					DieTime = x.ExpiresAtDateTime,
					NewReplies = x.NewPosts.Select(y => y.ToSureyomiChanModel(this.threadId, new NijiuraChanTsInteraction(this.urlString, y, null, this.config))).ToArray(),
					SupportFeature = new NijiuraChanTsFeature(),
				});
		}
	}
}


class NijiuraChanTsApi {
	private readonly Random random = new();
	private readonly HttpClient httpClient;
	private readonly Helpers.IApiUrl apiUrl;
	private readonly SerialRunner serialRunner = new(1000);
	private static readonly string UrlToken = "https://api-staging.nijiurachan.net/tokens";

	public NijiuraChanTsApi(HttpClient httpClient, Helpers.IApiUrl apiUrl) {
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

	public async Task<string> GetThreadMeta(string threadId) {
		static string url(string threadId) => $"https://api-staging.nijiurachan.net/threads/{threadId}/chunks/0";

		var json = "";
		try {
			using var r = await Utils.Util.Http(() => httpClient.GetAsync(url(threadId)));
			json = await r.Content.ReadAsStringAsync();
			/*
			if(JsonSerializer.Deserialize<Models.NijiuraChanState>(json) is { } obj) {
				return obj;
			} else {
				throw new Exceptions.ApiInvalidJsonException(json);
			}
			*/
			return json;
		}
		catch(JsonException _) {
			throw new Exceptions.ApiInvalidJsonException(json);
		}
	}


	public async Task<Models.NijiuraChanState> GetThread(string threadId, int? latestResSeq = null) {
		static string url(string threadId, int? latestResSeq) {
			var after = latestResSeq ?? 0;
			return $"https://api-staging.nijiurachan.net/threads/{threadId}/state?after={after}";
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

	public IObservable<Models.NijiuraChanState> GetThreadSerial(string threadId, int? latestResSeq = null)
		=> this.serialRunner.Dispatch(async () => await this.GetThread(threadId, latestResSeq));

}



file class NijiuraChanFeature : ISureyomiChanFeature {
	public bool IsSupportThreadOld => false;
	public bool IsSupportThreadDie => false;
	public bool IsSupportInspectSoudane => false;
}

file class NijiuraChaninternalFeature : ISureyomiChanFeature {
	public bool IsSupportThreadOld => true;
	public bool IsSupportThreadDie => true;
	public bool IsSupportInspectSoudane => true;
}

file class NijiuraChanTsFeature : ISureyomiChanFeature {
	public bool IsSupportThreadOld => true;
	public bool IsSupportThreadDie => true;
	public bool IsSupportInspectSoudane => true;
}
