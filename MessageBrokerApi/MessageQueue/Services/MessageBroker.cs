﻿using MessageBrokerApi.Common.Configuration;
using MessageBrokerApi.Common.Hashing;
using MessageBrokerApi.MessageQueue.Interfaces;
using System.Collections.Concurrent;

namespace MessageBrokerApi.MessageQueue.Services
{
    public class MessageBroker : IMessageBroker
    {
        private readonly IMessageStorage _storage;
        private readonly IHashGenerator _hashGen;
        private readonly ILogger<MessageBroker> _logger;
        private readonly bool _useAdvanced;

        private readonly ConcurrentDictionary<string, RequestState> _requests = new();

        public MessageBroker(IBrokerConfig config, IHashGenerator hashGen, ILogger<MessageBroker> logger, IMessageStorage storage)
        {
            _storage = storage;
            _hashGen = hashGen;
            _logger = logger;
            _useAdvanced = config.BrokerAdvancedMode;
        }

        public async Task<(int StatusCode, string Body)> SendAndWaitAsync(string method, string path, string body)
        {
            var keyInput = _useAdvanced
                ? method + path + body
                : method + path + DateTime.Now.ToString("O");

            var key = _hashGen.ComputeHash(keyInput);
            _logger.LogInformation("Generated key: {Key} for request [{Method}] {Path}", key, method, path);

            var state = _requests.GetOrAdd(key, _ => new RequestState());
            var waitersCount = state.IncrementWaiters();
            _logger.LogDebug("Waiters for key {Key}: {Count}", key, waitersCount);

            await state.Semaphore.WaitAsync();
            _logger.LogDebug("Semaphore entered for key: {Key}", key);

            try
            {
                await _storage.WriteRequestAsync(key, method, path, body);
                var result = await _storage.WaitForResponseAsync(key);
                return result;
            }
            finally
            {
                var remaining = state.DecrementWaiters();
                _logger.LogDebug("Remaining waiters for key {Key}: {Remaining}", key, remaining);

                if (remaining == 0)
                {
                    _storage.CleanUp(key);
                    _requests.TryRemove(key, out _);
                }

                state.Semaphore.Release();
                _logger.LogDebug("Semaphore released for key: {Key}", key);
            }
        }

        private class RequestState
        {
            public SemaphoreSlim Semaphore { get; }
            private int _waiters;

            public RequestState()
            {
                Semaphore = new SemaphoreSlim(1, 1);
                _waiters = 0;
            }

            public int IncrementWaiters() => Interlocked.Increment(ref _waiters);
            public int DecrementWaiters() => Interlocked.Decrement(ref _waiters);
            public int Waiters => Volatile.Read(ref _waiters);
        }
    }
}
