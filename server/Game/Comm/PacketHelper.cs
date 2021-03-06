#region

using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using Game.Battle;
using Game.Battle.CombatGroups;
using Game.Battle.CombatObjects;
using Game.Data;
using Game.Data.BarbarianTribe;
using Game.Data.Stronghold;
using Game.Data.Tribe;
using Game.Data.Troop;
using Game.Database;
using Game.Logic;
using Game.Map;
using Game.Setup;
using Game.Setup.DependencyInjection;
using Game.Util;
using Persistance;

#endregion

namespace Game.Comm
{
    public class PacketHelper
    {
        private static IRegionLocator regionLocator;

        public static IRegionLocator RegionLocator
        {
            set
            {
                regionLocator = value;
            }
        }

        public static void AddToPacket(IUnitTemplate template, Packet packet)
        {
            packet.AddUInt16((ushort)template.Size);
            foreach (var kvp in template)
            {
                packet.AddUInt16(kvp.Key);
                packet.AddByte(kvp.Value.Lvl);
            }
        }

        public static void AddToPacket(Logic.Notifications.Notification notification, Packet packet)
        {
            packet.AddUInt32(notification.GameObject.City.Id);
            packet.AddUInt32(notification.GameObject.ObjectId);
            packet.AddUInt32(notification.Action.ActionId);
            packet.AddUInt16((ushort)notification.Action.Type);

            var actionTime = notification.Action as IActionTime;
            if (actionTime != null)
            {
                packet.AddUInt32(actionTime.BeginTime == DateTime.MinValue
                                         ? 0
                                         : UnixDateTime.DateTimeToUnix(actionTime.BeginTime.ToUniversalTime()));
                packet.AddUInt32(actionTime.EndTime == DateTime.MinValue
                                         ? 0
                                         : UnixDateTime.DateTimeToUnix(actionTime.EndTime.ToUniversalTime()));
            }
            else
            {
                packet.AddUInt32(0);
                packet.AddUInt32(0);
            }
        }

        public static void AddToPacket(ISimpleGameObject obj, Packet packet, bool forRegion = false)
        {
            packet.AddUInt16(obj.Type);

            packet.AddUInt16((ushort)(obj.PrimaryPosition.X % Config.region_width));
            packet.AddUInt16((ushort)(obj.PrimaryPosition.Y % Config.region_height));
            packet.AddByte(obj.Size);

            packet.AddUInt32(obj.GroupId);
            packet.AddUInt32(obj.ObjectId);

            var gameObj = obj as IGameObject;
            if (gameObj != null)
            {
                packet.AddUInt32(gameObj.City.Owner.PlayerId);
            }

            var troopObj = obj as ITroopObject;
            if (troopObj != null)
            {
                packet.AddString(troopObj.Theme);
            }

            IHasLevel objHasLevel = obj as IHasLevel;
            if (objHasLevel != null)
            {
                packet.AddByte(objHasLevel.Lvl);
            }

            var stronghold = obj as IStronghold;
            if (stronghold != null)
            {
                packet.AddUInt32(stronghold.StrongholdState == StrongholdState.Occupied ? stronghold.Tribe.Id : 0);
                packet.AddInt32(stronghold.GateMax);
                packet.AddString(stronghold.Theme);
            }

            var structure = obj as IStructure;
            if (structure != null)
            {
                packet.AddString(structure.Theme);

                if (!forRegion)
                {
                    packet.AddUInt16(structure.Stats.Labor);                    
                }

                if (structure.IsMainBuilding)
                {
                    packet.AddByte(gameObj.City.Radius);
                    packet.AddString(gameObj.City.WallTheme);
                }
            }

            var barbarianTribe = obj as IBarbarianTribe;
            if (barbarianTribe!=null)
            {
                packet.AddByte(barbarianTribe.CampRemains);
            }

            AddToPacket(obj.State, packet);
        }

