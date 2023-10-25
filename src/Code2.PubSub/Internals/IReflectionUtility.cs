using System;
using System.Reflection;

namespace Code2.PubSub.Internals
{
	internal interface IReflectionUtility
	{
		(Type argType, Delegate handler)? GetMessageBusPublishEventHandler(EventInfo eventInfo, IMessageBus messageBus, int channel);
		Delegate GetMessageBusPublishAction(Type messageType, bool addChannelArg, IMessageBus messageBus);
		void MessageBusSubscribeObjectMethod(MethodInfo methodInfo, object instance, IMessageBus messageBus, int channel);
		
	}
}