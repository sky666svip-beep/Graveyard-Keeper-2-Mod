using System;
using System.Reflection;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace KeeperBodyZombieEditor
{
	public static class SkullModifierPatches
	{
		private static readonly PropertyInfo UIResurrectionWhiteSkullsProp =
			AccessTools.Property(typeof(UIResurrectionWindowData), nameof(UIResurrectionWindowData.WhiteSkulls));

		private static readonly PropertyInfo UIResurrectionRedSkullsProp =
			AccessTools.Property(typeof(UIResurrectionWindowData), nameof(UIResurrectionWindowData.RedSkulls));

		// 1. 拦截僵尸工人白骷髅读取
		[HarmonyPatch(typeof(ZombieWgoData), "get_WhiteSkulls")]
		public static class ZombieWhiteSkullsPatch
		{
			[HarmonyPrefix]
			public static bool Prefix(ZombieWgoData __instance, ref int __result)
			{
				try
				{
					if (__instance == null) return true;
					string uid = __instance.UniqueId.ToString();
					if (BodyZombieCustomData.TryGetCustomSkulls(uid, out int white, out _))
					{
						__result = white;
						return false;
					}
					if (__instance.ZombieItem != null)
					{
						string itemUid = __instance.ZombieItem.UniqueId.ToString();
						if (BodyZombieCustomData.TryGetCustomSkulls(itemUid, out white, out _))
						{
							__result = white;
							return false;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[KeeperBodyZombieEditor] Error in ZombieWhiteSkullsPatch: " + ex.Message);
				}
				return true;
			}
		}

		// 2. 拦截僵尸工人红骷髅读取
		[HarmonyPatch(typeof(ZombieWgoData), "get_RedSkulls")]
		public static class ZombieRedSkullsPatch
		{
			[HarmonyPrefix]
			public static bool Prefix(ZombieWgoData __instance, ref int __result)
			{
				try
				{
					if (__instance == null) return true;
					string uid = __instance.UniqueId.ToString();
					if (BodyZombieCustomData.TryGetCustomSkulls(uid, out _, out int red))
					{
						__result = red;
						return false;
					}
					if (__instance.ZombieItem != null)
					{
						string itemUid = __instance.ZombieItem.UniqueId.ToString();
						if (BodyZombieCustomData.TryGetCustomSkulls(itemUid, out _, out red))
						{
							__result = red;
							return false;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[KeeperBodyZombieEditor] Error in ZombieRedSkullsPatch: " + ex.Message);
				}
				return true;
			}
		}

		// 3. 拦截尸体控件数据中的白骷髅读取 (UICorpseWidgetData.WhiteSkulls)
		[HarmonyPatch(typeof(UICorpseWidgetData), "get_WhiteSkulls")]
		public static class UICorpseWidgetDataWhiteSkullsPatch
		{
			[HarmonyPrefix]
			public static bool Prefix(UICorpseWidgetData __instance, ref int __result)
			{
				try
				{
					if (__instance == null || __instance.IsEmpty) return true;
					if (__instance.Body != null)
					{
						string uid = __instance.Body.UniqueId.ToString();
						if (BodyZombieCustomData.TryGetCustomSkulls(uid, out int white, out _))
						{
							__result = white;
							return false;
						}
					}
					if (__instance.ZombieWgoData != null)
					{
						string zUid = __instance.ZombieWgoData.UniqueId.ToString();
						if (BodyZombieCustomData.TryGetCustomSkulls(zUid, out int white, out _))
						{
							__result = white;
							return false;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[KeeperBodyZombieEditor] Error in UICorpseWidgetDataWhiteSkullsPatch: " + ex.Message);
				}
				return true;
			}
		}

		// 3.1 拦截尸体控件数据中的红骷髅读取 (UICorpseWidgetData.RedSkulls)
		[HarmonyPatch(typeof(UICorpseWidgetData), "get_RedSkulls")]
		public static class UICorpseWidgetDataRedSkullsPatch
		{
			[HarmonyPrefix]
			public static bool Prefix(UICorpseWidgetData __instance, ref int __result)
			{
				try
				{
					if (__instance == null || __instance.IsEmpty) return true;
					if (__instance.Body != null)
					{
						string uid = __instance.Body.UniqueId.ToString();
						if (BodyZombieCustomData.TryGetCustomSkulls(uid, out _, out int red))
						{
							__result = red;
							return false;
						}
					}
					if (__instance.ZombieWgoData != null)
					{
						string zUid = __instance.ZombieWgoData.UniqueId.ToString();
						if (BodyZombieCustomData.TryGetCustomSkulls(zUid, out _, out int red))
						{
							__result = red;
							return false;
						}
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[KeeperBodyZombieEditor] Error in UICorpseWidgetDataRedSkullsPatch: " + ex.Message);
				}
				return true;
			}
		}

		private static readonly FieldInfo UIResurrectionBodyItemField =
			AccessTools.Field(typeof(UIResurrectionWindowData), "bodyItem");

		private static readonly FieldInfo InventoryItemField =
			AccessTools.Field(typeof(Inventory), "inventoryItem");

		// 4. 拦截复活台界面的红白骷髅展示
		[HarmonyPatch(typeof(UIResurrectionWindowData), MethodType.Constructor, new Type[] { typeof(WgoData) })]
		public static class UIResurrectionWindowDataConstructorPatch
		{
			[HarmonyPostfix]
			public static void Postfix(UIResurrectionWindowData __instance)
			{
				try
				{
					if (__instance == null || __instance.IsEmpty) return;
					Item bodyItem = UIResurrectionBodyItemField?.GetValue(__instance) as Item;
					if (bodyItem == null) return;

					string uid = bodyItem.UniqueId.ToString();
					if (BodyZombieCustomData.TryGetCustomSkulls(uid, out int white, out int red))
					{
						UIResurrectionWhiteSkullsProp?.SetValue(__instance, white);
						UIResurrectionRedSkullsProp?.SetValue(__instance, red);
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[KeeperBodyZombieEditor] Error in UIResurrectionWindowDataConstructorPatch: " + ex.Message);
				}
			}
		}

		// 5. 拦截墓地评分计算（使埋葬带有自定义骷髅的尸体时墓地星级正确生效）
		[HarmonyPatch(typeof(Inventory), nameof(Inventory.GetTotalQualityGrave))]
		public static class InventoryGetTotalQualityGravePatch
		{
			[HarmonyPostfix]
			public static void Postfix(Inventory __instance, ref float __result)
			{
				try
				{
					if (__instance == null) return;
					Item rootItem = InventoryItemField?.GetValue(__instance) as Item;
					if (rootItem == null || rootItem.Inventory == null) return;

					bool hasCustom = false;
					int customRed = 0;
					int customWhite = 0;
					float baseDecorationQuality = 0f;

					foreach (Item item in rootItem.Inventory)
					{
						if (item == null) continue;
						baseDecorationQuality += (float)item.Definition.quality;
						if (item.Definition.itemGroupIds != null && item.Definition.itemGroupIds.Contains("body"))
						{
							string uid = item.UniqueId.ToString();
							if (BodyZombieCustomData.TryGetCustomSkulls(uid, out int white, out int red))
							{
								hasCustom = true;
								customWhite = white;
								customRed = red;
							}
						}
					}

					if (hasCustom)
					{
						float total = baseDecorationQuality - (float)customRed;
						total = Mathf.Clamp(total, -999f, (float)customWhite);
						__result = total;
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[KeeperBodyZombieEditor] Error in InventoryGetTotalQualityGravePatch: " + ex.Message);
				}
			}
		}

		// 6. 解剖台窗口上下文记录（不主动弹出，仅记录活动窗口与同步已打开状态）
		[HarmonyPatch(typeof(UIAutopsyWindow), nameof(UIAutopsyWindow.Redraw))]
		public static class UIAutopsyWindowRedrawPatch
		{
			[HarmonyPostfix]
			public static void Postfix(UIAutopsyWindow __instance)
			{
				try
				{
					if (__instance != null && __instance.IsShown)
					{
						BodyZombieEditorWindow.Instance?.OnAutopsyActive(__instance);
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[KeeperBodyZombieEditor] Error in UIAutopsyWindowRedrawPatch: " + ex.Message);
				}
			}
		}

		[HarmonyPatch(typeof(UIAutopsyWindow), nameof(UIAutopsyWindow.Hide))]
		public static class UIAutopsyWindowHidePatch
		{
			[HarmonyPostfix]
			public static void Postfix()
			{
				try
				{
					BodyZombieEditorWindow.Instance?.OnAutopsyHide();
				}
				catch { }
			}
		}

		// 7. 僵尸工人窗口上下文记录（不主动弹出，仅记录活动窗口与同步已打开状态）
		[HarmonyPatch(typeof(UIZombieWorkerWindow), nameof(UIZombieWorkerWindow.Redraw))]
		public static class UIZombieWorkerWindowRedrawPatch
		{
			[HarmonyPostfix]
			public static void Postfix(UIZombieWorkerWindow __instance)
			{
				try
				{
					if (__instance != null && __instance.IsShown)
					{
						BodyZombieEditorWindow.Instance?.OnZombieWorkerActive(__instance);
					}
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[KeeperBodyZombieEditor] Error in UIZombieWorkerWindowRedrawPatch: " + ex.Message);
				}
			}
		}

		[HarmonyPatch(typeof(UIZombieWorkerWindow), nameof(UIZombieWorkerWindow.Hide))]
		public static class UIZombieWorkerWindowHidePatch
		{
			[HarmonyPostfix]
			public static void Postfix()
			{
				try
				{
					BodyZombieEditorWindow.Instance?.OnZombieWorkerHide();
				}
				catch { }
			}
		}

		// 8. 语言加载/切换拦截补丁（确保语言切换时 100% 触发界面刷新）
		[HarmonyPatch(typeof(LLBase), nameof(LLBase.LoadLanguageResource))]
		public static class LLBaseLoadLanguageResourcePatch
		{
			[HarmonyPostfix]
			public static void Postfix()
			{
				try
				{
					BodyZombieEditorWindow.Instance?.OnLanguageChanged();
				}
				catch { }
			}
		}

		[HarmonyPatch(typeof(GameSettings), nameof(GameSettings.ApplyLanguageSettings))]
		public static class GameSettingsApplyLanguageSettingsPatch
		{
			[HarmonyPostfix]
			public static void Postfix()
			{
				try
				{
					BodyZombieEditorWindow.Instance?.OnLanguageChanged();
				}
				catch { }
			}
		}
	}
}
