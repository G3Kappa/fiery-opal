using SadConsole;
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

    public class MessageSentEventArgs<T> : EventArgs
    {
        public Guid From;
        public Guid To;
        public Func<T, string> Message;

        public MessageSentEventArgs(Guid from, Guid to, Func<T, string> message)
        {
            From = from;
            To = to;
            Message = message;
        }
    }

    public delegate void MessageSentDelegate<T>(MessageSentEventArgs<T> _args);

    public class MessagePipeline<T> : IDisposable where T : IPipelineSubscriber<T>
    {
        protected Dictionary<Guid, IPipelineSubscriber<T>> Subscribers { get; }
        protected static readonly Dictionary<Guid, MessagePipeline<T>> Pipelines = new Dictionary<Guid, MessagePipeline<T>>();
        public event MessageSentDelegate<T> OnMessageSent; 

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
                Guid from = sender == null ? Guid.Empty : sender.Handle;
                sub.ReceiveMessage(Handle, from, action, true);
                OnMessageSent?.Invoke(new MessageSentEventArgs<T>(from, sub.Handle, action));
            }
        }

        public bool Unicast(T sender, Guid receiver_handle, Func<T, string> action)
        {
            if (!Subscribers.ContainsKey(receiver_handle)) return false;

            Guid from = sender == null ? Guid.Empty : sender.Handle;
            Subscribers[receiver_handle].ReceiveMessage(Handle, from, action, false);
            OnMessageSent?.Invoke(new MessageSentEventArgs<T>(from, receiver_handle, action));
            return true;
        }

        public void Dispose()
        {
            Pipelines.Remove(Handle);
        }
    }

    public static class MessagePipelineExtensions
    {
        public static void BroadcastLogMessage(this MessagePipeline<OpalConsoleWindow> self, OpalConsoleWindow sender, ColoredString msg, bool debug)
        {
            self.Broadcast(null, new Func<OpalConsoleWindow, string>(ocw => { if (ocw is OpalLogWindow) { (ocw as OpalLogWindow).Log(msg, debug); }; return "BroadcastLogMessage"; }));
        }
    }
}
