using System;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x0200002B RID: 43
	internal sealed class SavedPositionTeleportData : TeleportDataBase
	{
		// Token: 0x06000183 RID: 387 RVA: 0x00018000 File Offset: 0x00016200
		public SavedPositionTeleportData(SavedLocation location)
			: base(string.Empty, string.Empty, null, false, 0f)
		{
			this._location = location;
		}

		// Token: 0x06000184 RID: 388 RVA: 0x00018020 File Offset: 0x00016220
		public override string GetDestinationId()
		{
			SavedLocation location = this._location;
			return ((location != null) ? location.name : null) ?? "saved_location";
		}

		// Token: 0x06000185 RID: 389 RVA: 0x0001803D File Offset: 0x0001623D
		public override GameSceneData GetDestinationSceneData()
		{
			if (this._location != null && MainGame.WorldData != null)
			{
				return MainGame.WorldData.GetGameSceneDataById(this._location.sceneId);
			}
			return null;
		}

		// Token: 0x06000186 RID: 390 RVA: 0x00018065 File Offset: 0x00016265
		public override Vector3 GetPosition()
		{
			SavedLocation location = this._location;
			if (location == null)
			{
				return Vector3.zero;
			}
			return location.Position;
		}

		// Token: 0x06000187 RID: 391 RVA: 0x0001807C File Offset: 0x0001627C
		public override bool CanTeleport(out string error)
		{
			if (this._location == null)
			{
				error = "The saved location is invalid.";
				return false;
			}
			if (this.GetDestinationSceneData() == null)
			{
				error = "The saved scene is not available in this save.";
				return false;
			}
			error = string.Empty;
			return true;
		}

		// Token: 0x04000127 RID: 295
		private readonly SavedLocation _location;
	}
}