        public static void AddToPacket(GameObjectState state, Packet packet)
        {
            packet.AddByte((byte)state.Type);
            foreach (var parameter in state.Parameters)
            {
                if (parameter is byte)
                {
                    packet.AddByte((byte)parameter);
                }
                else if (parameter is short)
                {
                    packet.AddInt16((short)parameter);
                }
                else if (parameter is int)
                {
                    packet.AddInt32((int)parameter);
                }
                else if (parameter is ushort)
                {
                    packet.AddUInt16((ushort)parameter);
                }
                else if (parameter is uint)
                {
                    packet.AddUInt32((uint)parameter);
                }
                else if (parameter is string)
                {
                    packet.AddString((string)parameter);
                }
            }
        }

        public static void AddToPacket(Resource resource, Packet packet, bool includeLabor = false)
        {
            packet.AddUInt32((uint)resource.Crop);
            packet.AddUInt32((uint)resource.Gold);
            packet.AddUInt32((uint)resource.Iron);
            packet.AddUInt32((uint)resource.Wood);
            if (includeLabor)
            {
                packet.AddUInt32((uint)resource.Labor);
            }
        }

        public static void AddToPacket(ILazyValue value, Packet packet)
        {
            packet.AddInt32(value.RawValue);
            packet.AddInt32(value.Rate);
            packet.AddInt32(value.Upkeep);
            packet.AddInt32(value.Limit);
            packet.AddUInt32(UnixDateTime.DateTimeToUnix(value.LastRealizeTime.ToUniversalTime()));
        }

        public static void AddToPacket(ILazyResource resource, Packet packet)
        {
            packet.AddInt32(resource.Crop.RawValue);
            packet.AddInt32(resource.Crop.Rate);
            packet.AddInt32(resource.Crop.Upkeep);
            packet.AddInt32(resource.Crop.Limit);
            packet.AddUInt32(UnixDateTime.DateTimeToUnix(resource.Crop.LastRealizeTime.ToUniversalTime()));

            packet.AddInt32(resource.Iron.RawValue);
            packet.AddInt32(resource.Iron.Rate);
            packet.AddInt32(resource.Iron.Limit);
            packet.AddUInt32(UnixDateTime.DateTimeToUnix(resource.Iron.LastRealizeTime.ToUniversalTime()));

            packet.AddInt32(resource.Gold.RawValue);
            packet.AddInt32(resource.Gold.Rate);
            packet.AddInt32(resource.Gold.Limit);
            packet.AddUInt32(UnixDateTime.DateTimeToUnix(resource.Gold.LastRealizeTime.ToUniversalTime()));

            packet.AddInt32(resource.Wood.RawValue);
            packet.AddInt32(resource.Wood.Rate);
            packet.AddInt32(resource.Wood.Limit);
            packet.AddUInt32(UnixDateTime.DateTimeToUnix(resource.Wood.LastRealizeTime.ToUniversalTime()));

            packet.AddInt32(resource.Labor.RawValue);
            packet.AddInt32(resource.Labor.Rate);
            packet.AddInt32(resource.Labor.Limit);
            packet.AddUInt32(UnixDateTime.DateTimeToUnix(resource.Wood.LastRealizeTime.ToUniversalTime()));
        }

        public static void AddToPacket(List<GameAction> actions, Packet packet, bool includeWorkerId)
        {
            packet.AddByte((byte)actions.Count);
            foreach (var actionStub in actions)
            {
                AddToPacket(actionStub, packet, includeWorkerId);
            }
        }

