using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000033 RID: 51
	[HarmonyPatch(typeof(UIPrayWindowData), "OnPrayFinished")]
	internal static class EverydaySermonFinishedPatch
	{
		// Token: 0x06000193 RID: 403 RVA: 0x000183CD File Offset: 0x000165CD
		[HarmonyPostfix]
		private static void RecordSermon()
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			instance.RecordEverydaySermon();
		}
	}
}
