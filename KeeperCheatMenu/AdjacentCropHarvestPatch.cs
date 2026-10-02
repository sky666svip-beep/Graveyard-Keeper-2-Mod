using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x02000004 RID: 4
	[HarmonyPatch(typeof(WgoData), "RunLogicsAfterDeath")]
	internal static class AdjacentCropHarvestPatch
	{
		// Token: 0x06000003 RID: 3 RVA: 0x00002067 File Offset: 0x00000267
		internal static void RunDeath(WgoData data)
		{
			AdjacentCropHarvestPatch.RunDeathMethod.Invoke(data, null);
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002078 File Offset: 0x00000278
		private static void Prefix(WgoData __instance, out AdjacentCropHarvestPatch.HarvestState __state)
		{
			__state = null;
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (AdjacentCropHarvestPatch.Suppress || instance == null || !instance.HarvestAdjacentCropsEnabled || __instance == null)
			{
				return;
			}
			try
			{
				string text;
				if (GardenBedNavigation.IsGardenPlot(__instance) && GardenTabletWorldIconLogic.TryGetCropIdFromPlotWgoId(__instance.id, out text))
				{
					__state = new AdjacentCropHarvestPatch.HarvestState
					{
						Zone = __instance.WorldZoneData,
						Bounds = GardenBedNavigation.GetWorldOccupancyRect(__instance),
						CropId = text,
						PlotId = __instance.id,
						OriginKey = __instance.UniqueId.ToString()
					};
					instance.LogCropHarvestTrace(string.Concat(new string[] { "Harvested garden object '", __instance.id, "' detected for crop '", text, "'." }));
				}
			}
			catch (Exception ex)
			{
				instance.LogCropHarvestWarning("Could not inspect the harvested crop", ex);
			}
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002164 File Offset: 0x00000364
		private static void Postfix(AdjacentCropHarvestPatch.HarvestState __state)
		{
			if (__state == null)
			{
				return;
			}
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || !instance.HarvestAdjacentCropsEnabled)
			{
				return;
			}
			try
			{
				instance.HarvestAdjacentReadyCrops(__state.Zone, __state.Bounds, __state.OriginKey, __state.PlotId, __state.CropId);
			}
			catch (Exception ex)
			{
				instance.LogCropHarvestWarning("Could not harvest adjacent crops", ex);
			}
		}

		// Token: 0x04000002 RID: 2
		[ThreadStatic]
		internal static bool Suppress;

		// Token: 0x04000003 RID: 3
		private static readonly MethodInfo RunDeathMethod = AccessTools.Method(typeof(WgoData), "RunLogicsAfterDeath", null, null);

		// Token: 0x0200003A RID: 58
		private sealed class HarvestState
		{
			// Token: 0x04000133 RID: 307
			public WorldZoneData Zone;

			// Token: 0x04000134 RID: 308
			public Rect Bounds;

			// Token: 0x04000135 RID: 309
			public string CropId;

			// Token: 0x04000136 RID: 310
			public string PlotId;

			// Token: 0x04000137 RID: 311
			public string OriginKey;
		}
	}
}
