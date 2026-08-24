using Haru.Kei.SureyomiChan.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Haru.Kei.SureyomiChan;

static class ModelsExtensions {
	private readonly static TimeZoneInfo jstZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
	private static string? stringnull(string s) => s switch {
	{ } v when !string.IsNullOrEmpty(v) => v,
		_ => null,
	};

	private static string Comment2Text(string com) {
		var s1 = Regex.Replace(com, @"<br>", Environment.NewLine,
			RegexOptions.IgnoreCase | RegexOptions.Multiline);
		var s2 = Regex.Replace(s1, @"<[^>]*>", "",
			RegexOptions.IgnoreCase | RegexOptions.Multiline);
		var s3 = System.Net.WebUtility.HtmlDecode(s2);

		return s3;
	}

	private static DateTime NijiuraChanTime2Local(string time) {
		if(DateTime.TryParseExact(time, "yyyy-MM-dd HH:mm:ss",
			null, System.Globalization.DateTimeStyles.None,
			out var result)) {

			// タイムゾーンが設定されていないので念のため設定する
			var d = result.AddHours(-9);
			var jtc = TimeZoneInfo.ConvertTimeFromUtc(
				new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second, DateTimeKind.Utc),
				jstZone);
			return jtc;
		} else {
			// なんか適当な時間かえす
			return new DateTime(1970, 1, 1);
		}
	}

	private static DateTime NijiuraChanTime2LocalTs(string time)
		=> DateTime.TryParse(time,
			null, System.Globalization.DateTimeStyles.None,
			out var result) switch {
				true => result,
				_ => new(1970, 1, 1),
			};

	extension(Models.SureyomiChanModel source) {
		public bool IsId => source.Id is { };
		//public bool HasImage => source.ImageFileName is { };
		/// <summary>画面表示用の日付テキストを取得</summary>
		/// <returns></returns>
		public string FormatDateTime() => Utils.Util.FormatFutabaDateTime(source.PostTime);
		/// <summary><br>を改行に変換し</br>HTMLタグを除去した表示用本文を取得</summary>
		/// <returns></returns>
		public string FormatBody() => Comment2Text(source.Body);
		/// <summary>読み上げ用の本文を取得</summary>
		/// <returns></returns>
		public string ToSpeakText() {
			static string decodeFromCodePoint(string p1) {
				if(p1[0] == '#') {
					var span = p1.AsSpan().Slice(1);
					if(span[0] switch {
						'x' => true,
						'X' => true,
						_ => false,
					}) {
						span = span.Slice(1);
						try {
							return char.ConvertFromUtf32(
								BitConverter.ToInt32(
									Convert.FromHexString(span)));
						}
						catch(FormatException) { }
					} else {
						try {
							return char.ConvertFromUtf32(
								int.Parse(span));
						}
						catch(FormatException) { }
					}
				}
				return "";
			}

			var speakLines = source.Body.Split("<br>")
				.Select(line => {
					var t1 = Regex.Replace(line, @"<(\""[^\""]*\""|'[^']*'|[^'\"">])*>", "");
					var t2 = Regex.Replace(t1, @"&([^;]+);", m => {
						return m.Groups[1].Value.ToLower() switch {
							"gt" => ">",
							"lt" => "<",
							"amp" => "&",
							"quot" => "\"",
							string v when v[0] == '#' => decodeFromCodePoint(v),
							_ => "",
						};
					});
					return t2;
				});
			return string.Join("\n", speakLines);
		}


		public Models.TegakiSaveResData ToTegakiSaveModel(bool isNg, IEnumerable<Models.AttachmentObject> attachments, string? replaceComment = null)
			=> source.ToTegakiSaveModel(isNg, attachments.FirstOrDefault()?.Hash?.Value, replaceComment);

		public Models.TegakiSaveResData ToTegakiSaveModel(bool isNg, ulong? imageHash, string? replaceComment=null) {
			static string @string(string? s) => s ?? "";

			var r = new Models.TegakiSaveResData() {
				ResCount = source.ResIndex,
				ResNo = $"{source.No}",
				Del = source.DeleteType.FormatString(),
				Id = @string(source.Id),
				Email = source.Email,
				Comment = replaceComment switch {
					{ } x => x,
					_ => source.Body
				},
				Now = source.FormatDateTime(),
				Time = $"{Utils.Util.ToUnixTimeSeconds(source.PostTime)}",
				TegakiNg = isNg,
				ImageHash = imageHash switch {
					{ } v => $"{v}",
					_ => ""
				},
				SureyomiTerms = new(source.Token),

				FileSource = @string(source.Images.FirstOrDefault()?.ImageSource),
				FileThumb = @string(source.Images.FirstOrDefault()?.ThumbnailSource),
				FileSize = source.Images.FirstOrDefault() switch {
					{ } => 1,
					_ => 0,
				},
				FileExtension = @string(System.IO.Path.GetExtension(source.Images.FirstOrDefault()?.ImageFileName))
			};

			return r;
		}
	}

	extension(Models.FutabaResponse source) {
		public DateTime NowDateTime => Utils.Util.FromUnixTimeSeconds(source.NowTime);
		public DateTime DieDateTime => DateTime.TryParse(
			source.DieLong,
			System.Globalization.CultureInfo.InvariantCulture,
			System.Globalization.DateTimeStyles.None, out var d) switch {
				true => d,
				_ => DateTime.MaxValue
			};
	}

	extension(Models.__FutabaResData source) {
		public DateTime PostDateTime => long.TryParse(source.Time, out var v) switch {
			true => Utils.Util.FromUnixTimeMiliSeconds(v),
			_ => DateTime.Now
		};
		public Models.SureyomiChanDeleteType DeleteType => source.Del.ToLower() switch {
			var v when string.IsNullOrEmpty(v) => Models.SureyomiChanDeleteType.None,
			var v when v == "del" => Models.SureyomiChanDeleteType.DeleteFromOwner,
			var v when v == "del2" => Models.SureyomiChanDeleteType.DeleteFromDel,
			var v when v == "selfdel" => Models.SureyomiChanDeleteType.SelfDelete,
			_ => Models.SureyomiChanDeleteType.None,
		};
		public string FormatBody() => Comment2Text(source.Comment);
		public Models.SureyomiChanModel ToSureyomiChanModel(Helpers.ThreadId threadId, Models.ISureyomiChanInteraction interaction) => new(
			threadId: threadId,
			resIndex: source.ResCount,
			no: source.ResNoInt,
			postTime: source.PostDateTime,
			email: source.Email,
			body: source.Comment,
			id: string.IsNullOrEmpty(source.Id) switch {
				true => null,
				_ => source.Id
			},
			deleteType: source.DeleteType,

			images: source.ToImages(),

			token: Utils.Util.Tokenize(source.FormatBody()),
			interaction: interaction);

		private IEnumerable<Models.SureyomiChanImage> ToImages() => source.FileSource switch {
			string v when !string.IsNullOrEmpty(v) => [
				new(Path.GetFileName(v), source.FileSource, source.FileThumb)
			],
			_ => []
		};
	}

	extension(Models.SureyomiChanDeleteType source) {
		public string FormatString() => source switch {
			Models.SureyomiChanDeleteType.DeleteFromOwner => "del",
			Models.SureyomiChanDeleteType.DeleteFromDel => "del2",
			Models.SureyomiChanDeleteType.SelfDelete => "selfdel",
			_ => "",
		};
	}

	extension(Models.NijiuraChanState source) {
		public DateTime? ArchivedAtDateTime => source.ArchivedAt switch {
			{ } v => NijiuraChanTime2LocalTs(v),
			_ => default,
		};
		public DateTime ExpiresAtDateTime => NijiuraChanTime2LocalTs(source.ExpiresAt);
		public DateTime? ClosedAtDateTime => source.ClosedAt switch {
			{ } v => NijiuraChanTime2LocalTs(v),
			_ => default,
		};
	}

	extension(Models.NijiuraChanPost source) {
		public DateTime CreatedAtDateTime => NijiuraChanTime2LocalTs(source.CreatedAt);
		public string FormatBody() => Comment2Text(source.Body);

		public Models.SureyomiChanModel ToSureyomiChanModel(Helpers.ThreadId threadId, Models.ISureyomiChanInteraction interaction) => new(
			threadId: threadId,
			resIndex: source.Sequence,
			no: source.BoardNo,
			postTime: source.CreatedAtDateTime,
			email: "",
			body: RemoveUnicodePrivateChar(source.Body),
			id: string.IsNullOrEmpty(source.DisplayId) switch {
				true => null,
				_ => source.DisplayId
			},
			deleteType: Models.SureyomiChanDeleteType.None,

			images: source.ToImages(),

			token: Utils.Util.Tokenize(source.FormatBody()),
			interaction: interaction);

		// 仕様として入っているaimgだけ今のところ除去する
		private static string RemoveUnicodePrivateChar(string s) {
			var ary = s.Select((x, i) => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(x) switch {
				System.Globalization.UnicodeCategory.PrivateUse => default(int?),
				_ => i
			}).ToArray();
			if(!ary.Any(x => x == default(int?))) {
				return s;
			}

			var r = new StringBuilder();
			foreach(var it in ary) {
				if(it is { } i) {
					r.Append(s[i]);
				}
			}
			return r.ToString();
		}

		private IEnumerable<Models.SureyomiChanImage> ToImages()
			=> source.Attachments
				.Select(x => new Models.SureyomiChanImage(
					Path.GetFileName(x.OriginalUrl),
					x.OriginalUrl,
					x.OriginalUrl))
				.ToArray()
				.AsReadOnly();
	}

	extension(Models.AttachmentObject _) {
		public static Models.AttachmentObject Empty(string fileName, string imageName) => new() {
			IsUpdatedTegakiPng = false,
			FileName = fileName,
			ImageName = imageName,
			OriginalFileBytes = default,
			ImageFileBytes = default,
			Hash = default,
		};
	}
}