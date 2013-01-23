#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Battle.CombatGroups;
using Game.Battle.CombatObjects;
using Game.Map;
using Game.Util;
using Persistance;

#endregion

namespace Game.Battle
{
    /// <summary>
    ///     A list of combat objects and manages targetting.
    /// </summary>
    public class CombatList : PersistableObjectList<ICombatGroup>, ICombatList
    {
        private readonly BattleFormulas battleFormulas;

        private readonly RadiusLocator radiusLocator;

        #region BestTargetResult enum

        public enum BestTargetResult
        {
            NoneInRange,

            Ok
        }

        #endregion

        public CombatList(IDbManager manager, RadiusLocator radiusLocator, BattleFormulas battleFormulas)
                : base(manager)
        {
            this.radiusLocator = radiusLocator;
            this.battleFormulas = battleFormulas;
        }

        public int Upkeep
        {
            get
            {
                return AllCombatObjects().Sum(obj => obj.Upkeep);
            }
        }

        public bool HasInRange(ICombatObject attacker)
        {
            return AllAliveCombatObjects().Any(obj => obj.InRange(attacker) && attacker.InRange(obj));
        }

        public BestTargetResult GetBestTargets(uint battleId, ICombatObject attacker, out List<Target> result, int maxCount)
        {
            result = new List<Target>();

            var objectsByScore = new List<CombatScoreItem>(Count);

            var objsInRange = (from @group in this
                               from combatObject in @group
                               where
                                       combatObject.InRange(attacker) && attacker.InRange(combatObject) &&
                                       !combatObject.IsDead
                               select new Target {Group = @group, CombatObject = combatObject}).ToList();

            if (objsInRange.Count == 0)
            {
                return BestTargetResult.NoneInRange;
            }

            uint lowestRow = objsInRange.Min(target => target.CombatObject.Stats.Stl);

            var attackerLocation = attacker.Location();

            Target bestTarget = null;
            int bestTargetScore = 0;
            foreach (var target in objsInRange)
            {
                if (!attacker.CanSee(target.CombatObject, lowestRow))
                {
                    continue;
                }

                int score = 0;

                Position defenderPosition = target.CombatObject.Location();

                // Distance 0 gives 60% higher chance to hit, distance 1 gives 20%
                score +=
                        Math.Max(
                                 3 -
                                 radiusLocator.RadiusDistance(attackerLocation.X,
                                                              attackerLocation.Y,
                                                              defenderPosition.X,
                                                              defenderPosition.Y) * 2,
                                 0);

                // Have to compare armor and weapon type here to give some sort of score
                score += ((int)(battleFormulas.GetDmgModifier(attacker, target.CombatObject) * 10));

                if (bestTarget == null || score > bestTargetScore)
                {
                    bestTarget = target;
                    bestTargetScore = score;
                }

                objectsByScore.Add(new CombatScoreItem {Target = target, Score = score});
            }

            if (objectsByScore.Count == 0)
            {
                return BestTargetResult.Ok;                
            }

            // Shuffle to get some randomization in the attack order. We pass the battleId as a seed in order to 
            // make it so the attacker doesn't switch targets once it starts attacking them but this way
            // they won't attack the stacks in the order they joined the battle, which usually would mean
            // they will attack the same type of units one after another
            // then sort by score descending
            objectsByScore.Sort((x, y) => x.Score.CompareTo(y.Score) * -1);
            
            var numberOfTargetsToHit = Math.Min(maxCount, objectsByScore.Count);
            var optimalScore = objectsByScore.First().Score;

            var highestScoringObjectsOnly = objectsByScore.Where(p => p.Score == optimalScore).ToList().Shuffle((int)battleId);

            // Get top results specified by the maxCount param
            result = highestScoringObjectsOnly.Take(numberOfTargetsToHit).Select(scoreItem => scoreItem.Target).ToList();

            if (result.Count < numberOfTargetsToHit)
            {
                result.AddRange(objectsByScore.GetRange(result.Count, numberOfTargetsToHit - result.Count).Select(scoreItem => scoreItem.Target));
            }

            return BestTargetResult.Ok;
        }

        public IEnumerable<ICombatObject> AllCombatObjects()
        {
            return BackingList.SelectMany(group => group.Select(combatObject => combatObject));
        }

        public IEnumerable<ICombatObject> AllAliveCombatObjects()
        {
            return
                    BackingList.SelectMany(
                                           group =>
                                           group.Where(combatObject => !combatObject.IsDead)
                                                .Select(combatObject => combatObject));
        }

        #region Nested type: CombatScoreItem

        private class CombatScoreItem
        {
            public int Score { get; set; }

            public Target Target { get; set; }
        }

        #endregion

        #region Nested type: Target

        public class Target
        {
            public ICombatObject CombatObject { get; set; }

            public ICombatGroup Group { get; set; }
        }

        #endregion

        #region Nested type: NoneInRange

        public class NoneInRange : Exception
        {
        }

        #endregion

        #region Nested type: NoneVisible

        public class NoneVisible : Exception
        {
        }

        #endregion
    }
}