using System.Collections.Generic;
using LazyBearTechnology;

namespace FreeExhumation
{
	/// <summary>
	/// Performs the exhumation itself. Vanilla UIGraveWindowData.ExhumeBody() is bypassed because it always
	/// removes one "exhume_certificate" from the player inventory first.
	/// </summary>
	internal static class ExhumeService
	{
		/// <summary>Item group ids used by the grave decorations (tombstone = gravetop, fence = gravebot).</summary>
		private const string GraveTopGroupId = "gravetop";

		private const string GraveBotGroupId = "gravebot";

		/// <summary>
		/// Digs the grave up. Optionally returns the decorations to the player inventory and optionally
		/// consumes a certificate. Mirrors UIGraveWindowData.ExhumeBody() minus the forced certificate cost.
		/// </summary>
		internal static void Exhume(WgoData graveData)
		{
			if (graveData == null)
			{
				FreeExhumationPlugin.LogWarning("Exhume aborted: no grave data.");
				return;
			}

			int removedDecorations = 0;
			if (FreeExhumationPlugin.AutoRemoveDecorations)
			{
				removedDecorations = RemoveDecorations(graveData);
			}

			if (FreeExhumationPlugin.ConsumeCertificate)
			{
				ConsumeCertificate();
			}

			if (FreeExhumationPlugin.VerboseLogging)
			{
				FreeExhumationPlugin.LogInfo("Exhuming grave [" + graveData.id + "], decorations returned: " + removedDecorations);
			}

			MainGame.Instance.GameSave.worldData.ChangeWgoData(graveData, GameConsts.WGOs.EXHUMATION_GRAVE_WGO_ID);

			LazyUI.GetWindow<UIDialogWindow>().Close();
			LazyUI.GetWindow<UIGraveWindow>().Close();
		}

		/// <summary>
		/// Takes the tombstone / fence off the grave and puts them into the player inventory.
		/// Decorations that do not fit are left on the grave (never destroyed).
		/// </summary>
		private static int RemoveDecorations(WgoData graveData)
		{
			int removed = 0;
			Inventory graveInventory = graveData.Inventory;
			Inventory playerInventory = MainGame.PlayerData != null ? MainGame.PlayerData.Inventory : null;
			if (graveInventory == null || graveInventory.Data == null || playerInventory == null)
			{
				return removed;
			}

			List<Item> decorations = new List<Item>();
			foreach (Item item in graveInventory.Data.Inventory)
			{
				if (item == null || item.Count <= 0)
				{
					continue;
				}
				ItemDef definition = item.Definition;
				if (definition == null)
				{
					continue;
				}
				if (definition.itemGroupIds.Contains(GraveTopGroupId) || definition.itemGroupIds.Contains(GraveBotGroupId))
				{
					decorations.Add(item);
				}
			}

			foreach (Item decoration in decorations)
			{
				if (!playerInventory.CanAddItemToInventory(decoration))
				{
					FreeExhumationPlugin.LogWarning("Inventory is full, decoration [" + decoration.id + "] stays on the grave.");
					continue;
				}
				if (!graveInventory.RemoveItemFromInventoryByUID(decoration, -1))
				{
					continue;
				}
				if (!playerInventory.AddItemToInventory(decoration, null, false))
				{
					// Roll back so the decoration is never lost.
					graveInventory.AddItemToInventory(decoration, null, false);
					continue;
				}
				removed++;
			}

			removed += RemoveDecorationParts(graveData);
			return removed;
		}

		/// <summary>
		/// Drops the decoration entries from WgoData.AdditionalWgoPartsData so the parts are not rebuilt by
		/// ChangeWgoData(). Visual parts and inventory items are two parallel datasets in this game.
		/// </summary>
		private static int RemoveDecorationParts(WgoData graveData)
		{
			int removed = 0;
			List<WgoPartData> parts = graveData.AdditionalWgoPartsData;
			if (parts == null)
			{
				return removed;
			}

			for (int i = parts.Count - 1; i >= 0; i--)
			{
				WgoPartData part = parts[i];
				if (part == null || string.IsNullOrEmpty(part.id))
				{
					continue;
				}
				ItemDef definition = GameBalance.Me.GetDataOrNull<ItemDef>(part.id);
				if (definition == null)
				{
					continue;
				}
				if (definition.itemGroupIds.Contains(GraveTopGroupId) || definition.itemGroupIds.Contains(GraveBotGroupId))
				{
					parts.RemoveAt(i);
					removed++;
				}
			}

			return removed;
		}

		private static void ConsumeCertificate()
		{
			Inventory playerInventory = MainGame.PlayerData != null ? MainGame.PlayerData.Inventory : null;
			if (playerInventory == null)
			{
				return;
			}
			if (playerInventory.Data.GetTotalCountInInventory(GameConsts.Items.EXHUME_CERTIFICATE_ITEM, null, false) <= 0)
			{
				return;
			}
			playerInventory.RemoveItemById(GameConsts.Items.EXHUME_CERTIFICATE_ITEM, 1, null, null, false);
		}
	}
}
