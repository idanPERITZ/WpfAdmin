using System;
using System.ServiceModel;
using System.Threading;

namespace WpfAdminPeritz
{
    /// <summary>
    /// Generic WCF client helper that manages channel state and automatic recovery
    /// Prevents crashes from faulted WCF channels
    /// </summary>
    /// <typeparam name="TService">The WCF service interface (e.g., IChessServiceUser)</typeparam>
    public class WcfClientHelper<TService> where TService : class
    {
        private TService _client;
        private readonly Func<TService> _clientFactory;
        private readonly object _lock = new object();
        private int _consecutiveFailures = 0;
        private const int MAX_CONSECUTIVE_FAILURES = 3;

        public WcfClientHelper(Func<TService> clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _client = _clientFactory();
        }

        /// <summary>
        /// Execute a WCF service call with automatic channel state checking and recovery
        /// </summary>
        public TResult Execute<TResult>(Func<TService, TResult> operation, TResult defaultValue = default(TResult))
        {
            lock (_lock)
            {
                if (_consecutiveFailures >= MAX_CONSECUTIVE_FAILURES)
                {
                    throw new InvalidOperationException(
                        $"Service has failed {MAX_CONSECUTIVE_FAILURES} consecutive times. " +
                        "The connection is unstable and cannot be recovered.");
                }

                try
                {
                    // Check channel state before attempting operation
                    EnsureChannelIsHealthy();

                    // Execute the operation
                    var result = operation(_client);

                    // Success - reset failure counter
                    _consecutiveFailures = 0;
                    return result;
                }
                catch (CommunicationException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CommunicationException: {ex.Message}");
                    _consecutiveFailures++;
                    RecreateClient();

                    // Retry once after recreation
                    try
                    {
                        EnsureChannelIsHealthy();
                        var result = operation(_client);
                        _consecutiveFailures = 0;
                        return result;
                    }
                    catch
                    {
                        _consecutiveFailures++;
                        return defaultValue;
                    }
                }
                catch (TimeoutException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"TimeoutException: {ex.Message}");
                    _consecutiveFailures++;
                    return defaultValue;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected exception: {ex.Message}");
                    _consecutiveFailures++;
                    RecreateClient();
                    return defaultValue;
                }
            }
        }

        /// <summary>
        /// Execute a void WCF service call
        /// </summary>
        public bool ExecuteVoid(Action<TService> operation)
        {
            return Execute(client =>
            {
                operation(client);
                return true;
            }, false);
        }

        private void EnsureChannelIsHealthy()
        {
            var commObj = _client as ICommunicationObject;
            if (commObj == null)
                return;

            var state = commObj.State;

            if (state == CommunicationState.Faulted || state == CommunicationState.Closed)
            {
                System.Diagnostics.Debug.WriteLine($"Channel is {state}, recreating...");
                RecreateClient();
            }
            else if (state == CommunicationState.Created || state == CommunicationState.Opening)
            {
                // Give it a moment to open
                Thread.Sleep(100);
            }
        }

        private void RecreateClient()
        {
            try
            {
                // Abort the old client
                var commObj = _client as ICommunicationObject;
                if (commObj != null)
                {
                    try
                    {
                        if (commObj.State == CommunicationState.Faulted)
                        {
                            commObj.Abort();
                        }
                        else
                        {
                            commObj.Close();
                        }
                    }
                    catch
                    {
                        // Ignore errors during cleanup
                        try { commObj.Abort(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing old client: {ex.Message}");
            }

            // Create new client
            try
            {
                _client = _clientFactory();
                System.Diagnostics.Debug.WriteLine("Client recreated successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating new client: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get the current client (for direct access if needed)
        /// </summary>
        public TService Client
        {
            get
            {
                lock (_lock)
                {
                    EnsureChannelIsHealthy();
                    return _client;
                }
            }
        }

        /// <summary>
        /// Reset failure counter (call after successful non-critical operations)
        /// </summary>
        public void ResetFailureCount()
        {
            lock (_lock)
            {
                _consecutiveFailures = 0;
            }
        }
    }
}
