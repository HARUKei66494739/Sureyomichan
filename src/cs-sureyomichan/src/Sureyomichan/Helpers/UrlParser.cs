using Haru.Kei.SureyomiChan.Models;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Haru.Kei.SureyomiChan.Helpers;

// 仮置き、モデルあたりの名前空間に移動？
class ThreadId {
	private readonly int? threadNo;
	private readonly string? threadId;

	public ThreadId(int threadNo) {
		this.threadNo = threadNo;
		this.threadId = null;
	}

	public ThreadId(string threadId) {
		this.threadNo = null;
		this.threadId = threadId;
	}


	public bool IsInt => this.threadNo != null;
	public bool IsString => this.threadId != null;
	public int ThreadNo => this.threadNo switch {
		{ } v => v,
		_ => throw new InvalidOperationException($"意図しないフロー{nameof(threadId)}はnull"),
	};

	public override int GetHashCode() {
		return this.ToString().GetHashCode();
	}

	public override bool Equals(object? obj) {
		if(object.ReferenceEquals(this, obj)) {
			return true;
		}
		if(obj?.ToString() == this.ToString()) {
			return true;
		}
		return false;
	}

	public override string ToString() {
		if(this.threadNo is { }) {
			return $"{this.threadNo}";
		}
		if(this.threadId is { }) {
			return $"{this.threadId}";
		}

		throw new InvalidOperationException();
	}
}


interface IApiUrl {
	public SureyomiChanBoardId BoardId { get; }
	public string GenUrlThread(ThreadId thread);
	public bool IsValidUrl(string url) => this.ParseThreadNo(url) != null;
	public ThreadId? ParseThreadNo(string url);
}

class FutabaUrl : IApiUrl {
	private readonly string domain;
	private readonly string boardName;
	public string Domain => this.domain;
	public string BoardName => this.boardName;
	public string FutabaEndPoint => $"https://{domain}.2chan.net/{boardName}/futaba.php";
	public string FutabaDelEndPoint => $"https://{domain}.2chan.net/del.php";

	public FutabaUrl() : this("img", "b") { }
	public FutabaUrl(string domain, string boardName) {
		this.domain = domain;
		this.boardName = boardName;
	}

	// 他に影響するのでimgで固定
	public SureyomiChanBoardId BoardId => SureyomiChanBoardId.FutabaImg;
	public string GenUrlThread(ThreadId thread) => $"https://{domain}.2chan.net/{boardName}/res/{thread.ThreadNo}.htm";
	public ThreadId? ParseThreadNo(string url) {
		var m = Regex.Match(url, @$"https://{domain}\.2chan\.net/{boardName}/res/([0-9]+)\.htm");
		if (!m.Success) {
			return null;
		}

		if (!int.TryParse(m.Groups[1].Value, out var no)) {
			return null;
		}

		return new(no);
	}
}

class NijiuraChanUrl : IApiUrl {
	public SureyomiChanBoardId BoardId => SureyomiChanBoardId.NijiuraChanAimg;
	public string GenUrlThread(ThreadId thread) => $"https://nijiurachan.net/pc/thread.php?id={thread.ThreadNo}";
	public ThreadId? ParseThreadNo(string url) {
		var m = Regex.Match(url, @$"https://nijiurachan\.net/pc/thread\.php\?id=([0-9]+)");
		if (!m.Success) {
			return null;
		}

		if (!int.TryParse(m.Groups[1].Value, out var no)) {
			return null;
		}

		return new(no);
	}
}

class NijiuraChanTsUrl : IApiUrl {
	public SureyomiChanBoardId BoardId => SureyomiChanBoardId.NijiuraChan__Ts;
	public string GenUrlThread(ThreadId thread) => $"https://staging.nijiurachan.net/b/ai2/thread/{thread}";
	public ThreadId? ParseThreadNo(string url) {
		var m = Regex.Match(
			url,
			@$"https://staging\.nijiurachan\.net/b/ai2/thread/([a-f0-9]{{8}}-[a-f0-9]{{4}}-[a-f0-9]{{4}}-[a-f0-9]{{4}}-[a-f0-9]{{12}})",
			RegexOptions.IgnoreCase);
		if(!m.Success) {
			return null;
		}

		return new(m.Groups[1].Value);
	}
}