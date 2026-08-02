using LiteDB;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Ja;
using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Analysis.Ja.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace Haru.Kei.SureyomiChan.Utils;
//
// 雑多なユーティリティクラス
//

static class Util {
	class HttpStoreImpl /*: Models.IImageStore*/ {
		private const string DbFile = "http.db";
		private const string VersionTable = "version";
		private const string CookieTable = "cookie";
		private static readonly object lockObj = new();
		private string __DbFile => Path.Combine(AppContext.BaseDirectory, DbFile);

		public HttpStoreImpl() {}

		public IEnumerable<int> Get(string board, Helpers.ThreadId threadId, string imageName) {
			lock(lockObj) {
				try {
					using var db = new LiteDatabase(__DbFile);
					return db.GetCollection<DbCookieObject>(CookieTable)
						.Query()
						.Select(x => 0)
						.ToArray()
						.AsReadOnly();
				}
				catch(LiteDB.LiteException e) {
					Logger.Instance.Error(e);
					return Array.Empty<int>();
				}
			}
		}
	}

	/// <summary>UNIX時間(秒)から変換</summary>
	/// <param name="t"></param>
	/// <returns></returns>
	public static DateTime FromUnixTimeSeconds(long t)
	=> new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
		.AddSeconds(t)
		.ToLocalTime();

	/// <summary>UNIX時間(ミリ秒)から変換</summary>
	/// <param name="t"></param>
	/// <returns></returns>
	public static DateTime FromUnixTimeMiliSeconds(long t)
		=> new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			.AddMilliseconds(t)
			.ToLocalTime();

	/// <summary>UNIX時間(秒)に変換</summary>
	/// <param name="d"></param>
	/// <returns></returns>
	public static long ToUnixTimeSeconds(DateTime d) {
		var offest = TimeZoneInfo.Local.GetUtcOffset(d);
		return new DateTimeOffset(d).ToUnixTimeMilliseconds()
				- (((offest.Hours * 3600) + (offest.Minutes * 60) + offest.Seconds));
	}

	public static string FormatFutabaDateTime(DateTime d) {
		var date = d.ToString("yy/MM/dd");
		var dw = d.DayOfWeek switch {
			DayOfWeek.Sunday => "日",
			DayOfWeek.Monday => "月",
			DayOfWeek.Tuesday => "火",
			DayOfWeek.Wednesday => "水",
			DayOfWeek.Thursday => "木",
			DayOfWeek.Friday => "金",
			DayOfWeek.Saturday => "土",
			_ => "？"
		};
		var time = d.ToString("HH:mm:ss");
		return $"{date}({dw}){time}";
	}

	/// <summary>
	/// HTTP API呼び出しの定型処理
	/// </summary>
	/// <param name="http"></param>
	/// <returns></returns>
	/// <exception cref="Exceptions.ApiHttpErrorException"></exception>
	/// <exception cref="Exceptions.ApiHttpConnectionException"></exception>
	// TODO: あとで名前考える
	public static async Task<HttpResponseMessage> Http(Func<Task<HttpResponseMessage>> http) {
		var url = "--";
		try {
			var r = await http();
			url = r.RequestMessage?.RequestUri?.ToString() ?? url;
			r.EnsureSuccessStatusCode();
			return r;
		}
		catch (HttpRequestException ex) {
			throw new Exceptions.ApiHttpErrorException(url, ex);
		}
		catch (Exception ex) when (ex is SocketException || ex is TimeoutException) {
			throw new Exceptions.ApiHttpConnectionException(ex);
		}
	}

	public static async Task<T> AwaitObserver<T>(IObservable<T> o, T defaultValue)
			=> await Task.Run(async () => {
				T result = defaultValue;
				var ev = new AutoResetEvent(false);
				o.Subscribe(
					x => {
						result = x;
						ev.Set();
					}, ex => {
						throw ex;
					});
				ev.WaitOne();
				return result;
			});

	public static Task<int> AddCustomUrlScheme(string name, string exePath, nint hwnd)
		=> DoCustomUrlScheme(
			@$"/c reg add ""HKEY_CLASSES_ROOT\{name}"" /v ""URL Protocol"" /t ""REG_SZ"" /f  & reg add ""HKEY_CLASSES_ROOT\{name}\shell\open\command"" /t ""REG_SZ"" /d ""{exePath} %1"" /f",
			hwnd);

	public static Task<int> RemoveCustomUrlScheme(string name, nint hwnd)
		=> DoCustomUrlScheme(
			@$"/c reg delete ""HKEY_CLASSES_ROOT\{name}"" /f",
			hwnd);

	private static async Task<int> DoCustomUrlScheme(string arg, nint hwnd) {
		var psi = new System.Diagnostics.ProcessStartInfo() {
			UseShellExecute = true,
			FileName = "cmd.exe",
			Verb = "runas",
			Arguments = arg,

			ErrorDialog = true,
			ErrorDialogParentHandle = hwnd,
		};

		try {
			return await Task.Run(() => {
				using var p = System.Diagnostics.Process.Start(psi);
				p?.WaitForExit();
				return p?.ExitCode ?? 255;
			});
		}
		catch(System.ComponentModel.Win32Exception e) {
			Logger.Instance.Error(e);
			return await Task.FromResult(255);
		}
	}

