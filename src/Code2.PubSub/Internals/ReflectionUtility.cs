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

		private IMessageBusMappingUtility GetMessageBusMappingUtility(Type type)
		{
			Type utilType = typeof(MessageBusMappingUtility<>).MakeGenericType(type);
			return (IMessageBusMappingUtility)Activator.CreateInstance(utilType)!;
		}

		private interface IMessageBusMappingUtility
		{
			Delegate GetMessageBusPublishEventHandler(IMessageBus messageBus, int channel);
			void MessageBusSubscribeObjectMethod(IMessageBus messageBus, int channel, object instance, MethodInfo methodInfo);
		}

		private class MessageBusMappingUtility<T> : IMessageBusMappingUtility where T : class
		{
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
