using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LazyBearTechnology;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;

namespace KeeperCheatMenu
{
	// Token: 0x02000005 RID: 5
	[BepInPlugin("Narodum.gk2.keepercheatmenu", "Keeper Cheat Menu", "0.22.2")]
	public sealed class KeeperCheatMenuPlugin : BaseUnityPlugin
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000007 RID: 7 RVA: 0x000021F1 File Offset: 0x000003F1
		internal bool HarvestAdjacentCropsEnabled
		{
			get
			{
				return this._harvestAdjacentCrops != null && this._harvestAdjacentCrops.Value;
			}
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002208 File Offset: 0x00000408
		internal void HarvestAdjacentReadyCrops(WorldZoneData zone, Rect originBounds, string originKey, string plotId, string cropId)
		{
			if (zone == null || string.IsNullOrEmpty(plotId) || string.IsNullOrEmpty(cropId))
			{
				return;
			}
			Queue<Rect> queue = new Queue<Rect>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal) { originKey };
			queue.Enqueue(originBounds);
			int num = 0;
			while (queue.Count > 0)
			{
				Rect rect = queue.Dequeue();
				Rect rect2 = new Rect(rect.xMin - 1.5f, rect.yMin - 1.5f, rect.width + 3f, rect.height + 3f);
				List<WgoData> wgoDataByRect = zone.GetWgoDataByRect(rect2);
				if (wgoDataByRect != null)
				{
					foreach (WgoData wgoData in wgoDataByRect)
					{
						if (wgoData != null)
						{
							string text = wgoData.UniqueId.ToString();
							string text2;
							if (hashSet.Add(text) && GardenBedNavigation.IsGardenPlot(wgoData) && string.Equals(wgoData.id, plotId, StringComparison.Ordinal) && GardenTabletWorldIconLogic.TryGetCropIdFromPlotWgoId(wgoData.id, out text2) && GardenTabletWorldIconLogic.CropsMatch(cropId, text2))
							{
								queue.Enqueue(GardenBedNavigation.GetWorldOccupancyRect(wgoData));
								try
								{
									AdjacentCropHarvestPatch.Suppress = true;
									AdjacentCropHarvestPatch.RunDeath(wgoData);
									num++;
								}
								finally
								{
									AdjacentCropHarvestPatch.Suppress = false;
								}
							}
						}
					}
				}
			}
			base.Logger.LogInfo(string.Format("Harvested {0} adjacent ready crop plot(s) matching '{1}'.", num, cropId));
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002398 File Offset: 0x00000598
		internal void LogCropHarvestWarning(string message, Exception ex)
		{
			base.Logger.LogWarning(message + ": " + ex.Message);
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000023B6 File Offset: 0x000005B6
		internal void LogCropHarvestTrace(string message)
		{
			base.Logger.LogInfo(message);
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000023C4 File Offset: 0x000005C4
		internal void OpenAlchemyFolioCompanion(UIAlchemyWindow alchemyWindow, UIAlchemyWindowData windowData)
		{
			if (alchemyWindow == null)
			{
				return;
			}
			this._activeAlchemyWindow = alchemyWindow;
			WgoData wgoData;
			if (windowData == null)
			{
				wgoData = null;
			}
			else
			{
				Wgo wgo = windowData.Wgo;
				wgoData = ((wgo != null) ? wgo.Data : null);
			}
			this._activeAlchemyWgoData = wgoData;
			this._companionAlchemyWindow = alchemyWindow;
			KeeperCheatMenuPlugin.CaptureDefaultPosition(alchemyWindow.transform as RectTransform, ref this._alchemyPositionCaptured, ref this._alchemyDefaultPosition);
			this.RestoreRetainedAlchemyIngredients();
			if (!this.AlchemyFolioCompanionEnabled)
			{
				this.RestoreAlchemyFolioLayout(false);
				return;
			}
			WgoData wgoData2;
			if (windowData == null)
			{
				wgoData2 = null;
			}
			else
			{
				Wgo wgo2 = windowData.Wgo;
				wgoData2 = ((wgo2 != null) ? wgo2.Data : null);
			}
			WgoData wgoData3 = wgoData2;
			if (wgoData3 == null)
			{
				base.Logger.LogWarning("Could not open the alchemy folio companion because the active alchemy table data is unavailable.");
				this.RestoreAlchemyFolioLayout(false);
				return;
			}
			try
			{
				UIAlchemyFolioWindow window = LazyUI.GetWindow<UIAlchemyFolioWindow>();
				if (window == null)
				{
					throw new InvalidOperationException("The alchemy folio window is unavailable.");
				}
				this._companionFolioWindow = window;
				KeeperCheatMenuPlugin.CaptureDefaultPosition(window.transform as RectTransform, ref this._folioPositionCaptured, ref this._folioDefaultPosition);
				UIAlchemyFolioWindowData uialchemyFolioWindowData = new UIAlchemyFolioWindowData();
				uialchemyFolioWindowData.FillFromGaveSave(wgoData3);
				Traverse.Create(window).Field("isModalWindow").SetValue(false);
				window.Open(uialchemyFolioWindowData);
				this._companionFolioOpenedByMod = true;
				this.HideCompanionFolioBackdrops();
				this.ApplyAlchemyFolioLayout();
			}
			catch (Exception ex)
			{
				base.Logger.LogWarning("Could not open the alchemy folio companion: " + ex.Message);
				this.RestoreAlchemyFolioLayout(false);
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002528 File Offset: 0x00000728
		internal void CloseAlchemyFolioCompanion(UIAlchemyWindow alchemyWindow)
		{
			if (this._companionAlchemyWindow != null && alchemyWindow != null && this._companionAlchemyWindow != alchemyWindow)
			{
				return;
			}
			this.RememberAlchemyIngredientSelection();
			this.RestoreAlchemyFolioLayout(true);
			this._activeAlchemyWindow = null;
			this._activeAlchemyWgoData = null;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002568 File Offset: 0x00000768
		private void UpdateAlchemyFolioCompanion()
		{
			if (!this._companionFolioOpenedByMod)
			{
				return;
			}
			if (this._companionAlchemyWindow == null || !this._companionAlchemyWindow.IsShown)
			{
				this.RestoreAlchemyFolioLayout(true);
				return;
			}
			if (this._companionFolioWindow == null || !this._companionFolioWindow.IsShown)
			{
				this.RestoreAlchemyFolioLayout(false);
				return;
			}
			if (!this.AlchemyFolioCompanionEnabled)
			{
				this.RestoreAlchemyFolioLayout(true);
			}
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000025D4 File Offset: 0x000007D4
		private void ApplyAlchemyFolioLayout()
		{
			UIAlchemyWindow companionAlchemyWindow = this._companionAlchemyWindow;
			RectTransform rectTransform = ((companionAlchemyWindow != null) ? companionAlchemyWindow.transform : null) as RectTransform;
			UIAlchemyFolioWindow companionFolioWindow = this._companionFolioWindow;
			RectTransform rectTransform2 = ((companionFolioWindow != null) ? companionFolioWindow.transform : null) as RectTransform;
			if (rectTransform != null && this._alchemyPositionCaptured)
			{
				rectTransform.anchoredPosition = this._alchemyDefaultPosition + new Vector2(-170f, 0f);
			}
			if (rectTransform2 != null && this._folioPositionCaptured)
			{
				rectTransform2.anchoredPosition = this._folioDefaultPosition + new Vector2(175f, 0f);
			}
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002674 File Offset: 0x00000874
		private void HideCompanionFolioBackdrops()
		{
			this.RestoreCompanionFolioBackdrops();
			if (this._companionFolioWindow == null)
			{
				return;
			}
			Canvas componentInParent = this._companionFolioWindow.GetComponentInParent<Canvas>();
			RectTransform rectTransform = ((componentInParent != null) ? (componentInParent.transform as RectTransform) : null);
			if (rectTransform == null || rectTransform.rect.width <= 0f || rectTransform.rect.height <= 0f)
			{
				return;
			}
			float num = rectTransform.rect.width * 0.8f;
			float num2 = rectTransform.rect.height * 0.8f;
			foreach (Graphic graphic in this._companionFolioWindow.GetComponentsInChildren<Graphic>(true))
			{
				RectTransform rectTransform2 = graphic.rectTransform;
				if (!(rectTransform2 == null) && rectTransform2.rect.width >= num && rectTransform2.rect.height >= num2 && graphic.color.a > 0.01f)
				{
					this._companionFolioBackdropStates[graphic] = graphic.enabled;
					graphic.enabled = false;
					base.Logger.LogInfo(string.Concat(new string[]
					{
						"Disabled companion folio backdrop: ",
						graphic.transform.name,
						" (",
						rectTransform2.rect.width.ToString(),
						"x",
						rectTransform2.rect.height.ToString(),
						")"
					}));
				}
			}
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000283C File Offset: 0x00000A3C
		private void RestoreCompanionFolioBackdrops()
		{
			foreach (KeyValuePair<Graphic, bool> keyValuePair in this._companionFolioBackdropStates)
			{
				if (keyValuePair.Key != null)
				{
					keyValuePair.Key.enabled = keyValuePair.Value;
				}
			}
			this._companionFolioBackdropStates.Clear();
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000028B8 File Offset: 0x00000AB8
		private void RestoreAlchemyFolioLayout(bool closeFolio)
		{
			UIAlchemyWindow companionAlchemyWindow = this._companionAlchemyWindow;
			RectTransform rectTransform = ((companionAlchemyWindow != null) ? companionAlchemyWindow.transform : null) as RectTransform;
			if (rectTransform != null && this._alchemyPositionCaptured)
			{
				rectTransform.anchoredPosition = this._alchemyDefaultPosition;
			}
			UIAlchemyFolioWindow companionFolioWindow = this._companionFolioWindow;
			RectTransform rectTransform2 = ((companionFolioWindow != null) ? companionFolioWindow.transform : null) as RectTransform;
			if (rectTransform2 != null && this._folioPositionCaptured)
			{
				rectTransform2.anchoredPosition = this._folioDefaultPosition;
			}
			if (closeFolio && this._companionFolioOpenedByMod && this._companionFolioWindow != null && this._companionFolioWindow.IsShown)
			{
				this._companionFolioOpenedByMod = false;
				this._companionFolioWindow.Close();
			}
			this.RestoreCompanionFolioBackdrops();
			this._companionFolioOpenedByMod = false;
			this._companionAlchemyWindow = null;
			this._companionFolioWindow = null;
			this._alchemyPositionCaptured = false;
			this._folioPositionCaptured = false;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002990 File Offset: 0x00000B90
		private static void CaptureDefaultPosition(RectTransform rect, ref bool captured, ref Vector2 position)
		{
			if ((rect == null) | captured)
			{
				return;
			}
			position = rect.anchoredPosition;
			captured = true;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x000029AE File Offset: 0x00000BAE
		private void SetAlchemyFolioCompanionEnabled(bool enabled)
		{
			this._alchemyFolioCompanion.Value = enabled;
			base.Config.Save();
			if (!enabled)
			{
				this.RestoreAlchemyFolioLayout(true);
				return;
			}
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000029D4 File Offset: 0x00000BD4
		internal bool TrySelectAlchemyIngredientFromFolio(Item item)
		{
			if (!this.AlchemyFolioCraftingEnabled || !this._companionFolioOpenedByMod || item == null || item.IsEmpty || item.Definition == null || !item.Definition.canBeUsedInAlchemy || this._activeAlchemyWindow == null || !this._activeAlchemyWindow.IsShown)
			{
				return false;
			}
			PlayerController playerController = MainGame.PlayerController;
			MultiInventory multiInventory = ((playerController != null) ? playerController.WorkerMultiInventory : null);
			int num = ((multiInventory != null) ? multiInventory.GetTotalCount(item.id) : 0);
			if (num <= 0)
			{
				return true;
			}
			List<UIAlchemyIngredient> alchemyIngredients = KeeperCheatMenuPlugin.GetAlchemyIngredients(this._activeAlchemyWindow);
			if (alchemyIngredients == null)
			{
				return true;
			}
			foreach (UIAlchemyIngredient uialchemyIngredient in alchemyIngredients)
			{
				if (!(uialchemyIngredient == null) && uialchemyIngredient.gameObject.activeSelf)
				{
					UIItemCell alchemyIngredientCell = KeeperCheatMenuPlugin.GetAlchemyIngredientCell(uialchemyIngredient);
					if (!(alchemyIngredientCell == null) && (alchemyIngredientCell.DisplayingItem == null || alchemyIngredientCell.DisplayingItem.IsEmpty))
					{
						alchemyIngredientCell.Draw(new Item(item.id, 1), true, num, false, 1, false, 0, false, false, false, (ItemRelatedWidgetState)4, false);
						FieldInfo alchemyIngredientPlusField = KeeperCheatMenuPlugin.AlchemyIngredientPlusField;
						GameObject gameObject = ((alchemyIngredientPlusField != null) ? alchemyIngredientPlusField.GetValue(uialchemyIngredient) : null) as GameObject;
						if (gameObject != null)
						{
							gameObject.SetActive(false);
						}
						MethodInfo redrawAlchemyTabLiteMethod = KeeperCheatMenuPlugin.RedrawAlchemyTabLiteMethod;
						if (redrawAlchemyTabLiteMethod != null)
						{
							redrawAlchemyTabLiteMethod.Invoke(this._activeAlchemyWindow, new object[1]);
						}
						this.RememberAlchemyIngredientSelection();
						return true;
					}
				}
			}
			return true;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002B68 File Offset: 0x00000D68
		private void RememberAlchemyIngredientSelection()
		{
			if (!this.RetainAlchemyIngredientsEnabled || this._activeAlchemyWindow == null || this._activeAlchemyWgoData == null)
			{
				return;
			}
			List<UIAlchemyIngredient> alchemyIngredients = KeeperCheatMenuPlugin.GetAlchemyIngredients(this._activeAlchemyWindow);
			if (alchemyIngredients == null)
			{
				return;
			}
			string[] array = new string[alchemyIngredients.Count];
			for (int i = 0; i < alchemyIngredients.Count; i++)
			{
				UIAlchemyIngredient uialchemyIngredient = alchemyIngredients[i];
				UIItemCell uiitemCell = ((uialchemyIngredient != null) ? KeeperCheatMenuPlugin.GetAlchemyIngredientCell(uialchemyIngredient) : null);
				Item item = ((uiitemCell != null) ? uiitemCell.DisplayingItem : null);
				if (uialchemyIngredient != null && uialchemyIngredient.gameObject.activeSelf && item != null && !item.IsEmpty)
				{
					array[i] = item.id;
				}
			}
			this._retainedAlchemyIngredients[KeeperCheatMenuPlugin.AlchemyStationKey(this._activeAlchemyWgoData)] = array;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002C30 File Offset: 0x00000E30
		private void RestoreRetainedAlchemyIngredients()
		{
			string[] array;
			if (!this.RetainAlchemyIngredientsEnabled || this._activeAlchemyWindow == null || this._activeAlchemyWgoData == null || !this._retainedAlchemyIngredients.TryGetValue(KeeperCheatMenuPlugin.AlchemyStationKey(this._activeAlchemyWgoData), out array))
			{
				return;
			}
			List<UIAlchemyIngredient> alchemyIngredients = KeeperCheatMenuPlugin.GetAlchemyIngredients(this._activeAlchemyWindow);
			PlayerController playerController = MainGame.PlayerController;
			MultiInventory multiInventory = ((playerController != null) ? playerController.WorkerMultiInventory : null);
			if (alchemyIngredients == null || multiInventory == null)
			{
				return;
			}
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
			bool flag = false;
			int num = Math.Min(alchemyIngredients.Count, array.Length);
			for (int i = 0; i < num; i++)
			{
				UIAlchemyIngredient uialchemyIngredient = alchemyIngredients[i];
				string text = array[i];
				if (!(uialchemyIngredient == null) && uialchemyIngredient.gameObject.activeSelf && !string.IsNullOrEmpty(text))
				{
					int num2;
					dictionary.TryGetValue(text, out num2);
					int totalCount = multiInventory.GetTotalCount(text);
					if (totalCount <= num2)
					{
						array[i] = null;
					}
					else
					{
						UIItemCell alchemyIngredientCell = KeeperCheatMenuPlugin.GetAlchemyIngredientCell(uialchemyIngredient);
						if (!(alchemyIngredientCell == null))
						{
							alchemyIngredientCell.Draw(new Item(text, 1), true, totalCount, false, 1, false, 0, false, false, false, (ItemRelatedWidgetState)4, false);
							FieldInfo alchemyIngredientPlusField = KeeperCheatMenuPlugin.AlchemyIngredientPlusField;
							GameObject gameObject = ((alchemyIngredientPlusField != null) ? alchemyIngredientPlusField.GetValue(uialchemyIngredient) : null) as GameObject;
							if (gameObject != null)
							{
								gameObject.SetActive(false);
							}
							dictionary[text] = num2 + 1;
							flag = true;
						}
					}
				}
			}
			if (flag)
			{
				MethodInfo redrawAlchemyTabLiteMethod = KeeperCheatMenuPlugin.RedrawAlchemyTabLiteMethod;
				if (redrawAlchemyTabLiteMethod == null)
				{
					return;
				}
				redrawAlchemyTabLiteMethod.Invoke(this._activeAlchemyWindow, new object[1]);
			}
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002DB5 File Offset: 0x00000FB5
		private static List<UIAlchemyIngredient> GetAlchemyIngredients(UIAlchemyWindow window)
		{
			FieldInfo alchemyIngredientsField = KeeperCheatMenuPlugin.AlchemyIngredientsField;
			return ((alchemyIngredientsField != null) ? alchemyIngredientsField.GetValue(window) : null) as List<UIAlchemyIngredient>;
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002DCE File Offset: 0x00000FCE
		private static UIItemCell GetAlchemyIngredientCell(UIAlchemyIngredient ingredient)
		{
			FieldInfo alchemyIngredientCellField = KeeperCheatMenuPlugin.AlchemyIngredientCellField;
			return ((alchemyIngredientCellField != null) ? alchemyIngredientCellField.GetValue(ingredient) : null) as UIItemCell;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002DE7 File Offset: 0x00000FE7
		private static string AlchemyStationKey(WgoData station)
		{
			return ((station != null) ? station.UniqueId.ToString() : null) ?? string.Empty;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002E03 File Offset: 0x00001003
		private void BeginFinishGrowingHotkeyCapture()
		{
			this.BeginHotkeyCapture(KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishGrowing);
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002E0C File Offset: 0x0000100C
		private void BeginFinishCraftsHotkeyCapture()
		{
			this.BeginHotkeyCapture(KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishCrafts);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002E15 File Offset: 0x00001015
		private void BeginRegrowForageHotkeyCapture()
		{
			this.BeginHotkeyCapture(KeeperCheatMenuPlugin.HotkeyCaptureTarget.RegrowForage);
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002E1E File Offset: 0x0000101E
		private void BeginWakeUpHotkeyCapture()
		{
			this.BeginHotkeyCapture(KeeperCheatMenuPlugin.HotkeyCaptureTarget.WakeUp);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002E27 File Offset: 0x00001027
		private void BeginHotkeyCapture(KeeperCheatMenuPlugin.HotkeyCaptureTarget target)
		{
			this._hotkeyCaptureTarget = ((this._hotkeyCaptureTarget == target) ? KeeperCheatMenuPlugin.HotkeyCaptureTarget.None : target);
			this.SetNativeStatus((this._hotkeyCaptureTarget == KeeperCheatMenuPlugin.HotkeyCaptureTarget.None) ? "Hotkey capture cancelled." : "Press an unbound keyboard key.");
			this.RefreshFinishGrowingControls();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002E5C File Offset: 0x0000105C
		private bool UpdateRangeActionHotkeyCapture()
		{
			if (this._hotkeyCaptureTarget == KeeperCheatMenuPlugin.HotkeyCaptureTarget.None)
			{
				return false;
			}
			if (!this._show)
			{
				this._hotkeyCaptureTarget = KeeperCheatMenuPlugin.HotkeyCaptureTarget.None;
				this.RefreshFinishGrowingControls();
				return false;
			}
			foreach (object obj in Enum.GetValues(typeof(KeyCode)))
			{
				KeyCode keyCode = (KeyCode)obj;
				if (KeeperCheatMenuPlugin.IsKeyboardHotkey(keyCode) && Input.GetKeyDown(keyCode))
				{
					if (this.IsKeyAlreadyBound(keyCode, this._hotkeyCaptureTarget))
					{
						this.SetNativeStatus("Key already bound.");
						TextMeshProUGUI textMeshProUGUI = this.TextForTarget(this._hotkeyCaptureTarget);
						if (textMeshProUGUI != null)
						{
							textMeshProUGUI.text = "Key already bound - press another";
						}
						return true;
					}
					this.SetHotkey(this._hotkeyCaptureTarget, keyCode);
					string text = KeeperCheatMenuPlugin.ActionLabel(this._hotkeyCaptureTarget);
					this._hotkeyCaptureTarget = KeeperCheatMenuPlugin.HotkeyCaptureTarget.None;
					base.Config.Save();
					this.SetNativeStatus(text + " hotkey set to " + keyCode.ToString() + ".");
					this.RefreshFinishGrowingControls();
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002F94 File Offset: 0x00001194
		private bool IsKeyAlreadyBound(KeyCode key, KeeperCheatMenuPlugin.HotkeyCaptureTarget target)
		{
			if (this._menuKey != null && this._menuKey.Value.MainKey == key)
			{
				return true;
			}
			if (target != KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishGrowing && this._finishGrowingHotkey.Value == key)
			{
				return true;
			}
			if (target != KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishCrafts && this._finishCraftsHotkey.Value == key)
			{
				return true;
			}
			if (target != KeeperCheatMenuPlugin.HotkeyCaptureTarget.RegrowForage && this._regrowForageHotkey.Value == key)
			{
				return true;
			}
			if (target != KeeperCheatMenuPlugin.HotkeyCaptureTarget.WakeUp && this._wakeUpHotkey.Value == key)
			{
				return true;
			}
			GameSettings instance = GameSettings.Instance;
			if (((instance != null) ? instance.keyboardKeybindings : null) == null)
			{
				return false;
			}
			foreach (GameSettings.KeyBindingForSave keyBindingForSave in instance.keyboardKeybindings)
			{
				if (keyBindingForSave != null && keyBindingForSave.keyCode == key)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000021 RID: 33 RVA: 0x0000307C File Offset: 0x0000127C
		private static bool IsKeyboardHotkey(KeyCode key)
		{
			if (key == KeyCode.None)
			{
				return false;
			}
			string text = key.ToString();
			return !text.StartsWith("Mouse", StringComparison.Ordinal) && !text.StartsWith("Joystick", StringComparison.Ordinal);
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000030BC File Offset: 0x000012BC
		private void SetHotkey(KeeperCheatMenuPlugin.HotkeyCaptureTarget target, KeyCode key)
		{
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishGrowing)
			{
				this._finishGrowingHotkey.Value = key;
				return;
			}
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishCrafts)
			{
				this._finishCraftsHotkey.Value = key;
				return;
			}
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.RegrowForage)
			{
				this._regrowForageHotkey.Value = key;
				return;
			}
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.WakeUp)
			{
				this._wakeUpHotkey.Value = key;
			}
		}

		// Token: 0x06000023 RID: 35 RVA: 0x0000310C File Offset: 0x0000130C
		private KeyCode HotkeyForTarget(KeeperCheatMenuPlugin.HotkeyCaptureTarget target)
		{
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishGrowing)
			{
				return this._finishGrowingHotkey.Value;
			}
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishCrafts)
			{
				return this._finishCraftsHotkey.Value;
			}
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.RegrowForage)
			{
				return this._regrowForageHotkey.Value;
			}
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.WakeUp)
			{
				return this._wakeUpHotkey.Value;
			}
			return KeyCode.None;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x0000315A File Offset: 0x0000135A
		private TextMeshProUGUI TextForTarget(KeeperCheatMenuPlugin.HotkeyCaptureTarget target)
		{
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishGrowing)
			{
				return this._finishGrowingHotkeyText;
			}
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishCrafts)
			{
				return this._finishCraftsHotkeyText;
			}
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.WakeUp)
			{
				return this._wakeUpHotkeyText;
			}
			return this._regrowForageHotkeyText;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00003183 File Offset: 0x00001383
		private static string ActionLabel(KeeperCheatMenuPlugin.HotkeyCaptureTarget target)
		{
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishGrowing)
			{
				return "Finish growing";
			}
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishCrafts)
			{
				return "Finish crafts";
			}
			if (target == KeeperCheatMenuPlugin.HotkeyCaptureTarget.WakeUp)
			{
				return "Wake up";
			}
			return "Regrow forage";
		}

		// Token: 0x06000026 RID: 38 RVA: 0x000031A8 File Offset: 0x000013A8
		private void SetFinishGrowingRangeFromText(string value)
		{
			int value2;
			if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value2))
			{
				value2 = this._finishGrowingRange.Value;
			}
			this.SetFinishGrowingRange(value2);
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000031D8 File Offset: 0x000013D8
		private void ChangeFinishGrowingRange(int delta)
		{
			this.SetFinishGrowingRange(this._finishGrowingRange.Value + delta);
		}

		// Token: 0x06000028 RID: 40 RVA: 0x000031ED File Offset: 0x000013ED
		private void SetFinishGrowingRange(int range)
		{
			this._finishGrowingRange.Value = Mathf.Clamp(range, 1, 30);
			base.Config.Save();
			this.RefreshFinishGrowingControls();
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00003214 File Offset: 0x00001414
		private void RefreshFinishGrowingControls()
		{
			if (this._finishGrowingRangeInput != null)
			{
				this._finishGrowingRangeInput.text = Mathf.Clamp(this._finishGrowingRange.Value, 1, 30).ToString(CultureInfo.InvariantCulture);
			}
			this.RefreshHotkeyText(KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishGrowing);
			this.RefreshHotkeyText(KeeperCheatMenuPlugin.HotkeyCaptureTarget.FinishCrafts);
			this.RefreshHotkeyText(KeeperCheatMenuPlugin.HotkeyCaptureTarget.RegrowForage);
			this.RefreshHotkeyText(KeeperCheatMenuPlugin.HotkeyCaptureTarget.WakeUp);
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00003278 File Offset: 0x00001478
		private void RefreshHotkeyText(KeeperCheatMenuPlugin.HotkeyCaptureTarget target)
		{
			TextMeshProUGUI textMeshProUGUI = this.TextForTarget(target);
			if (textMeshProUGUI == null)
			{
				return;
			}
			KeyCode keyCode = this.HotkeyForTarget(target);
			textMeshProUGUI.text = ((this._hotkeyCaptureTarget == target) ? this.L("Press a key...") : (this.L("Hotkey") + ": " + ((keyCode == KeyCode.None) ? this.L("Unassigned") : keyCode.ToString())));
		}

		// Token: 0x0600002B RID: 43 RVA: 0x000032F0 File Offset: 0x000014F0
		private bool TryGetRangeContext(bool showStatus, out GameScene scene, out Vector3 playerPosition, out int range)
		{
			scene = null;
			playerPosition = Vector3.zero;
			range = Mathf.Clamp(this._finishGrowingRange.Value, 1, 30);
			PlayerController playerController = MainGame.PlayerController;
			if (playerController == null)
			{
				if (showStatus)
				{
					this.SetNativeStatus("Load or start a save first.");
				}
				return false;
			}
			scene = playerController.CurrentGameScene;
			if (scene == null)
			{
				playerController.TryGetCurrentGameScene(out scene);
			}
			GameScene gameScene = scene;
			if (((gameScene != null) ? gameScene.GameSceneData : null) == null)
			{
				if (showStatus)
				{
					this.SetNativeStatus("The current scene is not ready.");
				}
				return false;
			}
			playerPosition = playerController.MovablePosition;
			return true;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00003387 File Offset: 0x00001587
		private IEnumerable<WgoData> WgosInRange(GameScene scene, Vector3 playerPosition, int range)
		{
			List<WgoData> wgoDataList = scene.GameSceneData.wgoDataList;
			if (wgoDataList == null)
			{
				yield break;
			}
			foreach (WgoData wgoData in wgoDataList)
			{
				if (wgoData != null)
				{
					Vector3 position = wgoData.Position;
					if (Vector2.Distance(new Vector2(playerPosition.x, playerPosition.z), new Vector2(position.x, position.z)) <= (float)range)
					{
						yield return wgoData;
					}
				}
			}
			List<WgoData>.Enumerator enumerator = default(List<WgoData>.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x000033A8 File Offset: 0x000015A8
		private void FinishGrowingCropsInRange(bool showStatus)
		{
			GameScene gameScene;
			Vector3 vector;
			int num;
			if (!this.TryGetRangeContext(showStatus, out gameScene, out vector, out num))
			{
				return;
			}
			int num2 = 0;
			foreach (WgoData wgoData in this.WgosInRange(gameScene, vector, num))
			{
				if (GardenBedNavigation.IsGardenPlot(wgoData))
				{
					CraftComponent craftComponent = wgoData.CraftComponent;
					CraftElementBase craftElementBase = ((craftComponent != null) ? craftComponent.CurrentCraftElement : null);
					if (craftComponent != null && craftElementBase != null && craftComponent.IsStarted)
					{
						GameBalance me = GameBalance.Me;
						if (((me != null) ? me.gardenGrowingCrafts : null) != null && GameBalance.Me.gardenGrowingCrafts.ContainsKey(craftElementBase.CraftId) && craftElementBase.ProgressTicks < craftElementBase.TotalProgressTicks)
						{
							craftComponent.UpdateManual(craftElementBase.TotalProgressTicks - craftElementBase.ProgressTicks);
							num2++;
						}
					}
				}
			}
			this.ReportRangeAction(showStatus, (num2 == 0) ? string.Format("No growing crops found within range {0}.", num) : string.Format("Finished growing {0} crop plot(s) within range {1}.", num2, num));
		}

		// Token: 0x0600002E RID: 46 RVA: 0x000034CC File Offset: 0x000016CC
		private void FinishCraftsInRange(bool showStatus)
		{
			GameScene gameScene;
			Vector3 vector;
			int num;
			if (!this.TryGetRangeContext(showStatus, out gameScene, out vector, out num))
			{
				return;
			}
			int num2 = 0;
			foreach (WgoData wgoData in this.WgosInRange(gameScene, vector, num))
			{
				if (!GardenBedNavigation.IsGardenPlot(wgoData))
				{
					CraftComponent craftComponent = wgoData.CraftComponent;
					CraftElementBase craftElementBase = ((craftComponent != null) ? craftComponent.CurrentCraftElement : null);
					if (craftComponent != null && craftElementBase != null && craftComponent.IsStarted && !craftComponent.IsDestroyingCraftActive && craftElementBase.ProgressTicks < craftElementBase.TotalProgressTicks)
					{
						craftComponent.UpdateManual(craftElementBase.TotalProgressTicks - craftElementBase.ProgressTicks);
						craftComponent.TryFinishCurCraft();
						num2++;
					}
				}
			}
			this.ReportRangeAction(showStatus, (num2 == 0) ? string.Format("No active machines found within range {0}.", num) : string.Format("Finished {0} active machine craft(s) within range {1}.", num2, num));
		}

		// Token: 0x0600002F RID: 47 RVA: 0x000035CC File Offset: 0x000017CC
		private void RegrowForageInRange(bool showStatus)
		{
			GameScene gameScene;
			Vector3 vector;
			int num;
			if (!this.TryGetRangeContext(showStatus, out gameScene, out vector, out num))
			{
				return;
			}
			int num2 = 0;
			List<string> list = new List<string>();
			foreach (WgoData wgoData in this.WgosInRange(gameScene, vector, num))
			{
				CraftComponent craftComponent = wgoData.CraftComponent;
				CraftElementBase craftElementBase = ((craftComponent != null) ? craftComponent.CurrentCraftElement : null);
				if (craftComponent != null && craftElementBase != null && craftComponent.IsStarted && craftElementBase.ProgressTicks < craftElementBase.TotalProgressTicks)
				{
					WGODef definition = wgoData.Definition;
					string text = ((definition != null) ? definition.wgoGroup : null) ?? "";
					if (list.Count < 30)
					{
						list.Add(string.Concat(new string[] { "id=", wgoData.id, ", group=", text, ", craft=", craftElementBase.CraftId }));
					}
					if (KeeperCheatMenuPlugin.IsForageWgo(wgoData, craftElementBase.CraftId))
					{
						try
						{
							craftComponent.UpdateManual(craftElementBase.TotalProgressTicks - craftElementBase.ProgressTicks);
							craftComponent.TryFinishCurCraft();
							num2++;
						}
						catch (Exception ex)
						{
							base.Logger.LogWarning("Could not finish forage regrowth " + wgoData.id + ": " + ex.Message);
						}
					}
				}
			}
			MainGame instance = MainGame.Instance;
			WgoDelayedSpawnSystemData obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				GameSave gameSave = instance.GameSave;
				obj = ((gameSave != null) ? gameSave.wgoDelayedSpawnSystemData : null);
			}
			WgoDelayedSpawnSystemData obj2 = obj;
			List<SpawnDelayedObject> list2 = ((obj2 != null) ? obj2.spawnDelayedObjects : null);
			for (int i = ((list2 != null) ? list2.Count : 0) - 1; i >= 0; i--)
			{
				SpawnDelayedObject spawnDelayedObject = list2[i];
				WgoData wgoData2 = ((spawnDelayedObject != null) ? spawnDelayedObject.ResolvedWgoData : null);
				CraftElement craftElement = ((spawnDelayedObject != null) ? spawnDelayedObject.craftElement : null);
				if (wgoData2 != null && craftElement != null && KeeperCheatMenuPlugin.IsForageRespawn(wgoData2, craftElement))
				{
					Vector3 position = wgoData2.Position;
					if (Vector2.Distance(new Vector2(vector.x, vector.z), new Vector2(position.x, position.z)) <= (float)num)
					{
						try
						{
							craftElement.BindCraftable(wgoData2);
							craftElement.Count = 1;
							wgoData2.CraftComponent.AddCraftNoStart(craftElement);
							wgoData2.OnCraftEnd(craftElement);
							list2.RemoveAt(i);
							num2++;
						}
						catch (Exception ex2)
						{
							base.Logger.LogWarning("Could not regrow forage " + wgoData2.id + ": " + ex2.Message);
						}
					}
				}
			}
			if (num2 == 0 && list.Count > 0)
			{
				base.Logger.LogInfo("Forage diagnostics (nearby active crafts): " + string.Join(" | ", list));
			}
			this.ReportRangeAction(showStatus, (num2 == 0) ? string.Format("No harvested berries, mushrooms, or wild honey found within range {0}.", num) : string.Format("Regrew {0} forage node(s) within range {1}.", num2, num));
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00003914 File Offset: 0x00001B14
		private static bool IsForageRespawn(WgoData wgo, CraftElement element)
		{
			if (KeeperCheatMenuPlugin.IsForageWgo(wgo, element.CraftId))
			{
				return true;
			}
			WGODef definition = wgo.Definition;
			if (!string.Equals((definition != null) ? definition.wgoGroup : null, "spawner", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			string text = (wgo.id + " " + element.CraftId).ToLowerInvariant();
			return text.Contains("berry") || text.Contains("berries") || text.Contains("mushroom") || text.Contains("honey") || text.Contains("wild_honey") || text.Contains("beehive") || text.Contains("bee_hive");
		}

		// Token: 0x06000031 RID: 49 RVA: 0x000039CC File Offset: 0x00001BCC
		private static bool IsForageWgo(WgoData wgo, string craftId)
		{
			WGODef definition = wgo.Definition;
			string text = (((definition != null) ? definition.wgoGroup : null) ?? "").ToLowerInvariant();
			if (text == "bushes" || text == "collectable_bushes")
			{
				return true;
			}
			string text2 = (wgo.id + " " + craftId).ToLowerInvariant();
			return text2.Contains("berry") || text2.Contains("mushroom") || text2.Contains("honey") || text2.Contains("wild_bee") || text2.Contains("beehive") || text2.Contains("bee_hive");
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003A7B File Offset: 0x00001C7B
		private void ReportRangeAction(bool showStatus, string message)
		{
			base.Logger.LogInfo(message);
			if (showStatus)
			{
				this.SetNativeStatus(message);
			}
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003A94 File Offset: 0x00001C94
		private void RefillLoadedFishingPonds()
		{
			PlayerController playerController = MainGame.PlayerController;
			GameScene gameScene = ((playerController != null) ? playerController.CurrentGameScene : null);
			if (gameScene == null)
			{
				PlayerController playerController2 = MainGame.PlayerController;
				if (playerController2 != null)
				{
					playerController2.TryGetCurrentGameScene(out gameScene);
				}
			}
			if (((gameScene != null) ? gameScene.Wgos : null) == null)
			{
				return;
			}
			foreach (Wgo wgo in gameScene.Wgos)
			{
				FishingPondStockHelper.RefillToConfiguredCapacity(wgo, true);
			}
			this._nextFishingOverlayRefresh = 0f;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003B30 File Offset: 0x00001D30
		internal void SetFishingReservoirInRange(Wgo reservoir)
		{
			this._fishingReservoirInRange = reservoir != null;
			this._activeFishingReservoir = ((reservoir != null) ? reservoir.Data : null);
			this._nextFishingOverlayRefresh = 0f;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00003B5C File Offset: 0x00001D5C
		internal void ClearFishingReservoirInRange(Wgo reservoir)
		{
			this._fishingReservoirInRange = false;
			if (!this._fishingActivityActive && (reservoir == null || this._activeFishingReservoir == reservoir.Data))
			{
				this._activeFishingReservoir = null;
				this.HideFishingOverlay();
			}
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00003B91 File Offset: 0x00001D91
		internal void SetFishingActivityReservoir(Wgo reservoir)
		{
			this._fishingActivityActive = true;
			if (reservoir != null)
			{
				this._activeFishingReservoir = reservoir.Data;
			}
			this._nextFishingOverlayRefresh = 0f;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003BBA File Offset: 0x00001DBA
		internal void EndFishingActivity()
		{
			this._fishingActivityActive = false;
			if (!this._fishingReservoirInRange)
			{
				this._activeFishingReservoir = null;
				this.HideFishingOverlay();
			}
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00003BD8 File Offset: 0x00001DD8
		private void UpdateFishingOverlay()
		{
			ConfigEntry<bool> fishOverlayEnabled = this._fishOverlayEnabled;
			if (fishOverlayEnabled == null || !fishOverlayEnabled.Value || this._show || this._activeFishingReservoir == null)
			{
				this.HideFishingOverlay();
				return;
			}
			if (Time.unscaledTime < this._nextFishingOverlayRefresh)
			{
				return;
			}
			this._nextFishingOverlayRefresh = Time.unscaledTime + 0.25f;
			List<FishingDef> allForReservoir = FishingDef.GetAllForReservoir(this._activeFishingReservoir.id);
			if (allForReservoir == null || allForReservoir.Count == 0)
			{
				this.HideFishingOverlay();
				return;
			}
			this.EnsureFishingOverlay();
			StringBuilder stringBuilder = new StringBuilder();
			int num = 0;
			foreach (IGrouping<string, FishingDef> grouping in (from def in allForReservoir
				where def != null && !string.IsNullOrEmpty(def.fishId)
				group def by def.fishId).OrderBy<IGrouping<string, FishingDef>, string>((IGrouping<string, FishingDef> group) => this.DisplayItemName(group.Key), StringComparer.OrdinalIgnoreCase))
			{
				int num2 = Mathf.Max(0, this._activeFishingReservoir.GetGameResInt(grouping.Key));
				HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (FishingDef fishingDef in grouping)
				{
					GameRes baitMod = fishingDef.baitMod;
					if (((baitMod != null) ? baitMod.List : null) != null)
					{
						foreach (GameResAtom gameResAtom in fishingDef.baitMod.List)
						{
							if (gameResAtom != null && gameResAtom.value != 0f && !string.IsNullOrEmpty(gameResAtom.type))
							{
								hashSet.Add(gameResAtom.type);
							}
						}
					}
				}
				string text = ((hashSet.Count == 0) ? "None listed" : string.Join(", ", hashSet.OrderBy<string, string>(new Func<string, string>(this.DisplayItemName)).Select<string, string>(new Func<string, string>(this.DisplayItemName))));
				stringBuilder.Append("<color=#F5D58A>").Append(num2).Append("</color>")
					.Append("<pos=52><color=#FFF0C2>")
					.Append(KeeperCheatMenuPlugin.EscapeOverlayText(KeeperCheatMenuPlugin.TrimOverlayColumn(this.DisplayItemName(grouping.Key), 24)))
					.Append("</color><pos=285><color=#F2C27B>")
					.Append(KeeperCheatMenuPlugin.EscapeOverlayText(text))
					.Append("</color>\n");
				num++;
			}
			this._fishingOverlayText.text = stringBuilder.ToString().TrimEnd();
			float num3 = Mathf.Clamp(10f + (float)num * 28f, 38f, 320f);
			this._fishingOverlayPanel.sizeDelta = new Vector2(680f, num3);
			this.RefreshFishingOverlayLockState();
			this._fishingOverlayRoot.SetActive(true);
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00003F14 File Offset: 0x00002114
		private static string TrimOverlayColumn(string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
			{
				return value ?? string.Empty;
			}
			return value.Substring(0, Math.Max(1, maxLength - 1)) + "…";
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003F4C File Offset: 0x0000214C
		private static string EscapeOverlayText(string value)
		{
			return (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00003F88 File Offset: 0x00002188
		private string DisplayItemName(string itemId)
		{
			if (string.Equals(itemId, "no_bait", StringComparison.OrdinalIgnoreCase))
			{
				return "No bait";
			}
			GameBalance me = GameBalance.Me;
			ItemDef itemDef;
			if (me == null)
			{
				itemDef = null;
			}
			else
			{
				List<ItemDef> itemDefs = me.itemDefs;
				itemDef = ((itemDefs != null) ? itemDefs.FirstOrDefault<ItemDef>((ItemDef def) => def != null && def.id == itemId) : null);
			}
			ItemDef itemDef2 = itemDef;
			if (itemDef2 != null)
			{
				return KeeperCheatMenuPlugin.ShortItemName(itemDef2);
			}
			return itemId;
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00003FF8 File Offset: 0x000021F8
		private void EnsureFishingOverlay()
		{
			if (this._fishingOverlayRoot != null)
			{
				return;
			}
			if (this._pixelFont == null)
			{
				this._pixelFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault<TMP_FontAsset>((TMP_FontAsset font) => font != null && font.name.IndexOf("small_font_bold", StringComparison.OrdinalIgnoreCase) >= 0) ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault<TMP_FontAsset>();
			}
			this._fishingOverlayRoot = new GameObject("KeeperFishOverlay", new Type[]
			{
				typeof(RectTransform),
				typeof(Canvas),
				typeof(CanvasScaler),
				typeof(GraphicRaycaster)
			});
			UnityEngine.Object.DontDestroyOnLoad(this._fishingOverlayRoot);
			Canvas component = this._fishingOverlayRoot.GetComponent<Canvas>();
			component.renderMode = 0;
			component.sortingOrder = 20000;
			CanvasScaler component2 = this._fishingOverlayRoot.GetComponent<CanvasScaler>();
			component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			component2.referenceResolution = new Vector2(1920f, 1080f);
			component2.matchWidthOrHeight = 0.5f;
			this._fishingOverlayPanel = this.Rect("FishPanel", this._fishingOverlayRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(680f, 180f), Color.clear);
			this._fishingOverlayPanel.anchorMin = new Vector2(0f, 1f);
			this._fishingOverlayPanel.anchorMax = new Vector2(0f, 1f);
			this._fishingOverlayPanel.pivot = new Vector2(0f, 1f);
			this._fishingOverlayPanel.sizeDelta = new Vector2(680f, 180f);
			this._fishingOverlayPanel.anchoredPosition = new Vector2(this._fishOverlayPositionX.Value, -this._fishOverlayPositionY.Value);
			this._fishingOverlayDragSurface = this._fishingOverlayPanel.GetComponent<Image>();
			this._fishingOverlayDragSurface.color = new Color(0f, 0f, 0f, 0f);
			this._fishingOverlayPanel.gameObject.AddComponent<FishingOverlayDragHandle>().Owner = this;
			this._fishingOverlayText = this.CreateText("", this._fishingOverlayPanel, 18f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, Vector2.zero, Vector2.one, new Vector2(34f, 0f), Vector2.zero);
			this._fishingOverlayText.lineSpacing = 4f;
			this._fishingOverlayText.raycastTarget = false;
			this._fishingOverlayText.enableWordWrapping = false;
			this._fishingOverlayText.outlineColor = new Color32(18, 12, 8, byte.MaxValue);
			this._fishingOverlayText.outlineWidth = 0.18f;
			this.CreateFishingOverlayLockButton();
			this.RefreshFishingOverlayLockState();
			this._fishingOverlayRoot.SetActive(false);
		}

		// Token: 0x0600003D RID: 61 RVA: 0x000042E0 File Offset: 0x000024E0
		private void CreateFishingOverlayLockButton()
		{
			RectTransform rectTransform = this.Rect("LockToggle", this._fishingOverlayPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -28f), new Vector2(28f, 0f), Color.clear);
			rectTransform.GetComponent<Image>().raycastTarget = true;
			Button button = rectTransform.gameObject.AddComponent<Button>();
			button.transition = Selectable.Transition.None;
			button.onClick.AddListener(new UnityAction(this.ToggleFishingOverlayLock));
			RectTransform rectTransform2 = this.Rect("Body", rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(7f, -23f), new Vector2(21f, -13f), KeeperCheatMenuPlugin.TextPale);
			rectTransform2.GetComponent<Image>().raycastTarget = false;
			this._fishingOverlayLockBody = rectTransform2.gameObject;
			this._fishingOverlayClosedShackle = new GameObject("ClosedShackle", new Type[] { typeof(RectTransform) });
			this._fishingOverlayClosedShackle.transform.SetParent(rectTransform, false);
			KeeperCheatMenuPlugin.StretchToParent((RectTransform)this._fishingOverlayClosedShackle.transform);
			this.CreateLockLine(this._fishingOverlayClosedShackle.transform, 8f, -14f, 2f, 7f);
			this.CreateLockLine(this._fishingOverlayClosedShackle.transform, 19f, -14f, 2f, 7f);
			this.CreateLockLine(this._fishingOverlayClosedShackle.transform, 8f, -9f, 13f, 2f);
			this._fishingOverlayOpenShackle = new GameObject("OpenShackle", new Type[] { typeof(RectTransform) });
			this._fishingOverlayOpenShackle.transform.SetParent(rectTransform, false);
			KeeperCheatMenuPlugin.StretchToParent((RectTransform)this._fishingOverlayOpenShackle.transform);
			this.CreateLockLine(this._fishingOverlayOpenShackle.transform, 9f, -14f, 2f, 7f);
			this.CreateLockLine(this._fishingOverlayOpenShackle.transform, 20f, -10f, 2f, 5f);
			this.CreateLockLine(this._fishingOverlayOpenShackle.transform, 10f, -7f, 12f, 2f);
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00004552 File Offset: 0x00002752
		private static void StretchToParent(RectTransform rect)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00004580 File Offset: 0x00002780
		private RectTransform CreateLockLine(Transform parent, float x, float y, float width, float height)
		{
			RectTransform rectTransform = this.Rect("Line", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y - height), new Vector2(x + width, y), KeeperCheatMenuPlugin.TextPale);
			rectTransform.GetComponent<Image>().raycastTarget = false;
			return rectTransform;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x000045DC File Offset: 0x000027DC
		private void ToggleFishingOverlayLock()
		{
			this._fishOverlayLocked.Value = !this._fishOverlayLocked.Value;
			base.Config.Save();
			this.RefreshFishingOverlayLockState();
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00004608 File Offset: 0x00002808
		private void RefreshFishingOverlayLockState()
		{
			if (this._fishingOverlayDragSurface != null)
			{
				Graphic fishingOverlayDragSurface = this._fishingOverlayDragSurface;
				ConfigEntry<bool> fishOverlayLocked = this._fishOverlayLocked;
				fishingOverlayDragSurface.raycastTarget = fishOverlayLocked != null && !fishOverlayLocked.Value;
			}
			if (this._fishingOverlayClosedShackle != null)
			{
				GameObject fishingOverlayClosedShackle = this._fishingOverlayClosedShackle;
				ConfigEntry<bool> fishOverlayLocked2 = this._fishOverlayLocked;
				fishingOverlayClosedShackle.SetActive(fishOverlayLocked2 == null || fishOverlayLocked2.Value);
			}
			if (this._fishingOverlayOpenShackle != null)
			{
				GameObject fishingOverlayOpenShackle = this._fishingOverlayOpenShackle;
				ConfigEntry<bool> fishOverlayLocked3 = this._fishOverlayLocked;
				fishingOverlayOpenShackle.SetActive(fishOverlayLocked3 != null && !fishOverlayLocked3.Value);
			}
			ConfigEntry<bool> fishOverlayLocked4 = this._fishOverlayLocked;
			Color color = ((fishOverlayLocked4 != null && !fishOverlayLocked4.Value) ? new Color32(174, 232, 154, byte.MaxValue) : new Color32(245, 213, 138, byte.MaxValue));
			KeeperCheatMenuPlugin.SetFishingLockColor(this._fishingOverlayLockBody, color);
			ConfigEntry<bool> fishOverlayLocked5 = this._fishOverlayLocked;
			KeeperCheatMenuPlugin.SetFishingLockColor((fishOverlayLocked5 != null && !fishOverlayLocked5.Value) ? this._fishingOverlayOpenShackle : this._fishingOverlayClosedShackle, color);
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00004724 File Offset: 0x00002924
		private static void SetFishingLockColor(GameObject root, Color color)
		{
			if (root == null)
			{
				return;
			}
			Image[] componentsInChildren = root.GetComponentsInChildren<Image>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].color = color;
			}
		}

		// Token: 0x06000043 RID: 67 RVA: 0x0000475C File Offset: 0x0000295C
		internal void DragFishingOverlay(Vector2 screenDelta)
		{
			ConfigEntry<bool> fishOverlayLocked = this._fishOverlayLocked;
			if (fishOverlayLocked == null || fishOverlayLocked.Value || this._fishingOverlayPanel == null)
			{
				return;
			}
			Canvas component = this._fishingOverlayRoot.GetComponent<Canvas>();
			float num = Mathf.Max(0.01f, component.scaleFactor);
			Vector2 vector = this._fishingOverlayPanel.anchoredPosition + screenDelta / num;
			vector.x = Mathf.Clamp(vector.x, 0f, 1920f - this._fishingOverlayPanel.sizeDelta.x);
			vector.y = Mathf.Clamp(vector.y, -1080f + this._fishingOverlayPanel.sizeDelta.y, 0f);
			this._fishingOverlayPanel.anchoredPosition = vector;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00004828 File Offset: 0x00002A28
		internal void SaveFishingOverlayPosition()
		{
			if (this._fishingOverlayPanel == null)
			{
				return;
			}
			this._fishOverlayPositionX.Value = this._fishingOverlayPanel.anchoredPosition.x;
			this._fishOverlayPositionY.Value = -this._fishingOverlayPanel.anchoredPosition.y;
			base.Config.Save();
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00004886 File Offset: 0x00002A86
		private void HideFishingOverlay()
		{
			if (this._fishingOverlayRoot != null)
			{
				this._fishingOverlayRoot.SetActive(false);
			}
		}

		// Token: 0x06000046 RID: 70 RVA: 0x000048A4 File Offset: 0x00002AA4
		private void DestroyFishingOverlay()
		{
			if (this._fishingOverlayRoot != null)
			{
				UnityEngine.Object.Destroy(this._fishingOverlayRoot);
			}
			this._fishingOverlayRoot = null;
			this._fishingOverlayPanel = null;
			this._fishingOverlayText = null;
			this._fishingOverlayDragSurface = null;
			this._fishingOverlayLockBody = null;
			this._fishingOverlayClosedShackle = null;
			this._fishingOverlayOpenShackle = null;
			this._activeFishingReservoir = null;
			this._fishingReservoirInRange = false;
			this._fishingActivityActive = false;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00004910 File Offset: 0x00002B10
		private void RepairLoadedZombieJobs()
		{
			PlayerController playerController = MainGame.PlayerController;
			if (playerController == null)
			{
				this.SetNativeStatus("Load or start a save first.");
				return;
			}
			GameScene currentGameScene = playerController.CurrentGameScene;
			if (currentGameScene == null)
			{
				playerController.TryGetCurrentGameScene(out currentGameScene);
			}
			if (((currentGameScene != null) ? currentGameScene.Wgos : null) == null)
			{
				this.SetNativeStatus("The current scene is not ready.");
				return;
			}
			ZombieWgoData[] array = (from zombie in currentGameScene.Wgos.Select<Wgo, WgoData>(delegate(Wgo wgo)
				{
					if (wgo == null)
					{
						return null;
					}
					return wgo.Data;
				}).OfType<ZombieWgoData>()
				where zombie.AttachedWgoData != null
				select zombie).Distinct<ZombieWgoData>().ToArray<ZombieWgoData>();
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			foreach (ZombieWgoData zombieWgoData in array)
			{
				try
				{
					WgoData attachedWgoData = zombieWgoData.AttachedWgoData;
					IWorker worker = attachedWgoData.Worker;
					if (worker == null)
					{
						attachedWgoData.ClearWorker();
						if (attachedWgoData.TrySetWorker(zombieWgoData, null))
						{
							num2++;
						}
					}
					else if (worker != zombieWgoData && worker.Id != zombieWgoData.Id)
					{
						num3++;
						base.Logger.LogWarning(string.Concat(new string[] { "Zombie job repair skipped '", zombieWgoData.Name, "': station '", attachedWgoData.id, "' belongs to another worker." }));
						goto IL_03EA;
					}
					CraftComponent craftComponent = attachedWgoData.CraftComponent;
					if (craftComponent != null)
					{
						craftComponent.UpdateQueueElementsCraftStatus();
					}
					switch (zombieWgoData.ZombieType)
					{
					case (ZombieType)1:
						if (craftComponent != null)
						{
							CraftElementBase currentCraftElement = craftComponent.CurrentCraftElement;
							bool? flag = ((currentCraftElement != null) ? new bool?(currentCraftElement.IsStarted) : null);
							bool flag2 = true;
							if (((flag.GetValueOrDefault() == flag2) & (flag != null)) && zombieWgoData.WorkerActivity == null)
							{
								zombieWgoData.CrafterStartCraftActivity(false);
								num++;
								break;
							}
						}
						if (craftComponent != null && craftComponent.HasCraftsInQueue && zombieWgoData.WorkerActivity == null)
						{
							zombieWgoData.TryResumeCrafterWorkAfterLoad();
							num++;
						}
						break;
					case (ZombieType)2:
						if (zombieWgoData.CaretakerState == (ZombieWgoData.ZombieCaretakerState)10 || zombieWgoData.CaretakerState == null)
						{
							zombieWgoData.CaretakerState = 0;
							MethodInfo caretakerResumeMethod = KeeperCheatMenuPlugin.CaretakerResumeMethod;
							if (caretakerResumeMethod != null)
							{
								caretakerResumeMethod.Invoke(zombieWgoData, null);
							}
							num++;
						}
						break;
					case (ZombieType)3:
						if (craftComponent != null)
						{
							CraftElementBase currentCraftElement2 = craftComponent.CurrentCraftElement;
							bool? flag = ((currentCraftElement2 != null) ? new bool?(currentCraftElement2.IsStarted) : null);
							bool flag2 = true;
							if (((flag.GetValueOrDefault() == flag2) & (flag != null)) && zombieWgoData.WorkerActivity == null)
							{
								MethodInfo conveyorCrafterStartMethod = KeeperCheatMenuPlugin.ConveyorCrafterStartMethod;
								if (conveyorCrafterStartMethod != null)
								{
									conveyorCrafterStartMethod.Invoke(zombieWgoData, new object[] { false });
								}
								num++;
								break;
							}
						}
						if (craftComponent != null && craftComponent.HasCraftsInQueue && zombieWgoData.WorkerActivity == null)
						{
							MethodInfo conveyorCrafterResumeMethod = KeeperCheatMenuPlugin.ConveyorCrafterResumeMethod;
							if (conveyorCrafterResumeMethod != null)
							{
								conveyorCrafterResumeMethod.Invoke(zombieWgoData, new object[1]);
							}
							num++;
						}
						break;
					case (ZombieType)5:
						if (zombieWgoData.PorterCheckDeliveryStart())
						{
							num++;
						}
						break;
					case (ZombieType)6:
						if (zombieWgoData.GardenerState == (ZombieWgoData.ZombieGardenerState)8 || (zombieWgoData.GardenerState == null && zombieWgoData.WorkerActivity == null))
						{
							zombieWgoData.GardenerState = 0;
							MethodInfo gardenerResumeMethod = KeeperCheatMenuPlugin.GardenerResumeMethod;
							if (gardenerResumeMethod != null)
							{
								gardenerResumeMethod.Invoke(zombieWgoData, null);
							}
							num++;
						}
						break;
					case (ZombieType)7:
						if (zombieWgoData.ConveyorTransporterState == (ZombieWgoData.ZombieConveyorTransporterState)4 || zombieWgoData.ConveyorTransporterState == null)
						{
							zombieWgoData.ConveyorTransporterState = 0;
							MethodInfo conveyorTransporterResumeMethod = KeeperCheatMenuPlugin.ConveyorTransporterResumeMethod;
							if (conveyorTransporterResumeMethod != null)
							{
								conveyorTransporterResumeMethod.Invoke(zombieWgoData, null);
							}
							num++;
						}
						break;
					}
					WorldZoneData worldZoneData = attachedWgoData.WorldZoneData;
					if (worldZoneData != null)
					{
						worldZoneData.NotifyWgoDataChanged();
					}
				}
				catch (Exception ex)
				{
					num3++;
					base.Logger.LogWarning(string.Format("Could not repair zombie '{0}': {1}", ((zombieWgoData != null) ? zombieWgoData.Name : null) ?? "unknown", ex));
				}
				IL_03EA:;
			}
			string text = string.Format("Checked {0} assigned zombie(s): restarted {1}, rebound {2}, skipped {3}.", new object[] { array.Length, num, num2, num3 });
			base.Logger.LogInfo(text);
			this.SetNativeStatus(text);
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00004D84 File Offset: 0x00002F84
		private void ResetNearestComposter()
		{
			PlayerController playerController = MainGame.PlayerController;
			if (playerController == null)
			{
				this.SetNativeStatus("Load or start a save first.");
				return;
			}
			GameScene currentGameScene = playerController.CurrentGameScene;
			if (currentGameScene == null)
			{
				playerController.TryGetCurrentGameScene(out currentGameScene);
			}
			if (((currentGameScene != null) ? currentGameScene.Wgos : null) == null)
			{
				this.SetNativeStatus("The current scene is not ready.");
				return;
			}
			Vector3 playerPosition = playerController.MovablePosition;
			IEnumerable<Wgo> wgos = currentGameScene.Wgos;
			Func<Wgo, bool> func;
			if ((func = KeeperCheatMenuPlugin.__OCache.__f0_IsComposter) == null)
			{
				func = (KeeperCheatMenuPlugin.__OCache.__f0_IsComposter = new Func<Wgo, bool>(KeeperCheatMenuPlugin.IsComposter));
			}
			Wgo wgo2 = (from wgo in wgos.Where<Wgo>(func)
				orderby KeeperCheatMenuPlugin.HorizontalDistance(playerPosition, wgo.Data.Position)
				select wgo).FirstOrDefault<Wgo>();
			if (wgo2 == null)
			{
				this.SetNativeStatus("No loaded composter found in this scene.");
				return;
			}
			float num = KeeperCheatMenuPlugin.HorizontalDistance(playerPosition, wgo2.Data.Position);
			if (num > 12f)
			{
				this.SetNativeStatus(string.Format("Nearest composter is too far away ({0:0.0}m). Stand next to it first.", num));
				return;
			}
			WgoData data = wgo2.Data;
			CraftComponent craftComponent = data.CraftComponent;
			CraftElementBase craftElementBase = ((craftComponent != null) ? craftComponent.CurrentCraftElement : null);
			string text = ((craftElementBase != null) ? craftElementBase.CraftId : null);
			int? num2;
			if (craftComponent == null)
			{
				num2 = null;
			}
			else
			{
				List<CraftElementBase> craftElementsQueue = craftComponent.CraftElementsQueue;
				num2 = ((craftElementsQueue != null) ? new int?(craftElementsQueue.Count) : null);
			}
			int? num3 = num2;
			int valueOrDefault = num3.GetValueOrDefault();
			try
			{
				base.Logger.LogInfo("Composter state before reset: " + KeeperCheatMenuPlugin.DescribeComposterState(wgo2, craftComponent));
				data.IsInteractable = false;
				data.ClearWorker();
				if (craftComponent != null && craftElementBase != null)
				{
					if (craftComponent.IsStarted)
					{
						craftComponent.Cancel();
					}
					craftComponent.Clear();
				}
				if (craftComponent != null)
				{
					FieldInfo craftHasPreFinishUpdateField = KeeperCheatMenuPlugin.CraftHasPreFinishUpdateField;
					if (craftHasPreFinishUpdateField != null)
					{
						craftHasPreFinishUpdateField.SetValue(craftComponent, false);
					}
					FieldInfo craftPreFinishHoldCountField = KeeperCheatMenuPlugin.CraftPreFinishHoldCountField;
					if (craftPreFinishHoldCountField != null)
					{
						craftPreFinishHoldCountField.SetValue(craftComponent, 0);
					}
					FieldInfo craftFinishHeldTimerField = KeeperCheatMenuPlugin.CraftFinishHeldTimerField;
					if (craftFinishHeldTimerField != null)
					{
						craftFinishHeldTimerField.SetValue(craftComponent, 0f);
					}
					FieldInfo craftRemovingDestroyField = KeeperCheatMenuPlugin.CraftRemovingDestroyField;
					if (craftRemovingDestroyField != null)
					{
						craftRemovingDestroyField.SetValue(craftComponent, false);
					}
					FieldInfo craftRestartQueueTimerField = KeeperCheatMenuPlugin.CraftRestartQueueTimerField;
					if (craftRestartQueueTimerField != null)
					{
						craftRestartQueueTimerField.SetValue(craftComponent, 0f);
					}
					FieldInfo craftAutoTickTimerField = KeeperCheatMenuPlugin.CraftAutoTickTimerField;
					if (craftAutoTickTimerField != null)
					{
						craftAutoTickTimerField.SetValue(craftComponent, 0f);
					}
					craftComponent.ResetCraftsFromBalanceCache();
					craftComponent.UpdateQueueElementsCraftStatus();
				}
				data.IsInteractable = true;
				wgo2.SetInteractableCollidersState(true);
				CraftInteractionHandler craftInteractionHandler = new CraftInteractionHandler();
				craftInteractionHandler.Init(wgo2);
				FieldInfo wgoInteractionHandlerField = KeeperCheatMenuPlugin.WgoInteractionHandlerField;
				if (wgoInteractionHandlerField != null)
				{
					wgoInteractionHandlerField.SetValue(wgo2, craftInteractionHandler);
				}
				PlayerInteractionComponent playerInteractionComponent = playerController.PlayerInteractionComponent;
				if (((playerInteractionComponent != null) ? playerInteractionComponent.WgoUnderInteraction : null) == wgo2)
				{
					craftInteractionHandler.OnInteractionTargetEnter(playerController);
				}
				wgo2.DrawWidgets();
				WorldZoneData worldZoneData = data.WorldZoneData;
				if (worldZoneData != null)
				{
					worldZoneData.NotifyWgoDataChanged();
				}
				string text2 = ((valueOrDefault > 0) ? (string.Format(" Cleared {0} stuck craft(s)", valueOrDefault) + (string.IsNullOrEmpty(text) ? "." : (" including '" + text + "'."))) : " No active craft needed clearing.");
				string text3 = string.Format("Reset composter '{0}' at {1:0.0}m.{2}", data.id, num, text2);
				base.Logger.LogInfo(text3);
				base.Logger.LogInfo("Composter state after reset: " + KeeperCheatMenuPlugin.DescribeComposterState(wgo2, craftComponent));
				this.SetNativeStatus(text3);
			}
			catch (Exception ex)
			{
				try
				{
					data.IsInteractable = true;
					wgo2.SetInteractableCollidersState(true);
				}
				catch
				{
				}
				ManualLogSource logger = base.Logger;
				string text4 = "Could not reset composter: ";
				Exception ex2 = ex;
				logger.LogError(text4 + ((ex2 != null) ? ex2.ToString() : null));
				this.SetNativeStatus("Composter reset failed: " + ex.Message);
			}
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00005168 File Offset: 0x00003368
		private void ResetNearestGardenBed()
		{
			PlayerController playerController = MainGame.PlayerController;
			if (playerController == null)
			{
				this.SetNativeStatus("Load or start a save first.");
				return;
			}
			GameScene currentGameScene = playerController.CurrentGameScene;
			if (currentGameScene == null)
			{
				playerController.TryGetCurrentGameScene(out currentGameScene);
			}
			if (((currentGameScene != null) ? currentGameScene.Wgos : null) == null)
			{
				this.SetNativeStatus("The current scene is not ready.");
				return;
			}
			Vector3 playerPosition = playerController.MovablePosition;
			IEnumerable<Wgo> wgos = currentGameScene.Wgos;
			Func<Wgo, bool> func;
			if ((func = KeeperCheatMenuPlugin.__OCache.__f1_IsGardenBed) == null)
			{
				func = (KeeperCheatMenuPlugin.__OCache.__f1_IsGardenBed = new Func<Wgo, bool>(KeeperCheatMenuPlugin.IsGardenBed));
			}
			Wgo wgo3 = (from wgo in wgos.Where<Wgo>(func)
				orderby KeeperCheatMenuPlugin.HorizontalDistance(playerPosition, wgo.Data.Position)
				select wgo).FirstOrDefault<Wgo>();
			if (wgo3 == null)
			{
				this.SetNativeStatus("No loaded garden bed found in this scene.");
				return;
			}
			float num = KeeperCheatMenuPlugin.HorizontalDistance(playerPosition, wgo3.Data.Position);
			if (num > 8f)
			{
				this.SetNativeStatus(string.Format("Nearest garden bed is too far away ({0:0.0}m). Stand next to it first.", num));
				return;
			}
			WgoData data = wgo3.Data;
			CraftComponent craftComponent = data.CraftComponent;
			int? num2;
			if (craftComponent == null)
			{
				num2 = null;
			}
			else
			{
				List<CraftElementBase> craftElementsQueue = craftComponent.CraftElementsQueue;
				num2 = ((craftElementsQueue != null) ? new int?(craftElementsQueue.Count) : null);
			}
			int? num3 = num2;
			int valueOrDefault = num3.GetValueOrDefault();
			string text;
			if (craftComponent == null)
			{
				text = null;
			}
			else
			{
				CraftElementBase currentCraftElement = craftComponent.CurrentCraftElement;
				text = ((currentCraftElement != null) ? currentCraftElement.CraftId : null);
			}
			string text2 = text;
			string id = data.id;
			WGODef definition = data.Definition;
			string text3 = ((((definition != null) ? definition.wgoGroup : null) == "vineyard_objects") ? "vineyard_empty" : "garden_empty");
			bool flag = !KeeperCheatMenuPlugin.IsEmptyGardenPlot(id);
			try
			{
				base.Logger.LogInfo("Garden plot state before reset: " + KeeperCheatMenuPlugin.DescribeGardenBedState(wgo3, craftComponent));
				data.IsInteractable = false;
				data.ClearWorker();
				if (craftComponent != null)
				{
					if (craftComponent.IsStarted)
					{
						craftComponent.Cancel();
					}
					craftComponent.Clear();
					FieldInfo craftHasPreFinishUpdateField = KeeperCheatMenuPlugin.CraftHasPreFinishUpdateField;
					if (craftHasPreFinishUpdateField != null)
					{
						craftHasPreFinishUpdateField.SetValue(craftComponent, false);
					}
					FieldInfo craftPreFinishHoldCountField = KeeperCheatMenuPlugin.CraftPreFinishHoldCountField;
					if (craftPreFinishHoldCountField != null)
					{
						craftPreFinishHoldCountField.SetValue(craftComponent, 0);
					}
					FieldInfo craftFinishHeldTimerField = KeeperCheatMenuPlugin.CraftFinishHeldTimerField;
					if (craftFinishHeldTimerField != null)
					{
						craftFinishHeldTimerField.SetValue(craftComponent, 0f);
					}
					FieldInfo craftRemovingDestroyField = KeeperCheatMenuPlugin.CraftRemovingDestroyField;
					if (craftRemovingDestroyField != null)
					{
						craftRemovingDestroyField.SetValue(craftComponent, false);
					}
					FieldInfo craftRestartQueueTimerField = KeeperCheatMenuPlugin.CraftRestartQueueTimerField;
					if (craftRestartQueueTimerField != null)
					{
						craftRestartQueueTimerField.SetValue(craftComponent, 0f);
					}
					FieldInfo craftAutoTickTimerField = KeeperCheatMenuPlugin.CraftAutoTickTimerField;
					if (craftAutoTickTimerField != null)
					{
						craftAutoTickTimerField.SetValue(craftComponent, 0f);
					}
					craftComponent.ResetCraftsFromBalanceCache();
					craftComponent.UpdateQueueElementsCraftStatus();
				}
				if (flag)
				{
					MainGame.Instance.GameSave.WorldData.ChangeWgoData(data, text3);
				}
				data.IsInteractable = true;
				GardenBedNavigation.TryRebuild(data);
				Wgo wgo2 = currentGameScene.Wgos.FirstOrDefault<Wgo>((Wgo wgo) => ((wgo != null) ? wgo.Data : null) == data);
				if (wgo2 != null)
				{
					wgo2.SetInteractableCollidersState(true);
					GardenInteractionHandler gardenInteractionHandler = new GardenInteractionHandler();
					gardenInteractionHandler.Init(wgo2);
					FieldInfo wgoInteractionHandlerField = KeeperCheatMenuPlugin.WgoInteractionHandlerField;
					if (wgoInteractionHandlerField != null)
					{
						wgoInteractionHandlerField.SetValue(wgo2, gardenInteractionHandler);
					}
					PlayerInteractionComponent playerInteractionComponent = playerController.PlayerInteractionComponent;
					if (((playerInteractionComponent != null) ? playerInteractionComponent.WgoUnderInteraction : null) == wgo2)
					{
						gardenInteractionHandler.OnInteractionTargetEnter(playerController);
					}
					wgo2.DrawWidgets();
				}
				WorldZoneData worldZoneData = data.WorldZoneData;
				if (worldZoneData != null)
				{
					worldZoneData.NotifyWgoDataChanged();
				}
				string text4 = ((valueOrDefault > 0) ? (string.Format(" Cleared {0} stuck craft(s)", valueOrDefault) + (string.IsNullOrEmpty(text2) ? "." : (" including '" + text2 + "'."))) : " No active craft needed clearing.");
				string text5 = (flag ? string.Concat(new string[] { " Restored '", id, "' to '", text3, "'." }) : " Plot was already empty.");
				string text6 = string.Format("Reset garden bed at {0:0.0}m.{1}{2}", num, text5, text4);
				base.Logger.LogInfo(text6);
				base.Logger.LogInfo("Garden plot state after reset: " + KeeperCheatMenuPlugin.DescribeGardenBedState(wgo2, data.CraftComponent));
				this.SetNativeStatus(text6);
			}
			catch (Exception ex)
			{
				try
				{
					data.IsInteractable = true;
					wgo3.SetInteractableCollidersState(true);
				}
				catch
				{
				}
				ManualLogSource logger = base.Logger;
				string text7 = "Could not reset garden bed: ";
				Exception ex2 = ex;
				logger.LogError(text7 + ((ex2 != null) ? ex2.ToString() : null));
				this.SetNativeStatus("Garden bed reset failed: " + ex.Message);
			}
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00005638 File Offset: 0x00003838
		private void RepairPlayerInventory()
		{
			PlayerController playerController = MainGame.PlayerController;
			Inventory inventory;
			if (playerController == null)
			{
				inventory = null;
			}
			else
			{
				PlayerData playerData = playerController.PlayerData;
				inventory = ((playerData != null) ? playerData.Inventory : null);
			}
			Inventory inventory2 = inventory;
			bool flag;
			if (inventory2 == null)
			{
				flag = null != null;
			}
			else
			{
				Item data = inventory2.Data;
				flag = ((data != null) ? data.Inventory : null) != null;
			}
			if (!flag)
			{
				this.SetNativeStatus("Load or start a save first.");
				return;
			}
			try
			{
				int num = KeeperCheatMenuPlugin.RemoveBrokenInventoryEntries(inventory2.Data, new HashSet<Item>());
				KeeperCheatMenuPlugin.RecalculateInventoryFillSize(inventory2.Data);
				inventory2.ForceTriggerOnItemsAddEventWithoutItems();
				this._clearInventoryConfirmationArmed = false;
				string text = ((num == 0) ? "Inventory scan found no malformed entries. Use Clear inventory only as a last resort." : string.Format("Removed {0} malformed inventory entr{1}. Valid items were kept.", num, (num == 1) ? "y" : "ies"));
				base.Logger.LogInfo(text);
				this.SetNativeStatus(text);
			}
			catch (Exception ex)
			{
				ManualLogSource logger = base.Logger;
				string text2 = "Could not repair player inventory: ";
				Exception ex2 = ex;
				logger.LogError(text2 + ((ex2 != null) ? ex2.ToString() : null));
				this.SetNativeStatus("Inventory repair failed: " + ex.Message);
			}
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00005740 File Offset: 0x00003940
		private static int RemoveBrokenInventoryEntries(Item container, HashSet<Item> visited)
		{
			if (container == null || container.Inventory == null || !visited.Add(container))
			{
				return 0;
			}
			int num = 0;
			List<Item> inventory = container.Inventory;
			for (int i = inventory.Count - 1; i >= 0; i--)
			{
				Item item = inventory[i];
				if (item == null || item.Count <= 0 || string.IsNullOrEmpty(item.id) || item.id == "empty" || item.Definition == null)
				{
					inventory.RemoveAt(i);
					num++;
				}
				else
				{
					num += KeeperCheatMenuPlugin.RemoveBrokenInventoryEntries(item, visited);
					KeeperCheatMenuPlugin.RecalculateInventoryFillSize(item);
				}
			}
			KeeperCheatMenuPlugin.RecalculateInventoryFillSize(container);
			return num;
		}

		// Token: 0x0600004C RID: 76 RVA: 0x000057E4 File Offset: 0x000039E4
		private static void RecalculateInventoryFillSize(Item item)
		{
			MethodInfo calculateInventoryFillSizeMethod = KeeperCheatMenuPlugin.CalculateInventoryFillSizeMethod;
			if (calculateInventoryFillSizeMethod == null)
			{
				return;
			}
			calculateInventoryFillSizeMethod.Invoke(item, null);
		}

		// Token: 0x0600004D RID: 77 RVA: 0x000057F8 File Offset: 0x000039F8
		private void ClearPlayerInventoryWithConfirmation()
		{
			PlayerController playerController = MainGame.PlayerController;
			Inventory inventory;
			if (playerController == null)
			{
				inventory = null;
			}
			else
			{
				PlayerData playerData = playerController.PlayerData;
				inventory = ((playerData != null) ? playerData.Inventory : null);
			}
			Inventory inventory2 = inventory;
			bool flag;
			if (inventory2 == null)
			{
				flag = null != null;
			}
			else
			{
				Item data = inventory2.Data;
				flag = ((data != null) ? data.Inventory : null) != null;
			}
			if (!flag)
			{
				this.SetNativeStatus("Load or start a save first.");
				return;
			}
			if (!this._clearInventoryConfirmationArmed)
			{
				this._clearInventoryConfirmationArmed = true;
				this.SetNativeStatus("Inventory clearing is permanent. Click Clear entire inventory again to confirm.");
				return;
			}
			try
			{
				KeeperCheatMenuPlugin.RemoveBrokenInventoryEntries(inventory2.Data, new HashSet<Item>());
				int count = inventory2.Data.Inventory.Count;
				inventory2.Clear();
				inventory2.ForceTriggerOnItemsAddEventWithoutItems();
				this._clearInventoryConfirmationArmed = false;
				string text = string.Format("Cleared the player inventory ({0} stack(s)).", count);
				base.Logger.LogWarning(text);
				this.SetNativeStatus(text);
			}
			catch (Exception ex)
			{
				this._clearInventoryConfirmationArmed = false;
				ManualLogSource logger = base.Logger;
				string text2 = "Could not clear player inventory: ";
				Exception ex2 = ex;
				logger.LogError(text2 + ((ex2 != null) ? ex2.ToString() : null));
				this.SetNativeStatus("Inventory clear failed: " + ex.Message);
			}
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00005914 File Offset: 0x00003B14
		private static string DescribeComposterState(Wgo composter, CraftComponent craft)
		{
			string[] array = new string[8];
			int num = 0;
			string text = "id={0}, interactable={1}, ";
			object obj;
			if (composter == null)
			{
				obj = null;
			}
			else
			{
				WgoData data = composter.Data;
				obj = ((data != null) ? data.id : null);
			}
			bool? flag;
			if (composter == null)
			{
				flag = null;
			}
			else
			{
				WgoData data2 = composter.Data;
				flag = ((data2 != null) ? new bool?(data2.IsInteractable) : null);
			}
			array[num] = string.Format(text, obj, flag);
			array[1] = "handler=";
			int num2 = 2;
			string text2;
			if (composter == null)
			{
				text2 = null;
			}
			else
			{
				IWGOInteractionHandler interactionHandler = composter.InteractionHandler;
				text2 = ((interactionHandler != null) ? interactionHandler.GetType().Name : null);
			}
			array[num2] = text2 ?? "null";
			array[3] = ", ";
			int num3 = 4;
			string text3 = "started={0}, queue={1}, ";
			object obj2 = ((craft != null) ? new bool?(craft.IsStarted) : null);
			int? num4;
			if (craft == null)
			{
				num4 = null;
			}
			else
			{
				List<CraftElementBase> craftElementsQueue = craft.CraftElementsQueue;
				num4 = ((craftElementsQueue != null) ? new int?(craftElementsQueue.Count) : null);
			}
			int? num5 = num4;
			array[num3] = string.Format(text3, obj2, num5.GetValueOrDefault());
			array[5] = string.Format("preFinish={0}, held={1}, ", (craft != null) ? new bool?(craft.HasPreFinishUpdate) : null, (craft != null) ? new bool?(craft.IsPreFinishHeld) : null);
			array[6] = string.Format("destroying={0}, status={1}, ", (craft != null) ? new bool?(craft.IsDestroyingCraftActive) : null, (craft != null) ? new CraftComponentStatus?(craft.Status) : null);
			int num6 = 7;
			string text4 = "available={0}, inputs={1}";
			int? num7;
			if (craft == null)
			{
				num7 = null;
			}
			else
			{
				List<CraftDefBase> availableCrafts = craft.AvailableCrafts;
				num7 = ((availableCrafts != null) ? new int?(availableCrafts.Count) : null);
			}
			num5 = num7;
			object obj3 = num5.GetValueOrDefault();
			int? num8;
			if (craft == null)
			{
				num8 = null;
			}
			else
			{
				List<CraftDefBase> craftsIn = craft.CraftsIn;
				num8 = ((craftsIn != null) ? new int?(craftsIn.Count) : null);
			}
			num5 = num8;
			array[num6] = string.Format(text4, obj3, num5.GetValueOrDefault());
			return string.Concat(array);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00005B34 File Offset: 0x00003D34
		private static string DescribeGardenBedState(Wgo gardenBed, CraftComponent craft)
		{
			string[] array = new string[9];
			int num = 0;
			string text = "id={0}, interactable={1}, ";
			object obj;
			if (gardenBed == null)
			{
				obj = null;
			}
			else
			{
				WgoData data = gardenBed.Data;
				obj = ((data != null) ? data.id : null);
			}
			object obj2 = obj ?? "null";
			bool? flag;
			if (gardenBed == null)
			{
				flag = null;
			}
			else
			{
				WgoData data2 = gardenBed.Data;
				flag = ((data2 != null) ? new bool?(data2.IsInteractable) : null);
			}
			array[num] = string.Format(text, obj2, flag);
			array[1] = "handler=";
			int num2 = 2;
			string text2;
			if (gardenBed == null)
			{
				text2 = null;
			}
			else
			{
				IWGOInteractionHandler interactionHandler = gardenBed.InteractionHandler;
				text2 = ((interactionHandler != null) ? interactionHandler.GetType().Name : null);
			}
			array[num2] = text2 ?? "null";
			array[3] = ", ";
			int num3 = 4;
			string text3 = "started={0}, queue={1}, ";
			object obj3 = ((craft != null) ? new bool?(craft.IsStarted) : null);
			int? num4;
			if (craft == null)
			{
				num4 = null;
			}
			else
			{
				List<CraftElementBase> craftElementsQueue = craft.CraftElementsQueue;
				num4 = ((craftElementsQueue != null) ? new int?(craftElementsQueue.Count) : null);
			}
			int? num5 = num4;
			array[num3] = string.Format(text3, obj3, num5.GetValueOrDefault());
			array[5] = "current=";
			int num6 = 6;
			string text4;
			if (craft == null)
			{
				text4 = null;
			}
			else
			{
				CraftElementBase currentCraftElement = craft.CurrentCraftElement;
				text4 = ((currentCraftElement != null) ? currentCraftElement.CraftId : null);
			}
			array[num6] = text4 ?? "null";
			array[7] = ", worker=";
			int num7 = 8;
			string text5;
			if (gardenBed == null)
			{
				text5 = null;
			}
			else
			{
				WgoData data3 = gardenBed.Data;
				if (data3 == null)
				{
					text5 = null;
				}
				else
				{
					IWorker worker = data3.Worker;
					text5 = ((worker != null) ? worker.Id.ToString() : null);
				}
			}
			array[num7] = text5 ?? "null";
			return string.Concat(array);
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00005CB4 File Offset: 0x00003EB4
		private static bool IsEmptyGardenPlot(string id)
		{
			return !string.IsNullOrEmpty(id) && (id == "garden_empty" || id == "vineyard_empty" || id.StartsWith("garden_empty_", StringComparison.Ordinal) || id.StartsWith("vineyard_empty_", StringComparison.Ordinal));
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00005D04 File Offset: 0x00003F04
		private static bool IsComposter(Wgo wgo)
		{
			string text;
			if (wgo == null)
			{
				text = null;
			}
			else
			{
				WgoData data = wgo.Data;
				text = ((data != null) ? data.id : null);
			}
			string text2 = text;
			return !string.IsNullOrEmpty(text2) && (text2.IndexOf("compost", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("composter", StringComparison.OrdinalIgnoreCase) >= 0);
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00005D58 File Offset: 0x00003F58
		private static bool IsGardenBed(Wgo wgo)
		{
			string text;
			if (wgo == null)
			{
				text = null;
			}
			else
			{
				WgoData data = wgo.Data;
				if (data == null)
				{
					text = null;
				}
				else
				{
					WGODef definition = data.Definition;
					text = ((definition != null) ? definition.wgoGroup : null);
				}
			}
			string text2 = text;
			return text2 == "garden_bed" || text2 == "vineyard_objects";
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00005DA4 File Offset: 0x00003FA4
		private static float HorizontalDistance(Vector3 a, Vector3 b)
		{
			return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000054 RID: 84 RVA: 0x00005DCD File Offset: 0x00003FCD
		// (set) Token: 0x06000055 RID: 85 RVA: 0x00005DD4 File Offset: 0x00003FD4
		internal static KeeperCheatMenuPlugin Instance { get; private set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000056 RID: 86 RVA: 0x00005DDC File Offset: 0x00003FDC
		internal bool InstantActionsEnabled
		{
			get
			{
				ConfigEntry<bool> instantActions = this._instantActions;
				return instantActions != null && instantActions.Value;
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000057 RID: 87 RVA: 0x00005DEF File Offset: 0x00003FEF
		internal bool SleepAlwaysEnabled
		{
			get
			{
				ConfigEntry<bool> sleepAlways = this._sleepAlways;
				return sleepAlways != null && sleepAlways.Value;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000058 RID: 88 RVA: 0x00005E02 File Offset: 0x00004002
		internal bool SleepWithoutSavingEnabled
		{
			get
			{
				ConfigEntry<bool> sleepWithoutSaving = this._sleepWithoutSaving;
				return sleepWithoutSaving != null && sleepWithoutSaving.Value;
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000059 RID: 89 RVA: 0x00005E15 File Offset: 0x00004015
		internal float HarvestYieldMultiplier
		{
			get
			{
				ConfigEntry<float> harvestYieldMultiplier = this._harvestYieldMultiplier;
				if (harvestYieldMultiplier == null)
				{
					return 1f;
				}
				return harvestYieldMultiplier.Value;
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600005A RID: 90 RVA: 0x00005E2C File Offset: 0x0000402C
		internal float GratitudeMultiplier
		{
			get
			{
				ConfigEntry<float> gratitudeMultiplier = this._gratitudeMultiplier;
				if (gratitudeMultiplier == null)
				{
					return 1f;
				}
				return gratitudeMultiplier.Value;
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600005B RID: 91 RVA: 0x00005E43 File Offset: 0x00004043
		internal bool FreeBuildEnabled
		{
			get
			{
				ConfigEntry<bool> freeBuild = this._freeBuild;
				return freeBuild != null && freeBuild.Value;
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x0600005C RID: 92 RVA: 0x00005E56 File Offset: 0x00004056
		internal bool UnrestrictedBuildEnabled
		{
			get
			{
				ConfigEntry<bool> unrestrictedBuild = this._unrestrictedBuild;
				return unrestrictedBuild != null && unrestrictedBuild.Value;
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600005D RID: 93 RVA: 0x00005E69 File Offset: 0x00004069
		internal float SermonSpeedMultiplier
		{
			get
			{
				ConfigEntry<float> sermonSpeedMultiplier = this._sermonSpeedMultiplier;
				if (sermonSpeedMultiplier == null)
				{
					return 1f;
				}
				return sermonSpeedMultiplier.Value;
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600005E RID: 94 RVA: 0x00005E80 File Offset: 0x00004080
		internal bool SharedStorageEnabled
		{
			get
			{
				ConfigEntry<bool> sharedStorage = this._sharedStorage;
				return sharedStorage != null && sharedStorage.Value;
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600005F RID: 95 RVA: 0x00005E93 File Offset: 0x00004093
		internal bool AutoUseLaddersEnabled
		{
			get
			{
				ConfigEntry<bool> autoUseLadders = this._autoUseLadders;
				return autoUseLadders != null && autoUseLadders.Value;
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000060 RID: 96 RVA: 0x00005EA6 File Offset: 0x000040A6
		internal float LadderClimbSpeedMultiplier
		{
			get
			{
				ConfigEntry<float> ladderClimbSpeedMultiplier = this._ladderClimbSpeedMultiplier;
				if (ladderClimbSpeedMultiplier == null)
				{
					return 1f;
				}
				return ladderClimbSpeedMultiplier.Value;
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000061 RID: 97 RVA: 0x00005EBD File Offset: 0x000040BD
		internal bool FreeCraftingEnabled
		{
			get
			{
				ConfigEntry<bool> freeCrafting = this._freeCrafting;
				return freeCrafting != null && freeCrafting.Value;
			}
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000062 RID: 98 RVA: 0x00005ED0 File Offset: 0x000040D0
		internal bool FreeBuildingCostsEnabled
		{
			get
			{
				ConfigEntry<bool> freeBuildingCosts = this._freeBuildingCosts;
				return freeBuildingCosts != null && freeBuildingCosts.Value;
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00005EE3 File Offset: 0x000040E3
		internal float MachineSpeedMultiplier
		{
			get
			{
				ConfigEntry<float> machineSpeedMultiplier = this._machineSpeedMultiplier;
				if (machineSpeedMultiplier == null)
				{
					return 1f;
				}
				return machineSpeedMultiplier.Value;
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000064 RID: 100 RVA: 0x00005EFA File Offset: 0x000040FA
		internal float CraftedOutputMultiplier
		{
			get
			{
				ConfigEntry<float> craftedOutputMultiplier = this._craftedOutputMultiplier;
				if (craftedOutputMultiplier == null)
				{
					return 1f;
				}
				return craftedOutputMultiplier.Value;
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000065 RID: 101 RVA: 0x00005F11 File Offset: 0x00004111
		internal bool AlchemyFolioCompanionEnabled
		{
			get
			{
				ConfigEntry<bool> alchemyFolioCompanion = this._alchemyFolioCompanion;
				return alchemyFolioCompanion != null && alchemyFolioCompanion.Value;
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000066 RID: 102 RVA: 0x00005F24 File Offset: 0x00004124
		internal bool AlchemyFolioCraftingEnabled
		{
			get
			{
				ConfigEntry<bool> alchemyFolioCrafting = this._alchemyFolioCrafting;
				return alchemyFolioCrafting != null && alchemyFolioCrafting.Value;
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000067 RID: 103 RVA: 0x00005F37 File Offset: 0x00004137
		internal bool RetainAlchemyIngredientsEnabled
		{
			get
			{
				ConfigEntry<bool> retainAlchemyIngredients = this._retainAlchemyIngredients;
				return retainAlchemyIngredients != null && retainAlchemyIngredients.Value;
			}
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000068 RID: 104 RVA: 0x00005F4A File Offset: 0x0000414A
		internal bool FreeResearchEnabled
		{
			get
			{
				ConfigEntry<bool> freeResearch = this._freeResearch;
				return freeResearch != null && freeResearch.Value;
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000069 RID: 105 RVA: 0x00005F5D File Offset: 0x0000415D
		internal bool SermonEveryDayEnabled
		{
			get
			{
				ConfigEntry<bool> sermonEveryDay = this._sermonEveryDay;
				return sermonEveryDay != null && sermonEveryDay.Value;
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600006A RID: 106 RVA: 0x00005F70 File Offset: 0x00004170
		internal bool SermonRepeatableEnabled
		{
			get
			{
				ConfigEntry<bool> sermonRepeatable = this._sermonRepeatable;
				return sermonRepeatable != null && sermonRepeatable.Value;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600006B RID: 107 RVA: 0x00005F83 File Offset: 0x00004183
		internal bool ZombiesEveryDayEnabled
		{
			get
			{
				ConfigEntry<bool> zombiesEveryDay = this._zombiesEveryDay;
				return zombiesEveryDay != null && zombiesEveryDay.Value;
			}
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x0600006C RID: 108 RVA: 0x00005F96 File Offset: 0x00004196
		internal bool ZombiesRepeatableEnabled
		{
			get
			{
				ConfigEntry<bool> zombiesRepeatable = this._zombiesRepeatable;
				return zombiesRepeatable != null && zombiesRepeatable.Value;
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600006D RID: 109 RVA: 0x00005FA9 File Offset: 0x000041A9
		internal bool MapTeleportEverywhereEnabled
		{
			get
			{
				ConfigEntry<bool> mapTeleportEverywhere = this._mapTeleportEverywhere;
				return mapTeleportEverywhere != null && mapTeleportEverywhere.Value;
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600006E RID: 110 RVA: 0x00005FBC File Offset: 0x000041BC
		internal float ZombieMoveSpeedMultiplier
		{
			get
			{
				ConfigEntry<float> zombieMoveSpeedMultiplier = this._zombieMoveSpeedMultiplier;
				if (zombieMoveSpeedMultiplier == null)
				{
					return 1f;
				}
				return zombieMoveSpeedMultiplier.Value;
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x0600006F RID: 111 RVA: 0x00005FD3 File Offset: 0x000041D3
		internal float ZombieWorkSpeedMultiplier
		{
			get
			{
				ConfigEntry<float> zombieWorkSpeedMultiplier = this._zombieWorkSpeedMultiplier;
				if (zombieWorkSpeedMultiplier == null)
				{
					return 1f;
				}
				return zombieWorkSpeedMultiplier.Value;
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000070 RID: 112 RVA: 0x00005FEA File Offset: 0x000041EA
		internal float ZombieRedExperienceMultiplier
		{
			get
			{
				ConfigEntry<float> zombieRedExperienceMultiplier = this._zombieRedExperienceMultiplier;
				if (zombieRedExperienceMultiplier == null)
				{
					return 1f;
				}
				return zombieRedExperienceMultiplier.Value;
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000071 RID: 113 RVA: 0x00006001 File Offset: 0x00004201
		internal float ZombieGreenExperienceMultiplier
		{
			get
			{
				ConfigEntry<float> zombieGreenExperienceMultiplier = this._zombieGreenExperienceMultiplier;
				if (zombieGreenExperienceMultiplier == null)
				{
					return 1f;
				}
				return zombieGreenExperienceMultiplier.Value;
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000072 RID: 114 RVA: 0x00006018 File Offset: 0x00004218
		internal float ZombieBlueExperienceMultiplier
		{
			get
			{
				ConfigEntry<float> zombieBlueExperienceMultiplier = this._zombieBlueExperienceMultiplier;
				if (zombieBlueExperienceMultiplier == null)
				{
					return 1f;
				}
				return zombieBlueExperienceMultiplier.Value;
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000073 RID: 115 RVA: 0x0000602F File Offset: 0x0000422F
		internal bool ZombieCollectFinishedProductsEnabled
		{
			get
			{
				ConfigEntry<bool> zombieCollectFinishedProducts = this._zombieCollectFinishedProducts;
				return zombieCollectFinishedProducts != null && zombieCollectFinishedProducts.Value;
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000074 RID: 116 RVA: 0x00006042 File Offset: 0x00004242
		internal float FishPondStockMultiplier
		{
			get
			{
				ConfigEntry<float> fishPondStockMultiplier = this._fishPondStockMultiplier;
				if (fishPondStockMultiplier == null)
				{
					return 1f;
				}
				return fishPondStockMultiplier.Value;
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000075 RID: 117 RVA: 0x00006059 File Offset: 0x00004259
		internal bool InstantFishingBiteEnabled
		{
			get
			{
				ConfigEntry<bool> instantFishingBite = this._instantFishingBite;
				return instantFishingBite != null && instantFishingBite.Value;
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000076 RID: 118 RVA: 0x0000606C File Offset: 0x0000426C
		internal bool AutoReelFishingEnabled
		{
			get
			{
				ConfigEntry<bool> autoReelFishing = this._autoReelFishing;
				return autoReelFishing != null && autoReelFishing.Value;
			}
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00006080 File Offset: 0x00004280
		private void Awake()
		{
			KeeperCheatMenuPlugin.Instance = this;
			this._menuKey = base.Config.Bind<KeyboardShortcut>("General", "Menu shortcut", new KeyboardShortcut(KeyCode.F6, Array.Empty<KeyCode>()), "Opens or closes Keeper Cheat Menu.");
			this._language = base.Config.Bind<string>("General", "Language", "English", "Keeper Cheat Menu language: English, German, Korean, Japanese, Chinese, Brazilian Portuguese, or European Portuguese.");
			this._uiScale = base.Config.Bind<float>("General", "UI scale", 0f, "Menu scale multiplier. 0 = automatic whole-pixel scaling (sharpest); set e.g. 1.25 or 1.5 to enlarge the menu.");
			this._pixelPerfectUi = base.Config.Bind<bool>("General", "Pixel perfect UI", true, "Snaps the menu to whole screen pixels so text and artwork stop blurring.");
			this._fontMode = base.Config.Bind<string>("General", "Font mode", "Auto", "Auto = keep the game pixel font unless its atlas is low resolution, then use a crisp system SDF font; Game = always the game font; System = always the crisp system SDF font.");
			this._panelArtwork = base.Config.Bind<bool>("General", "Panel artwork", true, "Draws the pixel-art panel frames. Disable for flat panels that stay sharp at any scale.");
			this._infiniteEnergy = base.Config.Bind<bool>("Player", "Infinite energy", false, "Continuously restores the player's energy.");
			this._infiniteStamina = base.Config.Bind<bool>("Player", "Infinite stamina", false, "Continuously restores combat stamina.");
			this._invulnerable = base.Config.Bind<bool>("Player", "Invulnerable", false, "Prevents damage to the player while enabled.");
			this._instantActions = base.Config.Bind<bool>("Player", "Instant actions", false, "Makes manual player craft and tool actions finish in one action.");
			this._sleepAlways = base.Config.Bind<bool>("Player", "Always allow sleep", false, "Allows sleeping at full energy and continues sleeping until the wake-up hotkey is pressed.");
			this._sleepWithoutSaving = base.Config.Bind<bool>("Player", "Sleep without saving", false, "Prevents the normal automatic save when the player wakes up.");
			this._alwaysMaxTownGratitude = base.Config.Bind<bool>("Player", "Always maximum town gratitude", false, "Continuously fills town gratitude to the maximum allowed by the current town quality.");
			this._harvestYieldMultiplier = base.Config.Bind<float>("Player", "Harvest yield multiplier", 1f, "Multiplier applied to item stacks produced by harvesting crops and manually gathering resources. Allowed range: 0.1x to 100x.");
			this._gratitudeMultiplier = base.Config.Bind<float>("Player", "Gratitude multiplier", 1f, "Multiplier applied to positive town-gratitude rewards. Costs and negative changes are unchanged. Allowed range: 0.1x to 100x.");
			this._wakeUpHotkey = base.Config.Bind<KeyCode>("Player", "Wake-up hotkey", KeyCode.F7, "Wakes the player while Always allow sleep is enabled.");
			this._harvestAdjacentCrops = base.Config.Bind<bool>("Misc", "Harvest adjacent crops", false, "Harvests touching ready crops of the same type after the player harvests one crop.");
			this._freeBuild = base.Config.Bind<bool>("Misc", "Free build placement", false, "Allows continuous building placement without snapping to the construction grid. Normal collision, area and resource checks remain active.");
			this._unrestrictedBuild = base.Config.Bind<bool>("Misc", "No placement restrictions", false, "Allows building outside construction zones and ignores placement obstacles. Material costs remain active.");
			this._sermonSpeedMultiplier = base.Config.Bind<float>("Misc", "Sermon speed multiplier", 1f, "Speeds up the sermon sequence. Allowed range: 1x to 20x.");
			this._sharedStorage = base.Config.Bind<bool>("Misc", "Shared storage", false, "Allows crafting stations to use items and fishing to use bait from eligible storage containers across all areas.");
			this._autoUseLadders = base.Config.Bind<bool>("Misc", "Auto use ladders", false, "Automatically starts climbing when the player enters a ladder interaction point.");
			this._ladderClimbSpeedMultiplier = base.Config.Bind<float>("Misc", "Ladder climb speed multiplier", 1f, "Multiplier applied when a ladder climb starts. Allowed range: 0.1x to 20x.");
			this._freeCrafting = base.Config.Bind<bool>("Crafting", "Craft items for free", false, "Ignores material requirements for crafting while keeping tools, unlocks and other restrictions.");
			this._freeBuildingCosts = base.Config.Bind<bool>("Crafting", "Build stuff for free", false, "Ignores material requirements for buildings while keeping building limits and placement rules.");
			this._machineSpeedMultiplier = base.Config.Bind<float>("Crafting", "Machine speed multiplier", 1f, "Multiplier applied to automatic machine crafting speed. Allowed range: 0.1x to 100x.");
			this._craftedOutputMultiplier = base.Config.Bind<float>("Crafting", "Crafted item multiplier", 1f, "Multiplier applied to item stacks produced by crafts. Allowed range: 0.1x to 100x.");
			this._alchemyFolioCompanion = base.Config.Bind<bool>("Alchemy", "Show folio beside alchemy table", false, "Opens an interactive folio beside the alchemy table and moves the laboratory window to the left.");
			this._alchemyFolioCrafting = base.Config.Bind<bool>("Alchemy", "Folio ingredient selection", false, "Clicking an available alchemy ingredient in the companion folio places it in the first free alchemy-table slot.");
			this._retainAlchemyIngredients = base.Config.Bind<bool>("Alchemy", "Keep selected ingredients", false, "Restores selected alchemy ingredients when the table is reopened while enough of each ingredient remains accessible.");
			this._freeResearch = base.Config.Bind<bool>("Alchemy", "Free research", false, "Allows study-table research without consuming the researched item, science, faith, or other requirements.");
			this._sermonEveryDay = base.Config.Bind<bool>("World", "Sermon every day", false, "Allows one sermon on every in-game day instead of only the normal sermon day.");
			this._sermonRepeatable = base.Config.Bind<bool>("World", "Repeat sermons", false, "Allows sermons to be performed repeatedly without waiting for another in-game day.");
			this._zombiesEveryDay = base.Config.Bind<bool>("World", "Make zombies every day", false, "Starts prepared zombie resurrections at the beginning of every in-game day.");
			this._zombiesRepeatable = base.Config.Bind<bool>("World", "Repeat zombie making", false, "Starts a prepared zombie resurrection immediately so another can be prepared on the same day.");
			this._mapTeleportEverywhere = base.Config.Bind<bool>("World", "Map teleport from everywhere", false, "Makes activated map beacons selectable when the map is opened away from a beacon.");
			this._lastEverydaySermonDay = base.Config.Bind<int>("World", "Last everyday sermon day", -1, "Internal day marker used to keep Sermon every day limited to once per day.");
			this._fishOverlayEnabled = base.Config.Bind<bool>("Misc", "Nearby fishing overlay", false, "Shows fish stock and required bait while the player is near a fishing reservoir.");
			this._fishOverlayLocked = base.Config.Bind<bool>("Misc", "Fishing overlay locked", true, "Locks the fishing overlay position.");
			this._fishOverlayPositionX = base.Config.Bind<float>("Misc", "Fishing overlay X", 48f, "Horizontal fishing overlay position in reference UI pixels.");
			this._fishOverlayPositionY = base.Config.Bind<float>("Misc", "Fishing overlay Y", 420f, "Vertical fishing overlay position in reference UI pixels from the top.");
			this._fishPondStockMultiplier = base.Config.Bind<float>("Misc", "Fish pond stock multiplier", 1f, "Multiplier for each fish species' maximum stock in fishing ponds. Allowed range: 1x to 100x.");
			this._instantFishingBite = base.Config.Bind<bool>("Misc", "Instant fishing bite", false, "Makes a fish bite immediately after the fishing cast begins.");
			this._autoReelFishing = base.Config.Bind<bool>("Misc", "Auto reel fishing", false, "Automatically hooks and successfully reels in a biting fish.");
			this._finishGrowingRange = base.Config.Bind<int>("Misc", "Finish growing range", 8, "World-space radius used by the finish-growing action.");
			this._finishGrowingHotkey = base.Config.Bind<KeyCode>("Misc", "Finish growing hotkey", KeyCode.None, "Hotkey that instantly finishes growing crops inside the selected range.");
			this._finishCraftsHotkey = base.Config.Bind<KeyCode>("Misc", "Finish crafts hotkey", KeyCode.None, "Hotkey that instantly finishes active machines inside the selected range.");
			this._regrowForageHotkey = base.Config.Bind<KeyCode>("Misc", "Regrow forage hotkey", KeyCode.None, "Hotkey that regrows harvested forage inside the selected range.");
			this._zombieMoveSpeedMultiplier = base.Config.Bind<float>("Zombies", "Movement speed multiplier", 1f, "Multiplier applied to zombie movement speed.");
			this._zombieWorkSpeedMultiplier = base.Config.Bind<float>("Zombies", "Work speed multiplier", 1f, "Multiplier applied to zombie work actions.");
			this._zombieRedExperienceMultiplier = base.Config.Bind<float>("Zombies", "Red experience multiplier", 1f, "Multiplier applied to red technology experience earned by zombies.");
			this._zombieGreenExperienceMultiplier = base.Config.Bind<float>("Zombies", "Green experience multiplier", 1f, "Multiplier applied to green technology experience earned by zombies.");
			this._zombieBlueExperienceMultiplier = base.Config.Bind<float>("Zombies", "Blue experience multiplier", 1f, "Multiplier applied to blue technology experience earned by zombies.");
			this._zombieCollectFinishedProducts = base.Config.Bind<bool>("Zombies", "Collect finished products", false, "Lets crafter zombies collect their own finished products and immediately continue the next queued cycle. If their inventory cannot hold the output, the normal pickup order remains active.");
			this._savedLocationsJson = base.Config.Bind<string>("Teleport", "Saved locations", string.Empty, "Saved named teleport locations. Managed by the in-game menu.");
			this._teleportHotbarSlots = new ConfigEntry<string>[4];
			for (int i = 0; i < this._teleportHotbarSlots.Length; i++)
			{
				this._teleportHotbarSlots[i] = base.Config.Bind<string>("Teleport", string.Format("Hotbar slot {0}", i + 1), string.Empty, string.Format("Saved location assigned to teleport hotbar slot {0}.", i + 1));
			}
			this._harmony = new Harmony("Narodum.gk2.keepercheatmenu");
			this._harmony.PatchAll(typeof(KeeperCheatMenuPlugin).Assembly);
			base.Logger.LogInfo(string.Format("{0} {1} loaded. Press {2} in game.", "Keeper Cheat Menu", "0.22.2", this._menuKey.Value));
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00006834 File Offset: 0x00004A34
		private void OnDestroy()
		{
			if (this._savedLocationsLoaded)
			{
				this.PersistSavedLocations();
			}
			this.RestoreImmunity();
			this.RestoreAlchemyFolioLayout(true);
			this.RestoreSermonSpeed();
			this.DestroyNativeMenu();
			this.DestroyFishingOverlay();
			Harmony harmony = this._harmony;
			if (harmony != null)
			{
				harmony.UnpatchAll(harmony.Id);
			}
			this._harmony = null;
			KeeperCheatMenuPlugin.Instance = null;
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00006890 File Offset: 0x00004A90
		private void Update()
		{
			this.UpdateMenuCanvasScale();
			if (this.UpdateRangeActionHotkeyCapture())
			{
				return;
			}
			KeyboardShortcut value = this._menuKey.Value;
			bool flag;
			if (!value.IsDown())
			{
				if (value.MainKey != KeyCode.None && Input.GetKeyDown(value.MainKey))
				{
					IEnumerable<KeyCode> modifiers = value.Modifiers;
					Func<KeyCode, bool> func;
					if ((func = KeeperCheatMenuPlugin.__OCache.__f2_GetKey) == null)
					{
						func = (KeeperCheatMenuPlugin.__OCache.__f2_GetKey = new Func<KeyCode, bool>(Input.GetKey));
					}
					flag = modifiers.All<KeyCode>(func);
				}
				else
				{
					flag = false;
				}
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				this.SetNativeMenuVisible(!this._show);
			}
			this.UpdateFishingOverlay();
			this.UpdateAlchemyFolioCompanion();
			this.ProcessPendingAutomaticLadderUse();
			if (!this._show && this._finishGrowingHotkey.Value != KeyCode.None && Input.GetKeyDown(this._finishGrowingHotkey.Value))
			{
				this.FinishGrowingCropsInRange(false);
			}
			if (!this._show && this._finishCraftsHotkey.Value != KeyCode.None && Input.GetKeyDown(this._finishCraftsHotkey.Value))
			{
				this.FinishCraftsInRange(false);
			}
			if (!this._show && this._regrowForageHotkey.Value != KeyCode.None && Input.GetKeyDown(this._regrowForageHotkey.Value))
			{
				this.RegrowForageInRange(false);
			}
			PlayerData playerData = KeeperCheatMenuPlugin.SafePlayer();
			if (this._sleepAlways.Value && this._wakeUpHotkey.Value != KeyCode.None && playerData != null)
			{
				EnergySystem energySystem = playerData.energySystem;
				bool? flag2 = ((energySystem != null) ? new bool?(energySystem.IsSleeping) : null);
				bool flag3 = true;
				if (((flag2.GetValueOrDefault() == flag3) & (flag2 != null)) && Input.GetKeyDown(this._wakeUpHotkey.Value))
				{
					KeeperCheatMenuPlugin.FillResource(playerData, "energy");
					if (KeeperCheatMenuPlugin.RemainingSleepTimeField != null)
					{
						KeeperCheatMenuPlugin.RemainingSleepTimeField.SetValue(playerData.energySystem, 0f);
					}
					else
					{
						playerData.energySystem.StopSleeping();
					}
				}
			}
			PlayerData playerData2 = playerData;
			this.UpdateInvulnerability(playerData2);
			if (playerData2 == null || Time.unscaledTime < this._nextRefill)
			{
				return;
			}
			this._nextRefill = Time.unscaledTime + 0.15f;
			try
			{
				if (this._infiniteEnergy.Value)
				{
					KeeperCheatMenuPlugin.FillResource(playerData2, "energy");
				}
				if (this._infiniteStamina.Value)
				{
					KeeperCheatMenuPlugin.FillResource(playerData2, "stamina");
				}
				if (this._alwaysMaxTownGratitude.Value)
				{
					KeeperCheatMenuPlugin.FillTownGratitudeToCurrentMaximum(playerData2);
				}
			}
			catch (Exception ex)
			{
				base.Logger.LogWarning("Could not refill a player resource: " + ex.Message);
			}
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00006B04 File Offset: 0x00004D04
		private void OnGUI()
		{
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00006B08 File Offset: 0x00004D08
		private void DrawWindow(int id)
		{
			GUILayout.BeginVertical(Array.Empty<GUILayoutOption>());
			GUILayout.Label("Player", this._headerStyle, Array.Empty<GUILayoutOption>());
			bool flag = GUILayout.Toggle(this._infiniteEnergy.Value, "Infinite energy", Array.Empty<GUILayoutOption>());
			if (flag != this._infiniteEnergy.Value)
			{
				this._infiniteEnergy.Value = flag;
			}
			bool flag2 = GUILayout.Toggle(this._infiniteStamina.Value, "Infinite combat stamina", Array.Empty<GUILayoutOption>());
			if (flag2 != this._infiniteStamina.Value)
			{
				this._infiniteStamina.Value = flag2;
			}
			bool flag3 = GUILayout.Toggle(this._invulnerable.Value, "Invulnerable", Array.Empty<GUILayoutOption>());
			if (flag3 != this._invulnerable.Value)
			{
				this._invulnerable.Value = flag3;
			}
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			if (GUILayout.Button("Refill energy", new GUILayoutOption[] { GUILayout.Height(28f) }))
			{
				this.RunWithPlayer(delegate(PlayerData p)
				{
					KeeperCheatMenuPlugin.FillResource(p, "energy");
				}, "Energy refilled.");
			}
			if (GUILayout.Button("Refill stamina", new GUILayoutOption[] { GUILayout.Height(28f) }))
			{
				this.RunWithPlayer(delegate(PlayerData p)
				{
					KeeperCheatMenuPlugin.FillResource(p, "stamina");
				}, "Stamina refilled.");
			}
			if (GUILayout.Button("Restore health", new GUILayoutOption[] { GUILayout.Height(28f) }))
			{
				Action<PlayerData> action;
				if ((action = KeeperCheatMenuPlugin.__OCache.__f3_RestoreHealth) == null)
				{
					action = (KeeperCheatMenuPlugin.__OCache.__f3_RestoreHealth = new Action<PlayerData>(KeeperCheatMenuPlugin.RestoreHealth));
				}
				this.RunWithPlayer(action, "Health restored.");
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(8f);
			GUILayout.Label("Currency and technology", this._headerStyle, Array.Empty<GUILayoutOption>());
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label("Amount", new GUILayoutOption[] { GUILayout.Width(58f) });
			this._moneyAmount = GUILayout.TextField(this._moneyAmount, new GUILayoutOption[] { GUILayout.Width(110f) });
			if (GUILayout.Button("Add money", new GUILayoutOption[] { GUILayout.Height(25f) }))
			{
				this.AddResourceFromText("money", this._moneyAmount, "money");
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label("Amount", new GUILayoutOption[] { GUILayout.Width(58f) });
			this._techAmount = GUILayout.TextField(this._techAmount, new GUILayoutOption[] { GUILayout.Width(110f) });
			if (GUILayout.Button("Add red", new GUILayoutOption[] { GUILayout.Height(25f) }))
			{
				this.AddResourceFromText("tech_red", this._techAmount, "red technology points");
			}
			if (GUILayout.Button("Add green", new GUILayoutOption[] { GUILayout.Height(25f) }))
			{
				this.AddResourceFromText("tech_green", this._techAmount, "green technology points");
			}
			if (GUILayout.Button("Add blue", new GUILayoutOption[] { GUILayout.Height(25f) }))
			{
				this.AddResourceFromText("tech_blue", this._techAmount, "blue technology points");
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(8f);
			GUILayout.Label("Item spawner", this._headerStyle, Array.Empty<GUILayoutOption>());
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label("Search", new GUILayoutOption[] { GUILayout.Width(58f) });
			this._itemSearch = GUILayout.TextField(this._itemSearch, Array.Empty<GUILayoutOption>());
			GUILayout.Label("Qty", new GUILayoutOption[] { GUILayout.Width(28f) });
			this._itemQuantity = GUILayout.TextField(this._itemQuantity, new GUILayoutOption[] { GUILayout.Width(54f) });
			GUILayout.EndHorizontal();
			this.RefreshItemListIfNeeded();
			GUILayout.Label(string.Format("{0} matching items. Showing the first 100.", this._filteredItems.Count), this._noteStyle, Array.Empty<GUILayoutOption>());
			this._itemScroll = GUILayout.BeginScrollView(this._itemScroll, GUI.skin.box, new GUILayoutOption[] { GUILayout.Height(265f) });
			foreach (ItemDef itemDef in this._filteredItems.Take<ItemDef>(100))
			{
				GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				GUILayout.Label(KeeperCheatMenuPlugin.ItemLabel(itemDef), new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
				if (GUILayout.Button("Add", new GUILayoutOption[] { GUILayout.Width(64f) }))
				{
					this.SpawnItem(itemDef.id);
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();
			GUILayout.Space(6f);
			GUILayout.Label(this._status, this._noteStyle, Array.Empty<GUILayoutOption>());
			GUILayout.Label("Quest and story-state editing is intentionally excluded from this early build.", this._noteStyle, Array.Empty<GUILayoutOption>());
			if (GUILayout.Button("Close (F6)", new GUILayoutOption[] { GUILayout.Height(28f) }))
			{
				this._show = false;
			}
			GUILayout.EndVertical();
			GUI.DragWindow(new Rect(0f, 0f, 10000f, 26f));
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00007068 File Offset: 0x00005268
		private void EnsureStyles()
		{
			if (this._headerStyle != null)
			{
				return;
			}
			this._headerStyle = new GUIStyle(GUI.skin.label)
			{
				fontStyle = FontStyle.Bold,
				fontSize = 15,
				normal = 
				{
					textColor = new Color(0.95f, 0.84f, 0.45f)
				}
			};
			this._noteStyle = new GUIStyle(GUI.skin.label)
			{
				wordWrap = true,
				normal = 
				{
					textColor = new Color(0.84f, 0.84f, 0.84f)
				}
			};
		}

		// Token: 0x0600007D RID: 125 RVA: 0x000070FC File Offset: 0x000052FC
		private static PlayerData SafePlayer()
		{
			PlayerData playerData;
			try
			{
				playerData = MainGame.PlayerData;
			}
			catch
			{
				playerData = null;
			}
			return playerData;
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00007128 File Offset: 0x00005328
		private void RunWithPlayer(Action<PlayerData> action, string success)
		{
			PlayerData playerData = KeeperCheatMenuPlugin.SafePlayer();
			if (playerData == null)
			{
				this._status = "No active player. Load or start a save first.";
				return;
			}
			try
			{
				action(playerData);
				this._status = success;
			}
			catch (Exception ex)
			{
				this._status = "Action failed: " + ex.Message;
				base.Logger.LogError(ex);
			}
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00007190 File Offset: 0x00005390
		private static void FillResource(PlayerData player, string resource)
		{
			GK2GameResSystem system = GK2GameResSystem.GetSystem(resource);
			float num = ((system != null && system.HasMax()) ? system.Max : 100f);
			player.SetRes(resource, num);
		}

		// Token: 0x06000080 RID: 128 RVA: 0x000071C5 File Offset: 0x000053C5
		private static void RestoreHealth(PlayerData player)
		{
			FieldInfo hpComponentField = KeeperCheatMenuPlugin.HpComponentField;
			HPComponent hpcomponent = ((hpComponentField != null) ? hpComponentField.GetValue(player) : null) as HPComponent;
			if (hpcomponent == null)
			{
				return;
			}
			hpcomponent.RestoreFullHp();
		}

		// Token: 0x06000081 RID: 129 RVA: 0x000071E8 File Offset: 0x000053E8
		private static void FillTownGratitudeToCurrentMaximum(PlayerData player)
		{
			if (player == null)
			{
				return;
			}
			float res = player.GetRes("happiness", 0f);
			MainGame instance = MainGame.Instance;
			int? num;
			if (instance == null)
			{
				num = null;
			}
			else
			{
				GameSave gameSave = instance.GameSave;
				if (gameSave == null)
				{
					num = null;
				}
				else
				{
					TownSystem townSystem = gameSave.townSystem;
					num = ((townSystem != null) ? new int?(townSystem.Quality) : null);
				}
			}
			int? num2 = num;
			float num3 = ((num2 != null) ? ((float)num2.GetValueOrDefault()) : 0f);
			if (num3 <= 0f)
			{
				GK2GameResSystem system = GK2GameResSystem.GetSystem("happiness");
				num3 = ((system != null && system.HasMax()) ? system.Max : res);
			}
			if (res + 0.001f < num3)
			{
				player.AddRes("happiness", num3 - res);
			}
		}

		// Token: 0x06000082 RID: 130 RVA: 0x000072B0 File Offset: 0x000054B0
		private void UpdateInvulnerability(PlayerData player)
		{
			HPComponent hpcomponent;
			if (player != null)
			{
				FieldInfo hpComponentField = KeeperCheatMenuPlugin.HpComponentField;
				hpcomponent = ((hpComponentField != null) ? hpComponentField.GetValue(player) : null) as HPComponent;
			}
			else
			{
				hpcomponent = null;
			}
			HPComponent hpcomponent2 = hpcomponent;
			if (!this._invulnerable.Value)
			{
				this.RestoreImmunity();
				return;
			}
			if (hpcomponent2 == null)
			{
				return;
			}
			if (this._protectedHp != hpcomponent2)
			{
				this.RestoreImmunity();
				this._protectedHp = hpcomponent2;
				this._originalImmunity = hpcomponent2.IsImmuneToDamage;
			}
			hpcomponent2.IsImmuneToDamage = true;
		}

		// Token: 0x06000083 RID: 131 RVA: 0x0000731C File Offset: 0x0000551C
		private void RestoreImmunity()
		{
			if (this._protectedHp == null)
			{
				return;
			}
			try
			{
				this._protectedHp.IsImmuneToDamage = this._originalImmunity;
			}
			catch
			{
			}
			this._protectedHp = null;
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00007360 File Offset: 0x00005560
		private void AddResourceFromText(string resource, string amountText, string displayName)
		{
			float amount;
			if (!float.TryParse(amountText, out amount) || amount <= 0f)
			{
				this._status = "Enter a positive number.";
				return;
			}
			this.RunWithPlayer(delegate(PlayerData p)
			{
				p.AddRes(resource, amount);
			}, string.Format("Added {0:0.##} {1}.", amount, displayName));
		}

		// Token: 0x06000085 RID: 133 RVA: 0x000073CC File Offset: 0x000055CC
		private void RefreshItemListIfNeeded()
		{
			string search = (this._itemSearch ?? string.Empty).Trim();
			if (string.Equals(search, this._lastSearch, StringComparison.Ordinal))
			{
				return;
			}
			this._lastSearch = search;
			try
			{
				GameBalance me = GameBalance.Me;
				IEnumerable<ItemDef> enumerable = ((me != null) ? me.itemDefs : null);
				IEnumerable<ItemDef> enumerable2 = enumerable ?? Enumerable.Empty<ItemDef>();
				this._filteredItems = (from i in enumerable2
					where i != null && !string.IsNullOrEmpty(i.id)
					where KeeperCheatMenuPlugin.ItemMatchesSearch(i, search, false)
					select i).OrderBy<ItemDef, string>((ItemDef i) => i.id, StringComparer.OrdinalIgnoreCase).ToList<ItemDef>();
			}
			catch (Exception ex)
			{
				this._filteredItems.Clear();
				this._status = "Could not read the item catalog: " + ex.Message;
			}
		}

		// Token: 0x06000086 RID: 134 RVA: 0x000074D8 File Offset: 0x000056D8
		private static string ItemLabel(ItemDef item)
		{
			string text;
			try
			{
				string header = item.GetHeader();
				text = ((string.IsNullOrWhiteSpace(header) || header == item.id) ? item.id : (header + "  [" + item.id + "]"));
			}
			catch
			{
				text = item.id;
			}
			return text;
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00007540 File Offset: 0x00005740
		private void SpawnItem(string itemId)
		{
			int count;
			if (!int.TryParse(this._itemQuantity, out count) || count < 1 || count > 9999)
			{
				this._status = "Item quantity must be between 1 and 9999.";
				return;
			}
			this.RunWithPlayer(delegate(PlayerData player)
			{
				List<Item> list = new ItemCount(itemId, count).CreateItems();
				if (!player.Inventory.AddItemsToInventory(list))
				{
					throw new InvalidOperationException("Not enough inventory space.");
				}
			}, string.Format(this.L("Added {0} x {1}."), count, KeeperCheatMenuPlugin.ItemDisplayName(itemId)));
		}

		// Token: 0x06000088 RID: 136 RVA: 0x000075BC File Offset: 0x000057BC
		internal void QueueAutomaticLadderUse(LadderInteractionHandler interaction, PlayerController player)
		{
			if (!this.AutoUseLaddersEnabled || this._show || interaction == null || player == null || Time.unscaledTime < this._nextAutomaticLadderUse)
			{
				return;
			}
			this._pendingAutomaticLadder = interaction;
			this._pendingAutomaticLadderPlayer = player;
			this._pendingAutomaticLadderFrame = Time.frameCount + 1;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00007610 File Offset: 0x00005810
		private void ProcessPendingAutomaticLadderUse()
		{
			if (this._pendingAutomaticLadder == null || Time.frameCount < this._pendingAutomaticLadderFrame)
			{
				return;
			}
			LadderInteractionHandler pendingAutomaticLadder = this._pendingAutomaticLadder;
			PlayerController pendingAutomaticLadderPlayer = this._pendingAutomaticLadderPlayer;
			this._pendingAutomaticLadder = null;
			this._pendingAutomaticLadderPlayer = null;
			if (!this.AutoUseLaddersEnabled || this._show || pendingAutomaticLadder == null || pendingAutomaticLadderPlayer == null || Time.unscaledTime < this._nextAutomaticLadderUse)
			{
				return;
			}
			try
			{
				LadderClimbController ladderClimbController = pendingAutomaticLadderPlayer.LadderClimbController;
				if (!(ladderClimbController == null) && !ladderClimbController.IsClimbActive && ladderClimbController.CanUse && pendingAutomaticLadder.HasInteraction(pendingAutomaticLadderPlayer))
				{
					this._nextAutomaticLadderUse = Time.unscaledTime + 0.4f;
					pendingAutomaticLadder.Interact(pendingAutomaticLadderPlayer);
				}
			}
			catch (Exception ex)
			{
				base.Logger.LogWarning("Could not automatically use ladder: " + ex.Message);
			}
		}

		// Token: 0x0600008A RID: 138 RVA: 0x000076F0 File Offset: 0x000058F0
		private void ToggleAutoUseLadders()
		{
			this._autoUseLadders.Value = !this._autoUseLadders.Value;
			base.Config.Save();
			this.RefreshMiscToggleLabel();
		}

		// Token: 0x0600008B RID: 139 RVA: 0x0000771C File Offset: 0x0000591C
		private void SetLadderClimbSpeedFromField()
		{
			float num = KeeperCheatMenuPlugin.ParseMultiplier(this._ladderClimbSpeedMultiplierInput, this._ladderClimbSpeedMultiplier.Value);
			this._ladderClimbSpeedMultiplier.Value = Mathf.Clamp(num, 0.1f, 20f);
			base.Config.Save();
			this.RefreshLadderClimbSpeedInput();
			this.SetNativeStatus("Ladder climb speed multiplier applied and saved.");
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00007778 File Offset: 0x00005978
		private void RefreshLadderClimbSpeedInput()
		{
			if (this._ladderClimbSpeedMultiplierInput != null)
			{
				this._ladderClimbSpeedMultiplierInput.text = Mathf.Clamp(this._ladderClimbSpeedMultiplier.Value, 0.1f, 20f).ToString("0.##", CultureInfo.InvariantCulture);
			}
		}

		// Token: 0x0600008D RID: 141 RVA: 0x000077CC File Offset: 0x000059CC
		private static Dictionary<string, string> CreateEuropeanPortugueseText()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(KeeperCheatMenuPlugin.BrazilianPortugueseText, StringComparer.Ordinal);
			dictionary["Items"] = "Objetos";
			dictionary["Crafting"] = "Fabrico";
			dictionary["Misc"] = "Diversos";
			dictionary["Zombies"] = "Zombies";
			dictionary["Search items..."] = "Procurar objetos...";
			dictionary["Infinite stamina"] = "Resistência infinita";
			dictionary["Sleep without saving"] = "Dormir sem guardar";
			dictionary["SAVE CURRENT LOCATION"] = "GUARDAR LOCALIZAÇÃO ATUAL";
			dictionary["Save current location"] = "Guardar localização atual";
			dictionary["SAVED LOCATIONS"] = "LOCALIZAÇÕES GUARDADAS";
			dictionary["Select a saved location"] = "Selecionar uma localização guardada";
			dictionary["Delete selected"] = "Eliminar seleção";
			dictionary["Shared storage for crafting"] = "Armazenamento partilhado para fabrico";
			dictionary["Craft items for free"] = "Fabricar objetos gratuitamente";
			dictionary["Build stuff for free"] = "Construir gratuitamente";
			dictionary["CRAFTING"] = "FABRICO";
			dictionary["CRAFTED ITEM MULTIPLIER"] = "MULTIPLICADOR DE OBJETOS FABRICADOS";
			dictionary["Auto use ladders"] = "Usar escadas automaticamente";
			dictionary["Ladder climb speed"] = "Velocidade de subida";
			dictionary["Supply missing items"] = "Fornecer objetos em falta";
			dictionary["Missing"] = "Em falta";
			dictionary["No active quests are available in the current save."] = "Não existem missões ativas disponíveis no jogo guardado atual.";
			dictionary["All required items are already available. Finish the quest normally."] = "Todos os objetos necessários já estão disponíveis. Conclua a missão normalmente.";
			dictionary["Missing quest items supplied. Complete the quest through its normal NPC dialogue."] = "Os objetos da missão em falta foram fornecidos. Conclua a missão através do diálogo normal com a personagem.";
			dictionary["Choose a cheat. Changes apply to the current save."] = "Escolha uma batota. As alterações aplicam-se ao jogo guardado atual.";
			dictionary["Load or start a save first."] = "Carregue ou inicie primeiro um jogo guardado.";
			dictionary["Save-safe tools only. Quest and story-state editing remains disabled in this build."] = "Apenas ferramentas seguras para o jogo guardado. A edição direta de missões e da história permanece desativada.";
			dictionary["Language"] = "Idioma";
			dictionary["Unassigned"] = "Não atribuída";
			return dictionary;
		}

		// Token: 0x0600008E RID: 142 RVA: 0x000079B8 File Offset: 0x00005BB8
		private bool IsLanguage(string language)
		{
			ConfigEntry<string> language2 = this._language;
			return string.Equals((language2 != null) ? language2.Value : null, language, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x0600008F RID: 143 RVA: 0x000079D4 File Offset: 0x00005BD4
		private string L(string english)
		{
			if (string.IsNullOrEmpty(english))
			{
				return english;
			}
			Dictionary<string, string> dictionary = (this.IsLanguage("German") ? KeeperCheatMenuPlugin.GermanText : (this.IsLanguage("Korean") ? KeeperCheatMenuPlugin.KoreanText : (this.IsLanguage("Japanese") ? KeeperCheatMenuPlugin.JapaneseText : (this.IsLanguage("Chinese") ? KeeperCheatMenuPlugin.ChineseText : (this.IsLanguage("PortugueseBrazil") ? KeeperCheatMenuPlugin.BrazilianPortugueseText : (this.IsLanguage("PortuguesePortugal") ? KeeperCheatMenuPlugin.PortugueseText : null))))));
			string text;
			if (dictionary == null || !dictionary.TryGetValue(english, out text))
			{
				return english;
			}
			return text;
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00007A78 File Offset: 0x00005C78
		private void CreateLanguageSelector(RectTransform panel)
		{
			Button button = this.CreateButton("LanguageSelector", panel, this.LanguageSelectorLabel(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 16f), new Vector2(238f, 62f), new Action(this.ToggleLanguageDropdown), 17f);
			this._languageSelectionText = button.GetComponentInChildren<TextMeshProUGUI>();
			this._languageDropdownList = this.Rect("LanguageDropdown", panel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 68f), new Vector2(238f, 446f), KeeperCheatMenuPlugin.HeaderDark);
			this.CreateButton("LanguageEnglish", this._languageDropdownList, "English", new Vector2(0f, 0.85714287f), Vector2.one, new Vector2(6f, 4f), new Vector2(-6f, -6f), delegate
			{
				this.SelectLanguage("English");
			}, 17f);
			this.CreateButton("LanguageGerman", this._languageDropdownList, "German", new Vector2(0f, 0.71428573f), new Vector2(1f, 0.85714287f), new Vector2(6f, 4f), new Vector2(-6f, -4f), delegate
			{
				this.SelectLanguage("German");
			}, 17f);
			this.CreateButton("LanguageKorean", this._languageDropdownList, "Korean", new Vector2(0f, 0.5714286f), new Vector2(1f, 0.71428573f), new Vector2(6f, 6f), new Vector2(-6f, -4f), delegate
			{
				this.SelectLanguage("Korean");
			}, 17f);
			this.CreateButton("LanguageJapanese", this._languageDropdownList, "Japanese", new Vector2(0f, 0.42857143f), new Vector2(1f, 0.5714286f), new Vector2(6f, 6f), new Vector2(-6f, -4f), delegate
			{
				this.SelectLanguage("Japanese");
			}, 17f);
			this.CreateButton("LanguageChinese", this._languageDropdownList, "Chinese (Simplified)", new Vector2(0f, 0.2857143f), new Vector2(1f, 0.42857143f), new Vector2(6f, 6f), new Vector2(-6f, -4f), delegate
			{
				this.SelectLanguage("Chinese");
			}, 17f);
			this.CreateButton("LanguagePortugueseBrazil", this._languageDropdownList, "Português (Brasil)", new Vector2(0f, 0.14285715f), new Vector2(1f, 0.2857143f), new Vector2(6f, 6f), new Vector2(-6f, -4f), delegate
			{
				this.SelectLanguage("PortugueseBrazil");
			}, 16f);
			this.CreateButton("LanguagePortuguesePortugal", this._languageDropdownList, "Português (Portugal)", Vector2.zero, new Vector2(1f, 0.14285715f), new Vector2(6f, 6f), new Vector2(-6f, -4f), delegate
			{
				this.SelectLanguage("PortuguesePortugal");
			}, 16f);
			this._languageDropdownList.gameObject.SetActive(false);
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00007DFD File Offset: 0x00005FFD
		private string LanguageSelectorLabel()
		{
			string text = this.L("Language");
			string text2 = ": ";
			ConfigEntry<string> language = this._language;
			return text + text2 + KeeperCheatMenuPlugin.LanguageDisplayName((language != null) ? language.Value : null) + "  ▼";
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00007E30 File Offset: 0x00006030
		private static string LanguageDisplayName(string language)
		{
			if (string.Equals(language, "PortugueseBrazil", StringComparison.OrdinalIgnoreCase))
			{
				return "Português (Brasil)";
			}
			if (string.Equals(language, "PortuguesePortugal", StringComparison.OrdinalIgnoreCase))
			{
				return "Português (Portugal)";
			}
			if (string.Equals(language, "Chinese", StringComparison.OrdinalIgnoreCase))
			{
				return "Chinese (Simplified)";
			}
			if (!string.IsNullOrEmpty(language))
			{
				return language;
			}
			return "English";
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00007E88 File Offset: 0x00006088
		private void ToggleLanguageDropdown()
		{
			if (this._languageDropdownList != null)
			{
				this._languageDropdownList.gameObject.SetActive(!this._languageDropdownList.gameObject.activeSelf);
			}
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00007EBC File Offset: 0x000060BC
		private void SelectLanguage(string language)
		{
			if (this._language == null || string.Equals(this._language.Value, language, StringComparison.OrdinalIgnoreCase))
			{
				if (this._languageDropdownList != null)
				{
					this._languageDropdownList.gameObject.SetActive(false);
				}
				return;
			}
			this._language.Value = language;
			base.Config.Save();
			this.RebuildNativeMenuForLanguage();
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00007F24 File Offset: 0x00006124
		private void RebuildNativeMenuForLanguage()
		{
			GameObject nativeRoot = this._nativeRoot;
			this._nativeRoot = null;
			this._playerPage = (this._itemsPage = (this._teleportPage = (this._miscPage = (this._craftingPage = (this._alchemyPage = (this._questsPage = (this._worldPage = (this._zombiesPage = (this._fixPage = null)))))))));
			this._questContent = null;
			this._languageDropdownList = null;
			this._locationDropdownContent = null;
			this.ClearZombieWorkManagerReferences();
			this._pixelFont = null;
			if (nativeRoot != null)
			{
				UnityEngine.Object.Destroy(nativeRoot);
			}
			this.EnsureNativeMenu();
			this._nativeRoot.SetActive(true);
			Time.timeScale = 0f;
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
			this.ShowPlayerPage();
			this.RefreshNativeToggleLabels();
			this.SetNativeStatus("Choose a cheat. Changes apply to the current save.");
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00008008 File Offset: 0x00006208
		internal void ApplySermonSpeed()
		{
			if (this._sermonSpeedActive)
			{
				return;
			}
			float num = Mathf.Clamp(this.SermonSpeedMultiplier, 1f, 20f);
			if (num <= 1f || Time.timeScale <= 0f)
			{
				return;
			}
			this._sermonPreviousTimeScale = Time.timeScale;
			Time.timeScale = this._sermonPreviousTimeScale * num;
			this._sermonSpeedActive = true;
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00008068 File Offset: 0x00006268
		internal void RestoreSermonSpeed()
		{
			if (!this._sermonSpeedActive)
			{
				return;
			}
			Time.timeScale = this._sermonPreviousTimeScale;
			this._sermonSpeedActive = false;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00008085 File Offset: 0x00006285
		private void ToggleSharedStorage()
		{
			this._sharedStorage.Value = !this._sharedStorage.Value;
			base.Config.Save();
			this.RefreshMiscToggleLabel();
		}

		// Token: 0x06000099 RID: 153 RVA: 0x000080B4 File Offset: 0x000062B4
		private void SetSermonSpeedFromField()
		{
			float num = KeeperCheatMenuPlugin.ParseMultiplier(this._sermonSpeedMultiplierInput, this._sermonSpeedMultiplier.Value);
			this._sermonSpeedMultiplier.Value = Mathf.Clamp(num, 1f, 20f);
			base.Config.Save();
			this.RefreshSermonSpeedInput();
			this.SetNativeStatus("Sermon speed multiplier applied and saved.");
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00008110 File Offset: 0x00006310
		private void RefreshSermonSpeedInput()
		{
			if (this._sermonSpeedMultiplierInput != null)
			{
				this._sermonSpeedMultiplierInput.text = Mathf.Clamp(this._sermonSpeedMultiplier.Value, 1f, 20f).ToString("0.##", CultureInfo.InvariantCulture);
			}
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00008164 File Offset: 0x00006364
		private void SetNativeMenuVisible(bool visible)
		{
			if (visible)
			{
				try
				{
					this.EnsureNativeMenu();
				}
				catch (Exception ex)
				{
					base.Logger.LogError(string.Format("Could not build native menu: {0}", ex));
					this._status = "Native menu failed: " + ex.Message;
					return;
				}
			}
			this._show = visible;
			if (this._nativeRoot != null)
			{
				this._nativeRoot.SetActive(visible);
			}
			if (visible)
			{
				this._oldTimeScale = Time.timeScale;
				this._oldCursorVisible = Cursor.visible;
				this._oldCursorLock = Cursor.lockState;
				Time.timeScale = 0f;
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
				this.ShowPlayerPage();
				this.RefreshNativeToggleLabels();
				this.SetNativeStatus("Choose a cheat. Changes apply to the current save.");
				return;
			}
			this.SetTextInputLock(false);
			if (this._savedLocationsLoaded)
			{
				this.PersistSavedLocations();
			}
			Time.timeScale = this._oldTimeScale;
			Cursor.visible = this._oldCursorVisible;
			Cursor.lockState = this._oldCursorLock;
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00008268 File Offset: 0x00006468
		private void DestroyNativeMenu()
		{
			this.SetTextInputLock(false);
			if (this._show)
			{
				Time.timeScale = this._oldTimeScale;
				Cursor.visible = this._oldCursorVisible;
				Cursor.lockState = this._oldCursorLock;
			}
			if (this._nativeRoot != null)
			{
				UnityEngine.Object.Destroy(this._nativeRoot);
			}
			this._nativeRoot = null;
			if (this._buttonSprite != null)
			{
				UnityEngine.Object.Destroy(this._buttonSprite);
			}
			if (this._buttonTexture != null)
			{
				UnityEngine.Object.Destroy(this._buttonTexture);
			}
			if (this._headerSprite != null)
			{
				UnityEngine.Object.Destroy(this._headerSprite);
			}
			if (this._headerTexture != null)
			{
				UnityEngine.Object.Destroy(this._headerTexture);
			}
			if (this._sidebarSprite != null)
			{
				UnityEngine.Object.Destroy(this._sidebarSprite);
			}
			if (this._sidebarTexture != null)
			{
				UnityEngine.Object.Destroy(this._sidebarTexture);
			}
			if (this._backgroundSprite != null)
			{
				UnityEngine.Object.Destroy(this._backgroundSprite);
			}
			if (this._backgroundTexture != null)
			{
				UnityEngine.Object.Destroy(this._backgroundTexture);
			}
			if (this._borderSprite != null)
			{
				UnityEngine.Object.Destroy(this._borderSprite);
			}
			if (this._borderTexture != null)
			{
				UnityEngine.Object.Destroy(this._borderTexture);
			}
			if (this._scrollbarTrackSprite != null)
			{
				UnityEngine.Object.Destroy(this._scrollbarTrackSprite);
			}
			if (this._scrollbarTrackTexture != null)
			{
				UnityEngine.Object.Destroy(this._scrollbarTrackTexture);
			}
			if (this._scrollbarHandleSprite != null)
			{
				UnityEngine.Object.Destroy(this._scrollbarHandleSprite);
			}
			if (this._scrollbarHandleTexture != null)
			{
				UnityEngine.Object.Destroy(this._scrollbarHandleTexture);
			}
			this._buttonSprite = null;
			this._buttonTexture = null;
			this._headerSprite = null;
			this._headerTexture = null;
			this._sidebarSprite = null;
			this._sidebarTexture = null;
			this._backgroundSprite = null;
			this._backgroundTexture = null;
			this._borderSprite = null;
			this._borderTexture = null;
			this._scrollbarTrackSprite = null;
			this._scrollbarTrackTexture = null;
			this._scrollbarHandleSprite = null;
			this._scrollbarHandleTexture = null;
			if (this._koreanFontAsset != null)
			{
				UnityEngine.Object.Destroy(this._koreanFontAsset);
			}
			if (this._koreanSystemFont != null)
			{
				UnityEngine.Object.Destroy(this._koreanSystemFont);
			}
			this._koreanFontAsset = null;
			this._koreanSystemFont = null;
		}

		// Token: 0x0600009D RID: 157 RVA: 0x000084C8 File Offset: 0x000066C8
		private void EnsureNativeMenu()
		{
			if (this._nativeRoot != null)
			{
				return;
			}
			TMP_FontAsset[] array = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
			TMP_FontAsset tmp_FontAsset = array.FirstOrDefault<TMP_FontAsset>((TMP_FontAsset f) => f != null && f.name.IndexOf("small_font_bold", StringComparison.OrdinalIgnoreCase) >= 0) ?? array.FirstOrDefault<TMP_FontAsset>();
			this._pixelFont = ((this.IsLanguage("Korean") || this.IsLanguage("Japanese") || this.IsLanguage("Chinese")) ? this.ResolveCjkFont(array) : tmp_FontAsset);
			if (this._pixelFont == null)
			{
				this._pixelFont = tmp_FontAsset;
			}
			this.LoadButtonArtwork();
			this.LoadUiArtwork();
			this._nativeRoot = new GameObject("KeeperCheatMenuCanvas", new Type[]
			{
				typeof(RectTransform),
				typeof(Canvas),
				typeof(CanvasScaler),
				typeof(GraphicRaycaster)
			});
			UnityEngine.Object.DontDestroyOnLoad(this._nativeRoot);
			Canvas component = this._nativeRoot.GetComponent<Canvas>();
			component.renderMode = 0;
			component.sortingOrder = 32000;
			component.pixelPerfect = (this._pixelPerfectUi == null || this._pixelPerfectUi.Value);
			CanvasScaler component2 = this._nativeRoot.GetComponent<CanvasScaler>();
			component2.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
			component2.referencePixelsPerUnit = 100f;
			component2.scaleFactor = this.ResolveUiScale();
			this._lastScreenWidth = Screen.width;
			this._lastScreenHeight = Screen.height;
			base.Logger.LogInfo(string.Format("Menu canvas: {0}x{1} screen, scale {2:0.##}x, pixel perfect = {3}.", new object[] { Screen.width, Screen.height, component2.scaleFactor, component.pixelPerfect }));
			RectTransform rectTransform = this.Rect("Shade", this._nativeRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.62f));
			rectTransform.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
			RectTransform rectTransform2 = this.Rect("OuterFrame", rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-560f, -455f), new Vector2(560f, 455f), KeeperCheatMenuPlugin.PanelOuter);
			RectTransform rectTransform3 = this.Rect("GoldFrame", rectTransform2, Vector2.zero, Vector2.one, new Vector2(7f, 7f), new Vector2(-7f, -7f), KeeperCheatMenuPlugin.PanelBorder);
			RectTransform rectTransform4 = this.Rect("DarkFrame", rectTransform3, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f), KeeperCheatMenuPlugin.HeaderDark);
			RectTransform rectTransform5 = this.Rect("Panel", rectTransform4, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f), KeeperCheatMenuPlugin.PanelInner);
			RectTransform rectTransform6 = this.Rect("Header", rectTransform5, new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -105f), new Vector2(-10f, -10f), KeeperCheatMenuPlugin.HeaderBrown);
			KeeperCheatMenuPlugin.ApplyArtwork(rectTransform6, this._headerSprite);
			if (this._headerSprite != null)
			{
				rectTransform6.GetComponent<Image>().type = Image.Type.Simple;
			}
			this.Rect("HeaderBottom", rectTransform6, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 5f), KeeperCheatMenuPlugin.HeaderDark);
			this.CreateButton("Close", rectTransform6, "X", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-78f, -36f), new Vector2(-18f, 36f), delegate
			{
				this.SetNativeMenuVisible(false);
			}, 32f);
			RectTransform rectTransform7 = this.Rect("Sidebar", rectTransform5, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(20f, 66f), new Vector2(238f, -116f), KeeperCheatMenuPlugin.HeaderDark);
			KeeperCheatMenuPlugin.ApplyArtwork(rectTransform7, this._sidebarSprite);
			if (this._sidebarSprite != null)
			{
				rectTransform7.GetComponent<Image>().type = Image.Type.Simple;
			}
			this.CreateText("MENU", rectTransform7, 20f, KeeperCheatMenuPlugin.HeaderDark, (TextAlignmentOptions)514, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(8f, -46f), new Vector2(-8f, -6f));
			this._playerTabButton = this.CreateButton("PlayerTab", rectTransform7, "Player", new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -108f), new Vector2(-10f, -54f), new Action(this.ShowPlayerPage), 21f);
			this._itemsTabButton = this.CreateButton("ItemsTab", rectTransform7, "Items", new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -172f), new Vector2(-10f, -118f), new Action(this.ShowItemsPage), 21f);
			this._teleportTabButton = this.CreateButton("TeleportTab", rectTransform7, "Teleport", new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -236f), new Vector2(-10f, -182f), new Action(this.ShowTeleportPage), 21f);
			this._miscTabButton = this.CreateButton("MiscTab", rectTransform7, "Misc", new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -300f), new Vector2(-10f, -246f), new Action(this.ShowMiscPage), 21f);
			this._craftingTabButton = this.CreateButton("CraftingTab", rectTransform7, "Crafting", new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -364f), new Vector2(-10f, -310f), new Action(this.ShowCraftingPage), 21f);
			this._alchemyTabButton = this.CreateButton("AlchemyTab", rectTransform7, "Alchemy", new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -428f), new Vector2(-10f, -374f), new Action(this.ShowAlchemyPage), 21f);
			this._questsTabButton = this.CreateButton("QuestsTab", rectTransform7, "Quests", new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -492f), new Vector2(-10f, -438f), new Action(this.ShowQuestsPage), 21f);
			this._worldTabButton = this.CreateButton("WorldTab", rectTransform7, "World", new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -556f), new Vector2(-10f, -502f), new Action(this.ShowWorldPage), 21f);
			this._zombiesTabButton = this.CreateButton("ZombiesTab", rectTransform7, "Zombies", new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -620f), new Vector2(-10f, -566f), new Action(this.ShowZombiesPage), 21f);
			this._fixTabButton = this.CreateButton("FixTab", rectTransform7, "Fix", new Vector2(0f, 1f), Vector2.one, new Vector2(10f, -684f), new Vector2(-10f, -630f), new Action(this.ShowFixPage), 21f);
			RectTransform rectTransform8 = this.Rect("Body", rectTransform5, Vector2.zero, Vector2.one, new Vector2(258f, 66f), new Vector2(-28f, -116f), Color.clear);
			KeeperCheatMenuPlugin.ApplyArtwork(rectTransform8, this._backgroundSprite);
			this.BuildPlayerPage(rectTransform8);
			this.BuildItemsPage(rectTransform8);
			this.BuildTeleportPage(rectTransform8);
			this.BuildMiscPage(rectTransform8);
			this.BuildCraftingPage(rectTransform8);
			this.BuildAlchemyPage(rectTransform8);
			this.BuildQuestsPage(rectTransform8);
			this.BuildWorldPage(rectTransform8);
			this.BuildZombiesPage(rectTransform8);
			this.BuildFixPage(rectTransform8);
			this.LoadSavedLocations();
			this._nativeStatus = this.CreateText("", rectTransform5, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(260f, 16f), new Vector2(-30f, 58f));
			this.CreateLanguageSelector(rectTransform5);
			RectTransform rectTransform9 = this.Rect("ArtworkBorder", rectTransform2, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
			KeeperCheatMenuPlugin.ApplyArtwork(rectTransform9, this._borderSprite);
			rectTransform9.GetComponent<Image>().raycastTarget = false;
			this._nativeRoot.SetActive(false);
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00008E28 File Offset: 0x00007028
		private TMP_FontAsset ResolveCjkFont(IEnumerable<TMP_FontAsset> loadedFonts)
		{
			bool flag = this.IsLanguage("Japanese");
			bool flag2 = this.IsLanguage("Chinese");
			string text = (flag2 ? "Chinese" : (flag ? "Japanese" : "Korean"));
			char[] requiredCharacters;
			if (!flag2)
			{
				if (!flag)
				{
					requiredCharacters = new char[] { '한', '국', '어' };
				}
				else
				{
					requiredCharacters = new char[] { '中', '文', '字', '体' };
				}
			}
			else
			{
				requiredCharacters = new char[] { '中', '文', '字', '体' };
			}
			if (!flag && !flag2 && this._koreanFontAsset != null && requiredCharacters.All<char>((char character) => this._koreanFontAsset.HasCharacter(character, false, false)))
			{
				return this._koreanFontAsset;
			}
			TMP_FontAsset tmp_FontAsset = ((loadedFonts != null) ? loadedFonts.FirstOrDefault<TMP_FontAsset>((TMP_FontAsset font) => font != null && requiredCharacters.All<char>((char character) => font.HasCharacter(character, false, false))) : null);
			bool useLoadedFont = (tmp_FontAsset != null);
			if (useLoadedFont)
			{
				string fontMode = this.FontModeValue();
				if (fontMode.Equals("Game", StringComparison.OrdinalIgnoreCase))
				{
					useLoadedFont = true;
				}
				else if (fontMode.Equals("System", StringComparison.OrdinalIgnoreCase))
				{
					useLoadedFont = false;
				}
				else
				{
					useLoadedFont = !KeeperCheatMenuPlugin.IsLowDetailFont(tmp_FontAsset);
					if (!useLoadedFont)
					{
						base.Logger.LogInfo(string.Format("Game {0} font [{1}] is low detail; switching to a crisp system SDF font. Set 'Font mode = Game' in the config to keep the pixel font.", text, KeeperCheatMenuPlugin.DescribeFont(tmp_FontAsset)));
					}
				}
			}
			if (useLoadedFont)
			{
				base.Logger.LogInfo("Using loaded " + text + " font asset: " + tmp_FontAsset.name + " [" + KeeperCheatMenuPlugin.DescribeFont(tmp_FontAsset) + "]");
				return tmp_FontAsset;
			}
			TMP_FontAsset tmp_FontAsset2;
			try
			{
				TMP_FontAsset created = null;
				if (!flag && !flag2)
				{
					if (this._koreanSystemFont == null)
					{
						this._koreanSystemFont = Font.CreateDynamicFontFromOSFont(new string[] { "Malgun Gothic", "맑은 고딕" }, 48);
					}
					if (this._koreanSystemFont != null)
					{
						created = TMP_FontAsset.CreateFontAsset(this._koreanSystemFont, 48, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
					}
				}
				else
				{
					string windir = Environment.GetEnvironmentVariable("WINDIR") ?? "C:////Windows";
					string[] fontCandidates = (flag2 ? new string[]
					{
						"msyh.ttf",
						"simhei.ttf",
						"msyh.ttc",
						"simsun.ttc",
						"simkai.ttf"
					} : new string[]
					{
						"YuGothM.ttc",
						"meiryo.ttc",
						"msgothic.ttc",
						"yugothic.ttf"
					});
					foreach (string candidate in fontCandidates)
					{
						string text2 = Path.Combine(windir, "Fonts", candidate);
						if (!File.Exists(text2))
						{
							continue;
						}
						created = TMP_FontAsset.CreateFontAsset(text2, 0, 48, 6, GlyphRenderMode.SDFAA, 2048, 2048);
						if (created != null)
						{
							base.Logger.LogInfo("Loaded CJK system font file: " + text2);
							break;
						}
					}
				}
				if (created == null)
				{
					created = TMP_FontAsset.CreateFontAsset(flag2 ? "Microsoft YaHei" : (flag ? "Yu Gothic" : "Malgun Gothic"), "Regular", 48);
				}
				if (created == null)
				{
					throw new InvalidOperationException("TextMeshPro could not create the CJK font asset.");
				}
				created.name = (flag2 ? "KeeperCheatMenu_ChineseMicrosoftYaHei" : (flag ? "KeeperCheatMenu_JapaneseYuGothic" : "KeeperCheatMenu_KoreanMalgunGothic"));
				created.atlasPopulationMode = AtlasPopulationMode.Dynamic;
				created.isMultiAtlasTexturesEnabled = true;
				string text3 = string.Concat(flag2 ? KeeperCheatMenuPlugin.ChineseText.Values : (flag ? KeeperCheatMenuPlugin.JapaneseText.Values : KeeperCheatMenuPlugin.KoreanText.Values)) + "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?+-×–()[]/<>▼";
				uint[] seedCharacters = new uint[text3.Length];
				for (int i = 0; i < text3.Length; i++)
				{
					seedCharacters[i] = text3[i];
				}
				bool flag3 = created.TryAddCharacters(seedCharacters);
				string text4 = string.Empty;
				if (!requiredCharacters.All<char>((char character) => created.HasCharacter(character, false, false)))
				{
					if (tmp_FontAsset != null)
					{
						base.Logger.LogWarning("System CJK font could not provide the required glyphs; falling back to the game font.");
						return tmp_FontAsset;
					}
					throw new InvalidOperationException("The CJK font was loaded, but required glyphs could not be added.");
				}
				if (!flag && !flag2)
				{
					this._koreanFontAsset = created;
					this._useLegacyKoreanText = false;
				}
				base.Logger.LogInfo(string.Concat(new string[]
				{
					"Created ",
					text,
					" TMP font directly from the OS font. Glyph seed complete=",
					flag3.ToString(),
					string.IsNullOrEmpty(text4) ? string.Empty : ("; missing=" + text4)
				}));
				tmp_FontAsset2 = created;
			}
			catch (Exception ex)
			{
				if (!flag && !flag2 && this._koreanSystemFont != null)
				{
					this._useLegacyKoreanText = true;
					base.Logger.LogWarning("Korean TMP font unavailable; using Unity legacy text fallback: " + ex.Message);
				}
				else
				{
					base.Logger.LogWarning("Could not create " + text + " font: " + ex.Message);
				}
				tmp_FontAsset2 = null;
			}
			return tmp_FontAsset2;
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00009200 File Offset: 0x00007400
		private RectTransform WrapPageInScrollView(RectTransform content, float contentHeight)
		{
			Transform parent = content.parent;
			int siblingIndex = content.GetSiblingIndex();
			RectTransform rectTransform = this.Rect(content.name + "Scroll", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
			rectTransform.SetSiblingIndex(siblingIndex);
			RectTransform rectTransform2 = this.Rect("Viewport", rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-86f, 0f), Color.clear);
			rectTransform2.GetComponent<Image>().raycastTarget = true;
			rectTransform2.gameObject.AddComponent<RectMask2D>();
			content.SetParent(rectTransform2, false);
			content.anchorMin = new Vector2(0f, 1f);
			content.anchorMax = new Vector2(1f, 1f);
			content.pivot = new Vector2(0.5f, 1f);
			content.offsetMin = new Vector2(0f, -contentHeight);
			content.offsetMax = Vector2.zero;
			ScrollRect scrollRect = rectTransform.gameObject.AddComponent<ScrollRect>();
			scrollRect.viewport = rectTransform2;
			scrollRect.content = content;
			scrollRect.horizontal = false;
			scrollRect.vertical = true;
			scrollRect.movementType = ScrollRect.MovementType.Clamped;
			scrollRect.inertia = true;
			scrollRect.decelerationRate = 0.12f;
			scrollRect.scrollSensitivity = 55f;
			Scrollbar scrollbar = this.CreateOrnateScrollbar(rectTransform, scrollRect, 8f, 8f);
			scrollRect.verticalScrollbar = scrollbar;
			scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
			rectTransform2.gameObject.AddComponent<ScrollWheelForwarder>().Target = scrollRect;
			Canvas.ForceUpdateCanvases();
			scrollbar.value = 1f;
			scrollRect.verticalNormalizedPosition = 1f;
			scrollRect.StopMovement();
			return rectTransform;
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x000093B4 File Offset: 0x000075B4
		private Scrollbar CreateOrnateScrollbar(RectTransform root, ScrollRect scroll, float topMargin, float bottomMargin)
		{
			RectTransform rectTransform = this.Rect("Scrollbar", root, new Vector2(1f, 0f), Vector2.one, new Vector2(-80f, bottomMargin), new Vector2(-4f, -topMargin), KeeperCheatMenuPlugin.HeaderDark);
			if (this._scrollbarTrackSprite != null)
			{
				Image component = rectTransform.GetComponent<Image>();
				component.sprite = this._scrollbarTrackSprite;
				component.type = Image.Type.Simple;
				component.color = Color.white;
			}
			RectTransform rectTransform2 = this.Rect("Handle", rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f), Color.clear);
			Image component2 = rectTransform2.GetComponent<Image>();
			component2.raycastTarget = true;
			Scrollbar scrollbar = rectTransform.gameObject.AddComponent<Scrollbar>();
			scrollbar.handleRect = rectTransform2;
			scrollbar.targetGraphic = component2;
			scrollbar.direction = Scrollbar.Direction.BottomToTop;
			scrollbar.numberOfSteps = 0;
			Navigation navigation = scrollbar.navigation;
			navigation.mode = Navigation.Mode.None;
			scrollbar.navigation = navigation;
			if (this._scrollbarHandleSprite != null)
			{
				RectTransform rectTransform3 = this.Rect("Artwork", rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-38f, -89f), new Vector2(38f, 89f), Color.white);
				rectTransform3.pivot = new Vector2(0.5f, 0.5f);
				Image component3 = rectTransform3.GetComponent<Image>();
				component3.sprite = this._scrollbarHandleSprite;
				component3.type = Image.Type.Simple;
				component3.raycastTarget = false;
				ScrollbarArtworkFollower scrollbarArtworkFollower = rectTransform3.gameObject.AddComponent<ScrollbarArtworkFollower>();
				scrollbarArtworkFollower.Target = scroll;
				scrollbarArtworkFollower.Track = rectTransform;
				scrollbarArtworkFollower.Artwork = rectTransform3;
				scrollbarArtworkFollower.TopInset = 88f;
				scrollbarArtworkFollower.BottomInset = 94f;
			}
			return scrollbar;
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00009584 File Offset: 0x00007784
		private void BuildPlayerPage(RectTransform parent)
		{
			this._playerPage = this.Rect("PlayerViewport", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, KeeperCheatMenuPlugin.InputDark);
			this._playerPage.gameObject.AddComponent<Mask>().showMaskGraphic = false;
			RectTransform content = this.Rect("PlayerContent", this._playerPage, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 900f), Color.clear);
			content.pivot = new Vector2(0.5f, 1f);
			ScrollRect contentScroll = this._playerPage.gameObject.AddComponent<ScrollRect>();
			contentScroll.viewport = this._playerPage;
			contentScroll.content = content;
			contentScroll.horizontal = false;
			contentScroll.vertical = true;
			contentScroll.scrollSensitivity = 34f;
			this.CreateText("PLAYER", content, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(4f, -44f), new Vector2(-4f, -4f));
			this._energyToggleText = this.CreateButton("Energy", content, "", new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(-7f, -52f), new Action(this.ToggleEnergy), 20f).GetComponentInChildren<TextMeshProUGUI>();
			this._staminaToggleText = this.CreateButton("Stamina", content, "", new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(7f, -104f), new Vector2(0f, -52f), new Action(this.ToggleStamina), 20f).GetComponentInChildren<TextMeshProUGUI>();
			this._invulnerableToggleText = this.CreateButton("Invulnerable", content, "", new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -166f), new Vector2(-7f, -114f), new Action(this.ToggleInvulnerable), 20f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateButton("Restore", content, "Restore health", new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(7f, -166f), new Vector2(0f, -114f), delegate
			{
				this.NativeRun(delegate(PlayerData p)
				{
					KeeperCheatMenuPlugin.RestoreHealth(p);
				}, "Health restored.");
			}, 20f);
			this._instantActionsToggleText = this.CreateButton("InstantActions", content, "", new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -228f), new Vector2(-7f, -176f), new Action(this.ToggleInstantActions), 20f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateButton("ZeroInsanity", content, "Set insanity to 0", new Vector2(0.5f, 1f), Vector2.one, new Vector2(7f, -228f), new Vector2(0f, -176f), delegate
			{
				this.NativeRun(delegate(PlayerData p)
				{
					p.SetRes("insanity", 0f);
				}, "Insanity set to 0.");
			}, 20f);
			this._sleepAlwaysToggleText = this.CreateButton("SleepAlways", content, "", new Vector2(0f, 1f), new Vector2(0.58f, 1f), new Vector2(0f, -290f), new Vector2(-8f, -238f), new Action(this.ToggleSleepAlways), 18f).GetComponentInChildren<TextMeshProUGUI>();
			this._wakeUpHotkeyText = this.CreateButton("WakeUpHotkey", content, "", new Vector2(0.58f, 1f), Vector2.one, new Vector2(8f, -290f), new Vector2(0f, -238f), new Action(this.BeginWakeUpHotkeyCapture), 17f).GetComponentInChildren<TextMeshProUGUI>();
			this._sleepWithoutSavingToggleText = this.CreateButton("SleepWithoutSaving", content, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -352f), new Vector2(0f, -300f), new Action(this.ToggleSleepWithoutSaving), 19f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("CURRENCY", content, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(4f, -410f), new Vector2(-4f, -370f));
			this.CreateText("Gold", content, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.2f, 1f), new Vector2(4f, -436f), new Vector2(-4f, -410f));
			this.CreateText("Silver (0-99)", content, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0.2f, 1f), new Vector2(0.37f, 1f), new Vector2(6f, -436f), new Vector2(-4f, -410f));
			this.CreateText("Copper (0-99)", content, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0.37f, 1f), new Vector2(0.54f, 1f), new Vector2(6f, -436f), new Vector2(-4f, -410f));
			this._goldInput = this.CreateInput("GoldInput", content, "1", new Vector2(0f, 1f), new Vector2(0.2f, 1f), new Vector2(0f, -486f), new Vector2(-7f, -436f), false);
			this._silverInput = this.CreateInput("SilverInput", content, "0", new Vector2(0.2f, 1f), new Vector2(0.37f, 1f), new Vector2(7f, -486f), new Vector2(-7f, -436f), false);
			this._copperInput = this.CreateInput("CopperInput", content, "0", new Vector2(0.37f, 1f), new Vector2(0.54f, 1f), new Vector2(7f, -486f), new Vector2(-7f, -436f), false);
			KeeperCheatMenuPlugin.ConfigureWholeNumberInput(this._goldInput);
			this.ConfigureCoinInput(this._silverInput);
			this.ConfigureCoinInput(this._copperInput);
			this.CreateButton("AddMoney", content, "Add money", new Vector2(0.54f, 1f), new Vector2(1f, 1f), new Vector2(7f, -486f), new Vector2(0f, -436f), new Action(this.AddMoneyFromCoinFields), 20f);
			this.CreateText("TECHNOLOGY POINTS", content, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(4f, -542f), new Vector2(-4f, -502f));
			this._techInput = this.CreateInput("TechInput", content, "100", new Vector2(0f, 1f), new Vector2(0.24f, 1f), new Vector2(0f, -598f), new Vector2(-8f, -548f), false);
			KeeperCheatMenuPlugin.ConfigureWholeNumberInput(this._techInput);
			this.CreateButton("RedTech", content, "Red", new Vector2(0.24f, 1f), new Vector2(0.49f, 1f), new Vector2(8f, -598f), new Vector2(-4f, -548f), delegate
			{
				this.NativeAddResource("tech_red", this._techInput.text, "red technology points");
			}, 19f);
			this.CreateButton("GreenTech", content, "Green", new Vector2(0.49f, 1f), new Vector2(0.74f, 1f), new Vector2(4f, -598f), new Vector2(-4f, -548f), delegate
			{
				this.NativeAddResource("tech_green", this._techInput.text, "green technology points");
			}, 19f);
			this.CreateButton("BlueTech", content, "Blue", new Vector2(0.74f, 1f), new Vector2(1f, 1f), new Vector2(4f, -598f), new Vector2(0f, -548f), delegate
			{
				this.NativeAddResource("tech_blue", this._techInput.text, "blue technology points");
			}, 19f);
			this.CreateText("TOWN GRATITUDE", content, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -632f), new Vector2(-4f, -604f));
			this._townGratitudeInput = this.CreateInput("TownGratitudeInput", content, "10", new Vector2(0f, 1f), new Vector2(0.24f, 1f), new Vector2(0f, -688f), new Vector2(-8f, -638f), false);
			KeeperCheatMenuPlugin.ConfigureWholeNumberInput(this._townGratitudeInput);
			this.CreateButton("AddTownGratitude", content, "Add gratitude", new Vector2(0.24f, 1f), new Vector2(0.58f, 1f), new Vector2(8f, -688f), new Vector2(-5f, -638f), delegate
			{
				this.NativeAddResource("happiness", this._townGratitudeInput.text, "town gratitude");
			}, 18f);
			this._alwaysMaxTownGratitudeToggleText = this.CreateButton("AlwaysMaxTownGratitude", content, "", new Vector2(0.58f, 1f), Vector2.one, new Vector2(5f, -688f), new Vector2(0f, -638f), new Action(this.ToggleAlwaysMaxTownGratitude), 16f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("PLAYER MULTIPLIERS", content, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -734f), new Vector2(-4f, -700f));
			this.CreateText("Harvest yield", content, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.22f, 1f), new Vector2(4f, -766f), new Vector2(-4f, -738f));
			this.CreateText("Gratitude gain", content, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0.22f, 1f), new Vector2(0.44f, 1f), new Vector2(7f, -766f), new Vector2(-4f, -738f));
			this._harvestYieldMultiplierInput = this.CreateInput("HarvestYieldMultiplier", content, KeeperCheatMenuPlugin.FormatMultiplier(this._harvestYieldMultiplier.Value), new Vector2(0f, 1f), new Vector2(0.22f, 1f), new Vector2(0f, -820f), new Vector2(-7f, -770f), false);
			this._gratitudeMultiplierInput = this.CreateInput("GratitudeMultiplier", content, KeeperCheatMenuPlugin.FormatMultiplier(this._gratitudeMultiplier.Value), new Vector2(0.22f, 1f), new Vector2(0.44f, 1f), new Vector2(7f, -820f), new Vector2(-7f, -770f), false);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._harvestYieldMultiplierInput);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._gratitudeMultiplierInput);
			this._harvestYieldMultiplierInput.onEndEdit.AddListener(delegate(string _)
			{
				this.SaveHarvestYieldMultiplierFromField();
			});
			this._gratitudeMultiplierInput.onEndEdit.AddListener(delegate(string _)
			{
				this.SaveGratitudeMultiplierFromField();
			});
			this.CreateButton("ApplyPlayerMultipliers", content, "Apply multipliers", new Vector2(0.44f, 1f), new Vector2(0.72f, 1f), new Vector2(7f, -820f), new Vector2(-5f, -770f), new Action(this.SetPlayerMultipliersFromFields), 17f);
			this.CreateButton("ResetPlayerMultipliers", content, "Reset to 1x", new Vector2(0.72f, 1f), Vector2.one, new Vector2(5f, -820f), new Vector2(0f, -770f), new Action(this.ResetPlayerMultipliers), 17f);
			this.CreateText("Crop harvests, manually gathered resources, and positive town-gratitude rewards use these saved multipliers. Costs are not changed.", content, 15f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -866f), new Vector2(-4f, -826f));
			content = this.WrapPageInScrollView(content, 900f);
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x0000A390 File Offset: 0x00008590
		private void BuildItemsPage(RectTransform parent)
		{
			this._itemsPage = this.Rect("ItemsPage", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
			this._searchInput = this.CreateInput("Search", this._itemsPage, "Search items...", new Vector2(0f, 1f), new Vector2(0.72f, 1f), new Vector2(0f, -54f), new Vector2(-8f, 0f), true);
			this._searchInput.onValueChanged.AddListener(delegate(string value)
			{
				this._itemSearch = ((value == "Search items...") ? string.Empty : value);
				this.RebuildItemGrid();
			});
			this._quantityInput = this.CreateInput("Quantity", this._itemsPage, "1", new Vector2(0.72f, 1f), new Vector2(1f, 1f), new Vector2(8f, -54f), Vector2.zero, false);
			RectTransform rectTransform = this.Rect("Viewport", this._itemsPage, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-86f, -68f), KeeperCheatMenuPlugin.InputDark);
			rectTransform.gameObject.AddComponent<Mask>().showMaskGraphic = true;
			this._itemContent = this.Rect("Content", rectTransform, new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
			this._itemContent.pivot = new Vector2(0.5f, 1f);
			GridLayoutGroup gridLayoutGroup = this._itemContent.gameObject.AddComponent<GridLayoutGroup>();
			gridLayoutGroup.cellSize = new Vector2(245f, 96f);
			gridLayoutGroup.spacing = new Vector2(10f, 10f);
			gridLayoutGroup.padding = new RectOffset(12, 12, 12, 12);
			gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			gridLayoutGroup.constraintCount = 3;
			this._itemContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			ScrollRect scrollRect = rectTransform.gameObject.AddComponent<ScrollRect>();
			scrollRect.viewport = rectTransform;
			scrollRect.content = this._itemContent;
			scrollRect.horizontal = false;
			scrollRect.vertical = true;
			scrollRect.movementType = ScrollRect.MovementType.Clamped;
			scrollRect.inertia = true;
			scrollRect.decelerationRate = 0.12f;
			scrollRect.scrollSensitivity = 55f;
			Scrollbar scrollbar = this.CreateOrnateScrollbar(this._itemsPage, scrollRect, 76f, 8f);
			scrollRect.verticalScrollbar = scrollbar;
			scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
			this.RebuildItemGrid();
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x0000A618 File Offset: 0x00008818
		private void BuildTeleportPage(RectTransform parent)
		{
			this._teleportPage = this.Rect("TeleportPage", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
			this.CreateText("SAVE CURRENT LOCATION", this._teleportPage, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -44f), new Vector2(-4f, -4f));
			this._locationNameInput = this.CreateInput("LocationName", this._teleportPage, "Location name...", new Vector2(0f, 1f), new Vector2(0.58f, 1f), new Vector2(0f, -104f), new Vector2(-8f, -52f), true);
			this._locationNameInput.onValueChanged.AddListener(delegate(string value)
			{
				this._pendingLocationName = value ?? string.Empty;
			});
			this._locationNameInput.onSubmit.AddListener(delegate(string _)
			{
				this.SaveCurrentLocation();
			});
			this.CreateButton("SaveLocation", this._teleportPage, "Save current location", new Vector2(0.58f, 1f), Vector2.one, new Vector2(8f, -104f), new Vector2(0f, -52f), new Action(this.SaveCurrentLocation), 19f);
			this.CreateText("SAVED LOCATIONS", this._teleportPage, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -162f), new Vector2(-4f, -122f));
			Button button = this.CreateButton("LocationDropdown", this._teleportPage, "", new Vector2(0f, 1f), new Vector2(0.68f, 1f), new Vector2(0f, -222f), new Vector2(-8f, -170f), new Action(this.ToggleLocationDropdown), 17f);
			this._locationSelectionText = button.GetComponentInChildren<TextMeshProUGUI>();
			this._locationSelectionText.alignment = TextAlignmentOptions.MidlineLeft;
			RectTransform rectTransform = this._locationSelectionText.rectTransform;
			rectTransform.offsetMin = new Vector2(14f, 3f);
			rectTransform.offsetMax = new Vector2(-42f, -3f);
			this.CreateText("▼", button.transform, 19f, KeeperCheatMenuPlugin.TextGold, (TextAlignmentOptions)514, FontStyles.Normal, new Vector2(1f, 0f), Vector2.one, new Vector2(-40f, 2f), new Vector2(-6f, -2f));
			this.CreateButton("DeleteLocation", this._teleportPage, "Delete selected", new Vector2(0.68f, 1f), Vector2.one, new Vector2(8f, -222f), new Vector2(0f, -170f), new Action(this.DeleteSelectedLocation), 18f);
			this.CreateButton("Teleport", this._teleportPage, "Teleport", new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(0f, -322f), new Vector2(-8f, -262f), new Action(this.TeleportToSelectedLocation), 22f);
			this.CreateText("The preset name is saved with its scene and exact XYZ coordinates.", this._teleportPage, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -370f), new Vector2(-4f, -334f));
			this.CreateText("TELEPORT HOTBAR", this._teleportPage, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -410f), new Vector2(-4f, -374f));
			for (int i = 0; i < 4; i++)
			{
				int capturedSlot = i;
				float num = -418f - (float)i * 58f;
				float num2 = num - 50f;
				Button button2 = this.CreateButton("TeleportHotbarSlot" + i.ToString(), this._teleportPage, "", new Vector2(0f, 1f), new Vector2(0.72f, 1f), new Vector2(0f, num2), new Vector2(-8f, num), delegate
				{
					this.ToggleHotbarLocationDropdown(capturedSlot);
				}, 16f);
				this._hotbarSlotTexts[i] = button2.GetComponentInChildren<TextMeshProUGUI>();
				this._hotbarSlotTexts[i].alignment = TextAlignmentOptions.MidlineLeft;
				this.CreateText("▼", button2.transform, 18f, KeeperCheatMenuPlugin.TextGold, (TextAlignmentOptions)514, FontStyles.Normal, new Vector2(1f, 0f), Vector2.one, new Vector2(-38f, 2f), new Vector2(-6f, -2f));
				this.CreateButton("TeleportHotbarGo" + i.ToString(), this._teleportPage, "Go", new Vector2(0.72f, 1f), Vector2.one, new Vector2(8f, num2), new Vector2(0f, num), delegate
				{
					this.TeleportToHotbarSlot(capturedSlot);
				}, 19f);
			}
			this._locationDropdownList = this.Rect("LocationDropdownList", this._teleportPage, new Vector2(0f, 1f), new Vector2(0.68f, 1f), new Vector2(0f, -680f), new Vector2(-8f, -226f), KeeperCheatMenuPlugin.PanelOuter);
			RectTransform rectTransform2 = this.Rect("Viewport", this._locationDropdownList, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-22f, -4f), Color.clear);
			rectTransform2.gameObject.AddComponent<RectMask2D>();
			this._locationDropdownContent = this.Rect("Content", rectTransform2, new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
			this._locationDropdownContent.pivot = new Vector2(0.5f, 1f);
			RectTransform rectTransform3 = this.Rect("Scrollbar", this._locationDropdownList, new Vector2(1f, 0f), Vector2.one, new Vector2(-18f, 6f), new Vector2(-5f, -6f), KeeperCheatMenuPlugin.HeaderDark);
			RectTransform rectTransform4 = this.Rect("Handle", rectTransform3, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f), KeeperCheatMenuPlugin.ButtonGold);
			Scrollbar scrollbar = rectTransform3.gameObject.AddComponent<Scrollbar>();
			scrollbar.handleRect = rectTransform4;
			scrollbar.targetGraphic = rectTransform4.GetComponent<Image>();
			scrollbar.direction = Scrollbar.Direction.BottomToTop;
			ScrollRect scrollRect = this._locationDropdownList.gameObject.AddComponent<ScrollRect>();
			scrollRect.viewport = rectTransform2;
			scrollRect.content = this._locationDropdownContent;
			scrollRect.horizontal = false;
			scrollRect.vertical = true;
			scrollRect.movementType = ScrollRect.MovementType.Clamped;
			scrollRect.scrollSensitivity = 38f;
			scrollRect.verticalScrollbar = scrollbar;
			scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
			this._locationDropdownList.gameObject.SetActive(false);
			this.RefreshLocationSelection();
			this._teleportPage = this.WrapPageInScrollView(this._teleportPage, 800f);
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x0000AE00 File Offset: 0x00009000
		private void BuildMiscPage(RectTransform parent)
		{
			this._miscPage = this.Rect("MiscViewport", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, KeeperCheatMenuPlugin.InputDark);
			this._miscPage.gameObject.AddComponent<Mask>().showMaskGraphic = false;
			RectTransform content = this.Rect("MiscContent", this._miscPage, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 1430f), Color.clear);
			content.pivot = new Vector2(0.5f, 1f);
			ScrollRect contentScroll = this._miscPage.gameObject.AddComponent<ScrollRect>();
			contentScroll.viewport = this._miscPage;
			contentScroll.content = content;
			contentScroll.horizontal = false;
			contentScroll.vertical = true;
			contentScroll.scrollSensitivity = 34f;
			this.CreateText("MISC", content, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -44f), new Vector2(-4f, -4f));
			this._harvestAdjacentToggleText = this.CreateButton("HarvestAdjacent", content, "", new Vector2(0f, 1f), new Vector2(0.72f, 1f), new Vector2(0f, -108f), new Vector2(-8f, -52f), new Action(this.ToggleHarvestAdjacentCrops), 19f).GetComponentInChildren<TextMeshProUGUI>();
			this._fishOverlayToggleText = this.CreateButton("FishOverlay", content, "", new Vector2(0.72f, 1f), Vector2.one, new Vector2(8f, -108f), new Vector2(0f, -52f), new Action(this.ToggleFishOverlay), 15f).GetComponentInChildren<TextMeshProUGUI>();
			this._freeBuildToggleText = this.CreateButton("FreeBuild", content, "", new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(-8f, -120f), new Action(this.ToggleFreeBuild), 18f).GetComponentInChildren<TextMeshProUGUI>();
			this._unrestrictedBuildToggleText = this.CreateButton("UnrestrictedBuild", content, "", new Vector2(0.5f, 1f), Vector2.one, new Vector2(8f, -176f), new Vector2(0f, -120f), new Action(this.ToggleUnrestrictedBuild), 17f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("FINISH GROWING IN RANGE", content, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -238f), new Vector2(-4f, -198f));
			this.CreateText("Range", content, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.2f, 1f), new Vector2(4f, -268f), new Vector2(-4f, -242f));
			this._finishGrowingRangeInput = this.CreateInput("FinishGrowingRange", content, this._finishGrowingRange.Value.ToString(CultureInfo.InvariantCulture), new Vector2(0f, 1f), new Vector2(0.2f, 1f), new Vector2(0f, -320f), new Vector2(-7f, -270f), false);
			KeeperCheatMenuPlugin.ConfigureWholeNumberInput(this._finishGrowingRangeInput);
			this._finishGrowingRangeInput.onEndEdit.AddListener(new UnityAction<string>(this.SetFinishGrowingRangeFromText));
			this.CreateButton("RangeMinus", content, "-", new Vector2(0.2f, 1f), new Vector2(0.3f, 1f), new Vector2(7f, -320f), new Vector2(-5f, -270f), delegate
			{
				this.ChangeFinishGrowingRange(-1);
			}, 24f);
			this.CreateButton("RangePlus", content, "+", new Vector2(0.3f, 1f), new Vector2(0.4f, 1f), new Vector2(5f, -320f), new Vector2(-7f, -270f), delegate
			{
				this.ChangeFinishGrowingRange(1);
			}, 24f);
			this._finishGrowingHotkeyText = this.CreateButton("FinishGrowingHotkey", content, "", new Vector2(0.4f, 1f), Vector2.one, new Vector2(7f, -320f), new Vector2(0f, -270f), new Action(this.BeginFinishGrowingHotkeyCapture), 18f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateButton("FinishGrowingNow", content, "Finish growing now", new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(0f, -388f), new Vector2(-8f, -332f), delegate
			{
				this.FinishGrowingCropsInRange(true);
			}, 19f);
			this.CreateText("The hotkey matures growing crops around the player without harvesting them. Range: 1-30.", content, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -430f), new Vector2(-4f, -396f));
			this.CreateButton("FinishCraftsNow", content, "Finish nearby machines", new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(0f, -492f), new Vector2(-8f, -440f), delegate
			{
				this.FinishCraftsInRange(true);
			}, 18f);
			this._finishCraftsHotkeyText = this.CreateButton("FinishCraftsHotkey", content, "", new Vector2(0.55f, 1f), Vector2.one, new Vector2(8f, -492f), new Vector2(0f, -440f), new Action(this.BeginFinishCraftsHotkeyCapture), 17f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateButton("RegrowForageNow", content, "Regrow nearby forage", new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(0f, -554f), new Vector2(-8f, -502f), delegate
			{
				this.RegrowForageInRange(true);
			}, 18f);
			this._regrowForageHotkeyText = this.CreateButton("RegrowForageHotkey", content, "", new Vector2(0.55f, 1f), Vector2.one, new Vector2(8f, -554f), new Vector2(0f, -502f), new Action(this.BeginRegrowForageHotkeyCapture), 17f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("Machines: compost, ovens, forges, etc. Forage: harvested berries, mushrooms, and wild honey. All three actions use the range above.", content, 15f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -626f), new Vector2(-4f, -566f));
			this.CreateText("SERMON SPEED MULTIPLIER", content, 19f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(4f, -692f), new Vector2(-4f, -650f));
			this._sermonSpeedMultiplierInput = this.CreateInput("SermonSpeedMultiplier", content, Mathf.Clamp(this._sermonSpeedMultiplier.Value, 1f, 20f).ToString("0.##", CultureInfo.InvariantCulture), new Vector2(0.55f, 1f), new Vector2(0.76f, 1f), new Vector2(8f, -700f), new Vector2(-6f, -648f), false);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._sermonSpeedMultiplierInput);
			this.CreateButton("ApplySermonSpeed", content, "Apply", new Vector2(0.76f, 1f), Vector2.one, new Vector2(6f, -700f), new Vector2(0f, -648f), new Action(this.SetSermonSpeedFromField), 18f);
			this.CreateText("Allowed range: 1x to 20x. Only the running sermon sequence is accelerated.", content, 15f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -748f), new Vector2(-4f, -710f));
			this._sharedStorageToggleText = this.CreateButton("SharedStorage", content, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -816f), new Vector2(0f, -760f), new Action(this.ToggleSharedStorage), 19f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("Crafting stations can use materials from eligible storage chests in every area, not only the current zone.", content, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -890f), new Vector2(-4f, -828f));
			this.CreateText("LADDERS", content, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -940f), new Vector2(-4f, -900f));
			this._autoUseLaddersToggleText = this.CreateButton("AutoUseLadders", content, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -1004f), new Vector2(0f, -948f), new Action(this.ToggleAutoUseLadders), 19f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("Ladder climb speed", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(4f, -1042f), new Vector2(-4f, -1010f));
			this._ladderClimbSpeedMultiplierInput = this.CreateInput("LadderClimbSpeedMultiplier", content, Mathf.Clamp(this._ladderClimbSpeedMultiplier.Value, 0.1f, 20f).ToString("0.##", CultureInfo.InvariantCulture), new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0f, -1098f), new Vector2(-8f, -1046f), false);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._ladderClimbSpeedMultiplierInput);
			this.CreateButton("ApplyLadderClimbSpeed", content, "Apply", new Vector2(0.25f, 1f), new Vector2(0.48f, 1f), new Vector2(8f, -1098f), new Vector2(-8f, -1046f), new Action(this.SetLadderClimbSpeedFromField), 18f);
			this.CreateText("Allowed range: 0.1x to 20x. The multiplier is applied when a ladder climb starts.", content, 15f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0.48f, 1f), Vector2.one, new Vector2(8f, -1100f), new Vector2(-4f, -1044f));
			this.CreateText("FISHING", content, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -1160f), new Vector2(-4f, -1120f));
			this.CreateText("Fish pond amount multiplier", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(4f, -1198f), new Vector2(-4f, -1166f));
			this._fishPondStockMultiplierInput = this.CreateInput("FishPondStockMultiplier", content, Mathf.Clamp(this._fishPondStockMultiplier.Value, 1f, 100f).ToString("0.##", CultureInfo.InvariantCulture), new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0f, -1254f), new Vector2(-8f, -1202f), false);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._fishPondStockMultiplierInput);
			this.CreateButton("ApplyFishPondStock", content, "Apply", new Vector2(0.25f, 1f), new Vector2(0.48f, 1f), new Vector2(8f, -1254f), new Vector2(-8f, -1202f), new Action(this.SetFishPondStockMultiplierFromField), 18f);
			this.CreateText("Allowed range: 1x to 100x. Loaded ponds refill each fish species to the new capacity.", content, 15f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0.48f, 1f), Vector2.one, new Vector2(8f, -1256f), new Vector2(-4f, -1200f));
			this._instantFishingBiteToggleText = this.CreateButton("InstantFishingBite", content, "", new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1322f), new Vector2(-8f, -1266f), new Action(this.ToggleInstantFishingBite), 18f).GetComponentInChildren<TextMeshProUGUI>();
			this._autoReelFishingToggleText = this.CreateButton("AutoReelFishing", content, "", new Vector2(0.5f, 1f), Vector2.one, new Vector2(8f, -1322f), new Vector2(0f, -1266f), new Action(this.ToggleAutoReelFishing), 18f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("Instant bite removes the waiting time after casting. Auto reel hooks the bite and completes the reeling minigame.", content, 15f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -1390f), new Vector2(-4f, -1334f));
			this.RefreshMiscToggleLabel();
			this.RefreshFinishGrowingControls();
			this.RefreshSermonSpeedInput();
			this.RefreshLadderClimbSpeedInput();
			this.RefreshFishPondStockMultiplierInput();
			content = this.WrapPageInScrollView(content, 1440f);
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x0000BD38 File Offset: 0x00009F38
		private void BuildZombiesPage(RectTransform parent)
		{
			this._zombiesPage = this.Rect("ZombiesPage", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
			this.CreateText("ZOMBIES", this._zombiesPage, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -44f), new Vector2(-4f, -4f));
			this.CreateText("Multipliers affect every owned zombie and are saved between game sessions.", this._zombiesPage, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -92f), new Vector2(-4f, -50f));
			this.CreateText("MOVEMENT SPEED MULTIPLIER", this._zombiesPage, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.58f, 1f), new Vector2(4f, -150f), new Vector2(-4f, -110f));
			this._zombieMoveSpeedInput = this.CreateInput("ZombieMoveSpeed", this._zombiesPage, KeeperCheatMenuPlugin.FormatMultiplier(this._zombieMoveSpeedMultiplier.Value), new Vector2(0.58f, 1f), Vector2.one, new Vector2(8f, -158f), new Vector2(0f, -108f), false);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._zombieMoveSpeedInput);
			this.CreateText("WORK SPEED MULTIPLIER", this._zombiesPage, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.58f, 1f), new Vector2(4f, -222f), new Vector2(-4f, -182f));
			this._zombieWorkSpeedInput = this.CreateInput("ZombieWorkSpeed", this._zombiesPage, KeeperCheatMenuPlugin.FormatMultiplier(this._zombieWorkSpeedMultiplier.Value), new Vector2(0.58f, 1f), Vector2.one, new Vector2(8f, -230f), new Vector2(0f, -180f), false);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._zombieWorkSpeedInput);
			this.CreateText("EXPERIENCE GAIN MULTIPLIERS", this._zombiesPage, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -298f), new Vector2(-4f, -258f));
			this.CreateText("Red", this._zombiesPage, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.33f, 1f), new Vector2(4f, -330f), new Vector2(-4f, -302f));
			this.CreateText("Green", this._zombiesPage, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0.33f, 1f), new Vector2(0.66f, 1f), new Vector2(7f, -330f), new Vector2(-4f, -302f));
			this.CreateText("Blue", this._zombiesPage, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0.66f, 1f), Vector2.one, new Vector2(7f, -330f), new Vector2(-4f, -302f));
			this._zombieRedExperienceInput = this.CreateInput("ZombieRedExperience", this._zombiesPage, KeeperCheatMenuPlugin.FormatMultiplier(this._zombieRedExperienceMultiplier.Value), new Vector2(0f, 1f), new Vector2(0.33f, 1f), new Vector2(0f, -382f), new Vector2(-7f, -332f), false);
			this._zombieGreenExperienceInput = this.CreateInput("ZombieGreenExperience", this._zombiesPage, KeeperCheatMenuPlugin.FormatMultiplier(this._zombieGreenExperienceMultiplier.Value), new Vector2(0.33f, 1f), new Vector2(0.66f, 1f), new Vector2(7f, -382f), new Vector2(-7f, -332f), false);
			this._zombieBlueExperienceInput = this.CreateInput("ZombieBlueExperience", this._zombiesPage, KeeperCheatMenuPlugin.FormatMultiplier(this._zombieBlueExperienceMultiplier.Value), new Vector2(0.66f, 1f), Vector2.one, new Vector2(7f, -382f), new Vector2(0f, -332f), false);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._zombieRedExperienceInput);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._zombieGreenExperienceInput);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._zombieBlueExperienceInput);
			this.CreateButton("ApplyZombieMultipliers", this._zombiesPage, "Apply multipliers", new Vector2(0f, 1f), new Vector2(0.66f, 1f), new Vector2(0f, -452f), new Vector2(-8f, -398f), new Action(this.SetZombieMultipliersFromFields), 20f);
			this.CreateButton("ResetZombieMultipliers", this._zombiesPage, "Reset to 1x", new Vector2(0.66f, 1f), Vector2.one, new Vector2(8f, -452f), new Vector2(0f, -398f), new Action(this.ResetZombieMultipliers), 19f);
			this.CreateText("Allowed range: 0.1x to 100x. New experience rewards use the red, green, and blue values separately.", this._zombiesPage, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -522f), new Vector2(-4f, -470f));
			this._zombieCollectFinishedProductsToggleText = this.CreateButton("ZombieCollectFinishedProducts", this._zombiesPage, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -596f), new Vector2(0f, -540f), new Action(this.ToggleZombieCollectFinishedProducts), 20f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("Crafter zombies keep finished products in their work inventory and immediately continue the next cycle. A full inventory leaves the normal pickup order active.", this._zombiesPage, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -674f), new Vector2(-4f, -608f));
			this.BuildZombieWorkManager(this._zombiesPage);
			this.RefreshZombieCollectFinishedProductsLabel();
			this._zombiesPage = this.WrapPageInScrollView(this._zombiesPage, 1100f);
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x0000C438 File Offset: 0x0000A638
		private void BuildCraftingPage(RectTransform parent)
		{
			this._craftingPage = this.Rect("CraftingViewport", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, KeeperCheatMenuPlugin.InputDark);
			this._craftingPage.gameObject.AddComponent<Mask>().showMaskGraphic = false;
			RectTransform content = this.Rect("CraftingContent", this._craftingPage, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 740f), Color.clear);
			content.pivot = new Vector2(0.5f, 1f);
			ScrollRect contentScroll = this._craftingPage.gameObject.AddComponent<ScrollRect>();
			contentScroll.viewport = this._craftingPage;
			contentScroll.content = content;
			contentScroll.horizontal = false;
			contentScroll.vertical = true;
			contentScroll.scrollSensitivity = 34f;
			this.CreateText("CRAFTING", content, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -44f), new Vector2(-4f, -4f));
			this._freeCraftingToggleText = this.CreateButton("FreeCrafting", content, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -110f), new Vector2(0f, -54f), new Action(this.ToggleFreeCrafting), 20f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("Craft recipes without consuming their material ingredients. Required tools, recipe unlocks and other restrictions remain active.", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -184f), new Vector2(-4f, -122f));
			this._freeBuildingCostsToggleText = this.CreateButton("FreeBuildingCosts", content, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -252f), new Vector2(0f, -196f), new Action(this.ToggleFreeBuildingCosts), 20f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("Build without consuming materials. Building limits and the placement options in Misc remain independent.", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -326f), new Vector2(-4f, -264f));
			this.CreateText("MACHINE SPEED MULTIPLIER", content, 19f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.58f, 1f), new Vector2(4f, -382f), new Vector2(-4f, -342f));
			this._machineSpeedMultiplierInput = this.CreateInput("MachineSpeedMultiplier", content, KeeperCheatMenuPlugin.FormatMultiplier(this._machineSpeedMultiplier.Value), new Vector2(0.58f, 1f), Vector2.one, new Vector2(8f, -390f), new Vector2(0f, -340f), false);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._machineSpeedMultiplierInput);
			this.CreateText("CRAFTED ITEM MULTIPLIER", content, 19f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.58f, 1f), new Vector2(4f, -454f), new Vector2(-4f, -414f));
			this._craftedOutputMultiplierInput = this.CreateInput("CraftedOutputMultiplier", content, KeeperCheatMenuPlugin.FormatMultiplier(this._craftedOutputMultiplier.Value), new Vector2(0.58f, 1f), Vector2.one, new Vector2(8f, -462f), new Vector2(0f, -412f), false);
			KeeperCheatMenuPlugin.ConfigureMultiplierInput(this._craftedOutputMultiplierInput);
			this.CreateButton("ApplyCraftingMultipliers", content, "Apply multipliers", new Vector2(0f, 1f), new Vector2(0.66f, 1f), new Vector2(0f, -532f), new Vector2(-8f, -478f), new Action(this.SetCraftingMultipliersFromFields), 20f);
			this.CreateButton("ResetCraftingMultipliers", content, "Reset to 1x", new Vector2(0.66f, 1f), Vector2.one, new Vector2(8f, -532f), new Vector2(0f, -478f), new Action(this.ResetCraftingMultipliers), 19f);
			this.CreateText("Allowed range: 0.1x to 100x. Machine speed affects automatic stations; output changes completed item stack sizes.", content, 15f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -598f), new Vector2(-4f, -548f));
			RectTransform rectTransform = this.Rect("CraftingNote", content, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 92f), KeeperCheatMenuPlugin.HeaderDark);
			this.CreateText("These settings only remove resource costs; they do not unlock recipes or bypass building limits.", rectTransform, 16f, KeeperCheatMenuPlugin.TextPale, (TextAlignmentOptions)514, FontStyles.Normal, Vector2.zero, Vector2.one, new Vector2(18f, 8f), new Vector2(-18f, -8f));
			this.RefreshCraftingToggleLabels();
			this.RefreshCraftingMultiplierInputs();
			content = this.WrapPageInScrollView(content, 800f);
			this.CreateText("DISPLAY", content, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -644f), new Vector2(-4f, -608f));
			this.CreateButton("UiScaleMinus", content, "-", new Vector2(0f, 1f), new Vector2(0.12f, 1f), new Vector2(0f, -690f), new Vector2(-8f, -646f), delegate
			{
				this.ChangeUiScale(-0.25f);
			}, 22f);
			this._uiScaleText = this.CreateText("", content, 18f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0.12f, 1f), new Vector2(0.5f, 1f), new Vector2(4f, -688f), new Vector2(-4f, -648f));
			this.CreateButton("UiScalePlus", content, "+", new Vector2(0.5f, 1f), new Vector2(0.62f, 1f), new Vector2(8f, -690f), new Vector2(0f, -646f), delegate
			{
				this.ChangeUiScale(0.25f);
			}, 22f);
			this.CreateButton("UiScaleAuto", content, this.L("Auto"), new Vector2(0.62f, 1f), Vector2.one, new Vector2(8f, -690f), new Vector2(0f, -646f), new Action(this.ResetUiScale), 17f);
			this.RefreshDisplayLabels();

		}

		// Token: 0x060000A7 RID: 167 RVA: 0x0000C988 File Offset: 0x0000AB88
		private void BuildAlchemyPage(RectTransform parent)
		{
			this._alchemyPage = this.Rect("AlchemyViewport", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, KeeperCheatMenuPlugin.InputDark);
			this._alchemyPage.gameObject.AddComponent<Mask>().showMaskGraphic = false;
			RectTransform content = this.Rect("AlchemyContent", this._alchemyPage, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 740f), Color.clear);
			content.pivot = new Vector2(0.5f, 1f);
			ScrollRect contentScroll = this._alchemyPage.gameObject.AddComponent<ScrollRect>();
			contentScroll.viewport = this._alchemyPage;
			contentScroll.content = content;
			contentScroll.horizontal = false;
			contentScroll.vertical = true;
			contentScroll.scrollSensitivity = 34f;
			this.CreateText("ALCHEMY", content, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -44f), new Vector2(-4f, -4f));
			this._alchemyFolioCompanionToggleText = this.CreateButton("AlchemyFolioCompanion", content, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -110f), new Vector2(0f, -54f), new Action(this.ToggleAlchemyFolioCompanion), 20f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("When an alchemy table opens, the laboratory window moves left and an interactive folio opens beside it as a cheat sheet.", content, 18f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -198f), new Vector2(-4f, -124f));
			this.CreateText("Turning this option off keeps the original laboratory-window position and opening behavior.", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -270f), new Vector2(-4f, -212f));
			this._alchemyFolioCraftingToggleText = this.CreateButton("AlchemyFolioCrafting", content, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -342f), new Vector2(0f, -286f), new Action(this.ToggleAlchemyFolioCrafting), 20f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("Click a usable ingredient in the companion folio to place it in the first free table slot. The item must exist in an accessible inventory.", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -416f), new Vector2(-4f, -354f));
			this._retainAlchemyIngredientsToggleText = this.CreateButton("RetainAlchemyIngredients", content, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -488f), new Vector2(0f, -432f), new Action(this.ToggleRetainAlchemyIngredients), 20f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("Selected ingredient types return to their slots when the table is reopened, until you replace them or no matching material remains.", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -562f), new Vector2(-4f, -500f));
			this._freeResearchToggleText = this.CreateButton("FreeResearch", content, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -634f), new Vector2(0f, -578f), new Action(this.ToggleFreeResearch), 20f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("Research at the study table without consuming the selected item, science, faith, or other requirements.", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -708f), new Vector2(-4f, -646f));
			RectTransform rectTransform = this.Rect("AlchemyNote", content, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 92f), KeeperCheatMenuPlugin.HeaderDark);
			this.CreateText("The folio uses the formulas and runes already known by the current save.", rectTransform, 16f, KeeperCheatMenuPlugin.TextPale, (TextAlignmentOptions)514, FontStyles.Normal, Vector2.zero, Vector2.one, new Vector2(18f, 8f), new Vector2(-18f, -8f));
			this.RefreshAlchemyToggleLabel();
			content = this.WrapPageInScrollView(content, 900f);
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x0000CE00 File Offset: 0x0000B000
		private void BuildFixPage(RectTransform parent)
		{
			this._fixPage = this.Rect("FixViewport", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, KeeperCheatMenuPlugin.InputDark);
			this._fixPage.gameObject.AddComponent<Mask>().showMaskGraphic = false;
			RectTransform content = this.Rect("FixContent", this._fixPage, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 970f), Color.clear);
			content.pivot = new Vector2(0.5f, 1f);
			ScrollRect contentScroll = this._fixPage.gameObject.AddComponent<ScrollRect>();
			contentScroll.viewport = this._fixPage;
			contentScroll.content = content;
			contentScroll.horizontal = false;
			contentScroll.vertical = true;
			contentScroll.scrollSensitivity = 34f;
			this.CreateText("FIX", content, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(2f, -42f), new Vector2(-2f, -4f));
			this.CreateText("RESET COMPOSTER", content, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(2f, -102f), new Vector2(-2f, -62f));
			this.CreateText("Stand next to the broken composter. This repairs its interaction state and clears a stuck active craft. Any committed craft inputs are returned as drops beside the composter.", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(2f, -190f), new Vector2(-2f, -112f));
			this.CreateButton("ResetComposter", content, "Reset nearest composter", new Vector2(0f, 1f), new Vector2(0.62f, 1f), new Vector2(0f, -258f), new Vector2(-8f, -202f), new Action(this.ResetNearestComposter), 20f);
			this.CreateText("RESET GARDEN BED", content, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(2f, -326f), new Vector2(-2f, -286f));
			this.CreateText("Stand next to the broken garden bed. This clears its stuck crop or craft state, restores interaction, and rebuilds its approach path.", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(2f, -414f), new Vector2(-2f, -336f));
			this.CreateButton("ResetGardenBed", content, "Reset nearest garden bed", new Vector2(0f, 1f), new Vector2(0.62f, 1f), new Vector2(0f, -482f), new Vector2(-8f, -426f), new Action(this.ResetNearestGardenBed), 20f);
			this.CreateText("REPAIR INVENTORY", content, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(2f, -550f), new Vector2(-2f, -510f));
			this.CreateText("Repair removes only null, empty, or unknown item entries that can crash the inventory screen. Clear inventory is a two-click emergency fallback and removes everything.", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(2f, -638f), new Vector2(-2f, -560f));
			this.CreateButton("RepairInventory", content, "Repair broken inventory item", new Vector2(0f, 1f), new Vector2(0.62f, 1f), new Vector2(0f, -706f), new Vector2(-8f, -650f), new Action(this.RepairPlayerInventory), 19f);
			this.CreateButton("ClearInventory", content, "Clear entire inventory", new Vector2(0.62f, 1f), Vector2.one, new Vector2(8f, -706f), new Vector2(0f, -650f), new Action(this.ClearPlayerInventoryWithConfirmation), 18f);
			this.CreateText("REPAIR ZOMBIE JOBS", content, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(2f, -774f), new Vector2(-2f, -734f));
			this.CreateText("Repairs loaded zombies that still have an assigned station but lost their live worker link or stopped activity. Existing assignments and craft queues are preserved.", content, 17f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(2f, -862f), new Vector2(-2f, -784f));
			this.CreateButton("RepairZombieJobs", content, "Zombie job fix (not tested)", new Vector2(0f, 1f), new Vector2(0.62f, 1f), new Vector2(0f, -930f), new Vector2(-8f, -874f), delegate
			{
			}, 19f).interactable = false;
			RectTransform rectTransform = this.Rect("FixWarning", content, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 92f), KeeperCheatMenuPlugin.HeaderDark);
			this.CreateText("Use fix tools only when the matching object is stuck. Save and reload first when possible.", rectTransform, 16f, KeeperCheatMenuPlugin.TextPale, (TextAlignmentOptions)514, FontStyles.Normal, Vector2.zero, Vector2.one, new Vector2(18f, 8f), new Vector2(-18f, -8f));
			content = this.WrapPageInScrollView(content, 1120f);
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x0000D3E0 File Offset: 0x0000B5E0
		private void BuildWorldPage(RectTransform parent)
		{
			this._worldPage = this.Rect("WorldViewport", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, KeeperCheatMenuPlugin.InputDark);
			this._worldPage.gameObject.AddComponent<Mask>().showMaskGraphic = false;
			RectTransform content = this.Rect("WorldContent", this._worldPage, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 770f), Color.clear);
			content.pivot = new Vector2(0.5f, 1f);
			ScrollRect contentScroll = this._worldPage.gameObject.AddComponent<ScrollRect>();
			contentScroll.viewport = this._worldPage;
			contentScroll.content = content;
			contentScroll.horizontal = false;
			contentScroll.vertical = true;
			contentScroll.scrollSensitivity = 34f;
			this.CreateText("WORLD", content, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -44f), new Vector2(-4f, -4f));
			this._sermonEveryDayToggleText = this.CreateButton("SermonEveryDay", content, "", new Vector2(0f, 1f), new Vector2(0.56f, 1f), new Vector2(0f, -110f), new Vector2(-6f, -54f), new Action(this.ToggleSermonEveryDay), 18f).GetComponentInChildren<TextMeshProUGUI>();
			this._sermonRepeatableToggleText = this.CreateButton("SermonRepeatable", content, "", new Vector2(0.56f, 1f), Vector2.one, new Vector2(6f, -110f), new Vector2(0f, -54f), new Action(this.ToggleSermonRepeatable), 18f).GetComponentInChildren<TextMeshProUGUI>();
			this._zombiesEveryDayToggleText = this.CreateButton("ZombiesEveryDay", content, "", new Vector2(0f, 1f), new Vector2(0.56f, 1f), new Vector2(0f, -176f), new Vector2(-6f, -120f), new Action(this.ToggleZombiesEveryDay), 18f).GetComponentInChildren<TextMeshProUGUI>();
			this._zombiesRepeatableToggleText = this.CreateButton("ZombiesRepeatable", content, "", new Vector2(0.56f, 1f), Vector2.one, new Vector2(6f, -176f), new Vector2(0f, -120f), new Action(this.ToggleZombiesRepeatable), 18f).GetComponentInChildren<TextMeshProUGUI>();
			this._mapTeleportEverywhereToggleText = this.CreateButton("MapTeleportEverywhere", content, "", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -242f), new Vector2(0f, -186f), new Action(this.ToggleMapTeleportEverywhere), 19f).GetComponentInChildren<TextMeshProUGUI>();
			this.CreateText("DAY-SPECIFIC INTERACTIONS", content, 21f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -300f), new Vector2(-4f, -260f));
			string[] array = new string[] { "Pride-day interactions every day", "Lust-day interactions every day", "Gluttony-day interactions every day", "Envy-day interactions every day", "Wrath-day interactions every day", "Sloth-day interactions every day" };
			for (int i = 0; i < array.Length; i++)
			{
				float num = -310f - (float)i * 62f;
				this.CreateButton("WorldEverydayPlaceholder" + i.ToString(), content, this.L(array[i]) + " — " + this.L("Placeholder"), new Vector2(0f, 1f), new Vector2(0.68f, 1f), new Vector2(0f, num - 52f), new Vector2(-6f, num), delegate
				{
				}, 15f).interactable = false;
				this.CreateButton("WorldRepeatPlaceholder" + i.ToString(), content, this.L("Repeatable") + " — " + this.L("Placeholder"), new Vector2(0.68f, 1f), Vector2.one, new Vector2(6f, num - 52f), new Vector2(0f, num), delegate
				{
				}, 15f).interactable = false;
			}
			this.CreateText("The day-specific switches are visible placeholders for future game interactions and currently have no gameplay effect.", content, 15f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -736f), new Vector2(-4f, -690f));
			this.RefreshWorldToggleLabels();
			content = this.WrapPageInScrollView(content, 820f);
		}

		// Token: 0x060000AA RID: 170 RVA: 0x0000D8CC File Offset: 0x0000BACC
		private void RebuildItemGrid()
		{
			if (this._itemContent == null)
			{
				return;
			}
			for (int j = this._itemContent.childCount - 1; j >= 0; j--)
			{
				UnityEngine.Object.Destroy(this._itemContent.GetChild(j).gameObject);
			}
			string search = (this._itemSearch ?? string.Empty).Trim();
			GameBalance me = GameBalance.Me;
			IEnumerable<ItemDef> enumerable = ((me != null) ? me.itemDefs : null);
			IEnumerable<ItemDef> enumerable2 = (enumerable ?? Enumerable.Empty<ItemDef>()).Where<ItemDef>((ItemDef i) => i != null && !string.IsNullOrEmpty(i.id));
			Func<ItemDef, bool> __lambda1 = null;
			Func<ItemDef, bool> func;
			if ((func = __lambda1) == null)
			{
				func = (__lambda1 = (ItemDef i) => KeeperCheatMenuPlugin.ItemMatchesSearch(i, search, false));
			}
			using (IEnumerator<ItemDef> enumerator = enumerable2.Where<ItemDef>(func).OrderBy<ItemDef, string>((ItemDef i) => KeeperCheatMenuPlugin.ItemLabel(i), StringComparer.OrdinalIgnoreCase).Take<ItemDef>(300)
				.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ItemDef item = enumerator.Current;
					RectTransform rectTransform = this.Rect("Item_" + item.id, this._itemContent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, KeeperCheatMenuPlugin.HeaderDark);
					Image component = this.Rect("Icon", rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, -35f), new Vector2(80f, 35f), KeeperCheatMenuPlugin.HeaderDark).GetComponent<Image>();
					component.preserveAspect = true;
					component.sprite = this.ResolveItemSprite(item);
					if (component.sprite == null)
					{
						component.color = KeeperCheatMenuPlugin.HeaderDark;
						this.CreateText("?", component.transform, 28f, KeeperCheatMenuPlugin.TextGold, (TextAlignmentOptions)514, FontStyles.Normal, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
					}
					else
					{
						component.color = Color.white;
					}
					this.CreateText(KeeperCheatMenuPlugin.ShortItemName(item), rectTransform, 15f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 0.53f), new Vector2(1f, 1f), new Vector2(88f, 0f), new Vector2(-8f, -4f)).richText = false;
					this.CreateText(item.id, rectTransform, 10f, new Color(KeeperCheatMenuPlugin.TextPale.r, KeeperCheatMenuPlugin.TextPale.g, KeeperCheatMenuPlugin.TextPale.b, 0.55f), TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 0.38f), new Vector2(1f, 0.55f), new Vector2(88f, 0f), new Vector2(-8f, 0f)).richText = false;
					this.CreateButton("Add", rectTransform, "Add", new Vector2(0f, 0f), new Vector2(1f, 0.36f), new Vector2(88f, 5f), new Vector2(-8f, -2f), delegate
					{
						this.NativeSpawnItem(item.id);
					}, 15f);
				}
			}
		}

		// Token: 0x060000AB RID: 171 RVA: 0x0000DC90 File Offset: 0x0000BE90
		private Sprite ResolveItemSprite(ItemDef item)
		{
			Sprite sprite;
			try
			{
				if (item == null || string.IsNullOrEmpty(item.iconId) || LazySingletonSO<EasySpritesCollection>.Instance == null)
				{
					sprite = null;
				}
				else
				{
					sprite = LazySingletonSO<EasySpritesCollection>.Instance.GetSprite(item.iconId, null);
				}
			}
			catch
			{
				sprite = null;
			}
			return sprite;
		}

		// Token: 0x060000AC RID: 172 RVA: 0x0000DCE8 File Offset: 0x0000BEE8
		private static string ShortItemName(ItemDef item)
		{
			string text = KeeperCheatMenuPlugin.ItemLabel(item);
			int num = text.LastIndexOf("  [", StringComparison.Ordinal);
			if (num > 0)
			{
				text = text.Substring(0, num);
			}
			text = Regex.Replace(text, "<[^>]*>", string.Empty).Trim();
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			bool cjk = (instance != null && (instance.IsLanguage("Chinese") || instance.IsLanguage("Japanese") || instance.IsLanguage("Korean")));
			int limit = (cjk ? 30 : 24);
			if (text.Length <= limit)
			{
				return text;
			}
			return text.Substring(0, limit - 1) + "…";
		}

		// Token: 0x060000AD RID: 173 RVA: 0x0000DD4C File Offset: 0x0000BF4C
		private void ShowPlayerPage()
		{
			if (this._playerPage != null)
			{
				this._playerPage.gameObject.SetActive(true);
			}
			if (this._itemsPage != null)
			{
				this._itemsPage.gameObject.SetActive(false);
			}
			if (this._teleportPage != null)
			{
				this._teleportPage.gameObject.SetActive(false);
			}
			if (this._miscPage != null)
			{
				this._miscPage.gameObject.SetActive(false);
			}
			if (this._craftingPage != null)
			{
				this._craftingPage.gameObject.SetActive(false);
			}
			if (this._alchemyPage != null)
			{
				this._alchemyPage.gameObject.SetActive(false);
			}
			if (this._questsPage != null)
			{
				this._questsPage.gameObject.SetActive(false);
			}
			if (this._worldPage != null)
			{
				this._worldPage.gameObject.SetActive(false);
			}
			if (this._zombiesPage != null)
			{
				this._zombiesPage.gameObject.SetActive(false);
			}
			if (this._fixPage != null)
			{
				this._fixPage.gameObject.SetActive(false);
			}
			this.RefreshTabHighlights(this._playerTabButton);
			this.RefreshPlayerMultiplierInputs();
		}

		// Token: 0x060000AE RID: 174 RVA: 0x0000DEA4 File Offset: 0x0000C0A4
		private void ShowItemsPage()
		{
			if (this._playerPage != null)
			{
				this._playerPage.gameObject.SetActive(false);
			}
			if (this._itemsPage != null)
			{
				this._itemsPage.gameObject.SetActive(true);
			}
			if (this._teleportPage != null)
			{
				this._teleportPage.gameObject.SetActive(false);
			}
			if (this._miscPage != null)
			{
				this._miscPage.gameObject.SetActive(false);
			}
			if (this._craftingPage != null)
			{
				this._craftingPage.gameObject.SetActive(false);
			}
			if (this._alchemyPage != null)
			{
				this._alchemyPage.gameObject.SetActive(false);
			}
			if (this._questsPage != null)
			{
				this._questsPage.gameObject.SetActive(false);
			}
			if (this._worldPage != null)
			{
				this._worldPage.gameObject.SetActive(false);
			}
			if (this._zombiesPage != null)
			{
				this._zombiesPage.gameObject.SetActive(false);
			}
			if (this._fixPage != null)
			{
				this._fixPage.gameObject.SetActive(false);
			}
			this.RefreshTabHighlights(this._itemsTabButton);
			this.RebuildItemGrid();
		}

		// Token: 0x060000AF RID: 175 RVA: 0x0000DFFC File Offset: 0x0000C1FC
		private void ShowTeleportPage()
		{
			if (this._playerPage != null)
			{
				this._playerPage.gameObject.SetActive(false);
			}
			if (this._itemsPage != null)
			{
				this._itemsPage.gameObject.SetActive(false);
			}
			if (this._teleportPage != null)
			{
				this._teleportPage.gameObject.SetActive(true);
			}
			if (this._miscPage != null)
			{
				this._miscPage.gameObject.SetActive(false);
			}
			if (this._craftingPage != null)
			{
				this._craftingPage.gameObject.SetActive(false);
			}
			if (this._alchemyPage != null)
			{
				this._alchemyPage.gameObject.SetActive(false);
			}
			if (this._questsPage != null)
			{
				this._questsPage.gameObject.SetActive(false);
			}
			if (this._worldPage != null)
			{
				this._worldPage.gameObject.SetActive(false);
			}
			if (this._zombiesPage != null)
			{
				this._zombiesPage.gameObject.SetActive(false);
			}
			if (this._fixPage != null)
			{
				this._fixPage.gameObject.SetActive(false);
			}
			this.RefreshTabHighlights(this._teleportTabButton);
			this.RefreshLocationSelection();
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x0000E154 File Offset: 0x0000C354
		private void ShowMiscPage()
		{
			if (this._playerPage != null)
			{
				this._playerPage.gameObject.SetActive(false);
			}
			if (this._itemsPage != null)
			{
				this._itemsPage.gameObject.SetActive(false);
			}
			if (this._teleportPage != null)
			{
				this._teleportPage.gameObject.SetActive(false);
			}
			if (this._miscPage != null)
			{
				this._miscPage.gameObject.SetActive(true);
			}
			if (this._craftingPage != null)
			{
				this._craftingPage.gameObject.SetActive(false);
			}
			if (this._alchemyPage != null)
			{
				this._alchemyPage.gameObject.SetActive(false);
			}
			if (this._questsPage != null)
			{
				this._questsPage.gameObject.SetActive(false);
			}
			if (this._worldPage != null)
			{
				this._worldPage.gameObject.SetActive(false);
			}
			if (this._zombiesPage != null)
			{
				this._zombiesPage.gameObject.SetActive(false);
			}
			if (this._fixPage != null)
			{
				this._fixPage.gameObject.SetActive(false);
			}
			this.RefreshTabHighlights(this._miscTabButton);
			this.RefreshMiscToggleLabel();
			this.RefreshFinishGrowingControls();
			this.RefreshSermonSpeedInput();
			this.RefreshLadderClimbSpeedInput();
			this.RefreshFishPondStockMultiplierInput();
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x0000E2C4 File Offset: 0x0000C4C4
		private void ShowCraftingPage()
		{
			if (this._playerPage != null)
			{
				this._playerPage.gameObject.SetActive(false);
			}
			if (this._itemsPage != null)
			{
				this._itemsPage.gameObject.SetActive(false);
			}
			if (this._teleportPage != null)
			{
				this._teleportPage.gameObject.SetActive(false);
			}
			if (this._miscPage != null)
			{
				this._miscPage.gameObject.SetActive(false);
			}
			if (this._craftingPage != null)
			{
				this._craftingPage.gameObject.SetActive(true);
			}
			if (this._alchemyPage != null)
			{
				this._alchemyPage.gameObject.SetActive(false);
			}
			if (this._questsPage != null)
			{
				this._questsPage.gameObject.SetActive(false);
			}
			if (this._worldPage != null)
			{
				this._worldPage.gameObject.SetActive(false);
			}
			if (this._zombiesPage != null)
			{
				this._zombiesPage.gameObject.SetActive(false);
			}
			if (this._fixPage != null)
			{
				this._fixPage.gameObject.SetActive(false);
			}
			this.RefreshTabHighlights(this._craftingTabButton);
			this.RefreshCraftingToggleLabels();
			this.RefreshCraftingMultiplierInputs();
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x0000E420 File Offset: 0x0000C620
		private void ShowAlchemyPage()
		{
			if (this._playerPage != null)
			{
				this._playerPage.gameObject.SetActive(false);
			}
			if (this._itemsPage != null)
			{
				this._itemsPage.gameObject.SetActive(false);
			}
			if (this._teleportPage != null)
			{
				this._teleportPage.gameObject.SetActive(false);
			}
			if (this._miscPage != null)
			{
				this._miscPage.gameObject.SetActive(false);
			}
			if (this._craftingPage != null)
			{
				this._craftingPage.gameObject.SetActive(false);
			}
			if (this._alchemyPage != null)
			{
				this._alchemyPage.gameObject.SetActive(true);
			}
			if (this._questsPage != null)
			{
				this._questsPage.gameObject.SetActive(false);
			}
			if (this._worldPage != null)
			{
				this._worldPage.gameObject.SetActive(false);
			}
			if (this._zombiesPage != null)
			{
				this._zombiesPage.gameObject.SetActive(false);
			}
			if (this._fixPage != null)
			{
				this._fixPage.gameObject.SetActive(false);
			}
			this.RefreshTabHighlights(this._alchemyTabButton);
			this.RefreshAlchemyToggleLabel();
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x0000E578 File Offset: 0x0000C778
		private void ShowWorldPage()
		{
			if (this._playerPage != null)
			{
				this._playerPage.gameObject.SetActive(false);
			}
			if (this._itemsPage != null)
			{
				this._itemsPage.gameObject.SetActive(false);
			}
			if (this._teleportPage != null)
			{
				this._teleportPage.gameObject.SetActive(false);
			}
			if (this._miscPage != null)
			{
				this._miscPage.gameObject.SetActive(false);
			}
			if (this._craftingPage != null)
			{
				this._craftingPage.gameObject.SetActive(false);
			}
			if (this._alchemyPage != null)
			{
				this._alchemyPage.gameObject.SetActive(false);
			}
			if (this._questsPage != null)
			{
				this._questsPage.gameObject.SetActive(false);
			}
			if (this._worldPage != null)
			{
				this._worldPage.gameObject.SetActive(true);
			}
			if (this._zombiesPage != null)
			{
				this._zombiesPage.gameObject.SetActive(false);
			}
			if (this._fixPage != null)
			{
				this._fixPage.gameObject.SetActive(false);
			}
			this.RefreshTabHighlights(this._worldTabButton);
			this.RefreshWorldToggleLabels();
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x0000E6D0 File Offset: 0x0000C8D0
		private void ShowZombiesPage()
		{
			if (this._playerPage != null)
			{
				this._playerPage.gameObject.SetActive(false);
			}
			if (this._itemsPage != null)
			{
				this._itemsPage.gameObject.SetActive(false);
			}
			if (this._teleportPage != null)
			{
				this._teleportPage.gameObject.SetActive(false);
			}
			if (this._miscPage != null)
			{
				this._miscPage.gameObject.SetActive(false);
			}
			if (this._craftingPage != null)
			{
				this._craftingPage.gameObject.SetActive(false);
			}
			if (this._alchemyPage != null)
			{
				this._alchemyPage.gameObject.SetActive(false);
			}
			if (this._questsPage != null)
			{
				this._questsPage.gameObject.SetActive(false);
			}
			if (this._worldPage != null)
			{
				this._worldPage.gameObject.SetActive(false);
			}
			if (this._zombiesPage != null)
			{
				this._zombiesPage.gameObject.SetActive(true);
			}
			if (this._fixPage != null)
			{
				this._fixPage.gameObject.SetActive(false);
			}
			this.RefreshTabHighlights(this._zombiesTabButton);
			this.RefreshZombieMultiplierInputs();
			this.RefreshZombieCollectFinishedProductsLabel();
			this.RefreshZombieWorkManager();
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x0000E834 File Offset: 0x0000CA34
		private void ShowFixPage()
		{
			if (this._playerPage != null)
			{
				this._playerPage.gameObject.SetActive(false);
			}
			if (this._itemsPage != null)
			{
				this._itemsPage.gameObject.SetActive(false);
			}
			if (this._teleportPage != null)
			{
				this._teleportPage.gameObject.SetActive(false);
			}
			if (this._miscPage != null)
			{
				this._miscPage.gameObject.SetActive(false);
			}
			if (this._craftingPage != null)
			{
				this._craftingPage.gameObject.SetActive(false);
			}
			if (this._alchemyPage != null)
			{
				this._alchemyPage.gameObject.SetActive(false);
			}
			if (this._questsPage != null)
			{
				this._questsPage.gameObject.SetActive(false);
			}
			if (this._worldPage != null)
			{
				this._worldPage.gameObject.SetActive(false);
			}
			if (this._zombiesPage != null)
			{
				this._zombiesPage.gameObject.SetActive(false);
			}
			if (this._fixPage != null)
			{
				this._fixPage.gameObject.SetActive(true);
			}
			this.RefreshTabHighlights(this._fixTabButton);
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x0000E984 File Offset: 0x0000CB84
		private void RefreshTabHighlights(Button selected)
		{
			KeeperCheatMenuPlugin.SetTabHighlight(this._playerTabButton, selected == this._playerTabButton);
			KeeperCheatMenuPlugin.SetTabHighlight(this._itemsTabButton, selected == this._itemsTabButton);
			KeeperCheatMenuPlugin.SetTabHighlight(this._teleportTabButton, selected == this._teleportTabButton);
			KeeperCheatMenuPlugin.SetTabHighlight(this._miscTabButton, selected == this._miscTabButton);
			KeeperCheatMenuPlugin.SetTabHighlight(this._craftingTabButton, selected == this._craftingTabButton);
			KeeperCheatMenuPlugin.SetTabHighlight(this._alchemyTabButton, selected == this._alchemyTabButton);
			KeeperCheatMenuPlugin.SetTabHighlight(this._questsTabButton, selected == this._questsTabButton);
			KeeperCheatMenuPlugin.SetTabHighlight(this._worldTabButton, selected == this._worldTabButton);
			KeeperCheatMenuPlugin.SetTabHighlight(this._zombiesTabButton, selected == this._zombiesTabButton);
			KeeperCheatMenuPlugin.SetTabHighlight(this._fixTabButton, selected == this._fixTabButton);
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x0000EA5C File Offset: 0x0000CC5C
		private static void SetTabHighlight(Button button, bool selected)
		{
			if (button == null)
			{
				return;
			}
			Color color = (selected ? new Color(1f, 0.72f, 0.5f, 1f) : Color.white);
			ColorBlock colors = button.colors;
			colors.normalColor = color;
			colors.selectedColor = color;
			colors.highlightedColor = (selected ? new Color(1f, 0.82f, 0.62f, 1f) : new Color(1f, 0.88f, 0.72f, 1f));
			colors.pressedColor = new Color(0.72f, 0.58f, 0.48f, 1f);
			button.colors = colors;
			button.image.color = color;
			TextMeshProUGUI componentInChildren = button.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren != null)
			{
				componentInChildren.color = (selected ? Color.white : KeeperCheatMenuPlugin.TextGold);
			}
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x0000EB44 File Offset: 0x0000CD44
		private void LoadButtonArtwork()
		{
			if (this._buttonSprite != null)
			{
				return;
			}
			try
			{
				using (Stream manifestResourceStream = typeof(KeeperCheatMenuPlugin).Assembly.GetManifestResourceStream("KeeperCheatMenu.Assets.Button.png"))
				{
					if (manifestResourceStream == null)
					{
						throw new InvalidOperationException("Embedded button artwork was not found.");
					}
					byte[] array = new byte[manifestResourceStream.Length];
					int num;
					for (int i = 0; i < array.Length; i += num)
					{
						num = manifestResourceStream.Read(array, i, array.Length - i);
						if (num <= 0)
						{
							break;
						}
					}
					this._buttonTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
					this._buttonTexture.name = "KeeperCheatMenu_ButtonTexture";
					this._buttonTexture.filterMode = FilterMode.Point;
					this._buttonTexture.wrapMode = TextureWrapMode.Clamp;
					if (!this._buttonTexture.LoadImage(array, false))
					{
						throw new InvalidOperationException("Embedded button artwork could not be decoded.");
					}
					Rect rect = new Rect(49f, 214f, 317f, 44f);
					this._buttonSprite = Sprite.Create(this._buttonTexture, rect, new Vector2(0.5f, 0.5f), 100f, 0U, SpriteMeshType.FullRect, new Vector4(6f, 6f, 6f, 6f));
					this._buttonSprite.name = "KeeperCheatMenu_ButtonSprite";
				}
			}
			catch (Exception ex)
			{
				base.Logger.LogWarning("Could not load custom button artwork; using fallback styling: " + ex.Message);
				if (this._buttonTexture != null)
				{
					UnityEngine.Object.Destroy(this._buttonTexture);
				}
				this._buttonTexture = null;
				this._buttonSprite = null;
			}
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x0000ED00 File Offset: 0x0000CF00
		private void LoadUiArtwork()
		{
			if (this._headerSprite != null && this._sidebarSprite != null && this._backgroundSprite != null && this._borderSprite != null && this._scrollbarTrackSprite != null && this._scrollbarHandleSprite != null)
			{
				return;
			}
			try
			{
				this._headerSprite = this.LoadEmbeddedSprite("KeeperCheatMenu.Assets.Header.png", new Rect(0f, 0f, 4264f, 380f), Vector4.zero, "Header", out this._headerTexture);
				this._sidebarSprite = this.LoadEmbeddedSprite("KeeperCheatMenu.Assets.Header2.png", new Rect(0f, 0f, 872f, 2776f), Vector4.zero, "Sidebar", out this._sidebarTexture);
				this._backgroundSprite = this.LoadEmbeddedSprite("KeeperCheatMenu.Assets.Background.png", new Rect(20f, 18f, 375f, 269f), new Vector4(34f, 34f, 34f, 34f), "Background", out this._backgroundTexture);
				this._borderSprite = this.LoadEmbeddedSprite("KeeperCheatMenu.Assets.Border.png", new Rect(4f, 3f, 407f, 346f), new Vector4(42f, 42f, 42f, 42f), "Border", out this._borderTexture);
				this._scrollbarTrackSprite = this.LoadEmbeddedSprite("KeeperCheatMenu.Assets.ScrollbarTrack.png", new Rect(266f, 95f, 216f, 1974f), Vector4.zero, "ScrollbarTrack", out this._scrollbarTrackTexture);
				this._scrollbarHandleSprite = this.LoadEmbeddedSprite("KeeperCheatMenu.Assets.ScrollbarHandle.png", new Rect(279f, 297f, 470f, 1102f), Vector4.zero, "ScrollbarHandle", out this._scrollbarHandleTexture);
			}
			catch (Exception ex)
			{
				base.Logger.LogWarning("Could not load custom UI artwork; using fallback styling: " + ex.Message);
			}
		}

		// Token: 0x060000BA RID: 186 RVA: 0x0000EF24 File Offset: 0x0000D124
		private Sprite LoadEmbeddedSprite(string resourceName, Rect crop, Vector4 border, string assetName, out Texture2D texture)
		{
			Sprite sprite2;
			using (Stream manifestResourceStream = typeof(KeeperCheatMenuPlugin).Assembly.GetManifestResourceStream(resourceName))
			{
				if (manifestResourceStream == null)
				{
					throw new InvalidOperationException(assetName + " artwork was not found.");
				}
				byte[] array = new byte[manifestResourceStream.Length];
				int num;
				for (int i = 0; i < array.Length; i += num)
				{
					num = manifestResourceStream.Read(array, i, array.Length - i);
					if (num <= 0)
					{
						break;
					}
				}
				texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				texture.name = "KeeperCheatMenu_" + assetName + "Texture";
				texture.filterMode = FilterMode.Point;
				texture.wrapMode = TextureWrapMode.Clamp;
				if (!texture.LoadImage(array, false))
				{
					throw new InvalidOperationException(assetName + " artwork could not be decoded.");
				}
				Sprite sprite = Sprite.Create(texture, crop, new Vector2(0.5f, 0.5f), 100f, 0U, SpriteMeshType.FullRect, border);
				sprite.name = "KeeperCheatMenu_" + assetName + "Sprite";
				sprite2 = sprite;
			}
			return sprite2;
		}

		// Token: 0x060000BB RID: 187 RVA: 0x0000F038 File Offset: 0x0000D238
		private static void ApplyArtwork(RectTransform target, Sprite sprite)
		{
			if (target == null || sprite == null)
			{
				return;
			}
			Image component = target.GetComponent<Image>();
			component.sprite = sprite;
			component.type = Image.Type.Sliced;
			component.color = Color.white;
		}

		// Token: 0x060000BC RID: 188 RVA: 0x0000F06C File Offset: 0x0000D26C
		private void LoadSavedLocations()
		{
			this._savedLocations.Clear();
			this._savedLocationsLoaded = false;
			try
			{
				string text = null;
				string savedLocationsPath = this.SavedLocationsPath;
				if (File.Exists(savedLocationsPath))
				{
					text = File.ReadAllText(savedLocationsPath);
				}
				else
				{
					ConfigEntry<string> savedLocationsJson = this._savedLocationsJson;
					if (!string.IsNullOrWhiteSpace((savedLocationsJson != null) ? savedLocationsJson.Value : null) && this._savedLocationsJson.Value.TrimStart().StartsWith("[", StringComparison.Ordinal))
					{
						text = this._savedLocationsJson.Value;
					}
				}
				if (!string.IsNullOrWhiteSpace(text))
				{
					List<SavedLocation> list = JsonConvert.DeserializeObject<List<SavedLocation>>(text);
					if (list != null)
					{
						this._savedLocations.AddRange(list.Where<SavedLocation>((SavedLocation location) => location != null));
					}
				}
				this._savedLocationsLoaded = true;
				base.Logger.LogInfo(string.Format("Loaded {0} persistent teleport location(s) from {1}.", this._savedLocations.Count, savedLocationsPath));
			}
			catch (Exception ex)
			{
				base.Logger.LogWarning("Could not load teleport locations: " + ex.Message);
				this._savedLocationsLoaded = true;
			}
			this._selectedLocationIndex = ((this._savedLocations.Count > 0) ? 0 : (-1));
			this.RefreshLocationSelection();
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060000BD RID: 189 RVA: 0x0000F1AC File Offset: 0x0000D3AC
		private string SavedLocationsPath
		{
			get
			{
				return Path.Combine(Paths.ConfigPath, "KeeperCheatMenu.locations.json");
			}
		}

		// Token: 0x060000BE RID: 190 RVA: 0x0000F1C0 File Offset: 0x0000D3C0
		private bool PersistSavedLocations()
		{
			bool flag;
			try
			{
				string text = JsonConvert.SerializeObject(this._savedLocations, Formatting.Indented);
				File.WriteAllText(this.SavedLocationsPath, text);
				base.Logger.LogInfo(string.Format("Saved {0} persistent teleport location(s) to {1}.", this._savedLocations.Count, this.SavedLocationsPath));
				flag = true;
			}
			catch (Exception ex)
			{
				base.Logger.LogError(string.Format("Could not save teleport locations: {0}", ex));
				this.SetNativeStatus("Location remains available this session, but could not be saved: " + ex.Message);
				flag = false;
			}
			return flag;
		}

		// Token: 0x060000BF RID: 191 RVA: 0x0000F258 File Offset: 0x0000D458
		private void SaveCurrentLocation()
		{
			TMP_InputField locationNameInput = this._locationNameInput;
			string text;
			if ((text = ((locationNameInput != null) ? locationNameInput.text : null)) == null)
			{
				text = this._pendingLocationName ?? string.Empty;
			}
			string locationName = text.Trim();
			if (string.IsNullOrWhiteSpace(locationName))
			{
				locationName = (this._pendingLocationName ?? string.Empty).Trim();
			}
			base.Logger.LogInfo("Save-current-location requested. Name='" + locationName + "'.");
			if (string.IsNullOrWhiteSpace(locationName))
			{
				base.Logger.LogWarning("Save-current-location rejected because no name was entered.");
				this.SetNativeStatus("Enter a name before saving the current location.");
				return;
			}
			PlayerController playerController = MainGame.PlayerController;
			if (playerController == null)
			{
				base.Logger.LogWarning("Save-current-location rejected because MainGame.PlayerController is null.");
				this.SetNativeStatus("Load or start a save before saving a location.");
				return;
			}
			GameScene currentGameScene = playerController.CurrentGameScene;
			if (currentGameScene == null)
			{
				playerController.TryGetCurrentGameScene(out currentGameScene);
			}
			if (currentGameScene == null)
			{
				base.Logger.LogWarning("Save-current-location rejected because the player has no current GameScene.");
				this.SetNativeStatus("The player's current scene is not ready. Close the menu, move once, and try again.");
				return;
			}
			if (currentGameScene.GameSceneData == null || string.IsNullOrWhiteSpace(currentGameScene.GameSceneData.id))
			{
				base.Logger.LogWarning("Save-current-location rejected because scene '" + currentGameScene.name + "' has no usable GameSceneData id.");
				this.SetNativeStatus("The current scene has no teleport identifier and cannot be saved.");
				return;
			}
			Vector3 movablePosition = playerController.MovablePosition;
			SavedLocation savedLocation = new SavedLocation
			{
				name = locationName,
				sceneId = currentGameScene.GameSceneData.id,
				x = movablePosition.x,
				y = movablePosition.y,
				z = movablePosition.z
			};
			int num = this._savedLocations.FindIndex((SavedLocation entry) => string.Equals(entry.name, locationName, StringComparison.OrdinalIgnoreCase));
			if (num >= 0)
			{
				this._savedLocations[num] = savedLocation;
				this._selectedLocationIndex = num;
			}
			else
			{
				if (this._savedLocations.Count >= 64)
				{
					this.SetNativeStatus(string.Format("A maximum of {0} locations can be saved. Delete one first.", 64));
					return;
				}
				this._savedLocations.Add(savedLocation);
				this._selectedLocationIndex = this._savedLocations.Count - 1;
			}
			bool flag = this.PersistSavedLocations();
			this.RefreshLocationSelection();
			if (flag && this._locationNameInput != null)
			{
				this._locationNameInput.text = string.Empty;
			}
			if (flag)
			{
				this._pendingLocationName = string.Empty;
				this.SetNativeStatus("Saved " + KeeperCheatMenuPlugin.FormatLocation(savedLocation) + " permanently.");
			}
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x0000F4E0 File Offset: 0x0000D6E0
		private void DeleteSelectedLocation()
		{
			if (this._selectedLocationIndex < 0 || this._selectedLocationIndex >= this._savedLocations.Count)
			{
				this.SetNativeStatus("Select a saved location first.");
				return;
			}
			string name = this._savedLocations[this._selectedLocationIndex].name;
			this._savedLocations.RemoveAt(this._selectedLocationIndex);
			if (this._teleportHotbarSlots != null)
			{
				foreach (ConfigEntry<string> configEntry in this._teleportHotbarSlots)
				{
					if (configEntry != null && string.Equals(configEntry.Value, name, StringComparison.OrdinalIgnoreCase))
					{
						configEntry.Value = string.Empty;
					}
				}
				base.Config.Save();
			}
			this._selectedLocationIndex = ((this._savedLocations.Count == 0) ? (-1) : Math.Min(this._selectedLocationIndex, this._savedLocations.Count - 1));
			if (!this.PersistSavedLocations())
			{
				return;
			}
			if (this._locationDropdownList != null)
			{
				this._locationDropdownList.gameObject.SetActive(false);
			}
			this.RefreshLocationSelection();
			this.SetNativeStatus("Deleted saved location " + name + ".");
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x0000F5F8 File Offset: 0x0000D7F8
		private void ToggleLocationDropdown()
		{
			if (this._locationDropdownList == null)
			{
				return;
			}
			this._hotbarEditingSlot = -1;
			bool flag = !this._locationDropdownList.gameObject.activeSelf;
			this._locationDropdownList.gameObject.SetActive(flag);
			if (flag)
			{
				this.RebuildLocationDropdown();
			}
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x0000F64C File Offset: 0x0000D84C
		private void RebuildLocationDropdown()
		{
			if (this._locationDropdownList == null || this._locationDropdownContent == null)
			{
				return;
			}
			for (int i = this._locationDropdownContent.childCount - 1; i >= 0; i--)
			{
				UnityEngine.Object.Destroy(this._locationDropdownContent.GetChild(i).gameObject);
			}
			float num = Math.Max(446f, (float)this._savedLocations.Count * 38f + 10f);
			this._locationDropdownContent.offsetMin = new Vector2(0f, -num);
			this._locationDropdownContent.offsetMax = Vector2.zero;
			if (this._savedLocations.Count == 0)
			{
				this.CreateText("No saved locations", this._locationDropdownContent, 17f, KeeperCheatMenuPlugin.TextPale, (TextAlignmentOptions)514, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(8f, -60f), new Vector2(-8f, 0f));
				return;
			}
			for (int j = 0; j < this._savedLocations.Count; j++)
			{
				int capturedIndex = j;
				SavedLocation savedLocation = this._savedLocations[j];
				Button button = this.CreateButton("Location_" + j.ToString(), this._locationDropdownContent, KeeperCheatMenuPlugin.FormatLocation(savedLocation), new Vector2(0f, 1f), Vector2.one, new Vector2(6f, -((float)(j + 1) * 38f + 5f)), new Vector2(-6f, -((float)j * 38f + 5f)), delegate
				{
					this.SelectSavedLocation(capturedIndex);
				}, 14f);
				button.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
				bool flag = ((this._hotbarEditingSlot >= 0 && this._teleportHotbarSlots != null) ? string.Equals(this._teleportHotbarSlots[this._hotbarEditingSlot].Value, savedLocation.name, StringComparison.OrdinalIgnoreCase) : (j == this._selectedLocationIndex));
				KeeperCheatMenuPlugin.SetTabHighlight(button, flag);
			}
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x0000F860 File Offset: 0x0000DA60
		private void SelectSavedLocation(int index)
		{
			if (index < 0 || index >= this._savedLocations.Count)
			{
				return;
			}
			if (this._hotbarEditingSlot >= 0 && this._hotbarEditingSlot < 4 && this._teleportHotbarSlots != null)
			{
				this._teleportHotbarSlots[this._hotbarEditingSlot].Value = this._savedLocations[index].name ?? string.Empty;
				base.Config.Save();
				this._hotbarEditingSlot = -1;
				if (this._locationDropdownList != null)
				{
					this._locationDropdownList.gameObject.SetActive(false);
				}
				this.RefreshHotbarSlotLabels();
				return;
			}
			this._selectedLocationIndex = index;
			if (this._locationDropdownList != null)
			{
				this._locationDropdownList.gameObject.SetActive(false);
			}
			this.RefreshLocationSelection();
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x0000F92C File Offset: 0x0000DB2C
		private void RefreshLocationSelection()
		{
			if (this._locationSelectionText != null)
			{
				this._locationSelectionText.text = ((this._selectedLocationIndex >= 0 && this._selectedLocationIndex < this._savedLocations.Count) ? KeeperCheatMenuPlugin.FormatLocation(this._savedLocations[this._selectedLocationIndex]) : this.L("Select a saved location"));
			}
			this.RefreshHotbarSlotLabels();
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x0000F998 File Offset: 0x0000DB98
		private void ToggleHotbarLocationDropdown(int slot)
		{
			if (this._locationDropdownList == null || slot < 0 || slot >= 4)
			{
				return;
			}
			bool flag = this._locationDropdownList.gameObject.activeSelf && this._hotbarEditingSlot == slot;
			this._hotbarEditingSlot = (flag ? (-1) : slot);
			this._locationDropdownList.gameObject.SetActive(!flag);
			if (!flag)
			{
				this.RebuildLocationDropdown();
			}
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x0000FA08 File Offset: 0x0000DC08
		private void RefreshHotbarSlotLabels()
		{
			for (int i = 0; i < this._hotbarSlotTexts.Length; i++)
			{
				if (!(this._hotbarSlotTexts[i] == null))
				{
					string text;
					if (this._teleportHotbarSlots == null)
					{
						text = string.Empty;
					}
					else
					{
						ConfigEntry<string> configEntry = this._teleportHotbarSlots[i];
						text = ((configEntry != null) ? configEntry.Value : null);
					}
					string assignedName = text;
					SavedLocation savedLocation = this._savedLocations.FirstOrDefault<SavedLocation>((SavedLocation entry) => entry != null && string.Equals(entry.name, assignedName, StringComparison.OrdinalIgnoreCase));
					this._hotbarSlotTexts[i].text = ((savedLocation != null) ? KeeperCheatMenuPlugin.FormatLocation(savedLocation) : this.L("Select a saved location"));
				}
			}
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x0000FAA8 File Offset: 0x0000DCA8
		private void TeleportToHotbarSlot(int slot)
		{
			if (slot < 0 || slot >= 4 || this._teleportHotbarSlots == null)
			{
				return;
			}
			ConfigEntry<string> configEntry = this._teleportHotbarSlots[slot];
			string assignedName = ((configEntry != null) ? configEntry.Value : null);
			SavedLocation savedLocation = this._savedLocations.FirstOrDefault<SavedLocation>((SavedLocation entry) => entry != null && string.Equals(entry.name, assignedName, StringComparison.OrdinalIgnoreCase));
			if (savedLocation == null)
			{
				this.SetNativeStatus("Select a saved location for this hotbar slot first.");
				return;
			}
			this.TeleportToSavedLocation(savedLocation);
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x0000FB14 File Offset: 0x0000DD14
		private static string FormatLocation(SavedLocation location)
		{
			if (location == null)
			{
				return "Unknown location";
			}
			return string.Format(CultureInfo.InvariantCulture, "{0} ({1:0.0}, {2:0.0}, {3:0.0})", new object[] { location.name, location.x, location.y, location.z });
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x0000FB74 File Offset: 0x0000DD74
		private void TeleportToSelectedLocation()
		{
			if (this._selectedLocationIndex < 0 || this._selectedLocationIndex >= this._savedLocations.Count)
			{
				this.SetNativeStatus("Select a saved location first.");
				return;
			}
			this.TeleportToSavedLocation(this._savedLocations[this._selectedLocationIndex]);
		}

		// Token: 0x060000CA RID: 202 RVA: 0x0000FBC0 File Offset: 0x0000DDC0
		private void TeleportToSavedLocation(SavedLocation location)
		{
			if (MainGame.PlayerController == null)
			{
				this.SetNativeStatus("Load or start a save first.");
				return;
			}
			try
			{
				SavedPositionTeleportData savedPositionTeleportData = new SavedPositionTeleportData(location);
				string text;
				if (!savedPositionTeleportData.CanTeleport(out text))
				{
					this.SetNativeStatus(string.IsNullOrEmpty(text) ? "That destination is unavailable." : text);
				}
				else if (!PlayerController.Teleport(savedPositionTeleportData))
				{
					this.SetNativeStatus("The game refused the teleport request.");
				}
				else
				{
					this.SetNativeMenuVisible(false);
				}
			}
			catch (Exception ex)
			{
				base.Logger.LogError(ex);
				this.SetNativeStatus("Teleport failed: " + ex.Message);
			}
		}

		// Token: 0x060000CB RID: 203 RVA: 0x0000FC64 File Offset: 0x0000DE64
		private void ToggleEnergy()
		{
			this._infiniteEnergy.Value = !this._infiniteEnergy.Value;
			this.RefreshNativeToggleLabels();
		}

		// Token: 0x060000CC RID: 204 RVA: 0x0000FC85 File Offset: 0x0000DE85
		private void ToggleStamina()
		{
			this._infiniteStamina.Value = !this._infiniteStamina.Value;
			this.RefreshNativeToggleLabels();
		}

		// Token: 0x060000CD RID: 205 RVA: 0x0000FCA6 File Offset: 0x0000DEA6
		private void ToggleInvulnerable()
		{
			this._invulnerable.Value = !this._invulnerable.Value;
			this.RefreshNativeToggleLabels();
		}

		// Token: 0x060000CE RID: 206 RVA: 0x0000FCC7 File Offset: 0x0000DEC7
		private void ToggleInstantActions()
		{
			this._instantActions.Value = !this._instantActions.Value;
			base.Config.Save();
			this.RefreshNativeToggleLabels();
		}

		// Token: 0x060000CF RID: 207 RVA: 0x0000FCF3 File Offset: 0x0000DEF3
		private void ToggleSleepAlways()
		{
			this._sleepAlways.Value = !this._sleepAlways.Value;
			base.Config.Save();
			this.RefreshNativeToggleLabels();
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x0000FD1F File Offset: 0x0000DF1F
		private void ToggleSleepWithoutSaving()
		{
			this._sleepWithoutSaving.Value = !this._sleepWithoutSaving.Value;
			base.Config.Save();
			this.RefreshNativeToggleLabels();
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x0000FD4C File Offset: 0x0000DF4C
		private void ToggleAlwaysMaxTownGratitude()
		{
			this._alwaysMaxTownGratitude.Value = !this._alwaysMaxTownGratitude.Value;
			base.Config.Save();
			if (this._alwaysMaxTownGratitude.Value)
			{
				Action<PlayerData> action;
				if ((action = KeeperCheatMenuPlugin.__OCache.__f4_FillTownGratitudeToCurrentMaximum) == null)
				{
					action = (KeeperCheatMenuPlugin.__OCache.__f4_FillTownGratitudeToCurrentMaximum = new Action<PlayerData>(KeeperCheatMenuPlugin.FillTownGratitudeToCurrentMaximum));
				}
				this.NativeRun(action, "Town gratitude filled to its current maximum.");
			}
			this.RefreshNativeToggleLabels();
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x0000FDB6 File Offset: 0x0000DFB6
		private void ToggleHarvestAdjacentCrops()
		{
			this._harvestAdjacentCrops.Value = !this._harvestAdjacentCrops.Value;
			base.Config.Save();
			this.RefreshMiscToggleLabel();
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x0000FDE2 File Offset: 0x0000DFE2
		private void ToggleFishOverlay()
		{
			this._fishOverlayEnabled.Value = !this._fishOverlayEnabled.Value;
			base.Config.Save();
			this.RefreshMiscToggleLabel();
			if (!this._fishOverlayEnabled.Value)
			{
				this.HideFishingOverlay();
			}
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x0000FE21 File Offset: 0x0000E021
		private void ToggleInstantFishingBite()
		{
			this._instantFishingBite.Value = !this._instantFishingBite.Value;
			base.Config.Save();
			this.RefreshMiscToggleLabel();
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x0000FE4D File Offset: 0x0000E04D
		private void ToggleAutoReelFishing()
		{
			this._autoReelFishing.Value = !this._autoReelFishing.Value;
			base.Config.Save();
			this.RefreshMiscToggleLabel();
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x0000FE7C File Offset: 0x0000E07C
		private void SetFishPondStockMultiplierFromField()
		{
			float num = this._fishPondStockMultiplier.Value;
			float num2;
			if (this._fishPondStockMultiplierInput != null && float.TryParse(this._fishPondStockMultiplierInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out num2))
			{
				num = num2;
			}
			this._fishPondStockMultiplier.Value = Mathf.Clamp(num, 1f, 100f);
			base.Config.Save();
			this.RefreshFishPondStockMultiplierInput();
			this.RefillLoadedFishingPonds();
			this.SetNativeStatus("Fish pond amount multiplier applied and loaded ponds refilled.");
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x0000FF00 File Offset: 0x0000E100
		private void RefreshFishPondStockMultiplierInput()
		{
			if (this._fishPondStockMultiplierInput != null)
			{
				this._fishPondStockMultiplierInput.text = Mathf.Clamp(this._fishPondStockMultiplier.Value, 1f, 100f).ToString("0.##", CultureInfo.InvariantCulture);
			}
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x0000FF52 File Offset: 0x0000E152
		private void ToggleFreeBuild()
		{
			this._freeBuild.Value = !this._freeBuild.Value;
			base.Config.Save();
			this.RefreshMiscToggleLabel();
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000FF7E File Offset: 0x0000E17E
		private void ToggleUnrestrictedBuild()
		{
			this._unrestrictedBuild.Value = !this._unrestrictedBuild.Value;
			base.Config.Save();
			this.RefreshMiscToggleLabel();
		}

		// Token: 0x060000DA RID: 218 RVA: 0x0000FFAA File Offset: 0x0000E1AA
		private void ToggleFreeCrafting()
		{
			this._freeCrafting.Value = !this._freeCrafting.Value;
			base.Config.Save();
			this.RefreshCraftingToggleLabels();
		}

		// Token: 0x060000DB RID: 219 RVA: 0x0000FFD6 File Offset: 0x0000E1D6
		private void ToggleFreeBuildingCosts()
		{
			this._freeBuildingCosts.Value = !this._freeBuildingCosts.Value;
			base.Config.Save();
			this.RefreshCraftingToggleLabels();
		}

		// Token: 0x060000DC RID: 220 RVA: 0x00010004 File Offset: 0x0000E204
		private void RefreshCraftingToggleLabels()
		{
			if (this._freeCraftingToggleText != null)
			{
				this._freeCraftingToggleText.text = this.ToggleLabel("Craft items for free", this._freeCrafting.Value);
			}
			if (this._freeBuildingCostsToggleText != null)
			{
				this._freeBuildingCostsToggleText.text = this.ToggleLabel("Build stuff for free", this._freeBuildingCosts.Value);
			}
		}

		// Token: 0x060000DD RID: 221 RVA: 0x00010070 File Offset: 0x0000E270
		private void SetCraftingMultipliersFromFields()
		{
			this._machineSpeedMultiplier.Value = KeeperCheatMenuPlugin.ParseMultiplier(this._machineSpeedMultiplierInput, this._machineSpeedMultiplier.Value);
			this._craftedOutputMultiplier.Value = KeeperCheatMenuPlugin.ParseMultiplier(this._craftedOutputMultiplierInput, this._craftedOutputMultiplier.Value);
			base.Config.Save();
			this.RefreshCraftingMultiplierInputs();
			this.SetNativeStatus("Crafting multipliers applied and saved.");
		}

		// Token: 0x060000DE RID: 222 RVA: 0x000100DB File Offset: 0x0000E2DB
		private void ResetCraftingMultipliers()
		{
			this._machineSpeedMultiplier.Value = 1f;
			this._craftedOutputMultiplier.Value = 1f;
			base.Config.Save();
			this.RefreshCraftingMultiplierInputs();
			this.SetNativeStatus("Crafting multipliers reset to 1x.");
		}

		// Token: 0x060000DF RID: 223 RVA: 0x0001011C File Offset: 0x0000E31C
		private void RefreshCraftingMultiplierInputs()
		{
			if (this._machineSpeedMultiplierInput != null)
			{
				this._machineSpeedMultiplierInput.text = KeeperCheatMenuPlugin.FormatMultiplier(this._machineSpeedMultiplier.Value);
			}
			if (this._craftedOutputMultiplierInput != null)
			{
				this._craftedOutputMultiplierInput.text = KeeperCheatMenuPlugin.FormatMultiplier(this._craftedOutputMultiplier.Value);
			}
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x0001017C File Offset: 0x0000E37C
		private void SetPlayerMultipliersFromFields()
		{
			this._harvestYieldMultiplier.Value = KeeperCheatMenuPlugin.ParseMultiplier(this._harvestYieldMultiplierInput, this._harvestYieldMultiplier.Value);
			this._gratitudeMultiplier.Value = KeeperCheatMenuPlugin.ParseMultiplier(this._gratitudeMultiplierInput, this._gratitudeMultiplier.Value);
			base.Config.Save();
			this.RefreshPlayerMultiplierInputs();
			this.SetNativeStatus("Player multipliers applied and saved.");
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x000101E8 File Offset: 0x0000E3E8
		private void SaveHarvestYieldMultiplierFromField()
		{
			this._harvestYieldMultiplier.Value = KeeperCheatMenuPlugin.ParseMultiplier(this._harvestYieldMultiplierInput, this._harvestYieldMultiplier.Value);
			base.Config.Save();
			if (this._harvestYieldMultiplierInput != null)
			{
				this._harvestYieldMultiplierInput.text = KeeperCheatMenuPlugin.FormatMultiplier(this._harvestYieldMultiplier.Value);
			}
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0001024C File Offset: 0x0000E44C
		private void SaveGratitudeMultiplierFromField()
		{
			this._gratitudeMultiplier.Value = KeeperCheatMenuPlugin.ParseMultiplier(this._gratitudeMultiplierInput, this._gratitudeMultiplier.Value);
			base.Config.Save();
			if (this._gratitudeMultiplierInput != null)
			{
				this._gratitudeMultiplierInput.text = KeeperCheatMenuPlugin.FormatMultiplier(this._gratitudeMultiplier.Value);
			}
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x000102AE File Offset: 0x0000E4AE
		private void ResetPlayerMultipliers()
		{
			this._harvestYieldMultiplier.Value = 1f;
			this._gratitudeMultiplier.Value = 1f;
			base.Config.Save();
			this.RefreshPlayerMultiplierInputs();
			this.SetNativeStatus("Player multipliers reset to 1x.");
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x000102EC File Offset: 0x0000E4EC
		private void RefreshPlayerMultiplierInputs()
		{
			if (this._harvestYieldMultiplierInput != null)
			{
				this._harvestYieldMultiplierInput.text = KeeperCheatMenuPlugin.FormatMultiplier(this._harvestYieldMultiplier.Value);
			}
			if (this._gratitudeMultiplierInput != null)
			{
				this._gratitudeMultiplierInput.text = KeeperCheatMenuPlugin.FormatMultiplier(this._gratitudeMultiplier.Value);
			}
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0001034B File Offset: 0x0000E54B
		private void ToggleAlchemyFolioCompanion()
		{
			this.SetAlchemyFolioCompanionEnabled(!this._alchemyFolioCompanion.Value);
			this.RefreshAlchemyToggleLabel();
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00010367 File Offset: 0x0000E567
		private void ToggleFreeResearch()
		{
			this._freeResearch.Value = !this._freeResearch.Value;
			base.Config.Save();
			this.RefreshAlchemyToggleLabel();
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00010393 File Offset: 0x0000E593
		private void ToggleAlchemyFolioCrafting()
		{
			this._alchemyFolioCrafting.Value = !this._alchemyFolioCrafting.Value;
			base.Config.Save();
			this.RefreshAlchemyToggleLabel();
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x000103BF File Offset: 0x0000E5BF
		private void ToggleRetainAlchemyIngredients()
		{
			this._retainAlchemyIngredients.Value = !this._retainAlchemyIngredients.Value;
			base.Config.Save();
			this.RefreshAlchemyToggleLabel();
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x000103EC File Offset: 0x0000E5EC
		private void RefreshAlchemyToggleLabel()
		{
			if (this._alchemyFolioCompanionToggleText != null)
			{
				this._alchemyFolioCompanionToggleText.text = this.ToggleLabel("Show folio beside alchemy table", this._alchemyFolioCompanion.Value);
			}
			if (this._alchemyFolioCraftingToggleText != null)
			{
				this._alchemyFolioCraftingToggleText.text = this.ToggleLabel("Folio ingredient selection", this._alchemyFolioCrafting.Value);
			}
			if (this._retainAlchemyIngredientsToggleText != null)
			{
				this._retainAlchemyIngredientsToggleText.text = this.ToggleLabel("Keep selected ingredients", this._retainAlchemyIngredients.Value);
			}
			if (this._freeResearchToggleText != null)
			{
				this._freeResearchToggleText.text = this.ToggleLabel("Free research", this._freeResearch.Value);
			}
		}

		// Token: 0x060000EA RID: 234 RVA: 0x000104B8 File Offset: 0x0000E6B8
		private void RefreshWorldToggleLabels()
		{
			if (this._sermonEveryDayToggleText != null)
			{
				this._sermonEveryDayToggleText.text = this.ToggleLabel("Sermon every day", this._sermonEveryDay.Value);
			}
			if (this._sermonRepeatableToggleText != null)
			{
				this._sermonRepeatableToggleText.text = this.ToggleLabel("Repeat sermons", this._sermonRepeatable.Value);
			}
			if (this._zombiesEveryDayToggleText != null)
			{
				this._zombiesEveryDayToggleText.text = this.ToggleLabel("Make zombies every day", this._zombiesEveryDay.Value);
			}
			if (this._zombiesRepeatableToggleText != null)
			{
				this._zombiesRepeatableToggleText.text = this.ToggleLabel("Repeat zombie making", this._zombiesRepeatable.Value);
			}
			if (this._mapTeleportEverywhereToggleText != null)
			{
				this._mapTeleportEverywhereToggleText.text = this.ToggleLabel("Use map teleporters from everywhere", this._mapTeleportEverywhere.Value);
			}
		}

		// Token: 0x060000EB RID: 235 RVA: 0x000105B0 File Offset: 0x0000E7B0
		private void RefreshMiscToggleLabel()
		{
			if (this._harvestAdjacentToggleText != null)
			{
				this._harvestAdjacentToggleText.text = this.ToggleLabel("Harvest adjacent ready crops", this._harvestAdjacentCrops.Value);
			}
			if (this._fishOverlayToggleText != null)
			{
				this._fishOverlayToggleText.text = this.ToggleLabel("Fish HUD", this._fishOverlayEnabled.Value);
			}
			if (this._freeBuildToggleText != null)
			{
				this._freeBuildToggleText.text = this.ToggleLabel("Free build", this._freeBuild.Value);
			}
			if (this._unrestrictedBuildToggleText != null)
			{
				this._unrestrictedBuildToggleText.text = this.ToggleLabel("No placement restrictions", this._unrestrictedBuild.Value);
			}
			if (this._sharedStorageToggleText != null)
			{
				this._sharedStorageToggleText.text = this.ToggleLabel("Shared storage for crafting", this._sharedStorage.Value);
			}
			if (this._autoUseLaddersToggleText != null)
			{
				this._autoUseLaddersToggleText.text = this.ToggleLabel("Auto use ladders", this._autoUseLadders.Value);
			}
			if (this._instantFishingBiteToggleText != null)
			{
				this._instantFishingBiteToggleText.text = this.ToggleLabel("Instant bite", this._instantFishingBite.Value);
			}
			if (this._autoReelFishingToggleText != null)
			{
				this._autoReelFishingToggleText.text = this.ToggleLabel("Auto reel", this._autoReelFishing.Value);
			}
		}

		// Token: 0x060000EC RID: 236 RVA: 0x00010738 File Offset: 0x0000E938
		private void RefreshNativeToggleLabels()
		{
			if (this._energyToggleText != null)
			{
				this._energyToggleText.text = this.ToggleLabel("Infinite energy", this._infiniteEnergy.Value);
			}
			if (this._staminaToggleText != null)
			{
				this._staminaToggleText.text = this.ToggleLabel("Infinite stamina", this._infiniteStamina.Value);
			}
			if (this._invulnerableToggleText != null)
			{
				this._invulnerableToggleText.text = this.ToggleLabel("Invulnerable", this._invulnerable.Value);
			}
			if (this._instantActionsToggleText != null)
			{
				this._instantActionsToggleText.text = this.ToggleLabel("Instant actions", this._instantActions.Value);
			}
			if (this._sleepAlwaysToggleText != null)
			{
				this._sleepAlwaysToggleText.text = this.ToggleLabel("Sleep until wake key", this._sleepAlways.Value);
			}
			if (this._sleepWithoutSavingToggleText != null)
			{
				this._sleepWithoutSavingToggleText.text = this.ToggleLabel("Sleep without saving", this._sleepWithoutSaving.Value);
			}
			if (this._alwaysMaxTownGratitudeToggleText != null)
			{
				this._alwaysMaxTownGratitudeToggleText.text = this.ToggleLabel("Always current maximum", this._alwaysMaxTownGratitude.Value);
			}
			this.RefreshCraftingToggleLabels();
			this.RefreshAlchemyToggleLabel();
			this.RefreshWorldToggleLabels();
			this.RefreshHotkeyText(KeeperCheatMenuPlugin.HotkeyCaptureTarget.WakeUp);
		}

		// Token: 0x060000ED RID: 237 RVA: 0x000108A7 File Offset: 0x0000EAA7
		private string ToggleLabel(string label, bool enabled)
		{
			return this.L(label) + "  " + this.L(enabled ? "ON" : "OFF");
		}

		// Token: 0x060000EE RID: 238 RVA: 0x000108D0 File Offset: 0x0000EAD0
		private void NativeRun(Action<PlayerData> action, string success)
		{
			PlayerData playerData = KeeperCheatMenuPlugin.SafePlayer();
			if (playerData == null)
			{
				this.SetNativeStatus("Load or start a save first.");
				return;
			}
			try
			{
				action(playerData);
				this.SetNativeStatus(success);
			}
			catch (Exception ex)
			{
				base.Logger.LogError(ex);
				this.SetNativeStatus("Action failed: " + ex.Message);
			}
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00010938 File Offset: 0x0000EB38
		private void NativeAddResource(string resource, string amountText, string label)
		{
			float amount;
			if (!float.TryParse(amountText, out amount) || amount <= 0f)
			{
				this.SetNativeStatus("Enter a positive amount.");
				return;
			}
			this.NativeRun(delegate(PlayerData p)
			{
				p.AddRes(resource, amount);
			}, string.Format("Added {0:0.##} {1}.", amount, label));
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x000109A4 File Offset: 0x0000EBA4
		private void AddMoneyFromCoinFields()
		{
			double num;
			if (!double.TryParse((this._goldInput == null) ? "0" : this._goldInput.text, NumberStyles.None, CultureInfo.InvariantCulture, out num) || num < 0.0)
			{
				this.SetNativeStatus("Gold must be a whole number of 0 or more.");
				return;
			}
			int num2 = KeeperCheatMenuPlugin.ParseCoinField(this._silverInput);
			int num3 = KeeperCheatMenuPlugin.ParseCoinField(this._copperInput);
			double totalCopper = num * 10000.0 + (double)num2 * 100.0 + (double)num3;
			if (totalCopper <= 0.0)
			{
				this.SetNativeStatus("Enter a positive money amount.");
				return;
			}
			if (double.IsInfinity(totalCopper) || totalCopper > 3.4028234663852886E+38)
			{
				this.SetNativeStatus("That gold amount is too large for the game.");
				return;
			}
			this.NativeRun(delegate(PlayerData p)
			{
				p.AddRes("money", (float)totalCopper);
			}, string.Format(CultureInfo.InvariantCulture, "Added {0:0} gold, {1} silver, {2} copper.", num, num2, num3));
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00010AB1 File Offset: 0x0000ECB1
		private static void ConfigureWholeNumberInput(TMP_InputField input)
		{
			if (input != null)
			{
				input.characterValidation = (TMP_InputField.CharacterValidation)2;
			}
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00010AC3 File Offset: 0x0000ECC3
		private static void ConfigureMultiplierInput(TMP_InputField input)
		{
			if (input != null)
			{
				input.characterValidation = (TMP_InputField.CharacterValidation)3;
			}
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x00010AD8 File Offset: 0x0000ECD8
		private static string FormatMultiplier(float value)
		{
			return Mathf.Clamp(value, 0.1f, 100f).ToString("0.##", CultureInfo.InvariantCulture);
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x00010B08 File Offset: 0x0000ED08
		private static float ParseMultiplier(TMP_InputField input, float fallback)
		{
			float num;
			if (input == null || !float.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out num))
			{
				num = fallback;
			}
			return Mathf.Clamp(num, 0.1f, 100f);
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x00010B4C File Offset: 0x0000ED4C
		private void SetZombieMultipliersFromFields()
		{
			this._zombieMoveSpeedMultiplier.Value = KeeperCheatMenuPlugin.ParseMultiplier(this._zombieMoveSpeedInput, this._zombieMoveSpeedMultiplier.Value);
			this._zombieWorkSpeedMultiplier.Value = KeeperCheatMenuPlugin.ParseMultiplier(this._zombieWorkSpeedInput, this._zombieWorkSpeedMultiplier.Value);
			this._zombieRedExperienceMultiplier.Value = KeeperCheatMenuPlugin.ParseMultiplier(this._zombieRedExperienceInput, this._zombieRedExperienceMultiplier.Value);
			this._zombieGreenExperienceMultiplier.Value = KeeperCheatMenuPlugin.ParseMultiplier(this._zombieGreenExperienceInput, this._zombieGreenExperienceMultiplier.Value);
			this._zombieBlueExperienceMultiplier.Value = KeeperCheatMenuPlugin.ParseMultiplier(this._zombieBlueExperienceInput, this._zombieBlueExperienceMultiplier.Value);
			base.Config.Save();
			this.RefreshZombieMultiplierInputs();
			this.SetNativeStatus("Zombie multipliers applied and saved.");
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x00010C1C File Offset: 0x0000EE1C
		private void ResetZombieMultipliers()
		{
			this._zombieMoveSpeedMultiplier.Value = 1f;
			this._zombieWorkSpeedMultiplier.Value = 1f;
			this._zombieRedExperienceMultiplier.Value = 1f;
			this._zombieGreenExperienceMultiplier.Value = 1f;
			this._zombieBlueExperienceMultiplier.Value = 1f;
			base.Config.Save();
			this.RefreshZombieMultiplierInputs();
			this.SetNativeStatus("Zombie multipliers reset to 1x.");
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x00010C98 File Offset: 0x0000EE98
		private void RefreshZombieMultiplierInputs()
		{
			if (this._zombieMoveSpeedInput != null)
			{
				this._zombieMoveSpeedInput.text = KeeperCheatMenuPlugin.FormatMultiplier(this._zombieMoveSpeedMultiplier.Value);
			}
			if (this._zombieWorkSpeedInput != null)
			{
				this._zombieWorkSpeedInput.text = KeeperCheatMenuPlugin.FormatMultiplier(this._zombieWorkSpeedMultiplier.Value);
			}
			if (this._zombieRedExperienceInput != null)
			{
				this._zombieRedExperienceInput.text = KeeperCheatMenuPlugin.FormatMultiplier(this._zombieRedExperienceMultiplier.Value);
			}
			if (this._zombieGreenExperienceInput != null)
			{
				this._zombieGreenExperienceInput.text = KeeperCheatMenuPlugin.FormatMultiplier(this._zombieGreenExperienceMultiplier.Value);
			}
			if (this._zombieBlueExperienceInput != null)
			{
				this._zombieBlueExperienceInput.text = KeeperCheatMenuPlugin.FormatMultiplier(this._zombieBlueExperienceMultiplier.Value);
			}
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x00010D74 File Offset: 0x0000EF74
		private void ToggleZombieCollectFinishedProducts()
		{
			this._zombieCollectFinishedProducts.Value = !this._zombieCollectFinishedProducts.Value;
			base.Config.Save();
			this.RefreshZombieCollectFinishedProductsLabel();
			this.SetNativeStatus(this._zombieCollectFinishedProducts.Value ? "Crafter zombies will collect finished products." : "Automatic zombie product pickup disabled.");
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x00010DCA File Offset: 0x0000EFCA
		private void RefreshZombieCollectFinishedProductsLabel()
		{
			if (this._zombieCollectFinishedProductsToggleText != null)
			{
				this._zombieCollectFinishedProductsToggleText.text = this.ToggleLabel("Collect finished products", this._zombieCollectFinishedProducts.Value);
			}
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00010DFC File Offset: 0x0000EFFC
		private void ConfigureCoinInput(TMP_InputField input)
		{
			KeeperCheatMenuPlugin.ConfigureWholeNumberInput(input);
			if (input != null)
			{
				input.onEndEdit.AddListener(delegate(string _)
				{
					KeeperCheatMenuPlugin.ClampCoinField(input);
				});
			}
		}

		// Token: 0x060000FB RID: 251 RVA: 0x00010E4C File Offset: 0x0000F04C
		private static int ParseCoinField(TMP_InputField input)
		{
			int num;
			if (input == null || !int.TryParse(input.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
			{
				num = 0;
			}
			num = Mathf.Clamp(num, 0, 99);
			input.text = num.ToString(CultureInfo.InvariantCulture);
			return num;
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00010E96 File Offset: 0x0000F096
		private static void ClampCoinField(TMP_InputField input)
		{
			KeeperCheatMenuPlugin.ParseCoinField(input);
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00010E9F File Offset: 0x0000F09F
		private void NativeSpawnItem(string itemId)
		{
			this._itemQuantity = ((this._quantityInput == null) ? "1" : this._quantityInput.text);
			this.SpawnItem(itemId);
			this.SetNativeStatus(this._status);
		}

		// Token: 0x060000FE RID: 254 RVA: 0x00010EDA File Offset: 0x0000F0DA
		private void SetNativeStatus(string value)
		{
			this._status = this.L(value);
			if (this._nativeStatus != null)
			{
				this._nativeStatus.text = this._status;
			}
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00010F08 File Offset: 0x0000F108
		private RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
		{
			GameObject gameObject = new GameObject(name, new Type[]
			{
				typeof(RectTransform),
				typeof(Image)
			});
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.SetParent(parent, false);
			component.anchorMin = anchorMin;
			component.anchorMax = anchorMax;
			component.offsetMin = offsetMin;
			component.offsetMax = offsetMax;
			Image component2 = gameObject.GetComponent<Image>();
			component2.color = color;
			component2.raycastTarget = color.a > 0f;
			return component;
		}

		// Token: 0x06000100 RID: 256 RVA: 0x00010F8C File Offset: 0x0000F18C
		private TextMeshProUGUI CreateText(string value, Transform parent, float size, Color color, TextAlignmentOptions alignment, FontStyles style, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
		{
			GameObject gameObject = new GameObject("Text", new Type[]
			{
				typeof(RectTransform),
				typeof(TextMeshProUGUI)
			});
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.SetParent(parent, false);
			component.anchorMin = anchorMin;
			component.anchorMax = anchorMax;
			component.offsetMin = offsetMin;
			component.offsetMax = offsetMax;
			TextMeshProUGUI component2 = gameObject.GetComponent<TextMeshProUGUI>();
			component2.text = this.L(value);
			component2.font = this._pixelFont;
			component2.fontSize = size * (((this.IsLanguage("Chinese") || this.IsLanguage("Japanese") || this.IsLanguage("Korean")) ? 1.08f : 1f));
			component2.color = color;
			component2.alignment = alignment;
			component2.fontStyle = style;
			component2.textWrappingMode = (TextWrappingModes)1;
			component2.raycastTarget = false;
			if (this.IsLanguage("Korean") && this._useLegacyKoreanText && this._koreanSystemFont != null)
			{
				LegacyCjkTextMirror.Attach(component2, this._koreanSystemFont);
			}
			return component2;
		}

		// Token: 0x06000101 RID: 257 RVA: 0x0001106C File Offset: 0x0000F26C
		private Button CreateButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Action onClick, float fontSize)
		{
			RectTransform rectTransform = this.Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax, KeeperCheatMenuPlugin.ButtonRed);
			Image component = rectTransform.GetComponent<Image>();
			if (this._buttonSprite != null)
			{
				component.sprite = this._buttonSprite;
				component.type = Image.Type.Sliced;
				component.color = Color.white;
			}
			Button button = rectTransform.gameObject.AddComponent<Button>();
			button.targetGraphic = component;
			ColorBlock colors = button.colors;
			colors.normalColor = ((this._buttonSprite != null) ? Color.white : KeeperCheatMenuPlugin.ButtonRed);
			colors.highlightedColor = ((this._buttonSprite != null) ? new Color(1f, 0.88f, 0.72f, 1f) : KeeperCheatMenuPlugin.ButtonRedHover);
			colors.pressedColor = ((this._buttonSprite != null) ? new Color(0.72f, 0.58f, 0.48f, 1f) : KeeperCheatMenuPlugin.ButtonRedPressed);
			colors.selectedColor = colors.highlightedColor;
			colors.colorMultiplier = 1f;
			button.colors = colors;
			button.onClick.AddListener(delegate
			{
				Action onClick2 = onClick;
				if (onClick2 == null)
				{
					return;
				}
				onClick2();
			});
			if (this._buttonSprite == null)
			{
				this.Rect("TopLine", rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(2f, -3f), new Vector2(-2f, 0f), KeeperCheatMenuPlugin.ButtonGold).GetComponent<Image>().raycastTarget = false;
				this.Rect("BottomLine", rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(2f, 0f), new Vector2(-2f, 3f), KeeperCheatMenuPlugin.ButtonRedPressed).GetComponent<Image>().raycastTarget = false;
			}
			this.CreateText(label, rectTransform, fontSize, KeeperCheatMenuPlugin.TextGold, (TextAlignmentOptions)514, FontStyles.Normal, Vector2.zero, Vector2.one, new Vector2(8f, 3f), new Vector2(-8f, -3f));
			return button;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00011298 File Offset: 0x0000F498
		private TMP_InputField CreateInput(string name, Transform parent, string initialValue, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, bool useAsPlaceholder = false)
		{
			RectTransform rectTransform = this.Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax, KeeperCheatMenuPlugin.InputDark);
			Image outerImage = rectTransform.GetComponent<Image>();
			RectTransform rectTransform2 = this.Rect("Background", rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f), KeeperCheatMenuPlugin.HeaderDark);
			Image innerImage = rectTransform2.GetComponent<Image>();
			innerImage.raycastTarget = false;
			RectTransform rectTransform3 = this.Rect("TextViewport", rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 6f), new Vector2(-14f, -6f), Color.clear);
			rectTransform3.GetComponent<Image>().raycastTarget = false;
			rectTransform3.gameObject.AddComponent<RectMask2D>();
			TextMeshProUGUI textMeshProUGUI = this.CreateText(initialValue, rectTransform3, 18f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
			textMeshProUGUI.raycastTarget = true;
			TMP_InputField input = rectTransform.gameObject.AddComponent<TMP_InputField>();
			input.transition = Selectable.Transition.None;
			input.targetGraphic = outerImage;
			input.textViewport = rectTransform3;
			input.textComponent = textMeshProUGUI;
			if (useAsPlaceholder)
			{
				TextMeshProUGUI textMeshProUGUI2 = this.CreateText(initialValue, rectTransform3, 18f, new Color(KeeperCheatMenuPlugin.TextPale.r, KeeperCheatMenuPlugin.TextPale.g, KeeperCheatMenuPlugin.TextPale.b, 0.5f), TextAlignmentOptions.MidlineLeft, FontStyles.Italic, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
				input.placeholder = textMeshProUGUI2;
				input.text = string.Empty;
			}
			else
			{
				input.text = initialValue;
			}
			input.customCaretColor = true;
			input.caretColor = KeeperCheatMenuPlugin.TextGold;
			input.caretWidth = 3;
			input.caretBlinkRate = 0.65f;
			input.lineType = 0;
			input.richText = false;
			input.selectionColor = new Color(KeeperCheatMenuPlugin.TextGold.r, KeeperCheatMenuPlugin.TextGold.g, KeeperCheatMenuPlugin.TextGold.b, 0.35f);
			input.onSelect.AddListener(delegate(string _)
			{
				outerImage.color = KeeperCheatMenuPlugin.TextGold;
				innerImage.color = KeeperCheatMenuPlugin.PanelOuter;
				input.ForceLabelUpdate();
				this.SetTextInputLock(true);
			});
			input.onDeselect.AddListener(delegate(string _)
			{
				outerImage.color = KeeperCheatMenuPlugin.InputDark;
				innerImage.color = KeeperCheatMenuPlugin.HeaderDark;
				this.SetTextInputLock(false);
			});
			input.onEndEdit.AddListener(delegate(string _)
			{
				this.SetTextInputLock(false);
			});
			return input;
		}

		// Token: 0x06000103 RID: 259 RVA: 0x00011554 File Offset: 0x0000F754
		private void SetTextInputLock(bool locked)
		{
			if (locked)
			{
				PlayerController playerController = MainGame.PlayerController;
				if (playerController == null)
				{
					return;
				}
				if (this._textInputLockActive && this._textInputLockedController == playerController)
				{
					return;
				}
				this.SetTextInputLock(false);
				this._textInputLockedController = playerController;
				this._uiControlsWereEnabled = playerController.IsControlEnabledByType((TakenControlType)1);
				if (this._uiControlsWereEnabled)
				{
					playerController.SetControlTakenType((TakenControlType)1, false);
				}
				this._textInputLockActive = true;
				return;
			}
			else
			{
				if (!this._textInputLockActive)
				{
					return;
				}
				try
				{
					if (this._textInputLockedController != null && this._uiControlsWereEnabled)
					{
						this._textInputLockedController.SetControlTakenType((TakenControlType)1, true);
					}
				}
				catch
				{
				}
				this._textInputLockedController = null;
				this._textInputLockActive = false;
				this._uiControlsWereEnabled = false;
				return;
			}
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00011610 File Offset: 0x0000F810
		private static Color Hex(string hex)
		{
			Color color;
			ColorUtility.TryParseHtmlString("#" + hex, out color);
			return color;
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00011634 File Offset: 0x0000F834
		private void BuildQuestsPage(RectTransform parent)
		{
			this._questContent = this.Rect("QuestsPage", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
			this._questsPage = this.WrapPageInScrollView(this._questContent, 820f);
			this.RebuildQuestAssistList();
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00011689 File Offset: 0x0000F889
		private void ShowQuestsPage()
		{
			this.SetPageVisibility(this._questsPage);
			this.RefreshTabHighlights(this._questsTabButton);
			this.RebuildQuestAssistList();
		}

		// Token: 0x06000107 RID: 263 RVA: 0x000116AC File Offset: 0x0000F8AC
		private void SetPageVisibility(RectTransform active)
		{
			foreach (RectTransform rectTransform in new RectTransform[] { this._playerPage, this._itemsPage, this._teleportPage, this._miscPage, this._craftingPage, this._alchemyPage, this._questsPage, this._worldPage, this._zombiesPage, this._fixPage })
			{
				if (rectTransform != null)
				{
					rectTransform.gameObject.SetActive(rectTransform == active);
				}
			}
		}

		// Token: 0x06000108 RID: 264 RVA: 0x00011748 File Offset: 0x0000F948
		private void RebuildQuestAssistList()
		{
			if (this._questContent == null)
			{
				return;
			}
			for (int i = this._questContent.childCount - 1; i >= 0; i--)
			{
				UnityEngine.Object.Destroy(this._questContent.GetChild(i).gameObject);
			}
			this.CreateText("QUEST ASSIST", this._questContent, 22f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(0.7f, 1f), new Vector2(4f, -44f), new Vector2(-4f, -4f));
			this.CreateButton("RefreshQuests", this._questContent, "Refresh", new Vector2(0.72f, 1f), Vector2.one, new Vector2(8f, -48f), new Vector2(0f, -4f), new Action(this.RebuildQuestAssistList), 17f);
			this.CreateText("Only missing item-delivery requirements can be supplied. Friendship, reputation, story, day, order, and unknown conditions remain protected.", this._questContent, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -110f), new Vector2(-4f, -54f));
			List<KeeperCheatMenuPlugin.QuestAssistEntry> activeQuestAssistEntries = this.GetActiveQuestAssistEntries();
			if (activeQuestAssistEntries.Count == 0)
			{
				this.CreateText("No active quests are available in the current save.", this._questContent, 18f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -174f), new Vector2(-4f, -126f));
			}
			float num = 124f;
			using (List<KeeperCheatMenuPlugin.QuestAssistEntry>.Enumerator enumerator = activeQuestAssistEntries.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeeperCheatMenuPlugin.QuestAssistEntry entry = enumerator.Current;
					RectTransform rectTransform = this.Rect("QuestAssistCard", this._questContent, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -num - 132f), new Vector2(0f, -num), new Color(KeeperCheatMenuPlugin.HeaderDark.r, KeeperCheatMenuPlugin.HeaderDark.g, KeeperCheatMenuPlugin.HeaderDark.b, 0.92f));
					this.CreateText(KeeperCheatMenuPlugin.QuestDisplayName(entry.Quest), rectTransform, 18f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 0.52f), new Vector2(0.68f, 1f), new Vector2(12f, 4f), new Vector2(-8f, -8f));
					this.CreateText(entry.Detail, rectTransform, 14f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 0f), new Vector2(0.68f, 0.55f), new Vector2(12f, 8f), new Vector2(-8f, -2f));
					this.CreateButton("SupplyQuestItems", rectTransform, entry.CanSupply ? "Supply missing items" : "Protected", new Vector2(0.7f, 0.18f), new Vector2(0.98f, 0.82f), Vector2.zero, Vector2.zero, delegate
					{
						this.SupplyMissingQuestItems(entry.Quest);
					}, 16f).interactable = entry.CanSupply;
					num += 144f;
				}
			}
			float num2 = Mathf.Max(820f, num + 24f);
			this._questContent.offsetMin = new Vector2(0f, -num2);
			this._questContent.offsetMax = Vector2.zero;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00011B50 File Offset: 0x0000FD50
		private List<KeeperCheatMenuPlugin.QuestAssistEntry> GetActiveQuestAssistEntries()
		{
			List<KeeperCheatMenuPlugin.QuestAssistEntry> list = new List<KeeperCheatMenuPlugin.QuestAssistEntry>();
			MainGame instance = MainGame.Instance;
			QuestSystem questSystem = ((instance != null) ? instance.questSystem : null);
			QuestSystemData obj = ((questSystem == null || KeeperCheatMenuPlugin.QuestSystemDataGetter == null) ? null : (KeeperCheatMenuPlugin.QuestSystemDataGetter.Invoke(questSystem, null) as QuestSystemData));
			List<QuestData> list2;
			if (obj == null)
			{
				list2 = null;
			}
			else
			{
				QuestCollectionData questCollection = obj.questCollection;
				list2 = ((questCollection != null) ? questCollection.quests : null);
			}
			List<QuestData> list3 = list2;
			PlayerData playerData = KeeperCheatMenuPlugin.SafePlayer();
			if (list3 == null || playerData == null)
			{
				return list;
			}
			foreach (QuestData questData in list3.Where<QuestData>((QuestData candidate) => candidate != null && candidate.IsActiveQuest))
			{
				list.Add(this.AnalyseQuestForItemAssist(questData, playerData));
			}
			return list.OrderBy<KeeperCheatMenuPlugin.QuestAssistEntry, string>((KeeperCheatMenuPlugin.QuestAssistEntry entry) => KeeperCheatMenuPlugin.QuestDisplayName(entry.Quest), StringComparer.OrdinalIgnoreCase).ToList<KeeperCheatMenuPlugin.QuestAssistEntry>();
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00011C5C File Offset: 0x0000FE5C
		private KeeperCheatMenuPlugin.QuestAssistEntry AnalyseQuestForItemAssist(QuestData quest, PlayerData player)
		{
			KeeperCheatMenuPlugin.QuestAssistEntry questAssistEntry = new KeeperCheatMenuPlugin.QuestAssistEntry
			{
				Quest = quest
			};
			QuestFinishCheck questFinishCheck;
			if (quest == null)
			{
				questFinishCheck = null;
			}
			else
			{
				QuestDef definition = quest.Definition;
				questFinishCheck = ((definition != null) ? definition.finishCheck : null);
			}
			QuestFinishCheck questFinishCheck2 = questFinishCheck;
			if (questFinishCheck2 == null)
			{
				questAssistEntry.Detail = this.L("Protected: this quest has no standard item-delivery finish check.");
				return questAssistEntry;
			}
			try
			{
				if (questFinishCheck2.condExpressions != null)
				{
					if (questFinishCheck2.condExpressions.Any<LazyExpression>((LazyExpression expression) => expression != null && !expression.EvaluateBool()))
					{
						questAssistEntry.Detail = this.L("Protected: a friendship, story, day, order, or world condition is not ready.");
						return questAssistEntry;
					}
				}
			}
			catch
			{
				questAssistEntry.Detail = this.L("Protected: an unknown scripted condition could not be verified safely.");
				return questAssistEntry;
			}
			List<QuestPhraseRequirement> list = questFinishCheck2.phraseReqs ?? new List<QuestPhraseRequirement>();
			if (list.Any<QuestPhraseRequirement>((QuestPhraseRequirement requirement) => requirement == null || requirement.entity != null || requirement.requirement > 0))
			{
				questAssistEntry.Detail = this.L("Protected: this quest includes a non-item or non-consumable requirement.");
				return questAssistEntry;
			}
			List<ItemCount> list2 = (from requirement in list
				where requirement.itemCount != null && !string.IsNullOrEmpty(requirement.itemCount.itemId) && requirement.itemCount.count > 0
				select requirement.itemCount).ToList<ItemCount>();
			if (list2.Count == 0)
			{
				questAssistEntry.Detail = this.L("Protected: no supported item-delivery requirement was found.");
				return questAssistEntry;
			}
			foreach (IGrouping<string, ItemCount> grouping in from item in list2
				group item by item.itemId)
			{
				int num = grouping.Sum<ItemCount>((ItemCount item) => item.count);
				Inventory inventory = player.Inventory;
				int? num2;
				if (inventory == null)
				{
					num2 = null;
				}
				else
				{
					Item data = inventory.Data;
					num2 = ((data != null) ? new int?(data.GetTotalCountInInventory(grouping.Key, null, false)) : null);
				}
				int? num3 = num2;
				int valueOrDefault = num3.GetValueOrDefault();
				int num4 = Mathf.Max(0, num - valueOrDefault);
				if (num4 > 0)
				{
					questAssistEntry.MissingItems.Add(new ItemCount(grouping.Key, num4));
				}
			}
			if (questAssistEntry.MissingItems.Count == 0)
			{
				questAssistEntry.Detail = this.L("All required items are already available. Finish the quest normally.");
				return questAssistEntry;
			}
			questAssistEntry.CanSupply = true;
			KeeperCheatMenuPlugin.QuestAssistEntry questAssistEntry2 = questAssistEntry;
			string text = this.L("Missing");
			string text2 = ": ";
			string text3 = ", ";
			IEnumerable<ItemCount> missingItems = questAssistEntry.MissingItems;
			Func<ItemCount, string> func;
			if ((func = KeeperCheatMenuPlugin.__OCache.__f5_ItemCountDisplay) == null)
			{
				func = (KeeperCheatMenuPlugin.__OCache.__f5_ItemCountDisplay = new Func<ItemCount, string>(KeeperCheatMenuPlugin.ItemCountDisplay));
			}
			questAssistEntry2.Detail = text + text2 + string.Join(text3, missingItems.Select<ItemCount, string>(func));
			return questAssistEntry;
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00011F48 File Offset: 0x00010148
		private void SupplyMissingQuestItems(QuestData quest)
		{
			PlayerData playerData = KeeperCheatMenuPlugin.SafePlayer();
			if (playerData == null)
			{
				this.SetNativeStatus("Load or start a save first.");
				return;
			}
			KeeperCheatMenuPlugin.QuestAssistEntry questAssistEntry = this.AnalyseQuestForItemAssist(quest, playerData);
			if (!questAssistEntry.CanSupply)
			{
				this.SetNativeStatus("Quest item assistance is unavailable because the quest is protected or already ready.");
				this.RebuildQuestAssistList();
				return;
			}
			try
			{
				List<Item> list = questAssistEntry.MissingItems.SelectMany<ItemCount, Item>((ItemCount item) => item.CreateItems()).ToList<Item>();
				if (!playerData.Inventory.AddItemsToInventory(list))
				{
					throw new InvalidOperationException("Not enough inventory space.");
				}
				this.SetNativeStatus("Missing quest items supplied. Complete the quest through its normal NPC dialogue.");
			}
			catch (Exception ex)
			{
				this.SetNativeStatus("Could not supply quest items: " + ex.Message);
				ManualLogSource logger = base.Logger;
				string text = "Quest item assistance failed: ";
				Exception ex2 = ex;
				logger.LogWarning(text + ((ex2 != null) ? ex2.ToString() : null));
			}
			this.RebuildQuestAssistList();
		}

		// Token: 0x0600010C RID: 268 RVA: 0x00012038 File Offset: 0x00010238
		private static string QuestDisplayName(QuestData quest)
		{
			string text = string.Empty;
			try
			{
				text = ((quest != null) ? quest.Description : null) ?? string.Empty;
			}
			catch
			{
			}
			text = Regex.Replace(text, "<[^>]*>", string.Empty).Trim();
			string text2;
			if (quest == null)
			{
				text2 = null;
			}
			else
			{
				QuestDef definition = quest.Definition;
				text2 = ((definition != null) ? definition.id : null);
			}
			string text3 = text2 ?? "unknown";
			if (string.IsNullOrEmpty(text))
			{
				return text3;
			}
			if (text.Length > 76)
			{
				text = text.Substring(0, 75) + "…";
			}
			return text + "  [" + text3 + "]";
		}

		// Token: 0x0600010D RID: 269 RVA: 0x000120E8 File Offset: 0x000102E8
		private static string ItemCountDisplay(ItemCount item)
		{
			GameBalance me = GameBalance.Me;
			ItemDef itemDef;
			if (me == null)
			{
				itemDef = null;
			}
			else
			{
				List<ItemDef> itemDefs = me.itemDefs;
				itemDef = ((itemDefs != null) ? itemDefs.FirstOrDefault<ItemDef>((ItemDef candidate) => candidate != null && candidate.id == item.itemId) : null);
			}
			ItemDef itemDef2 = itemDef;
			string text = ((itemDef2 == null) ? item.itemId : KeeperCheatMenuPlugin.ShortItemName(itemDef2));
			return item.count.ToString() + "× " + text;
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x0600010E RID: 270 RVA: 0x00012160 File Offset: 0x00010360
		private int CurrentWorldDay
		{
			get
			{
				int num2;
				try
				{
					EnvironmentEngine instance = EnvironmentEngine.Instance;
					int? num;
					if (instance == null)
					{
						num = null;
					}
					else
					{
						EnvironmentData data = instance.Data;
						num = ((data != null) ? new int?(data.Day) : null);
					}
					num2 = num ?? (-1);
				}
				catch
				{
					num2 = -1;
				}
				return num2;
			}
		}

		// Token: 0x0600010F RID: 271 RVA: 0x000121CC File Offset: 0x000103CC
		internal void PrepareEverydaySermon()
		{
			if (!this.SermonEveryDayEnabled && !this.SermonRepeatableEnabled)
			{
				return;
			}
			int currentWorldDay = this.CurrentWorldDay;
			if (!this.SermonRepeatableEnabled && currentWorldDay >= 0 && this._lastEverydaySermonDay != null && this._lastEverydaySermonDay.Value == currentWorldDay)
			{
				return;
			}
			PlayerData playerData = MainGame.PlayerData;
			if (playerData == null)
			{
				return;
			}
			playerData.SetRes("sermon_ready", 1f);
		}

		// Token: 0x06000110 RID: 272 RVA: 0x0001222D File Offset: 0x0001042D
		internal void RecordEverydaySermon()
		{
			if ((!this.SermonEveryDayEnabled && !this.SermonRepeatableEnabled) || this._lastEverydaySermonDay == null)
			{
				return;
			}
			this._lastEverydaySermonDay.Value = this.CurrentWorldDay;
			base.Config.Save();
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00012264 File Offset: 0x00010464
		internal void StartPreparedZombieResurrections(bool repeatableTrigger = false)
		{
			if (!this.ZombiesEveryDayEnabled && (!repeatableTrigger || !this.ZombiesRepeatableEnabled))
			{
				return;
			}
			try
			{
				WorldData worldData = MainGame.WorldData;
				PlayerData playerData = MainGame.PlayerData;
				CraftDef craftDef = GameBalance.GetCraftDef("corpse_zombie_transition");
				List<WgoData> list = ((worldData != null) ? worldData.GetWgoDataList("resurrection_table_1") : null);
				if (playerData != null && craftDef != null && list != null)
				{
					int num = 0;
					foreach (WgoData wgoData in list)
					{
						if (wgoData != null && wgoData.GetGameResInt("resurrection_prepared") > 0 && wgoData.CraftComponent != null)
						{
							CraftElement craftElement = new CraftElement(craftDef.id, 1, new List<NeedItemData>(), new CraftParamsData(craftDef.id, new GameRes()));
							if (wgoData.CraftComponent.TryStartCraft(craftElement))
							{
								playerData.AddRes("cur_zombies_count", 1f);
								num++;
							}
						}
					}
					playerData.SetRes("resurrection_has_power", 0f);
					base.Logger.LogInfo("Everyday zombie resurrection started " + num.ToString() + " prepared table(s).");
				}
			}
			catch (Exception ex)
			{
				base.Logger.LogWarning("Could not start everyday zombie resurrections: " + ex.Message);
			}
		}

		// Token: 0x06000112 RID: 274 RVA: 0x000123BC File Offset: 0x000105BC
		private void ToggleSermonEveryDay()
		{
			this._sermonEveryDay.Value = !this._sermonEveryDay.Value;
			base.Config.Save();
			if (this._sermonEveryDay.Value)
			{
				this.PrepareEverydaySermon();
			}
			this.RefreshWorldToggleLabels();
		}

		// Token: 0x06000113 RID: 275 RVA: 0x000123FB File Offset: 0x000105FB
		private void ToggleSermonRepeatable()
		{
			this._sermonRepeatable.Value = !this._sermonRepeatable.Value;
			base.Config.Save();
			if (this._sermonRepeatable.Value)
			{
				this.PrepareEverydaySermon();
			}
			this.RefreshWorldToggleLabels();
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0001243A File Offset: 0x0001063A
		private void ToggleZombiesEveryDay()
		{
			this._zombiesEveryDay.Value = !this._zombiesEveryDay.Value;
			base.Config.Save();
			this.RefreshWorldToggleLabels();
		}

		// Token: 0x06000115 RID: 277 RVA: 0x00012466 File Offset: 0x00010666
		private void ToggleZombiesRepeatable()
		{
			this._zombiesRepeatable.Value = !this._zombiesRepeatable.Value;
			base.Config.Save();
			this.RefreshWorldToggleLabels();
		}

		// Token: 0x06000116 RID: 278 RVA: 0x00012492 File Offset: 0x00010692
		private void ToggleMapTeleportEverywhere()
		{
			this._mapTeleportEverywhere.Value = !this._mapTeleportEverywhere.Value;
			base.Config.Save();
			this.RefreshWorldToggleLabels();
		}

		// Token: 0x06000117 RID: 279 RVA: 0x000124C0 File Offset: 0x000106C0
		private void BuildZombieWorkManager(RectTransform page)
		{
			this.CreateText("MANAGE WORK", page, 20f, KeeperCheatMenuPlugin.TextGold, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -730f), new Vector2(-4f, -690f));
			this.CreateText("Choose an owned zombie, then open the crafting window of its assigned workstation.", page, 16f, KeeperCheatMenuPlugin.TextPale, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -778f), new Vector2(-4f, -736f));
			Button button = this.CreateButton("ZombieWorkSelector", page, string.Empty, new Vector2(0f, 1f), new Vector2(0.62f, 1f), new Vector2(0f, -842f), new Vector2(-8f, -790f), new Action(this.ToggleZombieWorkDropdown), 17f);
			this._zombieWorkSelectionText = button.GetComponentInChildren<TextMeshProUGUI>();
			this._zombieWorkSelectionText.alignment = TextAlignmentOptions.MidlineLeft;
			this._zombieWorkSelectionText.rectTransform.offsetMin = new Vector2(14f, 3f);
			this._zombieWorkSelectionText.rectTransform.offsetMax = new Vector2(-42f, -3f);
			this.CreateText("▼", button.transform, 19f, KeeperCheatMenuPlugin.TextGold, (TextAlignmentOptions)514, FontStyles.Normal, new Vector2(1f, 0f), Vector2.one, new Vector2(-40f, 2f), new Vector2(-6f, -2f));
			this._zombieWorkActionButton = this.CreateButton("OpenZombieWorkstation", page, string.Empty, new Vector2(0.62f, 1f), Vector2.one, new Vector2(8f, -842f), new Vector2(0f, -790f), new Action(this.OpenSelectedZombieWorkstation), 18f);
			this._zombieWorkActionText = this._zombieWorkActionButton.GetComponentInChildren<TextMeshProUGUI>();
			this._zombieWorkDropdownList = this.Rect("ZombieWorkDropdownList", page, new Vector2(0f, 1f), new Vector2(0.62f, 1f), new Vector2(0f, -1080f), new Vector2(-8f, -846f), KeeperCheatMenuPlugin.PanelOuter);
			RectTransform rectTransform = this.Rect("Viewport", this._zombieWorkDropdownList, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-22f, -4f), Color.clear);
			rectTransform.GetComponent<Image>().raycastTarget = true;
			rectTransform.gameObject.AddComponent<RectMask2D>();
			this._zombieWorkDropdownContent = this.Rect("Content", rectTransform, new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
			this._zombieWorkDropdownContent.pivot = new Vector2(0.5f, 1f);
			RectTransform rectTransform2 = this.Rect("Scrollbar", this._zombieWorkDropdownList, new Vector2(1f, 0f), Vector2.one, new Vector2(-18f, 6f), new Vector2(-5f, -6f), KeeperCheatMenuPlugin.HeaderDark);
			RectTransform rectTransform3 = this.Rect("Handle", rectTransform2, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f), KeeperCheatMenuPlugin.ButtonGold);
			Scrollbar scrollbar = rectTransform2.gameObject.AddComponent<Scrollbar>();
			scrollbar.handleRect = rectTransform3;
			scrollbar.targetGraphic = rectTransform3.GetComponent<Image>();
			scrollbar.direction = Scrollbar.Direction.BottomToTop;
			ScrollRect scrollRect = this._zombieWorkDropdownList.gameObject.AddComponent<ScrollRect>();
			scrollRect.viewport = rectTransform;
			scrollRect.content = this._zombieWorkDropdownContent;
			scrollRect.horizontal = false;
			scrollRect.vertical = true;
			scrollRect.movementType = ScrollRect.MovementType.Clamped;
			scrollRect.scrollSensitivity = 38f;
			scrollRect.verticalScrollbar = scrollbar;
			scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
			this._zombieWorkDropdownList.gameObject.SetActive(false);
			this.RefreshZombieWorkManager();
		}

		// Token: 0x06000118 RID: 280 RVA: 0x000128F4 File Offset: 0x00010AF4
		private void ToggleZombieWorkDropdown()
		{
			if (this._zombieWorkDropdownList == null)
			{
				return;
			}
			bool flag = !this._zombieWorkDropdownList.gameObject.activeSelf;
			this._zombieWorkDropdownList.gameObject.SetActive(flag);
			if (flag)
			{
				this.RebuildZombieWorkDropdown();
				this._zombieWorkDropdownList.SetAsLastSibling();
			}
		}

		// Token: 0x06000119 RID: 281 RVA: 0x0001294C File Offset: 0x00010B4C
		private void RefreshZombieWorkManager()
		{
			this.ReloadManagedZombies();
			ZombieWgoData zombieWgoData = this.GetSelectedManagedZombie();
			if (zombieWgoData == null && this._managedZombies.Count > 0)
			{
				zombieWgoData = this._managedZombies[0];
				this._selectedManagedZombieId = zombieWgoData.Id.Guid;
			}
			if (this._zombieWorkSelectionText != null)
			{
				this._zombieWorkSelectionText.text = ((zombieWgoData == null) ? this.L("No zombies available") : this.GetManagedZombieLabel(zombieWgoData));
			}
			object obj;
			if (zombieWgoData == null)
			{
				obj = null;
			}
			else
			{
				WgoData attachedWgoData = zombieWgoData.AttachedWgoData;
				obj = ((attachedWgoData != null) ? attachedWgoData.CraftComponent : null);
			}
			bool flag = obj != null;
			if (this._zombieWorkActionText != null)
			{
				this._zombieWorkActionText.text = this.L((zombieWgoData == null) ? "Select zombie" : (flag ? "Open workstation" : "Jobless"));
			}
			if (this._zombieWorkActionButton != null)
			{
				this._zombieWorkActionButton.interactable = flag;
			}
		}

		// Token: 0x0600011A RID: 282 RVA: 0x00012A34 File Offset: 0x00010C34
		private void ReloadManagedZombies()
		{
			this._managedZombies.Clear();
			try
			{
				ZombieSystemData zombieSystemData = MainGame.ZombieSystemData;
				if (((zombieSystemData != null) ? zombieSystemData.Cache : null) == null)
				{
					return;
				}
				this._managedZombies.AddRange((from zombie in zombieSystemData.Cache.Values
					where zombie != null
					group zombie by zombie.Id.Guid into @group
					select @group.First<ZombieWgoData>()).OrderBy<ZombieWgoData, string>(delegate(ZombieWgoData zombie)
				{
					if (!string.IsNullOrWhiteSpace(zombie.Name))
					{
						return zombie.Name;
					}
					return "~";
				}, StringComparer.OrdinalIgnoreCase).ThenBy<ZombieWgoData, Guid>((ZombieWgoData zombie) => zombie.Id.Guid));
			}
			catch (Exception ex)
			{
				base.Logger.LogWarning("Could not enumerate owned zombies: " + ex.Message);
			}
			if (this._selectedManagedZombieId != Guid.Empty && this._managedZombies.All<ZombieWgoData>((ZombieWgoData zombie) => zombie.Id.Guid != this._selectedManagedZombieId))
			{
				this._selectedManagedZombieId = Guid.Empty;
			}
		}

		// Token: 0x0600011B RID: 283 RVA: 0x00012B9C File Offset: 0x00010D9C
		private void RebuildZombieWorkDropdown()
		{
			if (this._zombieWorkDropdownContent == null)
			{
				return;
			}
			this.ReloadManagedZombies();
			for (int i = this._zombieWorkDropdownContent.childCount - 1; i >= 0; i--)
			{
				UnityEngine.Object.Destroy(this._zombieWorkDropdownContent.GetChild(i).gameObject);
			}
			int num = Math.Max(1, this._managedZombies.Count);
			float num2 = Math.Max(226f, (float)num * 48f + 8f);
			this._zombieWorkDropdownContent.offsetMin = new Vector2(0f, -num2);
			this._zombieWorkDropdownContent.offsetMax = Vector2.zero;
			if (this._managedZombies.Count == 0)
			{
				this.CreateText("No zombies available", this._zombieWorkDropdownContent, 17f, KeeperCheatMenuPlugin.TextPale, (TextAlignmentOptions)514, FontStyles.Normal, new Vector2(0f, 1f), Vector2.one, new Vector2(4f, -54f), new Vector2(-4f, -6f));
				return;
			}
			for (int j = 0; j < this._managedZombies.Count; j++)
			{
				ZombieWgoData zombieWgoData = this._managedZombies[j];
				Guid zombieId = zombieWgoData.Id.Guid;
				float num3 = -4f - (float)j * 48f;
				float num4 = num3 - 44f;
				TextMeshProUGUI componentInChildren = this.CreateButton("ZombieWork_" + j.ToString(), this._zombieWorkDropdownContent, this.GetManagedZombieLabel(zombieWgoData), new Vector2(0f, 1f), Vector2.one, new Vector2(4f, num4), new Vector2(-4f, num3), delegate
				{
					this.SelectManagedZombie(zombieId);
				}, 16f).GetComponentInChildren<TextMeshProUGUI>();
				componentInChildren.alignment = TextAlignmentOptions.MidlineLeft;
				componentInChildren.rectTransform.offsetMin = new Vector2(12f, 2f);
			}
		}

		// Token: 0x0600011C RID: 284 RVA: 0x00012D94 File Offset: 0x00010F94
		private string GetManagedZombieLabel(ZombieWgoData zombie)
		{
			if (zombie == null)
			{
				return this.L("Unnamed zombie");
			}
			string text = (string.IsNullOrWhiteSpace(zombie.Name) ? this.L("Unnamed zombie") : zombie.Name.Trim());
			int num = this._managedZombies.FindIndex((ZombieWgoData candidate) => candidate == zombie);
			int num2 = ((num < 0) ? 0 : this._managedZombies.Take<ZombieWgoData>(num).Count<ZombieWgoData>(delegate(ZombieWgoData candidate)
			{
				string text2;
				if (candidate == null)
				{
					text2 = null;
				}
				else
				{
					string name = candidate.Name;
					text2 = ((name != null) ? name.Trim() : null);
				}
				string name2 = zombie.Name;
				return string.Equals(text2, (name2 != null) ? name2.Trim() : null, StringComparison.OrdinalIgnoreCase);
			}));
			if (num2 != 0)
			{
				return text + " #" + (num2 + 1).ToString();
			}
			return text;
		}

		// Token: 0x0600011D RID: 285 RVA: 0x00012E4B File Offset: 0x0001104B
		private ZombieWgoData GetSelectedManagedZombie()
		{
			if (!(this._selectedManagedZombieId == Guid.Empty))
			{
				return this._managedZombies.FirstOrDefault<ZombieWgoData>((ZombieWgoData zombie) => zombie.Id.Guid == this._selectedManagedZombieId);
			}
			return null;
		}

		// Token: 0x0600011E RID: 286 RVA: 0x00012E78 File Offset: 0x00011078
		private void SelectManagedZombie(Guid zombieId)
		{
			this._selectedManagedZombieId = zombieId;
			if (this._zombieWorkDropdownList != null)
			{
				this._zombieWorkDropdownList.gameObject.SetActive(false);
			}
			this.RefreshZombieWorkManager();
		}

		// Token: 0x0600011F RID: 287 RVA: 0x00012EA8 File Offset: 0x000110A8
		private void OpenSelectedZombieWorkstation()
		{
			this.RefreshZombieWorkManager();
			ZombieWgoData selectedManagedZombie = this.GetSelectedManagedZombie();
			WgoData wgoData = ((selectedManagedZombie != null) ? selectedManagedZombie.AttachedWgoData : null);
			if (((wgoData != null) ? wgoData.CraftComponent : null) == null)
			{
				this.SetNativeStatus("This zombie is jobless.");
				return;
			}
			PlayerController playerController = MainGame.PlayerController;
			if (playerController == null)
			{
				this.SetNativeStatus("Load or start a save first.");
				return;
			}
			Wgo wgoViewGlobal = GameScene.GetWgoViewGlobal(wgoData.UniqueId);
			if (wgoViewGlobal == null || wgoViewGlobal.InteractionHandler == null)
			{
				this.SetNativeStatus("That workstation is not loaded. Travel to its area and try again.");
				return;
			}
			IWGOInteractionHandler interactionHandler = wgoViewGlobal.InteractionHandler;
			try
			{
				if (!interactionHandler.HasInteraction(playerController))
				{
					this.SetNativeStatus("That workstation cannot be opened in its current state.");
				}
				else
				{
					this.SetNativeMenuVisible(false);
					if (!interactionHandler.Interact(playerController))
					{
						this.SetNativeMenuVisible(true);
						this.ShowZombiesPage();
						this.SetNativeStatus("The game could not open that workstation.");
					}
				}
			}
			catch (Exception ex)
			{
				ManualLogSource logger = base.Logger;
				string text = "Could not open the selected zombie workstation: ";
				Exception ex2 = ex;
				logger.LogWarning(text + ((ex2 != null) ? ex2.ToString() : null));
				this.SetNativeMenuVisible(true);
				this.ShowZombiesPage();
				this.SetNativeStatus("Could not open workstation: " + ex.Message);
			}
		}

		// Token: 0x06000120 RID: 288 RVA: 0x00012FD0 File Offset: 0x000111D0
		private void ClearZombieWorkManagerReferences()
		{
			this._zombieWorkDropdownList = null;
			this._zombieWorkDropdownContent = null;
			this._zombieWorkSelectionText = null;
			this._zombieWorkActionText = null;
			this._zombieWorkActionButton = null;
		}

		// Token: 0x06000122 RID: 290 RVA: 0x000130D8 File Offset: 0x000112D8
		// Note: this type is marked as 'beforefieldinit'.
		static KeeperCheatMenuPlugin()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
			dictionary["Keeper Cheat Menu"] = "Keeper Cheat Menü";
			dictionary["MENU"] = "MENÜ";
			dictionary["Player"] = "Spieler";
			dictionary["Items"] = "Gegenstände";
			dictionary["Teleport"] = "Teleport";
			dictionary["Misc"] = "Sonstiges";
			dictionary["Crafting"] = "Herstellung";
			dictionary["Alchemy"] = "Alchemie";
			dictionary["Quests"] = "Quests";
			dictionary["World"] = "Welt";
			dictionary["Zombies"] = "Zombies";
			dictionary["Fix"] = "Reparatur";
			dictionary["PLAYER"] = "SPIELER";
			dictionary["CURRENCY"] = "WÄHRUNG";
			dictionary["TECHNOLOGY POINTS"] = "TECHNOLOGIEPUNKTE";
			dictionary["Gold"] = "Gold";
			dictionary["Silver (0-99)"] = "Silber (0–99)";
			dictionary["Copper (0-99)"] = "Kupfer (0–99)";
			dictionary["Restore health"] = "Gesundheit auffüllen";
			dictionary["Set insanity to 0"] = "Wahnsinn auf 0 setzen";
			dictionary["Add money"] = "Geld hinzufügen";
			dictionary["Red"] = "Rot";
			dictionary["Green"] = "Grün";
			dictionary["Blue"] = "Blau";
			dictionary["Infinite energy"] = "Unendliche Energie";
			dictionary["Infinite stamina"] = "Unendliche Ausdauer";
			dictionary["Invulnerable"] = "Unverwundbar";
			dictionary["Instant actions"] = "Sofortige Aktionen";
			dictionary["Sleep until wake key"] = "Schlafen bis Wecktaste";
			dictionary["Sleep without saving"] = "Schlafen ohne Speichern";
			dictionary["TOWN GRATITUDE"] = "STADT-DANKBARKEIT";
			dictionary["Add gratitude"] = "Dankbarkeit hinzufügen";
			dictionary["Always current maximum"] = "Immer aktuelles Maximum";
			dictionary["PLAYER MULTIPLIERS"] = "SPIELER-MULTIPLIKATOREN";
			dictionary["Harvest yield"] = "Ernteertrag";
			dictionary["Gratitude gain"] = "Dankbarkeitsgewinn";
			dictionary["Crop harvests, manually gathered resources, and positive town-gratitude rewards use these saved multipliers. Costs are not changed."] = "Pflanzenernten, manuell gesammelte Rohstoffe und positive Stadt-Dankbarkeit verwenden diese gespeicherten Multiplikatoren. Kosten bleiben unverändert.";
			dictionary["Player multipliers applied and saved."] = "Spieler-Multiplikatoren übernommen und gespeichert.";
			dictionary["Player multipliers reset to 1x."] = "Spieler-Multiplikatoren auf 1× zurückgesetzt.";
			dictionary["ON"] = "AN";
			dictionary["OFF"] = "AUS";
			dictionary["Search items..."] = "Gegenstände suchen...";
			dictionary["Location name..."] = "Name des Ortes...";
			dictionary["SAVE CURRENT LOCATION"] = "AKTUELLEN ORT SPEICHERN";
			dictionary["Save current location"] = "Aktuellen Ort speichern";
			dictionary["SAVED LOCATIONS"] = "GESPEICHERTE ORTE";
			dictionary["Delete selected"] = "Auswahl löschen";
			dictionary["TELEPORT HOTBAR"] = "TELEPORT-SCHNELLWAHL";
			dictionary["Go"] = "Los";
			dictionary["Select a saved location"] = "Gespeicherten Ort auswählen";
			dictionary["MISC"] = "SONSTIGES";
			dictionary["Harvest adjacent ready crops"] = "Benachbarte reife Pflanzen ernten";
			dictionary["Fish HUD"] = "Fischanzeige";
			dictionary["FINISH GROWING IN RANGE"] = "WACHSTUM IM UMKREIS ABSCHLIESSEN";
			dictionary["FISHING"] = "ANGELN";
			dictionary["Fish pond amount multiplier"] = "Fischbestand-Multiplikator";
			dictionary["Instant bite"] = "Sofortiger Biss";
			dictionary["Auto reel"] = "Automatisch einholen";
			dictionary["Allowed range: 1x to 100x. Loaded ponds refill each fish species to the new capacity."] = "Erlaubter Bereich: 1× bis 100×. Geladene Teiche füllen jede Fischart bis zur neuen Kapazität auf.";
			dictionary["Instant bite removes the waiting time after casting. Auto reel hooks the bite and completes the reeling minigame."] = "Sofortiger Biss entfernt die Wartezeit nach dem Auswerfen. Automatisch einholen setzt den Haken und schließt das Einhol-Minispiel ab.";
			dictionary["Fish pond amount multiplier applied and loaded ponds refilled."] = "Fischbestand-Multiplikator angewendet und geladene Teiche aufgefüllt.";
			dictionary["Free build"] = "Freies Bauen";
			dictionary["No placement restrictions"] = "Keine Baubeschränkungen";
			dictionary["SERMON SPEED MULTIPLIER"] = "PREDIGTGESCHWINDIGKEIT";
			dictionary["Apply"] = "Übernehmen";
			dictionary["Allowed range: 1x to 20x. Only the running sermon sequence is accelerated."] = "Erlaubter Bereich: 1× bis 20×. Nur die laufende Predigtsequenz wird beschleunigt.";
			dictionary["Shared storage for crafting"] = "Gemeinsames Lager zum Herstellen";
			dictionary["Crafting stations can use materials from eligible storage chests in every area, not only the current zone."] = "Werkstätten können Materialien aus geeigneten Lagertruhen in allen Gebieten verwenden, nicht nur aus der aktuellen Zone.";
			dictionary["LADDERS"] = "LEITERN";
			dictionary["Auto use ladders"] = "Leitern automatisch benutzen";
			dictionary["Ladder climb speed"] = "Klettergeschwindigkeit";
			dictionary["Allowed range: 0.1x to 20x. The multiplier is applied when a ladder climb starts."] = "Erlaubter Bereich: 0,1× bis 20×. Der Multiplikator wird beim Beginn des Kletterns angewendet.";
			dictionary["Ladder climb speed multiplier applied and saved."] = "Klettergeschwindigkeit angewendet und gespeichert.";
			dictionary["Free build removes grid snapping while keeping collision, build-area, and resource checks."] = "Freies Bauen entfernt das Einrasten im Raster. Kollisions-, Baubereichs- und Materialprüfungen bleiben aktiv.";
			dictionary["Range"] = "Umkreis";
			dictionary["Finish growing now"] = "Wachstum jetzt abschließen";
			dictionary["Finish nearby machines"] = "Nahe Maschinen abschließen";
			dictionary["Regrow nearby forage"] = "Sammelbares nachwachsen lassen";
			dictionary["CRAFTING"] = "HERSTELLUNG";
			dictionary["Craft items for free"] = "Kostenlos herstellen";
			dictionary["Build stuff for free"] = "Kostenlos bauen";
			dictionary["MACHINE SPEED MULTIPLIER"] = "MASCHINENGESCHWINDIGKEIT";
			dictionary["CRAFTED ITEM MULTIPLIER"] = "HERGESTELLTE GEGENSTÄNDE";
			dictionary["Allowed range: 0.1x to 100x. Machine speed affects automatic stations; output changes completed item stack sizes."] = "Erlaubter Bereich: 0,1× bis 100×. Maschinengeschwindigkeit betrifft automatische Stationen; die Ausgabemenge ändert fertige Gegenstandsstapel.";
			dictionary["Craft recipes without consuming their material ingredients. Required tools, recipe unlocks and other restrictions remain active."] = "Rezepte herstellen, ohne Materialien zu verbrauchen. Benötigte Werkzeuge, Rezeptfreischaltungen und andere Bedingungen bleiben aktiv.";
			dictionary["Build without consuming materials. Building limits and the placement options in Misc remain independent."] = "Bauen, ohne Materialien zu verbrauchen. Gebäudelimits und die Platzierungsoptionen unter Sonstiges bleiben unabhängig.";
			dictionary["These settings only remove resource costs; they do not unlock recipes or bypass building limits."] = "Diese Einstellungen entfernen nur Materialkosten; sie schalten keine Rezepte frei und umgehen keine Gebäudelimits.";
			dictionary["ALCHEMY"] = "ALCHEMIE";
			dictionary["Show folio beside alchemy table"] = "Foliant neben Alchemietisch anzeigen";
			dictionary["Folio ingredient selection"] = "Zutaten per Foliant auswählen";
			dictionary["Keep selected ingredients"] = "Ausgewählte Zutaten behalten";
			dictionary["Free research"] = "Kostenlose Forschung";
			dictionary["Click a usable ingredient in the companion folio to place it in the first free table slot. The item must exist in an accessible inventory."] = "Klicke im eingeblendeten Folianten auf eine verwendbare Zutat, um sie in den ersten freien Tisch-Slot zu legen. Der Gegenstand muss in einem zugänglichen Inventar vorhanden sein.";
			dictionary["Selected ingredient types return to their slots when the table is reopened, until you replace them or no matching material remains."] = "Ausgewählte Zutatentypen kehren beim erneuten Öffnen in ihre Slots zurück, bis du sie ersetzt oder kein passendes Material mehr vorhanden ist.";
			dictionary["Research at the study table without consuming the selected item, science, faith, or other requirements."] = "Am Studiertisch forschen, ohne den ausgewählten Gegenstand, Wissenschaft, Glauben oder andere Voraussetzungen zu verbrauchen.";
			dictionary["When an alchemy table opens, the laboratory window moves left and an interactive folio opens beside it as a cheat sheet."] = "Beim Öffnen eines Alchemietisches wird das Laborfenster nach links verschoben und daneben ein bedienbarer Foliant als Spickzettel geöffnet.";
			dictionary["Turning this option off keeps the original laboratory-window position and opening behavior."] = "Ist diese Option ausgeschaltet, bleiben Position und Öffnungsverhalten des Laborfensters unverändert.";
			dictionary["The folio uses the formulas and runes already known by the current save."] = "Der Foliant verwendet die Formeln und Runen, die im aktuellen Spielstand bereits bekannt sind.";
			dictionary["WORLD"] = "WELT";
			dictionary["Sermon every day"] = "Predigt jeden Tag";
			dictionary["Repeat sermons"] = "Predigten wiederholen";
			dictionary["Make zombies every day"] = "Zombies jeden Tag herstellen";
			dictionary["Repeat zombie making"] = "Zombie-Herstellung wiederholen";
			dictionary["Use map teleporters from everywhere"] = "Karten-Teleporter von überall verwenden";
			dictionary["DAY-SPECIFIC INTERACTIONS"] = "TAGESSPEZIFISCHE INTERAKTIONEN";
			dictionary["Pride-day interactions every day"] = "Hochmut-Interaktionen jeden Tag";
			dictionary["Lust-day interactions every day"] = "Wollust-Interaktionen jeden Tag";
			dictionary["Gluttony-day interactions every day"] = "Völlerei-Interaktionen jeden Tag";
			dictionary["Envy-day interactions every day"] = "Neid-Interaktionen jeden Tag";
			dictionary["Wrath-day interactions every day"] = "Zorn-Interaktionen jeden Tag";
			dictionary["Sloth-day interactions every day"] = "Trägheit-Interaktionen jeden Tag";
			dictionary["Placeholder"] = "Platzhalter";
			dictionary["Repeatable"] = "Mehrmals täglich";
			dictionary["The day-specific switches are visible placeholders for future game interactions and currently have no gameplay effect."] = "Die tagesspezifischen Schalter sind sichtbare Platzhalter für spätere Spielfunktionen und haben derzeit keine Auswirkung.";
			dictionary["ZOMBIES"] = "ZOMBIES";
			dictionary["MOVEMENT SPEED MULTIPLIER"] = "BEWEGUNGSGESCHWINDIGKEIT";
			dictionary["WORK SPEED MULTIPLIER"] = "ARBEITSGESCHWINDIGKEIT";
			dictionary["EXPERIENCE GAIN MULTIPLIERS"] = "ERFAHRUNGSMULTIPLIKATOREN";
			dictionary["Apply multipliers"] = "Multiplikatoren anwenden";
			dictionary["Reset to 1x"] = "Auf 1× zurücksetzen";
			dictionary["Collect finished products"] = "Fertige Produkte einsammeln";
			dictionary["Crafter zombies keep finished products in their work inventory and immediately continue the next cycle. A full inventory leaves the normal pickup order active."] = "Handwerkszombies behalten fertige Produkte in ihrem Arbeitsinventar und beginnen sofort den nächsten Durchlauf. Bei vollem Inventar bleibt der normale Abholauftrag bestehen.";
			dictionary["MANAGE WORK"] = "ARBEIT VERWALTEN";
			dictionary["Select zombie"] = "Zombie auswählen";
			dictionary["Open workstation"] = "Werkstatt öffnen";
			dictionary["Jobless"] = "Arbeitslos";
			dictionary["No zombies available"] = "Keine Zombies verfügbar";
			dictionary["Unnamed zombie"] = "Unbenannter Zombie";
			dictionary["Choose an owned zombie, then open the crafting window of its assigned workstation."] = "Wähle einen eigenen Zombie und öffne dann das Herstellungsfenster seiner zugewiesenen Werkstatt.";
			dictionary["This zombie is jobless."] = "Dieser Zombie ist arbeitslos.";
			dictionary["That workstation is not loaded. Travel to its area and try again."] = "Diese Werkstatt ist nicht geladen. Reise in ihr Gebiet und versuche es erneut.";
			dictionary["That workstation cannot be opened in its current state."] = "Diese Werkstatt kann in ihrem aktuellen Zustand nicht geöffnet werden.";
			dictionary["FIX"] = "REPARATUR";
			dictionary["RESET COMPOSTER"] = "KOMPOSTER ZURÜCKSETZEN";
			dictionary["Reset nearest composter"] = "Nächsten Komposter zurücksetzen";
			dictionary["RESET GARDEN BED"] = "GARTENBEET ZURÜCKSETZEN";
			dictionary["Reset nearest garden bed"] = "Nächstes Gartenbeet zurücksetzen";
			dictionary["REPAIR INVENTORY"] = "INVENTAR REPARIEREN";
			dictionary["Repair broken inventory item"] = "Defekten Inventareintrag reparieren";
			dictionary["Clear entire inventory"] = "Gesamtes Inventar leeren";
			dictionary["REPAIR ZOMBIE JOBS"] = "ZOMBIE-ARBEIT REPARIEREN";
			dictionary["Repair loaded zombie jobs"] = "Geladene Zombie-Arbeit reparieren";
			dictionary["Zombie job fix (not tested)"] = "Zombie-Arbeitsfix (nicht getestet)";
			dictionary["Repairs loaded zombies that still have an assigned station but lost their live worker link or stopped activity. Existing assignments and craft queues are preserved."] = "Repariert geladene Zombies, deren Station noch zugewiesen ist, aber deren aktive Arbeitsverbindung oder Tätigkeit verloren ging. Zuweisungen und Warteschlangen bleiben erhalten.";
			dictionary["Choose a cheat. Changes apply to the current save."] = "Wähle eine Funktion. Änderungen gelten für den aktuellen Spielstand.";
			dictionary["Load or start a save first."] = "Lade oder starte zuerst einen Spielstand.";
			dictionary["Language"] = "Sprache";
			dictionary["Hotkey"] = "Taste";
			dictionary["Press a key..."] = "Taste drücken...";
			dictionary["Unassigned"] = "Nicht belegt";
			dictionary["Save-safe tools only. Quest and story-state editing remains disabled in this build."] = "Nur spielstandsichere Werkzeuge. Quest- und Storyzustände bleiben in dieser Version deaktiviert.";
			dictionary["The preset name is saved with its scene and exact XYZ coordinates."] = "Der Name wird zusammen mit der Szene und den genauen XYZ-Koordinaten gespeichert.";
			dictionary["After you harvest one crop, every touching crop that is ready and has the same type is harvested too."] = "Nach dem Ernten werden alle angrenzenden reifen Pflanzen desselben Typs ebenfalls geerntet.";
			dictionary["The hotkey matures growing crops around the player without harvesting them. Range: 1-30."] = "Die Taste lässt Pflanzen im Umkreis vollständig wachsen, ohne sie zu ernten. Umkreis: 1–30.";
			dictionary["Machines: compost, ovens, forges, etc. Forage: harvested berries, mushrooms, and wild honey. All three actions use the range above."] = "Maschinen: Komposter, Öfen, Schmieden usw. Sammelbares: Beeren, Pilze und Wildhonig. Alle Aktionen verwenden den obigen Umkreis.";
			dictionary["Multipliers affect every owned zombie and are saved between game sessions."] = "Multiplikatoren gelten für alle eigenen Zombies und werden dauerhaft gespeichert.";
			dictionary["Allowed range: 0.1x to 100x. New experience rewards use the red, green, and blue values separately."] = "Erlaubter Bereich: 0,1× bis 100×. Neue Erfahrung wird für Rot, Grün und Blau getrennt berechnet.";
			dictionary["Stand next to the broken composter. This repairs its interaction state and clears a stuck active craft. Any committed craft inputs are returned as drops beside the composter."] = "Stelle dich neben den defekten Komposter. Dies repariert die Interaktion und entfernt einen festhängenden Vorgang. Eingesetzte Materialien werden daneben abgelegt.";
			dictionary["Stand next to the broken garden bed. This clears its stuck crop or craft state, restores interaction, and rebuilds its approach path."] = "Stelle dich neben das defekte Gartenbeet. Dies entfernt festhängende Pflanzen- oder Herstellungsdaten, stellt die Interaktion wieder her und baut den Zugangsweg neu auf.";
			dictionary["Repair removes only null, empty, or unknown item entries that can crash the inventory screen. Clear inventory is a two-click emergency fallback and removes everything."] = "Die Reparatur entfernt nur ungültige, leere oder unbekannte Einträge, die das Inventar abstürzen lassen. Inventar leeren ist ein Notfallknopf mit doppelter Bestätigung und entfernt alles.";
			dictionary["Use fix tools only when the matching object is stuck. Save and reload first when possible."] = "Reparaturwerkzeuge nur bei festhängenden Objekten verwenden. Wenn möglich zuerst speichern und neu laden.";
			dictionary["QUEST ASSIST"] = "QUEST-HILFE";
			dictionary["Refresh"] = "Aktualisieren";
			dictionary["Supply missing items"] = "Fehlende Gegenstände geben";
			dictionary["Protected"] = "Geschützt";
			dictionary["Missing"] = "Fehlt";
			dictionary["Only missing item-delivery requirements can be supplied. Friendship, reputation, story, day, order, and unknown conditions remain protected."] = "Nur fehlende Gegenstände für Lieferaufgaben können bereitgestellt werden. Freundschaft, Ruf, Geschichte, Tag, Reihenfolge und unbekannte Bedingungen bleiben geschützt.";
			dictionary["No active quests are available in the current save."] = "Im aktuellen Spielstand sind keine aktiven Quests verfügbar.";
			dictionary["Protected: this quest has no standard item-delivery finish check."] = "Geschützt: Diese Quest besitzt keine normale Gegenstandsabgabe.";
			dictionary["Protected: a friendship, story, day, order, or world condition is not ready."] = "Geschützt: Eine Freundschafts-, Story-, Tages-, Reihenfolge- oder Weltbedingung ist noch nicht erfüllt.";
			dictionary["Protected: an unknown scripted condition could not be verified safely."] = "Geschützt: Eine unbekannte Skriptbedingung konnte nicht sicher geprüft werden.";
			dictionary["Protected: this quest includes a non-item or non-consumable requirement."] = "Geschützt: Diese Quest enthält eine Bedingung, die kein verbrauchbarer Gegenstand ist.";
			dictionary["Protected: no supported item-delivery requirement was found."] = "Geschützt: Keine unterstützte Gegenstandsabgabe gefunden.";
			dictionary["All required items are already available. Finish the quest normally."] = "Alle benötigten Gegenstände sind bereits vorhanden. Schließe die Quest normal ab.";
			dictionary["Quest item assistance is unavailable because the quest is protected or already ready."] = "Gegenstandshilfe ist nicht verfügbar, da die Quest geschützt oder bereits bereit ist.";
			dictionary["Missing quest items supplied. Complete the quest through its normal NPC dialogue."] = "Fehlende Questgegenstände wurden gegeben. Schließe die Quest über den normalen NPC-Dialog ab.";
			KeeperCheatMenuPlugin.GermanText = dictionary;
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.Ordinal);
			dictionary2["Keeper Cheat Menu"] = "키퍼 치트 메뉴";
			dictionary2["MENU"] = "메뉴";
			dictionary2["Player"] = "플레이어";
			dictionary2["Items"] = "아이템";
			dictionary2["Teleport"] = "텔레포트";
			dictionary2["Misc"] = "기타";
			dictionary2["Crafting"] = "제작";
			dictionary2["Alchemy"] = "연금술";
			dictionary2["Quests"] = "퀘스트";
			dictionary2["World"] = "세계";
			dictionary2["Zombies"] = "좀비";
			dictionary2["Fix"] = "복구";
			dictionary2["PLAYER"] = "플레이어";
			dictionary2["CURRENCY"] = "화폐";
			dictionary2["TECHNOLOGY POINTS"] = "기술 포인트";
			dictionary2["Gold"] = "금";
			dictionary2["Silver (0-99)"] = "은 (0–99)";
			dictionary2["Copper (0-99)"] = "동 (0–99)";
			dictionary2["Restore health"] = "체력 회복";
			dictionary2["Set insanity to 0"] = "광기 0으로 설정";
			dictionary2["Add money"] = "돈 추가";
			dictionary2["Red"] = "빨강";
			dictionary2["Green"] = "초록";
			dictionary2["Blue"] = "파랑";
			dictionary2["Infinite energy"] = "무한 에너지";
			dictionary2["Infinite stamina"] = "무한 스태미나";
			dictionary2["Invulnerable"] = "무적";
			dictionary2["Instant actions"] = "즉시 행동";
			dictionary2["Sleep until wake key"] = "깨우기 키까지 수면";
			dictionary2["Sleep without saving"] = "저장 없이 수면";
			dictionary2["TOWN GRATITUDE"] = "마을 감사도";
			dictionary2["Add gratitude"] = "감사도 추가";
			dictionary2["Always current maximum"] = "항상 현재 최대치";
			dictionary2["PLAYER MULTIPLIERS"] = "플레이어 배율";
			dictionary2["Harvest yield"] = "수확량";
			dictionary2["Gratitude gain"] = "감사도 획득량";
			dictionary2["Crop harvests, manually gathered resources, and positive town-gratitude rewards use these saved multipliers. Costs are not changed."] = "작물 수확물, 직접 채집한 자원, 양의 마을 감사도 보상에 저장된 배율을 적용합니다. 비용은 변경되지 않습니다.";
			dictionary2["Player multipliers applied and saved."] = "플레이어 배율을 적용하고 저장했습니다.";
			dictionary2["Player multipliers reset to 1x."] = "플레이어 배율을 1배로 초기화했습니다.";
			dictionary2["ON"] = "켜짐";
			dictionary2["OFF"] = "꺼짐";
			dictionary2["Search items..."] = "아이템 검색...";
			dictionary2["Location name..."] = "위치 이름...";
			dictionary2["SAVE CURRENT LOCATION"] = "현재 위치 저장";
			dictionary2["Save current location"] = "현재 위치 저장";
			dictionary2["SAVED LOCATIONS"] = "저장된 위치";
			dictionary2["Delete selected"] = "선택 삭제";
			dictionary2["TELEPORT HOTBAR"] = "텔레포트 단축 목록";
			dictionary2["Go"] = "이동";
			dictionary2["Select a saved location"] = "저장된 위치 선택";
			dictionary2["MISC"] = "기타";
			dictionary2["Harvest adjacent ready crops"] = "인접한 수확 가능 작물 수확";
			dictionary2["Fish HUD"] = "낚시 정보";
			dictionary2["FINISH GROWING IN RANGE"] = "범위 내 성장 완료";
			dictionary2["FISHING"] = "낚시";
			dictionary2["Fish pond amount multiplier"] = "연못 물고기 수량 배율";
			dictionary2["Instant bite"] = "즉시 입질";
			dictionary2["Auto reel"] = "자동 릴 감기";
			dictionary2["Allowed range: 1x to 100x. Loaded ponds refill each fish species to the new capacity."] = "허용 범위: 1배~100배. 로드된 연못의 각 물고기 종류가 새 최대 수량까지 채워집니다.";
			dictionary2["Instant bite removes the waiting time after casting. Auto reel hooks the bite and completes the reeling minigame."] = "즉시 입질은 캐스팅 후 대기 시간을 없앱니다. 자동 릴 감기는 입질을 걸고 릴 미니게임을 완료합니다.";
			dictionary2["Fish pond amount multiplier applied and loaded ponds refilled."] = "연못 물고기 수량 배율을 적용하고 로드된 연못을 채웠습니다.";
			dictionary2["Free build"] = "자유 건설";
			dictionary2["No placement restrictions"] = "배치 제한 없음";
			dictionary2["SERMON SPEED MULTIPLIER"] = "설교 속도 배율";
			dictionary2["Apply"] = "적용";
			dictionary2["Allowed range: 1x to 20x. Only the running sermon sequence is accelerated."] = "허용 범위: 1배~20배. 진행 중인 설교 장면만 가속됩니다.";
			dictionary2["Shared storage for crafting"] = "제작용 공유 보관함";
			dictionary2["Crafting stations can use materials from eligible storage chests in every area, not only the current zone."] = "제작대가 현재 구역뿐 아니라 모든 구역의 사용 가능한 보관함 재료를 사용할 수 있습니다.";
			dictionary2["LADDERS"] = "사다리";
			dictionary2["Auto use ladders"] = "사다리 자동 사용";
			dictionary2["Ladder climb speed"] = "사다리 오르기 속도";
			dictionary2["Allowed range: 0.1x to 20x. The multiplier is applied when a ladder climb starts."] = "허용 범위: 0.1배~20배. 사다리를 오르기 시작할 때 배율이 적용됩니다.";
			dictionary2["Ladder climb speed multiplier applied and saved."] = "사다리 오르기 속도 배율을 적용하고 저장했습니다.";
			dictionary2["Free build removes grid snapping while keeping collision, build-area, and resource checks."] = "자유 건설은 격자 맞춤을 해제하지만 충돌, 건설 구역 및 재료 검사는 유지합니다.";
			dictionary2["Range"] = "범위";
			dictionary2["Finish growing now"] = "지금 성장 완료";
			dictionary2["Finish nearby machines"] = "주변 기계 즉시 완료";
			dictionary2["Regrow nearby forage"] = "주변 채집물 재생";
			dictionary2["CRAFTING"] = "제작";
			dictionary2["Craft items for free"] = "무료 아이템 제작";
			dictionary2["Build stuff for free"] = "무료 건설";
			dictionary2["MACHINE SPEED MULTIPLIER"] = "기계 속도 배율";
			dictionary2["CRAFTED ITEM MULTIPLIER"] = "제작 아이템 배율";
			dictionary2["Allowed range: 0.1x to 100x. Machine speed affects automatic stations; output changes completed item stack sizes."] = "허용 범위: 0.1배~100배. 기계 속도는 자동 작업대에 적용되며 출력 배율은 완성된 아이템 수량을 변경합니다.";
			dictionary2["Craft recipes without consuming their material ingredients. Required tools, recipe unlocks and other restrictions remain active."] = "재료를 소모하지 않고 제작합니다. 필요한 도구, 레시피 해금 및 기타 제한은 유지됩니다.";
			dictionary2["Build without consuming materials. Building limits and the placement options in Misc remain independent."] = "재료를 소모하지 않고 건설합니다. 건설 개수 제한과 기타 메뉴의 배치 옵션은 별도로 적용됩니다.";
			dictionary2["These settings only remove resource costs; they do not unlock recipes or bypass building limits."] = "이 설정은 재료 비용만 제거하며 레시피를 해금하거나 건설 제한을 우회하지 않습니다.";
			dictionary2["ALCHEMY"] = "연금술";
			dictionary2["Show folio beside alchemy table"] = "연금술대 옆에 책 표시";
			dictionary2["Folio ingredient selection"] = "책에서 재료 선택";
			dictionary2["Keep selected ingredients"] = "선택한 재료 유지";
			dictionary2["Free research"] = "무료 연구";
			dictionary2["Click a usable ingredient in the companion folio to place it in the first free table slot. The item must exist in an accessible inventory."] = "함께 표시된 책에서 사용 가능한 재료를 클릭하면 연금술대의 첫 번째 빈 칸에 놓입니다. 접근 가능한 인벤토리에 해당 아이템이 있어야 합니다.";
			dictionary2["Selected ingredient types return to their slots when the table is reopened, until you replace them or no matching material remains."] = "선택한 재료 종류는 교체하거나 같은 재료가 없어질 때까지 연금술대를 다시 열면 해당 칸에 복원됩니다.";
			dictionary2["Research at the study table without consuming the selected item, science, faith, or other requirements."] = "연구대에서 선택한 아이템, 과학, 신앙 또는 기타 요구 자원을 소모하지 않고 연구합니다.";
			dictionary2["When an alchemy table opens, the laboratory window moves left and an interactive folio opens beside it as a cheat sheet."] = "연금술대를 열면 실험실 창이 왼쪽으로 이동하고 옆에 조작 가능한 참고용 책이 열립니다.";
			dictionary2["Turning this option off keeps the original laboratory-window position and opening behavior."] = "이 옵션을 끄면 실험실 창의 기본 위치와 열기 동작이 유지됩니다.";
			dictionary2["The folio uses the formulas and runes already known by the current save."] = "책에는 현재 저장 파일에서 이미 알고 있는 공식과 룬이 표시됩니다.";
			dictionary2["WORLD"] = "세계";
			dictionary2["Sermon every day"] = "매일 설교";
			dictionary2["Repeat sermons"] = "설교 반복";
			dictionary2["Make zombies every day"] = "매일 좀비 제작";
			dictionary2["Repeat zombie making"] = "좀비 제작 반복";
			dictionary2["Use map teleporters from everywhere"] = "어디서나 지도 텔레포터 사용";
			dictionary2["DAY-SPECIFIC INTERACTIONS"] = "요일별 상호작용";
			dictionary2["Pride-day interactions every day"] = "교만의 날 상호작용을 매일";
			dictionary2["Lust-day interactions every day"] = "욕망의 날 상호작용을 매일";
			dictionary2["Gluttony-day interactions every day"] = "폭식의 날 상호작용을 매일";
			dictionary2["Envy-day interactions every day"] = "질투의 날 상호작용을 매일";
			dictionary2["Wrath-day interactions every day"] = "분노의 날 상호작용을 매일";
			dictionary2["Sloth-day interactions every day"] = "나태의 날 상호작용을 매일";
			dictionary2["Placeholder"] = "준비 중";
			dictionary2["Repeatable"] = "반복 가능";
			dictionary2["The day-specific switches are visible placeholders for future game interactions and currently have no gameplay effect."] = "요일별 스위치는 향후 기능을 위한 표시용 항목이며 현재 게임에는 영향을 주지 않습니다.";
			dictionary2["ZOMBIES"] = "좀비";
			dictionary2["MOVEMENT SPEED MULTIPLIER"] = "이동 속도 배율";
			dictionary2["WORK SPEED MULTIPLIER"] = "작업 속도 배율";
			dictionary2["EXPERIENCE GAIN MULTIPLIERS"] = "경험치 획득 배율";
			dictionary2["Apply multipliers"] = "배율 적용";
			dictionary2["Reset to 1x"] = "1배로 초기화";
			dictionary2["Collect finished products"] = "완성품 자동 수거";
			dictionary2["Crafter zombies keep finished products in their work inventory and immediately continue the next cycle. A full inventory leaves the normal pickup order active."] = "제작 좀비가 완성품을 작업 인벤토리에 보관하고 다음 작업을 즉시 시작합니다. 인벤토리가 가득 차면 일반 수거 명령이 유지됩니다.";
			dictionary2["MANAGE WORK"] = "작업 관리";
			dictionary2["Select zombie"] = "좀비 선택";
			dictionary2["Open workstation"] = "작업대 열기";
			dictionary2["Jobless"] = "무직";
			dictionary2["No zombies available"] = "사용 가능한 좀비 없음";
			dictionary2["Unnamed zombie"] = "이름 없는 좀비";
			dictionary2["Choose an owned zombie, then open the crafting window of its assigned workstation."] = "보유한 좀비를 선택한 뒤 배정된 작업대의 제작 창을 여세요.";
			dictionary2["This zombie is jobless."] = "이 좀비는 작업이 없습니다.";
			dictionary2["That workstation is not loaded. Travel to its area and try again."] = "해당 작업대가 로드되지 않았습니다. 그 지역으로 이동한 뒤 다시 시도하세요.";
			dictionary2["That workstation cannot be opened in its current state."] = "현재 상태에서는 해당 작업대를 열 수 없습니다.";
			dictionary2["FIX"] = "복구";
			dictionary2["RESET COMPOSTER"] = "퇴비통 초기화";
			dictionary2["Reset nearest composter"] = "가장 가까운 퇴비통 초기화";
			dictionary2["RESET GARDEN BED"] = "텃밭 초기화";
			dictionary2["Reset nearest garden bed"] = "가장 가까운 텃밭 초기화";
			dictionary2["REPAIR INVENTORY"] = "인벤토리 복구";
			dictionary2["Repair broken inventory item"] = "손상된 인벤토리 항목 복구";
			dictionary2["Clear entire inventory"] = "인벤토리 전체 비우기";
			dictionary2["REPAIR ZOMBIE JOBS"] = "좀비 작업 복구";
			dictionary2["Repair loaded zombie jobs"] = "불러온 좀비 작업 복구";
			dictionary2["Zombie job fix (not tested)"] = "좀비 작업 복구 (테스트 안 됨)";
			dictionary2["Repairs loaded zombies that still have an assigned station but lost their live worker link or stopped activity. Existing assignments and craft queues are preserved."] = "작업장이 배정되어 있지만 연결이나 작업이 중단된 불러온 좀비를 복구합니다. 기존 배정과 제작 대기열은 유지됩니다.";
			dictionary2["Choose a cheat. Changes apply to the current save."] = "기능을 선택하세요. 변경 사항은 현재 저장 파일에 적용됩니다.";
			dictionary2["Load or start a save first."] = "먼저 저장 파일을 불러오거나 새 게임을 시작하세요.";
			dictionary2["Language"] = "언어";
			dictionary2["Hotkey"] = "단축키";
			dictionary2["Press a key..."] = "키를 누르세요...";
			dictionary2["Unassigned"] = "미지정";
			dictionary2["Save-safe tools only. Quest and story-state editing remains disabled in this build."] = "저장 파일에 안전한 기능만 제공합니다. 퀘스트와 스토리 상태 편집은 비활성화되어 있습니다.";
			dictionary2["The preset name is saved with its scene and exact XYZ coordinates."] = "이름은 장면 및 정확한 XYZ 좌표와 함께 저장됩니다.";
			dictionary2["After you harvest one crop, every touching crop that is ready and has the same type is harvested too."] = "작물 하나를 수확하면 인접한 같은 종류의 수확 가능한 작물도 함께 수확됩니다.";
			dictionary2["The hotkey matures growing crops around the player without harvesting them. Range: 1-30."] = "단축키로 플레이어 주변 작물을 수확하지 않고 즉시 성장시킵니다. 범위: 1–30.";
			dictionary2["Machines: compost, ovens, forges, etc. Forage: harvested berries, mushrooms, and wild honey. All three actions use the range above."] = "기계: 퇴비통, 화덕, 대장간 등. 채집물: 베리, 버섯, 야생 꿀. 모든 기능은 위 범위를 사용합니다.";
			dictionary2["Multipliers affect every owned zombie and are saved between game sessions."] = "배율은 보유한 모든 좀비에 적용되며 게임을 종료해도 저장됩니다.";
			dictionary2["Allowed range: 0.1x to 100x. New experience rewards use the red, green, and blue values separately."] = "허용 범위: 0.1배~100배. 빨강, 초록, 파랑 경험치가 각각 적용됩니다.";
			dictionary2["Stand next to the broken composter. This repairs its interaction state and clears a stuck active craft. Any committed craft inputs are returned as drops beside the composter."] = "고장 난 퇴비통 옆에 서세요. 상호작용 상태를 복구하고 멈춘 작업을 제거합니다. 투입한 재료는 퇴비통 옆에 떨어집니다.";
			dictionary2["Stand next to the broken garden bed. This clears its stuck crop or craft state, restores interaction, and rebuilds its approach path."] = "고장 난 텃밭 옆에 서세요. 멈춘 작물 또는 제작 상태를 지우고 상호작용과 접근 경로를 복구합니다.";
			dictionary2["Repair removes only null, empty, or unknown item entries that can crash the inventory screen. Clear inventory is a two-click emergency fallback and removes everything."] = "복구는 인벤토리 화면 오류를 일으키는 잘못되거나 비어 있거나 알 수 없는 항목만 제거합니다. 전체 비우기는 두 번 눌러 확인하는 비상 기능이며 모든 항목을 제거합니다.";
			dictionary2["Use fix tools only when the matching object is stuck. Save and reload first when possible."] = "대상이 멈춘 경우에만 복구 기능을 사용하세요. 가능하면 먼저 저장 후 다시 불러오세요.";
			dictionary2["QUEST ASSIST"] = "퀘스트 지원";
			dictionary2["Refresh"] = "새로 고침";
			dictionary2["Supply missing items"] = "부족한 아이템 지급";
			dictionary2["Protected"] = "보호됨";
			dictionary2["Missing"] = "부족";
			dictionary2["Only missing item-delivery requirements can be supplied. Friendship, reputation, story, day, order, and unknown conditions remain protected."] = "아이템 전달에 필요한 부족분만 지급할 수 있습니다. 친밀도, 평판, 스토리, 요일, 순서 및 알 수 없는 조건은 보호됩니다.";
			dictionary2["No active quests are available in the current save."] = "현재 저장 파일에 활성 퀘스트가 없습니다.";
			dictionary2["Protected: this quest has no standard item-delivery finish check."] = "보호됨: 표준 아이템 전달 완료 조건이 없습니다.";
			dictionary2["Protected: a friendship, story, day, order, or world condition is not ready."] = "보호됨: 친밀도, 스토리, 요일, 순서 또는 월드 조건이 충족되지 않았습니다.";
			dictionary2["Protected: an unknown scripted condition could not be verified safely."] = "보호됨: 알 수 없는 스크립트 조건을 안전하게 확인할 수 없습니다.";
			dictionary2["Protected: this quest includes a non-item or non-consumable requirement."] = "보호됨: 소모 아이템이 아닌 조건이 포함되어 있습니다.";
			dictionary2["Protected: no supported item-delivery requirement was found."] = "보호됨: 지원되는 아이템 전달 조건을 찾지 못했습니다.";
			dictionary2["All required items are already available. Finish the quest normally."] = "필요한 아이템을 이미 모두 보유하고 있습니다. 정상적으로 퀘스트를 완료하세요.";
			dictionary2["Quest item assistance is unavailable because the quest is protected or already ready."] = "퀘스트가 보호되어 있거나 이미 완료 준비가 되어 있어 아이템 지원을 사용할 수 없습니다.";
			dictionary2["Missing quest items supplied. Complete the quest through its normal NPC dialogue."] = "부족한 퀘스트 아이템을 지급했습니다. 정상 NPC 대화로 퀘스트를 완료하세요.";
			KeeperCheatMenuPlugin.KoreanText = dictionary2;
			Dictionary<string, string> dictionary3 = new Dictionary<string, string>(StringComparer.Ordinal);
			dictionary3["Keeper Cheat Menu"] = "守墓人作弊菜单";
			dictionary3["MENU"] = "菜单";
			dictionary3["Player"] = "玩家";
			dictionary3["Items"] = "物品";
			dictionary3["Teleport"] = "传送";
			dictionary3["Misc"] = "杂项";
			dictionary3["Crafting"] = "制作";
			dictionary3["Alchemy"] = "炼金";
			dictionary3["Quests"] = "任务";
			dictionary3["World"] = "世界";
			dictionary3["Zombies"] = "僵尸";
			dictionary3["Fix"] = "修复";
			dictionary3["PLAYER"] = "玩家";
			dictionary3["CURRENCY"] = "货币";
			dictionary3["TECHNOLOGY POINTS"] = "科技点";
			dictionary3["Gold"] = "金币";
			dictionary3["Silver (0-99)"] = "银币 (0–99)";
			dictionary3["Copper (0-99)"] = "铜币 (0–99)";
			dictionary3["Restore health"] = "恢复生命";
			dictionary3["Set insanity to 0"] = "将疯狂值设为 0";
			dictionary3["Add money"] = "增加金钱";
			dictionary3["Red"] = "红色";
			dictionary3["Green"] = "绿色";
			dictionary3["Blue"] = "蓝色";
			dictionary3["Infinite energy"] = "无限能量";
			dictionary3["Infinite stamina"] = "无限耐力";
			dictionary3["Invulnerable"] = "无敌";
			dictionary3["Instant actions"] = "瞬间行动";
			dictionary3["Sleep until wake key"] = "睡到按下唤醒键";
			dictionary3["Sleep without saving"] = "睡眠时不保存";
			dictionary3["TOWN GRATITUDE"] = "城镇感激度";
			dictionary3["Add gratitude"] = "增加感激度";
			dictionary3["Always current maximum"] = "始终保持当前上限";
			dictionary3["PLAYER MULTIPLIERS"] = "玩家倍率";
			dictionary3["Harvest yield"] = "采集产量";
			dictionary3["Gratitude gain"] = "感激度获取";
			dictionary3["Crop harvests, manually gathered resources, and positive town-gratitude rewards use these saved multipliers. Costs are not changed."] = "作物、手动采集资源和正向城镇感激度奖励使用已保存的倍率。消耗不会改变。";
			dictionary3["Player multipliers applied and saved."] = "玩家倍率已应用并保存。";
			dictionary3["Player multipliers reset to 1x."] = "玩家倍率已重置为 1 倍。";
			dictionary3["ON"] = "开";
			dictionary3["OFF"] = "关";
			dictionary3["Search items..."] = "搜索物品...";
			dictionary3["Location name..."] = "地点名称...";
			dictionary3["SAVE CURRENT LOCATION"] = "保存当前位置";
			dictionary3["Save current location"] = "保存当前位置";
			dictionary3["SAVED LOCATIONS"] = "已保存地点";
			dictionary3["Delete selected"] = "删除所选";
			dictionary3["TELEPORT HOTBAR"] = "传送快捷栏";
			dictionary3["Go"] = "前往";
			dictionary3["Select a saved location"] = "选择已保存地点";
			dictionary3["MISC"] = "杂项";
			dictionary3["Harvest adjacent ready crops"] = "收获相邻成熟作物";
			dictionary3["Fish HUD"] = "鱼类信息";
			dictionary3["Free build"] = "自由建造";
			dictionary3["No placement restrictions"] = "无放置限制";
			dictionary3["FISHING"] = "钓鱼";
			dictionary3["Fish pond amount multiplier"] = "鱼塘鱼量倍率";
			dictionary3["Instant bite"] = "立即咬钩";
			dictionary3["Auto reel"] = "自动收线";
			dictionary3["Allowed range: 1x to 100x. Loaded ponds refill each fish species to the new capacity."] = "允许范围：1 倍至 100 倍。已加载鱼塘中的每种鱼会补充到新的容量。";
			dictionary3["Instant bite removes the waiting time after casting. Auto reel hooks the bite and completes the reeling minigame."] = "立即咬钩会移除抛竿后的等待时间。自动收线会自动提竿并完成收线小游戏。";
			dictionary3["Fish pond amount multiplier applied and loaded ponds refilled."] = "鱼塘鱼量倍率已应用，已加载的鱼塘已补满。";
			dictionary3["SERMON SPEED MULTIPLIER"] = "布道速度倍率";
			dictionary3["Apply"] = "应用";
			dictionary3["Allowed range: 1x to 20x. Only the running sermon sequence is accelerated."] = "允许范围：1～20 倍。仅加速正在进行的布道流程。";
			dictionary3["Shared storage for crafting"] = "制作共享仓储";
			dictionary3["Crafting stations can use materials from eligible storage chests in every area, not only the current zone."] = "制作设施可以使用所有区域中符合条件的箱子材料，而不只限于当前区域。";
			dictionary3["LADDERS"] = "梯子";
			dictionary3["Auto use ladders"] = "自动使用梯子";
			dictionary3["Ladder climb speed"] = "攀爬梯子速度";
			dictionary3["Allowed range: 0.1x to 20x. The multiplier is applied when a ladder climb starts."] = "允许范围：0.1～20 倍。开始攀爬梯子时应用该倍率。";
			dictionary3["Ladder climb speed multiplier applied and saved."] = "梯子攀爬速度倍率已应用并保存。";
			dictionary3["Free build removes grid snapping while keeping collision, build-area, and resource checks."] = "自由建造会取消网格吸附，但保留碰撞、建造区域和资源检查。";
			dictionary3["FINISH GROWING IN RANGE"] = "完成范围内生长";
			dictionary3["Range"] = "范围";
			dictionary3["Finish growing now"] = "立即完成生长";
			dictionary3["Finish nearby machines"] = "立即完成附近机器";
			dictionary3["Regrow nearby forage"] = "重生附近采集物";
			dictionary3["CRAFTING"] = "制作";
			dictionary3["Craft items for free"] = "免费制作物品";
			dictionary3["Build stuff for free"] = "免费建造";
			dictionary3["MACHINE SPEED MULTIPLIER"] = "机器速度倍率";
			dictionary3["CRAFTED ITEM MULTIPLIER"] = "制作产量倍率";
			dictionary3["Allowed range: 0.1x to 100x. Machine speed affects automatic stations; output changes completed item stack sizes."] = "允许范围：0.1～100 倍。机器速度影响自动设施；产量倍率改变成品数量。";
			dictionary3["Craft recipes without consuming their material ingredients. Required tools, recipe unlocks and other restrictions remain active."] = "制作时不消耗材料。所需工具、配方解锁和其他限制仍然有效。";
			dictionary3["Build without consuming materials. Building limits and the placement options in Misc remain independent."] = "建造时不消耗材料。建筑数量限制和杂项中的放置选项独立生效。";
			dictionary3["These settings only remove resource costs; they do not unlock recipes or bypass building limits."] = "这些设置只移除资源消耗，不会解锁配方或绕过建筑限制。";
			dictionary3["ALCHEMY"] = "炼金";
			dictionary3["Show folio beside alchemy table"] = "在炼金台旁显示配方书";
			dictionary3["Folio ingredient selection"] = "从配方书选择材料";
			dictionary3["Keep selected ingredients"] = "保留已选材料";
			dictionary3["Free research"] = "免费研究";
			dictionary3["Click a usable ingredient in the companion folio to place it in the first free table slot. The item must exist in an accessible inventory."] = "点击配方书中的可用材料，将其放入炼金台第一个空位。该物品必须位于可访问的库存中。";
			dictionary3["Selected ingredient types return to their slots when the table is reopened, until you replace them or no matching material remains."] = "重新打开炼金台时会恢复已选材料，直到更换材料或没有对应材料为止。";
			dictionary3["Research at the study table without consuming the selected item, science, faith, or other requirements."] = "在研究台研究时不消耗所选物品、科学点、信仰或其他需求。";
			dictionary3["When an alchemy table opens, the laboratory window moves left and an interactive folio opens beside it as a cheat sheet."] = "打开炼金台时，实验室窗口移到左侧，并在旁边打开可交互的配方书。";
			dictionary3["Turning this option off keeps the original laboratory-window position and opening behavior."] = "关闭此选项会保留实验室窗口的默认位置和打开方式。";
			dictionary3["The folio uses the formulas and runes already known by the current save."] = "配方书显示当前存档已知的配方与符文。";
			dictionary3["WORLD"] = "世界";
			dictionary3["Sermon every day"] = "每天均可布道";
			dictionary3["Repeat sermons"] = "重复布道";
			dictionary3["Make zombies every day"] = "每天均可制造僵尸";
			dictionary3["Repeat zombie making"] = "重复制造僵尸";
			dictionary3["Use map teleporters from everywhere"] = "可从任意地点使用地图传送点";
			dictionary3["DAY-SPECIFIC INTERACTIONS"] = "限定日期互动";
			dictionary3["Pride-day interactions every day"] = "每天启用傲慢日互动";
			dictionary3["Lust-day interactions every day"] = "每天启用色欲日互动";
			dictionary3["Gluttony-day interactions every day"] = "每天启用暴食日互动";
			dictionary3["Envy-day interactions every day"] = "每天启用嫉妒日互动";
			dictionary3["Wrath-day interactions every day"] = "每天启用愤怒日互动";
			dictionary3["Sloth-day interactions every day"] = "每天启用懒惰日互动";
			dictionary3["Placeholder"] = "预留功能";
			dictionary3["Repeatable"] = "可重复";
			dictionary3["The day-specific switches are visible placeholders for future game interactions and currently have no gameplay effect."] = "日期限定开关目前只是未来功能的占位选项，暂时不会影响游戏。";
			dictionary3["ZOMBIES"] = "僵尸";
			dictionary3["MOVEMENT SPEED MULTIPLIER"] = "移动速度倍率";
			dictionary3["WORK SPEED MULTIPLIER"] = "工作速度倍率";
			dictionary3["EXPERIENCE GAIN MULTIPLIERS"] = "经验获取倍率";
			dictionary3["Apply multipliers"] = "应用倍率";
			dictionary3["Reset to 1x"] = "重置为 1 倍";
			dictionary3["Collect finished products"] = "收取完成品";
			dictionary3["Crafter zombies keep finished products in their work inventory and immediately continue the next cycle. A full inventory leaves the normal pickup order active."] = "制作僵尸会把完成品存入工作库存并立即开始下一轮。库存已满时保留正常取货指令。";
			dictionary3["MANAGE WORK"] = "管理工作";
			dictionary3["Select zombie"] = "选择僵尸";
			dictionary3["Open workstation"] = "打开工作站";
			dictionary3["Jobless"] = "无工作";
			dictionary3["No zombies available"] = "没有可用的僵尸";
			dictionary3["Unnamed zombie"] = "未命名僵尸";
			dictionary3["Choose an owned zombie, then open the crafting window of its assigned workstation."] = "选择一个拥有的僵尸，然后打开其分配工作站的制作窗口。";
			dictionary3["This zombie is jobless."] = "这个僵尸没有工作。";
			dictionary3["That workstation is not loaded. Travel to its area and try again."] = "该工作站尚未加载。请前往其所在区域后重试。";
			dictionary3["That workstation cannot be opened in its current state."] = "该工作站当前无法打开。";
			dictionary3["FIX"] = "修复";
			dictionary3["RESET COMPOSTER"] = "重置堆肥箱";
			dictionary3["Reset nearest composter"] = "重置最近的堆肥箱";
			dictionary3["RESET GARDEN BED"] = "重置菜圃";
			dictionary3["Reset nearest garden bed"] = "重置最近的菜圃";
			dictionary3["REPAIR INVENTORY"] = "修复背包";
			dictionary3["Repair broken inventory item"] = "修复损坏的背包物品";
			dictionary3["Clear entire inventory"] = "清空整个背包";
			dictionary3["REPAIR ZOMBIE JOBS"] = "修复僵尸工作";
			dictionary3["Repair loaded zombie jobs"] = "修复已加载的僵尸工作";
			dictionary3["Zombie job fix (not tested)"] = "僵尸工作修复（未测试）";
			dictionary3["Repairs loaded zombies that still have an assigned station but lost their live worker link or stopped activity. Existing assignments and craft queues are preserved."] = "修复仍有工作站分配但失去工作连接或停止活动的已加载僵尸。保留现有分配和制作队列。";
			dictionary3["Choose a cheat. Changes apply to the current save."] = "请选择功能。更改应用于当前存档。";
			dictionary3["Load or start a save first."] = "请先加载或开始一个存档。";
			dictionary3["Language"] = "语言";
			dictionary3["Hotkey"] = "快捷键";
			dictionary3["Press a key..."] = "请按一个键...";
			dictionary3["Unassigned"] = "未设置";
			dictionary3["Save-safe tools only. Quest and story-state editing remains disabled in this build."] = "仅提供对存档安全的功能。此版本仍禁用任务和剧情状态编辑。";
			dictionary3["The preset name is saved with its scene and exact XYZ coordinates."] = "预设名称会与场景及精确 XYZ 坐标一起保存。";
			dictionary3["After you harvest one crop, every touching crop that is ready and has the same type is harvested too."] = "收获一个作物后，所有相邻且成熟的同类作物也会一起收获。";
			dictionary3["The hotkey matures growing crops around the player without harvesting them. Range: 1-30."] = "快捷键会使玩家周围的作物立即成熟，但不会收获。范围：1～30。";
			dictionary3["Machines: compost, ovens, forges, etc. Forage: harvested berries, mushrooms, and wild honey. All three actions use the range above."] = "机器：堆肥箱、烤炉、锻炉等。采集物：已采摘的浆果、蘑菇和野蜂蜜。三种操作均使用上方范围。";
			dictionary3["Multipliers affect every owned zombie and are saved between game sessions."] = "倍率影响所有拥有的僵尸，并会跨游戏会话保存。";
			dictionary3["Allowed range: 0.1x to 100x. New experience rewards use the red, green, and blue values separately."] = "允许范围：0.1～100 倍。新的红、绿、蓝经验奖励分别使用对应倍率。";
			dictionary3["Stand next to the broken composter. This repairs its interaction state and clears a stuck active craft. Any committed craft inputs are returned as drops beside the composter."] = "站在损坏的堆肥箱旁。此功能会修复互动状态并清除卡住的制作，已投入材料会掉落在堆肥箱旁。";
			dictionary3["Stand next to the broken garden bed. This clears its stuck crop or craft state, restores interaction, and rebuilds its approach path."] = "站在损坏的菜圃旁。此功能会清除卡住的作物或制作状态，恢复互动并重建接近路径。";
			dictionary3["Repair removes only null, empty, or unknown item entries that can crash the inventory screen. Clear inventory is a two-click emergency fallback and removes everything."] = "修复只会删除导致背包界面崩溃的无效、空白或未知物品条目。清空背包是需要点击两次确认的紧急选项，会删除所有物品。";
			dictionary3["Use fix tools only when the matching object is stuck. Save and reload first when possible."] = "仅在对应对象卡住时使用修复工具。条件允许时请先保存并重新加载。";
			dictionary3["QUEST ASSIST"] = "任务辅助";
			dictionary3["Refresh"] = "刷新";
			dictionary3["Supply missing items"] = "提供缺少物品";
			dictionary3["Protected"] = "受保护";
			dictionary3["Missing"] = "缺少";
			dictionary3["Only missing item-delivery requirements can be supplied. Friendship, reputation, story, day, order, and unknown conditions remain protected."] = "仅可提供物品交付任务中缺少的物品。友谊、声望、剧情、日期、顺序及未知条件仍受保护。";
			dictionary3["No active quests are available in the current save."] = "当前存档中没有可用的进行中任务。";
			dictionary3["Protected: this quest has no standard item-delivery finish check."] = "受保护：此任务没有标准物品交付条件。";
			dictionary3["Protected: a friendship, story, day, order, or world condition is not ready."] = "受保护：友谊、剧情、日期、顺序或世界条件尚未满足。";
			dictionary3["Protected: an unknown scripted condition could not be verified safely."] = "受保护：无法安全验证未知脚本条件。";
			dictionary3["Protected: this quest includes a non-item or non-consumable requirement."] = "受保护：此任务包含非物品或非消耗型条件。";
			dictionary3["Protected: no supported item-delivery requirement was found."] = "受保护：未找到支持的物品交付条件。";
			dictionary3["All required items are already available. Finish the quest normally."] = "所需物品均已拥有，请正常完成任务。";
			dictionary3["Quest item assistance is unavailable because the quest is protected or already ready."] = "任务受保护或已可正常完成，无法使用物品辅助。";
			dictionary3["Missing quest items supplied. Complete the quest through its normal NPC dialogue."] = "已提供缺少的任务物品。请通过正常 NPC 对话完成任务。";
						dictionary3["DISPLAY"] = "显示";
			dictionary3["Auto"] = "自动";
			dictionary3["Added {0} x {1}."] = "已添加 {0} 个 {1}。";
			KeeperCheatMenuPlugin.ChineseText = dictionary3;
			Dictionary<string, string> dictionary4 = new Dictionary<string, string>(StringComparer.Ordinal);
			dictionary4["Keeper Cheat Menu"] = "Menu de Trapaças do Keeper";
			dictionary4["MENU"] = "MENU";
			dictionary4["Player"] = "Jogador";
			dictionary4["Items"] = "Itens";
			dictionary4["Teleport"] = "Teleporte";
			dictionary4["Misc"] = "Diversos";
			dictionary4["Crafting"] = "Fabricação";
			dictionary4["Alchemy"] = "Alquimia";
			dictionary4["Quests"] = "Missões";
			dictionary4["World"] = "Mundo";
			dictionary4["Zombies"] = "Zumbis";
			dictionary4["Fix"] = "Correções";
			dictionary4["PLAYER"] = "JOGADOR";
			dictionary4["CURRENCY"] = "MOEDA";
			dictionary4["TECHNOLOGY POINTS"] = "PONTOS DE TECNOLOGIA";
			dictionary4["Gold"] = "Ouro";
			dictionary4["Silver (0-99)"] = "Prata (0–99)";
			dictionary4["Copper (0-99)"] = "Cobre (0–99)";
			dictionary4["Restore health"] = "Restaurar vida";
			dictionary4["Set insanity to 0"] = "Definir insanidade como 0";
			dictionary4["Add money"] = "Adicionar dinheiro";
			dictionary4["Red"] = "Vermelho";
			dictionary4["Green"] = "Verde";
			dictionary4["Blue"] = "Azul";
			dictionary4["Infinite energy"] = "Energia infinita";
			dictionary4["Infinite stamina"] = "Vigor infinito";
			dictionary4["Invulnerable"] = "Invulnerável";
			dictionary4["Instant actions"] = "Ações instantâneas";
			dictionary4["Sleep until wake key"] = "Dormir até a tecla de acordar";
			dictionary4["Sleep without saving"] = "Dormir sem salvar";
			dictionary4["TOWN GRATITUDE"] = "GRATIDÃO DA CIDADE";
			dictionary4["Add gratitude"] = "Adicionar gratidão";
			dictionary4["Always current maximum"] = "Sempre no máximo atual";
			dictionary4["PLAYER MULTIPLIERS"] = "MULTIPLICADORES DO JOGADOR";
			dictionary4["Harvest yield"] = "Rendimento da coleta";
			dictionary4["Gratitude gain"] = "Ganho de gratidão";
			dictionary4["Apply"] = "Aplicar";
			dictionary4["Reset to 1x"] = "Redefinir para 1×";
			dictionary4["ON"] = "LIGADO";
			dictionary4["OFF"] = "DESLIGADO";
			dictionary4["Search items..."] = "Pesquisar itens...";
			dictionary4["Location name..."] = "Nome do local...";
			dictionary4["SAVE CURRENT LOCATION"] = "SALVAR LOCAL ATUAL";
			dictionary4["Save current location"] = "Salvar local atual";
			dictionary4["SAVED LOCATIONS"] = "LOCAIS SALVOS";
			dictionary4["Delete selected"] = "Excluir selecionado";
			dictionary4["TELEPORT HOTBAR"] = "ATALHOS DE TELEPORTE";
			dictionary4["Go"] = "Ir";
			dictionary4["Select a saved location"] = "Selecionar um local salvo";
			dictionary4["MISC"] = "DIVERSOS";
			dictionary4["Harvest adjacent ready crops"] = "Colher plantações maduras adjacentes";
			dictionary4["Fish HUD"] = "Painel de pesca";
			dictionary4["Free build"] = "Construção livre";
			dictionary4["FISHING"] = "PESCA";
			dictionary4["Fish pond amount multiplier"] = "Multiplicador de peixes no lago";
			dictionary4["Instant bite"] = "Mordida instantânea";
			dictionary4["Auto reel"] = "Recolher automaticamente";
			dictionary4["Allowed range: 1x to 100x. Loaded ponds refill each fish species to the new capacity."] = "Faixa permitida: 1x a 100x. Os lagos carregados repõem cada espécie até a nova capacidade.";
			dictionary4["Instant bite removes the waiting time after casting. Auto reel hooks the bite and completes the reeling minigame."] = "Mordida instantânea remove a espera após lançar a linha. Recolher automaticamente fisga o peixe e conclui o minijogo.";
			dictionary4["Fish pond amount multiplier applied and loaded ponds refilled."] = "Multiplicador de peixes aplicado e lagos carregados reabastecidos.";
			dictionary4["No placement restrictions"] = "Sem restrições de posicionamento";
			dictionary4["SERMON SPEED MULTIPLIER"] = "MULTIPLICADOR DE VELOCIDADE DO SERMÃO";
			dictionary4["Shared storage for crafting"] = "Armazenamento compartilhado para fabricação";
			dictionary4["FINISH GROWING IN RANGE"] = "CONCLUIR CRESCIMENTO NA ÁREA";
			dictionary4["Range"] = "Alcance";
			dictionary4["Finish growing now"] = "Concluir crescimento agora";
			dictionary4["Finish nearby machines"] = "Concluir máquinas próximas";
			dictionary4["Regrow nearby forage"] = "Renovar recursos próximos";
			dictionary4["LADDERS"] = "ESCADAS";
			dictionary4["Auto use ladders"] = "Usar escadas automaticamente";
			dictionary4["Ladder climb speed"] = "Velocidade de subida";
			dictionary4["CRAFTING"] = "FABRICAÇÃO";
			dictionary4["Craft items for free"] = "Fabricar itens de graça";
			dictionary4["Build stuff for free"] = "Construir de graça";
			dictionary4["MACHINE SPEED MULTIPLIER"] = "MULTIPLICADOR DE VELOCIDADE DAS MÁQUINAS";
			dictionary4["CRAFTED ITEM MULTIPLIER"] = "MULTIPLICADOR DE ITENS FABRICADOS";
			dictionary4["ALCHEMY"] = "ALQUIMIA";
			dictionary4["Show folio beside alchemy table"] = "Mostrar fólio ao lado da mesa de alquimia";
			dictionary4["Folio ingredient selection"] = "Selecionar ingredientes pelo fólio";
			dictionary4["Keep selected ingredients"] = "Manter ingredientes selecionados";
			dictionary4["Free research"] = "Pesquisa gratuita";
			dictionary4["QUEST ASSIST"] = "ASSISTÊNCIA DE MISSÕES";
			dictionary4["Refresh"] = "Atualizar";
			dictionary4["Supply missing items"] = "Fornecer itens ausentes";
			dictionary4["Protected"] = "Protegida";
			dictionary4["Missing"] = "Faltando";
			dictionary4["Only missing item-delivery requirements can be supplied. Friendship, reputation, story, day, order, and unknown conditions remain protected."] = "Somente itens ausentes de entregas podem ser fornecidos. Amizade, reputação, história, dia, ordem e condições desconhecidas permanecem protegidas.";
			dictionary4["No active quests are available in the current save."] = "Não há missões ativas disponíveis no jogo atual.";
			dictionary4["Protected: this quest has no standard item-delivery finish check."] = "Protegida: esta missão não possui uma entrega de itens padrão.";
			dictionary4["Protected: a friendship, story, day, order, or world condition is not ready."] = "Protegida: uma condição de amizade, história, dia, ordem ou mundo ainda não foi cumprida.";
			dictionary4["Protected: an unknown scripted condition could not be verified safely."] = "Protegida: uma condição de script desconhecida não pôde ser verificada com segurança.";
			dictionary4["Protected: this quest includes a non-item or non-consumable requirement."] = "Protegida: esta missão inclui um requisito que não é um item consumível.";
			dictionary4["Protected: no supported item-delivery requirement was found."] = "Protegida: nenhum requisito compatível de entrega de itens foi encontrado.";
			dictionary4["All required items are already available. Finish the quest normally."] = "Todos os itens necessários já estão disponíveis. Conclua a missão normalmente.";
			dictionary4["Quest item assistance is unavailable because the quest is protected or already ready."] = "A assistência de itens não está disponível porque a missão está protegida ou já está pronta.";
			dictionary4["Missing quest items supplied. Complete the quest through its normal NPC dialogue."] = "Os itens ausentes foram fornecidos. Conclua a missão pelo diálogo normal do NPC.";
			dictionary4["WORLD"] = "MUNDO";
			dictionary4["Sermon every day"] = "Sermão todos os dias";
			dictionary4["Repeat sermons"] = "Repetir sermões";
			dictionary4["Make zombies every day"] = "Criar zumbis todos os dias";
			dictionary4["Repeat zombie making"] = "Repetir criação de zumbis";
			dictionary4["Use map teleporters from everywhere"] = "Usar teleportadores do mapa de qualquer lugar";
			dictionary4["DAY-SPECIFIC INTERACTIONS"] = "INTERAÇÕES DE DIAS ESPECÍFICOS";
			dictionary4["Placeholder"] = "Reservado";
			dictionary4["Repeatable"] = "Repetível";
			dictionary4["Pride-day interactions every day"] = "Interações do dia do Orgulho todos os dias";
			dictionary4["Lust-day interactions every day"] = "Interações do dia da Luxúria todos os dias";
			dictionary4["Gluttony-day interactions every day"] = "Interações do dia da Gula todos os dias";
			dictionary4["Envy-day interactions every day"] = "Interações do dia da Inveja todos os dias";
			dictionary4["Wrath-day interactions every day"] = "Interações do dia da Ira todos os dias";
			dictionary4["Sloth-day interactions every day"] = "Interações do dia da Preguiça todos os dias";
			dictionary4["ZOMBIES"] = "ZUMBIS";
			dictionary4["MOVEMENT SPEED MULTIPLIER"] = "MULTIPLICADOR DE VELOCIDADE DE MOVIMENTO";
			dictionary4["WORK SPEED MULTIPLIER"] = "MULTIPLICADOR DE VELOCIDADE DE TRABALHO";
			dictionary4["EXPERIENCE GAIN MULTIPLIERS"] = "MULTIPLICADORES DE EXPERIÊNCIA";
			dictionary4["Apply multipliers"] = "Aplicar multiplicadores";
			dictionary4["Collect finished products"] = "Coletar produtos concluídos";
			dictionary4["MANAGE WORK"] = "GERENCIAR TRABALHO";
			dictionary4["Select zombie"] = "Selecionar zumbi";
			dictionary4["Open workstation"] = "Abrir estação";
			dictionary4["Jobless"] = "Sem trabalho";
			dictionary4["No zombies available"] = "Nenhum zumbi disponível";
			dictionary4["Unnamed zombie"] = "Zumbi sem nome";
			dictionary4["Choose an owned zombie, then open the crafting window of its assigned workstation."] = "Escolha um zumbi seu e abra a janela de criação da estação atribuída a ele.";
			dictionary4["This zombie is jobless."] = "Este zumbi está sem trabalho.";
			dictionary4["That workstation is not loaded. Travel to its area and try again."] = "Essa estação não está carregada. Vá até a área dela e tente novamente.";
			dictionary4["That workstation cannot be opened in its current state."] = "Essa estação não pode ser aberta no estado atual.";
			dictionary4["FIX"] = "CORREÇÕES";
			dictionary4["RESET COMPOSTER"] = "REDEFINIR COMPOSTEIRA";
			dictionary4["Reset nearest composter"] = "Redefinir composteira mais próxima";
			dictionary4["RESET GARDEN BED"] = "REDEFINIR CANTEIRO";
			dictionary4["Reset nearest garden bed"] = "Redefinir canteiro mais próximo";
			dictionary4["REPAIR INVENTORY"] = "REPARAR INVENTÁRIO";
			dictionary4["Repair broken inventory item"] = "Reparar item quebrado do inventário";
			dictionary4["Clear entire inventory"] = "Limpar inventário inteiro";
			dictionary4["REPAIR ZOMBIE JOBS"] = "REPARAR TRABALHOS DOS ZUMBIS";
			dictionary4["Repair loaded zombie jobs"] = "Reparar trabalhos dos zumbis carregados";
			dictionary4["Zombie job fix (not tested)"] = "Correção de trabalho dos zumbis (não testada)";
			dictionary4["Choose a cheat. Changes apply to the current save."] = "Escolha uma trapaça. As alterações se aplicam ao jogo atual.";
			dictionary4["Load or start a save first."] = "Carregue ou inicie um jogo primeiro.";
			dictionary4["Language"] = "Idioma";
			dictionary4["Hotkey"] = "Tecla de atalho";
			dictionary4["Press a key..."] = "Pressione uma tecla...";
			dictionary4["Unassigned"] = "Não atribuída";
			dictionary4["Save-safe tools only. Quest and story-state editing remains disabled in this build."] = "Somente ferramentas seguras para o jogo salvo. A edição direta de missões e história permanece desativada.";
			dictionary4["The preset name is saved with its scene and exact XYZ coordinates."] = "O nome é salvo com a cena e as coordenadas XYZ exatas.";
			dictionary4["After you harvest one crop, every touching crop that is ready and has the same type is harvested too."] = "Ao colher uma plantação, todas as plantações maduras adjacentes do mesmo tipo também são colhidas.";
			dictionary4["The hotkey matures growing crops around the player without harvesting them. Range: 1-30."] = "A tecla amadurece as plantações ao redor do jogador sem colhê-las. Alcance: 1–30.";
			dictionary4["Multipliers affect every owned zombie and are saved between game sessions."] = "Os multiplicadores afetam todos os zumbis e permanecem salvos entre sessões.";
			dictionary4["Allowed range: 0.1x to 20x. The multiplier is applied when a ladder climb starts."] = "Faixa permitida: 0,1× a 20×. O multiplicador é aplicado ao começar a subir uma escada.";
			dictionary4["Ladder climb speed multiplier applied and saved."] = "Multiplicador de subida aplicado e salvo.";
			KeeperCheatMenuPlugin.BrazilianPortugueseText = dictionary4;
			KeeperCheatMenuPlugin.PortugueseText = KeeperCheatMenuPlugin.CreateEuropeanPortugueseText();
			Dictionary<string, string> dictionary5 = new Dictionary<string, string>(StringComparer.Ordinal);
			dictionary5["Keeper Cheat Menu"] = "キーパー チートメニュー";
			dictionary5["MENU"] = "メニュー";
			dictionary5["Player"] = "プレイヤー";
			dictionary5["Items"] = "アイテム";
			dictionary5["Teleport"] = "テレポート";
			dictionary5["Misc"] = "その他";
			dictionary5["Crafting"] = "クラフト";
			dictionary5["Alchemy"] = "錬金術";
			dictionary5["Quests"] = "クエスト";
			dictionary5["World"] = "ワールド";
			dictionary5["Zombies"] = "ゾンビ";
			dictionary5["Fix"] = "修復";
			dictionary5["PLAYER"] = "プレイヤー";
			dictionary5["CURRENCY"] = "通貨";
			dictionary5["TECHNOLOGY POINTS"] = "テクノロジーポイント";
			dictionary5["Gold"] = "金";
			dictionary5["Silver (0-99)"] = "銀 (0～99)";
			dictionary5["Copper (0-99)"] = "銅 (0～99)";
			dictionary5["Restore health"] = "体力を回復";
			dictionary5["Set insanity to 0"] = "狂気度を0に設定";
			dictionary5["Add money"] = "お金を追加";
			dictionary5["Red"] = "赤";
			dictionary5["Green"] = "緑";
			dictionary5["Blue"] = "青";
			dictionary5["Infinite energy"] = "エネルギー無限";
			dictionary5["Infinite stamina"] = "スタミナ無限";
			dictionary5["Invulnerable"] = "無敵";
			dictionary5["Instant actions"] = "即時アクション";
			dictionary5["Sleep until wake key"] = "起床キーまで睡眠";
			dictionary5["Sleep without saving"] = "保存せずに睡眠";
			dictionary5["TOWN GRATITUDE"] = "町の感謝度";
			dictionary5["Add gratitude"] = "感謝度を追加";
			dictionary5["Always current maximum"] = "常に現在の最大値";
			dictionary5["PLAYER MULTIPLIERS"] = "プレイヤー倍率";
			dictionary5["Harvest yield"] = "収穫量";
			dictionary5["Gratitude gain"] = "感謝度獲得量";
			dictionary5["Crop harvests, manually gathered resources, and positive town-gratitude rewards use these saved multipliers. Costs are not changed."] = "作物の収穫物、手作業で採取した資源、正の町の感謝度報酬に保存した倍率を適用します。コストは変更されません。";
			dictionary5["Player multipliers applied and saved."] = "プレイヤー倍率を適用して保存しました。";
			dictionary5["Player multipliers reset to 1x."] = "プレイヤー倍率を1倍に戻しました。";
			dictionary5["ON"] = "オン";
			dictionary5["OFF"] = "オフ";
			dictionary5["Search items..."] = "アイテムを検索...";
			dictionary5["Location name..."] = "場所の名前...";
			dictionary5["SAVE CURRENT LOCATION"] = "現在地を保存";
			dictionary5["Save current location"] = "現在地を保存";
			dictionary5["SAVED LOCATIONS"] = "保存した場所";
			dictionary5["Delete selected"] = "選択を削除";
			dictionary5["TELEPORT HOTBAR"] = "テレポートショートカット";
			dictionary5["Go"] = "移動";
			dictionary5["Select a saved location"] = "保存した場所を選択";
			dictionary5["MISC"] = "その他";
			dictionary5["Harvest adjacent ready crops"] = "隣接する収穫可能な作物を収穫";
			dictionary5["Fish HUD"] = "釣り情報";
			dictionary5["Free build"] = "自由配置";
			dictionary5["FISHING"] = "釣り";
			dictionary5["Fish pond amount multiplier"] = "釣り場の魚数倍率";
			dictionary5["Instant bite"] = "即ヒット";
			dictionary5["Auto reel"] = "自動巻き上げ";
			dictionary5["Allowed range: 1x to 100x. Loaded ponds refill each fish species to the new capacity."] = "設定範囲：1倍～100倍。読み込み済みの釣り場は各魚種を新しい上限まで補充します。";
			dictionary5["Instant bite removes the waiting time after casting. Auto reel hooks the bite and completes the reeling minigame."] = "即ヒットは投げた後の待ち時間をなくします。自動巻き上げは魚を掛け、巻き上げミニゲームを完了します。";
			dictionary5["Fish pond amount multiplier applied and loaded ponds refilled."] = "魚数倍率を適用し、読み込み済みの釣り場を補充しました。";
			dictionary5["No placement restrictions"] = "配置制限なし";
			dictionary5["SERMON SPEED MULTIPLIER"] = "説教速度倍率";
			dictionary5["Apply"] = "適用";
			dictionary5["Allowed range: 1x to 20x. Only the running sermon sequence is accelerated."] = "設定範囲：1倍～20倍。進行中の説教シーンだけが加速されます。";
			dictionary5["Shared storage for crafting"] = "クラフト用共有ストレージ";
			dictionary5["Crafting stations can use materials from eligible storage chests in every area, not only the current zone."] = "クラフト設備は現在の区域だけでなく、全区域の利用可能な保管箱にある素材を使用できます。";
			dictionary5["LADDERS"] = "はしご";
			dictionary5["Auto use ladders"] = "はしごを自動使用";
			dictionary5["Ladder climb speed"] = "はしごの移動速度";
			dictionary5["Allowed range: 0.1x to 20x. The multiplier is applied when a ladder climb starts."] = "設定範囲：0.1倍～20倍。はしごを登り始めるときに倍率が適用されます。";
			dictionary5["Ladder climb speed multiplier applied and saved."] = "はしごの移動速度倍率を適用して保存しました。";
			dictionary5["Free build removes grid snapping while keeping collision, build-area, and resource checks."] = "自由配置はグリッドへの吸着を解除します。衝突、建築区域、資材の判定は維持されます。";
			dictionary5["FINISH GROWING IN RANGE"] = "範囲内の成長を完了";
			dictionary5["Range"] = "範囲";
			dictionary5["Finish growing now"] = "今すぐ成長を完了";
			dictionary5["Finish nearby machines"] = "周辺の機械を即時完了";
			dictionary5["Regrow nearby forage"] = "周辺の採集物を再生";
			dictionary5["CRAFTING"] = "クラフト";
			dictionary5["Craft items for free"] = "素材なしでクラフト";
			dictionary5["Build stuff for free"] = "素材なしで建築";
			dictionary5["MACHINE SPEED MULTIPLIER"] = "機械速度倍率";
			dictionary5["CRAFTED ITEM MULTIPLIER"] = "作成アイテム倍率";
			dictionary5["Allowed range: 0.1x to 100x. Machine speed affects automatic stations; output changes completed item stack sizes."] = "設定範囲：0.1倍～100倍。機械速度は自動設備に適用され、出力倍率は完成アイテムの数量を変更します。";
			dictionary5["Craft recipes without consuming their material ingredients. Required tools, recipe unlocks and other restrictions remain active."] = "素材を消費せずにクラフトします。必要な道具、レシピの解放、その他の制限は維持されます。";
			dictionary5["Build without consuming materials. Building limits and the placement options in Misc remain independent."] = "素材を消費せずに建築します。建築数の制限と「その他」の配置オプションは個別に適用されます。";
			dictionary5["These settings only remove resource costs; they do not unlock recipes or bypass building limits."] = "これらの設定は素材コストのみを除外し、レシピの解放や建築数制限は変更しません。";
			dictionary5["ALCHEMY"] = "錬金術";
			dictionary5["Show folio beside alchemy table"] = "錬金台の横に書物を表示";
			dictionary5["Folio ingredient selection"] = "書物から材料を選択";
			dictionary5["Keep selected ingredients"] = "選択した材料を保持";
			dictionary5["Free research"] = "研究無料";
			dictionary5["Click a usable ingredient in the companion folio to place it in the first free table slot. The item must exist in an accessible inventory."] = "併設された書物で使用可能な材料をクリックすると、錬金台の最初の空きスロットに入ります。アクセス可能なインベントリにそのアイテムが必要です。";
			dictionary5["Selected ingredient types return to their slots when the table is reopened, until you replace them or no matching material remains."] = "選択した材料の種類は、交換するか同じ材料がなくなるまで、錬金台を開き直したときにスロットへ復元されます。";
			dictionary5["Research at the study table without consuming the selected item, science, faith, or other requirements."] = "研究台で選択したアイテム、科学、信仰、その他の必要物を消費せずに研究します。";
			dictionary5["When an alchemy table opens, the laboratory window moves left and an interactive folio opens beside it as a cheat sheet."] = "錬金台を開くと研究室ウィンドウが左へ移動し、操作可能な参考用の書物が隣に開きます。";
			dictionary5["Turning this option off keeps the original laboratory-window position and opening behavior."] = "このオプションをオフにすると、研究室ウィンドウは標準の位置と動作のままになります。";
			dictionary5["The folio uses the formulas and runes already known by the current save."] = "書物には現在のセーブで既知の調合法とルーンが表示されます。";
			dictionary5["WORLD"] = "ワールド";
			dictionary5["Sermon every day"] = "毎日説教できる";
			dictionary5["Repeat sermons"] = "説教を繰り返す";
			dictionary5["Make zombies every day"] = "毎日ゾンビを作れる";
			dictionary5["Repeat zombie making"] = "ゾンビ作成を繰り返す";
			dictionary5["Use map teleporters from everywhere"] = "どこからでもマップのテレポーターを使用";
			dictionary5["DAY-SPECIFIC INTERACTIONS"] = "曜日限定の操作";
			dictionary5["Pride-day interactions every day"] = "傲慢の日の操作を毎日可能にする";
			dictionary5["Lust-day interactions every day"] = "色欲の日の操作を毎日可能にする";
			dictionary5["Gluttony-day interactions every day"] = "暴食の日の操作を毎日可能にする";
			dictionary5["Envy-day interactions every day"] = "嫉妬の日の操作を毎日可能にする";
			dictionary5["Wrath-day interactions every day"] = "憤怒の日の操作を毎日可能にする";
			dictionary5["Sloth-day interactions every day"] = "怠惰の日の操作を毎日可能にする";
			dictionary5["Placeholder"] = "準備中";
			dictionary5["Repeatable"] = "繰り返し可能";
			dictionary5["The day-specific switches are visible placeholders for future game interactions and currently have no gameplay effect."] = "曜日別の切り替えは今後の機能用の表示項目で、現在ゲームには影響しません。";
			dictionary5["ZOMBIES"] = "ゾンビ";
			dictionary5["MOVEMENT SPEED MULTIPLIER"] = "移動速度倍率";
			dictionary5["WORK SPEED MULTIPLIER"] = "作業速度倍率";
			dictionary5["EXPERIENCE GAIN MULTIPLIERS"] = "経験値獲得倍率";
			dictionary5["Apply multipliers"] = "倍率を適用";
			dictionary5["Reset to 1x"] = "1倍にリセット";
			dictionary5["Collect finished products"] = "完成品を自動回収";
			dictionary5["Crafter zombies keep finished products in their work inventory and immediately continue the next cycle. A full inventory leaves the normal pickup order active."] = "クラフトゾンビが完成品を作業用インベントリに保管し、次の工程をすぐ開始します。満杯の場合は通常の回収指示が残ります。";
			dictionary5["MANAGE WORK"] = "作業管理";
			dictionary5["Select zombie"] = "ゾンビを選択";
			dictionary5["Open workstation"] = "作業台を開く";
			dictionary5["Jobless"] = "仕事なし";
			dictionary5["No zombies available"] = "利用できるゾンビがいません";
			dictionary5["Unnamed zombie"] = "名前のないゾンビ";
			dictionary5["Choose an owned zombie, then open the crafting window of its assigned workstation."] = "所有しているゾンビを選び、割り当てられた作業台の制作画面を開きます。";
			dictionary5["This zombie is jobless."] = "このゾンビには仕事がありません。";
			dictionary5["That workstation is not loaded. Travel to its area and try again."] = "その作業台は読み込まれていません。該当エリアへ移動して再試行してください。";
			dictionary5["That workstation cannot be opened in its current state."] = "その作業台は現在の状態では開けません。";
			dictionary5["FIX"] = "修復";
			dictionary5["RESET COMPOSTER"] = "コンポスターをリセット";
			dictionary5["Reset nearest composter"] = "最寄りのコンポスターをリセット";
			dictionary5["RESET GARDEN BED"] = "畑をリセット";
			dictionary5["Reset nearest garden bed"] = "最寄りの畑をリセット";
			dictionary5["REPAIR INVENTORY"] = "インベントリ修復";
			dictionary5["Repair broken inventory item"] = "壊れたアイテムを修復";
			dictionary5["Clear entire inventory"] = "インベントリをすべて消去";
			dictionary5["REPAIR ZOMBIE JOBS"] = "ゾンビ作業を修復";
			dictionary5["Repair loaded zombie jobs"] = "読み込み済みゾンビの作業を修復";
			dictionary5["Zombie job fix (not tested)"] = "ゾンビ作業修復（未テスト）";
			dictionary5["Repairs loaded zombies that still have an assigned station but lost their live worker link or stopped activity. Existing assignments and craft queues are preserved."] = "作業場所が割り当てられているのに接続や作業が停止したゾンビを修復します。既存の割り当てと制作キューは維持されます。";
			dictionary5["Choose a cheat. Changes apply to the current save."] = "機能を選択してください。変更は現在のセーブに適用されます。";
			dictionary5["Load or start a save first."] = "先にセーブデータを読み込むか、新しいゲームを開始してください。";
			dictionary5["Language"] = "言語";
			dictionary5["Hotkey"] = "ホットキー";
			dictionary5["Press a key..."] = "キーを押してください...";
			dictionary5["Unassigned"] = "未設定";
			dictionary5["Save-safe tools only. Quest and story-state editing remains disabled in this build."] = "セーブデータに安全な機能のみです。このビルドではクエストとストーリー状態の編集は無効です。";
			dictionary5["The preset name is saved with its scene and exact XYZ coordinates."] = "名前はシーンおよび正確なXYZ座標と一緒に保存されます。";
			dictionary5["After you harvest one crop, every touching crop that is ready and has the same type is harvested too."] = "作物を1つ収穫すると、隣接する同種の収穫可能な作物も一緒に収穫されます。";
			dictionary5["The hotkey matures growing crops around the player without harvesting them. Range: 1-30."] = "ホットキーでプレイヤー周辺の作物を収穫せずに成熟させます。範囲：1～30。";
			dictionary5["Machines: compost, ovens, forges, etc. Forage: harvested berries, mushrooms, and wild honey. All three actions use the range above."] = "機械：コンポスター、オーブン、鍛冶場など。採集物：ベリー、キノコ、野生の蜂蜜。すべて上記の範囲を使用します。";
			dictionary5["Multipliers affect every owned zombie and are saved between game sessions."] = "倍率は所有するすべてのゾンビに適用され、ゲーム終了後も保存されます。";
			dictionary5["Allowed range: 0.1x to 100x. New experience rewards use the red, green, and blue values separately."] = "設定範囲：0.1倍～100倍。新しい経験値報酬には赤、緑、青の値が個別に適用されます。";
			dictionary5["Stand next to the broken composter. This repairs its interaction state and clears a stuck active craft. Any committed craft inputs are returned as drops beside the composter."] = "壊れたコンポスターの隣に立ってください。操作状態を修復し、停止した作業を解除します。投入済みの素材はコンポスターの横に戻されます。";
			dictionary5["Stand next to the broken garden bed. This clears its stuck crop or craft state, restores interaction, and rebuilds its approach path."] = "壊れた畑の隣に立ってください。停止した作物または作業状態を消去し、操作と進入経路を復旧します。";
			dictionary5["Repair removes only null, empty, or unknown item entries that can crash the inventory screen. Clear inventory is a two-click emergency fallback and removes everything."] = "修復はインベントリ画面を停止させる無効・空・不明な項目だけを削除します。全消去は2回クリックで確認する緊急用機能で、すべて削除します。";
			dictionary5["Use fix tools only when the matching object is stuck. Save and reload first when possible."] = "対応するオブジェクトが停止した場合のみ修復機能を使用してください。可能なら先に保存して再読み込みしてください。";
			dictionary5["QUEST ASSIST"] = "クエスト支援";
			dictionary5["Refresh"] = "更新";
			dictionary5["Supply missing items"] = "不足アイテムを追加";
			dictionary5["Protected"] = "保護対象";
			dictionary5["Missing"] = "不足";
			dictionary5["Only missing item-delivery requirements can be supplied. Friendship, reputation, story, day, order, and unknown conditions remain protected."] = "アイテム納品に必要な不足分だけを追加できます。友好度、評判、ストーリー、曜日、順序、不明な条件は保護されます。";
			dictionary5["No active quests are available in the current save."] = "現在のセーブには進行中のクエストがありません。";
			dictionary5["Protected: this quest has no standard item-delivery finish check."] = "保護対象：標準のアイテム納品条件がありません。";
			dictionary5["Protected: a friendship, story, day, order, or world condition is not ready."] = "保護対象：友好度、ストーリー、曜日、順序、またはワールド条件が未達成です。";
			dictionary5["Protected: an unknown scripted condition could not be verified safely."] = "保護対象：不明なスクリプト条件を安全に確認できません。";
			dictionary5["Protected: this quest includes a non-item or non-consumable requirement."] = "保護対象：消費アイテム以外の条件が含まれています。";
			dictionary5["Protected: no supported item-delivery requirement was found."] = "保護対象：対応するアイテム納品条件が見つかりません。";
			dictionary5["All required items are already available. Finish the quest normally."] = "必要なアイテムはすでに揃っています。通常どおりクエストを完了してください。";
			dictionary5["Quest item assistance is unavailable because the quest is protected or already ready."] = "クエストが保護対象か完了可能なため、アイテム支援は利用できません。";
			dictionary5["Missing quest items supplied. Complete the quest through its normal NPC dialogue."] = "不足していたクエストアイテムを追加しました。通常のNPC会話でクエストを完了してください。";
			KeeperCheatMenuPlugin.JapaneseText = dictionary5;
			KeeperCheatMenuPlugin.PanelOuter = KeeperCheatMenuPlugin.Hex("18191f");
			KeeperCheatMenuPlugin.PanelBorder = KeeperCheatMenuPlugin.Hex("6e604a");
			KeeperCheatMenuPlugin.PanelInner = KeeperCheatMenuPlugin.Hex("292b34");
			KeeperCheatMenuPlugin.HeaderBrown = KeeperCheatMenuPlugin.Hex("71583a");
			KeeperCheatMenuPlugin.HeaderDark = KeeperCheatMenuPlugin.Hex("3d3128");
			KeeperCheatMenuPlugin.ButtonRed = KeeperCheatMenuPlugin.Hex("971d20");
			KeeperCheatMenuPlugin.ButtonRedHover = KeeperCheatMenuPlugin.Hex("b52c2c");
			KeeperCheatMenuPlugin.ButtonRedPressed = KeeperCheatMenuPlugin.Hex("681317");
			KeeperCheatMenuPlugin.ButtonGold = KeeperCheatMenuPlugin.Hex("c9864b");
			KeeperCheatMenuPlugin.TextGold = KeeperCheatMenuPlugin.Hex("f1a160");
			KeeperCheatMenuPlugin.TextPale = KeeperCheatMenuPlugin.Hex("e7d7ba");
			KeeperCheatMenuPlugin.InputDark = KeeperCheatMenuPlugin.Hex("17181e");
			KeeperCheatMenuPlugin.ToggleOn = KeeperCheatMenuPlugin.Hex("4d7d3c");
			KeeperCheatMenuPlugin.QuestSystemDataGetter = AccessTools.PropertyGetter(typeof(QuestSystem), "QuestSystemData");
		}

		// Token: 0x04000004 RID: 4
		private UIAlchemyWindow _companionAlchemyWindow;

		// Token: 0x04000005 RID: 5
		private UIAlchemyFolioWindow _companionFolioWindow;

		// Token: 0x04000006 RID: 6
		private UIAlchemyWindow _activeAlchemyWindow;

		// Token: 0x04000007 RID: 7
		private WgoData _activeAlchemyWgoData;

		// Token: 0x04000008 RID: 8
		private Vector2 _alchemyDefaultPosition;

		// Token: 0x04000009 RID: 9
		private Vector2 _folioDefaultPosition;

		// Token: 0x0400000A RID: 10
		private bool _alchemyPositionCaptured;

		// Token: 0x0400000B RID: 11
		private bool _folioPositionCaptured;

		// Token: 0x0400000C RID: 12
		private bool _companionFolioOpenedByMod;

		// Token: 0x0400000D RID: 13
		private readonly Dictionary<Graphic, bool> _companionFolioBackdropStates = new Dictionary<Graphic, bool>();

		// Token: 0x0400000E RID: 14
		private readonly Dictionary<string, string[]> _retainedAlchemyIngredients = new Dictionary<string, string[]>(StringComparer.Ordinal);

		// Token: 0x0400000F RID: 15
		private static readonly FieldInfo AlchemyIngredientsField = AccessTools.Field(typeof(UIAlchemyWindow), "ingredients");

		// Token: 0x04000010 RID: 16
		private static readonly FieldInfo AlchemyIngredientCellField = AccessTools.Field(typeof(UIAlchemyIngredient), "cell");

		// Token: 0x04000011 RID: 17
		private static readonly FieldInfo AlchemyIngredientPlusField = AccessTools.Field(typeof(UIAlchemyIngredient), "plusObj");

		// Token: 0x04000012 RID: 18
		private static readonly MethodInfo RedrawAlchemyTabLiteMethod = AccessTools.Method(typeof(UIAlchemyWindow), "RedrawAlchemyTabLite", null, null);

		// Token: 0x04000013 RID: 19
		private KeeperCheatMenuPlugin.HotkeyCaptureTarget _hotkeyCaptureTarget;

		// Token: 0x04000014 RID: 20
		private GameObject _fishingOverlayRoot;

		// Token: 0x04000015 RID: 21
		private RectTransform _fishingOverlayPanel;

		// Token: 0x04000016 RID: 22
		private TextMeshProUGUI _fishingOverlayText;

		// Token: 0x04000017 RID: 23
		private Image _fishingOverlayDragSurface;

		// Token: 0x04000018 RID: 24
		private GameObject _fishingOverlayLockBody;

		// Token: 0x04000019 RID: 25
		private GameObject _fishingOverlayClosedShackle;

		// Token: 0x0400001A RID: 26
		private GameObject _fishingOverlayOpenShackle;

		// Token: 0x0400001B RID: 27
		private WgoData _activeFishingReservoir;

		// Token: 0x0400001C RID: 28
		private bool _fishingReservoirInRange;

		// Token: 0x0400001D RID: 29
		private bool _fishingActivityActive;

		// Token: 0x0400001E RID: 30
		private float _nextFishingOverlayRefresh;

		// Token: 0x0400001F RID: 31
		private const float ComposterResetRange = 12f;

		// Token: 0x04000020 RID: 32
		private const float GardenBedResetRange = 8f;

		// Token: 0x04000021 RID: 33
		private static readonly FieldInfo WgoInteractionHandlerField = AccessTools.Field(typeof(Wgo), "interactionHandler");

		// Token: 0x04000022 RID: 34
		private static readonly FieldInfo CraftHasPreFinishUpdateField = AccessTools.Field(typeof(CraftComponent), "hasPreFinishUpdate");

		// Token: 0x04000023 RID: 35
		private static readonly FieldInfo CraftPreFinishHoldCountField = AccessTools.Field(typeof(CraftComponent), "preFinishHoldCount");

		// Token: 0x04000024 RID: 36
		private static readonly FieldInfo CraftFinishHeldTimerField = AccessTools.Field(typeof(CraftComponent), "finishHeldTimer");

		// Token: 0x04000025 RID: 37
		private static readonly FieldInfo CraftRemovingDestroyField = AccessTools.Field(typeof(CraftComponent), "isRemovingDestroyCraft");

		// Token: 0x04000026 RID: 38
		private static readonly FieldInfo CraftRestartQueueTimerField = AccessTools.Field(typeof(CraftComponent), "restartQueueTimer");

		// Token: 0x04000027 RID: 39
		private static readonly FieldInfo CraftAutoTickTimerField = AccessTools.Field(typeof(CraftComponent), "currentAutoCraftTickTime");

		// Token: 0x04000028 RID: 40
		private static readonly MethodInfo ConveyorCrafterStartMethod = AccessTools.Method(typeof(ZombieWgoData), "ConveyorCrafterStartCraftActivity", null, null);

		// Token: 0x04000029 RID: 41
		private static readonly MethodInfo ConveyorCrafterResumeMethod = AccessTools.Method(typeof(ZombieWgoData), "ConveyorCrafterTryStartCurrentCraft", null, null);

		// Token: 0x0400002A RID: 42
		private static readonly MethodInfo CaretakerResumeMethod = AccessTools.Method(typeof(ZombieWgoData), "CaretakerTryGetNewOrderOrMoveToStation", null, null);

		// Token: 0x0400002B RID: 43
		private static readonly MethodInfo GardenerResumeMethod = AccessTools.Method(typeof(ZombieWgoData), "GardenerTryGetNewOrderOrMoveToStation", null, null);

		// Token: 0x0400002C RID: 44
		private static readonly MethodInfo ConveyorTransporterResumeMethod = AccessTools.Method(typeof(ZombieWgoData), "ConveyorTransporterTryGetNewOrder", null, null);

		// Token: 0x0400002D RID: 45
		private static readonly MethodInfo CalculateInventoryFillSizeMethod = AccessTools.Method(typeof(Item), "CalculateInventoryFillSize", null, null);

		// Token: 0x0400002E RID: 46
		private bool _clearInventoryConfirmationArmed;

		// Token: 0x0400002F RID: 47
		public const string PluginId = "Narodum.gk2.keepercheatmenu";

		// Token: 0x04000030 RID: 48
		public const string PluginName = "Keeper Cheat Menu";

		// Token: 0x04000031 RID: 49
		public const string PluginVersion = "0.22.2";

		// Token: 0x04000033 RID: 51
		private static readonly FieldInfo HpComponentField = typeof(PlayerData).GetField("hpComponent", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000034 RID: 52
		private static readonly FieldInfo RemainingSleepTimeField = typeof(EnergySystem).GetField("remainingSleepTime", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000035 RID: 53
		private ConfigEntry<KeyboardShortcut> _menuKey;

		// Token: 0x04000036 RID: 54
		private ConfigEntry<string> _language;

		// Token: 0x04000037 RID: 55
		private ConfigEntry<bool> _infiniteEnergy;

		// Token: 0x04000038 RID: 56
		private ConfigEntry<bool> _infiniteStamina;

		// Token: 0x04000039 RID: 57
		private ConfigEntry<bool> _invulnerable;

		// Token: 0x0400003A RID: 58
		private ConfigEntry<bool> _instantActions;

		// Token: 0x0400003B RID: 59
		private ConfigEntry<bool> _sleepAlways;

		// Token: 0x0400003C RID: 60
		private ConfigEntry<bool> _sleepWithoutSaving;

		// Token: 0x0400003D RID: 61
		private ConfigEntry<bool> _alwaysMaxTownGratitude;

		// Token: 0x0400003E RID: 62
		private ConfigEntry<float> _harvestYieldMultiplier;

		// Token: 0x0400003F RID: 63
		private ConfigEntry<float> _gratitudeMultiplier;

		// Token: 0x04000040 RID: 64
		private ConfigEntry<KeyCode> _wakeUpHotkey;

		// Token: 0x04000041 RID: 65
		private ConfigEntry<bool> _harvestAdjacentCrops;

		// Token: 0x04000042 RID: 66
		private ConfigEntry<bool> _freeBuild;

		// Token: 0x04000043 RID: 67
		private ConfigEntry<bool> _unrestrictedBuild;

		// Token: 0x04000044 RID: 68
		private ConfigEntry<float> _sermonSpeedMultiplier;

		// Token: 0x04000045 RID: 69
		private ConfigEntry<bool> _sharedStorage;

		// Token: 0x04000046 RID: 70
		private ConfigEntry<bool> _autoUseLadders;

		// Token: 0x04000047 RID: 71
		private ConfigEntry<float> _ladderClimbSpeedMultiplier;

		// Token: 0x04000048 RID: 72
		private ConfigEntry<bool> _freeCrafting;

		// Token: 0x04000049 RID: 73
		private ConfigEntry<bool> _freeBuildingCosts;

		// Token: 0x0400004A RID: 74
		private ConfigEntry<float> _machineSpeedMultiplier;

		// Token: 0x0400004B RID: 75
		private ConfigEntry<float> _craftedOutputMultiplier;

		// Token: 0x0400004C RID: 76
		private ConfigEntry<bool> _alchemyFolioCompanion;

		// Token: 0x0400004D RID: 77
		private ConfigEntry<bool> _alchemyFolioCrafting;

		// Token: 0x0400004E RID: 78
		private ConfigEntry<bool> _retainAlchemyIngredients;

		// Token: 0x0400004F RID: 79
		private ConfigEntry<bool> _freeResearch;

		// Token: 0x04000050 RID: 80
		private ConfigEntry<bool> _sermonEveryDay;

		// Token: 0x04000051 RID: 81
		private ConfigEntry<bool> _sermonRepeatable;

		// Token: 0x04000052 RID: 82
		private ConfigEntry<bool> _zombiesEveryDay;

		// Token: 0x04000053 RID: 83
		private ConfigEntry<bool> _zombiesRepeatable;

		// Token: 0x04000054 RID: 84
		private ConfigEntry<bool> _mapTeleportEverywhere;

		// Token: 0x04000055 RID: 85
		private ConfigEntry<int> _lastEverydaySermonDay;

		// Token: 0x04000056 RID: 86
		private ConfigEntry<bool> _fishOverlayEnabled;

		// Token: 0x04000057 RID: 87
		private ConfigEntry<bool> _fishOverlayLocked;

		// Token: 0x04000058 RID: 88
		private ConfigEntry<float> _fishOverlayPositionX;

		// Token: 0x04000059 RID: 89
		private ConfigEntry<float> _fishOverlayPositionY;

		// Token: 0x0400005A RID: 90
		private ConfigEntry<float> _fishPondStockMultiplier;

		// Token: 0x0400005B RID: 91
		private ConfigEntry<bool> _instantFishingBite;

		// Token: 0x0400005C RID: 92
		private ConfigEntry<bool> _autoReelFishing;

		// Token: 0x0400005D RID: 93
		private ConfigEntry<int> _finishGrowingRange;

		// Token: 0x0400005E RID: 94
		private ConfigEntry<KeyCode> _finishGrowingHotkey;

		// Token: 0x0400005F RID: 95
		private ConfigEntry<KeyCode> _finishCraftsHotkey;

		// Token: 0x04000060 RID: 96
		private ConfigEntry<KeyCode> _regrowForageHotkey;

		// Token: 0x04000061 RID: 97
		private ConfigEntry<float> _zombieMoveSpeedMultiplier;

		// Token: 0x04000062 RID: 98
		private ConfigEntry<float> _zombieWorkSpeedMultiplier;

		// Token: 0x04000063 RID: 99
		private ConfigEntry<float> _zombieRedExperienceMultiplier;

		// Token: 0x04000064 RID: 100
		private ConfigEntry<float> _zombieGreenExperienceMultiplier;

		// Token: 0x04000065 RID: 101
		private ConfigEntry<float> _zombieBlueExperienceMultiplier;

		// Token: 0x04000066 RID: 102
		private ConfigEntry<bool> _zombieCollectFinishedProducts;

		// Token: 0x04000067 RID: 103
		private ConfigEntry<string> _savedLocationsJson;

		// Token: 0x04000068 RID: 104
		private ConfigEntry<string>[] _teleportHotbarSlots;

		// Token: 0x04000069 RID: 105
		private Harmony _harmony;

		// Token: 0x0400006A RID: 106
		private Rect _window = new Rect(80f, 70f, 620f, 690f);

		// Token: 0x0400006B RID: 107
		private Vector2 _itemScroll;

		// Token: 0x0400006C RID: 108
		private bool _show;

		// Token: 0x0400006D RID: 109
		private string _moneyAmount = "10000";

		// Token: 0x0400006E RID: 110
		private string _techAmount = "100";

		// Token: 0x0400006F RID: 111
		private string _itemSearch = string.Empty;

		// Token: 0x04000070 RID: 112
		private string _itemQuantity = "1";

		// Token: 0x04000071 RID: 113
		private string _status = "Load a save, then use the controls below.";

		// Token: 0x04000072 RID: 114
		private List<ItemDef> _filteredItems = new List<ItemDef>();

		// Token: 0x04000073 RID: 115
		private string _lastSearch;

		// Token: 0x04000074 RID: 116
		private HPComponent _protectedHp;

		// Token: 0x04000075 RID: 117
		private bool _originalImmunity;

		// Token: 0x04000076 RID: 118
		private float _nextRefill;

		// Token: 0x04000077 RID: 119
		private GUIStyle _headerStyle;

		// Token: 0x04000078 RID: 120
		private GUIStyle _noteStyle;

		// Token: 0x04000079 RID: 121
		private float _nextAutomaticLadderUse;

		// Token: 0x0400007A RID: 122
		private LadderInteractionHandler _pendingAutomaticLadder;

		// Token: 0x0400007B RID: 123
		private PlayerController _pendingAutomaticLadderPlayer;

		// Token: 0x0400007C RID: 124
		private int _pendingAutomaticLadderFrame;

		// Token: 0x0400007D RID: 125
		private static readonly Dictionary<string, string> GermanText;

		// Token: 0x0400007E RID: 126
		private static readonly Dictionary<string, string> KoreanText;

		// Token: 0x0400007F RID: 127
		private static readonly Dictionary<string, string> ChineseText;

		// Token: 0x04000080 RID: 128
		private static readonly Dictionary<string, string> BrazilianPortugueseText;

		// Token: 0x04000081 RID: 129
		private static readonly Dictionary<string, string> PortugueseText;

		// Token: 0x04000082 RID: 130
		private static readonly Dictionary<string, string> JapaneseText;

		// Token: 0x04000083 RID: 131
		private bool _sermonSpeedActive;

		// Token: 0x04000084 RID: 132
		private float _sermonPreviousTimeScale = 1f;

		// Token: 0x04000085 RID: 133
		private static readonly Color PanelOuter;

		// Token: 0x04000086 RID: 134
		private static readonly Color PanelBorder;

		// Token: 0x04000087 RID: 135
		private static readonly Color PanelInner;

		// Token: 0x04000088 RID: 136
		private static readonly Color HeaderBrown;

		// Token: 0x04000089 RID: 137
		private static readonly Color HeaderDark;

		// Token: 0x0400008A RID: 138
		private static readonly Color ButtonRed;

		// Token: 0x0400008B RID: 139
		private static readonly Color ButtonRedHover;

		// Token: 0x0400008C RID: 140
		private static readonly Color ButtonRedPressed;

		// Token: 0x0400008D RID: 141
		private static readonly Color ButtonGold;

		// Token: 0x0400008E RID: 142
		private static readonly Color TextGold;

		// Token: 0x0400008F RID: 143
		private static readonly Color TextPale;

		// Token: 0x04000090 RID: 144
		private static readonly Color InputDark;

		// Token: 0x04000091 RID: 145
		private static readonly Color ToggleOn;

		// Token: 0x04000092 RID: 146
		private const int MaxSavedLocations = 64;

		// Token: 0x04000093 RID: 147
		private GameObject _nativeRoot;

		// Token: 0x04000094 RID: 148
		private RectTransform _playerPage;

		// Token: 0x04000095 RID: 149
		private RectTransform _itemsPage;

		// Token: 0x04000096 RID: 150
		private RectTransform _teleportPage;

		// Token: 0x04000097 RID: 151
		private RectTransform _miscPage;

		// Token: 0x04000098 RID: 152
		private RectTransform _craftingPage;

		// Token: 0x04000099 RID: 153
		private RectTransform _alchemyPage;

		// Token: 0x0400009A RID: 154
		private RectTransform _worldPage;

		// Token: 0x0400009B RID: 155
		private RectTransform _zombiesPage;

		// Token: 0x0400009C RID: 156
		private RectTransform _fixPage;

		// Token: 0x0400009D RID: 157
		private RectTransform _itemContent;

		// Token: 0x0400009E RID: 158
		private RectTransform _locationDropdownList;

		// Token: 0x0400009F RID: 159
		private RectTransform _locationDropdownContent;

		// Token: 0x040000A0 RID: 160
		private RectTransform _languageDropdownList;

		// Token: 0x040000A1 RID: 161
		private TMP_InputField _searchInput;

		// Token: 0x040000A2 RID: 162
		private TMP_InputField _quantityInput;

		// Token: 0x040000A3 RID: 163
		private TMP_InputField _goldInput;

		// Token: 0x040000A4 RID: 164
		private TMP_InputField _silverInput;

		// Token: 0x040000A5 RID: 165
		private TMP_InputField _copperInput;

		// Token: 0x040000A6 RID: 166
		private TMP_InputField _techInput;

		// Token: 0x040000A7 RID: 167
		private TMP_InputField _townGratitudeInput;

		// Token: 0x040000A8 RID: 168
		private TMP_InputField _harvestYieldMultiplierInput;

		// Token: 0x040000A9 RID: 169
		private TMP_InputField _gratitudeMultiplierInput;

		// Token: 0x040000AA RID: 170
		private TMP_InputField _locationNameInput;

		// Token: 0x040000AB RID: 171
		private TMP_InputField _finishGrowingRangeInput;

		// Token: 0x040000AC RID: 172
		private TMP_InputField _zombieMoveSpeedInput;

		// Token: 0x040000AD RID: 173
		private TMP_InputField _zombieWorkSpeedInput;

		// Token: 0x040000AE RID: 174
		private TMP_InputField _zombieRedExperienceInput;

		// Token: 0x040000AF RID: 175
		private TMP_InputField _zombieGreenExperienceInput;

		// Token: 0x040000B0 RID: 176
		private TMP_InputField _zombieBlueExperienceInput;

		// Token: 0x040000B1 RID: 177
		private TMP_InputField _machineSpeedMultiplierInput;

		// Token: 0x040000B2 RID: 178
		private TMP_InputField _craftedOutputMultiplierInput;

		// Token: 0x040000B3 RID: 179
		private TMP_InputField _sermonSpeedMultiplierInput;

		// Token: 0x040000B4 RID: 180
		private TMP_InputField _ladderClimbSpeedMultiplierInput;

		// Token: 0x040000B5 RID: 181
		private TMP_InputField _fishPondStockMultiplierInput;

		// Token: 0x040000B6 RID: 182
		private string _pendingLocationName = string.Empty;

		// Token: 0x040000B7 RID: 183
		private TextMeshProUGUI _nativeStatus;

		// Token: 0x040000B8 RID: 184
		private TextMeshProUGUI _locationSelectionText;

		// Token: 0x040000B9 RID: 185
		private readonly TextMeshProUGUI[] _hotbarSlotTexts = new TextMeshProUGUI[4];

		// Token: 0x040000BA RID: 186
		private TextMeshProUGUI _languageSelectionText;

		// Token: 0x040000BB RID: 187
		private TextMeshProUGUI _energyToggleText;

		// Token: 0x040000BC RID: 188
		private TextMeshProUGUI _staminaToggleText;

		// Token: 0x040000BD RID: 189
		private TextMeshProUGUI _invulnerableToggleText;

		// Token: 0x040000BE RID: 190
		private TextMeshProUGUI _instantActionsToggleText;

		// Token: 0x040000BF RID: 191
		private TextMeshProUGUI _sleepAlwaysToggleText;

		// Token: 0x040000C0 RID: 192
		private TextMeshProUGUI _sleepWithoutSavingToggleText;

		// Token: 0x040000C1 RID: 193
		private TextMeshProUGUI _alwaysMaxTownGratitudeToggleText;

		// Token: 0x040000C2 RID: 194
		private TextMeshProUGUI _wakeUpHotkeyText;

		// Token: 0x040000C3 RID: 195
		private TextMeshProUGUI _harvestAdjacentToggleText;

		// Token: 0x040000C4 RID: 196
		private TextMeshProUGUI _fishOverlayToggleText;

		// Token: 0x040000C5 RID: 197
		private TextMeshProUGUI _freeBuildToggleText;

		// Token: 0x040000C6 RID: 198
		private TextMeshProUGUI _unrestrictedBuildToggleText;

		// Token: 0x040000C7 RID: 199
		private TextMeshProUGUI _sharedStorageToggleText;

		// Token: 0x040000C8 RID: 200
		private TextMeshProUGUI _autoUseLaddersToggleText;

		// Token: 0x040000C9 RID: 201
		private TextMeshProUGUI _freeCraftingToggleText;

		// Token: 0x040000CA RID: 202
		private TextMeshProUGUI _freeBuildingCostsToggleText;

		// Token: 0x040000CB RID: 203
		private TextMeshProUGUI _alchemyFolioCompanionToggleText;

		// Token: 0x040000CC RID: 204
		private TextMeshProUGUI _alchemyFolioCraftingToggleText;

		// Token: 0x040000CD RID: 205
		private TextMeshProUGUI _retainAlchemyIngredientsToggleText;

		// Token: 0x040000CE RID: 206
		private TextMeshProUGUI _freeResearchToggleText;

		// Token: 0x040000CF RID: 207
		private TextMeshProUGUI _sermonEveryDayToggleText;

		// Token: 0x040000D0 RID: 208
		private TextMeshProUGUI _sermonRepeatableToggleText;

		// Token: 0x040000D1 RID: 209
		private TextMeshProUGUI _zombiesEveryDayToggleText;

		// Token: 0x040000D2 RID: 210
		private TextMeshProUGUI _zombiesRepeatableToggleText;

		// Token: 0x040000D3 RID: 211
		private TextMeshProUGUI _mapTeleportEverywhereToggleText;

		// Token: 0x040000D4 RID: 212
		private TextMeshProUGUI _finishGrowingHotkeyText;

		// Token: 0x040000D5 RID: 213
		private TextMeshProUGUI _finishCraftsHotkeyText;

		// Token: 0x040000D6 RID: 214
		private TextMeshProUGUI _regrowForageHotkeyText;

		// Token: 0x040000D7 RID: 215
		private TextMeshProUGUI _zombieCollectFinishedProductsToggleText;

		// Token: 0x040000D8 RID: 216
		private TextMeshProUGUI _instantFishingBiteToggleText;

		// Token: 0x040000D9 RID: 217
		private TextMeshProUGUI _autoReelFishingToggleText;

		// Token: 0x040000DA RID: 218
		private Button _playerTabButton;

		// Token: 0x040000DB RID: 219
		private Button _itemsTabButton;

		// Token: 0x040000DC RID: 220
		private Button _teleportTabButton;

		// Token: 0x040000DD RID: 221
		private Button _miscTabButton;

		// Token: 0x040000DE RID: 222
		private Button _craftingTabButton;

		// Token: 0x040000DF RID: 223
		private Button _alchemyTabButton;

		// Token: 0x040000E0 RID: 224
		private Button _worldTabButton;

		// Token: 0x040000E1 RID: 225
		private Button _zombiesTabButton;

		// Token: 0x040000E2 RID: 226
		private Button _fixTabButton;

		// Token: 0x040000E3 RID: 227
		private TMP_FontAsset _pixelFont;

		// Token: 0x040000E4 RID: 228
		private Font _koreanSystemFont;

		// Token: 0x040000E5 RID: 229
		private TMP_FontAsset _koreanFontAsset;

		// Token: 0x040000E6 RID: 230
		private bool _useLegacyKoreanText;

		// Token: 0x040000E7 RID: 231
		private Texture2D _buttonTexture;

		// Token: 0x040000E8 RID: 232
		private Sprite _buttonSprite;

		// Token: 0x040000E9 RID: 233
		private Texture2D _headerTexture;

		// Token: 0x040000EA RID: 234
		private Sprite _headerSprite;

		// Token: 0x040000EB RID: 235
		private Texture2D _sidebarTexture;

		// Token: 0x040000EC RID: 236
		private Sprite _sidebarSprite;

		// Token: 0x040000ED RID: 237
		private Texture2D _backgroundTexture;

		// Token: 0x040000EE RID: 238
		private Sprite _backgroundSprite;

		// Token: 0x040000EF RID: 239
		private Texture2D _borderTexture;

		// Token: 0x040000F0 RID: 240
		private Sprite _borderSprite;

		// Token: 0x040000F1 RID: 241
		private Texture2D _scrollbarTrackTexture;

		// Token: 0x040000F2 RID: 242
		private Sprite _scrollbarTrackSprite;

		// Token: 0x040000F3 RID: 243
		private Texture2D _scrollbarHandleTexture;

		// Token: 0x040000F4 RID: 244
		private Sprite _scrollbarHandleSprite;

		// Token: 0x040000F5 RID: 245
		private PlayerController _textInputLockedController;

		// Token: 0x040000F6 RID: 246
		private bool _textInputLockActive;

		// Token: 0x040000F7 RID: 247
		private bool _uiControlsWereEnabled;

		// Token: 0x040000F8 RID: 248
		private readonly List<SavedLocation> _savedLocations = new List<SavedLocation>();

		// Token: 0x040000F9 RID: 249
		private bool _savedLocationsLoaded;

		// Token: 0x040000FA RID: 250
		private int _selectedLocationIndex = -1;

		// Token: 0x040000FB RID: 251
		private int _hotbarEditingSlot = -1;

		// Token: 0x040000FC RID: 252
		private float _oldTimeScale;

		// Token: 0x040000FD RID: 253
		private bool _oldCursorVisible;

		// Token: 0x040000FE RID: 254
		private CursorLockMode _oldCursorLock;

		// Token: 0x040000FF RID: 255
		private RectTransform _questsPage;

		// Token: 0x04000100 RID: 256
		private RectTransform _questContent;

		// Token: 0x04000101 RID: 257
		private Button _questsTabButton;

		// Token: 0x04000102 RID: 258
		private static readonly MethodInfo QuestSystemDataGetter;

		// Token: 0x04000103 RID: 259
		private readonly List<ZombieWgoData> _managedZombies = new List<ZombieWgoData>();

		// Token: 0x04000104 RID: 260
		private RectTransform _zombieWorkDropdownList;

		// Token: 0x04000105 RID: 261
		private RectTransform _zombieWorkDropdownContent;

		// Token: 0x04000106 RID: 262
		private TextMeshProUGUI _zombieWorkSelectionText;

		// Token: 0x04000107 RID: 263
		private TextMeshProUGUI _zombieWorkActionText;

		// Token: 0x04000108 RID: 264
		private Button _zombieWorkActionButton;

		// Token: 0x04000109 RID: 265
		private Guid _selectedManagedZombieId = Guid.Empty;

		// Token: 0x0200003B RID: 59
		private enum HotkeyCaptureTarget
		{
			// Token: 0x04000139 RID: 313
			None,
			// Token: 0x0400013A RID: 314
			FinishGrowing,
			// Token: 0x0400013B RID: 315
			FinishCrafts,
			// Token: 0x0400013C RID: 316
			RegrowForage,
			// Token: 0x0400013D RID: 317
			WakeUp
		}

		// Token: 0x0200003C RID: 60
		private sealed class QuestAssistEntry
		{
			// Token: 0x0400013E RID: 318
			internal QuestData Quest;

			// Token: 0x0400013F RID: 319
			internal readonly List<ItemCount> MissingItems = new List<ItemCount>();

			// Token: 0x04000140 RID: 320
			internal bool CanSupply;

			// Token: 0x04000141 RID: 321
			internal string Detail;
		}

		// Token: 0x0200003D RID: 61
		[CompilerGenerated]
		private static class __OCache
		{
			// Token: 0x04000142 RID: 322
			public static Func<Wgo, bool> __f0_IsComposter;

			// Token: 0x04000143 RID: 323
			public static Func<Wgo, bool> __f1_IsGardenBed;

			// Token: 0x04000144 RID: 324
			public static Func<KeyCode, bool> __f2_GetKey;

			// Token: 0x04000145 RID: 325
			public static Action<PlayerData> __f3_RestoreHealth;

			// Token: 0x04000146 RID: 326
			public static Action<PlayerData> __f4_FillTownGratitudeToCurrentMaximum;

			// Token: 0x04000147 RID: 327
			public static Func<ItemCount, string> __f5_ItemCountDisplay;
		}
		internal bool PanelArtworkEnabled()
		{
			return this._panelArtwork == null || this._panelArtwork.Value;
		}

		private float ResolveUiScale()
		{
			float configuredScale = (this._uiScale != null) ? this._uiScale.Value : 0f;
			if (configuredScale > 0f)
			{
				return Mathf.Clamp(configuredScale, 0.5f, 4f);
			}
			float fit = Mathf.Min((float)Screen.width / 1200f, (float)Screen.height / 1100f);
			if (!this.PanelArtworkEnabled())
			{
				return Mathf.Clamp(fit, 0.6f, 2.5f);
			}
			float scale = (fit >= 0.9f) ? Mathf.Round(fit) : Mathf.Max(0.5f, Mathf.Floor(fit * 2f) / 2f);
			return Mathf.Clamp(scale, 0.5f, 3f);
		}
		private void UpdateMenuCanvasScale()
		{
			if (this._nativeRoot == null)
			{
				return;
			}
			if (Screen.width == this._lastScreenWidth && Screen.height == this._lastScreenHeight)
			{
				return;
			}
			this._lastScreenWidth = Screen.width;
			this._lastScreenHeight = Screen.height;
			CanvasScaler scaler = this._nativeRoot.GetComponent<CanvasScaler>();
			if (scaler == null)
			{
				return;
			}
			scaler.scaleFactor = this.ResolveUiScale();
			base.Logger.LogInfo(string.Format("Menu canvas rescaled to {0:0.##}x for {1}x{2}.", scaler.scaleFactor, Screen.width, Screen.height));
		}
		private void ApplyCanvasScale()
		{
			if (this._nativeRoot == null)
			{
				return;
			}
			CanvasScaler scaler = this._nativeRoot.GetComponent<CanvasScaler>();
			if (scaler == null)
			{
				return;
			}
			scaler.scaleFactor = this.ResolveUiScale();
		}
		private string UiScaleLabel()
		{
			float actual = this.ResolveUiScale();
			float configured = (this._uiScale != null) ? this._uiScale.Value : 0f;
			if (configured > 0f)
			{
				return string.Format(CultureInfo.InvariantCulture, "{0:0.##}x", actual);
			}
			return string.Format(CultureInfo.InvariantCulture, "{0} ({1:0.##}x)", this.L("Auto"), actual);
		}
		private void ChangeUiScale(float delta)
		{
			float next = Mathf.Clamp(Mathf.Round((this.ResolveUiScale() + delta) * 4f) / 4f, 0.5f, 3f);
			if (this._uiScale != null)
			{
				this._uiScale.Value = next;
			}
			this.ApplyCanvasScale();
			base.Config.Save();
			this.RefreshDisplayLabels();
		}
		private void ResetUiScale()
		{
			if (this._uiScale != null)
			{
				this._uiScale.Value = 0f;
			}
			this.ApplyCanvasScale();
			base.Config.Save();
			this.RefreshDisplayLabels();
		}
		private void TogglePixelPerfect()
		{
			bool enabled = (this._pixelPerfectUi == null || !this._pixelPerfectUi.Value);
			if (this._pixelPerfectUi != null)
			{
				this._pixelPerfectUi.Value = enabled;
			}
			Canvas canvas = (this._nativeRoot != null) ? this._nativeRoot.GetComponent<Canvas>() : null;
			if (canvas != null)
			{
				canvas.pixelPerfect = enabled;
			}
			base.Config.Save();
			this.RefreshDisplayLabels();
		}
		private void TogglePanelArtwork()
		{
			if (this._panelArtwork != null)
			{
				this._panelArtwork.Value = !this.PanelArtworkEnabled();
			}
			base.Config.Save();
			this.RebuildNativeMenuForLanguage();
		}
		private void CycleFontMode()
		{
			string mode = this.FontModeValue();
			string next;
			if (mode.Equals("Auto", StringComparison.OrdinalIgnoreCase))
			{
				next = "Game";
			}
			else if (mode.Equals("Game", StringComparison.OrdinalIgnoreCase))
			{
				next = "System";
			}
			else
			{
				next = "Auto";
			}
			if (this._fontMode != null)
			{
				this._fontMode.Value = next;
			}
			base.Config.Save();
			this.RebuildNativeMenuForLanguage();
		}
		private void RefreshDisplayLabels()
		{
			if (this._uiScaleText != null)
			{
				this._uiScaleText.text = this.UiScaleLabel();
			}
			if (this._pixelPerfectText != null)
			{
				this._pixelPerfectText.text = this.L((this._pixelPerfectUi == null || this._pixelPerfectUi.Value) ? "Pixel perfect: On" : "Pixel perfect: Off");
			}
			if (this._panelArtworkText != null)
			{
				this._panelArtworkText.text = this.L(this.PanelArtworkEnabled() ? "Panel artwork: On" : "Panel artwork: Off");
			}
			if (this._fontModeText != null)
			{
				this._fontModeText.text = this.L("Font mode: " + this.FontModeValue());
			}
		}
		private static string NormalizeSearchText(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}
			string text = Regex.Replace(value, "<[^>]*>", string.Empty);
			StringBuilder builder = new StringBuilder(text.Length);
			foreach (char c in text)
			{
				if (char.IsWhiteSpace(c) || c == '\u200b' || c == '\u200c' || c == '\u200d' || c == '\ufeff' || c == '\u3000')
				{
					continue;
				}
				builder.Append(c);
			}
			return builder.ToString();
		}
		private static bool ItemMatchesSearch(ItemDef item, string search, bool allowLoose)
		{
			string query = KeeperCheatMenuPlugin.NormalizeSearchText(search);
			if (query.Length == 0)
			{
				return true;
			}
			string haystack = KeeperCheatMenuPlugin.NormalizeSearchText(KeeperCheatMenuPlugin.ItemLabel(item));
			if (haystack.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			if (!allowLoose || query.Length < 2)
			{
				return false;
			}
			foreach (char c in query)
			{
				if (haystack.IndexOf(c) < 0)
				{
					return false;
				}
			}
			return true;
		}
		private static string DescribeChars(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return "(empty)";
			}
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < value.Length && i < 12; i++)
			{
				builder.Append("U+").Append(((int)value[i]).ToString("X4")).Append(' ');
			}
			return builder.ToString().Trim();
		}
		private static string ItemDisplayName(string itemId)
		{
			if (string.IsNullOrEmpty(itemId))
			{
				return string.Empty;
			}
			try
			{
				GameBalance me = GameBalance.Me;
				List<ItemDef> itemDefs = (me != null) ? me.itemDefs : null;
				if (itemDefs != null)
				{
					foreach (ItemDef itemDef in itemDefs)
					{
						if (itemDef != null && itemDef.id == itemId)
						{
							return KeeperCheatMenuPlugin.ShortItemName(itemDef);
						}
					}
				}
			}
			catch
			{
			}
			return itemId;
		}
		private string FontModeValue()
		{
			string value = (this._fontMode != null) ? this._fontMode.Value : "Auto";
			if (string.IsNullOrEmpty(value))
			{
				return "Auto";
			}
			return value.Trim();
		}
		private static bool IsLowDetailFont(TMP_FontAsset font)
		{
			if (font == null)
			{
				return false;
			}
			string renderMode = font.creationSettings.renderMode.ToString();
			if (renderMode.IndexOf("SDF", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return true;
			}
			Texture2D atlas = font.atlas;
			if (atlas != null && (atlas.width < 1024 || atlas.height < 1024))
			{
				return true;
			}
			return font.creationSettings.pointSize > 0 && font.creationSettings.pointSize < 44;
		}
		private static string DescribeFont(TMP_FontAsset font)
		{
			if (font == null)
			{
				return "none";
			}
			Texture2D atlas = font.atlas;
			string atlasSize = (atlas != null) ? (atlas.width + "x" + atlas.height) : "no atlas";
			return string.Format("{0}, render mode {1}, sampling {2}pt, atlas {3}", new object[]
			{
				font.name,
				font.creationSettings.renderMode,
				font.creationSettings.pointSize,
				atlasSize
			});
		}

		private ConfigEntry<float> _uiScale;

		private ConfigEntry<bool> _pixelPerfectUi;

		private ConfigEntry<string> _fontMode;

		private ConfigEntry<bool> _panelArtwork;

		private TextMeshProUGUI _uiScaleText;

		private TextMeshProUGUI _pixelPerfectText;

		private TextMeshProUGUI _panelArtworkText;

		private TextMeshProUGUI _fontModeText;

		private int _lastScreenWidth;

		private int _lastScreenHeight;

	}
}
