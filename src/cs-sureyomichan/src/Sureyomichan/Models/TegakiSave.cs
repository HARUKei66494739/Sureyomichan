using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Haru.Kei.SureyomiChan.Models; 
class TegakiSaveResData : JsonObject {
	[JsonPropertyName("com")]
	[JsonInclude]
	public string Comment { get; set; } = "";
	[JsonPropertyName("del")] // del, del2, selfdel
	[JsonInclude]
	public string Del { get; set; } = "";
	[JsonPropertyName("email")]
	[JsonInclude]
	public string Email { get; set; } = "";
	[JsonPropertyName("ext")]
	[JsonInclude]
	public string FileExtension { get; set; } = "";
	/// <summary>画像ファイルがある場合は1（ファイルサイズではない）</summary>
	[JsonPropertyName("fsize")]
	[JsonInclude]
	public int FileSize { get; set; }
	/// <summary>常に0</summary>
	[JsonPropertyName("h")]
	[JsonInclude]
	public int ThunbHeight { get; set; } = 0;
	[JsonPropertyName("host")]
	[JsonInclude]
	public string Host { get; set; } = "";
	[JsonPropertyName("id")]
	[JsonInclude]
	public string Id { get; set; } = "";
	[JsonPropertyName("name")]
	[JsonInclude]
	public string Name { get; set; } = "";
	[JsonPropertyName("now")]
	[JsonInclude]
	public string Now { get; set; } = "";
	[JsonPropertyName("rsc")]
	[JsonInclude]
	public int ResCount { get; set; }
	[JsonPropertyName("src")]
	[JsonInclude]
	public string FileSource { get; set; } = "";
	[JsonPropertyName("sub")]
	[JsonInclude]
	public string Subject { get; set; } = "";
	[JsonPropertyName("thumb")]
	[JsonInclude]
	public string FileThumb { get; set; } = "";
	[JsonPropertyName("tim")]
	[JsonInclude]
	public string Time { get; set; } = "";
	/// <summary>常に0</summary>
	[JsonPropertyName("w")]
	[JsonInclude]
	public int FileWidth { get; set; } = 0;


	[JsonPropertyName("resNo")]
	[JsonInclude]
	public string ResNo { get; set; } = "";
	[JsonPropertyName("__tageki_ng")]
	[JsonInclude]
	public bool TegakiNg { get; set; } = false;
	[JsonPropertyName("__tegaki_res")]
	[JsonInclude]
	public List<TegakiSaveResData>? TegakiRes { get; set; } = null;
	// ulongの最大値はnumberに入りきらないので文字列で渡す
	[JsonPropertyName("__sureyomi_image_hash")]
	[JsonInclude]
	public string ImageHash { get; set; } = "";
	[JsonPropertyName("__sureyomi_terms")]
	[JsonInclude]
	public List<Token>? SureyomiTerms { get; set; } = null;
	[JsonPropertyName("__sureyomi_board_id")]
	[JsonInclude]
	public string SureyomiBoardId { get; set; } = "";
}

class TegakiSavePluginResult : JsonObject {
	[JsonPropertyName("isStop")]
	[JsonInclude]
	public bool IsStop { get; private set; } = false;

	[JsonPropertyName("isError")]
	[JsonInclude]
	public bool IsError { get; private set; } = false;

	[JsonPropertyName("message")]
	[JsonInclude]
	public string Message { get; private set; } = "";

	[JsonPropertyName("resultValue")]
	[JsonInclude]
	public string ResultValue { get; private set; } = "";
}