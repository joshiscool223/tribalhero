﻿
package src.UI.Cursors {
	import flash.events.Event;
	import flash.geom.ColorTransform;
	import flash.geom.Point;
	import src.Global;
	import src.Map.City;
	import src.Map.Map;
	import src.Map.MapUtil;
	import flash.display.MovieClip;
	import flash.events.MouseEvent;
	import src.Map.Region;
	import src.Map.RegionList;
	import src.Map.RoadPathFinder;
	import src.Objects.Factories.*;
	import src.Objects.GameObject;
	import src.Objects.IDisposable;
	import src.Objects.ObjectContainer;
	import src.Objects.SimpleObject;
	import src.Objects.StructureObject;
	import src.UI.Components.GroundCallbackCircle;
	import src.UI.Components.GroundCircle;
	import src.UI.Sidebars.CursorCancel.CursorCancelSidebar;

	public class BuildRoadCursor extends MovieClip implements IDisposable
	{
		private var map: Map;
		private var objX: int;
		private var objY: int;
		private var city: City;

		private var originPoint: Point;

		private var cursor: SimpleObject;
		private var buildableArea: GroundCallbackCircle;
		private var parentObj: GameObject;

		public function BuildRoadCursor() { }

		public function init(map: Map, parentObject: GameObject):void
		{
			doubleClickEnabled = true;

			this.parentObj = parentObject;
			this.map = map;

			city = map.cities.get(parentObj.cityId);

			src.Global.gameContainer.setOverlaySprite(this);
			map.selectObject(null);

			cursor = new GroundCircle(0);
			cursor.alpha = 0.7;

			buildableArea = new GroundCallbackCircle(city.radius - 1, validateTileCallback);
			buildableArea.alpha = 0.3;
			var point: Point = MapUtil.getScreenCoord(city.MainBuilding.x, city.MainBuilding.y);
			buildableArea.setX(point.x); buildableArea.setY(point.y);
			buildableArea.moveWithCamera(src.Global.gameContainer.camera);
			map.objContainer.addObject(buildableArea, ObjectContainer.LOWER);

			var sidebar: CursorCancelSidebar = new CursorCancelSidebar(parentObj);
			src.Global.gameContainer.setSidebar(sidebar);

			addEventListener(MouseEvent.DOUBLE_CLICK, onMouseDoubleClick);
			addEventListener(MouseEvent.CLICK, onMouseStop, true);
			addEventListener(MouseEvent.MOUSE_MOVE, onMouseMove);
			addEventListener(MouseEvent.MOUSE_OVER, onMouseStop);
			addEventListener(MouseEvent.MOUSE_DOWN, onMouseDown);
			
			Global.map.regions.addEventListener(RegionList.REGION_UPDATED, update);

			src.Global.gameContainer.message.showMessage("Double click on the green squares to build roads.");
		}

		public function update(e: Event = null) : void {
			buildableArea.redraw();
			
		}
		
		public function dispose():void
		{
			Global.map.regions.removeEventListener(RegionList.REGION_UPDATED, update);
			
			src.Global.gameContainer.message.hide();

			if (cursor != null)
			{
				if (cursor.stage != null) map.objContainer.removeObject(cursor);
				if (buildableArea.stage != null) map.objContainer.removeObject(buildableArea, ObjectContainer.LOWER);
			}
		}

		private function showCursors() : void {
			if (cursor) cursor.visible = true;
		}

		private function hideCursors() : void {
			if (cursor) cursor.visible = false;
		}

		public function onMouseStop(event: MouseEvent):void
		{
			event.stopImmediatePropagation();
		}

		public function onMouseDoubleClick(event: MouseEvent):void
		{
			if (Point.distance(new Point(event.stageX, event.stageY), originPoint) > city.radius)
			return;

			event.stopImmediatePropagation();

			var pos: Point = MapUtil.getMapCoord(objX, objY);
			Global.mapComm.Region.buildRoad(parentObj.cityId, pos.x, pos.y);
		}

		public function onMouseDown(event: MouseEvent):void
		{
			originPoint = new Point(event.stageX, event.stageY);
		}

		public function onMouseMove(event: MouseEvent) : void
		{
			if (event.buttonDown)
			return;

			var pos: Point = MapUtil.getActualCoord(src.Global.gameContainer.camera.x + Math.max(event.stageX, 0), src.Global.gameContainer.camera.y + Math.max(event.stageY, 0));

			if (pos.x != objX || pos.y != objY)
			{
				objX = pos.x;
				objY = pos.y;

				//Object cursor
				if (cursor.stage != null) map.objContainer.removeObject(cursor);
				cursor.setX(objX); cursor.setY(objY);
				cursor.moveWithCamera(src.Global.gameContainer.camera);
				map.objContainer.addObject(cursor);
				validateBuilding();
			}
		}

		private function validateTile(screenPos: Point) : Boolean {
			var mapPos: Point = MapUtil.getMapCoord(screenPos.x, screenPos.y);
			var tileType: int = map.regions.getTileAt(mapPos.x, mapPos.y);

			if (RoadPathFinder.isRoad(tileType)) return false;

			if (!ObjectFactory.isType("TileBuildable", tileType)) return false;

			if (Global.map.regions.getObjectsAt(screenPos.x, screenPos.y, StructureObject).length > 0) return false;

			// Make sure there is a road next to this tile
			var hasRoad: Boolean = false;
			MapUtil.foreach_object(mapPos.x, mapPos.y, 1, function(x1: int, y1: int, custom: *) : Boolean
			{
				if (MapUtil.radiusDistance(mapPos.x, mapPos.y, x1, y1) != 1) return true;

				if (RoadPathFinder.isRoadByMapPosition(x1, y1))
				{
					hasRoad = true;
					return false;
				}

				return true;
			}, false, null);

			if (!hasRoad) return false;

			return true;
		}

		private function validateTileCallback(x: int, y: int, isCenter: Boolean) : * {

			// Get the screen position of the main building then we'll add the current tile x and y to get the point of this tile on the screen
			var point: Point = MapUtil.getScreenCoord(city.MainBuilding.x, city.MainBuilding.y);

			if (!validateTile(new Point(point.x + x, point.y + y))) return false;

			return new ColorTransform(1.0, 1.0, 1.0, 1.0, 0, 100);
		}

		public function validateBuilding():void
		{
			var msg: XML;

			var city: City = map.cities.get(parentObj.cityId);
			var mapObjPos: Point = MapUtil.getMapCoord(objX, objY);

			// Check if cursor is inside city walls
			if (city != null && MapUtil.distance(city.MainBuilding.x, city.MainBuilding.y, mapObjPos.x, mapObjPos.y) >= city.radius) {
				hideCursors();
			}
			// Perform other validations
			else if (!validateTile(new Point(objX, objY))) {
				hideCursors();
			}
			else {
				showCursors();
			}
		}
	}
}

