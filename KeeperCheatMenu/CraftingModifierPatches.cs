using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x0200000D RID: 13
	[HarmonyPatch]
	internal static class CraftingModifierPatches
	{
		// Token: 0x06000147 RID: 327 RVA: 0x00016CA8 File Offset: 0x00014EA8
		[HarmonyPrefix]
		[HarmonyPatch(typeof(CraftComponent), "Update")]
		private static void ScaleAutomaticMachineTime(CraftComponent __instance, ref float deltaTime)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance != null && __instance != null && __instance.IsAutoCraftable)
			{
				deltaTime *= Mathf.Clamp(instance.MachineSpeedMultiplier, 0.1f, 100f);
			}
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00016CEC File Offset: 0x00014EEC
		[HarmonyPostfix]
		[HarmonyPatch(typeof(CraftElementBase), "MakeOutput")]
		private static void ScaleCraftedItemStacks(CraftElementBase __instance, List<Item> __result)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || __result == null || __instance is CraftElementSurvey || __instance is CraftElementSermon)
			{
				return;
			}
			float num = Mathf.Clamp(instance.CraftedOutputMultiplier, 0.1f, 100f);
			if (Mathf.Approximately(num, 1f))
			{
				return;
			}
			foreach (Item item in __result)
			{
				if (item != null && item.Count > 0)
				{
					item.Count = Mathf.Max(1, Mathf.RoundToInt((float)item.Count * num));
				}
			}
		}
	}
}
