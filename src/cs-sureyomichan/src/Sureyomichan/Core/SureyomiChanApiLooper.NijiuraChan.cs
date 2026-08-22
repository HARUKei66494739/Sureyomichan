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
		private readonly NijiuraChanApi api;
		private readonly IConfigProxy config;
		private readonly Helpers.IApiUrl url;
		private readonly Helpers.ThreadId threadId;
		private readonly __NijiuraChanWebView2Proxy webView;

		public NijiuraChanApiWorker(Helpers.IApiUrl url, Helpers.ThreadId threadNo, IConfigProxy config, __NijiuraChanWebView2Proxy webView) {
			this.url = url;
			this.threadId = threadNo;
			this.config = config;

			this.api = Utils.Singleton.Instance.NijiuraChanTsApi;
			this.webView = webView;
		}

		public IObservable<Models.SureyomiChanThreadInfo> GetThreadInfo()
			/*
			=> this.api.GetThreadInfoSerial($"{this.threadId}")
				.Select(x => new Models.SureyomiChanThreadInfo() {
					BoardId = this.url.BoardId,
					ThreadId = this.threadId,
					ThreadNo = x.BoardNo,
				});
			*/
			=> Observable.FromAsync(async () => await this.api.__GetThreadInfoWithWebView(this.webView, $"{this.threadId}"))
				.Select(x => new Models.SureyomiChanThreadInfo() {
					BoardId = this.url.BoardId,
					ThreadId = this.threadId,
					ThreadNo = x.BoardNo,
				});

		public IObservable<Models.SureyomiChanResponse> GetThread(int? latestResNo) {
			var nowTime = DateTime.Now;
			/*
			return this.api.GetThreadSerial($"{this.threadId}", latestResNo)
				.Select(x => new Models.SureyomiChanResponse() {
					BoardId = SureyomiChanBoardId.NijiuraChanAimg,
					ThreadId = threadId,
					IsAlive = x.ArchivedAt is null,
					IsMaxRes = SureyomiChanEnviroment.NijiuraChanMaxRes <= x.ReplyCount,
					Soudane = x.PostStates?.Where(y => y.Sequence == 0).FirstOrDefault()?.Reaction.Up ?? 0,
					CurrentTime = nowTime,
					DieTime = x.ExpiresAtDateTime,
					NewReplies = x.NewPosts.Select(y => y.ToSureyomiChanModel(this.threadId, new NijiuraChanTsInteraction(y))).ToArray(),
					LatestResNo = x.NewPosts.LastOrDefault()?.Sequence ?? latestResNo,
					SupportFeature = new NijiuraChanTsFeature(),
				});
			*/
			return Observable.FromAsync(async () => await this.api.__GetThreadWithWebView(this.webView, $"{this.threadId}", latestResNo))
				.Select(x => new Models.SureyomiChanResponse() {
					BoardId = SureyomiChanBoardId.NijiuraChanAimg,
					ThreadId = threadId,
					IsAlive = x.ArchivedAt is null,
					IsMaxRes = SureyomiChanEnviroment.NijiuraChanMaxRes <= x.ReplyCount,
					Soudane = x.PostStates?.Where(y => y.Sequence == 0).FirstOrDefault()?.Reaction.Up ?? 0,
					CurrentTime = nowTime,
					DieTime = x.ExpiresAtDateTime,
					NewReplies = x.NewPosts.Select(y => y.ToSureyomiChanModel(this.threadId, new NijiuraChanTsInteraction(y))).ToArray(),
					LatestResNo = x.NewPosts.LastOrDefault()?.Sequence ?? latestResNo,
					SupportFeature = new NijiuraChanFeature(),
				});
		}
	}
}


file class NijiuraChanFeature : ISureyomiChanFeature {
	public bool IsSupportThreadOld => true;
	public bool IsSupportThreadDie => true;
	public bool IsSupportInspectSoudane => true;
}