        internal static void AddToPacket(GameAction actionStub, Packet packet, bool includeWorkerId)
        {
            if (includeWorkerId)
            {
                packet.AddUInt32(actionStub.WorkerObject.WorkerId);
            }

            if (actionStub is PassiveAction)
            {
                packet.AddByte(0);
                packet.AddUInt32(actionStub.ActionId);
                packet.AddUInt16((ushort)actionStub.Type);
                packet.AddString(actionStub is ScheduledPassiveAction
                                         ? ((ScheduledPassiveAction)actionStub).NlsDescription
                                         : string.Empty);
            }
            else
            {
                packet.AddByte(1);
                packet.AddUInt32(actionStub.ActionId);
                packet.AddInt32(((ActiveAction)actionStub).WorkerType);
                packet.AddByte(((ActiveAction)actionStub).WorkerIndex);
                packet.AddUInt16(((ActiveAction)actionStub).ActionCount);
            }

            if (actionStub is IActionTime)
            {
                var actionTime = actionStub as IActionTime;
                if (actionTime.BeginTime == DateTime.MinValue)
                {
                    packet.AddUInt32(0);
                }
                else
                {
                    packet.AddUInt32(UnixDateTime.DateTimeToUnix(actionTime.BeginTime.ToUniversalTime()));
                }

                if (actionTime.EndTime == DateTime.MinValue)
                {
                    packet.AddUInt32(0);
                }
                else
                {
                    packet.AddUInt32(UnixDateTime.DateTimeToUnix(actionTime.EndTime.ToUniversalTime()));
                }
            }
            else
            {
                packet.AddUInt32(0);
                packet.AddUInt32(0);
            }
        }

        internal static void AddToPacket(ITroopStub stub, Packet packet)
        {
            ITroopObject troopObject = stub.City.TroopObjects.FirstOrDefault(troopObj => troopObj.Stub == stub);

            packet.AddUInt32(stub.City.Owner.PlayerId);
            packet.AddUInt32(stub.City.Id);

            packet.AddUInt16(stub.TroopId);
            packet.AddByte((byte)stub.State);
            AddToPacket(stub.Station, packet);
            packet.AddByte((byte)stub.AttackMode);
            AddToPacket(troopObject != null?troopObject.Stats.Loot: new Resource(), packet);

            //Add troop template
            packet.AddByte(stub.Template.Count);
            foreach (var stats in stub.Template)
            {
                packet.AddUInt16(stats.Key);
                packet.AddByte(stats.Value.Base.Lvl);

                packet.AddFloat((float)stats.Value.MaxHp);
                packet.AddFloat((float)stats.Value.Atk);
                packet.AddByte(stats.Value.Splash);
                // Await client update
                packet.AddUInt16(0);
                packet.AddByte(stats.Value.Rng);
                packet.AddByte(stats.Value.Spd);
                packet.AddByte(stats.Value.Stl);
            }

            //Add state specific variables
            switch(stub.State)
            {
                case TroopState.Moving:
                case TroopState.ReturningHome:
                    packet.AddUInt32(troopObject.ObjectId);
                    packet.AddUInt32(troopObject.PrimaryPosition.X);
                    packet.AddUInt32(troopObject.PrimaryPosition.Y);
                    break;
                case TroopState.Battle:
                    // If the stub is in battle, determine whether there is a troop object attached to it or not.
                    // If there is we send the troop objs location otherwise we assume that the troop stub is 
                    // in their city
                    if (troopObject != null)
                    {
                        packet.AddUInt32(troopObject.ObjectId);
                        packet.AddUInt32(troopObject.PrimaryPosition.X);
                        packet.AddUInt32(troopObject.PrimaryPosition.Y);
                    }
                    else
                    {
                        packet.AddUInt32(1); // Main building id
                        packet.AddUInt32(stub.City.PrimaryPosition.X);
                        packet.AddUInt32(stub.City.PrimaryPosition.Y);
                    }
                    break;
                case TroopState.Stationed:
                case TroopState.BattleStationed:
                    packet.AddUInt32(1); // Main building id
                    packet.AddUInt32(stub.Station.PrimaryPosition.X);
                    packet.AddUInt32(stub.Station.PrimaryPosition.Y);
                    break;
            }

            //Actual formation and unit counts
            packet.AddByte(stub.FormationCount);
            foreach (var formation in stub)
            {
                packet.AddByte((byte)formation.Type);
                packet.AddByte((byte)formation.Count);
                foreach (var kvp in formation)
                {
                    packet.AddUInt16(kvp.Key);
                    packet.AddUInt16(kvp.Value);
                }
            }
        }

