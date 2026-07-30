using Prism.Events;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Haru.Kei.SureyomiChan.Utils;

class Singleton {
	private readonly Helpers.FutabaUrl futabaUrl = new();
	private readonly Helpers.NijiuraChanUrl nijiuraChanUrl = new();
	private readonly Helpers.NijiuraChanTsUrl nijiuraChanTsUrl = new();

	public HttpClient HttpClient { get; }
	public Helpers.IApiUrl FutabaUrl => this.futabaUrl;
	public Helpers.IApiUrl NijiuraChanUrl => this.nijiuraChanUrl;
	public Helpers.IApiUrl NijiuraChanTsUrl => this.nijiuraChanTsUrl;

	public Helpers.FutabaApi FutabaApi { get; }
	public Helpers.NijiuraChanApi NijiuraChanApi { get; }
	public Core.NijiuraChanTsApi NijiuraChanTsApi { get; }

	public Helpers.StartupSequence StartupSequence { get; } = new();
	public EventAggregator PrismMessenger { get; } = new();

	public Singleton() {
		/*
		var cookies = new CookieContainer();
		// TODO: Cookie読み込み処理
		foreach((string name, string value) it in (Array.Empty<(string, string)>())) {
			cookies.Add(new Cookie(it.name, it.value));
		}
		var clientHandler = new HttpClientHandler();
		clientHandler.CookieContainer = cookies;
		clientHandler.UseCookies = true;
		*/

		this.HttpClient = new(/*clientHandler*/);
		this.HttpClient.DefaultRequestHeaders.Add("User-Agent", "SureyomiChan/v1");
		this.HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

		this.FutabaApi = new(this.HttpClient, this.futabaUrl);
		this.NijiuraChanApi = new(this.HttpClient, this.NijiuraChanUrl);
		this.NijiuraChanTsApi = new(this.HttpClient, this.NijiuraChanTsUrl);
	}

	public static Singleton Instance {
		get {
			if (field == null) {
				field = new();
			}
			return field;
		}
	}
}
