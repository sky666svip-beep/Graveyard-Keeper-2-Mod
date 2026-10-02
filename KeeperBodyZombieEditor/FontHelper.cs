using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace KeeperBodyZombieEditor
{
	public static class FontHelper
	{
		private static TMP_FontAsset _cachedFont;
		private static TMP_SpriteAsset _cachedSpriteAsset;

		public static void ClearCache()
		{
			_cachedFont = null;
			_cachedSpriteAsset = null;
		}

		public static TMP_FontAsset GetFont()
		{
			if (_cachedFont != null)
			{
				return _cachedFont;
			}

			string lang = ModLocalization.Lang;
			bool isCjk = lang.StartsWith("zh") || lang.StartsWith("ja") || lang.StartsWith("ko");

			// 1. 尝试从游戏已加载的 TMP_FontAsset 寻找带有对应字符或游戏默认字体
			TMP_FontAsset[] loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
			if (loadedFonts != null && loadedFonts.Length > 0)
			{
				if (isCjk)
				{
					// 寻找包含常见中日韩字符的字体
					var cjkFont = loadedFonts.FirstOrDefault(f => f != null && f.HasCharacter('中', false, false) && f.HasCharacter('文', false, false));
					if (cjkFont != null)
					{
						_cachedFont = cjkFont;
						return _cachedFont;
					}
				}
				else
				{
					// 西欧/英文/俄语等优先使用游戏原版 small_font_bold
					var gameFont = loadedFonts.FirstOrDefault(f => f != null && f.name.IndexOf("small_font", StringComparison.OrdinalIgnoreCase) >= 0);
					if (gameFont != null)
					{
						_cachedFont = gameFont;
						return _cachedFont;
					}
				}

				// 通用回退已加载字体
				_cachedFont = loadedFonts.FirstOrDefault(f => f != null);
			}

			// 2. 如果当前字体是 CJK 但不支持中文，尝试补充系统字体作为 Fallback
			if (isCjk && (_cachedFont == null || !_cachedFont.HasCharacter('中', false, false)))
			{
				try
				{
					string windir = Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows";
					string[] fontCandidates = new string[]
					{
						Path.Combine(windir, "Fonts", "msyh.ttc"),
						Path.Combine(windir, "Fonts", "msyh.ttf"),
						Path.Combine(windir, "Fonts", "simhei.ttf"),
						Path.Combine(windir, "Fonts", "simsun.ttc")
					};

					foreach (string fontPath in fontCandidates)
					{
						if (File.Exists(fontPath))
						{
							try
							{
								var sysFont = TMP_FontAsset.CreateFontAsset(fontPath, 0, 36, 4, GlyphRenderMode.SDFAA, 1024, 1024);
								if (sysFont != null)
								{
									sysFont.name = "BodyZombieEditor_SysFont";
									sysFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
									sysFont.isMultiAtlasTexturesEnabled = true;

									if (_cachedFont != null)
									{
										_cachedFont.fallbackFontAssetTable.Add(sysFont);
									}
									else
									{
										_cachedFont = sysFont;
									}
									break;
								}
							}
							catch { }
						}
					}
				}
				catch { }
			}

			return _cachedFont;
		}

		public static TMP_SpriteAsset GetSpriteAsset()
		{
			if (_cachedSpriteAsset != null)
			{
				return _cachedSpriteAsset;
			}

			// 优先获取游戏中自带的 SpriteAsset（包含骷髅、科技点等贴图）
			TMP_SpriteAsset[] sprites = Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>();
			if (sprites != null && sprites.Length > 0)
			{
				// 寻找包含 skull 或 tech 或 icon 的 sprite 表
				var targetSprite = sprites.FirstOrDefault(s => s != null && (s.name.IndexOf("skull", StringComparison.OrdinalIgnoreCase) >= 0 || s.name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0));
				if (targetSprite != null)
				{
					_cachedSpriteAsset = targetSprite;
					return _cachedSpriteAsset;
				}

				_cachedSpriteAsset = sprites.FirstOrDefault(s => s != null);
				if (_cachedSpriteAsset != null)
				{
					return _cachedSpriteAsset;
				}
			}

			// 回退到 TMP_Settings 默认资源
			_cachedSpriteAsset = TMP_Settings.defaultSpriteAsset;
			return _cachedSpriteAsset;
		}
	}
}
