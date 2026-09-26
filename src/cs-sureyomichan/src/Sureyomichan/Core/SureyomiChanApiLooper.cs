using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Haru.Kei.SureyomiChan.Core;
partial class SureyomiChanApiLooper : IDisposable {
	interface IWorker {
		IObservable<Models.SureyomiChanThreadInfo> GetThreadInfo();
		IObservable<Models.SureyomiChanResponse> GetThread(int? latestResNo);
	}

	public Models.SureyomiChanThreadInfo? ThreadInfo { get; private set; }

	private readonly System.Threading.CountdownEvent condition = new(1);
	private readonly UiMessageDispatcher uiMsgDispatcher;
	private readonly IConfigProxy config;
	private readonly CancellationTokenSource cancel;
	private readonly IWorker worker;
	private IDisposable? runSubscriber = null;
	private bool isDisposed = false;

	public SureyomiChanApiLooper(string urlString, Helpers.IApiUrl url, Helpers.ThreadId threadId, UiMessageDispatcher uiMsgDispatcher, IConfigProxy config, __NijiuraChanWebView2Proxy webView) {
		this.uiMsgDispatcher = uiMsgDispatcher;
		this.config = config;

		this.cancel = new();
		this.worker = url.BoardId switch {
			SureyomiChanBoardId.FutabaImg => new FutabaApiWorker(url, threadId, this.config),
			SureyomiChanBoardId.NijiuraChanAimg => new NijiuraChanApiWorker(url, threadId, this.config, webView),
			_ => throw new NotSupportedException()
		};
		Utils.Logger.Instance.Info($"ApiLooperの作成完了 => url={url.GetType().Name}, worker={this.worker.GetType().Name}");
	}

	public void Dispose() {
		if(this.isDisposed) {
			return; 
		}

		this.runSubscriber?.Dispose();
		this.cancel?.Cancel();
		this.cancel?.Dispose();
		this.isDisposed = true;
	}


	public void Run(
		Func<
			(Models.SureyomiChanThreadInfo Info, Models.SureyomiChanResponse Response),
			bool, Task
		> callBack,
		bool skipToLast,
		int? latestResNo) {

		this.runSubscriber = Observable.Create<int>(async o => {
			await Task.Run(async () => {
				static IObservable<Models.SureyomiChanThreadInfo> getInfo(
					IWorker worker,
					Models.SureyomiChanThreadInfo? info)
						=> info switch {
							{ } v => Observable.Return(v)
								.ObserveOn(System.Reactive.Concurrency.ImmediateScheduler.Instance),
							_ => worker.GetThreadInfo()
						};

				int? latestNo = latestResNo;
				bool skip = skipToLast;
				while (!this.cancel.IsCancellationRequested) {
					Utils.Logger.Instance.Info($"API呼び出しを開始 => worker={this.worker.GetType().Name}, latestNo={latestNo}, skip={skip}");
					uiMsgDispatcher.DispatchBeginGetApi();

					this.condition.Reset();
					Observable.CombineLatest(
						getInfo(worker, this.ThreadInfo),
						worker.GetThread(latestNo),
						(i, r) => (Info: i, Response: r))
						.Subscribe(async x => {
							try {
								Utils.Logger.Instance.Info($"API呼び出しが成功");
								uiMsgDispatcher.DispatchEndGetApi(true, x);

								this.ThreadInfo = x.Info;
								latestNo = x.Response.LatestResNo;

								await callBack(x, skip);
								skip = false;
							}
							finally {
								this.condition.Signal();
							}
						}, ex => {
							try {
								uiMsgDispatcher.DispatchEndGetApi(false, null);
								Utils.Logger.Instance.Error(ex);
							}
							finally {
								this.condition.Signal();
							}
						}, () => {
							// Subscribe()でawaitを使用しているのでcallBack()が終わった後で
							// ここに入る保障がない
							//this.condition.Signal();
						});
					this.condition.Wait();
					await Task.Delay(5000);
				}
			}, this.cancel.Token);
		}).Subscribe();
	}
}