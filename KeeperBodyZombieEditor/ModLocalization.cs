using System;
using LazyBearTechnology;

namespace KeeperBodyZombieEditor
{
	public static class ModLocalization
	{
		public static string Lang
		{
			get
			{
				try
				{
					// 1. 优先读取 LLBase 当前真实加载的语言 ID (如 "zh_cn", "en", "ru", "de", "fr", "es", "ja", "ko", "pt-br")
					string current = LLBase.CurrentLang;
					if (!string.IsNullOrEmpty(current))
					{
						return current.ToLowerInvariant().Replace("-", "_");
					}
				}
				catch { }

				try
				{
					// 2. 备用读取 GameSettings.Instance.language
					string gameLang = GameSettings.Instance?.language;
					if (!string.IsNullOrEmpty(gameLang))
					{
						return gameLang.ToLowerInvariant().Replace("-", "_");
					}
				}
				catch { }

				return "en";
			}
		}

		/// <summary>
		/// 清除游戏原生本地化文本中的排版标签与零宽空格，使之更适合按钮和标签展示
		/// </summary>
		public static string Clean(string text)
		{
			if (string.IsNullOrEmpty(text)) return string.Empty;
			return text.Replace("<nobr>", "")
			           .Replace("</nobr>", "")
			           .Replace("\u200b", "")
			           .Trim();
		}

		// 游戏原生词条 (完全走 LLBase.L，随当前加载的语言动态翻译)
		public static string NativeBody => Clean(LLBase.L("ui_grave_corpse_widget_header")); // 尸体 / Body / Тело
		public static string NativeZombie => Clean(LLBase.L("body_zombie"));               // 僵尸 / Zombie / Зомби
		public static string NativeApply => Clean(LLBase.L("ui_apply"));                   // 应用 / Apply / Применить
		public static string NativeReset => Clean(LLBase.L("hint_reset"));                 // 重置 / Reset / Zurücksetzen

		// 原生字形图标 (与游戏 TextMeshPro 图标集对应)
		public static string IconWhiteSkullZombie => "<sprite name=\"skull-zombie_window\">";
		public static string IconRedSkullZombie => "<sprite name=\"rskull-zombie_window\">";
		public static string IconWhiteSkullBody => "<sprite name=\"skull\">";
		public static string IconRedSkullBody => "<sprite name=\"rskull\">";
		public static string IconTechRed => "<sprite name=\"tech_red\">";
		public static string IconTechGreen => "<sprite name=\"tech_green\">";
		public static string IconTechBlue => "<sprite name=\"tech_blue\">";

		// 全局编辑器文本
		public static string EditorTitle
		{
			get
			{
				string lang = Lang;
				if (lang == "zh_cn" || lang == "zh_cht") return "尸体与僵尸工人修改器";
				if (lang == "ru") return "Редактор тел и зомби";
				if (lang == "de") return "Körper- und Zombie-Editor";
				if (lang == "fr") return "Éditeur de cadavres et de zombies";
				if (lang == "es") return "Editor de cadáveres y zombis";
				if (lang == "ja") return "死体＆ゾンビ作業員エディター";
				if (lang == "ko") return "시체 및 좀비 일꾼 편집기";
				if (lang == "pt_br" || lang == "pt") return "Editor de Corpos e Zumbis";
				if (lang == "pl") return "Edytor ciał i zombie";
				return "Body & Zombie Worker Editor";
			}
		}

		public static string HotkeyNotice
		{
			get
			{
				string lang = Lang;
				string key = KeeperBodyZombieEditorPlugin.ToggleKeyEntry?.Value.ToString() ?? "F8";
				if (lang == "zh_cn" || lang == "zh_cht") return $"快捷键 [{key}] 开启/关闭 | 鼠标拖拽标题栏";
				if (lang == "ru") return $"[{key}] Открыть/закрыть | Перетащите за заголовок";
				if (lang == "de") return $"[{key}] Umschalten | Titelleiste ziehen";
				if (lang == "fr") return $"[{key}] Afficher/Masquer | Glisser la barre";
				if (lang == "es") return $"[{key}] Alternar | Arrastrar para mover";
				if (lang == "ja") return $"[{key}] 表示切替 | バーをドラッグして移動";
				if (lang == "ko") return $"[{key}] 열기/닫기 | 제목 표시줄 드래그로 이동";
				return $"[{key}] Toggle | Drag title bar to move";
			}
		}

		public static string HotkeyButton(string key)
		{
			string lang = Lang;
			if (lang == "zh_cn" || lang == "zh_cht") return $"热键: {key}";
			if (lang == "ru") return $"Клавиша: {key}";
			if (lang == "de") return $"Taste: {key}";
			if (lang == "fr") return $"Touche: {key}";
			if (lang == "es") return $"Tecla: {key}";
			if (lang == "ja") return $"キー: {key}";
			if (lang == "ko") return $"단축키: {key}";
			return $"Hotkey: {key}";
		}

