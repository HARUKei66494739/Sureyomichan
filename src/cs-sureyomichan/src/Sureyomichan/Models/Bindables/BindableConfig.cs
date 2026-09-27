using Livet.Messaging.IO;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows;

namespace Haru.Kei.SureyomiChan.Models.Bindables;

class BindableConfig : System.ComponentModel.INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;

	public ReactivePropertySlim<string> SaveSubFolderName { get; } = new(initialValue: "$Board$Thread");
	public ReactivePropertySlim<bool> IsSaveAttachmentFile { get; } = new(initialValue: false);
	public ReactivePropertySlim<bool> IsSaveUpFile { get; } = new(initialValue: false);
	public ReactivePropertySlim<bool> SaveThreadNo { get; } = new(initialValue: false);
	public BindableToggleWithTextConfig ChangeThreadNoTxt { get; } = new(
		false,
		"スレッドなし",
		"変更後txt");
	public ReactivePropertySlim<string> TegakiRotateTime { get; } = new(initialValue: "10");
	public ReactivePropertySlim<bool> TegakiRemove { get; } = new(initialValue: false);


	public BindableYomiageConfig YomiageStarted { get; } = new(
		Config.DefaultConfig.YomiageStarted.Method,
		Config.DefaultConfig.YomiageStarted.File,
		Config.DefaultConfig.YomiageStarted.Text);
	public BindableYomiageConfig YomiageOld { get; } = new(
		Config.DefaultConfig.YomiageOld.Method,
		Config.DefaultConfig.YomiageOld.File,
		Config.DefaultConfig.YomiageOld.Text);
	public ReactivePropertySlim<string> YomiageOldTime { get; } = new(initialValue: "5");
	public BindableYomiageConfig YomiageDie { get; } = new(
		Config.DefaultConfig.YomiageDie.Method,
		Config.DefaultConfig.YomiageDie.File,
		Config.DefaultConfig.YomiageDie.Text);
	public BindableYomiageConfig YomiageSaveTegaki { get; } = new(
		Config.DefaultConfig.YomiageSaveTegaki.Method,
		Config.DefaultConfig.YomiageSaveTegaki.File,
		Config.DefaultConfig.YomiageSaveTegaki.Text);

	public ReactivePropertySlim<string> AppendSpecialTag { get; } = new(initialValue: "");
	public ReactivePropertySlim<bool> NonReadId { get; } = new(initialValue: true);
	public ReactivePropertySlim<string> BouyomiChanPort { get; } = new(initialValue: "50080");
	public ReactivePropertySlim<string> FutabaPasswd { get; } = new(initialValue: "");
	public ReactivePropertySlim<string> NijiuraChanPasswd { get; } = new(initialValue: "");


	public ReactivePropertySlim<string> PathDwonload { get; } = new(initialValue: Config.DefaultConfig.PathDwonload);
	public ReactivePropertySlim<string> PathLegacyTegakiSave { get; } = new(initialValue: Config.DefaultConfig.PathLegacyTegakiSave2);
	public ReactiveProperty<bool> OpenWebViewDevTool { get; } = new(initialValue: false);
	
	public ReactiveCommandSlim<FolderSelectionMessage> DownloadFolderSelectCommand { get; } = new();
	public ReactiveCommandSlim<FolderSelectionMessage> TegakiSaveFolderSelectCommand { get; } = new();

	// 20260130
	public ReactivePropertySlim<bool> IsEnabledConvertObs { get; } = new(initialValue: false);
	public ReactivePropertySlim<bool> IsEnabledLogSave { get; } = new(initialValue: false);
	public BindableYomiageConfig YomiageSoudane { get; } = new(
		Config.DefaultConfig.YomiageSoudane.Method,
		Config.DefaultConfig.YomiageSoudane.File,
		Config.DefaultConfig.YomiageSoudane.Text);
	public ReactivePropertySlim<bool> IsEnabledAutoDleteIdRes { get; } = new(initialValue: false);
	public ReactivePropertySlim<BindableSoundDevice> UsedSoundDevice { get; } = new(initialValue: BindableSoundDevice.Empty);
	public ReactiveCollection<BindableSoundDevice> SoundDevices { get; } = new();

	// 20260926
	public BindableYomiageConfig YomiagePoll { get; } = new(
		Config.DefaultConfig.YomiagePoll.Method,
		Config.DefaultConfig.YomiagePoll.File,
		Config.DefaultConfig.YomiagePoll.Text);


	public BindableConfig(Config config) {
		static IEnumerable<BindableSoundDevice> getSoundDevices() {
			using var @enum = new NAudio.CoreAudioApi.MMDeviceEnumerator();
			return @enum.EnumerateAudioEndPoints(
					NAudio.CoreAudioApi.DataFlow.Render,
					NAudio.CoreAudioApi.DeviceState.Active)
					.Select(x => new BindableSoundDevice() {
						Name = x.FriendlyName,
						Guid = x.ID,
					}).ToList()
					.AsReadOnly();
		}

		this.DownloadFolderSelectCommand.Subscribe(x => OnDownloadFolderSelected(x));
		this.TegakiSaveFolderSelectCommand.Subscribe(x => OnTegakiSaveFolderSelected(x));


		var devices = getSoundDevices();
		this.SoundDevices.Add(BindableSoundDevice.Empty);
		this.SoundDevices.AddRange(devices);

		var usedSoundDevice = devices.Where(x => x.Guid == config.UsedSoundDevice).FirstOrDefault() ?? BindableSoundDevice.Empty;

		this.SaveSubFolderName.Value = config.SaveSubFolderName;
		this.IsSaveAttachmentFile.Value = config.IsEnabledAttacmentFile;
		this.IsSaveUpFile.Value = config.IsEnabledUpFile;
		this.SaveThreadNo.Value = config.SaveThreadNoEnabled;
		this.ChangeThreadNoTxt.Update(config.ChangeThreadNoTxtEnabled, config.ChangeThreadNoTxtText);
		this.TegakiRotateTime.Value = Math.Max(1, config.TegakiRotateTime / 1000).ToString();
		this.TegakiRemove.Value = config.TegakiRemoveEnabled;
		this.YomiageStarted.Update(config.YomiageStarted);
		this.YomiageOld.Update(config.YomiageOld);
		this.YomiageOldTime.Value = Math.Max(1, config.YomiageOldTime / 60 / 1000).ToString();
		this.YomiageDie.Update(config.YomiageDie);
		this.YomiageSaveTegaki.Update(config.YomiageSaveTegaki);
		this.AppendSpecialTag.Value = config.AppendSpecialTag;
		this.NonReadId.Value = config.NonReadId;
		this.BouyomiChanPort.Value = config.BouyomiChanPort.ToString();
		this.FutabaPasswd.Value = config.FutabaPasswd;
		this.NijiuraChanPasswd.Value = config.NijiuraChanPasswd;
		this.PathDwonload.Value = config.PathDwonload;
		this.PathLegacyTegakiSave.Value = config.PathLegacyTegakiSave2;
		this.OpenWebViewDevTool.Value = config.OpenWebViewDevTool;

		// 20260130
		this.IsEnabledConvertObs.Value = config.IsEnabledConvertObs;
		this.IsEnabledLogSave.Value = config.IsEnabledLogSave;
		this.YomiageSoudane.Update(config.YomiageSoudane);
		this.IsEnabledAutoDleteIdRes.Value = config.IsEnabledAutoDleteIdRes;
		this.UsedSoundDevice.Value = usedSoundDevice;

		// 20260926
		this.YomiagePoll.Update(config.YomiagePoll);
	}

	private void OnDownloadFolderSelected(FolderSelectionMessage m) {
		if(m.Response?.FirstOrDefault() is { } path) {
			this.PathDwonload.Value = path;
		}
	}

	private void OnTegakiSaveFolderSelected(FolderSelectionMessage m) {
		if(m.Response?.FirstOrDefault() is { } path) {
			this.PathLegacyTegakiSave.Value = path;
		}
	}

	public Config Save() {
		static int @int(string s, int @default) {
			try {
				return int.Parse(s);
			}
			catch(FormatException) {
				return @default;
			}
		}

		var bouyomiPort = @int(BouyomiChanPort.Value, 50080);
		var tegakiRotateTime = @int(this.TegakiRotateTime.Value, 10) * 1000;
		var yomiageOldTime = @int(this.YomiageOldTime.Value, 10) * 60 * 1000;
		var config = new Config() {
			PathDwonload = this.PathDwonload.Value,
			SaveSubFolderName = this.SaveSubFolderName.Value,
			IsEnabledAttacmentFile = this.IsSaveAttachmentFile.Value,
			IsEnabledUpFile = this.IsSaveUpFile.Value,
			SaveThreadNoEnabled = this.SaveThreadNo.Value,
			ChangeThreadNoTxtEnabled = this.ChangeThreadNoTxt.IsChecked.Value,
			ChangeThreadNoTxtText = this.ChangeThreadNoTxt.Input.Value,
			TegakiRotateTime = tegakiRotateTime,
			TegakiRemoveEnabled = this.TegakiRemove.Value,
			YomiageStarted = this.YomiageStarted.ToConfig(),
			YomiageOld = this.YomiageOld.ToConfig(),
			YomiageOldTime = yomiageOldTime,
			YomiageDie = this.YomiageDie.ToConfig(),
			YomiageSaveTegaki = this.YomiageSaveTegaki.ToConfig(),
			AppendSpecialTag = this.AppendSpecialTag.Value,
			NonReadId = this.NonReadId.Value,
			BouyomiChanPort = bouyomiPort,
			FutabaPasswd = this.FutabaPasswd.Value,
			NijiuraChanPasswd = this.NijiuraChanPasswd.Value,
			PathLegacyTegakiSave2 = this.PathLegacyTegakiSave.Value,
			OpenWebViewDevTool = this.OpenWebViewDevTool.Value,

			// 20260130
			IsEnabledConvertObs = this.IsEnabledConvertObs.Value,
			IsEnabledLogSave = this.IsEnabledLogSave.Value,
			YomiageSoudane = this.YomiageSoudane.ToConfig(),
			IsEnabledAutoDleteIdRes = this.IsEnabledAutoDleteIdRes.Value,
			UsedSoundDevice = this.UsedSoundDevice.Value.Guid,

			// 20260130
			YomiagePoll = this.YomiagePoll.ToConfig(),
		};
		try {
			File.WriteAllText(SureyomiChanEnviroment.GetStaticString(SureyomiChanStaticItem.ConfigFile), config.ToString());
		}
		catch(Exception ex) { }
		return config;
	}
}

