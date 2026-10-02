using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x0200002C RID: 44
	[HarmonyPatch(typeof(PlayerData), "CollectDrop")]
	internal static class HarvestYieldDropPatch
	{
		// Token: 0x06000188 RID: 392 RVA: 0x000180A8 File Offset: 0x000162A8
		private static void Prefix(DropView __0)
		{
			try
			{
				KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
				DropData dropData = ((__0 != null) ? __0.Data : null);
				Item item = ((dropData != null) ? dropData.Item : null);
				if (!(instance == null) && dropData != null && item != null && item.Count > 0 && !dropData.IsResDrop && !dropData.IsDroppedFromPlayer && !HarvestYieldDropPatch.ScaledDrops.Contains(dropData))
				{
					float num = Mathf.Clamp(instance.HarvestYieldMultiplier, 0.1f, 100f);
					if (!Mathf.Approximately(num, 1f))
					{
						item.Count = Mathf.Max(1, Mathf.RoundToInt((float)item.Count * num));
						HarvestYieldDropPatch.ScaledDrops.Add(dropData);
					}
				}
			}
			catch
			{
			}
		}

		// Token: 0x06000189 RID: 393 RVA: 0x0001816C File Offset: 0x0001636C
		private static void Postfix(DropView __0)
		{
			DropData dropData = ((__0 != null) ? __0.Data : null);
			if (dropData != null && dropData.Count <= 0)
			{
				HarvestYieldDropPatch.ScaledDrops.Remove(dropData);
			}
		}

		// Token: 0x04000128 RID: 296
		private static readonly HashSet<DropData> ScaledDrops = new HashSet<DropData>();
	}
}
