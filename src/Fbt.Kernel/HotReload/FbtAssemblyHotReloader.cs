using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

namespace Fbt.HotReload
{
    /// <summary>
    /// Watches a directory for new DLLs and orchestrates assembly-based hot reload.
    /// All ALC operations run on a background thread; results are enqueued for
    /// application-thread consumption via <see cref="DrainPendingCallbacks"/>.
    /// Does not reference any FDP/HROT types directly.
    /// </summary>
    public sealed class FbtAssemblyHotReloader : IDisposable
    {
        // ---- Dependency types (provided by caller) ----

        /// <summary>
        /// Called when an assembly is loaded. The caller should invoke RegisterAll
        /// on the provided [FbtRegistrar] type, then return the set of trees to reload.
        /// </summary>
        public delegate IEnumerable<(string treeName, BehaviorTreeBlob blob)>
            AssemblyReloadHandler(Type registrarType, Assembly newAssembly);

        // ---- Events (fired from DrainPendingCallbacks on application thread) ----
        public event Action<string>? OnReloadCompleted;
        public event Action<string, Exception>? OnReloadFailed;

        // ---- Private fields ----
        private readonly AssemblyReloadHandler _handler;
        private readonly FileSystemWatcher _watcher;
        private readonly Timer _debounceTimer;
        private string? _pendingPath;
        private readonly object _debounceLock = new object();
        private const int DebounceMs = 200;

        private AssemblyLoadContext? _currentAlc;
        private readonly ConcurrentQueue<Action> _pendingCallbacks
            = new ConcurrentQueue<Action>();

        // ---- Weak reference for GC verification ----
        /// <summary>
        /// A weak reference to the previously-unloaded ALC. Used in tests to verify
        /// the old ALC was GC'd. Null if no reload has occurred yet, or if only one
        /// reload has occurred (no previous ALC to track).
        /// </summary>
        public WeakReference<AssemblyLoadContext>? PreviousAlcRef { get; private set; }

        // ---- Constructor ----
        public FbtAssemblyHotReloader(string watchDirectory, AssemblyReloadHandler handler)
        {
            _handler = handler;
            _debounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);

            _watcher = new FileSystemWatcher(watchDirectory, "*.dll")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
            };
            _watcher.Created += OnFileChanged;
            _watcher.Changed += OnFileChanged;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            lock (_debounceLock) { _pendingPath = e.FullPath; }
            _debounceTimer.Change(DebounceMs, Timeout.Infinite);
        }

        private void OnDebounceElapsed(object? state)
        {
            string? path;
            lock (_debounceLock) { path = _pendingPath; }
            if (path != null)
                ThreadPool.QueueUserWorkItem(_ => LoadAndReload(path));
        }

        private void LoadAndReload(string dllPath)
        {
            try
            {
                var newAlc = new AssemblyLoadContext(
                    Path.GetFileNameWithoutExtension(dllPath), isCollectible: true);
                Assembly newAssembly;

                // Retry loop: the file may still be locked if the copy is in progress
                const int maxRetries = 5;
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        using var fs = File.OpenRead(dllPath);
                        newAssembly = newAlc.LoadFromStream(fs);
                        break;
                    }
                    catch (IOException) when (attempt++ < maxRetries)
                    {
                        Thread.Sleep(50);
                    }
                }

                // Find [FbtRegistrar]-annotated type
                Type? registrarType = null;
                foreach (var type in newAssembly.GetTypes())
                {
                    if (type.GetCustomAttribute(typeof(FbtRegistrarAttribute)) != null)
                    {
                        registrarType = type;
                        break;
                    }
                }

                if (registrarType == null)
                {
                    // No registrar found — fail gracefully
                    var ex = new InvalidOperationException(
                        $"No [FbtRegistrar] class found in '{dllPath}'.");
                    _pendingCallbacks.Enqueue(() => OnReloadFailed?.Invoke(dllPath, ex));
                    newAlc.Unload();
                    return;
                }

                // Call user handler to do registration + get blobs
                IEnumerable<(string, BehaviorTreeBlob)> results;
                try
                {
                    results = _handler(registrarType, newAssembly);
                }
                catch (Exception ex)
                {
                    _pendingCallbacks.Enqueue(() => OnReloadFailed?.Invoke(dllPath, ex));
                    newAlc.Unload();
                    return;
                }

                // Unload OLD ALC and track it for GC verification
                var oldAlc = Interlocked.Exchange(ref _currentAlc, newAlc);
                if (oldAlc != null)
                {
                    PreviousAlcRef = new WeakReference<AssemblyLoadContext>(oldAlc);
                    oldAlc.Unload();
                }

                // Queue success callbacks
                foreach (var (treeName, _) in results)
                {
                    var name = treeName;
                    _pendingCallbacks.Enqueue(() => OnReloadCompleted?.Invoke(name));
                }
            }
            catch (Exception ex)
            {
                _pendingCallbacks.Enqueue(() => OnReloadFailed?.Invoke(dllPath, ex));
            }
        }

        // ---- Application thread drain ----
        /// <summary>
        /// Must be called once per game update from the application thread.
        /// Fires OnReloadCompleted / OnReloadFailed for any queued reload results.
        /// </summary>
        public void DrainPendingCallbacks()
        {
            while (_pendingCallbacks.TryDequeue(out var cb))
                cb();
        }

        // ---- IDisposable ----
        public void Dispose()
        {
            _watcher.Dispose();
            _debounceTimer.Dispose();
            var alc = Interlocked.Exchange(ref _currentAlc, null);
            alc?.Unload();
        }
    }
}
