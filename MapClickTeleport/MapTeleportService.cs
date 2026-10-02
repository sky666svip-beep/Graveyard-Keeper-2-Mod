using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace MapClickTeleport
{
	/// <summary>
	/// All game-side work for "double-click a spot on the world map and go there".
	/// Every game symbol used here is taken from Assembly-CSharp:
	/// MapPageWidget / CharacterWindow / UIMapWindow / GUIElements / WorldZone / VisualConsts /
	/// PlayerController.Teleport / MainGame.WorldData.
	/// </summary>
	internal static class MapTeleportService
	{
		/// <summary>MapPageWidget's map fields are private serialized fields, so they are read once and cached.</summary>
		private static FieldInfo _mapRectField;

		private static FieldInfo _milestonesRectField;

		private static FieldInfo _scrollRectField;

		private static bool _mapFrameFieldsResolved;

		/// <summary>
		/// Tilt of the world map projection. VisualConsts shifts z by 0.75 per unit of elevation and the map
		/// uses the same tilt, so 0.75 is the safe fallback when the map bounds are not readable.
		/// </summary>
		private const float DefaultTiltTan = 0.75f;

		/// <summary>How far below the lowest known ground a surface hit may still be before it is treated as bogus.</summary>
		private const float LowGroundTolerance = 10f;

		internal static bool TryTeleportToMapCursor(string trigger, bool closeHostWindow, bool correctGroundHeight, bool validateLanding, float snapDistance, bool verbose, ManualLogSource logger, out Vector3 destination)
		{
			destination = Vector3.zero;
			try
			{
				if (MainGame.Instance == null || MainGame.PlayerController == null)
				{
					logger.LogWarning("Map click teleport (" + trigger + "): no game save is loaded.");
					return false;
				}
				MapPageWidget mapPageWidget = FindVisibleMapPage();
				if (mapPageWidget == null)
				{
					logger.LogInfo("Map click teleport (" + trigger + "): no map page is visible right now.");
					return false;
				}
				string frameSource;
				RectTransform mapFrame = GetMapFrame(mapPageWidget, out frameSource);
				if (mapFrame == null)
				{
					logger.LogWarning("Map click teleport (" + trigger + "): none of the MapPageWidget map fields (mapRect / milestonesRect / scrollRect) could be read.");
					return false;
				}
				if (!string.Equals(frameSource, "mapRect", StringComparison.Ordinal))
				{
					logger.LogWarning("Map click teleport (" + trigger + "): mapRect was not readable, falling back to " + frameSource + " for the coordinate conversion.");
				}
				float worldX;
				float projectedZ;
				if (!TryGetMapColumn(mapFrame, verbose, logger, out worldX, out projectedZ))
				{
					return false;
				}
				string sceneId = null;
				string heightSource;
				// True only when the clicked spot really sits inside a world zone, which is what makes it a
				// walkable place. The approximate paths below produce a height but say nothing about that.
				bool spotInsideZone = correctGroundHeight && TryResolveMapDestination(worldX, projectedZ, out sceneId, out destination);
				if (spotInsideZone)
				{
					heightSource = "zone data";
				}
				else
				{
					// Either the correction is switched off, or the spot is outside every zone / the target
					// scene is not loaded. Pick the scene from the navigation points of every scene in the save
					// and take the height from that scene's own nearest point.
					if (TryResolveApproximateDestination(worldX, projectedZ, out sceneId, out destination))
					{
						heightSource = "scene navigation points";
					}
					else
					{
						destination = ApproximateMapDestination(worldX, projectedZ);
						sceneId = ResolveSceneIdForWorldPoint(destination);
						heightSource = "global approximation";
					}
				}
				if (validateLanding && !spotInsideZone)
				{
					// The spot is not inside any world zone, so it is not one of the game's walkable areas. Cast
					// down the map column to find the ground surface the player is actually pointing at: that
					// keeps the landing precise instead of jumping to the nearest walkable point, and a surface
					// hit can never be inside solid rock.
					Vector3 surface;
					string surfaceSceneId;
					if (TryRaycastMapColumn(worldX, projectedZ, out surface, out surfaceSceneId))
					{
						logger.LogInfo(string.Format(CultureInfo.InvariantCulture, "Map click teleport ({0}): the clicked spot is not inside any world zone; using the ground surface under it at ({1:0.0}, {2:0.0}, {3:0.0}), {4:0.0} units from the click.", trigger, surface.x, surface.y, surface.z, MapTeleportService.ProjectedDistance(worldX, projectedZ, surface)));
						destination = surface;
						if (!string.IsNullOrEmpty(surfaceSceneId))
						{
							sceneId = surfaceSceneId;
						}
						heightSource = "ground surface";
					}
					else
					{
						// Nothing solid under that column (void or water): fall back to a known walkable point.
						Vector3 snapped;
						string snappedSceneId;
						if (TrySnapToNavigationPoint(worldX, projectedZ, sceneId, snapDistance, out snappedSceneId, out snapped))
						{
							logger.LogInfo(string.Format(CultureInfo.InvariantCulture, "Map click teleport ({0}): no ground surface under the clicked spot; snapped to the nearest walkable navigation point of scene '{1}' at ({2:0.0}, {3:0.0}, {4:0.0}), {5:0.0} units from the click.", trigger, snappedSceneId, snapped.x, snapped.y, snapped.z, MapTeleportService.ProjectedDistance(worldX, projectedZ, snapped)));
							destination = snapped;
							sceneId = snappedSceneId;
							heightSource = "navigation point snap";
						}
						else
						{
							logger.LogWarning(string.Format(CultureInfo.InvariantCulture, "Map click teleport ({0}): refused, the clicked spot has no ground surface and no navigation point is within {1:0} units, so it would have dropped the player outside the map.", trigger, snapDistance));
							return false;
						}
					}
				}
				if (string.IsNullOrEmpty(sceneId))
				{
					logger.LogWarning("Map click teleport (" + trigger + "): could not match that map spot to a scene.");
					return false;
				}
				WorldData worldData = MainGame.WorldData;
				if (worldData == null || worldData.GetGameSceneDataById(sceneId) == null)
				{
					logger.LogWarning("Map click teleport (" + trigger + "): scene '" + sceneId + "' is not available in this save.");
					return false;
				}
				logger.LogInfo(string.Format(CultureInfo.InvariantCulture, "Map click teleport ({0}): going to scene '{1}' at ({2:0.0}, {3:0.0}, {4:0.0}), height from {5}.", trigger, sceneId, destination.x, destination.y, destination.z, heightSource));
				if (closeHostWindow)
				{
					CloseMapHostWindow(mapPageWidget, logger);
				}
				MapPointTeleportData teleportData = new MapPointTeleportData(sceneId, destination, "map_point");
				string error;
				if (!teleportData.CanTeleport(out error))
				{
					logger.LogWarning("Map click teleport (" + trigger + "): " + error);
					return false;
				}
				if (!PlayerController.Teleport(teleportData))
				{
					logger.LogWarning("Map click teleport (" + trigger + "): the game refused the teleport request.");
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				logger.LogError("Map click teleport failed: " + ex);
				return false;
			}
		}

		/// <summary>
		/// Returns the map page that is actually on screen. The player-facing map lives inside CharacterWindow
		/// (CharacterWindowData.CharPage.Map); UIMapWindow hosts a second copy for the milestone flow, so the
		/// visible MapPageWidget is looked up directly instead of guessing the window type.
		/// </summary>
		internal static MapPageWidget FindVisibleMapPage()
		{
			MapPageWidget[] pages = Resources.FindObjectsOfTypeAll<MapPageWidget>();
			if (pages == null)
			{
				return null;
			}
			for (int i = 0; i < pages.Length; i++)
			{
				MapPageWidget page = pages[i];
				if (page != null && page.gameObject.activeInHierarchy)
				{
					return page;
				}
			}
			return null;
		}

		/// <summary>
		/// The rect the map coordinates are measured in. mapRect is the field the game positions map icons
		/// with; milestonesRect and the scroll view content cover the same area, so they are used as fallbacks
		/// in case a game update renames or drops mapRect.
		/// </summary>
		private static RectTransform GetMapFrame(MapPageWidget mapPageWidget, out string source)
		{
			source = null;
			if (!MapTeleportService._mapFrameFieldsResolved)
			{
				MapTeleportService._mapFrameFieldsResolved = true;
				Type type = typeof(MapPageWidget);
				MapTeleportService._mapRectField = type.GetField("mapRect", BindingFlags.Instance | BindingFlags.NonPublic);
				MapTeleportService._milestonesRectField = type.GetField("milestonesRect", BindingFlags.Instance | BindingFlags.NonPublic);
				MapTeleportService._scrollRectField = type.GetField("scrollRect", BindingFlags.Instance | BindingFlags.NonPublic);
			}
			RectTransform rect = MapTeleportService.ReadRectField(MapTeleportService._mapRectField, mapPageWidget);
			if (rect != null)
			{
				source = "mapRect";
				return rect;
			}
			rect = MapTeleportService.ReadRectField(MapTeleportService._milestonesRectField, mapPageWidget);
			if (rect != null)
			{
				source = "milestonesRect";
				return rect;
			}
			ScrollRect scrollRect = MapTeleportService.ReadScrollRectField(MapTeleportService._scrollRectField, mapPageWidget);
			if (scrollRect != null && scrollRect.content != null)
			{
				source = "scrollRect.content";
				return scrollRect.content;
			}
			return null;
		}

		private static RectTransform ReadRectField(FieldInfo field, MapPageWidget mapPageWidget)
		{
			if (field == null)
			{
				return null;
			}
			try
			{
				return field.GetValue(mapPageWidget) as RectTransform;
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static ScrollRect ReadScrollRectField(FieldInfo field, MapPageWidget mapPageWidget)
		{
			if (field == null)
			{
				return null;
			}
			try
			{
				return field.GetValue(mapPageWidget) as ScrollRect;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Converts the mouse position over the map rect into the world column it points at, inverting the
		/// projection MapPageWidget uses to place map icons: u/v over [WorldMin, WorldMax] with z + y * tan(tilt).
		/// </summary>
		private static bool TryGetMapColumn(RectTransform mapRect, bool verbose, ManualLogSource logger, out float worldX, out float projectedZ)
		{
			worldX = 0f;
			projectedZ = 0f;
			if (GUIElements.Instance == null)
			{
				logger.LogWarning("Map click teleport: the world map bounds are not ready.");
				return false;
			}
			Transform worldMin = GUIElements.Instance.WorldMin;
			Transform worldMax = GUIElements.Instance.WorldMax;
			if (worldMin == null || worldMax == null)
			{
				logger.LogWarning("Map click teleport: the world map bounds are not assigned.");
				return false;
			}
			Canvas canvas = mapRect.GetComponentInParent<Canvas>();
			Camera camera = (canvas != null) ? canvas.worldCamera : null;
			Vector2 localPoint;
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, Input.mousePosition, camera, out localPoint))
			{
				logger.LogWarning("Map click teleport: could not read the map cursor position.");
				return false;
			}
			Rect rect = mapRect.rect;
			if (rect.width <= 0f || rect.height <= 0f)
			{
				logger.LogWarning("Map click teleport: the world map has no usable size yet.");
				return false;
			}
			float u = localPoint.x / rect.width + mapRect.pivot.x;
			float v = localPoint.y / rect.height + mapRect.pivot.y;
			if (verbose)
			{
				logger.LogInfo(string.Format(CultureInfo.InvariantCulture, "Map click teleport: map rect {0:0}x{1:0} pivot ({2:0.##}, {3:0.##}), local ({4:0.0}, {5:0.0}) -> u/v ({6:0.###}, {7:0.###}).", rect.width, rect.height, mapRect.pivot.x, mapRect.pivot.y, localPoint.x, localPoint.y, u, v));
			}
			if (u < 0f || u > 1f || v < 0f || v > 1f)
			{
				logger.LogInfo("Map click teleport: the cursor is outside the drawn world map.");
				return false;
			}
			worldX = Mathf.Lerp(worldMin.position.x, worldMax.position.x, u);
			projectedZ = Mathf.Lerp(worldMin.position.z, worldMax.position.z, v);
			return true;
		}

		/// <summary>
		/// Resolves the clicked map column against the live WorldZone objects of every loaded scene, which is
		/// the only source of the real ground elevation of that spot (WorldZoneData and its elevation areas
		/// only exist while the scene is loaded). This is the same ground lookup BuildController uses.
		/// The player's own scene is tried first.
		/// </summary>
		private static bool TryResolveMapDestination(float worldX, float projectedZ, out string sceneId, out Vector3 destination)
		{
			sceneId = null;
			destination = Vector3.zero;
			WorldZone[] zones = Resources.FindObjectsOfTypeAll<WorldZone>();
			if (zones == null || zones.Length == 0)
			{
				return false;
			}
			string currentSceneId = CurrentSceneId();
			float tiltTan = MapTiltTan();
			for (int pass = 0; pass < 2; pass++)
			{
				for (int i = 0; i < zones.Length; i++)
				{
					WorldZone zone = zones[i];
					if (zone == null)
					{
						continue;
					}
					WorldZoneData data = zone.Data;
					if (data == null || string.IsNullOrEmpty(data.gameSceneId))
					{
						continue;
					}
					bool isCurrent = string.Equals(data.gameSceneId, currentSceneId, StringComparison.Ordinal);
					if ((pass == 0) != isCurrent)
					{
						continue;
					}
					float minX;
					float maxX;
					float minZ;
					float maxZ;
					float groundY;
					if (!TryGetZoneGroundRect(zone, out minX, out maxX, out minZ, out maxZ, out groundY))
					{
						continue;
					}
					float groundZ = projectedZ - groundY * tiltTan;
					if (worldX < minX || worldX > maxX || groundZ < minZ || groundZ > maxZ)
					{
						continue;
					}
					Vector3 groundPoint = new Vector3(worldX, groundY, groundZ);
					float elevationY;
					destination = zone.TryGetBuildElevationY(groundPoint.x, groundPoint.z, out elevationY) ? VisualConsts.ProjectGroundPointToElevation(groundPoint, elevationY) : groundPoint;
					sceneId = data.gameSceneId;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// The area a zone covers, expressed in the zone's ground frame (x, z with the ground plane at y=0).
		/// The zone collider's world bounds are authoritative: WorldZoneData.wholeZoneRect is built from the
		/// collider's local size and ignores rotation and scale, so it can miss parts of a real zone. The
		/// elevation shift uses the same convention as WorldZoneElevationArea.GetXZRect (0.75 per unit).
		/// </summary>
		private static bool TryGetZoneGroundRect(WorldZone zone, out float minX, out float maxX, out float minZ, out float maxZ, out float groundY)
		{
			minX = 0f;
			maxX = 0f;
			minZ = 0f;
			maxZ = 0f;
			groundY = zone.GroundPlaneY;
			BoxCollider collider = zone.ZoneCollider;
			if (collider != null)
			{
				Bounds bounds = collider.bounds;
				float shift = (bounds.center.y - groundY) * 0.75f;
				minX = bounds.min.x;
				maxX = bounds.max.x;
				minZ = bounds.min.z + shift;
				maxZ = bounds.max.z + shift;
				return true;
			}
			WorldZoneData data = zone.Data;
			if (data != null)
			{
				minX = data.wholeZoneRect.xMin;
				maxX = data.wholeZoneRect.xMax;
				minZ = data.wholeZoneRect.yMin;
				maxZ = data.wholeZoneRect.yMax;
				return true;
			}
			return false;
		}

		/// <summary>The zone the game itself says the player is in (zone membership comes from physics triggers).</summary>
		private static WorldZone FindPlayerWorldZone()
		{
			PlayerData playerData = MainGame.PlayerData;
			if (playerData == null)
			{
				return null;
			}
			WorldZoneData currentZoneData = playerData.CurrentWorldZoneData;
			if (currentZoneData == null)
			{
				return null;
			}
			WorldZone[] zones = Resources.FindObjectsOfTypeAll<WorldZone>();
			if (zones == null)
			{
				return null;
			}
			for (int i = 0; i < zones.Length; i++)
			{
				WorldZone zone = zones[i];
				if (zone != null && zone.Data == currentZoneData)
				{
					return zone;
				}
			}
			return null;
		}

		/// <summary>Zone whose world collider contains a raw world XZ position; the current scene is preferred.</summary>
		private static WorldZone FindZoneForWorldPoint(float x, float worldZ)
		{
			WorldZone[] zones = Resources.FindObjectsOfTypeAll<WorldZone>();
			if (zones == null)
			{
				return null;
			}
			string currentSceneId = CurrentSceneId();
			for (int pass = 0; pass < 2; pass++)
			{
				for (int i = 0; i < zones.Length; i++)
				{
					WorldZone zone = zones[i];
					if (zone == null)
					{
						continue;
					}
					WorldZoneData data = zone.Data;
					bool isCurrent = data != null && string.Equals(data.gameSceneId, currentSceneId, StringComparison.Ordinal);
					if ((pass == 0) != isCurrent)
					{
						continue;
					}
					BoxCollider collider = zone.ZoneCollider;
					if (collider == null)
					{
						continue;
					}
					Bounds bounds = collider.bounds;
					if (x >= bounds.min.x && x <= bounds.max.x && worldZ >= bounds.min.z && worldZ <= bounds.max.z)
					{
						return zone;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Casts a ray down the map column the click points at. The map collapses a whole elevation column into
		/// one spot, so the ray direction is the elevation shift VisualConsts uses (down and +z by the tilt),
		/// and the hit is the ground surface the player is actually pointing at. A surface hit is precise and
		/// can never be inside solid rock.
		/// </summary>
		private static bool TryRaycastMapColumn(float worldX, float projectedZ, out Vector3 surface, out string sceneId)
		{
			surface = Vector3.zero;
			sceneId = null;
			float tiltTan = MapTiltTan();
			const float startY = 200f;
			Vector3 origin = new Vector3(worldX, startY, projectedZ - startY * tiltTan);
			Vector3 direction = new Vector3(0f, -1f, tiltTan).normalized;
			RaycastHit hit;
			if (!Physics.Raycast(origin, direction, out hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
			{
				return false;
			}
			Vector3 point = hit.point + Vector3.up * 0.05f;
			// Deep colliders (world floors, chunk bounds) sit far below the scene's real ground. Real logs showed
			// a hit at y -32.9 while the scene's zones are around y 1-6, and the player fell straight through.
			float lowestGround = LowestKnownGroundY();
			if (lowestGround < float.MaxValue && point.y < lowestGround - LowGroundTolerance)
			{
				return false;
			}
			surface = point;
			WorldZone zone = (hit.collider != null) ? hit.collider.GetComponentInParent<WorldZone>() : null;
			if (zone != null && zone.Data != null && !string.IsNullOrEmpty(zone.Data.gameSceneId))
			{
				sceneId = zone.Data.gameSceneId;
			}
			return true;
		}

		/// <summary>Lowest ground height of every loaded zone, or float.MaxValue when nothing is known.</summary>
		private static float LowestKnownGroundY()
		{
			float lowest = float.MaxValue;
			WorldZone[] zones = Resources.FindObjectsOfTypeAll<WorldZone>();
			if (zones == null)
			{
				return lowest;
			}
			for (int i = 0; i < zones.Length; i++)
			{
				WorldZone zone = zones[i];
				if (zone == null)
				{
					continue;
				}
				float groundY = zone.GroundPlaneY;
				if (groundY < lowest)
				{
					lowest = groundY;
				}
				WorldZoneData data = zone.Data;
				if (data == null || data.elevationAreas == null)
				{
					continue;
				}
				for (int j = 0; j < data.elevationAreas.Count; j++)
				{
					float elevationY = data.elevationAreas[j].elevationY;
					if (elevationY < lowest)
					{
						lowest = elevationY;
					}
				}
			}
			return lowest;
		}

		/// <summary>Distance between a map column and a world point, measured in the map's projected frame.</summary>
		private static float ProjectedDistance(float worldX, float projectedZ, Vector3 point)
		{
			float dx = point.x - worldX;
			float dz = point.z + point.y * MapTiltTan() - projectedZ;
			return Mathf.Sqrt(dx * dx + dz * dz);
		}

		/// <summary>One zone's walkable area, expressed in the zone's ground frame.</summary>
		private struct ZoneGroundRect
		{
			public float MinX;

			public float MaxX;

			public float MinZ;

			public float MaxZ;

			public float GroundY;

			public bool Contains(float x, float y, float z)
			{
				// Same conversion VisualConsts.ProjectElevationPointToGround uses.
				float groundZ = z + (y - this.GroundY) * 0.75f;
				return x >= this.MinX && x <= this.MaxX && groundZ >= this.MinZ && groundZ <= this.MaxZ;
			}
		}

		/// <summary>
		/// Nearest known walkable navigation point to a map column. Points inside a world zone are preferred:
		/// the navigation data also holds transit and town link points that sit outside every zone, and real
		/// logs showed one of those with a y of 0 which dropped the player out of the map. The chosen point's
		/// own position is used verbatim, so the landing spot is a place the game itself walks on.
		/// </summary>
		private static bool TrySnapToNavigationPoint(float worldX, float projectedZ, string preferredSceneId, float maxDistance, out string sceneId, out Vector3 position)
		{
			sceneId = null;
			position = Vector3.zero;
			WorldData worldData = MainGame.WorldData;
			if (worldData == null || worldData.gdPointsData == null)
			{
				return false;
			}
			List<GDPointData> points = worldData.gdPointsData.Points;
			if (points == null || points.Count == 0)
			{
				return false;
			}
			float tiltTan = MapTiltTan();
			float limit = Mathf.Max(1f, maxDistance);
			limit *= limit;
			List<ZoneGroundRect> zones = CollectZoneGroundRects();
			bool hasPreferred = !string.IsNullOrEmpty(preferredSceneId);
			// Some navigation entries are transit links that sit in mid air (real logs: a point with y 0 whose
			// column has no ground at all). Verify the candidate really has ground under it before using it.
			List<Vector3> rejected = null;
			for (int attempt = 0; attempt < 4; attempt++)
			{
				string candidateSceneId;
				Vector3 candidate;
				bool found = (hasPreferred && zones.Count > 0 && FindNearestNavigationPoint(points, worldX, projectedZ, tiltTan, limit, preferredSceneId, zones, rejected, out candidateSceneId, out candidate))
					|| (hasPreferred && FindNearestNavigationPoint(points, worldX, projectedZ, tiltTan, limit, preferredSceneId, null, rejected, out candidateSceneId, out candidate))
					|| (zones.Count > 0 && FindNearestNavigationPoint(points, worldX, projectedZ, tiltTan, limit, null, zones, rejected, out candidateSceneId, out candidate))
					|| FindNearestNavigationPoint(points, worldX, projectedZ, tiltTan, limit, null, null, rejected, out candidateSceneId, out candidate);
				if (!found)
				{
					break;
				}
				if (HasGroundUnder(candidate))
				{
					sceneId = candidateSceneId;
					position = candidate;
					return true;
				}
				rejected = rejected ?? new List<Vector3>();
				rejected.Add(candidate);
			}
			sceneId = null;
			position = Vector3.zero;
			return false;
		}

		/// <summary>True when there is solid ground just below a world point (casts down its elevation column).</summary>
		private static bool HasGroundUnder(Vector3 point)
		{
			float tiltTan = MapTiltTan();
			const float above = 2f;
			Vector3 origin = new Vector3(point.x, point.y + above, point.z - above * tiltTan);
			Vector3 direction = new Vector3(0f, -1f, tiltTan).normalized;
			RaycastHit[] hits = Physics.RaycastAll(origin, direction, 6f, ~0, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < hits.Length; i++)
			{
				Collider collider = hits[i].collider;
				if (collider == null)
				{
					continue;
				}
				// The player's own capsule is not ground.
				if (collider.GetComponentInParent<PlayerPhysicalBody>() != null)
				{
					continue;
				}
				return true;
			}
			return false;
		}

		private static List<ZoneGroundRect> CollectZoneGroundRects()
		{
			List<ZoneGroundRect> list = new List<ZoneGroundRect>();
			WorldZone[] zones = Resources.FindObjectsOfTypeAll<WorldZone>();
			if (zones == null)
			{
				return list;
			}
			for (int i = 0; i < zones.Length; i++)
			{
				WorldZone zone = zones[i];
				if (zone == null)
				{
					continue;
				}
				float minX;
				float maxX;
				float minZ;
				float maxZ;
				float groundY;
				if (TryGetZoneGroundRect(zone, out minX, out maxX, out minZ, out maxZ, out groundY))
				{
					list.Add(new ZoneGroundRect
					{
						MinX = minX,
						MaxX = maxX,
						MinZ = minZ,
						MaxZ = maxZ,
						GroundY = groundY
					});
				}
			}
			return list;
		}

		private static bool FindNearestNavigationPoint(List<GDPointData> points, float worldX, float projectedZ, float tiltTan, float limitSqr, string onlySceneId, List<ZoneGroundRect> requiredZones, List<Vector3> rejected, out string sceneId, out Vector3 position)
		{
			sceneId = null;
			position = Vector3.zero;
			float bestDistance = limitSqr;
			for (int i = 0; i < points.Count; i++)
			{
				GDPointData point = points[i];
				if (point == null || string.IsNullOrEmpty(point.GameSceneDataId))
				{
					continue;
				}
				if (onlySceneId != null && !string.Equals(point.GameSceneDataId, onlySceneId, StringComparison.Ordinal))
				{
					continue;
				}
				Vector3 candidate = point.Position;
				if (requiredZones != null && !IsInsideAnyZone(requiredZones, candidate))
				{
					continue;
				}
				if (rejected != null && rejected.Contains(candidate))
				{
					continue;
				}
				float dx = candidate.x - worldX;
				float dz = candidate.z + candidate.y * tiltTan - projectedZ;
				float distance = dx * dx + dz * dz;
				if (distance < bestDistance)
				{
					bestDistance = distance;
					sceneId = point.GameSceneDataId;
					position = candidate;
				}
			}
			return sceneId != null;
		}

		private static bool IsInsideAnyZone(List<ZoneGroundRect> zones, Vector3 worldPoint)
		{
			for (int i = 0; i < zones.Count; i++)
			{
				if (zones[i].Contains(worldPoint.x, worldPoint.y, worldPoint.z))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Projected XZ bounds of one scene's navigation points.</summary>
		private struct SceneBounds
		{
			public float MinX;

			public float MinZ;

			public float MaxX;

			public float MaxZ;

			public float Area
			{
				get
				{
					return (this.MaxX - this.MinX) * (this.MaxZ - this.MinZ);
				}
			}
		}

		/// <summary>
		/// Scene lookup for a click into a scene that is not loaded. Every scene's navigation points are known
		/// without loading anything (WorldData.gdPointsData is filled for all scene configs), so the scene is
		/// picked by containment in the tightest matching point bounds, and the height then comes from the
		/// nearest navigation point of [b]that[/b] scene instead of a global nearest point that may belong to
		/// a different one. All comparisons happen in the map's projected frame (x, z + y * tan).
		/// </summary>
		private static bool TryResolveApproximateDestination(float worldX, float projectedZ, out string sceneId, out Vector3 destination)
		{
			sceneId = null;
			destination = Vector3.zero;
			WorldData worldData = MainGame.WorldData;
			if (worldData == null || worldData.gdPointsData == null)
			{
				return false;
			}
			List<GDPointData> points = worldData.gdPointsData.Points;
			if (points == null || points.Count == 0)
			{
				return false;
			}
			float tiltTan = MapTiltTan();
			Dictionary<string, SceneBounds> boundsByScene = new Dictionary<string, SceneBounds>();
			for (int i = 0; i < points.Count; i++)
			{
				GDPointData point = points[i];
				if (point == null || string.IsNullOrEmpty(point.GameSceneDataId))
				{
					continue;
				}
				Vector3 position = point.Position;
				float x = position.x;
				float z = position.z + position.y * tiltTan;
				SceneBounds bounds;
				if (boundsByScene.TryGetValue(point.GameSceneDataId, out bounds))
				{
					if (x < bounds.MinX)
					{
						bounds.MinX = x;
					}
					if (x > bounds.MaxX)
					{
						bounds.MaxX = x;
					}
					if (z < bounds.MinZ)
					{
						bounds.MinZ = z;
					}
					if (z > bounds.MaxZ)
					{
						bounds.MaxZ = z;
					}
					boundsByScene[point.GameSceneDataId] = bounds;
				}
				else
				{
					boundsByScene[point.GameSceneDataId] = new SceneBounds
					{
						MinX = x,
						MaxX = x,
						MinZ = z,
						MaxZ = z
					};
				}
			}
			if (boundsByScene.Count == 0)
			{
				return false;
			}
			string bestSceneId = null;
			float bestArea = float.MaxValue;
			foreach (KeyValuePair<string, SceneBounds> pair in boundsByScene)
			{
				SceneBounds bounds = pair.Value;
				if (worldX < bounds.MinX || worldX > bounds.MaxX || projectedZ < bounds.MinZ || projectedZ > bounds.MaxZ)
				{
					continue;
				}
				float area = bounds.Area;
				if (area < bestArea)
				{
					bestArea = area;
					bestSceneId = pair.Key;
				}
			}
			if (string.IsNullOrEmpty(bestSceneId))
			{
				return false;
			}
			float bestDistance = float.MaxValue;
			float elevation = 0f;
			for (int j = 0; j < points.Count; j++)
			{
				GDPointData point = points[j];
				if (point == null || !string.Equals(point.GameSceneDataId, bestSceneId, StringComparison.Ordinal))
				{
					continue;
				}
				Vector3 position = point.Position;
				float dx = position.x - worldX;
				float dz = position.z + position.y * tiltTan - projectedZ;
				float distance = dx * dx + dz * dz;
				if (distance < bestDistance)
				{
					bestDistance = distance;
					elevation = position.y;
				}
			}
			if (bestDistance == float.MaxValue)
			{
				return false;
			}
			sceneId = bestSceneId;
			destination = new Vector3(worldX, elevation, projectedZ - elevation * tiltTan);
			return true;
		}

		/// <summary>
		/// Last resort for a click into a scene that is not loaded: keep the clicked x/z but take the height
		/// from the globally nearest navigation point, which is authored at a walkable elevation.
		/// </summary>
		private static Vector3 ApproximateMapDestination(float worldX, float projectedZ)
		{
			float elevation = 0f;
			PlayerController playerController = MainGame.PlayerController;
			if (playerController != null)
			{
				elevation = playerController.MovablePosition.y;
			}
			WorldData worldData = MainGame.WorldData;
			if (worldData != null && worldData.gdPointsData != null)
			{
				List<GDPointData> points = worldData.gdPointsData.Points;
				if (points != null)
				{
					float tiltTan = MapTiltTan();
					float bestDistance = float.MaxValue;
					float bestElevation = elevation;
					for (int i = 0; i < points.Count; i++)
					{
						GDPointData point = points[i];
						if (point == null)
						{
							continue;
						}
						Vector3 position = point.Position;
						float dx = position.x - worldX;
						// Compare in the same projected frame the map uses, not raw z.
						float dz = position.z + position.y * tiltTan - projectedZ;
						float distance = dx * dx + dz * dz;
						if (distance < bestDistance)
						{
							bestDistance = distance;
							bestElevation = position.y;
						}
					}
					elevation = bestElevation;
				}
			}
			return new Vector3(worldX, elevation, projectedZ - elevation * MapTiltTan());
		}

		/// <summary>
		/// Finds the scene that owns a world point: the nearest navigation point (they exist for every scene)
		/// and, when that is far away, the nearest scene origin.
		/// </summary>
		private static string ResolveSceneIdForWorldPoint(Vector3 worldPoint)
		{
			WorldData worldData = MainGame.WorldData;
			if (worldData == null)
			{
				return null;
			}
			string bestSceneId = null;
			float bestDistance = float.MaxValue;
			GdPointsData gdPointsData = worldData.gdPointsData;
			if (gdPointsData != null)
			{
				List<GDPointData> points = gdPointsData.Points;
				if (points != null)
				{
					for (int i = 0; i < points.Count; i++)
					{
						GDPointData point = points[i];
						if (point == null || string.IsNullOrEmpty(point.GameSceneDataId))
						{
							continue;
						}
						Vector3 position = point.Position;
						float dx = position.x - worldPoint.x;
						float dz = position.z - worldPoint.z;
						float distance = dx * dx + dz * dz;
						if (distance < bestDistance)
						{
							bestDistance = distance;
							bestSceneId = point.GameSceneDataId;
						}
					}
				}
			}
			if (!string.IsNullOrEmpty(bestSceneId) && bestDistance <= 10000f)
			{
				return bestSceneId;
			}
			bestSceneId = null;
			bestDistance = float.MaxValue;
			List<GameSceneData> scenes = worldData.gameSceneDataList;
			if (scenes != null)
			{
				for (int i = 0; i < scenes.Count; i++)
				{
					GameSceneData scene = scenes[i];
					if (scene == null || string.IsNullOrEmpty(scene.id))
					{
						continue;
					}
					float dx = scene.offset.x - worldPoint.x;
					float dz = scene.offset.z - worldPoint.z;
					float distance = dx * dx + dz * dz;
					if (distance < bestDistance)
					{
						bestDistance = distance;
						bestSceneId = scene.id;
					}
				}
			}
			return bestSceneId;
		}

		/// <summary>
		/// Puts the player on the ground elevation of their current column. Used after a map teleport that
		/// could only approximate the height, and also to recover from any floating state.
		/// </summary>
		internal static bool TrySnapPlayerToGround(PlayerController playerController, string reason, ManualLogSource logger)
		{
			try
			{
				Vector3 position = playerController.MovablePosition;
				// The player's own world position is used directly (no map column involved), so this only needs
				// to find the zone they stand in: first what the game reports, then a collider test.
				WorldZone zone = FindPlayerWorldZone();
				if (zone == null)
				{
					zone = FindZoneForWorldPoint(position.x, position.z);
				}
				if (zone == null)
				{
					logger.LogInfo("Ground snap (" + reason + "): no world zone matched the player position.");
					return false;
				}
				WorldZoneData data = zone.Data;
				string sceneId = (data != null) ? data.gameSceneId : null;
				float groundY = zone.GroundPlaneY;
				Vector3 groundPoint = VisualConsts.ProjectElevationPointToGround(position, groundY);
				float elevationY;
				if (!zone.TryGetBuildElevationY(groundPoint.x, groundPoint.z, out elevationY))
				{
					// Only a real elevation area is a trustworthy height. A zone's ground plane is a designer
					// plane that can sit below the actual terrain (real logs: forest_post reports y 0 while the
					// ground there is at 1.2), and moving the player down to it drops them inside the terrain.
					logger.LogInfo(string.Format(CultureInfo.InvariantCulture, "Ground snap ({0}): no elevation area covers the player position in zone '{1}', keeping the current height of {2:0.00}.", reason, (data != null) ? data.id : "?", position.y));
					return false;
				}
				Vector3 destination = VisualConsts.ProjectGroundPointToElevation(groundPoint, elevationY);
				if ((destination - position).sqrMagnitude < 0.0004f)
				{
					return false;
				}
				playerController.SetPosition(destination, true, true);
				logger.LogInfo(string.Format(CultureInfo.InvariantCulture, "Ground snap ({0}): moved from y {1:0.00} to y {2:0.00} in scene '{3}' via zone '{4}'.", reason, position.y, destination.y, sceneId, (data != null) ? data.id : "?"));
				return true;
			}
			catch (Exception ex)
			{
				logger.LogError("Ground snap failed: " + ex);
				return false;
			}
		}

		/// <summary>
		/// Closes the window that currently shows the map so the teleport fade is not stuck behind an open UI.
		/// Mirrors MapPageWidget.OnPressMapMilestone.
		/// </summary>
		private static void CloseMapHostWindow(MapPageWidget mapPageWidget, ManualLogSource logger)
		{
			CharacterWindow characterWindow = mapPageWidget.GetComponentInParent<CharacterWindow>();
			if (characterWindow == null)
			{
				characterWindow = FindShownCharacterWindow();
			}
			if (characterWindow != null && characterWindow.IsShown)
			{
				characterWindow.Close();
				logger.LogInfo("Map click teleport: closed the character window.");
				return;
			}
			UIMapWindow mapWindow = mapPageWidget.GetComponentInParent<UIMapWindow>();
			if (mapWindow == null)
			{
				mapWindow = FindShownMapWindow();
			}
			if (mapWindow != null && mapWindow.IsShown)
			{
				mapWindow.Close();
				logger.LogInfo("Map click teleport: closed the map window.");
				return;
			}
			logger.LogInfo("Map click teleport: no host window was shown, teleporting without closing any UI.");
		}

		private static CharacterWindow FindShownCharacterWindow()
		{
			CharacterWindow[] windows = Resources.FindObjectsOfTypeAll<CharacterWindow>();
			if (windows == null)
			{
				return null;
			}
			for (int i = 0; i < windows.Length; i++)
			{
				CharacterWindow window = windows[i];
				if (window != null && window.IsShown)
				{
					return window;
				}
			}
			return null;
		}

		private static UIMapWindow FindShownMapWindow()
		{
			UIMapWindow[] windows = Resources.FindObjectsOfTypeAll<UIMapWindow>();
			if (windows == null)
			{
				return null;
			}
			for (int i = 0; i < windows.Length; i++)
			{
				UIMapWindow window = windows[i];
				if (window != null && window.IsShown)
				{
					return window;
				}
			}
			return null;
		}

		internal static string CurrentSceneId()
		{
			PlayerController playerController = MainGame.PlayerController;
			if (playerController == null)
			{
				return null;
			}
			GameScene currentGameScene = playerController.CurrentGameScene;
			if (currentGameScene == null)
			{
				playerController.TryGetCurrentGameScene(out currentGameScene);
			}
			return (currentGameScene != null && currentGameScene.GameSceneData != null) ? currentGameScene.GameSceneData.id : null;
		}

		internal static float MapTiltTan()
		{
			if (GUIElements.Instance == null || GUIElements.Instance.WorldMin == null)
			{
				return DefaultTiltTan;
			}
			return Mathf.Tan(0.017453292f * GUIElements.Instance.WorldMin.rotation.x);
		}
	}
}
