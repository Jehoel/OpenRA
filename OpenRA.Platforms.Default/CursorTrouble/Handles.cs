#pragma warning disable SX1101 // Do not prefix local calls with 'this.'
#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1100 // Do not prefix calls with base unless local implementation exists
#pragma warning disable SA1512 // Single-line comments should not be followed by blank line
#pragma warning disable SA1000 // Keywords should be spaced correctly

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using OpenRA.Primitives;
using SDL2;

namespace OpenRA.Platforms.Default
{
	public class SafeDCHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public SafeDCHandle( IntPtr hWnd, IntPtr hDC )
			: base( ownsHandle: true )
		{
			this.WindowHandle = hWnd;

			base.SetHandle( hDC );
		}

		public IntPtr WindowHandle { get; } // hWnd

		protected override bool ReleaseHandle()
		{
			if (!this.IsInvalid)
			{
				// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-releasedc
				// > The return value indicates whether the DC was released. If the DC was released, the return value is 1.
				// > If the DC was not released, the return value is zero.

				int ok = Gdi.GdiNativeMethods.ReleaseDC( hWnd: this.WindowHandle, hdc: this.handle );
				if (ok == 1)
				{
					this.handle = IntPtr.Zero;
					return true;
				}
				else
				{
					throw new Win32Exception();
				}
			}
			else
			{
				return true;
			}
		}
	}

	public class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public SafeIconHandle( IntPtr hIcon )
			: base( ownsHandle: true )
		{
			base.SetHandle( hIcon );
		}

		protected override bool ReleaseHandle()
		{
			if (!this.IsInvalid)
			{
				// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-releasedc
				// > The return value indicates whether the DC was released. If the DC was released, the return value is 1.
				// > If the DC was not released, the return value is zero.

				bool ok = Gdi.GdiNativeMethods.DestroyIcon( this.handle );
				if (ok)
				{
					this.handle = IntPtr.Zero;
					return true;
				}
				else
				{
					throw new Win32Exception();
				}
			}
			else
			{
				return true;
			}
		}
	}

	public abstract class SafeGdiHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		protected SafeGdiHandle( IntPtr gdiObjectHandle )
			: base( ownsHandle: true )
		{
			base.SetHandle( gdiObjectHandle );
		}

		protected sealed override bool ReleaseHandle()
		{
			if (!this.IsInvalid)
			{
				bool ok = Gdi.GdiNativeMethods.DeleteObject( this.handle );
				if (ok)
				{
					this.handle = IntPtr.Zero;
					return true;
				}
				else
				{
					throw new Win32Exception();
				}
			}
			else
			{
				return true;
			}
		}
	}

	public class SafeDibHandle : SafeGdiHandle
	{
		public SafeDibHandle( IntPtr dibHandle )
			: base( gdiObjectHandle: dibHandle )
		{
			base.SetHandle( dibHandle );
		}
	}

	public class SafeBitmapHandle : SafeGdiHandle
	{
		public SafeBitmapHandle( IntPtr hbitmap )
			: base( gdiObjectHandle: hbitmap )
		{
			base.SetHandle( hbitmap );
		}
	}
}
