using System;
using System.Reflection;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000016 RID: 22
	[HarmonyPatch(typeof(FishingMiniGame), "UpdateStage")]
	internal static class AutoFishingHookPatch
	{
		// Token: 0x06000156 RID: 342 RVA: 0x0001715E File Offset: 0x0001535E
		[HarmonyPrefix]
		private static void HookBitingFish(FishingMiniGame __instance, [HarmonyArgument(0)] FishingMiniGame.Stage stage)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance != null && instance.AutoReelFishingEnabled && stage == (FishingMiniGame.Stage)2)
			{
				FieldInfo playerPullingField = AutoFishingHookPatch.PlayerPullingField;
				if (playerPullingField == null)
				{
					return;
				}
				playerPullingField.SetValue(__instance, true);
			}
		}

		// Token: 0x0400010E RID: 270
		private static readonly FieldInfo PlayerPullingField = AccessTools.Field(typeof(FishingMiniGame), "isPlayerPulling");
	}
}
