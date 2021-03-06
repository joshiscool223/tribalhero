package src.Map.MiniMapDrawers
{
import flash.display.*;
import flash.geom.*;

import org.aswing.JToggleButton;

import src.Global;
import src.Map.MiniMap.LegendGroups.MiniMapGroupCity;
import src.Map.MiniMap.MiniMapLegend;
import src.Map.MiniMap.MiniMapLegendPanel;
import src.Map.MiniMap.MiniMapRegionObject;
import src.Map.Position;
import src.Map.TileLocator;
import src.Util.StringHelper;

public class MiniMapCityDistance implements IMiniMapObjectDrawer
	{
        private var cityButton: JToggleButton = new JToggleButton();
        private var d100Button: JToggleButton = new JToggleButton();
        private var d200Button: JToggleButton = new JToggleButton();
        private var d300Button: JToggleButton = new JToggleButton();
        private var d400Button: JToggleButton = new JToggleButton();
        private var d500Button: JToggleButton = new JToggleButton();

        private var DEFAULT_COLORS : * = MiniMapGroupCity.DEFAULT_COLORS;

		public function applyObject(obj: MiniMapRegionObject) : void {

			if (Global.map.cities.get(obj.groupId)) {
                if(cityButton.isSelected()) return;
                obj.setIcon(new DOT_SPRITE);
				obj.transform.colorTransform = new ColorTransform();
			} else {
				// Apply the difficulty transformation to the tile
				var point: Position = TileLocator.getScreenMinimapToMapCoord(obj.x, obj.y);
				var distance: int = TileLocator.distance(point.x, point.y, 1, Global.gameContainer.selectedCity.primaryPosition.x, Global.gameContainer.selectedCity.primaryPosition.y, 1);
				var distanceIdx: int;
				if (distance <= 100 && !d100Button.isSelected()) distanceIdx = 4;
				else if (distance > 100 && distance <= 200 && !d200Button.isSelected()) distanceIdx = 3;
				else if (distance > 200 && distance <= 300 && !d300Button.isSelected()) distanceIdx = 2;
				else if (distance > 300 && distance <= 400 && !d400Button.isSelected()) distanceIdx = 1;
				else if (distance > 400 && !d500Button.isSelected())distanceIdx = 0;
                else return;

                obj.setIcon(new DOT_SPRITE);
				obj.transform.colorTransform = new ColorTransform(.5, .5, .5, 1, DEFAULT_COLORS[distanceIdx].r, DEFAULT_COLORS[distanceIdx].g, DEFAULT_COLORS[distanceIdx].b);
			}
		}
		
		public function applyLegend(legend: MiniMapLegendPanel) : void {
			var icon: DisplayObject = new DOT_SPRITE;
            legend.addToggleButton(cityButton,StringHelper.localize("MINIMAP_LEGEND_CITY"),icon);

			icon = new DOT_SPRITE;
			icon.transform.colorTransform = new ColorTransform(.5, .5, .5, 1, DEFAULT_COLORS[4].r, DEFAULT_COLORS[4].g, DEFAULT_COLORS[4].b);
			legend.addToggleButton(d100Button,StringHelper.localize("MINIMAP_LEGEND_DISTANCE_LESS_THAN",100),icon);
			
			icon = new DOT_SPRITE;
			icon.transform.colorTransform = new ColorTransform(.5, .5, .5, 1, DEFAULT_COLORS[3].r, DEFAULT_COLORS[3].g, DEFAULT_COLORS[3].b);
            legend.addToggleButton(d200Button,StringHelper.localize("MINIMAP_LEGEND_DISTANCE_LESS_THAN",200),icon);

			icon = new DOT_SPRITE;
			icon.transform.colorTransform = new ColorTransform(.5, .5, .5, 1, DEFAULT_COLORS[2].r, DEFAULT_COLORS[2].g, DEFAULT_COLORS[2].b);
            legend.addToggleButton(d300Button,StringHelper.localize("MINIMAP_LEGEND_DISTANCE_LESS_THAN",300),icon);

			icon = new DOT_SPRITE;
			icon.transform.colorTransform = new ColorTransform(.5, .5, .5, 1, DEFAULT_COLORS[1].r, DEFAULT_COLORS[1].g, DEFAULT_COLORS[1].b);
            legend.addToggleButton(d400Button,StringHelper.localize("MINIMAP_LEGEND_DISTANCE_LESS_THAN",400),icon);

			icon = new DOT_SPRITE;
			icon.transform.colorTransform = new ColorTransform(.5, .5, .5, 1, DEFAULT_COLORS[0].r, DEFAULT_COLORS[0].g, DEFAULT_COLORS[0].b);
            legend.addToggleButton(d500Button,StringHelper.localize("MINIMAP_LEGEND_DISTANCE_500"),icon);
		}

        public function addOnChangeListener(callback:Function):void {
            cityButton.addActionListener(callback);
            d100Button.addActionListener(callback);
            d200Button.addActionListener(callback);
            d300Button.addActionListener(callback);
            d400Button.addActionListener(callback);
            d500Button.addActionListener(callback);
        }
    }
}