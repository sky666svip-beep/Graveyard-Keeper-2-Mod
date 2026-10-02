using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace KeeperBodyZombieEditor
{
	[BepInPlugin("Narodum.gk2.keeperbodyzombieeditor", "Keeper Body Zombie Editor", "1.0.0")]
	public class KeeperBodyZombieEditorPlugin : BaseUnityPlugin
	{
		public const string PluginGuid = "Narodum.gk2.keeperbodyzombieeditor";
		public const string PluginName = "Keeper Body Zombie Editor";
		public const string PluginVersion = "1.0.0";

		public static KeeperBodyZombieEditorPlugin Instance { get; private set; }
		internal new static ManualLogSource Logger { get; private set; }

		public static ConfigEntry<KeyCode> ToggleKeyEntry { get; private set; }
		private Harmony _harmony;

		private void Awake()
		{
			Instance = this;
			Logger = base.Logger;

			// 注册配置项
			ToggleKeyEntry = Config.Bind("General", "ToggleKey", KeyCode.F8, "Hotkey to toggle Body & Zombie Worker Editor window");

			// 打印规范日志横幅（加载链路三证据凭证与清晰指引）
			Logger.LogInfo("==================================================");
			Logger.LogInfo(string.Format("Loading [{0} {1}]", PluginName, PluginVersion));
			Logger.LogInfo(string.Format("GUID: {0}", PluginGuid));
			Logger.LogInfo(string.Format("Hotkey: Press [{0}] in game to toggle editor.", ToggleKeyEntry.Value));
			Logger.LogInfo("Or interact with Autopsy Table / Zombie Worker window.");
			Logger.LogInfo("==================================================");

			try
			{
				// 初始化数据持久化路径
				BodyZombieCustomData.Init(Paths.ConfigPath);

				// 应用 Harmony 补丁
				_harmony = new Harmony(PluginGuid);
				_harmony.PatchAll(typeof(KeeperBodyZombieEditorPlugin).Assembly);

				Logger.LogInfo("Keeper Body Zombie Editor initialized successfully.");

				// 预先初始化编辑器窗口并注册语言切换监听
				BodyZombieEditorWindow.EnsureInstance();
			}
			catch (Exception ex)
			{
				Logger.LogError("Failed to initialize Keeper Body Zombie Editor: " + ex);
			}
		}

		private void Update()
		{
			try
			{
				if (BodyZombieEditorWindow.Instance != null && BodyZombieEditorWindow.Instance.IsCapturingHotkey)
				{
					return;
				}

				if (ToggleKeyEntry != null && Input.GetKeyDown(ToggleKeyEntry.Value))
				{
					BodyZombieEditorWindow.EnsureInstance();
					BodyZombieEditorWindow.Instance.ToggleGlobal();
				}
			}
			catch (Exception ex)
			{
				Logger.LogWarning("Error in Plugin.Update: " + ex.Message);
			}
		}

		private void OnDestroy()
		{
			if (_harmony != null)
			{
				try
				{
					_harmony.UnpatchAll(PluginGuid);
				}
				catch { }
				_harmony = null;
			}
			Instance = null;
		}
	}
}