        internal static void AddToPacket(ICombatList combatList, Packet packet)
        {
            packet.AddInt32(combatList.Count);
            foreach (var group in combatList)
            {
                AddToPacket(group, packet);
            }
        }

        internal static void AddToPacket(ICombatGroup combatGroup, Packet packet)
        {
            packet.AddUInt32(combatGroup.Id);
            packet.AddUInt16(combatGroup.TroopId);
            packet.AddByte((byte)combatGroup.Owner.Type);
            packet.AddUInt32(combatGroup.Owner.Id);
            packet.AddString(combatGroup.Owner.GetName());

            packet.AddInt32(combatGroup.Count);
            foreach (var combatObject in combatGroup)
            {
                AddToPacket(combatObject, packet);
            }
        }

        internal static void AddToPacket(ICombatObject combatObject, Packet packet)
        {
            combatObject.AddPacketInfo(packet);            
        }

        public static void AddLoginToPacket(Session session, IThemeManager themeManager, Packet packet)
        {
            // Tribal info
            packet.AddUInt32(session.Player.Tribesman == null ? 0 : session.Player.Tribesman.Tribe.Id);
            packet.AddUInt32(session.Player.TribeRequest);
            packet.AddByte((byte)(session.Player.Tribesman == null ? 0 : session.Player.Tribesman.Rank.Id));
            packet.AddString(session.Player.Tribesman == null ? string.Empty : session.Player.Tribesman.Tribe.Name);
            if (session.Player.Tribesman != null)
            {
                AddTribeRanksToPacket(session.Player.Tribesman.Tribe, packet);
            }

            //Cities
            IEnumerable<ICity> list = session.Player.GetCityList();
            packet.AddInt32(session.Player.GetCityCount());
            foreach (var city in list)
            {
                AddToPacket(city, packet);
            }

            // Themes purchased
            packet.AddInt32(session.Player.ThemePurchases.Count);
            foreach (var theme in session.Player.ThemePurchases)
            {
                packet.AddString(theme.ThemeId);
            }
        }

        public static void AddTribeRanksToPacket(ITribe tribe, Packet packet)
        {
            packet.AddByte((byte)tribe.Ranks.Count());
            foreach (var rank in tribe.Ranks)
            {
                packet.AddString(rank.Name);
                packet.AddInt32((int)rank.Permission);
            }
        }

        public static void AddToPacket(ICity city, Packet packet)
        {
            packet.AddUInt32(city.Id);
            packet.AddString(city.Name);
            packet.AddUInt32(city.PrimaryPosition.X);
            packet.AddUInt32(city.PrimaryPosition.Y);
            AddToPacket(city.Resource, packet);
            packet.AddByte(city.Radius);
            packet.AddInt32(city.AttackPoint);
            packet.AddInt32(city.DefensePoint);
            packet.AddUInt16(city.Value);
            packet.AddFloat((float)city.AlignmentPoint);
            packet.AddByte(city.Battle != null ? (byte)1 : (byte)0);
            packet.AddByte(city.HideNewUnits ? (byte)1 : (byte)0);
            packet.AddString(city.DefaultTheme);
            packet.AddString(city.TroopTheme);

            //City Actions
            AddToPacket(new List<GameAction>(city.Worker.GetVisibleActions()), packet, true);

            //Notifications
            packet.AddUInt16(city.Notifications.Count);
            foreach (var notification in city.Notifications)
            {
                AddToPacket(notification, packet);
            }

            //References
            packet.AddUInt16(city.References.Count);
            foreach (var reference in city.References)
            {
                packet.AddUInt16(reference.ReferenceId);
                packet.AddUInt32(reference.WorkerObject.WorkerId);
                packet.AddUInt32(reference.Action.ActionId);
            }

            //Structures
            var structs = new List<IStructure>(city);
            packet.AddUInt16((ushort)structs.Count);
            foreach (var structure in structs)
            {
                packet.AddUInt16(regionLocator.GetRegionIndex(structure));
                AddToPacket(structure, packet);

                packet.AddUInt16((ushort)structure.Technologies.OwnedTechnologyCount);
                foreach (var tech in structure.Technologies)
                {
                    if (tech.OwnerLocation != EffectLocation.Object)
                    {
                        continue;
                    }
                    packet.AddUInt32(tech.Type);
                    packet.AddByte(tech.Level);
                }
            }

            //Troop objects
            var troops = new List<ITroopObject>(city.TroopObjects);
            packet.AddUInt16((ushort)troops.Count);
            foreach (var troop in troops)
            {
                packet.AddUInt16(regionLocator.GetRegionIndex(troop));
                AddToPacket(troop, packet);
            }

            //City Troops
            packet.AddUInt16(city.Troops.Size);
            foreach (var stub in city.Troops)
            {
                AddToPacket(stub, packet);
            }

            //Unit Template
            AddToPacket(city.Template, packet);
        }

