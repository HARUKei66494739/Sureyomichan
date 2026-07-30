using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Haru.Kei.SureyomiChan.Core;
partial class SureyomiChanApiLooper : IDisposable {
	interface IWorker {
		IObservable<Models.SureyomiChanResponse> GetThread(int? latestResNo);
	}

	private readonly System.Threading.CountdownEvent condition = new(1);
	private readonly UiMessageDispatcher uiMsgDispatcher;
	private readonly IConfigProxy config;
	private readonly CancellationTokenSource cancel;
	private readonly IWorker worker;
	private IDisposable? runSubscriber = null;
	private bool isDisposed = false;

	public SureyomiChanApiLooper(string urlString, Helpers.IApiUrl url, Helpers.ThreadId threadId, UiMessageDispatcher uiMsgDispatcher, IConfigProxy config, WebView2Proxy webView) {
		this.uiMsgDispatcher = uiMsgDispatcher;
		this.config = config;

		this.cancel = new();
		this.worker = url.BoardId switch {
			SureyomiChanBoardId.FutabaImg => new FutabaApiWorker(urlString, threadId, this.config),
			SureyomiChanBoardId.NijiuraChanAimg => new NijiuraChanInternalApiWorker(urlString, threadId, this.config, webView),
			SureyomiChanBoardId.NijiuraChan__Ts => new NijiuraChanTsApiWorker(urlString, threadId, this.config, webView),
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


	public void Run(Func<Models.SureyomiChanResponse, bool, Task> callBack, bool skipToLast, int? latestResNo) {
		this.runSubscriber = Observable.Create<int>(async o => {
			await Task.Run(async () => {
				int? latestNo = latestResNo;
				bool skip = skipToLast;
				while (!this.cancel.IsCancellationRequested) {
					Utils.Logger.Instance.Info($"API呼び出しを開始 => worker={this.worker.GetType().Name}, latestNo={latestNo}, skip={skip}");
					uiMsgDispatcher.DispatchBeginGetApi();

					this.condition.Reset();
					worker.GetThread(latestNo)
						.Subscribe(async x => {
							try {
								Utils.Logger.Instance.Info($"API呼び出しが成功");
								uiMsgDispatcher.DispatchEndGetApi(true, x);

								if(x.NewReplies.LastOrDefault() is { } it) {
									latestNo = it.No;
								}

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