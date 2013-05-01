using System;
using System.Windows;
using System.Windows.Interop;

namespace Mygod.Windows.Dialogs
{
	/// <summary>
	/// Provides safe Win32 API wrapper calls for various actions not directly
	/// supported by WPF classes out of the box.
	/// </summary>
	internal class SafeNativeMethods
	{
		/// <summary>
		/// Sets the window's close button visibility.
		/// </summary>
		/// <param name="window">The window to set.</param>
		/// <param name="showCloseButton"><c>true</c> to show the close button; otherwise, <c>false</c></param>
		public static void SetWindowCloseButtonVisibility(Window window, bool showCloseButton)
		{
			var wih = new WindowInteropHelper(window);
			
			int style = NativeMethods.GetWindowLong(wih.Handle, NativeMethods.GwlStyle);

			if (showCloseButton)
				NativeMethods.SetWindowLong(wih.Handle, NativeMethods.GwlStyle, style & NativeMethods.WsSysmenu);
			else
				NativeMethods.SetWindowLong(wih.Handle, NativeMethods.GwlStyle, style & ~NativeMethods.WsSysmenu);
		}
		/// <summary>
		/// Sets the window's icon visibility.
		/// </summary>
		/// <param name="window">The window to set.</param>
		/// <param name="showIcon"><c>true</c> to show the icon in the caption; otherwise, <c>false</c></param>
		public static void SetWindowIconVisibility(Window window, bool showIcon)
		{
			System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(window);

			// For Vista/7 and higher
			if (Environment.OSVersion.Version.Major >= 6)
			{
				// Change the extended window style
				if (showIcon)
				{
					int extendedStyle = NativeMethods.GetWindowLong(wih.Handle, NativeMethods.GwlExstyle);
					NativeMethods.SetWindowLong(wih.Handle, NativeMethods.GwlExstyle, extendedStyle | ~NativeMethods.WsExDlgmodalframe);
				}
				else
				{
					int extendedStyle = NativeMethods.GetWindowLong(wih.Handle, NativeMethods.GwlExstyle);
					NativeMethods.SetWindowLong(wih.Handle, NativeMethods.GwlExstyle, extendedStyle | NativeMethods.WsExDlgmodalframe);
				}

				// Update the window's non-client area to reflect the changes
				NativeMethods.SetWindowPos(wih.Handle, IntPtr.Zero, 0, 0, 0, 0,
					NativeMethods.SwpNomove | NativeMethods.SwpNosize | NativeMethods.SwpNozorder | NativeMethods.SwpFramechanged);
			}
			// For XP and older
			// TODO Setting Window Icon visibility doesn't work in XP
			else
			{
				// 0 - ICON_SMALL (caption bar)
				// 1 - ICON_BIG   (alt-tab)

				if (showIcon)
					NativeMethods.SendMessage(wih.Handle, NativeMethods.WmSeticon, new IntPtr(0),
						NativeMethods.DefWindowProc(wih.Handle, NativeMethods.WmSeticon, new IntPtr(0), IntPtr.Zero));
				else
					NativeMethods.SendMessage(wih.Handle, NativeMethods.WmSeticon, new IntPtr(0), IntPtr.Zero);
			}
		}
	}
}
