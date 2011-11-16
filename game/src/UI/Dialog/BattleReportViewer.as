﻿package src.UI.Dialog
{
	import flash.events.*;
	import org.aswing.*;
	import org.aswing.border.*;
	import org.aswing.colorchooser.*;
	import org.aswing.ext.*;
	import org.aswing.geom.*;
	import org.aswing.plaf.*;
	import src.*;
	import src.Comm.*;
	import src.Objects.*;
	import src.Objects.Troop.*;
	import src.UI.*;
	import src.UI.Components.*;
	import src.UI.Components.BattleReport.*;
	import src.UI.LookAndFeel.*;
	import src.UI.Tooltips.*;
	import src.Util.*;

	public class BattleReportViewer extends GameJPanel
	{

		private var pnlSnapshotsScroll: JScrollPane;
		private var pnlSnapshots: JPanel;
		private var pnlFooter:JPanel;
		private var chkViewAll:JCheckBox;
		private var pnlResources:JPanel;

		private var data: Object;

		private var loader: GameURLLoader = new GameURLLoader();
		private var id: int;
		private var isLocal: Boolean;
		private var playerNameFilter: String;
		
		public var refreshOnClose: Boolean;

		public function BattleReportViewer(id: int, playerNameFilter: String, isLocal: Boolean)
		{
			this.id = id;
			this.isLocal = isLocal;
			this.playerNameFilter = playerNameFilter;

			createUI();

			chkViewAll.addActionListener(function(): void {
				renderSnapshots();
			});

			loader.addEventListener(Event.COMPLETE, onLoaded);	
		}

		private function load(playerNameFilter: String) : void {			
			if (isLocal)
				Global.mapComm.BattleReport.viewLocal(loader, id, playerNameFilter);
			else
				Global.mapComm.BattleReport.viewRemote(loader, id, playerNameFilter);
		}

		private function onLoaded(e: Event) : void {
			try
			{				
				data = loader.getDataAsObject();
			}
			catch (e: Error) {
				InfoDialog.showMessageDialog("Error", "Unable to query report. Refresh the page if this problem persists");
				return;
			}

			if (data.outcomeOnly == false) calculateCountDeltas();
			
			refreshOnClose = data.refreshOnClose;
			renderSnapshots();

			// Show resources gained if applicable
			if (data.loot) {
				var loot: Resources = new Resources(data.loot.crop, data.loot.gold, data.loot.iron, data.loot.wood, 0);
				var bonus: Resources = new Resources(data.bonus.crop, data.bonus.gold, data.bonus.iron, data.bonus.wood, 0);
				var total: Resources = Resources.sum(loot, bonus);
				pnlResources.append(new ResourcesPanel(total, null, false, false));
				pnlFooter.append(pnlResources);
				new BattleLootTooltip(pnlResources, loot, bonus);
			}
			pack();
		}

		private function calculateCountDeltas() : void {
			var cache: Array = new Array();
			for (var i: int = 0; i < data.snapshots.length; i++) {
				for (var j: int = 0; j < data.snapshots[i].attackers.length; j++) setTroopDeltas(cache, data.snapshots[i].attackers[j], true);				
				for (j = 0; j < data.snapshots[i].defenders.length; j++) setTroopDeltas(cache, data.snapshots[i].defenders[j], false);
			}
		}

		private function setTroopDeltas(cache: Array, troopSnapshot: *, isAttack: Boolean) : void {
			var troop: *;
			for (var k: int = 0; k < cache.length; k++) {
				if (cache[k].groupId == troopSnapshot.groupId) {
					troop = cache[k];
					break;
				}
			}
			
			if (!troop) {
				troop = findInitialTroop(isAttack, troopSnapshot.groupId);
				if (troop == null) return;
				cache.push(troop);
			}

			setDeltas(troop.units, troopSnapshot.units);
		}

		private function setDeltas(initial: * , ending: *) : void {
			for (var i: int = 0; i < ending.length; i++) {
				if (ending[i].id == 0) continue;

				for (var j: int = 0; j < initial.length; j++) {
					if (ending[i].id != initial[j].id) continue;

					ending[i].delta = ending[i].count - initial[j].count;
					break;
				}
			}
		}

		private function findInitialTroop(isAttack: Boolean, groupId: int) : * {
			for (var i: int = 0; i < data.snapshots.length; i++) {
				if (isAttack) {
					for (var j: int = 0; j < data.snapshots[i].attackers.length; j++) {
						if (data.snapshots[i].attackers[j].groupId == groupId) return data.snapshots[i].attackers[j];
					}
				} else {
					for (j = 0; j < data.snapshots[i].defenders.length; j++) {
						if (data.snapshots[i].defenders[j].groupId == groupId) return data.snapshots[i].defenders[j];
					}
				}
			}

			return null;
		}

		private function canSeeSnapshot(snapshot: Object) : Boolean {
			
			for each (var event: * in snapshot.eventsRaw) {
				if (event.type != TroopStub.REPORT_STATE_STAYING && event.groupId == data.groupId) return true;
			}

			return false;
		}

		private function renderSnapshots() : void {

			chkViewAll.setText("View complete report");

			pnlSnapshots.removeAll();
			var showHiddenReportMessage: Boolean = false;
			for each (var snapshot: Object in data.snapshots) {
				// Don't show this snapshot if we arent viewing them all
				// if it doesnt pertain to this player OR show them all regardless if this is an outcome only reports
				if (data.outcomeOnly == false && !chkViewAll.isSelected()) {
					if (!canSeeSnapshot(snapshot)) {
						if (!showHiddenReportMessage) {							
							showHiddenReportMessage = true;
						}
						continue;
					}
				}

				if (data.outcomeOnly)  {
					pnlSnapshots.append(new OutcomeSnapshot(snapshot));

					//Resize accordingly
					setPreferredSize(new IntDimension(600, 520));
					getFrame().pack();
					Util.centerFrame(getFrame());
				}
				else {
					pnlSnapshotsScroll.setBorder(new SimpleTitledBorder(null, ""));
					pnlSnapshots.append(new Snapshot(snapshot));

					//Resize accordingly
					setPreferredSize(new IntDimension(865, 520));
					getFrame().pack();
					Util.centerFrame(getFrame());
				}
			}
			
			var headerFont: ASFontUIResource = GameLookAndFeel.getClassAttribute("darkHeader", "Label.font");
			if (data.outcomeOnly) {
				pnlSnapshotsScroll.setBorder(new SimpleTitledBorder(null, "Your troop was unable to report the enemies. You can only see a partial report.", AsWingConstants.TOP, AsWingConstants.CENTER, 0, headerFont));
			}
			else if (showHiddenReportMessage) {
				pnlSnapshotsScroll.setBorder(new SimpleTitledBorder(null, "You are currently viewing a summary of the battle report. To view the entire report, click 'View complete report' below", AsWingConstants.TOP, AsWingConstants.CENTER, 0, headerFont));
			}
			else {
				pnlSnapshotsScroll.setBorder(new SimpleTitledBorder(null, ""));
			}
			
			chkViewAll.setVisible(showHiddenReportMessage);
		}

		public function show(owner:* = null, modal:Boolean = true, onClose: Function = null):JFrame
		{
			super.showSelf(owner, modal, onClose);
			Global.gameContainer.showFrame(frame);
			frame.setTitle("Battle Report Viewer");
			
			load(playerNameFilter);
			
			return frame;
		}

		public function createUI() : void {
			var layout0:BorderLayout = new BorderLayout();
			setLayout(layout0);

			pnlSnapshots = new JPanel(new SoftBoxLayout(SoftBoxLayout.Y_AXIS, 25));

			pnlSnapshotsScroll = new JScrollPane(pnlSnapshots);
			pnlSnapshotsScroll.setConstraints("Center");

			pnlFooter = new JPanel();
			pnlFooter.setLayout(new BorderLayout(15));
			pnlFooter.setConstraints("South");

			pnlResources = new JPanel();
			pnlResources.setLayout(new FlowLayout(AsWingConstants.LEFT, 8));
			pnlResources.setConstraints("East");

			chkViewAll = new JCheckBox("");
			chkViewAll.setConstraints("West");

			pnlFooter.append(chkViewAll);

			//component layoution
			append(pnlSnapshotsScroll);
			append(pnlFooter);

		}
	}

}
