using System;
using UnityEngine;

namespace DRG.Core.Logs
{
	public class LoggerUnity : ILogger
	{
		private readonly ILogger.LogLevel _level;

		public LoggerUnity(ILogger.LogLevel level)
		{
			_level = level;
		}

		public void Log(string message)
		{
			if (_level > ILogger.LogLevel.Debug)
			{
				return;
			}
			Debug.Log(message);
		}

		public void LogWarning(string message)
		{
			if (_level > ILogger.LogLevel.Warning)
			{
				return;
			}
			Debug.LogWarning(message);
		}

		public void LogError(string message)
		{
			if (_level > ILogger.LogLevel.Error)
			{
				return;
			}
			Debug.LogError(message);
		}

		public void LogException(Exception exception)
		{
			if (_level > ILogger.LogLevel.Fatal)
			{
				return;
			}
			Debug.LogException(exception);
		}
	}
}
