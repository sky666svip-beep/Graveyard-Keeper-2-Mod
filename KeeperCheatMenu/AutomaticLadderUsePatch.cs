using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x0200001E RID: 30
	[HarmonyPatch(typeof(LadderInteractionHandler), "OnInteractionTargetEnter")]
	internal static class AutomaticLadderUsePatch
	{
		// Token: 0x06000169 RID: 361 RVA: 0x00017617 File Offset: 0x00015817
		[HarmonyPostfix]
		private static void UseWithoutButton(LadderInteractionHandler __instance, PlayerController __0)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			instance.QueueAutomaticLadderUse(__instance, __0);
		}
	}
}
