#region

using System;
using System.Collections.Generic;
using Game.Battle;
using Game.Battle.CombatGroups;
using Game.Battle.CombatObjects;
using Game.Data;
using Game.Data.Troop;
using Game.Logic.Procedures;
using Game.Map;
using Game.Setup;
using Game.Util;

#endregion

namespace Game.Logic.Actions
{
    public class EngageDefensePassiveAction : PassiveAction
    {
        private readonly uint cityId;
        private readonly uint troopObjectId;

        private readonly BattleProcedure battleProcedure;

        private readonly IGameObjectLocator gameObjectLocator;

        private decimal originalHp;
        private decimal remainingHp;

        public EngageDefensePassiveAction(uint cityId, uint troopObjectId, BattleProcedure battleProcedure, IGameObjectLocator gameObjectLocator)
        {
            this.cityId = cityId;
            this.troopObjectId = troopObjectId;
            this.battleProcedure = battleProcedure;
            this.gameObjectLocator = gameObjectLocator;
        }

        public EngageDefensePassiveAction(uint id, bool isVisible, IDictionary<string, string> properties, BattleProcedure battleProcedure, IGameObjectLocator gameObjectLocator) : base(id, isVisible)
        {
            this.battleProcedure = battleProcedure;
            this.gameObjectLocator = gameObjectLocator;
            cityId = uint.Parse(properties["troop_city_id"]);
            troopObjectId = uint.Parse(properties["troop_object_id"]);
            originalHp = decimal.Parse(properties["original_hp"]);
            remainingHp = decimal.Parse(properties["remaining_hp"]);

            ICity city;
            if (!gameObjectLocator.TryGetObjects(cityId, out city))
                throw new Exception();

            city.Battle.ActionAttacked += BattleActionAttacked;
            city.Battle.ExitBattle += BattleExitBattle;
        }

        public override ActionType Type
        {
            get
            {
                return ActionType.EngageDefensePassive;
            }
        }

        public override string Properties
        {
            get
            {
                return
                        XmlSerializer.Serialize(new[]
                                                {
                                                        new XmlKvPair("troop_city_id", cityId), new XmlKvPair("troop_object_id", troopObjectId),
                                                        new XmlKvPair("original_hp", originalHp), new XmlKvPair("remaining_hp", remainingHp)
                                                });
            }
        }

        public override Error Validate(string[] parms)
        {
            return Error.Ok;
        }

        public override Error Execute()
        {
            ICity city;

            ITroopObject troopObject;
            if (!gameObjectLocator.TryGetObjects(cityId, troopObjectId, out city, out troopObject))
                return Error.ObjectNotFound;

            if (city.Battle == null)
            {
                StateChange(ActionState.Completed);
                return Error.Ok;
            }

            originalHp = remainingHp = troopObject.Stub.TotalHp;

            city.Battle.ActionAttacked += BattleActionAttacked;
            city.Battle.ExitBattle += BattleExitBattle;

            troopObject.BeginUpdate();
            troopObject.State = GameObjectState.BattleState(city.Battle.BattleId);
            troopObject.EndUpdate();
            troopObject.Stub.BeginUpdate();
            troopObject.Stub.State = TroopState.Battle;
            troopObject.Stub.EndUpdate();

            // Add units to battle
            battleProcedure.AddReinforcementToBattle(city.Battle, troopObject.Stub);
            battleProcedure.AddLocalUnitsToBattle(city.Battle, city);

            return Error.Ok;
        }

        private void BattleActionAttacked(IBattleManager battle, BattleManager.BattleSide attackingSide, ICombatGroup attackerGroup, ICombatObject attacker, ICombatGroup targetGroup, ICombatObject target, decimal damage)
        {
            // TODO: Save groupId like in EngageAttackAction and use it to identify our objects
            // TODO: This whole method can be changed to use the GroupKilled event instead of keeping the HP around like this
            var unit = target as AttackCombatUnit;

            if (unit == null || unit.City.Id != cityId)
            {
                return;
            }

            ICity city;
            ITroopObject troopObject;
            if (!gameObjectLocator.TryGetObjects(cityId, troopObjectId, out city, out troopObject))
            {
                throw new Exception();
            }

            if (unit.TroopStub != troopObject.Stub)
            {
                return;
            }

            remainingHp -= damage;
            if (remainingHp > 0)
            {
                return;
            }

            city.Battle.ActionAttacked -= BattleActionAttacked;
            city.Battle.ExitBattle -= BattleExitBattle;

            troopObject.BeginUpdate();
            troopObject.State = GameObjectState.NormalState();
            troopObject.Stub.State = TroopState.Idle;
            troopObject.EndUpdate();
            StateChange(ActionState.Completed);
        }

        private void BattleExitBattle(IBattleManager battle, ICombatList atk, ICombatList def)
        {
            ICity city;
            ITroopObject troopObject;
            if (!gameObjectLocator.TryGetObjects(cityId, troopObjectId, out city, out troopObject))
                throw new Exception();

            city.Battle.ActionAttacked -= BattleActionAttacked;
            city.Battle.ExitBattle -= BattleExitBattle;

            troopObject.BeginUpdate();
            troopObject.Stub.BeginUpdate();
            troopObject.State = GameObjectState.NormalState();
            troopObject.Stub.State = TroopState.Idle;
            troopObject.Stub.EndUpdate();
            troopObject.EndUpdate();

            StateChange(ActionState.Completed);
        }

        public override void WorkerRemoved(bool wasKilled)
        {
        }

        public override void UserCancelled()
        {
        }
    }
}