		public static string HotkeyPressKey
		{
			get
			{
				string lang = Lang;
				if (lang == "zh_cn" || lang == "zh_cht") return "按任意键...";
				if (lang == "ru") return "Нажмите...";
				if (lang == "de") return "Taste drücken...";
				if (lang == "fr") return "Touche...";
				if (lang == "es") return "Presiona...";
				if (lang == "ja") return "キー入力...";
				if (lang == "ko") return "키 누름...";
				return "Press key...";
			}
		}

		public static string StatusHotkeyUpdated(string key)
		{
			string lang = Lang;
			if (lang == "zh_cn" || lang == "zh_cht") return $"快捷键已更改为: {key}";
			if (lang == "ru") return $"Горячая клавиша изменена на: {key}";
			if (lang == "de") return $"Hotkey geändert zu: {key}";
			if (lang == "fr") return $"Raccourci changé en: {key}";
			if (lang == "es") return $"Tecla cambiada a: {key}";
			return $"Hotkey set to: {key}";
		}

		public static string StatusHotkeyCancelled
		{
			get
			{
				string lang = Lang;
				if (lang == "zh_cn" || lang == "zh_cht") return "已取消快捷键修改";
				if (lang == "ru") return "Отмена изменения клавиши";
				return "Hotkey change cancelled";
			}
		}

		public static string TargetPrefixAutopsy => Clean(LLBase.L("ui_grave_corpse_widget_header"));
		public static string TargetPrefixZombie => Clean(LLBase.L("body_zombie"));

		public static string TargetPrefixCarried
		{
			get
			{
				string lang = Lang;
				if (lang == "zh_cn" || lang == "zh_cht") return "搬运中的目标";
				if (lang == "ru") return "Переносимое тело/зомби";
				if (lang == "de") return "Getragenes Objekt";
				if (lang == "fr") return "Cadavre transporté";
				if (lang == "es") return "Objet transportado";
				return "Carried Body/Zombie";
			}
		}

		public static string TargetPrefixWorldZombie
		{
			get
			{
				string lang = Lang;
				if (lang == "zh_cn" || lang == "zh_cht") return "世界僵尸工人";
				if (lang == "ru") return "Зомби в мире";
				if (lang == "de") return "Zombie-Arbeiter in der Welt";
				if (lang == "fr") return "Ouvrier zombie du monde";
				if (lang == "es") return "Zombi en el mundo";
				return "World Zombie Worker";
			}
		}

		public static string PrevZombie
		{
			get
			{
				string lang = Lang;
				if (lang == "zh_cn" || lang == "zh_cht") return "< 上一只";
				if (lang == "ru") return "< Пред.";
				if (lang == "de") return "< Vorherige";
				if (lang == "fr") return "< Préc.";
				if (lang == "es") return "< Anterior";
				if (lang == "ja") return "< 前へ";
				if (lang == "ko") return "< 이전";
				return "< Prev";
			}
		}

		public static string NextZombie
		{
			get
			{
				string lang = Lang;
				if (lang == "zh_cn" || lang == "zh_cht") return "下一只 >";
				if (lang == "ru") return "След. >";
				if (lang == "de") return "Nächste >";
				if (lang == "fr") return "Suiv. >";
				if (lang == "es") return "Siguiente >";
				if (lang == "ja") return "次へ >";
				if (lang == "ko") return "다음 >";
				return "Next >";
			}
		}

		public static string NoZombiesInWorld
		{
			get
			{
				string lang = Lang;
				if (lang == "zh_cn" || lang == "zh_cht") return "当前存档尚未创建任何僵尸工人";
				if (lang == "ru") return "В мире нет рабочих зомби";
				if (lang == "de") return "Keine Zombie-Arbeiter in der Welt gefunden";
				if (lang == "fr") return "Aucun ouvrier zombie trouvé dans le monde";
				if (lang == "es") return "No hay trabajadores zombis en el mundo";
				return "No zombie workers found in world";
			}
		}

		// 按钮文案
		public static string BtnApplySkulls
		{
			get
			{
				string lang = Lang;
				string apply = NativeApply;
				if (lang == "zh_cn" || lang == "zh_cht") return "应用骷髅";
				if (lang == "ru") return "Применить черепа";
				if (lang == "de") return "Schädel anwenden";
				if (lang == "fr") return "Appliquer crânes";
				if (lang == "es") return "Aplicar calaveras";
				if (lang == "ja") return "ドクロを適用";
				if (lang == "ko") return "해골 적용";
				return "Apply Skulls";
			}
		}

		public static string BtnResetSkulls
		{
			get
			{
				string lang = Lang;
				if (lang == "zh_cn" || lang == "zh_cht") return "还原器官值";
				if (lang == "ru") return "Восстановить";
				if (lang == "de") return "Standard wiederherstellen";
				if (lang == "fr") return "Restaurer";
				if (lang == "es") return "Restaurar";
				if (lang == "ja") return "初期値に戻す";
				if (lang == "ko") return "기본값 복원";
				return "Restore Default";
			}
		}

