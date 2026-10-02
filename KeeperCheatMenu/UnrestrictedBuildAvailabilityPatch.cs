using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x0200001C RID: 28
	[HarmonyPatch(typeof(WgoBuildPointer), "UpdateSelectionCellsState")]
	internal static class UnrestrictedBuildAvailabilityPatch
	{
		// Token: 0x06000163 RID: 355 RVA: 0x00017380 File Offset: 0x00015580
		[HarmonyPrefix]
		private static void TemporarilyAllowFreeBuilding(ref Func<bool> __state, WgoBuildPointer __instance)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || !instance.FreeBuildingCostsEnabled || __instance == null)
			{
				return;
			}
			FieldInfo canTakeResourcesField = UnrestrictedBuildAvailabilityPatch.CanTakeResourcesField;
			__state = ((canTakeResourcesField != null) ? canTakeResourcesField.GetValue(__instance) : null) as Func<bool>;
			FieldInfo canTakeResourcesField2 = UnrestrictedBuildAvailabilityPatch.CanTakeResourcesField;
			if (canTakeResourcesField2 == null)
			{
				return;
			}
			canTakeResourcesField2.SetValue(__instance, new Func<bool>(() => true));
		}

		// Token: 0x06000164 RID: 356 RVA: 0x000173F8 File Offset: 0x000155F8
		[HarmonyPostfix]
		private static void IgnorePlacementRestrictions(WgoBuildPointer __instance, Func<bool> __state)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || __instance == null)
			{
				return;
			}
			if (instance.FreeBuildingCostsEnabled)
			{
				FieldInfo canTakeResourcesField = UnrestrictedBuildAvailabilityPatch.CanTakeResourcesField;
				if (canTakeResourcesField != null)
				{
					canTakeResourcesField.SetValue(__instance, __state);
				}
			}
			if (!instance.UnrestrictedBuildEnabled)
			{
				return;
			}
			FieldInfo canTakeResourcesField2 = UnrestrictedBuildAvailabilityPatch.CanTakeResourcesField;
			Func<bool> func = ((canTakeResourcesField2 != null) ? canTakeResourcesField2.GetValue(__instance) : null) as Func<bool>;
			bool flag = instance.FreeBuildingCostsEnabled || func == null || func();
			FieldInfo shownAsActiveField = UnrestrictedBuildAvailabilityPatch.ShownAsActiveField;
			if (shownAsActiveField != null)
			{
				shownAsActiveField.SetValue(__instance, flag);
			}
			FieldInfo cellsField = UnrestrictedBuildAvailabilityPatch.CellsField;
			List<BuildSelectionCell> list = ((cellsField != null) ? cellsField.GetValue(__instance) : null) as List<BuildSelectionCell>;
			if (list != null)
			{
				foreach (BuildSelectionCell buildSelectionCell in list)
				{
					if (buildSelectionCell != null && !(buildSelectionCell is BuffCell))
					{
						buildSelectionCell.IsAvailableForBuild = flag;
					}
				}
			}
		}

		// Token: 0x04000112 RID: 274
		private static readonly FieldInfo ShownAsActiveField = AccessTools.Field(typeof(BuildPointerObject), "shownAsActive");

		// Token: 0x04000113 RID: 275
		private static readonly FieldInfo CellsField = AccessTools.Field(typeof(BuildPointerObject), "cells");

		// Token: 0x04000114 RID: 276
		private static readonly FieldInfo CanTakeResourcesField = AccessTools.Field(typeof(WgoBuildPointer), "canTakeResources");
	}
}
