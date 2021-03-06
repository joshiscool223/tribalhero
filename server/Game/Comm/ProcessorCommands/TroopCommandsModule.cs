#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Data.BarbarianTribe;
using Game.Data.Stats;
using Game.Data.Stronghold;
using Game.Data.Troop;
using Game.Logic.Actions;
using Game.Logic.Formulas;
using Game.Map;
using Game.Setup;
using Game.Util.Locking;

#endregion

namespace Game.Comm.ProcessorCommands
{
    class TroopCommandsModule : CommandModule
    {
        private readonly IActionFactory actionFactory;

        private readonly IGameObjectLocator gameObjectLocator;

        private readonly Formula formula;

        private readonly ILocker locker;

        private readonly ITroopObjectInitializerFactory troopObjectInitializerFactory;

        private readonly IStructureCsvFactory structureCsvFactory;

        private IThemeManager themeManager;

        public TroopCommandsModule(IActionFactory actionFactory,
                                   IStructureCsvFactory structureCsvFactory,
                                   IGameObjectLocator gameObjectLocator,
                                   Formula formula,
                                   ILocker locker,
                                   ITroopObjectInitializerFactory troopObjectInitializerFactory, 
                                   IThemeManager themeManager)
        {
            this.actionFactory = actionFactory;
            this.structureCsvFactory = structureCsvFactory;
            this.gameObjectLocator = gameObjectLocator;
            this.formula = formula;
            this.locker = locker;
            this.troopObjectInitializerFactory = troopObjectInitializerFactory;
            this.themeManager = themeManager;
        }

        public override void RegisterCommands(IProcessor processor)
        {
            processor.RegisterCommand(Command.UnitTrain, TrainUnit);
            processor.RegisterCommand(Command.UnitUpgrade, UnitUpgrade);
            processor.RegisterCommand(Command.TroopInfo, GetTroopInfo);
            processor.RegisterCommand(Command.TroopAttackCity, AttackCity);
            processor.RegisterCommand(Command.TroopAttackStronghold, AttackStronghold);
            processor.RegisterCommand(Command.TroopDefendCity, DefendCity);
            processor.RegisterCommand(Command.TroopDefendStronghold, DefendStronghold);
            processor.RegisterCommand(Command.TroopRetreat, Retreat);
            processor.RegisterCommand(Command.TroopLocalSet, LocalTroopSet);
            processor.RegisterCommand(Command.TroopAttackBarbarianTribe, AttackBarbarianTribe);
            processor.RegisterCommand(Command.TroopModeSwitch, ModeSwitch);
            processor.RegisterCommand(Command.TroopTransfer, Transfer);
            processor.RegisterCommand(Command.TroopSetTheme, TroopSetTheme);
        }

