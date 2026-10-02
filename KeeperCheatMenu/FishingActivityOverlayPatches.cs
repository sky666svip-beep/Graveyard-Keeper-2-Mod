using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x0200001A RID: 26
	[HarmonyPatch]
	internal static class FishingActivityOverlayPatches
	{
		// Token: 0x0600015F RID: 351 RVA: 0x0001727A File Offset: 0x0001547A
		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlayerFishingComponent), "StartActivity")]
		private static void StartActivity([HarmonyArgument(0)] Wgo reservoir)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			instance.SetFishingActivityReservoir(reservoir);
		}

		// Token: 0x06000160 RID: 352 RVA: 0x0001728C File Offset: 0x0001548C
		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlayerFishingComponent), "StopActivity")]
		private static void StopActivity()
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			instance.EndFishingActivity();
		}
	}
}
