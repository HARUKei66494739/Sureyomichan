using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text.Json.Serialization;

namespace Haru.Kei.SureyomiChan.Models;

#region 旧API-いらなくなったら消す
class NijiuraChanResponse<T> : JsonObject {
	[JsonPropertyName("ok")]
	[JsonInclude]
	public required bool Ok { get; init; }

	[JsonPropertyName("data")]
	[JsonInclude]
	public T? Data { get; init; }

	[JsonPropertyName("error")]
	[JsonInclude]
	public string? Error { get; init; }
}

class NijiuraChanThreadDataV1 : JsonObject {
	[JsonPropertyName("thread")]
	[JsonInclude]
	public required NijiuraChanThreadV1 Thread {  get; init; }
	[JsonPropertyName("replies")]
	[JsonInclude]
	public required IEnumerable<NijiuraChanReplyV1> Replies { get; init; }
	[JsonPropertyName("reply_count")]
	[JsonInclude]
	public required int ReplyCount { get; init; }
}


class NijiuraChanNewThreadDataV1 : JsonObject {
	[JsonPropertyName("thread_id")]
	[JsonInclude]
	public required int Thread { get; init; }
	[JsonPropertyName("after")]
	[JsonInclude]
	public required int After { get; init; }
	[JsonPropertyName("replies")]
	[JsonInclude]
	public required IEnumerable<NijiuraChanReplyV1> Replies { get; init; }
	[JsonPropertyName("count")]
	[JsonInclude]
	public required int Count { get; init; }
	[JsonPropertyName("latest_id")]
	[JsonInclude]
	public required int LatestId { get; init; }
	[JsonPropertyName("thread_reply_count")]
	[JsonInclude]
	public required int ThreadReplyCount { get; init; }
}


class NijiuraChanReplyV1 : JsonObject {
	[JsonPropertyName("id")]
	[JsonInclude]
	public int Id { get; private set; }
	[JsonPropertyName("thread_id")]
	[JsonInclude]
	public int ThreadId { get; private set; }
	[JsonPropertyName("number")]
	[JsonInclude]
	public int Number { get; private set; }
	[JsonPropertyName("name")]
	[JsonInclude]
	public string Name { get; private set; } = "";
	[JsonPropertyName("body")]
	[JsonInclude]
	public string Body { get; private set; } = "";
	[JsonPropertyName("image")]
	[JsonInclude]
	public string? Image { get; private set; }
	[JsonPropertyName("thumb")]
	[JsonInclude]
	public string? Thumb { get; private set; }
	[JsonPropertyName("soudane_count")]
	[JsonInclude]
	public int SoudaneCount { get; private set; }
	[JsonPropertyName("del_count")]
	[JsonInclude]
	public int DelCount { get; private set; }
	[JsonPropertyName("created_at")]
	[JsonInclude]
	public string CreatedAt { get; private set; } = "";
	[JsonPropertyName("poster_id")]
	[JsonInclude]
	public string? PosterId { get; private set; }
}


class NijiuraChanThreadV1 : JsonObject {
	[JsonPropertyName("id")]
	[JsonInclude]
	public int Id { get; private set; }
	[JsonPropertyName("name")]
	[JsonInclude]
	public string Name { get; private set; } = "";
	[JsonPropertyName("body")]
	[JsonInclude]
	public string Body { get; private set; } = "";
	[JsonPropertyName("image")]
	[JsonInclude]
	public string? Image { get; private set; }
	[JsonPropertyName("thumb")]
	[JsonInclude]
	public string? Thumb { get; private set; }
	[JsonPropertyName("original_filename")]
	[JsonInclude]
	public string? OriginalFilename { get; private set; }
	[JsonPropertyName("reply_count")]
	[JsonInclude]
	public int ReplyCount { get; private set; }
	[JsonPropertyName("soudane_count")]
	[JsonInclude]
	public int SoudaneCount { get; private set; }
	[JsonPropertyName("created_at")]
	[JsonInclude]
	public string CreatedAt { get; private set; } = "";
	[JsonPropertyName("bumped_at")]
	[JsonInclude]
	public string BumpedAt { get; private set; } = "";
	[JsonPropertyName("show_id")]
	[JsonInclude]
	public bool ShowId { get; private set; }
	[JsonPropertyName("poster_id")]
	[JsonInclude]
	public string? PosterId { get; private set; }
}
#endregion


class NijiuraChanToken : JsonObject {
	[JsonPropertyName("token")]
	[JsonInclude]
	public string Token { get; private set; } = "";
	[JsonPropertyName("role")]

	[JsonInclude]
	public string Role { get; private set; } = "";
}

class NijiuraChanState : JsonObject {
	[JsonPropertyName("replyCount")]
	[JsonInclude]
	public int ReplyCount { get; private set; }

	[JsonPropertyName("archivedAt")]
	[JsonInclude]
	public string? ArchivedAt { get; private set; }

	[JsonPropertyName("expiresAt")]
	[JsonInclude]
	public string ExpiresAt { get; private set; } = "";

	[JsonPropertyName("isPermanent")]
	[JsonInclude]
	public bool IsPermanent { get; private set; }

	[JsonPropertyName("isSage")]
	[JsonInclude]
	public bool IsSage { get; private set; }

