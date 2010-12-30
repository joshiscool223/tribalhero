﻿package src.Objects.Effects {

	/**
	 * ...
	 * @author Default
	 */
	import src.Constants;
	import src.Global;
	import src.Map.City;
	import src.Map.CityObject;
	import src.Objects.Factories.ObjectFactory;
	import src.Objects.GameObject;
	import src.Objects.Prototypes.EffectPrototype;
	import src.Objects.Prototypes.StructurePrototype;
	import src.Objects.Prototypes.UnitPrototype;
	import src.Objects.Resources;
	import src.Objects.StructureObject;
	import src.Objects.TechnologyManager;
	import src.Objects.Troop.TroopStub;

	public class Formula {

		public static const RESOURCE_CHUNK: int = 100;
		public static const RESOURCE_MAX_TRADE: int = 1500;
		
		public static function sendCapacity(level: int) : int {
			var rate: Array = [0, 200, 200, 400, 400, 600, 600, 800, 1000, 1200, 1200, 1400, 1600, 1800, 1800, 2000];
			return rate[level];
		}

		public static function troopRadius(troop: TroopStub) : int {
			return Math.min(Math.ceil(troop.getUpkeep(true) / 100.0), 5);
		}

		private static function timeDiscount(level: int) : int {
			var discount: Array = [0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 10, 15, 15, 20, 30, 40];
			return discount[level];
		}
		
		public static function trainTime(parentObj: GameObject, baseValue: int, techManager: TechnologyManager): int
		{			
			return (baseValue * Constants.secondsPerUnit) * (100 - timeDiscount(parentObj.level)) / 100;
		}

		public static function buildTime(parentObj: GameObject, baseValue: int, techManager:TechnologyManager): int
		{						
			var city: City = Global.map.cities.get(parentObj.cityId);
			
			if (!city) return 0;

			var university: CityObject;
			for each (var structure: CityObject in city.objects.each()) {
				if (ObjectFactory.isType("University", structure.getType())) {
					university = structure;
					break;
				}
			}			
			
			var buildTime: int = (baseValue * (100 - (university ? university.labor : 0) * 0.25) / 100);

			return buildTime * Constants.secondsPerUnit;
		}

		public static function moveTime(city: City, speed: int, distance: int) : int {
			var mod: int = 100;
			for each (var tech: EffectPrototype in city.techManager.getEffects(EffectPrototype.EFFECT_TROOP_SPEED_MOD, EffectPrototype.INHERIT_ALL)) {
				mod -= tech.param1;
			}
			mod = Math.max(50, mod);

			var moveTime: int = 80 * (100 - ((speed - 11) * 5)) / 100;

			return Math.max(1, moveTime * Constants.secondsPerUnit * mod / 100) * distance;
		}

		public static function marketBuyCost(price: int, amount: int, tax: Number): int
		{
			return Math.round(((amount / RESOURCE_CHUNK) * price) * (1.0 + tax));
		}

		public static function marketSellCost(price: int, amount: int, tax: Number): int
		{
			return Math.round(((amount / RESOURCE_CHUNK) * price) * (1.0 - tax));
		}

		public static function buildCost(city: City, prototype: StructurePrototype) : Resources
		{
			return prototype.buildResources;
		}

		public static function unitTrainCost(city: City, prototype: UnitPrototype) : Resources
		{
			return prototype.trainResources;
		}

		public static function unitUpgradeCost(city: City, prototype: UnitPrototype) : Resources
		{
			return prototype.upgradeResources;
		}

		public static function marketTax(structure: StructureObject): Number
		{
			var rate: Array = [ -0.15, -0.15, -0.12, -0.09, -0.06, -0.03, 0, 0.03, 0.06, 0.09, 0.12 ];			
			return rate[structure.level];
		}

		public static function maxForestLabor(level: int) : int {
			return level * 240;
		}

		public static function maxForestLaborPerUser(level: int) : int {
			return Formula.maxForestLabor(level) / 6;
		}

		public static function movementIconTroopSize(troopStub: TroopStub) : int {
			var upkeep: int = troopStub.getUpkeep();
			return Math.min(4, upkeep / 60);
		}

		public static function laborRate(city: City) : int {
			var laborTotal: int = city.getBusyLaborCount() + city.resources.labor.getValue();
			if (laborTotal < 140) laborTotal = 140;
			
			var effects: Array = city.techManager.getEffects(EffectPrototype.EFFECT_COUNT_LESS_THAN, EffectPrototype.INHERIT_SELF_ALL);
			for each (var effect: EffectPrototype in effects) {
				if (effect.param1 == 30021) return (43200 / (-6.845 * Math.log(laborTotal) + 55)) * 0.7;
			}
			
			return (43200 / (-6.845 * Math.log(laborTotal) + 55));
		}
	}
}

