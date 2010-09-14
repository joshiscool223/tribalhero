#region

using System.Collections.Generic;
using Game.Battle;
using Game.Data;
using Game.Data.Troop;
using Game.Fighting;

#endregion

namespace Game.Logic.Procedures {
    public partial class Procedure {
        public static void MoveUnitFormation(TroopStub stub, FormationType source, FormationType target) {
            stub[target].Add(stub[source]);
            stub[source].Clear();
        }

        public static void AddLocalToBattle(BattleManager bm, City city, ReportState state) {
            List<TroopStub> list = new List<TroopStub>(1) {city.DefaultTroop};           

            city.DefaultTroop.BeginUpdate();
            city.DefaultTroop.State = TroopState.BATTLE;
            city.DefaultTroop.Template.LoadStats(TroopBattleGroup.LOCAL);
            bm.AddToLocal(list, state);
            MoveUnitFormation(city.DefaultTroop, FormationType.NORMAL, FormationType.IN_BATTLE);
            city.DefaultTroop.EndUpdate(); 
        }
    }
}