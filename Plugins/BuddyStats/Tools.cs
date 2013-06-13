using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

namespace BuddyStatsD3Plugin
{
	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int x1;
		public int y1;
		public int x2;
		public int y2;
	}

	public static class ScreenShotTaker
	{
		public static Bitmap TakeScreenShot(IntPtr handle)
		{
			RECT rect;
			GetWindowRect(handle, out rect);
			Bitmap bmp = new Bitmap(rect.x2 - rect.x1, rect.y2 - rect.y1, PixelFormat.Format32bppArgb);
			Graphics img = Graphics.FromImage(bmp);
			IntPtr deviceHandle = img.GetHdc();
			PrintWindow(handle, deviceHandle, 0);
			img.ReleaseHdc(deviceHandle);
			img.Dispose();
			return bmp;
		}

		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr handle, out RECT rect);
		[DllImport("user32.dll")]
		public static extern bool PrintWindow(IntPtr handle, IntPtr device, int flags);
	}
}
