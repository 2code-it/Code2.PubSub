using Code2.PubSub.Internals;
using System.Reflection;

namespace Code2.PubSub
{
	public class ServiceHost
	{
		public ServiceHost(ServiceHostOptions options) : this(options, new ReflectionUtility()) { }

		internal ServiceHost(ServiceHostOptions options, IReflectionUtility reflectionUtility)
		{
			_reflectionUtility = reflectionUtility;
			ConfigureInner(options);
		}

		private readonly IReflectionUtility _reflectionUtility;
		private const string _serviceRecieveMethodName = "Receive";

		private IMessageBus _messageBus = default!;
		private IServiceProvider? _serviceProvider;

		private IList<ServiceRegistration> _registrations = new List<ServiceRegistration>();

		public bool IsStarted { get; private set; }

		public void Startup()
		{
			if (IsStarted) throw new InvalidOperationException($"{nameof(ServiceHost)} already started");

			foreach (ServiceRegistration registration in _registrations)
			{
				MapEventsToMessageBus(registration);
				MapReceiveMethodsToMessageBus(registration);
			}
			Parallel.ForEach(_registrations, x => x.AfterHostStartup());

			IsStarted = true;
		}

		public void Shutdown()
		{
			Parallel.ForEach(_registrations, x => x.BeforeHostShutdown());
			foreach (ServiceRegistration registration in _registrations)
			{
				UnMapEventsFromMessageBus(registration);
				UnMapReceiveMethodsFromMessageBus(registration);
			}

			IsStarted = false;
		}

		public static ServiceHost Configure(Action<ServiceHostOptions> configureAction)
		{
			ServiceHostOptions options = new ServiceHostOptions();
			configureAction(options);
			return new ServiceHost(options);
		}

		private void ConfigureInner(ServiceHostOptions options)
		{
			if (options.MessageBus is null) throw new InvalidOperationException($"Required option {nameof(ServiceHostOptions.MessageBus)} missing");
			if (options.Services.Count == 0) throw new InvalidOperationException($"Configure at least one service");

			_messageBus = options.MessageBus;
			_serviceProvider = options.ServiceProvider;

			foreach (var serviceInfo in options.Services)
			{
				object? serviceObject = serviceInfo.Instance;
				if (serviceObject is null)
				{
					if (serviceInfo.Type is null)
						throw new InvalidOperationException($"Options {nameof(ServiceHostOptions.ServiceInfo.Instance)} and {nameof(ServiceHostOptions.ServiceInfo.Type)} are both undefined");

					serviceObject = _serviceProvider is null
						? Activator.CreateInstance(serviceInfo.Type)
						: _serviceProvider.GetService(serviceInfo.Type);

					if (serviceObject is null)
						throw new InvalidOperationException($"Failed to create instance of type {serviceInfo.Type.Name}");
				}

				_registrations.Add(new ServiceRegistration(serviceObject, serviceInfo.Channel));
			}

			if (options.AutoStart) Startup();
		}

		private void UnMapReceiveMethodsFromMessageBus(ServiceRegistration registration)
		{

		}

		private void UnMapEventsFromMessageBus(ServiceRegistration registration)
		{

		}

		private void MapReceiveMethodsToMessageBus(ServiceRegistration registration)
		{
			var receiveMethods = registration.Type.GetMethods().Where(x => x.IsPublic && x.Name == _serviceRecieveMethodName);
			foreach (var receiveMethod in receiveMethods)
			{
				_reflectionUtility.MessageBusSubscribeObjectMethod(receiveMethod, registration.Instance, _messageBus, registration.Channel);
			}
		}

		private void MapEventsToMessageBus(ServiceRegistration registration)
		{
			var events = registration.Type.GetEvents();
			foreach (EventInfo eventInfo in events)
			{
				var typeHandlerPair = _reflectionUtility.GetMessageBusPublishEventHandler(eventInfo, _messageBus, registration.Channel);
				if (typeHandlerPair is null) continue;

				registration.EventHandlers.Add(typeHandlerPair.Value.argType, typeHandlerPair.Value.handler);
				eventInfo.AddEventHandler(registration.Instance, typeHandlerPair.Value.handler);
			}
		}
	}
}
