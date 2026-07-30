using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Haru.Kei.SureyomiChan.Models;

class SureyomiChanResponse {
	public required SureyomiChanBoardId BoardId { get; init; }
	public required Helpers.ThreadId ThreadId { get; init; }


	// コンパイルを通すために一旦定義
	[Obsolete]
	public int ThreadNo {
		get {
			return this.ThreadId.IsInt switch {
				true => this.ThreadId.ThreadNo,
				_ => 0,
			};
		}
	}


	public required bool IsAlive { get; init; }
	public required bool IsMaxRes { get; init; }
	public required DateTime CurrentTime { get; init; }
	public required DateTime DieTime { get; init; }
	/// <summary>スレ文に入っているそうだね</summary>
	public required int Soudane { get; init; }
	public required IEnumerable<SureyomiChanModel> NewReplies { get; init; }
	public required ISureyomiChanFeature SupportFeature { get; init; }
}

class SureyomiChanModel(
	Helpers.ThreadId threadId,
	int resIndex,
	int no,
	DateTime postTime,
	string email,
	string body,
	SureyomiChanDeleteType deleteType,

	// 画像関係いれる
	IEnumerable<SureyomiChanImage> images,

	string? id,
	IEnumerable<Models.Token> token,
	ISureyomiChanInteraction interaction) {

	public Helpers.ThreadId ThreadId { get; } = threadId;
	public int ResIndex { get; } = resIndex;
	public int No { get; } = no;
	public DateTime PostTime { get; } = postTime;
	public string Email { get; } = email;
	public string Body { get; } = body;
	public string? Id { get; } = id;
	public bool HasId { get; } = id is not null;

	public SureyomiChanDeleteType DeleteType { get; } = deleteType;

	public IEnumerable<SureyomiChanImage> Images { get; } = [..images];
	public IEnumerable<Models.Token> Token { get; } = token;
	public ISureyomiChanInteraction Interaction { get; } = interaction;
}

class SureyomiChanImage(
	string imageFileName,
	string imageSource,
	string thumbnailSource
	) {

	public string ImageFileName { get; } = imageFileName;
	public string ImageSource { get; } = imageSource;
	public string ThumbnailSource { get; } = thumbnailSource;
}

interface ISureyomiChanFeature {
	public bool IsSupportThreadOld { get; }
	public bool IsSupportThreadDie { get; }
	public bool IsSupportInspectSoudane { get; }
}

interface ISureyomiChanInteraction {
	SureyomiChanBoardId BoardId { get; }
	bool IsSupportSendDel { get; }
	bool IsSupportDeleteRes { get; }

	Task<bool> SendDelAction();
	Task<bool> DeleteResAction();

	Task<IEnumerable<AttachmentObject>> DownloadImages();
}

enum SureyomiChanDeleteType {
	None,
	/// <summary>スレッドを立てた人によって削除されました</summary>
	DeleteFromOwner,
	/// <summary>削除依頼によって隔離されました</summary>
	DeleteFromDel,
	/// <summary>本人が削除</summary>
	SelfDelete,
}