        private void TroopSetTheme(Session session, Packet packet)
        {
            uint cityId;
            uint troopId;
            string theme;

            try
            {
                cityId = packet.GetUInt32();
                troopId = packet.GetUInt32();
                theme = packet.GetString();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            ICity city;
            ITroopObject troop;
            locker.Lock(cityId, troopId, out city, out troop).Do(() =>
            {
                if (city == null || troop == null || city.Owner != session.Player)
                {
                    ReplyError(session, packet, Error.ObjectNotFound);
                    return;
                }

                var result = themeManager.SetTroopTheme(troop, theme);

                ReplyWithResult(session, packet, result);
            });
        }

        private void Transfer(Session session, Packet packet)
        {
            uint cityId;
            ushort troopId;
            uint strongholdId;
            bool completeRetreat;
            ISimpleStub unitsToRetreat = null;

            try
            {
                cityId = packet.GetUInt32();
                troopId = packet.GetUInt16();
                strongholdId = packet.GetUInt32();
                completeRetreat = packet.GetByte() == 1;

                if (!completeRetreat)
                {
                    unitsToRetreat = PacketHelper.ReadStub(packet, FormationType.Defense);
                }
            }
            catch (Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            ICity city;
            IStation station = null;

            //we need to find out the stationed city first then reacquire local + stationed stronghold locks            
            locker.Lock(cityId, out city).Do(() =>
            {
                if (city == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                ITroopStub stub;

                if (!city.Troops.TryGetStub(troopId, out stub) || stub.Station == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                station = stub.Station;
            });

            if (station == null)
            {
                return;
            }

            locker.Lock(city, station).Do(() =>
            {
                ITroopStub stub;

                if (!city.Troops.TryGetStub(troopId, out stub))
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                //Make sure that the person sending the retreat is either the guy who owns the troop and it's stationed.
                if ((city.Owner != session.Player) || stub.Station == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                if (stub.Station.LocationType != LocationType.Stronghold)
                {
                    ReplyError(session, packet, Error.TroopNotStationedStronghold);
                    return;
                }

                ITroopObjectInitializer troopInitializer;

                if (completeRetreat || unitsToRetreat.TotalCount == stub.TotalCount)
                {
                    troopInitializer = troopObjectInitializerFactory.CreateStationedTroopObjectInitializer(stub);
                }
                else
                {
                    troopInitializer = troopObjectInitializerFactory.CreateStationedPartialTroopObjectInitializer(stub, unitsToRetreat);
                }

                var ra = actionFactory.CreateStrongholdDefenseChainAction(cityId,
                                                                          troopInitializer,
                                                                          strongholdId);

                Error ret = city.Worker.DoPassive(city, ra, true);

                ReplyWithResult(session, packet, ret);
            });
        }

        private void ModeSwitch(Session session, Packet packet)
        {
            uint cityId;
            ushort stubId;
            AttackMode mode;
            try
            {
                cityId = packet.GetUInt32();
                stubId = packet.GetUInt16();
                mode = (AttackMode)packet.GetByte();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            ICity city;
            IStation station = null;

            //we need to find out the stationed city first then reacquire local + stationed city locks            
            locker.Lock(cityId, out city).Do(() =>
            {
                if (city == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                ITroopStub stub;

                if (!city.Troops.TryGetStub(stubId, out stub) || stub.Station == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                station = stub.Station;
            });

            if (station == null)
            {
                return;
            }

            locker.Lock(city, station).Do(() =>
            {
                ITroopStub stub;

                if (!city.Troops.TryGetStub(stubId, out stub))
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                if (stub.InitialCount == 0)
                {
                    ReplyError(session, packet, Error.NewFeature);
                    return;
                }

                if (stub.State != TroopState.Stationed)
                {
                    ReplyError(session, packet, Error.TroopNotStationed);
                    return;
                }

                stub.BeginUpdate();
                stub.RetreatCount = (ushort)formula.GetAttackModeTolerance(stub.InitialCount, mode);
                stub.AttackMode = mode;
                stub.EndUpdate();

                ReplySuccess(session, packet);
            });
        }

        private void GetTroopInfo(Session session, Packet packet)
        {
            ICity city;
            ITroopObject troop;

            uint cityId;
            uint objectId;

            try
            {
                cityId = packet.GetUInt32();
                objectId = packet.GetUInt32();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            locker.Lock(cityId, objectId, out city, out troop).Do(() =>
            {
                if (city == null || troop == null || troop.Stub == null)
                {
                    ReplyError(session, packet, Error.ObjectNotFound);
                    return;
                }

                var reply = new Packet(packet);
                reply.AddUInt16(troop.Stub.TroopId);

                if (city.Owner == session.Player)
                {
                    reply.AddByte(troop.Stats.AttackRadius);
                    reply.AddFloat((float)troop.Stats.Speed);
                    reply.AddUInt32(troop.TargetX);
                    reply.AddUInt32(troop.TargetY);

                    var template = new Dictionary<ushort, IBaseUnitStats>();

                    reply.AddByte(troop.Stub.FormationCount);
                    foreach (var formation in troop.Stub)
                    {
                        reply.AddByte((byte)formation.Type);
                        reply.AddByte((byte)formation.Count);
                        foreach (var kvp in formation)
                        {
                            reply.AddUInt16(kvp.Key);
                            reply.AddUInt16(kvp.Value);
                            template[kvp.Key] = city.Template[kvp.Key];
                        }
                    }

                    reply.AddUInt16((ushort)template.Count);
                    IEnumerator<KeyValuePair<ushort, IBaseUnitStats>> templateIter = template.GetEnumerator();
                    while (templateIter.MoveNext())
                    {
                        KeyValuePair<ushort, IBaseUnitStats> kvp = templateIter.Current;
                        reply.AddUInt16(kvp.Key);
                        reply.AddByte(kvp.Value.Lvl);
                    }
                }

                session.Write(reply);
            });
        }

        private void LocalTroopSet(Session session, Packet packet)
        {
            uint cityId;
            bool hideNewUnits;
            ISimpleStub stub;
            try
            {
                cityId = packet.GetUInt32();
                hideNewUnits = packet.GetByte() == 1;
                stub = PacketHelper.ReadStub(packet, FormationType.Normal, FormationType.Garrison);
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            locker.Lock(session.Player).Do(() =>
            {
                ICity city = session.Player.GetCity(cityId);

                if (city == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                // Set where new units should be sent to
                city.BeginUpdate();
                city.HideNewUnits = hideNewUnits;
                city.EndUpdate();

                // Move units
                if (stub.TotalCount > 0)
                {
                    var currentUnits = city.DefaultTroop.ToUnitList(FormationType.Normal, FormationType.Garrison);
                    var newUnits = stub.ToUnitList();

                    if (currentUnits.Count != newUnits.Count)
                    {
                        ReplyError(session, packet, Error.TroopChanged);
                        return;
                    }

                    // Units are ordered by their type so we can compare the array by indexes
                    if (
                            currentUnits.Where(
                                               (currentUnit, i) =>
                                               currentUnit.Type != newUnits[i].Type ||
                                               currentUnit.Count != newUnits[i].Count).Any())
                    {
                        ReplyError(session, packet, Error.TroopChanged);
                        return;
                    }

                    city.DefaultTroop.BeginUpdate();
                    city.DefaultTroop.RemoveAllUnits(FormationType.Normal, FormationType.Garrison);
                    city.DefaultTroop.Add(stub);
                    city.DefaultTroop.EndUpdate();
                }

                ReplySuccess(session, packet);
            });
        }

        private void UnitUpgrade(Session session, Packet packet)
        {
            uint cityId;
            uint objectId;
            ushort type;

            try
            {
                cityId = packet.GetUInt32();
                objectId = packet.GetUInt32();
                type = packet.GetUInt16();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            locker.Lock(session.Player).Do(() =>
            {
                ICity city = session.Player.GetCity(cityId);

                if (city == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                IStructure barrack;
                if (!city.TryGetStructure(objectId, out barrack))
                {
                    ReplyError(session, packet, Error.Unexpected);
                }

                var upgradeAction = actionFactory.CreateUnitUpgradeActiveAction(cityId, objectId, type);
                Error ret = city.Worker.DoActive(structureCsvFactory.GetActionWorkerType(barrack),
                                                 barrack,
                                                 upgradeAction,
                                                 barrack.Technologies);
                if (ret != 0)
                {
                    ReplyError(session, packet, ret);
                }
                else
                {
                    ReplySuccess(session, packet);
                }
            });
        }

        private void TrainUnit(Session session, Packet packet)
        {
            uint cityId;
            uint objectId;
            ushort type;
            ushort count;

            try
            {
                cityId = packet.GetUInt32();
                objectId = packet.GetUInt32();
                type = packet.GetUInt16();
                count = packet.GetUInt16();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            locker.Lock(session.Player).Do(() =>
            {
                ICity city = session.Player.GetCity(cityId);

                if (city == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                IStructure barrack;
                if (!city.TryGetStructure(objectId, out barrack))
                {
                    ReplyError(session, packet, Error.Unexpected);
                }

                var trainAction = actionFactory.CreateUnitTrainActiveAction(cityId, objectId, type, count);
                Error ret = city.Worker.DoActive(structureCsvFactory.GetActionWorkerType(barrack),
                                                 barrack,
                                                 trainAction,
                                                 barrack.Technologies);
                if (ret != 0)
                {
                    ReplyError(session, packet, ret);
                }
                else
                {
                    ReplySuccess(session, packet);
                }
            });
        }

        private void AttackStronghold(Session session, Packet packet)
        {
            uint cityId;
            uint targetStrongholdId;
            ISimpleStub simpleStub;
            AttackMode mode;

            try
            {
                mode = (AttackMode)packet.GetByte();
                cityId = packet.GetUInt32();
                targetStrongholdId = packet.GetUInt32();
                simpleStub = PacketHelper.ReadStub(packet, FormationType.Attack);
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            IStronghold stronghold = null;
            ICity city = null;

            locker.Lock(session.Player).Do(() =>
            {
                city = session.Player.GetCity(cityId);
                if (city == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                if (!gameObjectLocator.TryGetObjects(targetStrongholdId, out stronghold))
                {
                    ReplyError(session, packet, Error.Unexpected);
                }
            });

            if (city == null || stronghold == null)
            {
                return;
            }

            locker.Lock(city, stronghold).Do(() =>
            {
                var troopInitializer = troopObjectInitializerFactory.CreateCityTroopObjectInitializer(cityId, simpleStub, TroopBattleGroup.Attack, mode);

                var attackAction = actionFactory.CreateStrongholdAttackChainAction(cityId,
                                                                                   troopInitializer,
                                                                                   targetStrongholdId,
                                                                                   forceAttack: false);
                var result = city.Worker.DoPassive(city, attackAction, true);

                ReplyWithResult(session, packet, result);
            });
        }

        private void AttackBarbarianTribe(Session session, Packet packet)
        {
            uint cityId;
            uint targetBarbarianTribeId;
            ISimpleStub simpleStub;
            AttackMode mode;

            try
            {
                mode = (AttackMode)packet.GetByte();
                cityId = packet.GetUInt32();
                targetBarbarianTribeId = packet.GetUInt32();
                simpleStub = PacketHelper.ReadStub(packet, FormationType.Attack);
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            IBarbarianTribe barbarianTribe;

            if (!gameObjectLocator.TryGetObjects(targetBarbarianTribeId, out barbarianTribe))
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            locker.Lock(session.Player, barbarianTribe).Do(() =>
            {
                var city = session.Player.GetCity(cityId);
                if (city == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                var troopObjectInitializer = troopObjectInitializerFactory.CreateCityTroopObjectInitializer(cityId, simpleStub, TroopBattleGroup.Attack, mode);

                var attackAction = actionFactory.CreateBarbarianTribeAttackChainAction(cityId, targetBarbarianTribeId, troopObjectInitializer);

                var result = city.Worker.DoPassive(city, attackAction, true);

                ReplyWithResult(session, packet, result);
            });
        }

        private void AttackCity(Session session, Packet packet)
        {
            uint cityId;
            uint targetCityId;
            Position target;
            ISimpleStub simpleStub;
            AttackMode mode;

            try
            {
                mode = (AttackMode)packet.GetByte();
                cityId = packet.GetUInt32();
                targetCityId = packet.GetUInt32();
                target = new Position(packet.GetUInt32(), packet.GetUInt32());
                simpleStub = PacketHelper.ReadStub(packet, FormationType.Attack);
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            var cityNotFound = locker.Lock(session.Player).Do(() => session.Player.GetCity(cityId) == null);

            if (cityNotFound)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            Dictionary<uint, ICity> cities;
            locker.Lock(out cities, cityId, targetCityId).Do(() =>
            {
                if (cities == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                ICity city = cities[cityId];

                var troopInitializer = troopObjectInitializerFactory.CreateCityTroopObjectInitializer(cityId,
                                                                                                      simpleStub,
                                                                                                      TroopBattleGroup.Attack,
                                                                                                      mode);

                var attackAction = actionFactory.CreateCityAttackChainAction(cityId,
                                                                             troopInitializer,
                                                                             targetCityId,
                                                                             target);

                var result = city.Worker.DoPassive(city, attackAction, true);

                ReplyWithResult(session, packet, result);
            });
        }

        private void DefendCity(Session session, Packet packet)
        {
            uint cityId;
            uint targetCityId;
            ISimpleStub simpleStub;
            AttackMode mode;

            try
            {
                cityId = packet.GetUInt32();
                targetCityId = packet.GetUInt32();
                simpleStub = PacketHelper.ReadStub(packet, FormationType.Defense);
                mode = (AttackMode)packet.GetByte();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            if (cityId == targetCityId)
            {
                ReplyError(session, packet, Error.DefendSelf);
                return;
            }

            var cityNotFound = locker.Lock(session.Player).Do(() => session.Player.GetCity(cityId) == null);

            if (cityNotFound)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            Dictionary<uint, ICity> cities;
            locker.Lock(out cities, cityId, targetCityId).Do(() =>
            {
                ICity city = cities[cityId];

                var troopInitializer = troopObjectInitializerFactory.CreateCityTroopObjectInitializer(cityId,
                                                                                                      simpleStub,
                                                                                                      TroopBattleGroup.Defense,
                                                                                                      mode);

                var da = actionFactory.CreateCityDefenseChainAction(cityId, troopInitializer, targetCityId);
                var result = city.Worker.DoPassive(city, da, true);
                ReplyWithResult(session, packet, result);
            });
        }

        private void DefendStronghold(Session session, Packet packet)
        {
            uint cityId;
            uint targetStrongholdId;
            ISimpleStub simpleStub;
            AttackMode mode;

            try
            {
                cityId = packet.GetUInt32();
                targetStrongholdId = packet.GetUInt32();
                simpleStub = PacketHelper.ReadStub(packet, FormationType.Defense);
                mode = (AttackMode)packet.GetByte();
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            IStronghold stronghold = null;
            ICity city = null;
            locker.Lock(session.Player).Do(() =>
            {
                city = session.Player.GetCity(cityId);
                if (city == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                if (!gameObjectLocator.TryGetObjects(targetStrongholdId, out stronghold))
                {
                    ReplyError(session, packet, Error.Unexpected);
                }
            });

            if (stronghold == null || city == null)
            {
                return;
            }

            locker.Lock(city, stronghold).Do(() =>
            {
                var initializer = troopObjectInitializerFactory.CreateCityTroopObjectInitializer(cityId, simpleStub, TroopBattleGroup.Defense, mode);
                var da = actionFactory.CreateStrongholdDefenseChainAction(cityId, initializer, targetStrongholdId);
                Error ret = city.Worker.DoPassive(city, da, true);

                ReplyWithResult(session, packet, ret);
            });
        }

        private void Retreat(Session session, Packet packet)
        {
            uint cityId;
            ushort troopId;
            bool completeRetreat;
            ISimpleStub unitsToRetreat = null;

            try
            {
                cityId = packet.GetUInt32();
                troopId = packet.GetUInt16();
                completeRetreat = packet.GetByte() == 1;

                if (!completeRetreat)
                {
                    unitsToRetreat = PacketHelper.ReadStub(packet, FormationType.Defense);
                }
            }
            catch(Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            ICity city;
            IStation station = null;

            //we need to find out the stationed location first then reacquire local + stationed locks            
            locker.Lock(cityId, out city).Do(() =>
            {
                if (city == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                ITroopStub stub;

                if (!city.Troops.TryGetStub(troopId, out stub) || stub.Station == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                station = stub.Station;
            });

            if (station == null)
            {
                return;
            }

            locker.Lock(city, station).Do(() =>
            {
                ITroopStub stub;

                if (!city.Troops.TryGetStub(troopId, out stub))
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                //Make sure that the person sending the retreat is either the guy who owns the troop or the guy who owns the stationed city
                bool stationOwnerRetreating = session.Player.GetCityList().Any(x => x == stub.Station);
                if (city.Owner != session.Player && !stationOwnerRetreating)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                ITroopObjectInitializer troopInitializer;

                if (completeRetreat || unitsToRetreat.TotalCount == stub.TotalCount)
                {
                    troopInitializer = troopObjectInitializerFactory.CreateStationedTroopObjectInitializer(stub);
                }
                else
                {
                    troopInitializer = troopObjectInitializerFactory.CreateStationedPartialTroopObjectInitializer(stub, unitsToRetreat);
                }

                var ra = actionFactory.CreateRetreatChainAction(cityId, troopInitializer);

                Error ret = city.Worker.DoPassive(city, ra, true);
                ReplyWithResult(session, packet, ret);
            });
        }
    }
}