﻿package src.Comm 
{
	import com.adobe.serialization.json.*;
	import flash.events.Event;
	import flash.events.IEventDispatcher;
	import flash.net.*;
	import org.aswing.JPanel;
	import src.Constants;	
	import src.UI.Dialog.InfoDialog;
	import src.Util.Util;
	
	public class GameURLLoader implements IEventDispatcher
	{
		private var loader: URLLoader = new URLLoader();
		public var lastURL: String;
		private var pnlLoading: InfoDialog;
		
		public function GameURLLoader() 
		{
			addEventListener(Event.COMPLETE, onComplete);
		}
		
		public function getData() : String {
			return loader.data;
		}
		
		public function getDataAsXML() : XML {			
			return XML(loader.data);
		}
		
		public function getDataAsObject() : Object {
			return new JSONDecoder(loader.data).getValue();
		}
		
		public function load(path: String, params: Array, includeLoginInfo: Boolean = true, showLoadingMessage: Boolean = true) : void {			
			var request: URLRequest = new URLRequest("http://" + Constants.hostname + path);			
			var variables :URLVariables = new URLVariables();
			
			if (includeLoginInfo) {
				variables.sessionId = Constants.sessionId;
				variables.playerId = Constants.playerId;
			}

			for each (var obj: * in params) {
				if (obj.value is Array) {
					for (var i: int = 0; i < (obj.value as Array).length; i++) {
						variables[obj.key + "[]"] = obj.value[i];
					}
				}
				else {
					variables[obj.key] = obj.value;
				}
			}
			
			request.data = variables;			
			request.method = URLRequestMethod.POST;
			
			if (showLoadingMessage)
				pnlLoading = InfoDialog.showMessageDialog("Tribal Hero", "Loading...", null, null, true, false, 0);			
			
			try {
				lastURL = request.url + "?" + request.data;
				
				if (Constants.debug)
					Util.log(lastURL);
				
				loader.load(request);
			}
			catch (e: Error) {
				loader.dispatchEvent(new Event(Event.COMPLETE));
			}
		}
		
		private function onComplete(e: Event = null): void {
			if (pnlLoading) {
				pnlLoading.getFrame().dispose();				
				pnlLoading = null;
			}
		}
		
		public function addEventListener(type:String, listener:Function, useCapture:Boolean = false, priority:int = 0, useWeakReference:Boolean = false):void{
			loader.addEventListener(type, listener, useCapture, priority);
		}
		
		public function dispatchEvent(evt: Event):Boolean{
			return loader.dispatchEvent(evt);
		}
    
		public function hasEventListener(type:String):Boolean{
			return loader.hasEventListener(type);
		}
		
		public function removeEventListener(type:String, listener:Function, useCapture:Boolean = false):void{
			loader.removeEventListener(type, listener, useCapture);
		}
		
		public function willTrigger(type:String):Boolean {
			return loader.willTrigger(type);
		}	
		
	}

}