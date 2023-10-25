using System.Reflection;
using System;
using System.Collections.Generic;

namespace Code2.PubSub.Internals
{
	internal class ServiceRegistration
	{
		public ServiceRegistration(object instance, int channel)
		{
			Instance = instance;
			Channel = channel;
			Type = instance.GetType();

			_afterHostStartup = Type.GetMethod("AfterHostStartup");
			_beforeHostShutdown = Type.GetMethod("BeforeHostShutdown");
		}

		private readonly MethodInfo? _afterHostStartup;
		private readonly MethodInfo? _beforeHostShutdown;

		public Type Type { get; set; } = default!;
		public object Instance { get; set; } = default!;
		public int Channel { get; set; }
		public IDictionary<Type, Delegate> EventHandlers { get; private set; } = new Dictionary<Type, Delegate>();

		public void AfterHostStartup()
			=> _afterHostStartup?.Invoke(Instance, null);

		public void BeforeHostShutdown()
			=> _beforeHostShutdown?.Invoke(Instance, null);


	}
}
