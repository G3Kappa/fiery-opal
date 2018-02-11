using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.UI
{
    public interface IPipelineSubscriber<T>
    {
        Guid Handle { get; }

        void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<T, string> msg, bool is_broadcast);
    }

    public class MessagePipeline<T> : IDisposable where T : IPipelineSubscriber<T>
    {
        protected Dictionary<Guid, IPipelineSubscriber<T>> Subscribers { get; }
        protected static readonly Dictionary<Guid, MessagePipeline<T>> Pipelines = new Dictionary<Guid, MessagePipeline<T>>();

        public Guid Handle { get; }

        public static MessagePipeline<T> GetPipeline(Guid handle)
        {
            if (Pipelines.ContainsKey(handle))
            {
                return Pipelines[handle];
            }
            return null;
        }

        public static IEnumerable<Guid> GetSubscribedPipelines(Guid window_handle)
        {
            foreach(MessagePipeline<T> mp in Pipelines.Values)
            {
                if(mp.Subscribers.ContainsKey(window_handle)) {
                    yield return mp.Handle;
                }
            }
        }

        public MessagePipeline()
        {
            Subscribers = new Dictionary<Guid, IPipelineSubscriber<T>>();
            Handle = Guid.NewGuid();
            Pipelines.Add(Handle, this);
        }

        public bool Subscribe(T obj)
        {
            if (Subscribers.ContainsKey(obj.Handle)) return false;
            Subscribers[obj.Handle] = obj;
            return true;
        }

        public bool Unsubscribe(T obj)
        {
            if (!Subscribers.ContainsKey(obj.Handle)) return false;
            Subscribers.Remove(obj.Handle);
            return true;
        }

        public IPipelineSubscriber<T> GetSubscriber(Guid handle)
        {
            if (!Subscribers.ContainsKey(handle)) return null;
            return Subscribers[handle];
        }

        public void Broadcast(T sender, Func<T, string> action)
        {
            foreach(IPipelineSubscriber<T> sub in Subscribers.Values)
            {
                sub.ReceiveMessage(Handle, sender == null ? Guid.Empty : sender.Handle, action, true);
            }
        }

        public bool Unicast(T sender, Guid receiver_handle, Func<T, string> msg)
        {
            if (!Subscribers.ContainsKey(receiver_handle)) return false;
            Subscribers[receiver_handle].ReceiveMessage(Handle, sender == null ? Guid.Empty : sender.Handle, msg, false);
            return true;
        }

        public void Dispose()
        {
            Pipelines.Remove(Handle);
        }
    }
}