		public static string BtnApplyTechCurrent
		{
			get
			{
				string lang = Lang;
				if (lang == "zh_cn" || lang == "zh_cht") return "应用当前科技点";
				if (lang == "ru") return "Применить тек. очки";
				if (lang == "de") return "Aktuelle Tech anwenden";
				if (lang == "fr") return "Appliquer points actuels";
				if (lang == "es") return "Aplicar puntos actuales";
				if (lang == "ja") return "現在のポイントを適用";
				if (lang == "ko") return "현재 포인트 적용";
				return "Apply Current Tech";
			}
		}

		public static string BtnApplyTechAll
		{
			get
			{
				string lang = Lang;
				if (lang == "zh_cn" || lang == "zh_cht") return "应用至所有僵尸";
				if (lang == "ru") return "Применить ко всем";
				if (lang == "de") return "Auf alle anwenden";
				if (lang == "fr") return "Appliquer à tous";
				if (lang == "es") return "Aplicar a todos";
				if (lang == "ja") return "全ゾンビに適用";
				if (lang == "ko") return "모든 좀비에 적용";
				return "Apply to All Zombies";
			}
		}

		// 提示信息
		public static string StatusSetSkullsSuccess(int white, int red, bool isZombieWindow)
		{
			string iconW = isZombieWindow ? IconWhiteSkullZombie : IconWhiteSkullBody;
			string iconR = isZombieWindow ? IconRedSkullZombie : IconRedSkullBody;
			string lang = Lang;
			if (lang == "zh_cn" || lang == "zh_cht")
				return $"已更新: {iconW} {white}   {iconR} {red}";
			if (lang == "ru")
				return $"Обновлено: {iconW} {white}   {iconR} {red}";
			if (lang == "de")
				return $"Aktualisiert: {iconW} {white}   {iconR} {red}";
			if (lang == "fr")
				return $"Mis à jour: {iconW} {white}   {iconR} {red}";
			if (lang == "es")
				return $"Actualizado: {iconW} {white}   {iconR} {red}";
			return $"Updated: {iconW} {white}   {iconR} {red}";
		}

		public static string StatusRestoredDefault()
		{
			string lang = Lang;
			if (lang == "zh_cn" || lang == "zh_cht") return "已恢复为器官自然计算值";
			if (lang == "ru") return "Восстановлено к значениям органов";
			if (lang == "de") return "Auf natürliche Organwerte zurückgesetzt";
			if (lang == "fr") return "Rétabli aux valeurs naturelles";
			if (lang == "es") return "Restaurado a los valores de órganos";
			return "Restored to organ natural values";
		}

		public static string StatusSetCurrentTechSuccess(int r, int g, int b)
		{
			string lang = Lang;
			if (lang == "zh_cn" || lang == "zh_cht")
				return $"已更新科技点: {IconTechRed} {r}  {IconTechGreen} {g}  {IconTechBlue} {b}";
			if (lang == "ru")
				return $"Очки обновлены: {IconTechRed} {r}  {IconTechGreen} {g}  {IconTechBlue} {b}";
			if (lang == "de")
				return $"Punkte aktualisiert: {IconTechRed} {r}  {IconTechGreen} {g}  {IconTechBlue} {b}";
			if (lang == "fr")
				return $"Points mis à jour: {IconTechRed} {r}  {IconTechGreen} {g}  {IconTechBlue} {b}";
			if (lang == "es")
				return $"Puntos actualizados: {IconTechRed} {r}  {IconTechGreen} {g}  {IconTechBlue} {b}";
			return $"Tech points updated: {IconTechRed} {r}  {IconTechGreen} {g}  {IconTechBlue} {b}";
		}

		public static string StatusSetAllTechSuccess(int r, int g, int b, int count)
		{
			string lang = Lang;
			if (lang == "zh_cn" || lang == "zh_cht")
				return $"已将科技点({IconTechRed}{r} {IconTechGreen}{g} {IconTechBlue}{b})应用至全部 {count} 个僵尸工人！";
			if (lang == "ru")
				return $"Очки ({IconTechRed}{r} {IconTechGreen}{g} {IconTechBlue}{b}) применены ко всем {count} зомби!";
			if (lang == "de")
				return $"Tech-Punkte ({IconTechRed}{r} {IconTechGreen}{g} {IconTechBlue}{b}) auf alle {count} Zombies angewendet!";
			if (lang == "fr")
				return $"Points ({IconTechRed}{r} {IconTechGreen}{g} {IconTechBlue}{b}) appliqués à tous les {count} zombies !";
			if (lang == "es")
				return $"¡Puntos ({IconTechRed}{r} {IconTechGreen}{g} {IconTechBlue}{b}) aplicados a todos los {count} zombis!";
			return $"Tech points ({IconTechRed}{r} {IconTechGreen}{g} {IconTechBlue}{b}) applied to all {count} zombies!";
		}
	}
}
