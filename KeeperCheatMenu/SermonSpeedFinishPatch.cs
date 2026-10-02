using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000022 RID: 34
	[HarmonyPatch(typeof(UIPrayWindowData), "OnPrayFinished")]
	internal static class SermonSpeedFinishPatch
	{
		// Token: 0x06000172 RID: 370 RVA: 0x00017927 File Offset: 0x00015B27
		[HarmonyPrefix]
		private static void StopSpeedBoost()
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			instance.RestoreSermonSpeed();
		}
	}
}
