using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000032 RID: 50
	[HarmonyPatch(typeof(PrayerStandInteractionHandler), "Interact")]
	internal static class EverydaySermonInteractionPatch
	{
		// Token: 0x06000192 RID: 402 RVA: 0x000183BC File Offset: 0x000165BC
		[HarmonyPrefix]
		private static void PrepareSermon()
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			instance.PrepareEverydaySermon();
		}
	}
}
