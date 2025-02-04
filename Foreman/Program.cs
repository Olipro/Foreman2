﻿using Foreman.DataCache;

using System;
using System.Windows.Forms;

namespace Foreman {
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			ErrorLogging.ClearLog();
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.Run(new MainForm());
		}
	}
}