class BindableToggleWithTextConfig(bool isChecked, string input, string helpText) : System.ComponentModel.INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;

	public ReactivePropertySlim<bool> IsChecked { get; } = new(initialValue: isChecked);
	public ReactivePropertySlim<string> Input { get; } = new(initialValue: input);
	public ReactivePropertySlim<string> HelpText { get; } = new(initialValue: helpText);

	public void Update(bool isChecked, string input) {
		this.IsChecked.Value = isChecked;
		this.Input.Value = input;
	}
}

class BindableYomiageConfig : System.ComponentModel.INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;

	public ReactivePropertySlim<int> Method { get; }
	public ReactivePropertySlim<string> File { get; }
	public ReactivePropertySlim<string> Text { get; }

	public ReactiveCommandSlim<OpeningFileSelectionMessage> SoundOpenCommand { get; } = new();

	public BindableYomiageConfig(int method, string fileName, string yomiageText) {
		this.Method = new(initialValue: method);
		this.File = new(initialValue: fileName);
		this.Text = new(initialValue: yomiageText);

		this.SoundOpenCommand.Subscribe(x => OnOpeningFileSelection(x));
	}

	private void OnOpeningFileSelection(OpeningFileSelectionMessage m) {
		if(m.Response?.FirstOrDefault() is { } file) {
			this.File.Value = file;
		}
	}

	public void Update(YomiageConfig conf) {
		this.Method.Value = conf.Method;
		this.File.Value = conf.File;
		this.Text.Value = conf.Text;
	}

	public YomiageConfig ToConfig() => new() {
		Method = Method.Value,
		File = File.Value,
		Text = Text.Value,
	};
}

class BindableSoundDevice : System.ComponentModel.INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;

	public static BindableSoundDevice Empty { get; } = new() {
		Name = "デフォルトのデバイス",
		Guid = "",
	};

	public required string Name { get; init; }
	public required string Guid { get; init; }
}