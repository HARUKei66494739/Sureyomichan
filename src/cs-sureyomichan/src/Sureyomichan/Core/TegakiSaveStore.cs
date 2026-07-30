using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Haru.Kei.SureyomiChan.Core;

/// <summary>tegaki_saveのtegakiグローバル変数をWebViewとやりとりするためのinterface</summary>
public interface ITegakiSaveStore {
	/// <summary>tegakiグローバル変数を表すjson</summary>
	/// <returns></returns>
	public string GetStore(int resNo);
}

class TegakiSaveStore : ITegakiSaveStore {
	/// <summary>tegakiグローバル変数定義</summary>
	class StoreObject : Models.JsonObject {
		[JsonPropertyName("res")]
		[JsonInclude]
		public required List<Models.TegakiSaveResData> TegakiData { get; init; }
	}

	private readonly object lockObj = new();
	private Dictionary<string, List<Models.TegakiSaveResData>> TegakiData { get; } = new();


	string ITegakiSaveStore.GetStore(int resNo) {
		return new StoreObject() {
			TegakiData = ToTegakiSaveModels(new(resNo)),
		}.ToString();
	}

	public List<Models.TegakiSaveResData> ToTegakiSaveModels(Helpers.ThreadId resId) {
		lock(this.lockObj) {
			return this.TegakiData.TryGetValue($"{resId}", out var it) switch {
				true => [.. it],
				_ => []
			};
		}
	}


	public void Add(Helpers.ThreadId threadId, Models.SureyomiChanModel m, bool isNg, IEnumerable<Models.AttachmentObject> attachments) {
		lock(this.lockObj) {
			var model = m.ToTegakiSaveModel(isNg: isNg, attachments: attachments);
			if(this.TegakiData.TryGetValue($"{threadId}", out var lt)) {
				lt.Add(model);
			} else {
				this.TegakiData.Add($"{threadId}", new() { model });
			}
		}
	}

	public void MarkNg(int resNo) {
		lock(this.lockObj) {
			foreach(var it in this.TegakiData.Values) {
				if(it.Where(x => x.ResNo == $"{resNo}")
					.FirstOrDefault() is { } target) {

					target.TegakiNg = true;
					return;
				}
			}
		}
	}

	public void Clear(Helpers.ThreadId resId) {
		lock(this.lockObj) {
			this.TegakiData.Remove($"{resId}");
		}
	}
}
