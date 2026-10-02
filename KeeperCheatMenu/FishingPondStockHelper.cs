using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x02000012 RID: 18
	internal static class FishingPondStockHelper
	{
		// Token: 0x0600014D RID: 333 RVA: 0x00016E34 File Offset: 0x00015034
		internal static int ConfiguredCapacity(FishingDef fishing)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			float num = Mathf.Clamp((instance != null) ? instance.FishPondStockMultiplier : 1f, 1f, 100f);
			return Mathf.Max(0, Mathf.CeilToInt((float)((fishing != null) ? fishing.baseCount : 0) * num));
		}

		// Token: 0x0600014E RID: 334 RVA: 0x00016E80 File Offset: 0x00015080
		internal static void RefillToConfiguredCapacity(Wgo wgo, bool setExact = false)
		{
			WgoData wgoData = ((wgo != null) ? wgo.Data : null);
			if (wgoData != null)
			{
				if (!setExact)
				{
					KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
					if (((instance != null) ? instance.FishPondStockMultiplier : 1f) <= 1f)
					{
						return;
					}
				}
				List<FishingDef> allForReservoir = FishingDef.GetAllForReservoir(wgoData.id);
				if (allForReservoir == null || allForReservoir.Count == 0)
				{
					return;
				}
				foreach (IGrouping<string, FishingDef> grouping in allForReservoir.Where<FishingDef>((FishingDef fishing) => fishing != null && !string.IsNullOrEmpty(fishing.fishId)).GroupBy<FishingDef, string>((FishingDef fishing) => fishing.fishId, StringComparer.Ordinal))
				{
					IEnumerable<FishingDef> enumerable = grouping;
					Func<FishingDef, int> func;
					if ((func = FishingPondStockHelper.__OCache.__f0_ConfiguredCapacity) == null)
					{
						func = (FishingPondStockHelper.__OCache.__f0_ConfiguredCapacity = new Func<FishingDef, int>(FishingPondStockHelper.ConfiguredCapacity));
					}
					int num = enumerable.Max<FishingDef>(func);
					int num2 = Mathf.Max(0, wgoData.GetGameResInt(grouping.Key));
					if (setExact && num2 != num)
					{
						wgoData.SetGameRes(grouping.Key, num);
					}
					else if (num2 < num)
					{
						wgoData.AddGameRes(grouping.Key, num - num2);
					}
				}
				return;
			}
		}

		// Token: 0x0200005A RID: 90
		[CompilerGenerated]
		private static class __OCache
		{
			// Token: 0x0400019C RID: 412
			public static Func<FishingDef, int> __f0_ConfiguredCapacity;
		}
	}
}
