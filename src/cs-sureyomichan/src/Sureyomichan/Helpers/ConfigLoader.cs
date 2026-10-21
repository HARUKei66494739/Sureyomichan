using Haru.Kei.SureyomiChan.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;

namespace Haru.Kei.SureyomiChan.Helpers;

class ConfigLoader {
	private static Dictionary<int, Type> migrationTable = new() {
		[Models.Compat.Config20251229.CurrentVersion] = typeof(Models.Compat.Config20251229),
		[Models.Compat.Config20260130.CurrentVersion] = typeof(Models.Compat.Config20260130),
	};

	public Models.Config? Load() {
		// 動けばいいやの実装

		var config = default(Config?);
		try {
			var configFile = SureyomiChanEnviroment.GetStaticString(SureyomiChanStaticItem.ConfigFile);

			if(!File.Exists(configFile)) {
				Utils.Logger.Instance.Info("設定ファイルが見つかりません");
				return null;
			}

			Utils.Logger.Instance.Info("設定ファイルを読み込みます");
			var json = File.ReadAllText(configFile);
			var cm = JsonSerializer.Deserialize<ConfigObject>(json);
			if(cm is null) {
				Utils.Logger.Instance.Info("設定ファイルのフォーマットが不正です");
				Utils.Logger.Instance.Info(json);
				return null;
			}
			
			if(cm.Version < Config.CurrentVersion) {
				Utils.Logger.Instance.Info($"古い設定ファイルフォーマットです => {cm.Version}");
				if(migrationTable.TryGetValue(cm.Version, out var compat)) {
					if(JsonSerializer.Deserialize(json, compat) is Models.IMigration<Models.Config> m) {
						Utils.Logger.Instance.Info("設定ファイルのマイグレーションを行います");
						config = m.Migrate();
						if(config is { }) {
							try {
								File.WriteAllText(configFile, config.ToString());
							}
							catch(Exception ex) {
								Utils.Logger.Instance.Error("マイグレーションした設定の保存に失敗しました");
								Utils.Logger.Instance.Error(ex);
							}
						}
					}
				}
			} else {
				Utils.Logger.Instance.Info("設定ファイルのディシリアライズを行います");
				config = JsonSerializer.Deserialize<Config>(json);

				if(config == null) {
					Utils.Logger.Instance.Info("設定のインスタンス化に失敗しました");
					Utils.Logger.Instance.Info(json);
				}
			}
		}
		catch(Exception ex) {
			Utils.Logger.Instance.Error(ex);
		}
		return config;
	}
}