        internal static void AddToPacket(Assignment assignment, Packet packet)
        {
            packet.AddInt32(assignment.Id);
            packet.AddUInt32(UnixDateTime.DateTimeToUnix(assignment.TargetTime.ToUniversalTime()));
            packet.AddUInt32(assignment.X);
            packet.AddUInt32(assignment.Y);
            AddToPacket(assignment.Target, packet);
            packet.AddByte((byte)assignment.AttackMode);
            packet.AddUInt32(assignment.DispatchCount);
            packet.AddString(assignment.Description);
            packet.AddByte((byte)(assignment.IsAttack ? 1 : 0));
            packet.AddInt32(assignment.TroopCount);
            foreach (var assignmentTroop in assignment)
            {
                packet.AddUInt32(assignmentTroop.Stub.City.Owner.PlayerId);
                packet.AddUInt32(assignmentTroop.Stub.City.Id);
                packet.AddString(assignmentTroop.Stub.City.Owner.Name);
                packet.AddString(assignmentTroop.Stub.City.Name);
                packet.AddUInt16(assignmentTroop.Stub.TroopId);

                //Actual formation and unit counts
                packet.AddByte(assignmentTroop.Stub.FormationCount);
                foreach (var formation in assignmentTroop.Stub)
                {
                    packet.AddByte((byte)formation.Type);
                    packet.AddByte((byte)formation.Count);
                    foreach (var kvp in formation)
                    {
                        packet.AddUInt16(kvp.Key);
                        packet.AddUInt16(kvp.Value);
                    }
                }
            }
        }

        private static void AddToPacket(ILocation location, Packet packet)
        {
            if (location == null)
            {
                packet.AddInt32(-1);
                return;
            }

            packet.AddInt32((int)location.LocationType);
            switch(location.LocationType)
            {
                case LocationType.City:
                    ICity targetCity = location as ICity;
                    if (targetCity != null ||
                        Ioc.Kernel.Get<ICityManager>().TryGetCity(location.LocationId, out targetCity))
                    {
                        packet.AddUInt32(targetCity.Owner.PlayerId);
                        packet.AddUInt32(targetCity.Id);
                        packet.AddString(targetCity.Owner.Name);
                        packet.AddString(targetCity.Name);
                        return;
                    }
                    break;
                case LocationType.Stronghold:
                    IStronghold targetStronghold = location as IStronghold;
                    if (targetStronghold != null ||
                        Ioc.Kernel.Get<IStrongholdManager>().TryGetStronghold(location.LocationId, out targetStronghold))
                    {
                        packet.AddUInt32(targetStronghold.ObjectId);
                        packet.AddString(targetStronghold.Name);
                        packet.AddUInt32(targetStronghold.Tribe == null ? 0 : targetStronghold.Tribe.Id);
                        packet.AddString(targetStronghold.Tribe == null ? "" : targetStronghold.Tribe.Name);
                        return;
                    }
                    break;
            }

            packet.AddUInt32(0);
            packet.AddUInt32(0);
            packet.AddString("Error");
            packet.AddString("Error");
        }

