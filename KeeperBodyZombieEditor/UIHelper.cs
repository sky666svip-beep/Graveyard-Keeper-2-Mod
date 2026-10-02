using System;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KeeperBodyZombieEditor
{
	public class UIDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
	{
		private RectTransform _targetRect;
		private Vector2 _pointerOffset;

		public void Init(RectTransform targetRect)
		{
			_targetRect = targetRect;
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (_targetRect == null) return;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				_targetRect.parent as RectTransform,
				eventData.position,
				eventData.pressEventCamera,
				out Vector2 localPoint
			);
			_pointerOffset = _targetRect.anchoredPosition - localPoint;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (_targetRect == null) return;
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
				_targetRect.parent as RectTransform,
				eventData.position,
				eventData.pressEventCamera,
				out Vector2 localPoint))
			{
				_targetRect.anchoredPosition = localPoint + _pointerOffset;
			}
		}
	}

	public static class UIHelper
	{
		public static readonly Color ColorPanelBg = new Color(0.10f, 0.11f, 0.14f, 0.98f);
		public static readonly Color ColorHeaderBg = new Color(0.16f, 0.17f, 0.22f, 1f);
		public static readonly Color ColorBorder = new Color(0.55f, 0.46f, 0.32f, 1f);
		public static readonly Color ColorInnerBorder = new Color(0.25f, 0.23f, 0.20f, 1f);
		public static readonly Color ColorTextGold = new Color(0.98f, 0.76f, 0.42f, 1f);
		public static readonly Color ColorTextPale = new Color(0.92f, 0.88f, 0.82f, 1f);
		public static readonly Color ColorTextMuted = new Color(0.68f, 0.65f, 0.60f, 1f);

		public static readonly Color ColorBtnNormal = new Color(0.50f, 0.12f, 0.14f, 1f);
		public static readonly Color ColorBtnHover = new Color(0.68f, 0.18f, 0.20f, 1f);
		public static readonly Color ColorBtnPressed = new Color(0.35f, 0.08f, 0.10f, 1f);

		public static readonly Color ColorBtnSecNormal = new Color(0.22f, 0.24f, 0.28f, 1f);
		public static readonly Color ColorBtnSecHover = new Color(0.32f, 0.35f, 0.40f, 1f);

		public static readonly Color ColorInputBg = new Color(0.06f, 0.07f, 0.09f, 1f);

		public static T GetWidgetData<T>(this LazyWidget<T> widget) where T : LazyWidgetDataBase
		{
			if (widget == null) return null;
			return HarmonyLib.AccessTools.Field(typeof(LazyWidget<T>), "data")?.GetValue(widget) as T;
		}

		public static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
		{
			GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
			RectTransform rt = go.GetComponent<RectTransform>();
			rt.SetParent(parent, false);
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;
			rt.offsetMin = offsetMin;
			rt.offsetMax = offsetMax;
			Image img = go.GetComponent<Image>();
			img.color = color;
			img.raycastTarget = color.a > 0.01f;
			return rt;
		}

		public static TextMeshProUGUI CreateText(string text, Transform parent, float fontSize, Color color, TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
		{
			GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
			RectTransform rt = go.GetComponent<RectTransform>();
			rt.SetParent(parent, false);
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;
			rt.offsetMin = offsetMin;
			rt.offsetMax = offsetMax;

			TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
			tmp.font = FontHelper.GetFont();
			tmp.spriteAsset = FontHelper.GetSpriteAsset();
			tmp.text = text;
			tmp.fontSize = fontSize;
			tmp.color = color;
			tmp.alignment = align;
			tmp.raycastTarget = false;
			tmp.textWrappingMode = TextWrappingModes.NoWrap;
			return tmp;
		}

		public static Button CreateButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Action onClick, float fontSize = 14f, bool isSecondary = false)
		{
			Color normal = isSecondary ? ColorBtnSecNormal : ColorBtnNormal;
			Color hover = isSecondary ? ColorBtnSecHover : ColorBtnHover;
			Color pressed = isSecondary ? ColorBtnSecNormal * 0.8f : ColorBtnPressed;

			RectTransform rt = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax, normal);
			Image img = rt.GetComponent<Image>();
			Button btn = rt.gameObject.AddComponent<Button>();
			btn.targetGraphic = img;

			ColorBlock cb = btn.colors;
			cb.normalColor = normal;
			cb.highlightedColor = hover;
			cb.pressedColor = pressed;
			cb.selectedColor = hover;
			btn.colors = cb;

			if (onClick != null)
			{
				btn.onClick.AddListener(new UnityAction(onClick));
			}

			CreateText(label, rt, fontSize, ColorTextGold, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-4f, -2f));
			return btn;
		}

		public static void SetButtonText(Button btn, string text)
		{
			if (btn == null) return;
			TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
			if (tmp != null)
			{
				tmp.text = text;
			}
		}

		public static TMP_InputField CreateInputField(string name, Transform parent, string initialValue, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, float fontSize = 14f)
		{
			RectTransform rt = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax, ColorBorder);
			Image outerImg = rt.GetComponent<Image>();

			RectTransform bg = CreateRect("Bg", rt, Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f), ColorInputBg);
			bg.GetComponent<Image>().raycastTarget = false;

			RectTransform viewport = CreateRect("Viewport", rt, Vector2.zero, Vector2.one, new Vector2(6f, 2f), new Vector2(-6f, -2f), Color.clear);
			viewport.GetComponent<Image>().raycastTarget = false;
			viewport.gameObject.AddComponent<RectMask2D>();

			TextMeshProUGUI textComp = CreateText(initialValue, viewport, fontSize, ColorTextPale, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
			textComp.raycastTarget = true;

			TMP_InputField input = rt.gameObject.AddComponent<TMP_InputField>();
			input.targetGraphic = outerImg;
			input.textViewport = viewport;
			input.textComponent = textComp;
			input.text = initialValue;
			input.caretColor = ColorTextGold;
			input.caretWidth = 2;
			input.customCaretColor = true;
			input.lineType = TMP_InputField.LineType.SingleLine;
			input.characterValidation = TMP_InputField.CharacterValidation.Integer;
			return input;
		}
	}
}
