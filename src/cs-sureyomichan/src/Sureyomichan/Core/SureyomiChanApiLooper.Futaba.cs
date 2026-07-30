using Haru.Kei.SureyomiChan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace Haru.Kei.SureyomiChan.Core; 
partial class SureyomiChanApiLooper {
	class FutabaApiWorker : IWorker {
		private readonly Helpers.FutabaApi api;
		private readonly IConfigProxy config;
		private readonly string urlString;
		private readonly Helpers.ThreadId threadId;

		public FutabaApiWorker(string urlString, Helpers.ThreadId threadId, IConfigProxy config) {
			this.urlString = urlString;
			this.threadId = threadId;
			this.api = Utils.Singleton.Instance.FutabaApi;
			this.config = config;
		}

		public IObservable<Models.SureyomiChanResponse> GetThread(int? latestResNo) {
			return this.api.GetThreadSerial(this.threadId.ThreadNo, latestResNo)
				.Select(x => new Models.SureyomiChanResponse() {
					BoardId = SureyomiChanBoardId.FutabaImg,
					ThreadId = this.threadId,
					IsAlive = x.NowDateTime < x.DieDateTime,
					IsMaxRes = !string.IsNullOrEmpty(x.MaxRes),
					Soudane = x.Soudane.Where(x => x.ResNo == this.threadId.ThreadNo) switch {
						{ } v when v.Count() != 0 => v.FirstOrDefault().Value,
						_ => 0
					},
					CurrentTime = x.NowDateTime,
					DieTime = x.DieDateTime,
					NewReplies = x.Res.Select(x => x.ToSureyomiChanModel(this.threadId, new FutabaInteraction(this.urlString, x, this.api, this.config))).ToArray(),
					SupportFeature = new FutabaFeature(),
				});
		}
	}
}

file class FutabaFeature : ISureyomiChanFeature {
	public bool IsSupportThreadOld => true;
	public bool IsSupportThreadDie => true;
	public bool IsSupportInspectSoudane => false;
}