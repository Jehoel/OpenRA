#pragma warning disable SX1101 // Do not prefix local calls with 'this.'
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1100 // Do not prefix calls with base unless local implementation exists
#pragma warning disable SA1512 // Single-line comments should not be followed by blank line
#pragma warning disable SA1000 // Keywords should be spaced correctly
#pragma warning disable IDE0007 // Use implicit type
#pragma warning disable SA1606 // Element documentation should have summary text
#pragma warning disable SA1121 // Use built-in type alias
#pragma warning disable IDE0049 // Simplify Names
#pragma warning disable SA1120 // Comments should contain text
#pragma warning disable SA1025 // Code should not contain multiple whitespace in a row
#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OpenRA.Platforms.Default
{
	public enum DibUsage : UInt32
	{
		DIB_RGB_COLORS = 0,
		DIB_PAL_COLORS = 1,
	}

	public static class Gdi
	{
		internal static class GdiNativeMethods
		{
			[DllImport("gdi32.dll", SetLastError = true, ThrowOnUnmappableChar = true)]
			public static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, uint usage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

			[DllImport("gdi32.dll", SetLastError = true, ThrowOnUnmappableChar = true)]
			public static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

			[DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true)]
			public static extern IntPtr GetDC(IntPtr hWnd);

			/// <summary>Yes, the return-type really is a C `int`: https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-releasedc</summary>
			[DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true)]
			public static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

			/// <summary></summary>
			/// <param name="hObject">A handle to a logical pen, brush, font, bitmap, region, or palette.</param>
			[DllImport("gdi32.dll", SetLastError = true, ThrowOnUnmappableChar = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool DeleteObject([In] IntPtr hObject);

			[DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true)]
			public static extern IntPtr CreateIconIndirect([In] ref ICONINFO piconinfo);

			[DllImport("user32.dll", SetLastError = true, ThrowOnUnmappableChar = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool DestroyIcon( IntPtr hIcon );
		}

		public static SafeDCHandle GetDCForEntireScreen()
		{
			return GetDC( hWnd: IntPtr.Zero );
		}

		public static SafeDCHandle GetDC( IntPtr hWnd )
		{
			IntPtr hDC = GdiNativeMethods.GetDC(hWnd);
			if ( hDC == IntPtr.Zero )
			{
				throw new Win32Exception();
			}
			else
			{
				return new SafeDCHandle( hWnd, hDC );
			}
		}

		//

		public static ( SafeDibHandle dib, IntPtr dibPixelData ) CreateDibSection(
			SafeDCHandle hDC,
			BITMAPINFO   bitmapInfo,
			DibUsage     usage
		)
		{
			IntPtr hBitmap = GdiNativeMethods.CreateDIBSection( hDC.DangerousGetHandle(), ref bitmapInfo, (UInt32)usage, out IntPtr dibPixelData, hSection: IntPtr.Zero, dwOffset: 0 );
			SafeDibHandle dibHandle = new SafeDibHandle( hBitmap );
			return ( dibHandle, dibPixelData );
		}

		public static SafeBitmapHandle CreateBitmap( int width, int height, UInt32 planes, UInt32 bitsPerPixel, IntPtr pixelData )
		{
			IntPtr hBitmap = GdiNativeMethods.CreateBitmap( width, height, planes, bitsPerPixel, pixelData );
			return new SafeBitmapHandle( hBitmap );
		}

		/// <param name="isIcon">false for cursors, true for icons.</param>
		public static SafeIconHandle CreateIconIndirect( bool isIcon, int hotspotX, int hotspotY, SafeBitmapHandle mask, SafeDibHandle colorPixels )
		{
			if( hotspotX < 0 ) throw new ArgumentOutOfRangeException(nameof(hotspotX));
			if( hotspotY < 0 ) throw new ArgumentOutOfRangeException(nameof(hotspotY));

			ICONINFO ii = new ICONINFO
			{
				fIcon    = isIcon,
				xHotspot = (uint)hotspotX,
				yHotspot = (uint)hotspotY,
				hbmMask  = mask.DangerousGetHandle(),
				hbmColor = colorPixels.DangerousGetHandle()
			};

			return CreateIconIndirect( ii );
		}

		public static SafeIconHandle CreateIconIndirect( ICONINFO iconInfo )
		{
			IntPtr hIcon = GdiNativeMethods.CreateIconIndirect( ref iconInfo );
			return new SafeIconHandle( hIcon );
		}
	}
}
