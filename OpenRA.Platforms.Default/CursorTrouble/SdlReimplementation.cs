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
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1507 // Code should not contain multiple blank lines in a row
#pragma warning disable SA1515 // Single-line comment should be preceded by blank line
#pragma warning disable SA1005 // Single line comments should begin with single space
#pragma warning disable SA1003 // Symbols should be spaced correctly
#pragma warning disable SA1024 // Colons Should Be Spaced Correctly

using System;
using System.Runtime.InteropServices;

using static SDL2.SDL;

namespace OpenRA.Platforms.Win32
{
	public static class SdlReimplementation
	{
		[StructLayout(LayoutKind.Sequential)]
		private unsafe struct SDL_Cursor
		{
			public SDL_Cursor* next;
			public void*       driverdata;
		}

		private static void SDL_assert(bool cond)
		{
			if (!cond)
			{
				throw new InvalidOperationException("Assertion failure.");
			}
		}

		private static unsafe void SDL_memset(IntPtr ptr, byte value, int count)
		{
			byte* p = (byte*)ptr.ToPointer();
			for( int i = 0; i < count; i++ )
			{
				*p = 0xFF;
				p++;
			}
		}

		private static unsafe void SDL_memcpy(IntPtr dest, IntPtr source, int count)
		{
			byte* dst = (byte*)dest.ToPointer();
			byte* src = (byte*)source.ToPointer();
//			int len = surface.h * surface.pitch;
			for( int i = 0; i < count; i++ )
			{
				*dst = *src;
				dst++;
				src++;
			}
		}

		// https://github.com/libsdl-org/SDL/blob/c59d4dcd38c382a1e9b69b053756f1139a861574/src/video/windows/SDL_windowsmouse.c
		public static unsafe IntPtr SDL_WIN_CreateCursor(SDL_Surface surface, int hot_x, int hot_y)
		{
			using( SafeIconHandle cursorIconHandle = CreateCursorIconFromSdlSurface( surface, hot_x, hot_y ) )
			{
				IntPtr num          = new IntPtr(1);
				IntPtr sizeOfCursor = new IntPtr( Marshal.SizeOf<SDL_Cursor>() );

				void* cursorPtr = MSVCRT.calloc(num: num, size: sizeOfCursor );
				if( cursorPtr == null )
				{
					throw new InvalidOleVariantTypeException("calloc failed.");
				}
				else
				{
					IntPtr hIcon = cursorIconHandle.GetHIconAndDoNotDestroyIt();

					SDL_Cursor* cursor = (SDL_Cursor*)cursorPtr;
					cursor->driverdata = hIcon.ToPointer();
					cursor->next       = null;
					return new IntPtr( cursor );
				}
			}
		}

		private static SafeIconHandle CreateCursorIconFromSdlSurface(SDL_Surface surface, int hot_x, int hot_y)
		{
			BITMAPINFO bitmapInfo = new BITMAPINFO
			{
				bmiheader = new BITMAPV4HEADER
				{
					bV4Size          = BITMAPV4HEADER.SizeOf,
					bV4Width         =  surface.w,
					bV4Height        = -surface.h, /* Invert the image */
					bV4Planes        = 1,
					bV4BitCount      = 32,
					bV4V4Compression = BitmapCompressionMode.BI_BITFIELDS,
					bV4AlphaMask     = 0xFF000000,
					bV4RedMask       = 0x00FF0000,
					bV4GreenMask     = 0x0000FF00,
					bV4BlueMask      = 0x000000FF
				},
				bmiColors = default // <-- So apparently SDL doesn't set or use this, it casts `bmiheader` to BITMAPINFO directlly... though that should be okay as it sets `bmh.bV4Size = sizeof(bmh)`, not `sizeof(BITMAPINFO)`.
			};

			int pad = (IntPtr.Size * 8);  /* 32 or 64, or whatever. */
			int maskbitslen = ((surface.w + (pad - (surface.w % pad))) / 8) * surface.h;
			IntPtr maskbits = Marshal.AllocHGlobal(maskbitslen);
			SDL_assert(maskbits != IntPtr.Zero);

			/* AND the cursor against full bits: no change. We already have alpha. */
			SDL_memset(ptr: maskbits, value: 0xFF, count: maskbitslen);

			SafeDibHandle dib;
			IntPtr        dibPixelData;
			using( SafeDCHandle hDCEntireScreen = Gdi.GetDCForEntireScreen() )
			{
				( SafeDibHandle dib, IntPtr dibPixelData ) t = Gdi.CreateDibSection( hDCEntireScreen, bitmapInfo, DibUsage.DIB_RGB_COLORS );
				dib = t.dib;
				dibPixelData = t.dibPixelData;
			}

			using( dib )
			using( SafeBitmapHandle maskBitmap = Gdi.CreateBitmap( surface.w, surface.h, planes: 1, bitsPerPixel: 1, pixelData: maskbits ) )
			{
//				SDL_small_free(maskbits, isstack);
				Marshal.FreeHGlobal(maskbits);

//				SDL_assert(surface.format.format == SDL_PIXELFORMAT_ARGB8888);
				SDL_assert(surface.pitch == surface.w * 4);

				SDL_memcpy( dest: dibPixelData, source: surface.pixels, count: surface.h * surface.pitch );

				// `CreateIconIndirect` is the function that fails normally, for reasons still-unknown.
				SafeIconHandle cursorIcon = Gdi.CreateIconIndirect(
					isIcon     : false,
					hotspotX   : hot_x,
					hotspotY   : hot_y,
					mask       : maskBitmap,
					colorPixels: dib
				);

				return cursorIcon;
			}
		}
	}
}
