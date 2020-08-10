﻿using Rebus.Bus;
using Rebus.CircuitBreaker;
using Rebus.Logging;
using Rebus.Retry;
using Rebus.Threading;
using Rebus.Time;
using System;
using System.Collections.Generic;

namespace Rebus.Config
{
    /// <summary>
    /// Configuration extensions for the Circuit breakers
    /// </summary>
    public static class CircuitBreakerConfigurationExtensions
    {
        /// <summary>
        /// Enabling fluent configuration of circuit breakers
        /// </summary>
        /// <param name="optionsConfigurer"></param>
        /// <param name="circuitBreakerBuilder"></param>
        public static void EnableCircuitBreaker(this OptionsConfigurer optionsConfigurer, Action<CircuitBreakerConfigurationBuilder> circuitBreakerBuilder)
        {
            var builder = new CircuitBreakerConfigurationBuilder();
            circuitBreakerBuilder?.Invoke(builder);
            var circuitBreakers = builder.Build();

            optionsConfigurer.Decorate<IErrorTracker>(c =>
            {
                var innerErrorTracker = c.Get<IErrorTracker>();
                var loggerFactory = c.Get<IRebusLoggerFactory>();
                var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
                var rebusBus = c.Get<RebusBus>();

                var circuitBreakerEvents = new CircuitBreakerEvents();
                optionsConfigurer.Register(r => circuitBreakerEvents);

                var circuitBreaker = new MainCircuitBreaker(circuitBreakers, loggerFactory, asyncTaskFactory, rebusBus, circuitBreakerEvents);

                return new CircuitBreakerErrorTracker(innerErrorTracker, circuitBreaker);
            });
        }

        /// <summary>
        /// Configuration builder to fluently register circuit breakers
        /// </summary>
        public class CircuitBreakerConfigurationBuilder
        {
            private readonly IList<ICircuitBreaker> _circuitBreakerStores;

            internal CircuitBreakerConfigurationBuilder()
            {
                _circuitBreakerStores = new List<ICircuitBreaker>();
            }

            /// <summary>
            /// Register a circuit breaker based on an <typeparamref name="TException"/>
            /// </summary>
            /// <typeparam name="TException"></typeparam>
            public CircuitBreakerConfigurationBuilder OpenOn<TException>(
                int attempts = CircuitBreakerSettings.DefaultAttempts
                , int trackingPeriodInSeconds = CircuitBreakerSettings.DefaultTrackingPeriodInSeconds
                , int halfOpenPeriodInSeconds = CircuitBreakerSettings.DefaultHalfOpenResetInterval
                , int resetIntervalInSeconds = CircuitBreakerSettings.DefaultCloseResetInterval)
                where TException : Exception
            {
                var settings = new CircuitBreakerSettings(attempts, trackingPeriodInSeconds, halfOpenPeriodInSeconds, resetIntervalInSeconds);
                _circuitBreakerStores.Add(new ExceptionTypeCircuitBreaker(typeof(TException), settings, new DefaultRebusTime()));
                return this;
            }

            internal IList<ICircuitBreaker> Build()
            {
                return _circuitBreakerStores;
            }
        }
    }
}
