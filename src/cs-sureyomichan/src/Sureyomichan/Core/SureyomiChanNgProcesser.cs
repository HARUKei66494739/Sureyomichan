using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Haru.Kei.SureyomiChan.Core;

class SureyomiChanNgProcesser(WebView2Proxy webView2, ConfigProxy config) {

	public async Task<Models.NgResult> IsNgFromBody(Helpers.ThreadId threadId, Models.SureyomiChanModel m, Models.DifferenceHash? dhash) {
		var con = config.Get();
		if(m.HasId && con.NonReadId) {
			return await Task.FromResult(ToResult(true));
		}

		if(m.DeleteType != Models.SureyomiChanDeleteType.None) {
			return await Task.FromResult(ToResult(true));
		}

		var ret = await webView2.RunPlugin(threadId, m, dhash?.Value);
		if(ret is { }) {
			Action<string> func = ret.IsError switch {
				true => Utils.Logger.Instance.Error,
				_ => Utils.Logger.Instance.Info,
			};
			if(!string.IsNullOrEmpty(ret.Message)) {
				func($"tegaki_saveからのメッセ―ジ => {ret.Message}");
			}
		}
		return ToResult(ret);
	}

	public async Task<Models.NgResult> IsNgFromImage(Models.DifferenceHash dhash) {
		return await Task.FromResult(Models.NgResult.Default);
	}

	private static Models.NgResult ToResult(bool b) => new Models.NgResult(b, "");
	private static Models.NgResult ToResult(Models.TegakiSavePluginResult? r) => r switch {
		{ } v => new Models.NgResult(v.IsStop, v.ResultValue),
		_ => ToResult(false),
	};
		
}
