﻿package src.UI.Dialog 
{
    import mx.utils.StringUtil;

    import org.aswing.*;
    import org.aswing.event.*;
    import org.aswing.ext.*;
    import org.aswing.geom.*;
    import org.aswing.table.*;

    import src.*;
    import src.UI.*;
    import src.UI.Components.TableCells.*;
    import src.UI.Components.Tribe.*;
    import src.UI.LookAndFeel.*;
    import src.Util.DateUtil;
    import src.Util.StringHelper;

    public class TribePublicProfileDialog extends GameJPanel
	{
		private var profileData: * ;
		
		private var pnlHeader: JPanel;
		private var pnlInfoContainer: JPanel;
		private var lblTribeName: JLabel;
		
		private var pnlTabs: JTabbedPane;
		private var pnlInfoTabs: JTabbedPane;
		
		public function TribePublicProfileDialog(profileData: *) 
		{
			this.profileData = profileData;
			
			createUI();
		}

		public function show(owner:* = null, modal:Boolean = true, onClose: Function = null):JFrame 
		{
			super.showSelf(owner, modal, onClose);
			Global.gameContainer.closeAllFramesByType(TribePublicProfileDialog);
			Global.gameContainer.showFrame(frame);
			return frame;
		}
		
		private function addInfo(form: Form, title: String, text: *) : void {
			var rowTitle: JLabel = new JLabel(title, null, AsWingConstants.LEFT);
			rowTitle.setName("title");

			var label: JLabel = new JLabel(text, null, AsWingConstants.LEFT);
			label.setName("value");

			form.addRow(rowTitle, label);
		}
						
		private function createUI():void {
			setPreferredSize(new IntDimension(Math.min(400, Constants.screenW - GameJImagePanelBackground.getFrameWidth()) , Math.min(600, Constants.screenH - GameJImagePanelBackground.getFrameHeight())));
			
			title = "Tribe Profile - " + profileData.tribeName;
			setLayout(new BorderLayout(0, 15));
			
			// Header panel
			pnlHeader = new JPanel(new SoftBoxLayout(SoftBoxLayout.Y_AXIS, 5));
			pnlHeader.setConstraints("North");		
			lblTribeName = new JLabel(profileData.tribeName, null, AsWingConstants.LEFT);
			GameLookAndFeel.changeClass(lblTribeName, "darkSectionHeader");
			
			var stats: Form = new Form();
			
			var establishedDiff:int = Global.map.getServerTime() - profileData.created;
			addInfo(stats, StringHelper.localize("STR_LEVEL"), profileData.level);
			addInfo(stats, StringHelper.localize("STR_ESTABLISHED"), DateUtil.niceDays(establishedDiff));
			
			pnlHeader.appendAll(lblTribeName, stats);
			
			// Tab panel
			pnlTabs = new JTabbedPane();
			pnlTabs.setPreferredHeight(600);
			pnlTabs.setConstraints("Center");

			// Append tabs			
            if (profileData.publicDescription) {
                pnlTabs.appendTab(createAnnouncementTab(), StringUtil.substitute("Announcement", profileData.members.length));
            }
            
			pnlTabs.appendTab(createMembersTab(), StringUtil.substitute("Members ({0})", profileData.members.length));            
            pnlTabs.appendTab(createStrongholdsTab(), StringUtil.substitute("Strongholds ({0})", profileData.strongholds.length));
			
			// Append main panels
			appendAll(pnlHeader, pnlTabs);
		}
        
		private function createAnnouncementTab() : Container {			
			var announcement: MultilineLabel = new MultilineLabel(profileData.publicDescription);
			GameLookAndFeel.changeClass(announcement, "Message");
			return new JScrollPane(announcement);
		}                
        
		private function createMembersTab() : Container {
			var modelMembers: VectorListModel = new VectorListModel(profileData.members);
			var tableMembers: JTable = new JTable(new PropertyTableModel(
				modelMembers, 
				["Player", "Rank"],
				[".", "rank"],
				[null, new TribeRankTranslator(profileData.ranks)]
			));			
			tableMembers.addEventListener(TableCellEditEvent.EDITING_STARTED, function(e: TableCellEditEvent) : void {
				tableMembers.getCellEditor().cancelCellEditing();
			});			
			tableMembers.setRowSelectionAllowed(false);
			tableMembers.setAutoResizeMode(JTable.AUTO_RESIZE_OFF);
			tableMembers.getColumnAt(0).setPreferredWidth(145);
			tableMembers.getColumnAt(0).setCellFactory(new GeneralTableCellFactory(PlayerLabelCell));
			tableMembers.getColumnAt(1).setPreferredWidth(145);
			
			var scrollMembers: JScrollPane = new JScrollPane(tableMembers, JScrollPane.SCROLLBAR_ALWAYS, JScrollPane.SCROLLBAR_NEVER);
			
			return scrollMembers;
		}        
		
		private function createStrongholdsTab() : Container {
			var tableModel: VectorListModel = new VectorListModel(profileData.strongholds);
			var table: JTable = new JTable(new PropertyTableModel(
				tableModel, 
				["Name", "Level"],
				[".", "strongholdLevel"],
				[null, null]
			));			
			table.addEventListener(TableCellEditEvent.EDITING_STARTED, function(e: TableCellEditEvent) : void {
				table.getCellEditor().cancelCellEditing();
			});			
			table.setRowSelectionAllowed(false);
			table.setAutoResizeMode(JTable.AUTO_RESIZE_OFF);
			table.getColumnAt(0).setPreferredWidth(145);
			table.getColumnAt(0).setCellFactory(new GeneralTableCellFactory(StrongholdLabelCell));
			table.getColumnAt(1).setPreferredWidth(145);
			
			return new JScrollPane(table, JScrollPane.SCROLLBAR_ALWAYS, JScrollPane.SCROLLBAR_NEVER);
		}
	}
	
}