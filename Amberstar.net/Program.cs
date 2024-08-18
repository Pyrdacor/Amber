using System.Reflection;
using System.Text.RegularExpressions;

namespace Amberstar
{
	class Program
	{
		[STAThread]
		static void Main()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			var gameWindow = new GameWindow();
			int exitCode = 0;

			try
			{
				gameWindow.Run();
			}
			catch (Exception ex)
			{
				PrintException(ex);
				exitCode = 1;
			}
			finally
			{
				Environment.Exit(exitCode);
			}
		}

		static void OutputError(string error)
		{
			Console.WriteLine(error);
			Console.WriteLine();
			Console.WriteLine("Press return to exit");
			Console.ReadLine();
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception ex)
				PrintException(ex);
			else
				OutputError(e.ExceptionObject?.ToString() ?? "Unhandled exception without exception object");

			Environment.Exit(1);
		}

		static void PrintException(Exception ex)
		{
			string message = ex.Message;
			string? stackTrace = ex.StackTrace;
			var e = ex.InnerException;

			while (e != null)
			{
				message += Environment.NewLine + e.Message;
				e = e.InnerException;
			}

			OutputError(message + Environment.NewLine + stackTrace ?? "");
		}
	}
}
