using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000035 RID: 53
	[HarmonyPatch(typeof(UIResurrectionWindowData), "PrepareResurrection")]
	internal static class RepeatableZombieResurrectionPatch
	{
		// Token: 0x06000195 RID: 405 RVA: 0x000183F0 File Offset: 0x000165F0
		[HarmonyPostfix]
		private static void StartPreparedZombieImmediately()
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			instance.StartPreparedZombieResurrections(true);
		}
	}
}
