using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x0200001D RID: 29
	[HarmonyPatch]
	internal static class InstantActionsPatches
	{
		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000166 RID: 358 RVA: 0x00017554 File Offset: 0x00015754
		private static bool Enabled
		{
			get
			{
				KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
				return instance != null && instance.InstantActionsEnabled;
			}
		}

		// Token: 0x06000167 RID: 359 RVA: 0x00017568 File Offset: 0x00015768
		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlayerCraftActivity), "GetActionDamage")]
		private static void FinishManualCraftInOneAction(PlayerCraftActivity __instance, ref int __result)
		{
			if (!InstantActionsPatches.Enabled)
			{
				return;
			}
			CraftElementBase craftElementBase;
			if (__instance == null)
			{
				craftElementBase = null;
			}
			else
			{
				CraftComponent craftComponent = __instance.CraftComponent;
				craftElementBase = ((craftComponent != null) ? craftComponent.CurrentCraftElement : null);
			}
			CraftElementBase craftElementBase2 = craftElementBase;
			if (craftElementBase2 == null)
			{
				return;
			}
			int num = craftElementBase2.TotalProgressTicks - craftElementBase2.ProgressTicks;
			if (num > __result)
			{
				__result = num;
			}
		}

		// Token: 0x06000168 RID: 360 RVA: 0x000175B0 File Offset: 0x000157B0
		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlayerHPActivity), "GetActionDamage")]
		private static void FinishHpActionInOneHit(PlayerHPActivity __instance, ref int __result)
		{
			if (!InstantActionsPatches.Enabled)
			{
				return;
			}
			int? num;
			if (__instance == null)
			{
				num = null;
			}
			else
			{
				WgoData wgoData = __instance.WgoData;
				if (wgoData == null)
				{
					num = null;
				}
				else
				{
					HPComponent hpComponent = wgoData.HpComponent;
					num = ((hpComponent != null) ? new int?(hpComponent.Hp) : null);
				}
			}
			int? num2 = num;
			int valueOrDefault = num2.GetValueOrDefault();
			if (valueOrDefault > __result)
			{
				__result = valueOrDefault;
			}
		}
	}
}
