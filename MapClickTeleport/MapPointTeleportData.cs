using UnityEngine;

namespace MapClickTeleport
{
	/// <summary>
	/// Teleport payload that sends the player to an explicit scene and world position.
	/// Mirrors what the game does for its own map milestones (PlayerController.Teleport + TeleportDataBase).
	/// </summary>
	internal sealed class MapPointTeleportData : TeleportDataBase
	{
		private readonly string _sceneId;

		private readonly Vector3 _position;

		private readonly string _label;

		public MapPointTeleportData(string sceneId, Vector3 position, string label)
			: base(string.Empty, string.Empty, null, false, 0f)
		{
			this._sceneId = sceneId;
			this._position = position;
			this._label = label;
		}

		public override string GetDestinationId()
		{
			return string.IsNullOrEmpty(this._label) ? "map_point" : this._label;
		}

		public override GameSceneData GetDestinationSceneData()
		{
			if (string.IsNullOrEmpty(this._sceneId) || MainGame.WorldData == null)
			{
				return null;
			}
			return MainGame.WorldData.GetGameSceneDataById(this._sceneId);
		}

		public override Vector3 GetPosition()
		{
			return this._position;
		}

		public override bool CanTeleport(out string error)
		{
			if (string.IsNullOrEmpty(this._sceneId))
			{
				error = "The map point has no scene.";
				return false;
			}
			if (this.GetDestinationSceneData() == null)
			{
				error = "That scene is not available in this save.";
				return false;
			}
			error = string.Empty;
			return true;
		}
	}
}
