using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Haru.Kei.SureyomiChan.Core;

class Yomiage(BouyomiChan bouyomi, IConfigProxy config) {
	private readonly HashSet<string> yomiageState = new();

	public void SaveImage() {
		this.DoYomiage(config.Get().YomiageSaveTegaki);
	}

	public void DoYomiageOnce(Models.YomiageConfig config, string key) {
		if(!this.yomiageState.TryGetValue(key, out var _)) {
			this.yomiageState.Add(key);
			this.DoYomiage(config);
		}
	}

	public void DoYomiage(Models.YomiageConfig c) {
		void file() {
			MMDevice device() {
				using var @enmu = new MMDeviceEnumerator();
				MMDevice? device = null;
				var id = config.Get().UsedSoundDevice;
				if(!string.IsNullOrEmpty(id)) {
					try {
						device = @enmu.GetDevice(id);
					}
					catch(System.Runtime.InteropServices.COMException ex) {
						Utils.Logger.Instance.Error("再生デバイスが見つかりません");
						Utils.Logger.Instance.Error(ex);
					}
				}

				return device switch {
					{ } => device,
					_ => @enmu.GetDefaultAudioEndpoint(
						DataFlow.Render,
						Role.Console)
				};
			}

			var file = Path.IsPathFullyQualified(c.File) switch {
				true => c.File,
				_ => Path.Combine(AppContext.BaseDirectory, "assets", "sound", c.File)
			};
			Task.Run(() => {
				var reader = new AudioFileReader(file);

				var waveOut = new WasapiOut(
					device: device(),
					shareMode: AudioClientShareMode.Shared,
					useEventSync: true,
					latency: 200);
				waveOut.Init(reader);
				waveOut.Play();

				while(waveOut.PlaybackState == PlaybackState.Playing) ;
			});
		}

		switch(c.Method) {
		case Models.YomiageConfig.YomiageMethodOff:
			break;
		case Models.YomiageConfig.YomiageMethodFile:
			file();
			break;
		case Models.YomiageConfig.YomiageMethodText:
			this.EnqueueSpeak(c.Text);
			break;
		}
	}

	public void EnqueueSpeak(string text) {
		bouyomi.EnqueueSpeak(text);
	}
}