        public static ISimpleStub ReadStub(Packet packet, params FormationType[] formations)
        {
            var simpleStub = new SimpleStub();

            for (int i = 0; i < formations.Length; i++)
            {
                FormationType formationType = (FormationType)packet.GetByte();
                if (!formations.Contains(formationType))
                {
                    throw new Exception("Invalid formation sent");
                }

                byte unitCount = packet.GetByte();

                for (int u = 0; u < unitCount; ++u)
                {
                    ushort type = packet.GetUInt16();
                    ushort count = packet.GetUInt16();
                    simpleStub.AddUnit(formationType, type, count);
                }
            }

            return simpleStub;
        }

        public static void AddBattleProperties(IDictionary<string, object> properties, Packet reply)
        {
            reply.AddByte((byte)properties.Count);
            foreach (var property in properties)
            {
                reply.AddString(property.Key);
                if (property.Value is decimal)
                {
                    reply.AddString(((decimal)property.Value).ToString("#.##"));
                }
                else
                {
                    reply.AddString(property.Value.ToString());
                }
            }
        }

        public static void AddPlayerProfileToPacket(IPlayer player, Packet reply)
        {
            reply.AddUInt32(player.PlayerId);
            reply.AddString(player.Name);
            reply.AddString(player.Description);

            reply.AddUInt32(player.Tribesman != null ? player.Tribesman.Tribe.Id : 0);
            reply.AddString(player.Tribesman != null ? player.Tribesman.Tribe.Name : string.Empty);
            reply.AddString(player.Tribesman != null ? player.Tribesman.Rank.Name : string.Empty);

            // Ranking info
            List<dynamic> ranks = new List<dynamic>();

            string rankingQuery = string.Format("SELECT `city_id`, `rank`, `type` FROM `rankings` WHERE player_id = @playerId ORDER BY `type` ASC");
            using (var reader = DbPersistance.Current.ReaderQuery(rankingQuery, new[] {new DbColumn("playerId", player.PlayerId, DbType.String)}))
            {
                while (reader.Read())
                {
                    dynamic rank = new ExpandoObject();
                    rank.CityId = (uint)reader["city_id"];
                    rank.Rank = (int)reader["rank"];
                    rank.Type = (byte)((sbyte)reader["type"]);
                    ranks.Add(rank);
                }
            }

            reply.AddUInt16((ushort)ranks.Count);
            foreach (var rank in ranks)
            {
                reply.AddUInt32(rank.CityId);
                reply.AddInt32(rank.Rank);
                reply.AddByte(rank.Type);
            }

            // Achievement
            reply.AddUInt16((ushort)player.Achievements.Count);
            foreach (var achievement in player.Achievements)
            {
                reply.AddInt32(achievement.Id);
                reply.AddString(achievement.Type);
                reply.AddByte((byte)achievement.Tier);
                reply.AddString(achievement.Icon);
                reply.AddString(achievement.Title);
                reply.AddString(achievement.Description);
            }

            // City info
            var cityCount = (byte)player.GetCityCount();
            reply.AddByte(cityCount);
            foreach (var city in player.GetCityList())
            {
                reply.AddUInt32(city.Id);
                reply.AddString(city.Name);
                reply.AddUInt32(city.PrimaryPosition.X);
                reply.AddUInt32(city.PrimaryPosition.Y);
            }
        }

