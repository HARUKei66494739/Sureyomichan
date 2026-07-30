using System;
using System.Collections.Generic;
using System.Text;

namespace Haru.Kei.SureyomiChan.Models;

interface IMigration<T> {
	public T Migrate();
}

interface IAttachmentData {
	public string AttachmentImage { get; }
}

internal interface IImageStore {
	public byte[]? Get(string board, Helpers.ThreadId threadId, string imageName);
	public void Insert(string board, Helpers.ThreadId threadId, string imageName, byte[] imageBytes);
	public void Remove(string board, Helpers.ThreadId threadId);
}
