using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000034 RID: 52
	[HarmonyPatch(typeof(EnvironmentData), "AddToDay")]
	internal static class EverydayZombieResurrectionPatch
	{
		// Token: 0x06000194 RID: 404 RVA: 0x000183DE File Offset: 0x000165DE
		[HarmonyPostfix]
		private static void StartPreparedZombies()
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			instance.StartPreparedZombieResurrections(false);
		}
	}
}
