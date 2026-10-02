using System;
using System.Collections.Generic;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000008 RID: 8
	[HarmonyPatch(typeof(UIAlchemyFormulaWidget), "Redraw")]
	internal static class AlchemyFolioIngredientClickPatch
	{
		// Token: 0x06000140 RID: 320 RVA: 0x00016B80 File Offset: 0x00014D80
		[HarmonyPostfix]
		private static void MakeFolioIngredientClickable(UIItemCell ___itemCell)
		{
			if (___itemCell == null)
			{
				return;
			}
			Action<UIItemCell> original;
			if (!AlchemyFolioIngredientClickPatch.OriginalCallbacks.TryGetValue(___itemCell, out original))
			{
				original = ___itemCell.OnItemCellPress;
				AlchemyFolioIngredientClickPatch.OriginalCallbacks[___itemCell] = original;
			}
			___itemCell.OnItemCellPress = delegate(UIItemCell clicked)
			{
				KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
				if (instance != null && instance.TrySelectAlchemyIngredientFromFolio((clicked != null) ? clicked.DisplayingItem : null))
				{
					return;
				}
				Action<UIItemCell> original2 = original;
				if (original2 == null)
				{
					return;
				}
				original2(clicked);
			};
		}

		// Token: 0x0400010A RID: 266
		private static readonly Dictionary<UIItemCell, Action<UIItemCell>> OriginalCallbacks = new Dictionary<UIItemCell, Action<UIItemCell>>();
	}
}
