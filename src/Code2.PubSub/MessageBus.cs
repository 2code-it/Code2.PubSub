using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Code2.PubSub
{
	public class MessageBus : IMessageBus
	{
		public MessageBus() : this(false) { }
		public MessageBus(bool useWeakReferences)
		{
			_useWeakReferences = useWeakReferences;
		}

		private IDictionary<Type, IDictionary<int, IList<ISubscription>>> _subscriptions = new Dictionary<Type, IDictionary<int, IList<ISubscription>>>();
		private readonly bool _useWeakReferences;
		private readonly object _lock = new();


		public void Subscribe<Trecipient, Tmessage>(Trecipient recipient, Action<object?, Tmessage> messageHandler, int channel = 0)
			where Trecipient : class
			where Tmessage : class
		{
			ISubscription registration = _useWeakReferences
				? new WeakReferenceSubscription<Tmessage>(recipient, messageHandler)
				: new StrongReferenceSubscription<Tmessage>(recipient, messageHandler);

			IList<ISubscription> list = GetSubscriptionList(typeof(Tmessage), channel)!;
			lock (_lock)
			{
				if (list.Any(x => x.Recipient == recipient)) throw new InvalidOperationException($"{typeof(Trecipient).Name} already subscribed to {typeof(Tmessage)}");
				list.Add(registration);
			}
		}

		public void Unsubscribe<Trecipient>(Trecipient recipient, int? channel = null)
			where Trecipient : class
			=> Unsubscribe(recipient, null, channel);


		public void Unsubscribe<Trecipient, Tmessage>(Trecipient recipient, int? channel = null)
			where Trecipient : class
			where Tmessage : class
			=> Unsubscribe(recipient, typeof(Tmessage), channel);


		public Tmessage Publish<Tmessage>(Tmessage message, int channel = 0)
			where Tmessage : class
		{
			var lists = GetSubscriptionLists(typeof(Tmessage), channel);
			if (lists.Length == 0) return message;

			ISubscription[] subscriptions;
			lock (_lock)
			{
				subscriptions = lists.SelectMany(x=>x).Where(x => x.IsAlive).ToArray();
			}
			Parallel.ForEach(subscriptions, x => x.Send(message));
			return message;
		}

		public void Clear()
		{
			lock (_lock)
			{
				_subscriptions.Clear();
			}
		}

		private void Unsubscribe(object recipient, Type? messageType = null, int? channel = null)
		{
			var lists = GetSubscriptionLists(messageType, channel);
			lock (_lock)
			{
				foreach (var list in lists)
				{
					var item = list.FirstOrDefault(x => x.Recipient == recipient);
					if (item is null) continue;
					list.Remove(item);
				}
			}
		}

		private IList<ISubscription>[] GetSubscriptionLists(Type? messageType, int? channel)
		{
			lock (_lock)
			{
				return _subscriptions.Where(x => messageType is null || x.Key == messageType)
						   .SelectMany(x => x.Value)
						   .Where(x => channel is null || x.Key == channel)
						   .Select(x => x.Value)
						   .ToArray();
			}
		}


		private IList<ISubscription>? GetSubscriptionList(Type messageType, int channel)
		{
			lock (_lock)
			{
				if (!_subscriptions.ContainsKey(messageType))
				{
					_subscriptions.Add(messageType, new Dictionary<int, IList<ISubscription>>());
				}

				if (!_subscriptions[messageType].ContainsKey(channel))
				{
					_subscriptions[messageType].Add(channel, new List<ISubscription>());
				}

				return _subscriptions[messageType][channel];
			}
		}


		private interface ISubscription
		{
			bool IsAlive { get; }
			object? Recipient { get; }
			void Send(object message);
		}

		private class StrongReferenceSubscription<Tmessage> : Subscription<Tmessage>
		{
			public StrongReferenceSubscription(object recipient, Action<object?, Tmessage> handler) : base(recipient, handler)
			{
				IsAlive = true;
			}

			public override bool IsAlive { get; protected set; }
			public override object? Recipient { get; protected set; }
		}

		private class WeakReferenceSubscription<Tmessage> : Subscription<Tmessage>
		{
			public WeakReferenceSubscription(object recipient, Action<object?, Tmessage> handler)
				: base(recipient, handler) { }

			private WeakReference _recipientRef = default!;
			public override bool IsAlive
			{
				get => _recipientRef.IsAlive;
				protected set => throw new NotSupportedException();
			}
			public override object? Recipient
			{
				get => _recipientRef.Target;
				protected set => _recipientRef = new WeakReference(value);
			}
		}


		private abstract class Subscription<Tmessage> : ISubscription
		{
			public Subscription(object recipient, Action<object?, Tmessage> handler)
			{
				Recipient = recipient;
				_handler = handler;
			}

			protected Action<object?, Tmessage>? _handler;
			public abstract bool IsAlive { get; protected set; }
			public abstract object? Recipient { get; protected set; }

			public void Send(object message)
			{
				if (!IsAlive) return;
				_handler?.Invoke(Recipient, (Tmessage)message);
			}
		}
	}
}
