using System.Collections.Concurrent;

namespace IrisAI.Agent.Services;

/*
 * In-memory ring buffer of notable events (mostly errors and warnings)
 * so the operator can inspect what went wrong from the UI without opening
 * the server console. Not persisted - cleared on restart.
 */
public sealed class DiagnosticsLog
{
	private const int MaxEntries = 300;
	private const int MaxDetailLength = 4_000;

	private readonly ConcurrentQueue<DiagnosticEntry> _entries = new();
	private long _sequence;

	public void Error(string source, string message, string? detail = null)
		=> Add("error", source, message, detail);

	public void Warn(string source, string message, string? detail = null)
		=> Add("warn", source, message, detail);

	public void Info(string source, string message, string? detail = null)
		=> Add("info", source, message, detail);

	private void Add(
		string level,
		string source,
		string message,
		string? detail)
	{
		var entry = new DiagnosticEntry(
			Interlocked.Increment(ref _sequence),
			DateTimeOffset.UtcNow,
			level,
			source,
			message ?? string.Empty,
			Trim(detail));

		_entries.Enqueue(entry);

		while (_entries.Count > MaxEntries &&
			   _entries.TryDequeue(out _))
		{
		}

		Console.WriteLine(
			$"[{level.ToUpperInvariant()}] {source}: {message}");
	}

	public IReadOnlyList<DiagnosticEntry> Recent(int take = 200)
		=> _entries
			.Reverse()
			.Take(Math.Clamp(take, 1, MaxEntries))
			.ToList();

	public int Count => _entries.Count;

	public void Clear()
	{
		while (_entries.TryDequeue(out _))
		{
		}
	}

	private static string? Trim(string? detail)
	{
		if (string.IsNullOrWhiteSpace(detail))
		{
			return null;
		}

		return detail.Length <= MaxDetailLength
			? detail
			: detail[..MaxDetailLength] + "\n… (truncated)";
	}
}

public sealed record DiagnosticEntry(
	long Id,
	DateTimeOffset TimestampUtc,
	string Level,
	string Source,
	string Message,
	string? Detail);