        public static void AddTribeInfo(IStrongholdManager strongholdManager,
                                        ITribeManager tribeManager,
                                        Session session,
                                        ITribe tribe,
                                        Packet packet)
        {
            if (session.Player.IsInTribe && tribe.Id == session.Player.Tribesman.Tribe.Id)
            {
                packet.AddByte(1);
                packet.AddUInt32(tribe.Id);
                packet.AddUInt32(tribe.Owner.PlayerId);
                packet.AddByte(tribe.Level);
                packet.AddString(tribe.Name);
                packet.AddString(tribe.Description);
                packet.AddString(tribe.PublicDescription);
                packet.AddFloat((float)tribe.VictoryPoint);
                packet.AddUInt32(UnixDateTime.DateTimeToUnix(tribe.Created));
                AddToPacket(tribe.Resource, packet);

                packet.AddInt16((short)tribe.Count);
                foreach (var tribesman in tribe.Tribesmen)
                {
                    packet.AddUInt32(tribesman.Player.PlayerId);
                    packet.AddString(tribesman.Player.Name);
                    packet.AddInt32(tribesman.Player.GetCityCount());
                    packet.AddByte(tribesman.Rank.Id);
                    packet.AddUInt32(tribesman.Player.IsLoggedIn ? 0 : UnixDateTime.DateTimeToUnix(tribesman.Player.LastLogin));
                    AddToPacket(tribesman.Contribution, packet);
                }

                // Incoming List
                var incomingList = tribeManager.GetIncomingList(tribe).ToList();
                packet.AddInt16((short)incomingList.Count());
                foreach (var incoming in incomingList)
                {
                    AddToPacket(incoming.Target, packet);
                    AddToPacket(incoming.Source, packet);

                    packet.AddUInt32(UnixDateTime.DateTimeToUnix(incoming.EndTime.ToUniversalTime()));
                }

                // Assignment List
                packet.AddInt16(tribe.AssignmentCount);
                foreach (var assignment in tribe.Assignments)
                {
                    AddToPacket(assignment, packet);
                }

                // Strongholds
                var strongholds = strongholdManager.StrongholdsForTribe(tribe).ToList();
                packet.AddInt16((short)strongholds.Count);
                foreach (var stronghold in strongholds)
                {
                    packet.AddUInt32(stronghold.ObjectId);
                    packet.AddString(stronghold.Name);
                    packet.AddByte((byte)stronghold.StrongholdState);
                    packet.AddByte(stronghold.Lvl);
                    packet.AddFloat((float)stronghold.Gate);
                    packet.AddInt32(stronghold.GateMax);
                    packet.AddUInt32(stronghold.PrimaryPosition.X);
                    packet.AddUInt32(stronghold.PrimaryPosition.Y);
                    packet.AddInt32(stronghold.Troops.StationedHere().Sum(x => x.Upkeep));
                    packet.AddFloat((float)stronghold.VictoryPointRate);
                    packet.AddUInt32(UnixDateTime.DateTimeToUnix(stronghold.DateOccupied.ToUniversalTime()));
                    packet.AddUInt32(stronghold.GateOpenTo == null ? 0 : stronghold.GateOpenTo.Id);
                    packet.AddString(stronghold.GateOpenTo == null ? string.Empty : stronghold.GateOpenTo.Name);
                    if (stronghold.GateBattle != null)
                    {
                        packet.AddByte(1);
                        packet.AddUInt32(stronghold.GateBattle.BattleId);
                    }
                    else if (stronghold.MainBattle != null)
                    {
                        packet.AddByte(2);
                        packet.AddUInt32(stronghold.MainBattle.BattleId);
                    }
                    else
                    {
                        packet.AddByte(0);
                    }
                }

                // Attackable Strongholds 
                strongholds = strongholdManager.OpenStrongholdsForTribe(tribe).ToList();
                packet.AddInt16((short)strongholds.Count);
                foreach (var stronghold in strongholds)
                {
                    packet.AddUInt32(stronghold.ObjectId);
                    packet.AddString(stronghold.Name);
                    packet.AddUInt32(stronghold.Tribe == null ? 0 : stronghold.Tribe.Id);
                    packet.AddString(stronghold.Tribe == null ? string.Empty : stronghold.Tribe.Name);
                    packet.AddByte((byte)stronghold.StrongholdState);
                    packet.AddByte(stronghold.Lvl);
                    packet.AddUInt32(stronghold.PrimaryPosition.X);
                    packet.AddUInt32(stronghold.PrimaryPosition.Y);
                    if (stronghold.GateBattle != null)
                    {
                        packet.AddByte(1);
                        packet.AddUInt32(stronghold.GateBattle.BattleId);
                    }
                    else if (stronghold.MainBattle != null)
                    {
                        packet.AddByte(2);
                        packet.AddUInt32(stronghold.MainBattle.BattleId);
                    }
                    else
                    {
                        packet.AddByte(0);
                    }
                }
            }
            else
            {
                packet.AddByte(0);
                packet.AddUInt32(tribe.Id);
                packet.AddString(tribe.Name);
                packet.AddString(tribe.PublicDescription);
                packet.AddByte(tribe.Level);
                packet.AddUInt32(UnixDateTime.DateTimeToUnix(tribe.Created));

                packet.AddByte((byte)tribe.Ranks.Count());
                foreach (var rank in tribe.Ranks)
                {
                    packet.AddString(rank.Name);
                }

                packet.AddInt16((short)tribe.Count);
                foreach (var tribesman in tribe.Tribesmen)
                {
                    packet.AddUInt32(tribesman.Player.PlayerId);
                    packet.AddString(tribesman.Player.Name);
                    packet.AddInt32(tribesman.Player.GetCityCount());
                    packet.AddByte(tribesman.Rank.Id);
                }

                var strongholds = strongholdManager.StrongholdsForTribe(tribe).ToList();
                packet.AddInt16((short)strongholds.Count);
                foreach (var stronghold in strongholds)
                {
                    packet.AddUInt32(stronghold.ObjectId);                    
                    packet.AddString(stronghold.Name);
                    packet.AddByte(stronghold.Lvl);
                    packet.AddUInt32(stronghold.PrimaryPosition.X);
                    packet.AddUInt32(stronghold.PrimaryPosition.Y);
                }
            }
        }

