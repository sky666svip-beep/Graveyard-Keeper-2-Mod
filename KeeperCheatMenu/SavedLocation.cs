using System;
using Newtonsoft.Json;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x0200002A RID: 42
	[Serializable]
	internal sealed class SavedLocation
	{
		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000181 RID: 385 RVA: 0x00017FDF File Offset: 0x000161DF
		[JsonIgnore]
		public Vector3 Position
		{
			get
			{
				return new Vector3(this.x, this.y, this.z);
			}
		}

		// Token: 0x04000122 RID: 290
		public string name;

		// Token: 0x04000123 RID: 291
		public string sceneId;

		// Token: 0x04000124 RID: 292
		public float x;

		// Token: 0x04000125 RID: 293
		public float y;

		// Token: 0x04000126 RID: 294
		public float z;
	}
}
