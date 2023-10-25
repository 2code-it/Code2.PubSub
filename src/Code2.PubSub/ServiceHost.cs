using Code2.PubSub.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Code2.PubSub
{
	public class ServiceHost
	{
		public ServiceHost(ServiceHostOptions options) : this(options, new ReflectionUtility()) { }

		internal ServiceHost(ServiceHostOptions options, IReflectionUtility reflectionUtility)
		{
			_reflectionUtility = reflectionUtility;
			Configure(options);
		}

		private readonly IReflectionUtility _reflectionUtility;
		private const string _serviceSubscribeMethodName = "Subscribe";
		private const string _servicePublishPropertyPrefix= "Publish";

		private IMessageBus _messageBus = default!;
		private IServiceProvider? _serviceProvider;

		private IList<ServiceRegistration> _registrations = new List<ServiceRegistration>();

		public bool IsStarted { get; private set; }

		public void Startup()
		{
			if (IsStarted) throw new InvalidOperationException($"{nameof(ServiceHost)} already started");

			foreach (ServiceRegistration registration in _registrations)
			{
				SetMessageBusPublishDelegates(registration);
				MapEventsToMessageBus(registration);
				MapSubscribeMethodsToMessageBus(registration);
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
				UnMapSubscribeMethodsFromMessageBus(registration);
			}

			IsStarted = false;
		}

		public static ServiceHost CreateWith(Action<ServiceHostOptions> configureAction)
		{
			ServiceHostOptions options = new ServiceHostOptions();
			configureAction(options);
			return new ServiceHost(options);
		}

		public void Configure(ServiceHostOptions options)
		{
			var services = options.Services.ToArray();
			if (services.Length == 0) throw new InvalidOperationException($"No services defined");

			IMessageBus? messageBus = options.MessageBus ?? (IMessageBus?)options.ServiceProvider?.GetService(typeof(IMessageBus));
			if (messageBus is null) throw new InvalidOperationException($"Required option {nameof(ServiceHostOptions.MessageBus)} missing");

			_messageBus = messageBus;
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

		private void UnMapSubscribeMethodsFromMessageBus(ServiceRegistration registration)
		{

		}

		private void UnMapEventsFromMessageBus(ServiceRegistration registration)
		{

		}

		private void MapSubscribeMethodsToMessageBus(ServiceRegistration registration)
		{
			var receiveMethods = registration.Type.GetMethods().Where(x => x.IsPublic && x.Name == _serviceSubscribeMethodName);
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

		private void SetMessageBusPublishDelegates(ServiceRegistration registration)
		{
			var properties = GetMessageBusPublishProperties(registration);
			foreach (var property in properties)
			{
				var messageType = property.PropertyType.GetGenericArguments().FirstOrDefault();
				if (messageType is null) continue;
				int genericArgsLength = property.PropertyType.GetGenericArguments().Length;
				Delegate publishAction = _reflectionUtility.GetMessageBusPublishAction(messageType, genericArgsLength == 2, _messageBus);
				property.SetValue(registration.Instance, publishAction);
			}
		}

		private PropertyInfo[] GetMessageBusPublishProperties(ServiceRegistration registration)
		{
			Type[] propertyTypeFilter = new[] { typeof(Action<,>), typeof(Action<>) };
			return registration.Type.GetProperties()
				.Where(x =>
					x.CanWrite
					&& x.Name.StartsWith(_servicePublishPropertyPrefix)
					&& propertyTypeFilter.Contains(x.PropertyType.GetGenericTypeDefinition())
				).ToArray();
		}
	}
}
