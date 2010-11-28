#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Data.Troop;
using Game.Fighting;
using Game.Logic.Actions;
using Game.Setup;

#endregion

namespace Game.Logic {
    public partial class Formula {

        /// <summary>
        /// Applies the specified effects to the specified radius. This is used by AwayFromLayout for building validation.
        /// </summary>
        /// <param name="effects"></param>
        /// <param name="radius"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int GetAwayFromRadius(IEnumerable<Effect> effects, byte radius, ushort type)
        {
            return radius + effects.Sum(x => (x.id == EffectCode.AwayFromStructureMod && (int)x.value[0] == type) ? (int)x.value[1] : 0);
        }        
        
        /// <summary>
        /// Returns the crop cap based on the main building level
        /// </summary>
        /// <param name="lvl"></param>
        /// <returns></returns>
        public static int ResourceCropCap(byte lvl) {
            int[] cap = { 700, 700, 880, 1130, 1450, 1880, 2440, 3200, 4200, 5500, 7200, 9500, 12500, 16500, 21800, 25000 };
            return cap[lvl];
        }
        
        /// <summary>
        /// /// Returns the wood cap based on the main building level
        /// </summary>
        /// <param name="lvl"></param>
        /// <returns></returns>
        public static int ResourceWoodCap(byte lvl) {
            int[] cap = { 700, 700, 880, 1130, 1450, 1880, 2440, 3200, 4200, 5500, 7200, 9500, 12500, 16500, 21800, 25000 };
            return cap[lvl];
        }

        /// <summary>
        /// Returns the iron cap based on the main building level
        /// </summary>
        /// <param name="lvl"></param>
        /// <returns></returns>
        public static int ResourceIronCap(byte lvl) {
            int[] cap = { 0, 0, 0, 0, 0, 0, 170, 380, 620, 900, 1240, 1630, 2090, 2620, 3260, 4000 };
            return cap[lvl];
        }

        /// <summary>
        /// Returns the amount of iron the user will get from the specified structure
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static int GetIronRate(Structure structure) {
            int[] multiplier = { int.MaxValue, 70, 68, 66, 64, 62, 60, 58, 56, 54, 52, 50, 48, 45, 42, 40 };
            return structure.Stats.Labor * 10 / multiplier[structure.Lvl];
        }

        /// <summary>
        /// Returns the highest X for 1 count available or 0 if none is found
        /// </summary>
        /// <param name="tech"></param>
        /// <returns></returns>
        public static int GetXForOneCount(TechnologyManager tech) {
            int ret = tech.GetEffects(EffectCode.XFor1, EffectInheritance.INVISIBLE).DefaultIfEmpty(new Effect()).Min(x => x.value[0] == null ? ushort.MaxValue : (int)x.value[0]);
            return ret;
        }


        public static Resource GetInitialCityResources() {
            return new Resource(500, 0, 0, 500, 20);
        }
    }
}