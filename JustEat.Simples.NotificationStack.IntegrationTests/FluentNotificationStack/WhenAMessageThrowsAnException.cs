﻿using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading;
using Tests.MessageStubs;

namespace NotificationStack.IntegrationTests.FluentNotificationStack
{
    [TestFixture]
    public class WhenAMessageThrowsAnException
    {
        private readonly IHandler<GenericMessage> _handler = Substitute.For<IHandler<GenericMessage>>();
        private JustEat.Simples.NotificationStack.Stack.FluentNotificationStack _publisher;
        private Action<Exception> _globalErrorHandler;
        private bool _handledException;

        [SetUp]
        public void Given()
        {
            _handler.Handle(Arg.Any<GenericMessage>()).Returns(true).AndDoes(ex => { throw new Exception("My Ex"); });
            _globalErrorHandler = ex => { _handledException = true; };
            var publisher = JustEat.Simples.NotificationStack.Stack.FluentNotificationStack.Register(c =>
                                                                        {
                                                                            c.Component = "TestHarness";
                                                                            c.Tenant = "Wherever";
                                                                            c.Environment = "integration";
                                                                            c.PublishFailureBackoffMilliseconds = 1;
                                                                            c.PublishFailureReAttempts = 3;
                                                                        })
                                                                        .WithMonitoring(Substitute.For<IMessageMonitor>())
                .WithSnsMessagePublisher<GenericMessage>("CustomerCommunication")
                .WithSqsTopicSubscriber("CustomerCommunication", 60, instancePosition: 1, onError: _globalErrorHandler)
                .WithMessageHandler(_handler);

            publisher.StartListening();
            _publisher = publisher;
        }

        [Then]
        public void CustomExceptionHandlingIsCalled()
        {
            _publisher.Publish(new GenericMessage());
            Thread.Sleep(500);
            _handler.Received().Handle(Arg.Any<GenericMessage>());
            Thread.Sleep(200);
            Assert.That(_handledException, Is.EqualTo(true));
        }

        [TearDown]
        public void ByeBye()
        {
            _publisher.StopListening();
            _publisher = null;
        }
    }
}