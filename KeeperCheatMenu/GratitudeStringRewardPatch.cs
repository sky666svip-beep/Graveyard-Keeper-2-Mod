using System;
using HarmonyLib;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x0200002D RID: 45
	[HarmonyPatch(typeof(PlayerData), "AddRes", new Type[]
	{
		typeof(string),
		typeof(float)
	})]
	internal static class GratitudeStringRewardPatch
	{
		// Token: 0x0600018B RID: 395 RVA: 0x000181AC File Offset: 0x000163AC
		private static void Prefix(string __0, ref float __1)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || __1 <= 0f || !string.Equals(__0, "happiness", StringComparison.Ordinal))
			{
				return;
			}
			__1 *= Mathf.Clamp(instance.GratitudeMultiplier, 0.1f, 100f);
		}
	}
}
