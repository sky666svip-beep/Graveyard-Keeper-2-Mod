using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000009 RID: 9
	[HarmonyPatch(typeof(CraftElementBase), "CanStartCraft")]
	internal static class FreeCraftingAvailabilityPatch
	{
		// Token: 0x06000142 RID: 322 RVA: 0x00016BEB File Offset: 0x00014DEB
		[HarmonyPostfix]
		private static void IgnoreMissingMaterials(ref CraftStatus __result)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance != null && instance.FreeCraftingEnabled && __result == CraftStatus.NotEnoughResources)
			{
				__result = CraftStatus.OK;
			}
		}
	}
}
