using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KeeperCheatMenu
{
	// Token: 0x02000020 RID: 32
	internal sealed class LegacyCjkTextMirror : MonoBehaviour
	{
		// Token: 0x0600016C RID: 364 RVA: 0x000176EC File Offset: 0x000158EC
		internal static void Attach(TextMeshProUGUI source, Font font)
		{
			if (source == null || font == null)
			{
				return;
			}
			GameObject gameObject = new GameObject("KoreanLegacyText", new Type[]
			{
				typeof(RectTransform),
				typeof(Text)
			});
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.SetParent(source.transform, false);
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Text component2 = gameObject.GetComponent<Text>();
			component2.font = font;
			component2.raycastTarget = false;
			component2.horizontalOverflow = HorizontalWrapMode.Wrap;
			component2.verticalOverflow = VerticalWrapMode.Overflow;
			component2.supportRichText = source.richText;
			LegacyCjkTextMirror legacyCjkTextMirror = gameObject.AddComponent<LegacyCjkTextMirror>();
			legacyCjkTextMirror._source = source;
			legacyCjkTextMirror._target = component2;
			legacyCjkTextMirror.Sync();
			source.enabled = false;
		}

		// Token: 0x0600016D RID: 365 RVA: 0x000177C2 File Offset: 0x000159C2
		private void LateUpdate()
		{
			this.Sync();
		}

		// Token: 0x0600016E RID: 366 RVA: 0x000177CC File Offset: 0x000159CC
		private void Sync()
		{
			if (this._source == null || this._target == null)
			{
				return;
			}
			this._target.text = this._source.text;
			this._target.color = this._source.color;
			this._target.fontSize = Mathf.Max(1, Mathf.RoundToInt(this._source.fontSize));
			FontStyles sourceStyle = this._source.fontStyle;
			bool bold = (sourceStyle & FontStyles.Bold) != 0;
			bool italic = (sourceStyle & FontStyles.Italic) != 0;
			this._target.fontStyle = (bold ? (italic ? FontStyle.BoldAndItalic : FontStyle.Bold) : (italic ? FontStyle.Italic : FontStyle.Normal));
			this._target.alignment = LegacyCjkTextMirror.ToLegacyAlignment(this._source.alignment);
		}

		// Token: 0x0600016F RID: 367 RVA: 0x000178A0 File Offset: 0x00015AA0
		private static TextAnchor ToLegacyAlignment(TextAlignmentOptions alignment)
		{
			if (alignment == (TextAlignmentOptions)257)
			{
				return TextAnchor.UpperLeft;
			}
			if (alignment == (TextAlignmentOptions)258)
			{
				return TextAnchor.UpperCenter;
			}
			if (alignment == (TextAlignmentOptions)260)
			{
				return TextAnchor.UpperRight;
			}
			if (alignment == (TextAlignmentOptions)4097 || alignment == (TextAlignmentOptions)513)
			{
				return TextAnchor.MiddleLeft;
			}
			if (alignment == (TextAlignmentOptions)4100 || alignment == (TextAlignmentOptions)516)
			{
				return TextAnchor.MiddleRight;
			}
			if (alignment == (TextAlignmentOptions)1025)
			{
				return TextAnchor.LowerLeft;
			}
			if (alignment == (TextAlignmentOptions)1028)
			{
				return TextAnchor.LowerRight;
			}
			if (alignment == (TextAlignmentOptions)1026)
			{
				return TextAnchor.LowerCenter;
			}
			return TextAnchor.MiddleCenter;
		}

		// Token: 0x04000117 RID: 279
		private TextMeshProUGUI _source;

		// Token: 0x04000118 RID: 280
		private Text _target;
	}
}
