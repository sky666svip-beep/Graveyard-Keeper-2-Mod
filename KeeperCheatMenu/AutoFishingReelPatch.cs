using System;
using System.Reflection;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000017 RID: 23
	[HarmonyPatch(typeof(FishingMiniGame), "UpdateProgress")]
	internal static class AutoFishingReelPatch
	{
		// Token: 0x06000158 RID: 344 RVA: 0x000171A8 File Offset: 0x000153A8
		[HarmonyPrefix]
		private static bool FinishReeling(FishingMiniGame __instance)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || !instance.AutoReelFishingEnabled || __instance.CurrentStage != (FishingMiniGame.Stage)3 || AutoFishingReelPatch.HandleSuccessMethod == null)
			{
				return true;
			}
			MethodInfo handleSuccessMethod = AutoFishingReelPatch.HandleSuccessMethod;
			if (handleSuccessMethod != null)
			{
				handleSuccessMethod.Invoke(__instance, null);
			}
			return false;
		}

		// Token: 0x0400010F RID: 271
		private static readonly MethodInfo HandleSuccessMethod = AccessTools.Method(typeof(FishingMiniGame), "HandleSuccess", null, null);
	}
}
