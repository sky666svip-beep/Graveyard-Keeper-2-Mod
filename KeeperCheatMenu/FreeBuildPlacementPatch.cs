using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x0200001B RID: 27
	[HarmonyPatch(typeof(BuildController), "UpdatePointerAtPos")]
	internal static class FreeBuildPlacementPatch
	{
		// Token: 0x06000161 RID: 353 RVA: 0x000172A0 File Offset: 0x000154A0
		[HarmonyPostfix]
		private static void ApplyUnsnappedPosition(BuildController __instance, Vector3 pos)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || !instance.FreeBuildEnabled || __instance == null)
			{
				return;
			}
			RaycastHit raycastHit;
			if (!Physics.Raycast(CameraSystem.ScreenPointToRay(pos), out raycastHit, 100f, 2048))
			{
				return;
			}
			Vector3 vector = raycastHit.point;
			FieldInfo currentWorldZoneField = FreeBuildPlacementPatch.CurrentWorldZoneField;
			WorldZone worldZone = ((currentWorldZoneField != null) ? currentWorldZoneField.GetValue(__instance) : null) as WorldZone;
			if (worldZone != null)
			{
				vector = VisualConsts.ProjectElevationPointToGround(vector, worldZone.GroundPlaneY);
			}
			vector += Vector3.up * 0.006f;
			float num;
			if (worldZone != null && worldZone.TryGetBuildElevationY(vector.x, vector.z, out num))
			{
				vector = VisualConsts.ProjectGroundPointToElevation(vector, num);
			}
			__instance.UpdatePointerObjectPosition(vector);
		}

		// Token: 0x04000111 RID: 273
		private static readonly FieldInfo CurrentWorldZoneField = AccessTools.Field(typeof(BuildController), "currentWorldZone");
	}
}
