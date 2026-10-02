using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000021 RID: 33
	[HarmonyPatch(typeof(UIPrayWindowData), "StartCraft")]
	internal static class SermonSpeedStartPatch
	{
		// Token: 0x06000171 RID: 369 RVA: 0x00017916 File Offset: 0x00015B16
		[HarmonyPrefix]
		private static void StartSpeedBoost()
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			instance.ApplySermonSpeed();
		}
	}
}
