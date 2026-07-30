using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace Haru.Kei.SureyomiChan.Core;

class UiMessageDispatcher {

	public required Action? OnBeginApi { get; set; }
	public required Action<bool>? OnEndApi { get; set; }
	public required Action<DateTime, DateTime>? OnUpdateDieTime { get; set; }
	public required Action? OnMaxRes { get; set; }
	public required Action? OnThreadDied { get; set; }
	public required Action<IEnumerable<Models.Bindables.BindableSureyomiChanModel>>? OnNewReplies { get; set; }
	public required Action? OnBouyomiChanNotFound { get; set; }

	public required Action? OnSartDisplayTegaki { get; set; }
	public required Action? OnEndDisplayTegaki { get; set; }
	public required Action<string>? OnErrorTegaki { get; set; }


	public void DispatchBeginGetApi() => Dispatch(() => this.OnBeginApi?.Invoke());
	public void DispatchEndGetApi(bool sucessed, (Models.SureyomiChanThreadInfo Info, Models.SureyomiChanResponse Response)? apiResult)
		=> Dispatch(() => {
			this.OnEndApi?.Invoke(sucessed);
			if(!sucessed) {
				return;
			}
			if(!(apiResult is { } ret)) {
				return;
			}

			if(ret.Response.SupportFeature.IsSupportThreadOld && ret.Response.IsAlive) {
				this.OnUpdateDieTime?.Invoke(ret.Response.CurrentTime, ret.Response.DieTime);
			}
			if(ret.Response.SupportFeature.IsSupportThreadOld && ret.Response.IsAlive && ret.Response.IsMaxRes) {
				this.OnMaxRes?.Invoke();
			}
			if(ret.Response.SupportFeature.IsSupportThreadDie && !ret.Response.IsAlive) {
				this.OnThreadDied?.Invoke();
			}
		});

	public void DispatchNewRiplies(IEnumerable<Models.Bindables.BindableSureyomiChanModel> newReplies) => Dispatch(() => this.OnNewReplies?.Invoke(newReplies));

	// TegakiSave
	public void DispatchStartDisplayTegakiPng() => Dispatch(() => this.OnSartDisplayTegaki?.Invoke());
	public void DispatchEndDisplayTegakiPng() => Dispatch(() => this.OnEndDisplayTegaki?.Invoke());
	public void DispatchErrorTegakiPng(string message) => Dispatch(() => this.OnErrorTegaki?.Invoke(message));

	// BouyomiChan
	public void DispatchBouyomiChanNotFound() => Dispatch(() => this.OnBouyomiChanNotFound?.Invoke());


	private static void Dispatch(Action dispatch) {
		Observable.Return(dispatch)
			.ObserveOn(Reactive.Bindings.UIDispatcherScheduler.Default)
			.Subscribe(x => {
				x();
			});
	}
}


class UiMessageMultiDispatcher {
	private readonly object lockObj = new();
	private readonly List<WeakReference<UiMessageDispatcher>> items = new();

	public void Register(UiMessageDispatcher dispatcher) {
		lock(lockObj) {
			this.items.Add(new(dispatcher));

			foreach(var rm in this.items.Where(x => !x.TryGetTarget(out var _)).ToArray()) {
				this.items.Remove(rm);
			}
		}
	}

	public void Dispatch(Action<UiMessageDispatcher> action) {
		var invoke = new List<UiMessageDispatcher>();
		lock(lockObj) {
			foreach(var it in this.items) {
				if(it.TryGetTarget(out var d)) {
					invoke.Add(d);
				}
			}
		}

		foreach(var it in invoke) {
			action.Invoke(it);
		}
	}
}