	[JsonPropertyName("forceDisplayId")]
	[JsonInclude]
	public bool ForceDisplayId { get; private set; }

	[JsonPropertyName("tags")]
	[JsonInclude]
	public IEnumerable<NijiuraChanTag> Tags { get; private set; } = Array.Empty<NijiuraChanTag>();

	[JsonPropertyName("closedAt")]
	[JsonInclude]
	public string? ClosedAt { get; private set; }

	[JsonPropertyName("allowImageReplies")]
	[JsonInclude]
	public bool AllowImageReplies { get; private set; }

	[JsonPropertyName("censorshipNotices")]
	[JsonInclude]
	public IEnumerable<NijiuraChanCensorshipNotice> CensorshipNotices { get; private set; } = Array.Empty<NijiuraChanCensorshipNotice>();

	[JsonPropertyName("postStates")]
	[JsonInclude]
	public IEnumerable<NijiuraChanPostState> PostStates { get; private set; } = Array.Empty<NijiuraChanPostState>();

	[JsonPropertyName("newPosts")]
	[JsonInclude]
	public IEnumerable<NijiuraChanPost> NewPosts { get; private set; } = Array.Empty<NijiuraChanPost>();
}


class NijiuraChanPost : JsonObject {
	[JsonPropertyName("id")]
	[JsonInclude]
	public string Id { get; private set; } = "";

	[JsonPropertyName("seq")]
	[JsonInclude]
	public int Sequence { get; private set; }

	[JsonPropertyName("boardNo")]
	[JsonInclude]
	public int BoardNo { get; private set; }


	[JsonPropertyName("body")]
	[JsonInclude]
	public string Body { get; private set; } = "";

	[JsonPropertyName("createdAt")]
	[JsonInclude]
	public string CreatedAt { get; private set; } = "";

	[JsonPropertyName("displayId")]
	[JsonInclude]
	public string? DisplayId { get; private set; }

	[JsonPropertyName("displayIdSource")]
	[JsonInclude]
	public string? DisplayIdSource { get; private set; }

	[JsonPropertyName("attachment")]
	[JsonInclude]
	public NijiuraChanAttachment? Attachment { get; private set; }

	[JsonPropertyName("attachments")]
	[JsonInclude]
	public IEnumerable<NijiuraChanAttachment> Attachments { get; private set; } = Array.Empty<NijiuraChanAttachment>();

	[JsonPropertyName("sage")]
	[JsonInclude]
	public bool Sage { get; private set; }
}

class NijiuraChanPostState {
	// statusプロパティのとりえる値がよくわからないので一旦定数定義しておく
	public static readonly string StatePublic = "public";

	[JsonPropertyName("seq")]
	[JsonInclude]
	public int Sequence { get; private set; }

	[JsonPropertyName("status")]
	[JsonInclude]
	public string Status { get; private set; } = "";

	[JsonPropertyName("reactions")]
	[JsonInclude]
	public NijiuraChanPostReaction Reaction { get; private set; } = NijiuraChanPostReaction.Default();
}

class NijiuraChanPostReaction : JsonObject {
	// そうだねの種類を増やしたい雰囲気を感じるのでint?で宣言するべきかもしれない
	[JsonPropertyName("up")]
	[JsonInclude]
	public int Up { get; private set; }

	internal static NijiuraChanPostReaction Default() => new() {
		Up = 0
	};
}

class NijiuraChanAttachment : JsonObject {
	// kindプロパティのとりえる値がよくわからないので一旦定数定義しておく
	public static readonly string KindImage = "image";

	// サポートするMIMEを定義
	public static readonly string MimeJpeg = "image/jpeg";
	public static readonly string MimePng = "image/png";
	public static readonly string MimeWebP = "image/webp";
	public static readonly string MimeMp4 = "";
	public static readonly string MimeWebM = "";


	[JsonPropertyName("id")]
	[JsonInclude]
	public string id { get; private set; } = "";

	[JsonPropertyName("kind")]
	[JsonInclude]
	public string Kind { get; private set; } = "";

	[JsonPropertyName("mime")]
	[JsonInclude]
	public string Mime { get; private set; } = "";

	[JsonPropertyName("width")]
	[JsonInclude]
	public int Width { get; private set; }

	[JsonPropertyName("height")]
	[JsonInclude]
	public int Height { get; private set; }

	[JsonPropertyName("originalUrl")]
	[JsonInclude]
	public string OriginalUrl { get; private set; } = "";

	[JsonPropertyName("thumbnailUrl")]
	[JsonInclude]
	public string ThumbnailUrl { get; private set; } = "";

	[JsonPropertyName("ngHash")]
	[JsonInclude]
	public string NgHash { get; private set; } = "";

	[JsonPropertyName("isOekaki")]
	[JsonInclude]
	public bool IsOekaki { get; private set; }
}

class NijiuraChanTag : JsonObject {
	[JsonPropertyName("name")]
	[JsonInclude]
	public string? Name { get; private set; }

	[JsonPropertyName("kind")]
	[JsonInclude]
	public string? Kind { get; private set; }

	[JsonPropertyName("source")]
	[JsonInclude]
	public string? Source { get; private set; }
}


class NijiuraChanCensorshipNotice : JsonObject {
	[JsonPropertyName("id")]
	[JsonInclude]
	public string? id { get; private set; }
}