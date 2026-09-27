using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Haru.Kei.SureyomiChan.Models;

class ConfigObject(int version) : JsonObject {
	[JsonPropertyName("version")]
	[JsonInclude]
	public int Version { get; private set; } = version;
}

class Config : ConfigObject {
	public static readonly int CurrentVersion = 20260926;

	public static Config DefaultConfig { get; } = new() {
		SaveSubFolderName = "$Board$Thread",
		IsEnabledAttacmentFile = false,
		IsEnabledUpFile = false,
		SaveThreadNoEnabled = false,
		ChangeThreadNoTxtEnabled = false,
		ChangeThreadNoTxtText = "スレッドなし",
		TegakiRotateTime = 10 * 1000,
		TegakiRemoveEnabled = false,

		YomiageStarted = new() {
			Method = YomiageConfig.YomiageMethodText,
			File = "start.wav",
			Text = "手書き保存を開始します"
		},
		YomiageOld = new() {
			Method = YomiageConfig.YomiageMethodText,
			File = "alarm.wav",
			Text = "もうすぐスレッドが落ちます"
		},
		YomiageOldTime = 5 * 60 * 1000,
		YomiageDie = new() {
			Method = YomiageConfig.YomiageMethodText,
			File = "activealert.wav",
			Text = "スレッドが落ちましたライブを停止します"
		},
		YomiageSaveTegaki = new() {
			Method = YomiageConfig.YomiageMethodText,
			File = "save.wav",
			Text = "手書きを保存しました"
		},
		AppendSpecialTag = "",
		NonReadId = true,
		BouyomiChanPort = 50080,
		FutabaPasswd = "",
		NijiuraChanPasswd = "",
		PathDwonload = "",
		PathLegacyTegakiSave2 = "",
		OpenWebViewDevTool = false,

		// 20260130
		IsEnabledConvertObs = false,
		IsEnabledLogSave = false,
		YomiageSoudane = new() {
			Method = YomiageConfig.YomiageMethodText,
			File = "soudane.wav",
			Text = "そうだねかけるいち"
		},
		IsEnabledAutoDleteIdRes = false,
		UsedSoundDevice = "",

		// 20260926
		YomiagePoll = new() {
			Method = YomiageConfig.YomiageMethodText,
			File = "",
			Text = "アンケートがきてますよ"
		},
	};

	[JsonPropertyName("save-root-path")]
	[JsonInclude]
	public required string PathDwonload { get; init; }

	[JsonPropertyName("save-folder-name")]
	[JsonInclude]
	public required string SaveSubFolderName { get; init; }
	[JsonPropertyName("save-enabled-attachment")]
	[JsonInclude]
	public required bool IsEnabledAttacmentFile { get; init; }
	[JsonPropertyName("save-enabled-upfile")]
	[JsonInclude]
	public required bool IsEnabledUpFile { get; init; }

	[JsonPropertyName("save-enabled-threadno-txt")]
	[JsonInclude]
	public required bool SaveThreadNoEnabled { get; init; }
	[JsonPropertyName("save-enabled-die-threadno-txt")]
	[JsonInclude]
	public required bool ChangeThreadNoTxtEnabled { get; init; }
	[JsonPropertyName("save-die-string")]
	[JsonInclude]
	public required string ChangeThreadNoTxtText { get; init; }

	[JsonPropertyName("save-tegaki-roatate-time-milisec")]
	[JsonInclude]
	public required int TegakiRotateTime { get; init; }
	[JsonPropertyName("save-enabled-tegaki-remove")]
	[JsonInclude]
	public required bool TegakiRemoveEnabled { get; init; }


