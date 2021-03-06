﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WlanProfileViewer.Models
{
	internal class ProcessService
	{
		#region Win32

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool EnumWindows(
			EnumWindowsProc lpEnumFunc,
			IntPtr lParam);

		[return: MarshalAs(UnmanagedType.Bool)]
		private delegate bool EnumWindowsProc(
			IntPtr hWnd,
			IntPtr lParam);

		[DllImport("User32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(
			IntPtr hWnd,
			out uint processId); // IntPtr

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsIconic(IntPtr hWnd);

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ShowWindow(
			IntPtr hWnd,
			int nCmdShow);

		private const int SW_HIDE = 0;
		private const int SW_SHOWNORMAL = 1;
		private const int SW_NORMAL = 1;
		private const int SW_SHOWMINIMIZED = 2;
		private const int SW_SHOWMAXIMIZED = 3;
		private const int SW_MAXIMIZE = 3;
		private const int SW_SHOWNOACTIVATE = 4;
		private const int SW_SHOW = 5;
		private const int SW_MINIMIZE = 6;
		private const int SW_SHOWMINNOACTIVE = 7;
		private const int SW_SHOWNA = 8;
		private const int SW_RESTORE = 9;
		private const int SW_SHOWDEFAULT = 10;
		private const int SW_FORCEMINIMIZE = 11;
		private const int SW_MAX = 11;

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowText(
			IntPtr hWnd,
			StringBuilder lpString,
			int nMaxCount);

		#endregion

		/// <summary>
		/// Checks if a process of this application already exists and if so, activate its window.
		/// </summary>
		/// <returns>True if exists</returns>
		/// <remarks>This method should work even if the window is minimized and ShowInTaskbar is false.</remarks>
		public static bool CheckActivateExistingProcess()
		{
			var currentProcess = Process.GetCurrentProcess();
			Process existingProcess = null;

			try
			{
				existingProcess = Process.GetProcessesByName(currentProcess.ProcessName)
					.Where(x => x.MainModule.FileName == Assembly.GetExecutingAssembly().Location)
					.FirstOrDefault(x => x.Id != currentProcess.Id);

				if (existingProcess == null)
					return false;

				var windowHandle = existingProcess.MainWindowHandle;
				if (windowHandle == IntPtr.Zero) // If the window is minimized and ShowInTaskbar is false
				{
					Func<IntPtr, IntPtr, bool> search = (hWnd, lParam) =>
					{
						uint processId;
						GetWindowThreadProcessId(hWnd, out processId);

						if (processId != existingProcess.Id)
							return true; // Continue enumeration.

						if (!IsIconic(hWnd)) // This filters out handles other than the main window handle.
							return true; // Continue enumeration.

						ShowWindowTitle(hWnd); // For debug

						windowHandle = hWnd;
						return false; // Stop enumeration.
					};

					EnumWindows(new EnumWindowsProc(search), IntPtr.Zero);

					if (windowHandle == IntPtr.Zero)
						return false;
				}

				ShowWindow(windowHandle, SW_RESTORE);
				return SetForegroundWindow(windowHandle);
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Failed to check and activate existing process.\r\n{ex}");
				throw;
			}
			finally
			{
				currentProcess.Dispose();
				existingProcess?.Dispose();
			}
		}

		[Conditional("DEBUG")]
		private static void ShowWindowTitle(IntPtr hWnd)
		{
			int textLength = GetWindowTextLength(hWnd);
			if (0 < textLength)
			{
				var sb = new StringBuilder(textLength + 1);
				GetWindowText(hWnd, sb, sb.Capacity);

				Debug.WriteLine($"Title: {sb}");
			}
		}
	}
}