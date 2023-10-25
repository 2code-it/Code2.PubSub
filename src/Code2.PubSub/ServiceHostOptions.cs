
using System;
using System.Collections.Generic;

namespace Code2.PubSub
{
	public class ServiceHostOptions
	{
		public IServiceProvider? ServiceProvider { get; set; }
		public IMessageBus? MessageBus { get; set; }
		public bool AutoStart { get; set; } = true;
		public IList<ServiceInfo> Services { get; private set; } = new List<ServiceInfo>();

		public void AddService(Type serviceType, int channel = 0)
			=> Services.Add(new ServiceInfo { Type = serviceType, Channel = channel });

		public void AddService(object instance, int channel = 0)
			=> Services.Add(new ServiceInfo { Instance = instance, Channel = channel });

		public void AddService<TService>(int channel = 0) where TService : class
			=> AddService(typeof(TService), channel);

		public class ServiceInfo
		{
			public Type? Type { get; set; }
			public object? Instance { get; set; }
			public int Channel { get; set; }
		}

	}
}
