#region

using System;
using System.Collections.Generic;
using Game.Data;
using Game.Data.Tribe;
using Game.Database;
using Game.Logic.Formulas;
using Game.Logic.Procedures;
using Game.Map;
using Game.Setup;
using Game.Util;
using System.Linq;
using Game.Util.Locking;

#endregion

namespace Game.Comm.ProcessorCommands
{
    class TribeCommandsModule : CommandModule
    {
        private readonly ITribeFactory tribeFactory;

        public TribeCommandsModule(ITribeFactory tribeFactory)
        {
            this.tribeFactory = tribeFactory;
        }

        public override void RegisterCommands(Processor processor)
        {
            processor.RegisterCommand(Command.TribeNameGet, GetName);
            processor.RegisterCommand(Command.TribeInfo, GetInfo);
            processor.RegisterCommand(Command.TribeCreate, Create);
            processor.RegisterCommand(Command.TribeDelete, Delete);
            processor.RegisterCommand(Command.TribeUpgrade, Upgrade);
            processor.RegisterCommand(Command.TribeSetDescription, SetDescription);
            processor.RegisterCommand(Command.TribePublicInfo, GetPublicInfo);
        }

        private void SetDescription(Session session, Packet packet)
        {
            string description;
            try
            {
                description = packet.GetString();
            }
            catch (Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            using (Concurrency.Current.Lock(session.Player))
            {
                if (session.Player.Tribesman == null)
                {
                    ReplyError(session, packet, Error.TribeIsNull);
                    return;
                }

                if (!session.Player.Tribesman.Tribe.IsOwner(session.Player))
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                if (description.Length > Player.MAX_DESCRIPTION_LENGTH)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                session.Player.Tribesman.Tribe.Description = description;

                ReplySuccess(session, packet);
            }
        }

        private void GetName(Session session, Packet packet)
        {
            var reply = new Packet(packet);

            byte count;
            uint[] tribeIds;
            try
            {
                count = packet.GetByte();
                tribeIds = new uint[count];
                for (int i = 0; i < count; i++)
                    tribeIds[i] = packet.GetUInt32();
            }
            catch (Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            reply.AddByte(count);
            for (int i = 0; i < count; i++)
            {
                uint tribeId = tribeIds[i];
                ITribe tribe;

                if (!Global.Tribes.TryGetValue(tribeId, out tribe))
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                reply.AddUInt32(tribeId);
                reply.AddString(tribe.Name);
            }

            session.Write(reply);
        }

        private void GetInfo(Session session, Packet packet)
        {
            var reply = new Packet(packet);
            if (session.Player.Tribesman == null)
            {
                ReplyError(session, packet, Error.TribeIsNull);
                return;
            }
            var tribe = session.Player.Tribesman.Tribe;

            using (Concurrency.Current.Lock(tribe))
            {
                if (tribe == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                reply.AddUInt32(tribe.Id);
                reply.AddUInt32(tribe.Owner.PlayerId);
                reply.AddByte(tribe.Level);
                reply.AddString(tribe.Name);
                reply.AddString(tribe.Description);
                PacketHelper.AddToPacket(tribe.Resource, reply);

                reply.AddInt16((short)tribe.Count);
                foreach (var tribesman in tribe.Tribesmen)
                {
                    reply.AddUInt32(tribesman.Player.PlayerId);
                    reply.AddString(tribesman.Player.Name);
                    reply.AddInt32(tribesman.Player.GetCityCount());
                    reply.AddByte(tribesman.Rank);
                    reply.AddUInt32(tribesman.Player.IsLoggedIn ? 0 : UnixDateTime.DateTimeToUnix(tribesman.Player.LastLogin));
                    PacketHelper.AddToPacket(tribesman.Contribution, reply);
                }

                // Incoming List
                var incomingList = tribe.GetIncomingList().ToList();
                reply.AddInt16((short)incomingList.Count());
                foreach (var incoming in incomingList)
                {
                    // Target
                    ICity targetCity;
                    if (World.Current.TryGetObjects(incoming.Action.To, out targetCity))
                    {
                        reply.AddUInt32(targetCity.Owner.PlayerId);
                        reply.AddUInt32(targetCity.Id);
                        reply.AddString(targetCity.Owner.Name);
                        reply.AddString(targetCity.Name);
                    }
                    else
                    {
                        reply.AddUInt32(0);
                        reply.AddUInt32(0);
                        reply.AddString("N/A");
                        reply.AddString("N/A");
                    }

                    // Attacker
                    reply.AddUInt32(incoming.Action.WorkerObject.City.Owner.PlayerId);
                    reply.AddUInt32(incoming.Action.WorkerObject.City.Id);
                    reply.AddString(incoming.Action.WorkerObject.City.Owner.Name);
                    reply.AddString(incoming.Action.WorkerObject.City.Name);

                    reply.AddUInt32(UnixDateTime.DateTimeToUnix(incoming.Action.EndTime.ToUniversalTime()));
                }

                // Assignment List
                reply.AddInt16(tribe.AssignmentCount);
                foreach (var assignment in tribe.Assignments)
                {
                    PacketHelper.AddToPacket(assignment, reply);
                }

                session.Write(reply);
            }
        }

        private void GetPublicInfo(Session session, Packet packet)
        {
            var reply = new Packet(packet);
            uint id;
            try
            {
                id = packet.GetUInt32();
            }
            catch (Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            ITribe tribe;

            using (Concurrency.Current.Lock(id, out tribe))
            {
                if (tribe == null)
                {
                    ReplyError(session, packet, Error.Unexpected);
                    return;
                }

                reply.AddUInt32(tribe.Id);
                reply.AddString(tribe.Name);
                reply.AddInt16((short)tribe.Count);
                foreach (var tribesman in tribe.Tribesmen)
                {
                    reply.AddUInt32(tribesman.Player.PlayerId);
                    reply.AddString(tribesman.Player.Name);
                    reply.AddInt32(tribesman.Player.GetCityCount());
                    reply.AddByte(tribesman.Rank);
                }

                session.Write(reply);
            }
        }

        private void Create(Session session, Packet packet)
        {
            string name;
            try
            {
                name = packet.GetString();
            }
            catch (Exception)
            {
                ReplyError(session, packet, Error.Unexpected);
                return;
            }

            using (Concurrency.Current.Lock(session.Player))
            {
                if (session.Player.Tribesman != null)
                {
                    ReplyError(session, packet, Error.TribesmanAlreadyInTribe);
                    return;
                }

                if (World.Current.TribeNameTaken(name))
                {
                    ReplyError(session, packet, Error.TribeAlreadyExists);
                    return;
                }

                if (!Tribe.IsNameValid(name))
                {
                    ReplyError(session, packet, Error.TribeNameInvalid);
                    return;
                }

                if (session.Player.GetCityList().Count(city => city.Lvl >= 10) < 1)
                {
                    ReplyError(session, packet, Error.EffectRequirementNotMet);
                    return;
                }

                ITribe tribe = tribeFactory.CreateTribe(session.Player, name);
                Global.Tribes.Add(tribe.Id, tribe);
                DbPersistance.Current.Save(tribe);

                var tribesman = new Tribesman(tribe, session.Player, 0);
                tribe.AddTribesman(tribesman);

                Global.Channel.Subscribe(session, "/TRIBE/" + tribe.Id);
                ReplySuccess(session, packet);
            }
        }

        private void Delete(Session session, Packet packet)
        {
            if (session.Player.Tribesman == null)
            {
                ReplyError(session, packet, Error.TribeIsNull);
                return;
            }

            ITribe tribe = session.Player.Tribesman.Tribe;
            using (Concurrency.Current.Lock(custom => tribe.Tribesmen.ToArray(), new object[] { }, tribe))
            {
                if (!session.Player.Tribesman.Tribe.IsOwner(session.Player))
                {
                    ReplyError(session, packet, Error.TribesmanNotAuthorized);
                    return;
                }

                if (tribe.AssignmentCount > 0)
                {
                    ReplyError(session, packet, Error.TribeHasAssignment);
                    return;
                }

                foreach (var tribesman in new List<ITribesman>(tribe.Tribesmen))
                {
                    if (tribesman.Player.Session != null)
                        Procedure.Current.OnSessionTribesmanQuit(tribesman.Player.Session, tribe.Id, tribesman.Player.PlayerId, true);
                    tribe.RemoveTribesman(tribesman.Player.PlayerId);
                }

                Global.Tribes.Remove(tribe.Id);
                DbPersistance.Current.Delete(tribe);
            }

            ReplySuccess(session, packet);
        }

        private void Upgrade(Session session, Packet packet)
        {
            if (!session.Player.Tribesman.Tribe.IsOwner(session.Player))
            {
                ReplyError(session, packet, Error.TribesmanNotAuthorized);
                return;
            }

            ITribe tribe = session.Player.Tribesman.Tribe;
            using (Concurrency.Current.Lock(tribe))
            {
                if (tribe.Level >= 20)
                {
                    ReplyError(session, packet, Error.TribeMaxLevel);
                    return;
                }
                Resource cost = Formula.Current.GetTribeUpgradeCost(tribe.Level);
                if (!tribe.Resource.HasEnough(cost))
                {
                    ReplyError(session, packet, Error.ResourceNotEnough);
                    return;
                }

                tribe.Upgrade();
            }

            ReplySuccess(session, packet);
        }
    }
}