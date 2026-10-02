using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace FreeExhumation
{
	/// <summary>
	/// Graveyard Keeper 2 plugin: exhume a buried body without the vanilla requirements.
	/// Vanilla gates exhumation behind two conditions (see UIGraveWindowData):
	///   1. CanExhume()            - the grave must have no decoration (gravetop / gravebot parts) left on it;
	///   2. HasExhumeCertificate() - the player must own at least one "exhume_certificate".
	/// This plugin opens both gates and performs the exhumation itself, so no certificate is spent.
	/// </summary>
	[BepInPlugin(FreeExhumationPlugin.PluginGuid, "Free Exhumation", FreeExhumationPlugin.PluginVersion)]
	public sealed class FreeExhumationPlugin : BaseUnityPlugin
	{
		public const string PluginGuid = "Narodum.gk2.freeexhumation";

		public const string PluginVersion = "1.0.0";

		internal static FreeExhumationPlugin Instance { get; private set; }

		private ConfigEntry<bool> _enabled;

		private ConfigEntry<bool> _ignoreDecorationRequirement;

		private ConfigEntry<bool> _requireCertificate;

		private ConfigEntry<bool> _consumeCertificate;

		private ConfigEntry<bool> _autoRemoveDecorations;

		private ConfigEntry<bool> _skipConfirmationDialog;

		private ConfigEntry<bool> _verboseLogging;

		/// <summary>Master switch. When false every patch falls through to the vanilla behaviour.</summary>
		internal static bool Enabled => Instance != null && Instance._enabled != null && Instance._enabled.Value;

		/// <summary>When true the "remove the tombstone / fence first" condition is ignored.</summary>
		internal static bool IgnoreDecorationRequirement =>
			Enabled && Instance._ignoreDecorationRequirement != null && Instance._ignoreDecorationRequirement.Value;

		/// <summary>When true the player does not need to own an exhume certificate.</summary>
		internal static bool IgnoreCertificateRequirement =>
			Enabled && Instance._requireCertificate != null && !Instance._requireCertificate.Value;

		/// <summary>When true one certificate is consumed by an exhumation (only if the player owns one).</summary>
		internal static bool ConsumeCertificate =>
			Enabled && Instance._consumeCertificate != null && Instance._consumeCertificate.Value;

		/// <summary>When true the decorations are returned to the player inventory while exhuming.</summary>
		internal static bool AutoRemoveDecorations =>
			Enabled && Instance._autoRemoveDecorations != null && Instance._autoRemoveDecorations.Value;

		/// <summary>When true the confirmation dialog is skipped: pressing the exhume button digs right away.</summary>
		internal static bool SkipConfirmationDialog =>
			Enabled && Instance._skipConfirmationDialog != null && Instance._skipConfirmationDialog.Value;

		internal static bool VerboseLogging =>
			Instance != null && Instance._verboseLogging != null && Instance._verboseLogging.Value;

		private void Awake()
		{
			Instance = this;

			_enabled = Config.Bind(
				"General",
				"Enabled",
				true,
				"Master switch. Set to false to restore the vanilla exhumation rules.");

			_ignoreDecorationRequirement = Config.Bind(
				"Rules",
				"IgnoreDecorationRequirement",
				true,
				"Exhume even when a tombstone / fence is still installed on the grave.");

			_requireCertificate = Config.Bind(
				"Rules",
				"RequireExhumeCertificate",
				false,
				"When true the vanilla rule is kept: an exhume certificate is required.");

			_consumeCertificate = Config.Bind(
				"Rules",
				"ConsumeExhumeCertificate",
				false,
				"Consume one exhume certificate per exhumation (only when the player owns one).");

			_autoRemoveDecorations = Config.Bind(
				"Rules",
				"AutoRemoveDecorations",
				true,
				"Return the tombstone / fence to the player inventory while exhuming. Skipped when the inventory is full.");

			_skipConfirmationDialog = Config.Bind(
				"Rules",
				"SkipConfirmationDialog",
				false,
				"Dig immediately when the exhume button is pressed, without the confirmation dialog.");

			_verboseLogging = Config.Bind(
				"Debug",
				"VerboseLogging",
				false,
				"Log every patched exhumation to BepInEx/LogOutput.log.");

			try
			{
				Harmony harmony = new Harmony(PluginGuid);
				harmony.PatchAll();
				Logger.LogInfo("[Free Exhumation] loaded. GUID: " + PluginGuid + " v" + PluginVersion);
			}
			catch (Exception exception)
			{
				Logger.LogError("[Free Exhumation] failed to apply patches: " + exception);
			}
		}

		internal static void LogInfo(string message)
		{
			if (Instance == null)
			{
				return;
			}
			Instance.Logger.LogInfo("[Free Exhumation] " + message);
		}

		internal static void LogWarning(string message)
		{
			if (Instance == null)
			{
				return;
			}
			Instance.Logger.LogWarning("[Free Exhumation] " + message);
		}
	}
}
