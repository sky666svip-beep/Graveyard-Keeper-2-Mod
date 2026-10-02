using System;
using System.Collections.Generic;
using System.Linq;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KeeperBodyZombieEditor
{
	public enum EditorTargetMode
	{
		Autopsy,
		ZombieWindow,
		Carried,
		WorldZombie
	}

	public class BodyZombieEditorWindow : MonoBehaviour
	{
		public static BodyZombieEditorWindow Instance { get; private set; }

		private GameObject _canvasRoot;
		private RectTransform _windowPanel;
		private TextMeshProUGUI _titleText;
		private TextMeshProUGUI _targetDescText;
		private TextMeshProUGUI _noticeText;
		private TextMeshProUGUI _statusText;

		// 僵尸切换按键与容器
		private GameObject _zombieSwitcherRow;
		private Button _prevZombieBtn;
		private Button _nextZombieBtn;

		// 骷髅编辑控件
		private TextMeshProUGUI _whiteLabel;
		private TextMeshProUGUI _redLabel;
		private TMP_InputField _whiteInput;
		private TMP_InputField _redInput;
		private Button _applySkullsBtn;
		private Button _resetSkullsBtn;

		// 科技点编辑控件
		private GameObject _techSection;
		private TextMeshProUGUI _techRedLabel;
		private TextMeshProUGUI _techGreenLabel;
		private TextMeshProUGUI _techBlueLabel;
		private TMP_InputField _techRedInput;
		private TMP_InputField _techGreenInput;
		private TMP_InputField _techBlueInput;
		private Button _applyTechCurrentBtn;
		private Button _applyTechAllBtn;

		private EditorTargetMode _currentMode = EditorTargetMode.WorldZombie;
		private UIAutopsyWindow _activeAutopsyWindow;
		private UIZombieWorkerWindow _activeZombieWorkerWindow;
		private int _selectedWorldZombieIndex = 0;
		private List<ZombieWgoData> _cachedWorldZombies = new List<ZombieWgoData>();
		private string _lastKnownLang;

		private Button _hotkeyBtn;
		private bool _isCapturingHotkey = false;

		public bool IsCapturingHotkey => _isCapturingHotkey;

		public bool IsVisible => _windowPanel != null && _windowPanel.gameObject.activeSelf;

		public static bool IsGameReady => MainGame.Instance != null && MainGame.Instance.GameSave != null && MainGame.PlayerData != null;

		private static ZombieSystemData SafeZombieSystemData
		{
			get
			{
				try
				{
					if (MainGame.Instance != null && MainGame.Instance.GameSave != null)
					{
						return MainGame.Instance.GameSave.zombieSystemData;
					}
				}
				catch { }
				return null;
			}
		}

		private static PlayerData SafePlayerData
		{
			get
			{
				try
				{
					if (MainGame.Instance != null && MainGame.Instance.GameSave != null)
					{
						return MainGame.PlayerData;
					}
				}
				catch { }
				return null;
			}
		}

		public static void EnsureInstance()
		{
			if (Instance != null) return;
			GameObject go = new GameObject("KeeperBodyZombieEditorUI");
			DontDestroyOnLoad(go);
			Instance = go.AddComponent<BodyZombieEditorWindow>();
			Instance.InitCanvas();
		}

		private void InitCanvas()
		{
			_canvasRoot = gameObject;
			Canvas canvas = _canvasRoot.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 31000;

			CanvasScaler scaler = _canvasRoot.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
			scaler.scaleFactor = (LazyUI.ScaleFactor > 0.5f) ? LazyUI.ScaleFactor : 2f;

			_canvasRoot.AddComponent<GraphicRaycaster>();

			_lastKnownLang = ModLocalization.Lang;
			BuildWindowUI();
			GameSettings.OnLanguageChanged += OnLanguageChanged;

			// 初始默认隐藏
			_windowPanel.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			GameSettings.OnLanguageChanged -= OnLanguageChanged;
		}

		public void OnLanguageChanged()
		{
			try
			{
				FontHelper.ClearCache();
				UpdateLocalizedTexts();
				if (IsVisible && IsGameReady)
				{
					RefreshCurrentTargetData();
				}
				_lastKnownLang = ModLocalization.Lang;
				KeeperBodyZombieEditorPlugin.Logger?.LogInfo($"[Keeper Body Zombie Editor] Language switched to: {_lastKnownLang}");
			}
			catch (Exception ex)
			{
				KeeperBodyZombieEditorPlugin.Logger?.LogWarning("Error in OnLanguageChanged: " + ex.Message);
			}
		}

		private void BuildWindowUI()
		{
			// 主窗口边框 (宽 520, 高 350)
			_windowPanel = UIHelper.CreateRect("EditorPanel", transform,
				new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
				new Vector2(100f, -175f), new Vector2(620f, 175f),
				UIHelper.ColorBorder);

			// 内部深色背景
			RectTransform inner = UIHelper.CreateRect("InnerBg", _windowPanel,
				Vector2.zero, Vector2.one,
				new Vector2(2f, 2f), new Vector2(-2f, -2f),
				UIHelper.ColorPanelBg);

			// 1. 顶部标题栏 (高 36)
			RectTransform header = UIHelper.CreateRect("HeaderBar", inner,
				new Vector2(0f, 1f), new Vector2(1f, 1f),
				new Vector2(0f, -36f), new Vector2(0f, 0f),
				UIHelper.ColorHeaderBg);

			UIDragHandle drag = header.gameObject.AddComponent<UIDragHandle>();
			drag.Init(_windowPanel);

			_titleText = UIHelper.CreateText(ModLocalization.EditorTitle, header, 15f, UIHelper.ColorTextGold, TextAlignmentOptions.MidlineLeft,
				new Vector2(0f, 0f), new Vector2(1f, 1f),
				new Vector2(12f, 0f), new Vector2(-155f, 0f));

			// 热键修改按钮
			_hotkeyBtn = UIHelper.CreateButton("HotkeyBtn", header, "",
				new Vector2(1f, 0f), new Vector2(1f, 1f),
				new Vector2(-150f, 4f), new Vector2(-38f, -4f),
				OnHotkeyBtnClicked, 12f, true);

			// 关闭按钮 [X]
			Button closeBtn = UIHelper.CreateButton("CloseBtn", header, "X",
				new Vector2(1f, 0f), new Vector2(1f, 1f),
				new Vector2(-34f, 4f), new Vector2(-6f, -4f),
				CloseWindow, 14f, true);

			// 2. 当前目标描述栏 (y: -40 到 -75)
			RectTransform targetBox = UIHelper.CreateRect("TargetBox", inner,
				new Vector2(0f, 1f), new Vector2(1f, 1f),
				new Vector2(10f, -75f), new Vector2(-10f, -40f),
				UIHelper.ColorHeaderBg * 0.8f);

			_targetDescText = UIHelper.CreateText("", targetBox, 13f, UIHelper.ColorTextPale, TextAlignmentOptions.MidlineLeft,
				new Vector2(0f, 0f), new Vector2(1f, 1f),
				new Vector2(8f, 0f), new Vector2(-8f, 0f));

			// 3. 世界僵尸切换栏 (当处于 WorldZombie 模式时显示，y: -80 到 -115)
			RectTransform switchBox = UIHelper.CreateRect("ZombieSwitchBox", inner,
				new Vector2(0f, 1f), new Vector2(1f, 1f),
				new Vector2(10f, -115f), new Vector2(-10f, -80f),
				Color.clear);
			_zombieSwitcherRow = switchBox.gameObject;

			_prevZombieBtn = UIHelper.CreateButton("PrevZombieBtn", switchBox, ModLocalization.PrevZombie,
				new Vector2(0f, 0f), new Vector2(0f, 1f),
				new Vector2(0f, 0f), new Vector2(110f, 0f),
				OnPrevZombieClicked, 13f, true);

			_nextZombieBtn = UIHelper.CreateButton("NextZombieBtn", switchBox, ModLocalization.NextZombie,
				new Vector2(1f, 0f), new Vector2(1f, 1f),
				new Vector2(-110f, 0f), new Vector2(0f, 0f),
				OnNextZombieClicked, 13f, true);

			// 4. 骷髅编辑分组 (白骷髅、红骷髅，y: -125 到 -195)
			RectTransform skullGroup = UIHelper.CreateRect("SkullGroup", inner,
				new Vector2(0f, 1f), new Vector2(1f, 1f),
				new Vector2(10f, -195f), new Vector2(-10f, -125f),
				UIHelper.ColorHeaderBg * 0.6f);

			_whiteLabel = UIHelper.CreateText(ModLocalization.IconWhiteSkullZombie, skullGroup, 20f, Color.white, TextAlignmentOptions.Center,
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(12f, -16f), new Vector2(44f, 16f));

			_whiteInput = UIHelper.CreateInputField("WhiteInput", skullGroup, "0",
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(48f, -15f), new Vector2(105f, 15f), 14f);

			_redLabel = UIHelper.CreateText(ModLocalization.IconRedSkullZombie, skullGroup, 20f, Color.white, TextAlignmentOptions.Center,
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(115f, -16f), new Vector2(147f, 16f));

			_redInput = UIHelper.CreateInputField("RedInput", skullGroup, "0",
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(151f, -15f), new Vector2(208f, 15f), 14f);

			_applySkullsBtn = UIHelper.CreateButton("ApplySkullsBtn", skullGroup, ModLocalization.BtnApplySkulls,
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(220f, -15f), new Vector2(345f, 15f),
				OnApplySkullsClicked, 13f);

			_resetSkullsBtn = UIHelper.CreateButton("ResetSkullsBtn", skullGroup, ModLocalization.BtnResetSkulls,
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(355f, -15f), new Vector2(485f, 15f),
				OnResetSkullsClicked, 13f, true);

			// 5. 科技点编辑分组 (红/绿/蓝科技点，y: -205 到 -275)
			RectTransform techGroup = UIHelper.CreateRect("TechGroup", inner,
				new Vector2(0f, 1f), new Vector2(1f, 1f),
				new Vector2(10f, -275f), new Vector2(-10f, -205f),
				UIHelper.ColorHeaderBg * 0.6f);
			_techSection = techGroup.gameObject;

			_techRedLabel = UIHelper.CreateText(ModLocalization.IconTechRed, techGroup, 18f, Color.white, TextAlignmentOptions.Center,
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(8f, -15f), new Vector2(36f, 15f));

			_techRedInput = UIHelper.CreateInputField("TechRedInput", techGroup, "0",
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(40f, -15f), new Vector2(88f, 15f), 13f);

			_techGreenLabel = UIHelper.CreateText(ModLocalization.IconTechGreen, techGroup, 18f, Color.white, TextAlignmentOptions.Center,
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(92f, -15f), new Vector2(120f, 15f));

			_techGreenInput = UIHelper.CreateInputField("TechGreenInput", techGroup, "0",
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(124f, -15f), new Vector2(172f, 15f), 13f);

			_techBlueLabel = UIHelper.CreateText(ModLocalization.IconTechBlue, techGroup, 18f, Color.white, TextAlignmentOptions.Center,
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(176f, -15f), new Vector2(204f, 15f));

			_techBlueInput = UIHelper.CreateInputField("TechBlueInput", techGroup, "0",
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(208f, -15f), new Vector2(256f, 15f), 13f);

			_applyTechCurrentBtn = UIHelper.CreateButton("ApplyTechCurrentBtn", techGroup, ModLocalization.BtnApplyTechCurrent,
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(265f, -15f), new Vector2(375f, 15f),
				OnApplyTechCurrentClicked, 12f);

			_applyTechAllBtn = UIHelper.CreateButton("ApplyTechAllBtn", techGroup, ModLocalization.BtnApplyTechAll,
				new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
				new Vector2(382f, -15f), new Vector2(492f, 15f),
				OnApplyTechAllClicked, 12f, true);

			// 6. 状态栏与提示栏 (底部)
			_statusText = UIHelper.CreateText("", inner, 12f, UIHelper.ColorTextGold, TextAlignmentOptions.MidlineLeft,
				new Vector2(0f, 0f), new Vector2(1f, 0f),
				new Vector2(12f, 22f), new Vector2(-12f, 42f));

			_noticeText = UIHelper.CreateText(ModLocalization.HotkeyNotice, inner, 11f, UIHelper.ColorTextMuted, TextAlignmentOptions.MidlineLeft,
				new Vector2(0f, 0f), new Vector2(1f, 0f),
				new Vector2(12f, 4f), new Vector2(-12f, 22f));

			UpdateLocalizedTexts();
		}

		private void UpdateLocalizedTexts()
		{
			TMP_FontAsset font = FontHelper.GetFont();
			TMP_SpriteAsset spriteAsset = FontHelper.GetSpriteAsset();

			if (_windowPanel != null)
			{
				foreach (var tmp in _windowPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
				{
					if (tmp != null)
					{
						tmp.font = font;
						tmp.spriteAsset = spriteAsset;
					}
				}
			}

			if (_titleText != null) _titleText.text = ModLocalization.EditorTitle;
			UpdateHotkeyButtonText();
			if (_whiteLabel != null) _whiteLabel.text = ModLocalization.IconWhiteSkullZombie;
			if (_redLabel != null) _redLabel.text = ModLocalization.IconRedSkullZombie;
			if (_techRedLabel != null) _techRedLabel.text = ModLocalization.IconTechRed;
			if (_techGreenLabel != null) _techGreenLabel.text = ModLocalization.IconTechGreen;
			if (_techBlueLabel != null) _techBlueLabel.text = ModLocalization.IconTechBlue;

			UIHelper.SetButtonText(_prevZombieBtn, ModLocalization.PrevZombie);
			UIHelper.SetButtonText(_nextZombieBtn, ModLocalization.NextZombie);
			UIHelper.SetButtonText(_applySkullsBtn, ModLocalization.BtnApplySkulls);
			UIHelper.SetButtonText(_resetSkullsBtn, ModLocalization.BtnResetSkulls);
			UIHelper.SetButtonText(_applyTechCurrentBtn, ModLocalization.BtnApplyTechCurrent);
			UIHelper.SetButtonText(_applyTechAllBtn, ModLocalization.BtnApplyTechAll);

			if (_noticeText != null) _noticeText.text = ModLocalization.HotkeyNotice;
		}

		private void UpdateHotkeyButtonText()
		{
			if (_hotkeyBtn == null) return;
			if (_isCapturingHotkey)
			{
				UIHelper.SetButtonText(_hotkeyBtn, ModLocalization.HotkeyPressKey);
			}
			else
			{
				string keyStr = KeeperBodyZombieEditorPlugin.ToggleKeyEntry?.Value.ToString() ?? "F8";
				UIHelper.SetButtonText(_hotkeyBtn, ModLocalization.HotkeyButton(keyStr));
			}
		}

		private void OnHotkeyBtnClicked()
		{
			if (_isCapturingHotkey)
			{
				_isCapturingHotkey = false;
				UpdateHotkeyButtonText();
				_statusText.text = ModLocalization.StatusHotkeyCancelled;
			}
			else
			{
				_isCapturingHotkey = true;
				UpdateHotkeyButtonText();
				_statusText.text = ModLocalization.HotkeyPressKey;
			}
		}

		private static bool IsKeyboardHotkey(KeyCode key)
		{
			if (key == KeyCode.None || key == KeyCode.Escape) return false;
			string name = key.ToString();
			if (name.StartsWith("Mouse") || name.StartsWith("Joystick")) return false;
			return true;
		}

		private void SetNewHotkey(KeyCode newKey)
		{
			_isCapturingHotkey = false;
			if (KeeperBodyZombieEditorPlugin.ToggleKeyEntry != null)
			{
				KeeperBodyZombieEditorPlugin.ToggleKeyEntry.Value = newKey;
				try
				{
					KeeperBodyZombieEditorPlugin.Instance?.Config?.Save();
				}
				catch (Exception ex)
				{
					KeeperBodyZombieEditorPlugin.Logger?.LogWarning("Failed to save config: " + ex.Message);
				}
			}
			UpdateHotkeyButtonText();
			if (_noticeText != null) _noticeText.text = ModLocalization.HotkeyNotice;
			_statusText.text = ModLocalization.StatusHotkeyUpdated(newKey.ToString());
			KeeperBodyZombieEditorPlugin.Logger?.LogInfo($"[Keeper Body Zombie Editor] Hotkey set to: {newKey}");
		}

		private void Update()
		{
			// 语言热变更侦测
			string currentLang = ModLocalization.Lang;
			if (_lastKnownLang != currentLang)
			{
				OnLanguageChanged();
			}

			if (IsVisible)
			{
				if (_isCapturingHotkey)
				{
					if (Input.anyKeyDown)
					{
						if (Input.GetKeyDown(KeyCode.Escape))
						{
							_isCapturingHotkey = false;
							UpdateHotkeyButtonText();
							_statusText.text = ModLocalization.StatusHotkeyCancelled;
							return;
						}

						foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
						{
							if (Input.GetKeyDown(k) && IsKeyboardHotkey(k))
							{
								SetNewHotkey(k);
								return;
							}
						}
					}
					return;
				}

				// 惰性检测：如果依附的原版窗口已关闭，自动收起对应模式
				if (_currentMode == EditorTargetMode.Autopsy)
				{
					if (_activeAutopsyWindow == null || !_activeAutopsyWindow.IsShown)
					{
						CloseWindow();
					}
				}
				else if (_currentMode == EditorTargetMode.ZombieWindow)
				{
					if (_activeZombieWorkerWindow == null || !_activeZombieWorkerWindow.IsShown)
					{
						CloseWindow();
					}
				}

				if (Input.GetKeyDown(KeyCode.Escape))
				{
					CloseWindow();
				}
			}
		}

		// 呼出逻辑 (热键 F8 触发)
		public void ToggleGlobal()
		{
			if (IsVisible)
			{
				CloseWindow();
				return;
			}

			if (!IsGameReady)
			{
				KeeperBodyZombieEditorPlugin.Logger?.LogWarning("Cannot open editor: Game or save is not loaded yet.");
				return;
			}

			// 1. 优先检查当前是否正打开解剖台窗口
			UIAutopsyWindow autopsyWin = _activeAutopsyWindow;
			if (autopsyWin == null || !autopsyWin.IsShown)
			{
				try { autopsyWin = LazyUI.GetWindow<UIAutopsyWindow>(); } catch { }
			}
			if (autopsyWin != null && autopsyWin.IsShown)
			{
				ShowForAutopsy(autopsyWin);
				return;
			}

			// 2. 检查当前是否正打开僵尸工人窗口
			UIZombieWorkerWindow zombieWin = _activeZombieWorkerWindow;
			if (zombieWin == null || !zombieWin.IsShown)
			{
				try { zombieWin = LazyUI.GetWindow<UIZombieWorkerWindow>(); } catch { }
			}
			if (zombieWin != null && zombieWin.IsShown)
			{
				ShowForZombieWorker(zombieWin);
				return;
			}

			// 3. 检查玩家身上是否正搬运尸体/僵尸
			Item carried = SafePlayerData?.overheadItem;
			if (carried != null && carried.Definition?.itemGroupIds != null && carried.Definition.itemGroupIds.Contains("body"))
			{
				ShowForCarried(carried);
				return;
			}

			// 4. 否则开启全图僵尸管理模式
			ShowForWorldZombies();
		}

		public void OnAutopsyActive(UIAutopsyWindow window)
		{
			_activeAutopsyWindow = window;
			// 仅当用户主动打开了修改器且正处于解剖台模式时，才同步刷新数据；绝不主动弹出窗口！
			if (IsVisible && _currentMode == EditorTargetMode.Autopsy)
			{
				RefreshCurrentTargetData();
			}
		}

		public void ShowForAutopsy(UIAutopsyWindow window)
		{
			_currentMode = EditorTargetMode.Autopsy;
			_activeAutopsyWindow = window;
			OpenWindow();
		}

		public void OnAutopsyHide()
		{
			_activeAutopsyWindow = null;
			if (_currentMode == EditorTargetMode.Autopsy)
			{
				CloseWindow();
			}
		}

		public void OnZombieWorkerActive(UIZombieWorkerWindow window)
		{
			_activeZombieWorkerWindow = window;
			// 仅当用户主动打开了修改器且正处于僵尸窗口模式时，才同步刷新数据；绝不主动弹出窗口！
			if (IsVisible && _currentMode == EditorTargetMode.ZombieWindow)
			{
				RefreshCurrentTargetData();
			}
		}

		public void ShowForZombieWorker(UIZombieWorkerWindow window)
		{
			_currentMode = EditorTargetMode.ZombieWindow;
			_activeZombieWorkerWindow = window;
			OpenWindow();
		}

		public void OnZombieWorkerHide()
		{
			_activeZombieWorkerWindow = null;
			if (_currentMode == EditorTargetMode.ZombieWindow)
			{
				CloseWindow();
			}
		}

		public void ShowForCarried(Item item)
		{
			_currentMode = EditorTargetMode.Carried;
			OpenWindow();
		}

		public void ShowForWorldZombies()
		{
			_currentMode = EditorTargetMode.WorldZombie;
			ReloadWorldZombies();
			OpenWindow();
		}

		private void OpenWindow()
		{
			string currentLang = ModLocalization.Lang;
			if (_lastKnownLang != currentLang)
			{
				FontHelper.ClearCache();
				_lastKnownLang = currentLang;
			}

			UpdateLocalizedTexts();
			RefreshCurrentTargetData();

			if (_windowPanel != null)
			{
				_windowPanel.gameObject.SetActive(true);
			}

			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;

			KeeperBodyZombieEditorPlugin.Logger.LogInfo($"[Keeper Body Zombie Editor] Window opened in mode: {_currentMode} (Lang: {_lastKnownLang})");
		}

		public void CloseWindow()
		{
			if (_isCapturingHotkey)
			{
				_isCapturingHotkey = false;
				UpdateHotkeyButtonText();
			}

			if (_windowPanel != null)
			{
				_windowPanel.gameObject.SetActive(false);
			}
			KeeperBodyZombieEditorPlugin.Logger.LogInfo("[Keeper Body Zombie Editor] Window closed.");
		}

		private void ReloadWorldZombies()
		{
			_cachedWorldZombies.Clear();
			try
			{
				ZombieSystemData zsd = SafeZombieSystemData;
				if (zsd?.Cache != null)
				{
					foreach (ZombieWgoData z in zsd.Cache.Values)
					{
						if (z != null && !_cachedWorldZombies.Contains(z))
						{
							_cachedWorldZombies.Add(z);
						}
					}
				}
			}
			catch (Exception ex)
			{
				KeeperBodyZombieEditorPlugin.Logger?.LogWarning("Error in ReloadWorldZombies: " + ex.Message);
			}

			if (_selectedWorldZombieIndex >= _cachedWorldZombies.Count)
			{
				_selectedWorldZombieIndex = Math.Max(0, _cachedWorldZombies.Count - 1);
			}
		}

		private void OnPrevZombieClicked()
		{
			if (_cachedWorldZombies.Count == 0) return;
			_selectedWorldZombieIndex--;
			if (_selectedWorldZombieIndex < 0) _selectedWorldZombieIndex = _cachedWorldZombies.Count - 1;
			RefreshCurrentTargetData();
		}

		private void OnNextZombieClicked()
		{
			if (_cachedWorldZombies.Count == 0) return;
			_selectedWorldZombieIndex++;
			if (_selectedWorldZombieIndex >= _cachedWorldZombies.Count) _selectedWorldZombieIndex = 0;
			RefreshCurrentTargetData();
		}

		private static string GetItemDisplayName(Item item)
		{
			if (item == null) return "None";
			string id = item.id;
			string loc = LLBase.L(id);
			return string.IsNullOrEmpty(loc) ? id : ModLocalization.Clean(loc);
		}

		private static string GetWgoDisplayName(WgoData wgo)
		{
			if (wgo == null) return "Roaming";
			string id = wgo.id;
			string loc = LLBase.L(id);
			return string.IsNullOrEmpty(loc) ? id : ModLocalization.Clean(loc);
		}

		private void RefreshCurrentTargetData()
		{
			_statusText.text = "";

			switch (_currentMode)
			{
				case EditorTargetMode.Autopsy:
				{
					_zombieSwitcherRow.SetActive(false);
					_techSection.SetActive(false);

					var data = _activeAutopsyWindow?.GetWidgetData();
					Item body = data?.CorpseWidgetData?.Body;
					if (body == null)
					{
						_targetDescText.text = $"{ModLocalization.TargetPrefixAutopsy}: (空解剖台)";
						_whiteInput.text = "0";
						_redInput.text = "0";
						return;
					}

					string uid = body.UniqueId.ToString();
					int w;
					int r;
					if (BodyZombieCustomData.TryGetCustomSkulls(uid, out int customW, out int customR))
					{
						w = customW;
						r = customR;
					}
					else
					{
						w = data.CorpseWidgetData.WhiteSkulls;
						r = data.CorpseWidgetData.RedSkulls;
					}
					_targetDescText.text = $"{ModLocalization.TargetPrefixAutopsy}: {GetItemDisplayName(body)} (UID: {uid.Substring(0, Math.Min(8, uid.Length))}...)";
					_whiteInput.text = w.ToString();
					_redInput.text = r.ToString();
					break;
				}
				case EditorTargetMode.ZombieWindow:
				{
					_zombieSwitcherRow.SetActive(false);
					_techSection.SetActive(true);

					var data = _activeZombieWorkerWindow?.GetWidgetData();
					ZombieWgoData zombie = data?.ZombieWgoData;
					if (zombie == null)
					{
						_targetDescText.text = $"{ModLocalization.TargetPrefixZombie}: (未选中)";
						return;
					}

					string loc = GetWgoDisplayName(zombie.AttachedWgoData);
					_targetDescText.text = $"{ModLocalization.TargetPrefixZombie}: {zombie.Name} [{loc}]";
					_whiteInput.text = zombie.WhiteSkulls.ToString();
					_redInput.text = zombie.RedSkulls.ToString();
					_techRedInput.text = zombie.techRed.ToString();
					_techGreenInput.text = zombie.techGreen.ToString();
					_techBlueInput.text = zombie.techBlue.ToString();
					break;
				}
				case EditorTargetMode.Carried:
				{
					_zombieSwitcherRow.SetActive(false);
					Item carried = SafePlayerData?.overheadItem;
					if (carried == null)
					{
						_targetDescText.text = $"{ModLocalization.TargetPrefixCarried}: (无)";
						return;
					}

					ZombieWgoData zData = SafeZombieSystemData?.GetZombie(carried.UniqueId);
					bool isZombie = (zData != null);
					_techSection.SetActive(isZombie);

					string uid = carried.UniqueId.ToString();
					BodyZombieCustomData.TryGetCustomSkulls(uid, out int w, out int r);
					_targetDescText.text = $"{ModLocalization.TargetPrefixCarried}: {GetItemDisplayName(carried)}";
					_whiteInput.text = w.ToString();
					_redInput.text = r.ToString();
					if (isZombie)
					{
						_techRedInput.text = zData.techRed.ToString();
						_techGreenInput.text = zData.techGreen.ToString();
						_techBlueInput.text = zData.techBlue.ToString();
					}
					break;
				}
				case EditorTargetMode.WorldZombie:
				{
					_zombieSwitcherRow.SetActive(true);
					_techSection.SetActive(true);
					ReloadWorldZombies();

					if (_cachedWorldZombies.Count == 0)
					{
						_targetDescText.text = ModLocalization.NoZombiesInWorld;
						_whiteInput.text = "0";
						_redInput.text = "0";
						_techRedInput.text = "0";
						_techGreenInput.text = "0";
						_techBlueInput.text = "0";
						return;
					}

					ZombieWgoData currentZombie = _cachedWorldZombies[_selectedWorldZombieIndex];
					string loc = GetWgoDisplayName(currentZombie.AttachedWgoData);
					_targetDescText.text = $"{ModLocalization.TargetPrefixWorldZombie} ({_selectedWorldZombieIndex + 1}/{_cachedWorldZombies.Count}): {currentZombie.Name} [{loc}]";
					_whiteInput.text = currentZombie.WhiteSkulls.ToString();
					_redInput.text = currentZombie.RedSkulls.ToString();
					_techRedInput.text = currentZombie.techRed.ToString();
					_techGreenInput.text = currentZombie.techGreen.ToString();
					_techBlueInput.text = currentZombie.techBlue.ToString();
					break;
				}
			}
		}

		private void OnApplySkullsClicked()
		{
			try
			{
				if (!int.TryParse(_whiteInput.text, out int white) || white < 0) white = 0;
				if (!int.TryParse(_redInput.text, out int red) || red < 0) red = 0;

				string targetUid = null;
				bool isZombie = false;

				if (_currentMode == EditorTargetMode.Autopsy)
				{
					var data = _activeAutopsyWindow?.GetWidgetData();
					Item body = data?.CorpseWidgetData?.Body;
					if (body == null) return;
					targetUid = body.UniqueId.ToString();
					BodyZombieCustomData.SetCustomSkulls(targetUid, white, red);
					if (data.CorpseWidgetData.ZombieWgoData != null)
					{
						isZombie = true;
						BodyZombieCustomData.SetCustomSkulls(data.CorpseWidgetData.ZombieWgoData.UniqueId.ToString(), white, red);
					}
					try
					{
						_activeAutopsyWindow.Redraw();
					}
					catch { }
				}
				else if (_currentMode == EditorTargetMode.ZombieWindow)
				{
					ZombieWgoData zombie = _activeZombieWorkerWindow?.GetWidgetData()?.ZombieWgoData;
					if (zombie == null) return;
					targetUid = zombie.UniqueId.ToString();
					isZombie = true;
					BodyZombieCustomData.SetCustomSkulls(targetUid, white, red);
					if (zombie.ZombieItem != null)
					{
						BodyZombieCustomData.SetCustomSkulls(zombie.ZombieItem.UniqueId.ToString(), white, red);
					}
					try
					{
						_activeZombieWorkerWindow.Redraw();
					}
					catch { }
				}
				else if (_currentMode == EditorTargetMode.Carried)
				{
					Item carried = SafePlayerData?.overheadItem;
					if (carried == null) return;
					targetUid = carried.UniqueId.ToString();
					BodyZombieCustomData.SetCustomSkulls(targetUid, white, red);
					ZombieWgoData zData = SafeZombieSystemData?.GetZombie(carried.UniqueId);
					if (zData != null)
					{
						isZombie = true;
						BodyZombieCustomData.SetCustomSkulls(zData.UniqueId.ToString(), white, red);
					}
				}
				else if (_currentMode == EditorTargetMode.WorldZombie)
				{
					if (_cachedWorldZombies.Count == 0) return;
					ZombieWgoData currentZombie = _cachedWorldZombies[_selectedWorldZombieIndex];
					targetUid = currentZombie.UniqueId.ToString();
					isZombie = true;
					BodyZombieCustomData.SetCustomSkulls(targetUid, white, red);
					if (currentZombie.ZombieItem != null)
					{
						BodyZombieCustomData.SetCustomSkulls(currentZombie.ZombieItem.UniqueId.ToString(), white, red);
					}
				}

				if (!string.IsNullOrEmpty(targetUid))
				{
					_whiteInput.text = white.ToString();
					_redInput.text = red.ToString();
					_statusText.text = ModLocalization.StatusSetSkullsSuccess(white, red, isZombie);
					KeeperBodyZombieEditorPlugin.Logger.LogInfo($"[Keeper Body Zombie Editor] Applied skulls {white}W/{red}R to UID {targetUid}");
				}
			}
			catch (Exception ex)
			{
				_statusText.text = "Error: " + ex.Message;
			}
		}

		private void OnResetSkullsClicked()
		{
			try
			{
				string targetUid = null;
				if (_currentMode == EditorTargetMode.Autopsy)
				{
					var data = _activeAutopsyWindow?.GetWidgetData();
					Item body = data?.CorpseWidgetData?.Body;
					if (body == null) return;
					targetUid = body.UniqueId.ToString();
					BodyZombieCustomData.RemoveCustomSkulls(targetUid);
					if (data.CorpseWidgetData.ZombieWgoData != null)
					{
						BodyZombieCustomData.RemoveCustomSkulls(data.CorpseWidgetData.ZombieWgoData.UniqueId.ToString());
					}
					try
					{
						_activeAutopsyWindow.Redraw();
					}
					catch { }
				}
				else if (_currentMode == EditorTargetMode.ZombieWindow)
				{
					ZombieWgoData zombie = _activeZombieWorkerWindow?.GetWidgetData()?.ZombieWgoData;
					if (zombie == null) return;
					targetUid = zombie.UniqueId.ToString();
					BodyZombieCustomData.RemoveCustomSkulls(targetUid);
					if (zombie.ZombieItem != null)
					{
						BodyZombieCustomData.RemoveCustomSkulls(zombie.ZombieItem.UniqueId.ToString());
					}
					try
					{
						_activeZombieWorkerWindow.Redraw();
					}
					catch { }
				}
				else if (_currentMode == EditorTargetMode.Carried)
				{
					Item carried = SafePlayerData?.overheadItem;
					if (carried == null) return;
					targetUid = carried.UniqueId.ToString();
					BodyZombieCustomData.RemoveCustomSkulls(targetUid);
				}
				else if (_currentMode == EditorTargetMode.WorldZombie)
				{
					if (_cachedWorldZombies.Count == 0) return;
					ZombieWgoData currentZombie = _cachedWorldZombies[_selectedWorldZombieIndex];
					targetUid = currentZombie.UniqueId.ToString();
					BodyZombieCustomData.RemoveCustomSkulls(targetUid);
					if (currentZombie.ZombieItem != null)
					{
						BodyZombieCustomData.RemoveCustomSkulls(currentZombie.ZombieItem.UniqueId.ToString());
					}
				}

				if (!string.IsNullOrEmpty(targetUid))
				{
					_statusText.text = ModLocalization.StatusRestoredDefault();
					RefreshCurrentTargetData();
					KeeperBodyZombieEditorPlugin.Logger.LogInfo($"[Keeper Body Zombie Editor] Removed custom skulls for UID {targetUid}");
				}
			}
			catch (Exception ex)
			{
				_statusText.text = "Error: " + ex.Message;
			}
		}

		private void OnApplyTechCurrentClicked()
		{
			try
			{
				if (!int.TryParse(_techRedInput.text, out int r) || r < 0) r = 0;
				if (!int.TryParse(_techGreenInput.text, out int g) || g < 0) g = 0;
				if (!int.TryParse(_techBlueInput.text, out int b) || b < 0) b = 0;

				ZombieWgoData targetZombie = null;
				if (_currentMode == EditorTargetMode.ZombieWindow)
				{
					targetZombie = _activeZombieWorkerWindow?.GetWidgetData()?.ZombieWgoData;
				}
				else if (_currentMode == EditorTargetMode.Carried)
				{
					Item carried = SafePlayerData?.overheadItem;
					if (carried != null)
					{
						targetZombie = SafeZombieSystemData?.GetZombie(carried.UniqueId);
					}
				}
				else if (_currentMode == EditorTargetMode.WorldZombie)
				{
					if (_cachedWorldZombies.Count > 0)
					{
						targetZombie = _cachedWorldZombies[_selectedWorldZombieIndex];
					}
				}

				if (targetZombie != null)
				{
					targetZombie.techRed = r;
					targetZombie.techGreen = g;
					targetZombie.techBlue = b;
					targetZombie.DoTechPointsReward(null, 0, 0, 0);

					_statusText.text = ModLocalization.StatusSetCurrentTechSuccess(r, g, b);
					KeeperBodyZombieEditorPlugin.Logger.LogInfo($"[Keeper Body Zombie Editor] Applied Tech ({r}, {g}, {b}) to Zombie {targetZombie.Name}");

					if (_activeZombieWorkerWindow != null && _activeZombieWorkerWindow.IsShown)
					{
						_activeZombieWorkerWindow.Redraw();
					}
				}
			}
			catch (Exception ex)
			{
				_statusText.text = "Error: " + ex.Message;
			}
		}

		private void OnApplyTechAllClicked()
		{
			try
			{
				if (!int.TryParse(_techRedInput.text, out int r) || r < 0) r = 0;
				if (!int.TryParse(_techGreenInput.text, out int g) || g < 0) g = 0;
				if (!int.TryParse(_techBlueInput.text, out int b) || b < 0) b = 0;

				int count = 0;
				ZombieSystemData zsd = SafeZombieSystemData;
				if (zsd?.Cache != null)
				{
					foreach (ZombieWgoData z in zsd.Cache.Values)
					{
						if (z == null) continue;
						z.techRed = r;
						z.techGreen = g;
						z.techBlue = b;
						count++;
					}
				}

				_statusText.text = ModLocalization.StatusSetAllTechSuccess(r, g, b, count);
				KeeperBodyZombieEditorPlugin.Logger.LogInfo($"[Keeper Body Zombie Editor] Applied Tech ({r}, {g}, {b}) to all {count} zombies");

				if (_activeZombieWorkerWindow != null && _activeZombieWorkerWindow.IsShown)
				{
					_activeZombieWorkerWindow.Redraw();
				}
			}
			catch (Exception ex)
			{
				_statusText.text = "Error: " + ex.Message;
			}
		}
	}
}
