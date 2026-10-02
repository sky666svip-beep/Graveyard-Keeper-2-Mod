using System;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace MapClickTeleport
{
	/// <summary>
	/// Standalone Graveyard Keeper 2 plugin: open the world map, click a spot, and the player is sent there
	/// (cross-scene, with the correct ground height). It has no menu and no other dependencies - the cheat
	/// menu is not required and is not referenced.
	/// </summary>
	[BepInPlugin(MapClickTeleportPlugin.PluginGuid, "Map Click Teleport", MapClickTeleportPlugin.PluginVersion)]
	public sealed class MapClickTeleportPlugin : BaseUnityPlugin
	{
		public const string PluginGuid = "Narodum.gk2.mapclickteleport";

		public const string PluginVersion = "1.3.2";

		/// <summary>Canvas name the Keeper Cheat Menu overlay uses; clicks are ignored while it is on screen.</summary>
		private const string CheatMenuCanvasName = "KeeperCheatMenuCanvas";

		internal static MapClickTeleportPlugin Instance { get; private set; }

		private ConfigEntry<bool> _enabled;

		private ConfigEntry<bool> _doubleClickEnabled;

		private ConfigEntry<float> _doubleClickWindow;

		private ConfigEntry<float> _doubleClickDistance;

		private ConfigEntry<bool> _ctrlClickEnabled;

		private ConfigEntry<bool> _middleClickEnabled;

		private ConfigEntry<KeyCode> _hotkey;

		private ConfigEntry<float> _cooldown;

		private ConfigEntry<bool> _closeMapWindow;

		private ConfigEntry<bool> _correctGroundHeight;

		private ConfigEntry<bool> _freeIfStuck;

		private ConfigEntry<bool> _validateLanding;

		private ConfigEntry<float> _snapDistance;

		private ConfigEntry<bool> _recoverIfFalling;

		private ConfigEntry<bool> _verboseLogging;

		private int _clickCount;

		private float _clickTime;

		private Vector2 _clickPosition;

		private float _nextTriggerTime;

		private bool _postPending;

		private bool _postArrived;

		private bool _postSettled;

		private float _postArrivedAt;

		private bool _postHasOrigin;

		private string _postOriginSceneId;

		private Vector3 _postOriginPosition;

		private Vector3 _postAnchor;

		private float _postExpectedY;

		private float _postArmTime;

		private float _postDeadline;

		/// <summary>How far below the landing height the player must drop before it counts as falling out of the map.</summary>
		private const float FallOutThreshold = 10f;

		/// <summary>Seconds to wait after the player reached the landing spot before resolving the ground height.</summary>
		private const float SettleDelay = 0.35f;

		private void Awake()
		{
			MapClickTeleportPlugin.Instance = this;
			this._enabled = base.Config.Bind<bool>("General", "Enabled", true, "Master switch for this plugin.");
			this._doubleClickEnabled = base.Config.Bind<bool>("General", "Double click", true, "Teleport when the world map is double-clicked.");
			this._doubleClickWindow = base.Config.Bind<float>("General", "Double click window", 0.4f, "Maximum seconds between the two clicks of a double click (0.15-1).");
			this._doubleClickDistance = base.Config.Bind<float>("General", "Double click distance", 12f, "Maximum pixels between the two clicks of a double click (2-40).");
			this._ctrlClickEnabled = base.Config.Bind<bool>("General", "Ctrl + left click", false, "Also teleport on Ctrl + left click, for players who dislike double clicks.");
			this._middleClickEnabled = base.Config.Bind<bool>("General", "Middle click", false, "Also teleport on middle click.");
			this._hotkey = base.Config.Bind<KeyCode>("General", "Hotkey", KeyCode.None, "Optional extra trigger: while the map is open, pressing this key teleports to the spot under the mouse. Unbound by default.");
			this._cooldown = base.Config.Bind<float>("General", "Cooldown", 0.6f, "Seconds after a teleport during which further triggers are ignored (0-5).");
			this._closeMapWindow = base.Config.Bind<bool>("General", "Close map window", true, "Closes the map window before teleporting, the same way the game does for its own map milestones.");
			this._correctGroundHeight = base.Config.Bind<bool>("General", "Correct ground height", true, "Resolves the real ground elevation of the clicked spot. Keep this on, otherwise a teleport between different heights leaves the player floating.");
			this._freeIfStuck = base.Config.Bind<bool>("General", "Free the player if stuck", true, "After arriving, asks the game to move the player to a nearby free spot when they ended up stuck inside something.");
			this._validateLanding = base.Config.Bind<bool>("General", "Validate landing spot", true, "When the clicked spot is not inside any world zone (solid rock, cliff, water), land on the nearest walkable navigation point instead. Keep this on: without it such a click drops the player out of the map.");
			this._snapDistance = base.Config.Bind<float>("General", "Snap distance", 30f, "How far the nearest walkable navigation point may be when the clicked spot is not walkable (5-200).");
			this._recoverIfFalling = base.Config.Bind<bool>("General", "Recover if the player falls out", true, "If the player ends up falling well below the landing height, teleport them back to where they were before the map teleport.");
			this._verboseLogging = base.Config.Bind<bool>("General", "Verbose logging", false, "Also logs the raw map rectangle and cursor numbers of every attempt.");
			base.Logger.LogInfo(string.Format("{0} {1} loaded. Open the world map and click a spot to travel there.", "Map Click Teleport", MapClickTeleportPlugin.PluginVersion));
		}

		private void OnDestroy()
		{
			MapClickTeleportPlugin.Instance = null;
		}

		private void Update()
		{
			try
			{
				this.UpdatePostTeleport();
				if (!this.IsActive())
				{
					this._clickCount = 0;
					return;
				}
				this.UpdateHotkey();
				this.UpdateMouseTriggers();
			}
			catch (Exception ex)
			{
				base.Logger.LogError(ex);
			}
		}

		private bool IsActive()
		{
			if (!this._enabled.Value)
			{
				return false;
			}
			if (MapClickTeleportPlugin.IsCheatMenuOpen())
			{
				// The cheat menu draws its own full screen overlay, so the cursor would not be over the map.
				return false;
			}
			return true;
		}

		private void UpdateHotkey()
		{
			if (this._hotkey.Value == KeyCode.None || !Input.GetKeyDown(this._hotkey.Value))
			{
				return;
			}
			if (MapTeleportService.FindVisibleMapPage() == null)
			{
				base.Logger.LogInfo("Map click teleport (hotkey): no map page is visible right now.");
				return;
			}
			this.TryTrigger("hotkey " + this._hotkey.Value.ToString());
		}

		/// <summary>
		/// Mouse triggers. Everything is polled only on mouse down so the map lookup runs at most a few times
		/// per second, and every mouse trigger stays silent when no map is open so a stray click in the world
		/// does nothing at all.
		/// </summary>
		private void UpdateMouseTriggers()
		{
			bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
			if (this._ctrlClickEnabled.Value && control && Input.GetMouseButtonDown(0))
			{
				if (MapTeleportService.FindVisibleMapPage() != null)
				{
					this.TryTrigger("ctrl + left click");
				}
				return;
			}
			if (this._middleClickEnabled.Value && Input.GetMouseButtonDown(2))
			{
				if (MapTeleportService.FindVisibleMapPage() != null)
				{
					this.TryTrigger("middle click");
				}
				return;
			}
			if (!this._doubleClickEnabled.Value || !Input.GetMouseButtonDown(0))
			{
				return;
			}
			Vector2 position = Input.mousePosition;
			float now = Time.unscaledTime;
			float window = Mathf.Clamp(this._doubleClickWindow.Value, 0.15f, 1f);
			float distance = Mathf.Clamp(this._doubleClickDistance.Value, 2f, 40f);
			if (this._clickCount == 1 && now - this._clickTime <= window && (position - this._clickPosition).sqrMagnitude <= distance * distance)
			{
				this._clickCount = 0;
				if (MapTeleportService.FindVisibleMapPage() != null)
				{
					this.TryTrigger("double-click");
				}
				return;
			}
			this._clickCount = 1;
			this._clickTime = now;
			this._clickPosition = position;
		}

		private void TryTrigger(string trigger)
		{
			if (Time.unscaledTime < this._nextTriggerTime)
			{
				return;
			}
			PlayerController playerController = MainGame.PlayerController;
			// Remember where the player was, so a landing that goes wrong can be undone.
			string originSceneId = MapTeleportService.CurrentSceneId();
			Vector3 originPosition = (playerController != null) ? playerController.MovablePosition : Vector3.zero;
			Vector3 destination;
			if (!MapTeleportService.TryTeleportToMapCursor(trigger, this._closeMapWindow.Value, this._correctGroundHeight.Value, this._validateLanding.Value, this._snapDistance.Value, this._verboseLogging.Value, base.Logger, out destination))
			{
				return;
			}
			this._nextTriggerTime = Time.unscaledTime + Mathf.Clamp(this._cooldown.Value, 0f, 5f);
			this.ArmPostTeleport(destination, originSceneId, originPosition);
		}

		private void ArmPostTeleport(Vector3 destination, string originSceneId, Vector3 originPosition)
		{
			this._postOriginSceneId = originSceneId;
			this._postOriginPosition = originPosition;
			this._postHasOrigin = !string.IsNullOrEmpty(originSceneId);
			this._postArrived = false;
			this._postSettled = false;
			this._postPending = this._correctGroundHeight.Value || this._freeIfStuck.Value || this._recoverIfFalling.Value;
			this._postAnchor = destination;
			this._postExpectedY = destination.y;
			this._postArmTime = Time.unscaledTime;
			this._postDeadline = Time.unscaledTime + 12f;
		}

		/// <summary>
		/// Runs once after the teleport has really landed: corrects the ground height (a cross-scene target has
		/// no elevation data until it is loaded) and frees the player if they ended up inside something.
		/// </summary>
		private void UpdatePostTeleport()
		{
			if (!this._postPending)
			{
				return;
			}
			if (Time.unscaledTime > this._postDeadline)
			{
				if (!this._postSettled)
				{
					base.Logger.LogWarning("Map click teleport: the player never reached the landing spot, so the height correction was skipped.");
				}
				this._postPending = false;
				return;
			}
			if (MainGame.Instance == null)
			{
				return;
			}
			PlayerController playerController = MainGame.PlayerController;
			if (playerController == null)
			{
				return;
			}
			Vector3 position = playerController.MovablePosition;
			float dx = position.x - this._postAnchor.x;
			float dz = position.z - this._postAnchor.z;
			bool arrived = dx * dx + dz * dz <= 25f;
			// The teleport is asynchronous (fade, then position). Only work on the real landing spot: acting on a
			// timeout instead used to correct the ground height at whatever position the player still had, which
			// is why the same landing spot sometimes resolved a zone and sometimes did not.
			if (!this._postArrived)
			{
				if (!arrived)
				{
					return;
				}
				this._postArrived = true;
				this._postArrivedAt = Time.unscaledTime;
			}
			// A landing inside solid rock leaves the player falling out of the world: the camera follows them
			// down and the background turns black. Undo the teleport in that case. Only evaluated once the
			// player is at the landing spot horizontally (falling is vertical), so the position of a scene that
			// is still loading cannot be mistaken for a fall.
			bool nearLanding = dx * dx + dz * dz <= 400f;
			if (this._recoverIfFalling.Value && this._postHasOrigin && nearLanding && position.y < this._postExpectedY - FallOutThreshold)
			{
				this._postPending = false;
				this.RecoverToOrigin(playerController);
				return;
			}
			if (this._postSettled)
			{
				return;
			}
			// Give the zone triggers and zone objects of the arrival scene a moment to come up, otherwise the
			// ground lookup can run before the game knows which zone the player stands in.
			if (Time.unscaledTime - this._postArrivedAt < SettleDelay)
			{
				return;
			}
			this._postSettled = true;
			if (this._correctGroundHeight.Value)
			{
				MapTeleportService.TrySnapPlayerToGround(playerController, "map arrival", base.Logger);
			}
			if (this._freeIfStuck.Value)
			{
				this.FreePlayerIfStuck(playerController);
			}
		}

		/// <summary>Teleports the player back to where they stood before the map teleport.</summary>
		private void RecoverToOrigin(PlayerController playerController)
		{
			try
			{
				base.Logger.LogWarning(string.Format("Map click teleport: the player fell out of the map (y {0:0.0} vs landing {1:0.0}); returning to scene '{2}' at ({3:0.0}, {4:0.0}, {5:0.0}).", playerController.MovablePosition.y, this._postExpectedY, this._postOriginSceneId, this._postOriginPosition.x, this._postOriginPosition.y, this._postOriginPosition.z));
				MapPointTeleportData returnData = new MapPointTeleportData(this._postOriginSceneId, this._postOriginPosition, "return");
				if (!PlayerController.Teleport(returnData))
				{
					base.Logger.LogWarning("Map click teleport: the game refused the return teleport. Use the map again to travel somewhere valid.");
				}
			}
			catch (Exception ex)
			{
				base.Logger.LogError("Could not return the player to the previous position: " + ex);
			}
		}

		/// <summary>
		/// The game's own "move the player to any free place" helper: it checks whether the player overlaps
		/// something and only then relocates them, so calling it after every teleport is harmless.
		/// </summary>
		private void FreePlayerIfStuck(PlayerController playerController)
		{
			try
			{
				playerController.TryTeleportPlayerToAnyFreePlace();
				base.Logger.LogInfo("Map click teleport: asked the game to free the player if they are stuck.");
			}
			catch (Exception ex)
			{
				base.Logger.LogWarning("Could not run the stuck-player check: " + ex.Message);
			}
		}

		private static bool IsCheatMenuOpen()
		{
			GameObject canvas = GameObject.Find(MapClickTeleportPlugin.CheatMenuCanvasName);
			return canvas != null && canvas.activeInHierarchy;
		}
	}
}
