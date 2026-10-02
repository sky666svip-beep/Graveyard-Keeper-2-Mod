using System;
using System.Reflection;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x0200000A RID: 10
	[HarmonyPatch]
	internal static class FreeCraftingConsumptionPatch
	{
		// Token: 0x06000143 RID: 323 RVA: 0x00016C08 File Offset: 0x00014E08
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(CraftComponent), "RemoveRequirements", null, null);
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00016C20 File Offset: 0x00014E20
		[HarmonyPrefix]
		private static bool SkipMaterialConsumption(CraftElementBase craftElement)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			bool flag = instance != null && instance.FreeCraftingEnabled;
			bool flag2 = instance != null && instance.FreeResearchEnabled && craftElement is CraftElementSurvey;
			if (!flag && !flag2)
			{
				return true;
			}
			if (craftElement != null)
			{
				craftElement.IsAllRequirementsTaken = true;
			}
			return false;
		}
	}
}
