using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000007 RID: 7
	[HarmonyPatch(typeof(UIAlchemyWindow), "Close")]
	internal static class AlchemyWindowClosePatch
	{
		// Token: 0x0600013F RID: 319 RVA: 0x00016B6E File Offset: 0x00014D6E
		[HarmonyPrefix]
		private static void CloseCompanion(UIAlchemyWindow __instance)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			instance.CloseAlchemyFolioCompanion(__instance);
		}
	}
}
