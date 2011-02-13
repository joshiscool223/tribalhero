#region

using System;
using System.Collections.Generic;

#endregion

namespace Game.Util
{
    public class Channel
    {
        #region Structs

        private class Subscriber
        {
            public IChannel Session { get; private set; }
            public List<string> Channels { get; private set; }

            public Subscriber(IChannel session)
            {
                Session = session;
                Channels = new List<string>();
            }
        }

        #endregion

        #region Members

        private readonly object channelLock = new Object();

        private readonly Dictionary<string, List<Subscriber>> subscribersByChannel = new Dictionary<string, List<Subscriber>>();

        private readonly Dictionary<IChannel, Subscriber> subscribersBySession = new Dictionary<IChannel, Subscriber>();

        #endregion

        #region Events

        public delegate void OnPost(IChannel session, object custom);

        #endregion

        #region Methods

        public void Post(string channelId, object message)
        {
            lock (channelLock)
            {
                if (!subscribersByChannel.ContainsKey(channelId))
                    return;
                foreach (var sub in subscribersByChannel[channelId])
                    sub.Session.OnPost(message);
            }
        }

        public void Subscribe(IChannel session, string channelId)
        {
            lock (channelLock)
            {
                Subscriber sub;
                List<Subscriber> sublist;
                // Check if the channel list already exists. To keep memory down, we don't keep around subscription lists
                // for channels that have no subscribers
                if (!subscribersByChannel.TryGetValue(channelId, out sublist))
                {
                    sublist = new List<Subscriber>();
                    subscribersByChannel.Add(channelId, sublist);
                }

                // Check if there is already a subscription object for this session
                if (subscribersBySession.TryGetValue(session, out sub))
                {
                    // If subscription already exists then throw exception
                    if (sublist.Contains(sub))
                        throw new DuplicateSubscriptionException();

                    sub.Channels.Add(channelId);
                }
                        // If not we need to make an object for this session
                else
                {
                    sub = new Subscriber(session);
                    sub.Channels.Add(channelId);
                    subscribersBySession.Add(session, sub);
                }

                sublist.Add(sub);
            }
            return;
        }

        public bool Unsubscribe(IChannel session, string channelId)
        {
            lock (channelLock)
            {
                Subscriber sub;
                if (subscribersBySession.TryGetValue(session, out sub))
                {
                    sub.Channels.Remove(channelId);
                    if (sub.Channels.Count == 0)
                        subscribersBySession.Remove(session);

                    List<Subscriber> sublist;
                    if (subscribersByChannel.TryGetValue(channelId, out sublist))
                    {
                        sublist.Remove(sub);

                        if (sublist.Count == 0)
                            subscribersByChannel.Remove(channelId);
                    }

                    return true;
                }
            }
            return false;
        }

        public int SubscriptionCount(IChannel session)
        {
            lock (channelLock)
            {
                Subscriber sub;
                if (subscribersBySession.TryGetValue(session, out sub))
                    return sub.Channels.Count;
            }

            return 0;
        }

        public bool Unsubscribe(IChannel session)
        {
            lock (channelLock)
            {
                Subscriber sub;
                if (subscribersBySession.TryGetValue(session, out sub))
                {
                    foreach (var id in sub.Channels)
                    {
                        List<Subscriber> sublist;

                        if (!subscribersByChannel.TryGetValue(id, out sublist))
                            continue;

                        sublist.Remove(sub);

                        if (sublist.Count == 0)
                            subscribersByChannel.Remove(id);
                    }

                    sub.Channels.Clear();
                    subscribersBySession.Remove(session);
                    return true;
                }
            }
            return false;
        }

        public bool Unsubscribe(string channelId)
        {
            lock (channelLock)
            {
                List<Subscriber> subs;
                if (subscribersByChannel.TryGetValue(channelId, out subs))
                {
                    foreach (var sub in subs)
                    {
                        sub.Channels.Remove(channelId);
                        if (sub.Channels.Count == 0)
                            subscribersBySession.Remove(sub.Session);
                    }
                    subscribersByChannel.Remove(channelId);
                    return true;
                }
            }
            return false;
        }

        #endregion
    }

    public class DuplicateSubscriptionException : Exception
    {
    }
}