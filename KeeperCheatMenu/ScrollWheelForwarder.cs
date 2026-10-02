using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KeeperCheatMenu
{
	// Token: 0x02000030 RID: 48
	internal sealed class ScrollWheelForwarder : UIBehaviour, IScrollHandler, IEventSystemHandler
	{
		// Token: 0x0600018F RID: 399 RVA: 0x0001834C File Offset: 0x0001654C
		public void OnScroll(PointerEventData eventData)
		{
			if (this.Target == null || !this.Target.IsActive() || !this.Target.vertical)
			{
				return;
			}
			this.Target.OnScroll(eventData);
		}

		// Token: 0x0400012E RID: 302
		internal ScrollRect Target;
	}
}
