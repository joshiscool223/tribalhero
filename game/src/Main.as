﻿package src
{
	import fl.lang.Locale;
	import flash.display.MovieClip;
	import flash.display.StageAlign;
	import flash.display.StageScaleMode;
	import flash.events.*;
	import flash.net.URLLoader;
	import flash.net.URLRequest;
	import flash.ui.ContextMenu;
	import flash.ui.ContextMenuItem;
	import org.aswing.*;
	import src.Comm.*;
	import src.Map.*;
	import src.Objects.Factories.*;
	import src.UI.Dialog.InfoDialog;
	import src.UI.Dialog.InitialCityDialog;
	import src.UI.Dialog.LoginDialog;
	import src.UI.LookAndFeel.GameLookAndFeel;
	import src.Util.*;

	public class Main extends MovieClip
	{
		private var importObjects: ImportObjects;

		private var gameContainer: GameContainer;

		private var map:Map;
		private var miniMap: MiniMap;
		private var frameCounter:FPSCounter;
		public var packetCounter:GeneralCounter;
		private var session:TcpSession;
		private var password: String;
		private var parms: Object;

		private var loginDialog: LoginDialog;

		private var pnlLoading: InfoDialog;
		public var errorAlreadyTriggered: Boolean;

		private var siteVersion: String;
		
		private var uncaughtExceptionHandler: UncaughtExceptionHandler;
		
		public function Main()
		{
			trace("TribalHero v" + Constants.version + "." + Constants.revision);
			
			addEventListener(Event.ADDED_TO_STAGE, init);		
		}  

		public function init(e: Event = null) : void {			
			removeEventListener(Event.ADDED_TO_STAGE, init);		
			
			uncaughtExceptionHandler = new UncaughtExceptionHandler(loaderInfo);
			
			//Init ASWING
			AsWingManager.initAsStandard(stage);
			UIManager.setLookAndFeel(new GameLookAndFeel());

			//Init stage options
			stage.stageFocusRect = false;
			stage.scaleMode = StageScaleMode.NO_SCALE;
			stage.align = StageAlign.TOP_LEFT;
			
			Global.main = this;

			//Init right context menu for debugging
			if (Constants.debug > 0) {
				var fm_menu:ContextMenu = new ContextMenu();
				var dump:ContextMenuItem = new ContextMenuItem("Dump stage");
				dump.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, function(e:Event):void { Util.dumpDisplayObject(stage); } );
				var dumpRegionQueryInfo:ContextMenuItem = new ContextMenuItem("Dump region query info");
				dumpRegionQueryInfo.addEventListener(ContextMenuEvent.MENU_ITEM_SELECT, function(e:Event):void {
					if (!Global.map) return;
					Util.log("Pending regions:" + Util.implode(',', Global.map.pendingRegions));
				} );
				fm_menu.customItems.push(dump);
				fm_menu.customItems.push(dumpRegionQueryInfo);
				contextMenu = fm_menu;
			}

			//Flash params
			parms = loaderInfo.parameters;

			//GameContainer
			Global.gameContainer = gameContainer = new GameContainer();
			addChild(gameContainer);

			//Packet Counter
			if (Constants.debug > 0) {
				packetCounter = new GeneralCounter("pkts");
				packetCounter.y = Constants.screenH - 64;
				addChild(packetCounter);
			}

			//Define login type and perform login action
			if (parms.hostname)
			{
				siteVersion = parms.siteVersion;
				Constants.playerName = parms.playerName;
				Constants.loginKey = parms.lsessid;
				Constants.hostname = parms.hostname;
				loadData();
			}
			else
			{
				showLoginDialog();
			}			
		}

		private function loadData(): void
		{			
			pnlLoading = InfoDialog.showMessageDialog("TribalHero", "Launching the game...", null, null, true, false, 0);
					
			if (Constants.queryData) {
				var loader: URLLoader = new URLLoader();
				loader.addEventListener(Event.COMPLETE, function(e: Event) : void { 
					Constants.objData = XML(e.target.data);
					loadLanguages();
				});
				loader.addEventListener(IOErrorEvent.IO_ERROR, function(e: Event): void {
					onDisconnected();
					showConnectionError(true);
				});
				loader.load(new URLRequest("http://" + Constants.hostname + ":8085/data.xml?" + siteVersion));
			} 
			else
				loadLanguages();
		}
		
		private function loadLanguages():void
		{					
			Locale.setLoadCallback(function(success: Boolean) : void {
				if (!success) {
					onDisconnected();
					showConnectionError(true);
				}
				else doConnect();
			});
			Locale.addXMLPath(Constants.defLang, "http://" + Constants.hostname + ":8085/Game_" + Constants.defLang + ".xml?" + siteVersion);
			Locale.setDefaultLang(Constants.defLang);				
			Locale.loadLanguageXML(Constants.defLang);
		}

		public function doConnect():void
		{
			errorAlreadyTriggered = false;
			
			session = new TcpSession();
			session.setConnect(onConnected);
			session.setLogin(onLogin);
			session.setDisconnect(onDisconnected);
			session.setSecurityErrorCallback(onSecurityError);
			session.connect(Constants.hostname);
		}

		public function showLoginDialog():void
		{
			gameContainer.closeAllFrames();
			loginDialog = new LoginDialog(onConnect);
			loginDialog.show();
		}

		public function onConnect(sender: LoginDialog):void
		{
			Constants.username = sender.getTxtUsername().getText();
			password = sender.getTxtPassword().getText();
			Constants.hostname = sender.getTxtAddress().getText();

			loadData();
		}

		public function onSecurityError(event: SecurityErrorEvent):void
		{
			//if (pnlLoading) pnlLoading.getFrame().dispose();

			//InfoDialog.showMessageDialog("Security Error", event.toString());
			Util.log("Security error " + event.toString());
			
			if (session && session.hasLoginSuccess()) 
				return;
	
			onDisconnected();
		}

		public function onDisconnected(event: Event = null):void
		{			
			var wasStillLoading: Boolean = session == null || !session.hasLoginSuccess();
			
			if (pnlLoading)
				pnlLoading.getFrame().dispose();
		
			gameContainer.dispose();			
			if (Global.mapComm) Global.mapComm.dispose();
			
			Global.mapComm = null;
			Global.map = null;
			session = null;

			if (!errorAlreadyTriggered) {
				showConnectionError(wasStillLoading);
			}
		}
		
		public function showConnectionError(wasStillLoading: Boolean) : void {
			if (parms.hostname) InfoDialog.showMessageDialog("Connection Lost", (wasStillLoading ? "Unable to connect to server" : "Connection to Server Lost") + ". Refresh the page to rejoin the battle.", null, null, true, false, 1, true);
			else InfoDialog.showMessageDialog("Connection Lost", (wasStillLoading ? "Unable to connect to server." : "Connection to Server Lost."), function(result: int):void { showLoginDialog(); }, null, true, false, 1, true);			
		}

		public function onConnected(event: Event, connected: Boolean):void
		{
			if (pnlLoading) pnlLoading.getFrame().dispose();

			if (!connected) 
				showConnectionError(true);		
			else
			{
				Global.mapComm = new MapComm(session);

				if (Constants.loginKey) session.login(true, Constants.playerName, Constants.loginKey);
				else session.login(false, Constants.username, password);
			}

			password = '';
		}

		public function onLogin(packet: Packet):void
		{
			if (MapComm.tryShowError(packet, function(result: int) : void { showLoginDialog(); } , true)) {				
				errorAlreadyTriggered = true;
				return;
			}

			session.setLoginSuccess(true);

			if (loginDialog != null) loginDialog.getFrame().dispose();

			var newPlayer: Boolean = Global.mapComm.General.onLogin(packet);

			if (!newPlayer) {
				completeLogin(packet);
			}
			else {
				// Need to make the createInitialCity static and pass in the session
				var createCityDialog: InitialCityDialog = new InitialCityDialog(function(sender: InitialCityDialog): void {
					Global.mapComm.General.createInitialCity(sender.getCityName(), completeLogin);
				});

				createCityDialog.show();
			}
		}

		public function onReceiveXML(e: Event):void
		{
			var str: String = e.target.data;

			Constants.objData = XML(e.target.data);

			doConnect();
		}

		public function completeLogin(packet: Packet):void
		{
			Global.map = map = new Map();
			miniMap = new MiniMap(Constants.miniMapScreenW, Constants.miniMapScreenH);
			
			map.usernames.players.add(new Username(Constants.playerId, Constants.playerName));
			map.setTimeDelta(Constants.timeDelta);		
			
			EffectReqFactory.init(map, Constants.objData);
			PropertyFactory.init(map, Constants.objData);
			StructureFactory.init(map, Constants.objData);
			TechnologyFactory.init(map, Constants.objData);
			UnitFactory.init(map, Constants.objData);
			WorkerFactory.init(map, Constants.objData);
			ObjectFactory.init(map, Constants.objData);
			
			Constants.objData = <Data></Data>;

			gameContainer.show();
			Global.mapComm.General.readLoginInfo(packet);
			gameContainer.setMap(map, miniMap);

			if (Constants.debug > 0) {
				if (frameCounter)
					removeChild(frameCounter);

				frameCounter = new FPSCounter();
				frameCounter.y = Constants.screenH - 32;
				addChild(frameCounter);
			}
		}

		public function onReceive(packet: Packet):void
		{
			if (Constants.debug >= 2)
			{
				Util.log("Received packet to main processor");
				Util.log(packet.toString());
			}
		}

		private function resizeHandler(event:Event):void {
			Util.log("resizeHandler: " + event);
			Util.log("stageWidth: " + stage.stageWidth + " stageHeight: " + stage.stageHeight);
		}
	}
}

