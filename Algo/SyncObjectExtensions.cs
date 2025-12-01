namespace StockSharp.Algo;

using System.Runtime.CompilerServices;

using Ecng.Common;

/// <summary>
/// Extension methods for SyncObject to provide PulseSignal/WaitSignal functionality.
/// These methods were removed from Ecng.Common and are provided here for compatibility.
/// </summary>
internal static class SyncObjectExtensions
{
	private static readonly ConditionalWeakTable<SyncObject, SignalState> _states = new();

	private class SignalState
	{
		public object Signal { get; set; }
		public bool IsSignaled { get; set; }
	}

	private static SignalState GetState(SyncObject sync)
		=> _states.GetOrCreateValue(sync);

	/// <summary>
	/// Pulse signal without data.
	/// </summary>
	public static void PulseSignal(this SyncObject sync)
	{
		lock (sync)
		{
			var state = GetState(sync);
			state.IsSignaled = true;
			state.Signal = null;
			Monitor.PulseAll(sync);
		}
	}

	/// <summary>
	/// Pulse signal with data (typically an error).
	/// </summary>
	public static void PulseSignal(this SyncObject sync, object signal)
	{
		lock (sync)
		{
			var state = GetState(sync);
			state.IsSignaled = true;
			state.Signal = signal;
			Monitor.PulseAll(sync);
		}
	}

	/// <summary>
	/// Wait for signal indefinitely.
	/// </summary>
	public static void WaitSignal(this SyncObject sync)
	{
		lock (sync)
		{
			var state = GetState(sync);
			while (!state.IsSignaled)
			{
				Monitor.Wait(sync);
			}
			state.IsSignaled = false;
		}
	}

	/// <summary>
	/// Wait for signal with timeout.
	/// </summary>
	/// <param name="sync">The sync object.</param>
	/// <param name="timeout">The timeout.</param>
	/// <param name="signal">The signal data (typically error).</param>
	/// <returns>True if signaled within timeout, false if timed out.</returns>
	public static bool WaitSignal(this SyncObject sync, TimeSpan timeout, out object signal)
	{
		lock (sync)
		{
			var state = GetState(sync);
			var deadline = DateTime.UtcNow + timeout;

			while (!state.IsSignaled)
			{
				var remaining = deadline - DateTime.UtcNow;
				if (remaining <= TimeSpan.Zero)
				{
					signal = null;
					return false;
				}

				Monitor.Wait(sync, remaining);
			}

			signal = state.Signal;
			state.IsSignaled = false;
			state.Signal = null;
			return true;
		}
	}
}
