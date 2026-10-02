using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000014 RID: 20
	[HarmonyPatch]
	internal static class FishingPondRegenerationCapacityPatch
	{
		// Token: 0x06000150 RID: 336 RVA: 0x00016FC9 File Offset: 0x000151C9
		private static MethodBase TargetMethod()
		{
			Type closure = FishingPondRegenerationCapacityPatch.ClosureType;
			if (closure == null)
			{
				// 目标闭包找不到时返回占位方法：Harmony 的 TargetMethod 返回 null 会抛异常，
				// 那会中断整个插件的加载（菜单都打不开）。宁可这个补丁失效，也不能拖垮插件。
				return AccessTools.Method(typeof(FishingPondRegenerationCapacityPatch), "UnusedPatchPlaceholder", null, null);
			}
			return AccessTools.Method(closure, "<ReinitBalanceRelatedStuff>b__2", null, null);
		}

		private static void UnusedPatchPlaceholder()
		{
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00016FEC File Offset: 0x000151EC
		[HarmonyPrefix]
		private static bool RestoreTowardMultipliedCapacity(object __instance)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (((instance != null) ? instance.FishPondStockMultiplier : 1f) <= 1f)
			{
				return true;
			}
			FieldInfo wgoField = FishingPondRegenerationCapacityPatch.WgoField;
			Wgo wgo = ((wgoField != null) ? wgoField.GetValue(__instance) : null) as Wgo;
			FieldInfo fishingDefField = FishingPondRegenerationCapacityPatch.FishingDefField;
			FishingDef fishingDef = ((fishingDefField != null) ? fishingDefField.GetValue(__instance) : null) as FishingDef;
			WgoData wgoData = ((wgo != null) ? wgo.Data : null);
			if (wgoData == null || fishingDef == null || string.IsNullOrEmpty(fishingDef.fishId))
			{
				return true;
			}
			if (wgoData.GetGameResInt(fishingDef.fishId) < FishingPondStockHelper.ConfiguredCapacity(fishingDef))
			{
				wgoData.AddGameRes(fishingDef.fishId, 1);
			}
			return false;
		}

		// Token: 0x0400010B RID: 267
		private static readonly Type ClosureType = typeof(Wgo).GetNestedTypes(BindingFlags.NonPublic)
			.FirstOrDefault<Type>((Type type) => type.GetMethod("<ReinitBalanceRelatedStuff>b__2", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null);

		// Token: 0x0400010C RID: 268
		private static readonly FieldInfo WgoField = ((FishingPondRegenerationCapacityPatch.ClosureType == null) ? null : AccessTools.Field(FishingPondRegenerationCapacityPatch.ClosureType, "<>4__this"));

		// Token: 0x0400010D RID: 269
		private static readonly FieldInfo FishingDefField = ((FishingPondRegenerationCapacityPatch.ClosureType == null) ? null : AccessTools.Field(FishingPondRegenerationCapacityPatch.ClosureType, "fishingDef"));
	}
}
