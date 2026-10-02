using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KeeperCheatMenu
{
	// Token: 0x02000018 RID: 24
	internal sealed class FishingOverlayDragHandle : MonoBehaviour, IDragHandler, IEventSystemHandler, IEndDragHandler
	{
		// Token: 0x0600015A RID: 346 RVA: 0x00017214 File Offset: 0x00015414
		public void OnDrag(PointerEventData eventData)
		{
			KeeperCheatMenuPlugin owner = this.Owner;
			if (owner == null)
			{
				return;
			}
			owner.DragFishingOverlay(eventData.delta);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x0001722C File Offset: 0x0001542C
		public void OnEndDrag(PointerEventData eventData)
		{
			KeeperCheatMenuPlugin owner = this.Owner;
			if (owner == null)
			{
				return;
			}
			owner.SaveFishingOverlayPosition();
		}

		// Token: 0x04000110 RID: 272
		internal KeeperCheatMenuPlugin Owner;
	}
}
