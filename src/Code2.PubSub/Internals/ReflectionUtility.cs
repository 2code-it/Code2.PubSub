using System;
using System.Linq;
using System.Reflection;


namespace Code2.PubSub.Internals
{
	internal class ReflectionUtility : IReflectionUtility
	{
		public (Type argType, Delegate handler)? GetMessageBusPublishEventHandler(EventInfo eventInfo, IMessageBus messageBus, int channel)
		{
			Type? messageType = eventInfo.EventHandlerType?.GetGenericArguments().FirstOrDefault();
			if (messageType is null) return null;
			var utility = GetMessageBusMappingUtility(messageType);
			return (messageType, utility.GetMessageBusPublishEventHandler(messageBus, channel));
		}

		public void MessageBusSubscribeObjectMethod(MethodInfo methodInfo, object instance, IMessageBus messageBus, int channel)
		{
			Type? messageType = methodInfo.GetParameters().FirstOrDefault()?.ParameterType;
			if (messageType == null) return;
			var utility = GetMessageBusMappingUtility(messageType);
			utility.MessageBusSubscribeObjectMethod(messageBus, channel, instance, methodInfo);
		}

		public Delegate GetMessageBusPublishAction(Type messageType, bool addChannelArg, IMessageBus messageBus) 
		{
			var utility = GetMessageBusMappingUtility(messageType);
			return utility.GetMessageBusPublishAction(messageBus, addChannelArg);
		}

		private IMessageBusMappingUtility GetMessageBusMappingUtility(Type type)
		{
			Type utilType = typeof(MessageBusMappingUtility<>).MakeGenericType(type);
			return (IMessageBusMappingUtility)Activator.CreateInstance(utilType)!;
		}

		private interface IMessageBusMappingUtility
		{
			Delegate GetMessageBusPublishEventHandler(IMessageBus messageBus, int channel);
			Delegate GetMessageBusPublishAction(IMessageBus messageBus, bool addChannelArg);
			void MessageBusSubscribeObjectMethod(IMessageBus messageBus, int channel, object instance, MethodInfo methodInfo);
			
		}

		private class MessageBusMappingUtility<T> : IMessageBusMappingUtility where T : class
		{

			public Delegate GetMessageBusPublishAction(IMessageBus messageBus, bool addChannelArg)
			{
				if(addChannelArg)
				{
					return new Action<T, int>((message, channel) =>
					{
						messageBus.Publish(message, channel);
					});
				}
				return new Action<T>((message) =>
				{
					messageBus.Publish(message);
				});
			}

			public Delegate GetMessageBusPublishEventHandler(IMessageBus messageBus, int channel)
			{
				EventHandler<T> handler = (sender, args) =>
				{
					messageBus.Publish(args, channel);
				};
				return handler;
			}

			public void MessageBusSubscribeObjectMethod(IMessageBus messageBus, int channel, object instance, MethodInfo methodInfo)
			{
				Action<object?, T> handler = (r, m) =>
				{
					methodInfo.Invoke(r, new[] { m });
				};

				messageBus.Subscribe(instance, handler, channel);
			}
		}
	}
}