	[JsonPropertyName("yomiage-start")]
	[JsonInclude]
	public required YomiageConfig YomiageStarted { get; init; }
	[JsonPropertyName("yomiage-old")]
	[JsonInclude]
	public required YomiageConfig YomiageOld { get; init; }
	[JsonPropertyName("yomiage-old-time-milisec")]
	[JsonInclude]
	public required int YomiageOldTime { get; init; }
	[JsonPropertyName("yomiage-die")]
	[JsonInclude]
	public required YomiageConfig YomiageDie { get; init; }
	[JsonPropertyName("yomiage-save")]
	[JsonInclude]
	public required YomiageConfig YomiageSaveTegaki { get; init; }
	[JsonPropertyName("yomiage-appned-special-tag")]
	[JsonInclude]
	public required string AppendSpecialTag { get; init; }
	[JsonPropertyName("yomiage-enabled-no-id-speak")]
	[JsonInclude]
	public required bool NonReadId { get; init; }
	[JsonPropertyName("yomiage-bouyomicahn-port")]
	[JsonInclude]
	public required int BouyomiChanPort { get; init; }

	[JsonPropertyName("futaba-passwd")]
	[JsonInclude]
	public required string FutabaPasswd { get; init; }

	[JsonPropertyName("nijiurachan-passwd")]
	[JsonInclude]
	public required string NijiuraChanPasswd { get; init; }


	[JsonPropertyName("tegaki_save-path")]
	[JsonInclude]
	public required string PathLegacyTegakiSave2 { get; init; }
	[JsonPropertyName("tegaki_save-enabled-dev-tools")]
	[JsonInclude]
	public required bool OpenWebViewDevTool { get; init; }


	// 20260130
	[JsonPropertyName("save-enabled-convert-obs")]
	[JsonInclude]
	public required bool IsEnabledConvertObs { get; init; }
	[JsonPropertyName("save-enabled-log-save")]
	[JsonInclude]
	public required bool IsEnabledLogSave { get; init; }
	[JsonPropertyName("nijiurachan-yomiage-soudane")]
	[JsonInclude]
	public required YomiageConfig YomiageSoudane { get; init; }
	[JsonPropertyName("other-enabled-auto-delete" /* 現在の分類では該当するカテゴリがないのでその他に置く */)]
	[JsonInclude]
	public required bool IsEnabledAutoDleteIdRes { get; init; }
	[JsonPropertyName("other-use-output-sound-device" /* 現在の分類では該当するカテゴリがないのでその他に置く */)]
	[JsonInclude]
	public required string UsedSoundDevice { get; init; }


	// 20260926
	[JsonPropertyName("nijiurachan-yomiage-poll")]
	[JsonInclude]
	public required YomiageConfig YomiagePoll { get; init; }

	[JsonIgnore]
	public string PathDwonloadValue {
		get {
			if(!string.IsNullOrEmpty(this.PathDwonload)) {
				return this.PathDwonload;
			}

			var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", false);
			if(key is { } && key.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") is string s) {
				return s; 
			}
			return AppContext.BaseDirectory;
		}
	}

	[JsonIgnore]
	public string PathLegacyTegakiSaveValue {
		get {
			return this.PathLegacyTegakiSave2 switch {
				string v when !string.IsNullOrEmpty(v) => v,
				_ => Path.Combine(
					AppContext.BaseDirectory,
					"tegaki_save"),
			};
		}
	}

	[JsonIgnore]
	public YomiageConfig YomiageMaxRes => new() {
		Method = YomiageConfig.YomiageMethodText,
		File = "",
		Text = SureyomiChanEnviroment.YomiageMaxResText,
	};

	public Config() : base(CurrentVersion) { }
}

// 読み上げ/音声/OFFの3点セットのやつ
class YomiageConfig : JsonObject {
	public const int YomiageMethodOff = 0;
	public const int YomiageMethodFile = 1;
	public const int YomiageMethodText = 2;

	[JsonPropertyName("type")]
	[JsonInclude]
	public required int Method { get; init; }
	[JsonPropertyName("file")]
	[JsonInclude]
	public required string File { get; init; }
	[JsonPropertyName("text")]
	[JsonInclude]
	public required string Text { get; init; }
}