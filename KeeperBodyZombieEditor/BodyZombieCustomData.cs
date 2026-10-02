using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;

namespace KeeperBodyZombieEditor
{
	[Serializable]
	public class SkullEntry
	{
		public int WhiteSkulls;
		public int RedSkulls;

		public SkullEntry()
		{
		}

		public SkullEntry(int white, int red)
		{
			this.WhiteSkulls = Mathf.Clamp(white, 0, 999);
			this.RedSkulls = Mathf.Clamp(red, 0, 999);
		}
	}

	public static class BodyZombieCustomData
	{
		private static readonly Dictionary<string, SkullEntry> Entries = new Dictionary<string, SkullEntry>(StringComparer.OrdinalIgnoreCase);
		private static string _saveFilePath;

		public static void Init(string configPath)
		{
			_saveFilePath = Path.Combine(configPath, "KeeperBodyZombieEditor.data.json");
			Load();
		}

		public static void Load()
		{
			try
			{
				if (!string.IsNullOrEmpty(_saveFilePath) && File.Exists(_saveFilePath))
				{
					string json = File.ReadAllText(_saveFilePath);
					var loaded = JsonConvert.DeserializeObject<Dictionary<string, SkullEntry>>(json);
					if (loaded != null)
					{
						Entries.Clear();
						foreach (var kv in loaded)
						{
							Entries[kv.Key] = kv.Value;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[KeeperBodyZombieEditor] Could not load skull data: " + ex.Message);
			}
		}

		public static void Save()
		{
			try
			{
				if (!string.IsNullOrEmpty(_saveFilePath))
				{
					string dir = Path.GetDirectoryName(_saveFilePath);
					if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}
					string json = JsonConvert.SerializeObject(Entries, Formatting.Indented);
					File.WriteAllText(_saveFilePath, json);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[KeeperBodyZombieEditor] Could not save skull data: " + ex.Message);
			}
		}

		public static void SetCustomSkulls(string id, int white, int red)
		{
			if (string.IsNullOrEmpty(id)) return;
			Entries[id] = new SkullEntry(white, red);
			Save();
		}

		public static bool TryGetCustomSkulls(string id, out int white, out int red)
		{
			white = 0;
			red = 0;
			if (string.IsNullOrEmpty(id)) return false;
			if (Entries.TryGetValue(id, out SkullEntry entry))
			{
				white = entry.WhiteSkulls;
				red = entry.RedSkulls;
				return true;
			}
			return false;
		}

		public static bool RemoveCustomSkulls(string id)
		{
			if (string.IsNullOrEmpty(id)) return false;
			bool removed = Entries.Remove(id);
			if (removed)
			{
				Save();
			}
			return removed;
		}
	}
}
