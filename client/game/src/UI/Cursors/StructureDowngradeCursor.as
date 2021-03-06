﻿package src.UI.Cursors {
    import flash.display.MovieClip;
    import flash.events.Event;
    import flash.events.MouseEvent;
    import flash.geom.Point;

    import org.aswing.JOptionPane;

    import src.Global;
    import src.Map.City;
    import src.Map.ScreenPosition;
    import src.Map.TileLocator;
    import src.Objects.IDisposable;
    import src.Objects.ObjectContainer;
    import src.Objects.StructureObject;
    import src.UI.Components.GroundCircle;
    import src.UI.Dialog.InfoDialog;
    import src.UI.Sidebars.CursorCancel.CursorCancelSidebar;

    public class StructureDowngradeCursor extends MovieClip implements IDisposable
	{
		private var objPosition: ScreenPosition = new ScreenPosition();

		private var originPoint: Point;

		private var cursor: GroundCircle;

		private var city: City;

		private var highlightedObj: StructureObject;

		private var parentObj: StructureObject;

		public function StructureDowngradeCursor(parentObj: StructureObject)
		{
			doubleClickEnabled = true;

			this.parentObj = parentObj;

			city = Global.map.cities.get(parentObj.cityId);

			Global.map.selectObject(null);
			Global.map.objContainer.resetObjects();

			cursor = new GroundCircle(0);
			cursor.alpha = 0.6;

			Global.map.objContainer.addObject(cursor, ObjectContainer.LOWER);

			var sidebar: CursorCancelSidebar = new CursorCancelSidebar(parentObj);
			Global.gameContainer.setSidebar(sidebar);

			addEventListener(Event.ADDED_TO_STAGE, onAddedToStage);
			addEventListener(MouseEvent.DOUBLE_CLICK, onMouseDoubleClick);
			addEventListener(MouseEvent.CLICK, onMouseStop, true);
			addEventListener(MouseEvent.MOUSE_MOVE, onMouseMove);
			addEventListener(MouseEvent.MOUSE_OVER, onMouseStop);
			addEventListener(MouseEvent.MOUSE_DOWN, onMouseDown);

			Global.gameContainer.setOverlaySprite(this);
		}

		public function onAddedToStage(e: Event):void
		{
			moveTo(stage.mouseX, stage.mouseY);
		}

		public function dispose():void
		{
			if (cursor != null)
			{
				Global.map.objContainer.removeObject(cursor, ObjectContainer.LOWER);
				cursor.dispose();
			}

			Global.gameContainer.message.hide();

			if (highlightedObj)
			{
				highlightedObj.setHighlighted(false);
				highlightedObj = null;
			}
		}

		public function onMouseStop(event: MouseEvent):void
		{
			event.stopImmediatePropagation();
		}

		public function onMouseDoubleClick(event: MouseEvent):void
		{
			if (Point.distance(TileLocator.getPointWithZoomFactor(event.stageX, event.stageY), originPoint) > 4) return;

			event.stopImmediatePropagation();

			var objects: Array = Global.map.regions.getObjectsInTile(objPosition.toPosition(), StructureObject);

			if (objects.length == 0) return;

			var gameObj: StructureObject = objects[0];

			if (gameObj.cityId != parentObj.cityId) return;

			InfoDialog.showMessageDialog("Confirm", "Are you sure? Your structure is about to be completely removed.", function(result: int): void {				
				if (result == JOptionPane.YES)						
					Global.mapComm.Objects.downgrade(city.id, parentObj.objectId, gameObj.objectId);
					
			}, null, true, true, JOptionPane.YES | JOptionPane.NO);

			Global.gameContainer.setOverlaySprite(null);
			Global.gameContainer.setSidebar(null);
			Global.map.selectObject(null);
		}

		public function onMouseDown(event: MouseEvent):void
		{
			originPoint = TileLocator.getPointWithZoomFactor(event.stageX, event.stageY);
		}

		public function onMouseMove(event: MouseEvent):void
		{
			if (event.buttonDown) {
				return;
			}

			var mousePos: Point = TileLocator.getPointWithZoomFactor(event.stageX, event.stageY);
			moveTo(mousePos.x, mousePos.y);
		}

		public function moveTo(x: int, y: int):void
		{
			var pos: ScreenPosition = TileLocator.getActualCoord(Global.gameContainer.camera.currentPosition.x + Math.max(x, 0), Global.gameContainer.camera.currentPosition.y + Math.max(y, 0));

            // Don't do anything if position hasn't changed since last time the mouse moved
            if (pos.equals(objPosition)) {
                return;
            }

            Global.map.objContainer.removeObject(cursor, ObjectContainer.LOWER);

            objPosition = pos;

            cursor.x = cursor.primaryPosition.x = pos.x;
            cursor.y = cursor.primaryPosition.y = pos.y;
            Global.map.objContainer.addObject(cursor, ObjectContainer.LOWER);

            if (highlightedObj) {
                highlightedObj.setHighlighted(false);
                highlightedObj = null;
            }

            var objects: Array = Global.map.regions.getObjectsInTile(objPosition.toPosition(), StructureObject);
            if (objects.length == 0 || objects[0].cityId != parentObj.cityId) {
                Global.gameContainer.message.showMessage("Choose a structure to remove");
                return;
            }

            var gameObj: StructureObject = objects[0];
            gameObj.setHighlighted(true);
            highlightedObj = gameObj;

            Global.gameContainer.message.showMessage("Double click to remove this structure.");
		}

    }

}

