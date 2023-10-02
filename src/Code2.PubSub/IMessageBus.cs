namespace Code2.PubSub
{
	public interface IMessageBus
	{
		void Subscribe<Trecipient, Tmessage>(Trecipient recipient, Action<object?, Tmessage> messageHandler, int channel = 0)
			where Trecipient : class
			where Tmessage : class;

		Tmessage Publish<Tmessage>(Tmessage message, int channel = 0)
			where Tmessage : class;

		void Unsubscribe<Trecipient>(Trecipient recipient, int? channel = 0)
			where Trecipient : class;

		void Unsubscribe<Trecipient, Tmessage>(Trecipient recipient, int? channel = 0)
			where Trecipient : class
			where Tmessage : class;

		void Clear();
	}
}