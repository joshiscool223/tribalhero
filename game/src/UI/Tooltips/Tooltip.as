/**
 * ...
 * @author Default
 * @version 0.1
 */

package src.UI.Tooltips {
	import flash.display.DisplayObject;
	import flash.events.Event;
	import flash.events.MouseEvent;
	import org.aswing.border.EmptyBorder;
	import org.aswing.Component;
	import org.aswing.event.AWEvent;
	import org.aswing.geom.IntPoint;
	import org.aswing.Insets;
	import src.Global;
	import src.Map.Camera;
	import src.UI.GameJBox;

	public class Tooltip
	{
		protected var ui: GameJBox = new GameJBox();

		protected var viewObj: DisplayObject;
		
		private var position: IntPoint;

		public function Tooltip() {
			ui.setFocusable(false);
			ui.setBorder(new EmptyBorder(null, new Insets(3, 10, 3, 10)));		
		}

		public function getUI(): GameJBox {
			return ui;
		}
		
		public function bind(obj: DisplayObject) : void {
			obj.addEventListener(MouseEvent.MOUSE_MOVE, function(e: Event): void {
				show(obj);
			});
			obj.addEventListener(MouseEvent.MOUSE_OUT, function(e: Event): void {
				hide();
			});			
		}

		public function show(obj: DisplayObject):void
		{
			Global.map.camera.addEventListener(Camera.ON_MOVE, onCameraMove);
			
			if (this.viewObj == null || this.viewObj != obj) {
				this.viewObj = obj;
				viewObj.addEventListener(Event.REMOVED_FROM_STAGE, parentHidden);
				viewObj.addEventListener(MouseEvent.MOUSE_DOWN, parentHidden);				
				
				showFrame();
			}

			this.position = new IntPoint(ui.getFrame().stage.mouseX, ui.getFrame().stage.mouseY);			
			adjustPosition();
		}
		
		public function showFixed(position: IntPoint):void
		{			
			this.position = position;
			showFrame();
			adjustPosition();
		}		
		
		protected function showFrame(): void {
			ui.addEventListener(AWEvent.PAINT, onPaint);
			ui.show();
			
			if (!mouseInteractive()) {
				ui.getFrame().parent.mouseEnabled = false;
				ui.getFrame().parent.mouseChildren = false;
				ui.getFrame().parent.tabEnabled = false;
				ui.getFrame().setFocusable(false);
			}
		}
		
		protected function mouseInteractive(): Boolean {
			return false;
		}
		
		private function onCameraMove(e: Event): void {
			// Hide if camera is moving
			hide();
		}

		// We need this function since the size is wrong of the component until it has been painted
		private function onPaint(e: AWEvent): void {
			ui.removeEventListener(AWEvent.PAINT, onPaint);
			ui.getFrame().pack();
			adjustPosition();
		}

		private function parentHidden(e: Event) : void {
			Global.map.camera.removeEventListener(Camera.ON_MOVE, onCameraMove);
			hide();
		}

		private function adjustPosition() : void
		{
			if (ui.getFrame() == null || ui.getFrame().stage == null || ui.getComBounds().width == 0) {
				return;
			}

			if (viewObj is Component) {
				(viewObj as Component).requestFocus();
			}

			var posX: Number = position.x;
			var posY: Number = position.y;

			var boxX: Number = posX;
			var boxY: Number = posY;

			var boxWidth: Number = ui.getFrame().getPreferredWidth();
			var boxHeight: Number = ui.getFrame().getPreferredHeight();

			var stageWidth: Number = ui.getFrame().stage.stageWidth;
			var stageHeight: Number = ui.getFrame().stage.stageHeight;

			if (boxX + boxWidth > stageWidth) {
				boxX = posX - boxWidth + 5;
			}

			if (boxY + boxHeight > stageHeight) {
				boxY = posY - boxHeight + 5;
			}

			if (boxY < 0) {
				boxY = 0;
			}

			if (boxX < 0) {
				boxX = 0;
			}

			ui.getFrame().setGlobalLocation(new IntPoint(boxX, boxY));
		}

		public function hide():void
		{
			if (this.viewObj != null)
			{
				this.viewObj.removeEventListener(Event.REMOVED_FROM_STAGE, parentHidden);
				this.viewObj.removeEventListener(MouseEvent.MOUSE_DOWN, parentHidden);
				this.viewObj = null;
			}

			if (ui.getFrame())
				ui.getFrame().dispose();
		}
	}

}

