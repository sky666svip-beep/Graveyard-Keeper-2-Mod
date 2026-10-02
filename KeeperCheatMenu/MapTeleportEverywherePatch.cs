using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000036 RID: 54
	[HarmonyPatch(typeof(MapPageWidgetData), "set_MilestonesInteractable")]
	internal static class MapTeleportEverywherePatch
	{
		// Token: 0x06000196 RID: 406 RVA: 0x00018402 File Offset: 0x00016602
		[HarmonyPrefix]
		private static void ForceMapTeleport(ref bool value)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance != null && instance.MapTeleportEverywhereEnabled)
			{
				value = true;
			}
		}
	}
}
