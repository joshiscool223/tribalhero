﻿package src.Objects.Actions
{
	import src.Util.BinaryList.*;

	/**
	 * ...
	 * @author Giuliano
	 */
	public class ReferenceManager extends BinaryList
	{

		public function ReferenceManager()
		{
			super(CurrentActionReference.sortOnId, CurrentActionReference.compareId);
		}

		public function getByObject(objectId: int) : Array {
			var ret: Array = new Array();

			for each (var reference: CurrentActionReference in each()) {
				if (reference.objectId == objectId)
				ret.push(reference);
			}

			return ret;
		}

	}

}
