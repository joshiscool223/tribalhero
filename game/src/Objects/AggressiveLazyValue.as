﻿package src.Objects 
{
	import src.Constants;
	import src.Objects.LazyValue;
	
	/**
	 * ...
	 * @author Giuliano Barberi
	 */
	public class AggressiveLazyValue extends LazyValue
	{
		
		public function AggressiveLazyValue(value: int, rate: int, upkeep: int, limit: int, lastRealizeTime: int)
		{
			super(value, rate, upkeep, limit, lastRealizeTime);
		}		
		
		protected override function getCalculatedRate(): Number {
			return (3600.0 / (getRate() - getUpkeep())) * Constants.secondsPerUnit;
		}
		
	}

}