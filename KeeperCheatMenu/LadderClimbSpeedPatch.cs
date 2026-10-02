using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x0200001F RID: 31
	[HarmonyPatch(typeof(LadderClimbController), "StartClimb")]
	internal static class LadderClimbSpeedPatch
	{
		// Token: 0x0600016A RID: 362 RVA: 0x0001762C File Offset: 0x0001582C
		[HarmonyPostfix]
		private static void ApplyMultiplier(LadderClimbController __instance)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			float num = Mathf.Clamp((instance != null) ? instance.LadderClimbSpeedMultiplier : 1f, 0.1f, 20f);
			if (__instance == null || LadderClimbSpeedPatch.SpeedMultiplierField == null)
			{
				return;
			}
			LadderClimbSpeedPatch.OriginalSpeed originalSpeed;
			if (!LadderClimbSpeedPatch.OriginalSpeeds.TryGetValue(__instance, out originalSpeed))
			{
				originalSpeed = new LadderClimbSpeedPatch.OriginalSpeed
				{
					Value = (float)LadderClimbSpeedPatch.SpeedMultiplierField.GetValue(__instance)
				};
				LadderClimbSpeedPatch.OriginalSpeeds.Add(__instance, originalSpeed);
			}
			LadderClimbSpeedPatch.SpeedMultiplierField.SetValue(__instance, originalSpeed.Value * num);
		}

		// Token: 0x04000115 RID: 277
		private static readonly FieldInfo SpeedMultiplierField = AccessTools.Field(typeof(LadderClimbController), "speedMultiplier");

		// Token: 0x04000116 RID: 278
		private static readonly ConditionalWeakTable<LadderClimbController, LadderClimbSpeedPatch.OriginalSpeed> OriginalSpeeds = new ConditionalWeakTable<LadderClimbController, LadderClimbSpeedPatch.OriginalSpeed>();

		// Token: 0x0200005E RID: 94
		private sealed class OriginalSpeed
		{
			// Token: 0x040001A3 RID: 419
			internal float Value;
		}
	}
}
