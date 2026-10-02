using System;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace FreeExhumation
{
	/// <summary>
	/// UIGraveWindowData.CanExhume() -> false while a tombstone (gravetop) or a fence (gravebot) is installed.
	/// It gates both the exhume button state and the exhumation itself.
	/// </summary>
	[HarmonyPatch(typeof(UIGraveWindowData), "CanExhume")]
	internal static class GraveCanExhumePatch
	{
		private static void Postfix(ref bool __result)
		{
			try
			{
				if (!FreeExhumationPlugin.IgnoreDecorationRequirement)
				{
					return;
				}
				__result = true;
			}
			catch (Exception exception)
			{
				FreeExhumationPlugin.LogWarning("CanExhume patch failed: " + exception.Message);
			}
		}
	}

	/// <summary>
	/// UIGraveWindowData.HasExhumeCertificate() -> whether the player owns an "exhume_certificate".
	/// Only used by TryExhumeBody(); kept patched so any other call site sees the relaxed rule too.
	/// </summary>
	[HarmonyPatch(typeof(UIGraveWindowData), "HasExhumeCertificate")]
	internal static class GraveHasCertificatePatch
	{
		private static void Postfix(ref bool __result)
		{
			try
			{
				if (!FreeExhumationPlugin.IgnoreCertificateRequirement)
				{
					return;
				}
				__result = true;
			}
			catch (Exception exception)
			{
				FreeExhumationPlugin.LogWarning("HasExhumeCertificate patch failed: " + exception.Message);
			}
		}
	}

	/// <summary>
	/// UIGraveWindowData.TryExhumeBody() is the exhume button handler. It is replaced so the confirmation
	/// dialog always offers a working "OK" button and the exhumation runs through ExhumeService (no forced
	/// certificate cost, optional decoration clean-up).
	/// </summary>
	[HarmonyPatch(typeof(UIGraveWindowData), "TryExhumeBody")]
	internal static class GraveTryExhumeBodyPatch
	{
		private static bool Prefix(UIGraveWindowData __instance)
		{
			try
			{
				if (!FreeExhumationPlugin.Enabled)
				{
					return true;
				}

				UICorpseWidgetData corpse = __instance.CorpseWidgetData;
				if (corpse == null || corpse.IsEmpty || corpse.Body == null)
				{
					return false;
				}

				WgoData graveData = __instance.WgoData;
				if (graveData == null)
				{
					return false;
				}

				int ownedCertificates = CertificateCount();

				// Certificate rule kept and the player owns none: let the vanilla dialog explain it.
				if (!FreeExhumationPlugin.IgnoreCertificateRequirement && ownedCertificates <= 0)
				{
					return true;
				}

				if (FreeExhumationPlugin.SkipConfirmationDialog)
				{
					ExhumeService.Exhume(graveData);
					return false;
				}

				// The dialog only shows the counter (hasCount / needCount); it does not block the OK button.
				// Report at least 1 so the player never sees a "0/1" he can still confirm.
				int shownCertificates = FreeExhumationPlugin.IgnoreCertificateRequirement
					? Mathf.Max(ownedCertificates, 1)
					: ownedCertificates;

				UIDialogWindowData dialogData = new UIDialogWindowData(
					new Item(GameConsts.Items.EXHUME_CERTIFICATE_ITEM, 1),
					LLBase.L("exhume"),
					LLBase.L("exhume_confirmation"),
					LLBase.L("exhume_confirmation_bot"),
					shownCertificates,
					1,
					delegate
					{
						ExhumeService.Exhume(graveData);
					},
					delegate
					{
						LazyUI.GetWindow<UIDialogWindow>().Close();
					},
					false);
				dialogData.ShowCloseButton = false;

				LazyUI.GetWindow<UIDialogWindow>().Open(dialogData);
				return false;
			}
			catch (Exception exception)
			{
				FreeExhumationPlugin.LogWarning("TryExhumeBody patch failed, falling back to vanilla: " + exception.Message);
				return true;
			}
		}

		private static int CertificateCount()
		{
			PlayerData playerData = MainGame.PlayerData;
			if (playerData == null || playerData.Inventory == null || playerData.Inventory.Data == null)
			{
				return 0;
			}
			return playerData.Inventory.Data.GetTotalCountInInventory(GameConsts.Items.EXHUME_CERTIFICATE_ITEM, null, false);
		}
	}
}