        public static void AddStrongholdProfileToPacket(Session session, IStronghold stronghold, Packet packet)
        {
            if (!session.Player.IsInTribe || !stronghold.BelongsTo(session.Player.Tribesman.Tribe))
            {
                packet.AddByte(0);
                packet.AddUInt32(stronghold.PrimaryPosition.X);
                packet.AddUInt32(stronghold.PrimaryPosition.Y);
            }
            else
            {
                packet.AddByte(1);
                packet.AddUInt32(stronghold.ObjectId);
                packet.AddString(stronghold.Name);
                packet.AddByte(stronghold.Lvl);
                packet.AddFloat((float)stronghold.Gate);
                packet.AddInt32(stronghold.GateMax);
                packet.AddFloat((float)stronghold.VictoryPointRate);
                packet.AddUInt32(UnixDateTime.DateTimeToUnix(stronghold.DateOccupied.ToUniversalTime()));
                packet.AddUInt32(stronghold.PrimaryPosition.X);
                packet.AddUInt32(stronghold.PrimaryPosition.Y);
                AddToPacket(stronghold.State, packet);

                packet.AddUInt16(stronghold.Troops.Size);
                foreach (var troop in stronghold.Troops)
                {
                    packet.AddUInt32(troop.City.Owner.PlayerId);
                    packet.AddUInt32(troop.City.Id);
                    packet.AddString(troop.City.Owner.Name);
                    packet.AddString(troop.City.Name);
                    packet.AddUInt16(troop.TroopId);

                    //Actual formation and unit counts
                    packet.AddByte(troop.FormationCount);
                    foreach (var formation in troop)
                    {
                        packet.AddByte((byte)formation.Type);
                        packet.AddByte((byte)formation.Count);
                        foreach (var kvp in formation)
                        {
                            packet.AddUInt16(kvp.Key);
                            packet.AddUInt16(kvp.Value);
                        }
                    }
                }
            }
        }
    }
}