using System;
using System.Collections.Generic;
using System.Threading;

public static class LogMethodContext
{
	private static readonly AsyncLocal<Dictionary<string, string>> _context = new AsyncLocal<Dictionary<string, string>>();

	public static void Add(IDictionary<string, string> values)
	{
		if (_context.Value == null)
		{
			_context.Value = new Dictionary<string, string>();
		}

		foreach (KeyValuePair<string, string> value in values)
		{
			if (_context.Value.ContainsKey(value.Key))
			{
				throw new NotSupportedException($"The key {value.Key} already exists in the context.");
			}
			else
			{
				_context.Value.Add(value.Key, value.Value);
			}
		}
	}

	public static IDictionary<string, string> Get()
	{
		return _context.Value;
	}

	public static void Clear()
	{
		_context.Value.Clear(); ;
	}

	internal static void Add(string propertyName, string value)
	{
		if (_context.Value == null)
		{
			_context.Value = new Dictionary<string, string>();
		}

		if (_context.Value.ContainsKey(propertyName))
		{
			throw new NotSupportedException($"The key {propertyName} already exists in the context.");
		}
		else
		{
			_context.Value.Add(propertyName, value);
		}
	}
}