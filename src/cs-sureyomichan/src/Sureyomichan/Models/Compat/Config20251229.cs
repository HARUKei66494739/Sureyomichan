using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Haru.Kei.SureyomiChan.Models.Compat;

class Config20251229 : ConfigObject, IMigration<Config> {
	public static readonly int CurrentVersion = 20251229;

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

	public Config20251229() : base(CurrentVersion) { }

	public Config Migrate() => new() {
		SaveSubFolderName = this.SaveSubFolderName,
		IsEnabledAttacmentFile = this.IsEnabledAttacmentFile,
		IsEnabledUpFile = this.IsEnabledUpFile,
		SaveThreadNoEnabled = this.SaveThreadNoEnabled,
		ChangeThreadNoTxtEnabled = this.ChangeThreadNoTxtEnabled,
		ChangeThreadNoTxtText = this.ChangeThreadNoTxtText,
		TegakiRotateTime = this.TegakiRotateTime,
		TegakiRemoveEnabled = this.TegakiRemoveEnabled,

		YomiageStarted = this.YomiageStarted,
		YomiageOld = this.YomiageOld,
		YomiageOldTime = this.YomiageOldTime,
		YomiageDie = this.YomiageDie,
		YomiageSaveTegaki = this.YomiageSaveTegaki,
		AppendSpecialTag = this.AppendSpecialTag,
		NonReadId = this.NonReadId,
		BouyomiChanPort = this.BouyomiChanPort,
		FutabaPasswd = this.FutabaPasswd,
		NijiuraChanPasswd = this.NijiuraChanPasswd,
		PathDwonload = this.PathDwonload,
		PathLegacyTegakiSave2 = this.PathLegacyTegakiSave2,
		OpenWebViewDevTool = this.OpenWebViewDevTool,

		// 20260130
		IsEnabledConvertObs = Config.DefaultConfig.IsEnabledConvertObs,
		IsEnabledLogSave = Config.DefaultConfig.IsEnabledLogSave,
		YomiageSoudane = Config.DefaultConfig.YomiageSoudane,
		IsEnabledAutoDleteIdRes = Config.DefaultConfig.IsEnabledAutoDleteIdRes,
		UsedSoundDevice = Config.DefaultConfig.UsedSoundDevice,

		// 20260130
		YomiagePoll = Config.DefaultConfig.YomiagePoll,
	};
}

