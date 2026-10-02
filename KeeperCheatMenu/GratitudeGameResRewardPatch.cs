using System;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x0200002E RID: 46
	[HarmonyPatch(typeof(PlayerData), "AddRes", new Type[] { typeof(GameRes) })]
	internal static class GratitudeGameResRewardPatch
	{
		// Token: 0x0600018C RID: 396 RVA: 0x000181FC File Offset: 0x000163FC
		private static void Prefix(ref GameRes __0)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || __0 == null)
			{
				return;
			}
			float withoutSystemsCheck = __0.GetWithoutSystemsCheck("happiness", 0f);
			if (withoutSystemsCheck <= 0f)
			{
				return;
			}
			float num = Mathf.Clamp(instance.GratitudeMultiplier, 0.1f, 100f);
			if (Mathf.Approximately(num, 1f))
			{
				return;
			}
			GameRes gameRes = new GameRes(__0);
			gameRes.SetWithoutSystemsCheck("happiness", withoutSystemsCheck * num);
			__0 = gameRes;
		}
	}
}
