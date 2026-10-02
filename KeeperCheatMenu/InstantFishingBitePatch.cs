using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000015 RID: 21
	[HarmonyPatch(typeof(FishingMiniGame), "StartNewGame")]
	internal static class InstantFishingBitePatch
	{
		// Token: 0x06000153 RID: 339 RVA: 0x0001710C File Offset: 0x0001530C
		[HarmonyPrefix]
		private static void RemoveWait([HarmonyArgument(0)] FishingDef fishingDef, out float[] __state)
		{
			__state = null;
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || !instance.InstantFishingBiteEnabled || fishingDef == null)
			{
				return;
			}
			__state = fishingDef.waitTimeRange;
			fishingDef.waitTimeRange = new float[2];
		}

		// Token: 0x06000154 RID: 340 RVA: 0x0001713F File Offset: 0x0001533F
		[HarmonyPostfix]
		private static void RestoreWait([HarmonyArgument(0)] FishingDef fishingDef, float[] __state)
		{
			if (fishingDef != null && __state != null)
			{
				fishingDef.waitTimeRange = __state;
			}
		}

		// Token: 0x06000155 RID: 341 RVA: 0x0001714E File Offset: 0x0001534E
		[HarmonyFinalizer]
		private static Exception RestoreWaitAfterFailure([HarmonyArgument(0)] FishingDef fishingDef, float[] __state, Exception __exception)
		{
			if (fishingDef != null && __state != null)
			{
				fishingDef.waitTimeRange = __state;
			}
			return __exception;
		}
	}
}
