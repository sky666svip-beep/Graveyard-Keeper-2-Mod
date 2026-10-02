using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// 补齐上游 FreeCrafting* / FreeBuild* 没有覆盖到的两个入口。
	//
	// 上游的缺口：
	//   1) FreeCraftingAvailabilityPatch 只 patch 了 CraftElementBase.CanStartCraft，
	//      但它是 virtual，实际调用的是 CraftElement / ConveyorCraftElement 的 override，
	//      所以“材料不足”仍然拦住制作（只是不扣材料而已）。
	//   2) 建造的可用性检查是 WgoBuildPointer.SetTarget 里的 canTakeResources 委托，
	//      内部调用 MultiInventory.HasItemsById；上游只在 UpdateSelectionCellsState 期间
	//      临时把该委托换成 () => true，放置等其它路径仍会真的检查材料。
	//
	// 这里把“可用性检查”这一层整体放宽，开关沿用上游已有的配置项。
	internal static class FreeCraftingAvailabilityState
	{
		private static readonly ManualLogSource Log = Logger.CreateLogSource("Keeper Cheat Menu");

		internal static bool Enabled
		{
			get
			{
				KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
				return instance != null && (instance.FreeCraftingEnabled || instance.FreeBuildingCostsEnabled);
			}
		}

		internal static void WarnMissing(string target)
		{
			FreeCraftingAvailabilityState.Log.LogWarning("Free-crafting patch target not found: " + target);
		}

		internal static void Warn(string message, Exception ex)
		{
			FreeCraftingAvailabilityState.Log.LogWarning(message + ": " + ex.Message);
		}
	}

	// 制作：CanStartCraft 是 virtual，必须逐个 patch override，否则虚分派会绕过补丁。
	[HarmonyPatch]
	internal static class FreeCraftingOverrideAvailabilityPatch
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			Type[] types = new Type[]
			{
				typeof(CraftElement),
				typeof(ConveyorCraftElement)
			};
			foreach (Type type in types)
			{
				MethodBase method = AccessTools.Method(type, "CanStartCraft", null, null);
				if (method != null)
				{
					yield return method;
				}
				else
				{
					FreeCraftingAvailabilityState.WarnMissing(type.Name + ".CanStartCraft");
				}
			}
		}

		[HarmonyPostfix]
		private static void IgnoreMissingMaterials(ref CraftStatus __result)
		{
			try
			{
				if (FreeCraftingAvailabilityState.Enabled && __result == CraftStatus.NotEnoughResources)
				{
					__result = CraftStatus.OK;
				}
			}
			catch (Exception ex)
			{
				FreeCraftingAvailabilityState.Warn("Free-crafting override patch failed", ex);
			}
		}
	}

	// 建造 + 制作共用：材料检查的公共入口。
	[HarmonyPatch]
	internal static class FreeCraftingInventoryCheckPatch
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			Type type = typeof(MultiInventory);
			MethodBase twoArg = AccessTools.Method(type, "HasItemsById", new Type[]
			{
				typeof(List<NeedItemData>),
				typeof(WgoData)
			}, null);
			if (twoArg != null)
			{
				yield return twoArg;
			}
			else
			{
				FreeCraftingAvailabilityState.WarnMissing("MultiInventory.HasItemsById(needItems, wgoData)");
			}
			MethodBase threeArg = AccessTools.Method(type, "HasItemsById", new Type[]
			{
				typeof(List<NeedItemData>),
				typeof(int),
				typeof(WgoData)
			}, null);
			if (threeArg != null)
			{
				yield return threeArg;
			}
			else
			{
				FreeCraftingAvailabilityState.WarnMissing("MultiInventory.HasItemsById(needItems, multiplicator, wgoData)");
			}
		}

		[HarmonyPostfix]
		private static void AllowMissingItems(ref bool __result)
		{
			try
			{
				if (FreeCraftingAvailabilityState.Enabled && !__result)
				{
					__result = true;
				}
			}
			catch (Exception ex)
			{
				FreeCraftingAvailabilityState.Warn("Free-crafting inventory check patch failed", ex);
			}
		}
	}

	// 燃料类制作的 UI 可用性判断走 CraftDefExtensions 的静态扩展方法，不经 CanStartCraft。
	[HarmonyPatch]
	internal static class FreeCraftingFuelUiPatch
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			Type type = typeof(CraftDefExtensions);
			string[] names = new string[]
			{
				"CanActuallyStartCraft",
				"CanActuallyStartInstantCraft",
				"CanActuallyStartCraftWithNeeds"
			};
			foreach (string name in names)
			{
				MethodBase method = AccessTools.Method(type, name, null, null);
				if (method != null)
				{
					yield return method;
				}
				else
				{
					FreeCraftingAvailabilityState.WarnMissing("CraftDefExtensions." + name);
				}
			}
		}

		[HarmonyPostfix]
		private static void AllowFuelCraftWithoutMaterials(ref bool __result)
		{
			try
			{
				if (FreeCraftingAvailabilityState.Enabled && !__result)
				{
					__result = true;
				}
			}
			catch (Exception ex)
			{
				FreeCraftingAvailabilityState.Warn("Free-crafting fuel UI patch failed", ex);
			}
		}
	}
}
