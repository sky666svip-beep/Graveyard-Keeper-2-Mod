using System;
using UnityEngine;
using UnityEngine.UI;

namespace KeeperCheatMenu
{
	// Token: 0x0200002F RID: 47
	internal sealed class ScrollbarArtworkFollower : MonoBehaviour
	{
		// Token: 0x0600018D RID: 397 RVA: 0x0001827C File Offset: 0x0001647C
		private void LateUpdate()
		{
			if (this.Target == null || this.Track == null || this.Artwork == null)
			{
				return;
			}
			float num = this.Artwork.rect.height * 0.5f;
			float num2 = this.BottomInset + num;
			float num3 = this.Track.rect.height - this.TopInset - num;
			float num4 = ((num3 <= num2) ? (this.Track.rect.height * 0.5f) : Mathf.Lerp(num2, num3, Mathf.Clamp01(this.Target.verticalNormalizedPosition)));
			this.Artwork.anchoredPosition = new Vector2(0f, num4);
		}

		// Token: 0x04000129 RID: 297
		internal ScrollRect Target;

		// Token: 0x0400012A RID: 298
		internal RectTransform Track;

		// Token: 0x0400012B RID: 299
		internal RectTransform Artwork;

		// Token: 0x0400012C RID: 300
		internal float TopInset;

		// Token: 0x0400012D RID: 301
		internal float BottomInset;
	}
}
