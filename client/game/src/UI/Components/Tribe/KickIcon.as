﻿package src.UI.Components.Tribe
{
    import flash.display.MovieClip;
    import flash.events.MouseEvent;

    import org.aswing.AssetIcon;
    import org.aswing.JOptionPane;

    import src.Global;
    import src.UI.Components.SimpleTooltip;
    import src.UI.Dialog.InfoDialog;
    import src.Util.StringHelper;

    public class KickIcon extends AssetIcon
	{		
		private var icon: MovieClip = new ICON_UNFRIEND();

		public function KickIcon(playerId: int)
		{
			super(icon);

			icon.buttonMode = true;
			icon.mouseEnabled = true;
			new SimpleTooltip(icon, "Kick Out");
			icon.addEventListener(MouseEvent.MOUSE_DOWN, function(e: MouseEvent) : void {		
				InfoDialog.showMessageDialog("Kick tribesman", StringHelper.localize("TRIBE_KICK_WARNING"), function(result: int): void {
					if (result == JOptionPane.YES)
						Global.mapComm.Tribe.kick(playerId);
						
				}, null, true, true, JOptionPane.YES | JOptionPane.NO);
			});
		}

	}

}
