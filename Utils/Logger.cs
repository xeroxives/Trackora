using System;

namespace WpfApp14.Utils
{
	public static class Logger
	{
		// Глобальный переключатель логов
		public static bool Enabled = true;

		public static void Log(string message)
		{
			if (!Enabled) return;
			Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LOG: {message}");
		}

		public static void Error(string message)
		{
			if (!Enabled) return;
			Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERR: {message}");
		}
	}
}