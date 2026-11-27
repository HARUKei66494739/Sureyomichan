using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Haru.Kei.SureyomiChan.Models.Prism;


interface ICredentialStore {
	public ConfigCredential Credential { get; }
	public void AddUpdateEventListener(EventHandler handler);
	public void Update(string slug, NijiuraChanAppCredentialsResponse response);
}


class CredentialStore : ICredentialStore {
	private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "credential.conf");
	private readonly List<WeakReference<EventHandler>> updateEventListener = new();

	public ConfigCredential Credential {
		get {
			return field;
		}
		private set {
			field = value;
		}
	}


	public CredentialStore() {
		this.Credential = new() {
			Njiurachan = null,
		};

		try {
			if(File.Exists(FilePath)) {
				var json = File.ReadAllText(FilePath);
				if(JsonSerializer.Deserialize<ConfigCredential>(json) is { } v) {
					this.Credential = v;
				}
			}
		}
		catch(Exception ex) {
			Utils.Logger.Instance.Error(ex);
		}
	}

	public void AddUpdateEventListener(EventHandler handler) {
		updateEventListener.Add(new(handler));
	}

	public void Update(string slug, NijiuraChanAppCredentialsResponse response) {
		var update = new ConfigCredentialObj() {
			Credential = response.Credential,
			ExpiresAt = response.ExpiresAt,
			Slug = slug,
		};

		this.Credential = new() {
			Njiurachan = update,
		};

		try {
			File.WriteAllText(
				FilePath,
				this.Credential.ToString());
		}
		catch(Exception ex) {
			Utils.Logger.Instance.Error(ex);
		}


		//イベント発火
		foreach(var it in this.updateEventListener
			.Select(x => {
				if(x.TryGetTarget(out var ev)) {
					return ev;
				} else {
					return default(EventHandler?);
				}
			}).ToArray()) {

			it?.Invoke(this, EventArgs.Empty);
		}

		// nullオブジェクトの削除
		foreach(var it in this.updateEventListener
			.Select(x => {
				if(x.TryGetTarget(out var ev)) {
					return null;
				} else {
					return x;
				}
			}).Where(x => x != null)
			.ToArray()) {

			this.updateEventListener.Remove(it);
		}
	}
}