using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000031 RID: 49
	[HarmonyPatch(typeof(EnergySystem), "StartSleeping")]
	internal static class SleepAlwaysPatch
	{
		// Token: 0x06000191 RID: 401 RVA: 0x0001838B File Offset: 0x0001658B
		[HarmonyPrefix]
		private static void ConfigureCheatSleep(ref bool sleepWithoutSavingGame, ref bool sleepWithMaxEnergy, ref float sleepDuration)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance != null && instance.SleepAlwaysEnabled)
			{
				sleepWithMaxEnergy = true;
				sleepDuration = float.PositiveInfinity;
			}
			if (instance != null && instance.SleepWithoutSavingEnabled)
			{
				sleepWithoutSavingGame = true;
			}
		}
	}
}