	public static (SureyomiChanBoardId Board, Helpers.ThreadId ThreadId, bool IsLatest)? ParseCommandLine(string cmd) {
		static (SureyomiChanBoardId, Helpers.ThreadId, bool)? result((SureyomiChanBoardId Board, Helpers.ThreadId Thread) v, bool isLatest) {
			Logger.Instance.Info($"コマンドラインを解析しました => {v.Board}, {v.Thread}, {isLatest}");
			return (v.Board, v.Thread, isLatest);
		}
		static (SureyomiChanBoardId, Helpers.ThreadId, bool)? error(string cmd) {
			Logger.Instance.Info($"コマンドラインは不正でした => {cmd}");
			return null;
		}

		var span = cmd.AsSpan();
		// argv[0]を削除
		if(span[0] == '\"') {
			span = span.Slice(1);
			span = span.Slice(span.IndexOf('\"') + 1);
		} else {
			span = span.IndexOf(' ') switch {
				int v when 0 < v => span.Slice(v + 1),
				_ => span.Slice(span.Length),
			};
		}

		// argv[1]を取り出し
		span = span.IndexOf(' ') switch {
			int v when 0 < v => span.Slice(0, v),
			_ => span
		};
		if(span.Length == 0) {
			return null;
		}

		Logger.Instance.Info($"コマンドラインを解析します => {span}");
		var uri = new Uri(span.ToString());
		if(uri.Scheme == SureyomiChanEnviroment.Scheme) {
			if(!SureyomiChanEnviroment.SupportCommands.Where(x => x == uri.Host).Any()) {
				return error(span.ToString());
			}

			if(uri.Host == SureyomiChanEnviroment.CommandOpen) {
				var p = uri.LocalPath.Split("/");
				if(p.Length == 3) {
					static (SureyomiChanBoardId, Helpers.ThreadId)? parseType1(string p1, string p2) {
						var board = new[] { SureyomiChanBoardId.FutabaImg, SureyomiChanBoardId.NijiuraChanAimg }.Select<SureyomiChanBoardId, SureyomiChanBoardId?>(
							x => (SureyomiChanEnviroment.GetStaticString(x, SureyomiChanBoardItem.URiCommand) == p1) switch {
								true => x,
								false => null
							}).FirstOrDefault(x => x != null);
						if(board is null) {
							return default;
						}

						if(!uint.TryParse(p2, out var uno)) {
							return default;
						}
						return (board.Value, new((int)uno));
					}
					static (SureyomiChanBoardId, Helpers.ThreadId)? parseType2(string p1, string p2) {
						var board = new[] { SureyomiChanBoardId.NijiuraChan__Ts }.Select<SureyomiChanBoardId, SureyomiChanBoardId?>(
							x => (SureyomiChanEnviroment.GetStaticString(x, SureyomiChanBoardItem.URiCommand) == p1) switch {
								true => x,
								false => null
							}).FirstOrDefault(x => x != null);
						if(board is null) {
							return default;
						}

						if(!Regex.IsMatch(p2, "^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$", RegexOptions.IgnoreCase)) {
							return default;
						}

						return (board.Value, new(p2));
					}
					static bool isLatest(Uri uri) {
						if(1 < uri.Query.Length) {
							foreach(var it in uri.Query.Substring(1).Split('&')) {
								if($"{it}" == "latest") {
									return true;
								}
							}
						}
						return false;
					}

					if(parseType1(p[1], p[2]) is { } v1) {
						return result(v1, isLatest(uri));
					}
					if(parseType2(p[1], p[2]) is { } v2) {
						return result(v2, isLatest(uri));
					}
				}
			}
		}
		return error(span.ToString());
	}

	public static IEnumerable<Models.Token> Tokenize(string text) {
		var s_userDictionary = new UserDictionary(new StringReader("日本経済新聞,日本 経済 新聞,ニホン ケイザイ シンブン,カスタム名詞"));
		using var reader = new StringReader(text);
		using var tokenizer = new JapaneseTokenizer(reader, s_userDictionary, true, JapaneseTokenizerMode.SEARCH);

		using var ts = new TokenStreamComponents(tokenizer, tokenizer).TokenStream;
		ts.Reset();
		var r = new List<Models.Token>();
		while(ts.IncrementToken()) {
			var startOffset = 0;
			var endOffset = 0;
			var term = "";
			var partOfSpeech = "";

			if(ts.HasAttribute<IOffsetAttribute>()
				&& ts.GetAttribute<IOffsetAttribute>() is { } offsetAtt) {

				startOffset = offsetAtt.StartOffset;
				endOffset = offsetAtt.EndOffset;
			}

			if(ts.HasAttribute<IPartOfSpeechAttribute>() && ts.GetAttribute<IPartOfSpeechAttribute>() is { } prtAtt) {
				partOfSpeech = prtAtt.GetPartOfSpeech();
			}

			term = ts.GetAttribute<ICharTermAttribute>().ToString();
			r.Add(new() {
				Term = term,
				PartOfSpeech = partOfSpeech,
				StartOffset = startOffset,
				EndOffset = endOffset,
			});
		}
		return r.AsReadOnly();
	}

	public static string GetSaveDirectoryPath(Models.Config config, Models.SureyomiChanThreadInfo threadInfo) {
		var root = config.PathDwonloadValue;
		var sb = new StringBuilder(config.SaveSubFolderName);
		sb.Replace("$Board", $"{SureyomiChanEnviroment.GetStaticString(threadInfo.BoardId)}");
		sb.Replace("$Thread", $"{threadInfo.ThreadNo}");
		var sub = sb.ToString();

		return !string.IsNullOrWhiteSpace(sub) switch {
			true => Path.Combine(root, sub),
			_ => root,
		};
	}
}

file class DbVersionObject {
	public string Url { get; set; } = "";
}

file class DbCookieObject {
	public string Url { get; set; } = "";
	public string Name { get; set; } = "";
	public string Value { get; set; } = "";
}