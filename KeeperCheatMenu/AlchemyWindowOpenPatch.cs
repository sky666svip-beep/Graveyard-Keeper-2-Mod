using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000006 RID: 6
	[HarmonyPatch(typeof(UIAlchemyWindow), "Open")]
	internal static class AlchemyWindowOpenPatch
	{
		// Token: 0x0600013E RID: 318 RVA: 0x00016B5B File Offset: 0x00014D5B
		[HarmonyPostfix]
		private static void OpenCompanion(UIAlchemyWindow __instance, UIAlchemyWindowData __0)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			instance.OpenAlchemyFolioCompanion(__instance, __0);
		}
	}
}
