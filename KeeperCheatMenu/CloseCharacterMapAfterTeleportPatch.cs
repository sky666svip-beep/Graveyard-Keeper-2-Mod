using System;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x02000037 RID: 55
	[HarmonyPatch(typeof(PlayerController), "Teleport", new Type[] { typeof(TeleportDataBase) })]
	internal static class CloseCharacterMapAfterTeleportPatch
	{
		// Token: 0x06000197 RID: 407 RVA: 0x0001841C File Offset: 0x0001661C
		[HarmonyPostfix]
		private static void CloseMapAfterSuccessfulTeleport(bool __result)
		{
			if (__result)
			{
				KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
				if (instance != null && instance.MapTeleportEverywhereEnabled)
				{
					try
					{
						CharacterWindow window = LazyUI.GetWindow<CharacterWindow>();
						if (window != null && window.IsShown && window.LastOpenedPage == (CharacterWindowData.CharPage)5)
						{
							window.Close();
						}
					}
					catch (Exception ex)
					{
						Debug.LogWarning("[Keeper Cheat Menu] Could not close the character map after teleport: " + ex.Message);
					}
					return;
				}
			}
		}
	}
}
