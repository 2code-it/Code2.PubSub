using Microsoft.VisualStudio.TestTools.UnitTesting;
using Code2.PubSub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Code2.PubSubTests;
using Code2.PubSubTests.Assets;

namespace Code2.PubSub.Tests
{
    [TestClass]
	public class MessageBusTests
	{
		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void Subscribe_When_RecipientWithSameMessageAndChannel_Expect_Exception()
		{
			MessageBus messageBus = new MessageBus();
			var testRecipient = new TestRecipient();
			Action<object?, TestMessage1> handler = (r, m) => { };
			messageBus.Subscribe(testRecipient, handler);
			messageBus.Subscribe(testRecipient, handler);
		}

		[TestMethod]
		public void Subscribe_When_RecipientWithSameMessageAndDifferentChannel_Expect_HandlerCall()
		{
			MessageBus messageBus = new MessageBus();
			var testRecipient = new TestRecipient();

			int calls = 0;
			Action<object?, TestMessage1> handler = (r, m) => { calls++; };
			messageBus.Subscribe(testRecipient, handler);
			messageBus.Subscribe(testRecipient, handler, 1);

			messageBus.Publish(new TestMessage1());

			Assert.AreEqual(1, calls);
		}

		[TestMethod]
		public void Subscribe_When_UsingDifferentChannel_Expect_NoHandlerCall()
		{
			MessageBus messageBus = new MessageBus();
			var testRecipient = new TestRecipient();

			int calls = 0;
			Action<object?, TestMessage1> handler = (r, m) => { calls++; };
			messageBus.Subscribe(testRecipient, handler, 0);

			messageBus.Publish(new TestMessage1(), 1);

			Assert.AreEqual(0, calls);
		}

		[TestMethod]
		public void Subscribe_When_UsingSameChannel_Expect_HandlerCall()
		{
			MessageBus messageBus = new MessageBus();
			var testRecipient = new TestRecipient();

			int calls = 0;
			Action<object?, TestMessage1> handler = (r, m) => { calls++; };
			messageBus.Subscribe(testRecipient, handler, 0);

			messageBus.Publish(new TestMessage1(), 0);

			Assert.AreEqual(1, calls);
		}

		[TestMethod]
		public void Unsubscribe_When_UnsubscribingRecipient_Expect_NoHandlerCall()
		{
			MessageBus messageBus = new MessageBus();
			var testRecipient1 = new TestRecipient();
			var testRecipient2 = new TestRecipient();

			int calls = 0;
			Action<object?, TestMessage1> handler1 = (r, m) => { calls++; };
			Action<object?, TestMessage1> handler2 = (r, m) => {  };
			messageBus.Subscribe(testRecipient1, handler1);
			messageBus.Subscribe(testRecipient2, handler2);
			messageBus.Unsubscribe(testRecipient1);

			messageBus.Publish(new TestMessage1());

			Assert.AreEqual(0, calls);
		}

		[TestMethod]
		public void Unsubscribe_When_UnsubscribingRecipientFromChannel_Expect_HandlerCallForOtherChannel()
		{
			MessageBus messageBus = new MessageBus();
			var testRecipient1 = new TestRecipient();

			int calls = 0;
			Action<object?, TestMessage1> handler1 = (r, m) => { calls++; };
			messageBus.Subscribe(testRecipient1, handler1, 0);
			messageBus.Subscribe(testRecipient1, handler1, 1);
			messageBus.Unsubscribe(testRecipient1, 0);

			messageBus.Publish(new TestMessage1(), 0);
			messageBus.Publish(new TestMessage1(), 1);

			Assert.AreEqual(1, calls);
		}

		[TestMethod]
		[DataRow(true, 0)]
		[DataRow(false, 1)]
		public void MessageBus_When_UsingOutOfScopeObject_Expect_HandlerCallCount(bool useWeakReference, int expectedCalls)
		{
			MessageBus messageBus = new MessageBus(useWeakReference);

			int calls = 0;
			Action<object?, TestMessage1> handler = (r, m) => { calls++; };
			SubscribeOutOfScope(messageBus, handler);

			GC.Collect();
			messageBus.Publish(new TestMessage1());

			Assert.AreEqual(expectedCalls, calls);
		}


		private void SubscribeOutOfScope(IMessageBus messageBus, Action<object?, TestMessage1> handler)
		{
			var recipient = new TestRecipient();
			messageBus.Subscribe(recipient, handler);
		}
	}
}