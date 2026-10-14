using System;
using System.Diagnostics;

namespace WpfApp14.Utils
{
	public static class Logger
	{
		public static bool Enabled = true;
		public static void Log(string message)
		{
			if (!Enabled) return;
			Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] LOG: {message}");
		}

		public static void Error(string message)
		{
			if (!Enabled) return;
            Debug.WriteLine($"--------------------------------------------------\n" +
                $"=> [{DateTime.Now:HH:mm:ss}] ERR: {message}\n" +
				$"--------------------------------------------------\n");
		}
	}
}