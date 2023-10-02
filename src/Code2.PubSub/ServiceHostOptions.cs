namespace Code2.PubSub
{
	public class ServiceHostOptions
	{
		public IServiceProvider? ServiceProvider { get; set; }
		public IMessageBus? MessageBus { get; set; }
		public bool AutoStart { get; set; } = true;

		public IList<ServiceInfo> Services { get; private set; } = new List<ServiceInfo>();

		public ServiceHostOptions AddService(object instance, int channel = 0)
		{
			Services.Add(new ServiceInfo { Instance = instance, Channel = channel });
			return this;
		}

		public ServiceHostOptions AddService(Type serviceType, int channel = 0)
		{
			Services.Add(new ServiceInfo { Type = serviceType, Channel = channel });
			return this;
		}

		public ServiceHostOptions AddService<TService>(int channel = 0) where TService : class
			=> AddService(typeof(TService), channel);

		public class ServiceInfo
		{
			public Type? Type { get; set; }
			public object? Instance { get; set; }
			public int Channel { get; set; }
		}

	}
}
