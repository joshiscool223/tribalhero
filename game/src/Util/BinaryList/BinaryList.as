﻿package src.Util.BinaryList {
	import flash.events.EventDispatcher;
	import src.Util.Util;

	/**
	 * ...
	 * @author Default
	 */
	public class BinaryList extends EventDispatcher {

		private var sortFunc: Function;
		private var compareFunc: Function;

		private var list: Array = new Array();

		public function BinaryList(sortFunc: Function, compareFunc: Function) {
			this.sortFunc = sortFunc;
			this.compareFunc = compareFunc;
		}

		public function toArray(): Array
		{
			return list;
		}

		public function each(): Array
		{
			return list;
		}

		public function clear():void
		{
			list = new Array();
			super.dispatchEvent(new BinaryListEvent(BinaryListEvent.CHANGED));
		}

		public function add(obj: *, resort: Boolean = true):void
		{
			list.push(obj);

			if (resort)
			sort();

			super.dispatchEvent(new BinaryListEvent(BinaryListEvent.ADDED, obj));
		}

		public function update(obj: *, val: Array) : void
		{
			//find the index and change it
			var idx: int = Util.binarySearch(list, compareFunc, val);

			if (idx == -1)
			return;

			list[idx] = obj;

			super.dispatchEvent(new BinaryListEvent(BinaryListEvent.CHANGED));
			super.dispatchEvent(new BinaryListEvent(BinaryListEvent.UPDATED, obj));
		}

		public function remove(val: *): *
		{
			var idx: int = Util.binarySearch(list, compareFunc, val);

			if (idx == -1)
			return null;

			return removeByIndex(idx);
		}

		public function removeByIndex(index: int): *
		{
			var obj: * = list[index];

			list.splice(index, 1);

			super.dispatchEvent(new BinaryListEvent(BinaryListEvent.CHANGED));

			super.dispatchEvent(new BinaryListEvent(BinaryListEvent.REMOVED, obj));

			return obj;
		}

		public function sort():void
		{
			list.sort(sortFunc);
			super.dispatchEvent(new BinaryListEvent(BinaryListEvent.CHANGED));
		}

		public function getByIndex(index: int): *
		{
			return list[index];
		}

		public function size(): int
		{
			return list.length;
		}

		public function get(val: *): *
		{
			var idx: int = Util.binarySearch(list, compareFunc, val);

			if (idx == -1)
			return null;

			return list[idx];
		}
		

		public function getIdx(val: *): int
		{
			return Util.binarySearch(list, compareFunc, val);
		}		

		public function getRange(val: * ): *
		{
			var idxs: Array = Util.binarySearchRange(list, compareFunc, val);

			var objs: Array = new Array();

			for each(var idx: int in idxs)
			objs.push(list[idx]);

			return objs;
		}
	}

}
