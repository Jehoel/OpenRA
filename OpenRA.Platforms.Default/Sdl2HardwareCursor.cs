#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

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
#pragma warning disable

	public enum BitmapCompressionMode : int
	{
		BI_RGB = 0x0000,
		BI_RLE8 = 0x0001,
		BI_RLE4 = 0x0002,
		BI_BITFIELDS = 0x0003,
		BI_JPEG = 0x0004,
		BI_PNG = 0x0005,
		BI_CMYK = 0x000B,
		BI_CMYKRLE8 = 0x000C,
		BI_CMYKRLE4 = 0x000D
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct BITMAPINFO
	{
//		public BITMAPINFOHEADER bmiheader;
		public BITMAPV4HEADER bmiheader;
		public RGBQUAD bmiColors;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct RGBQUAD
	{
		public Byte b;
		public Byte g;
		public Byte r;
		public Byte reserved;
	}

	

	[StructLayout(LayoutKind.Sequential)]
	public struct BITMAPINFOHEADER
	{
		public uint  biSize;
		public int   biWidth;
		public int   biHeight;
		public ushort   biPlanes;
		public ushort   biBitCount;
		public BitmapCompressionMode  biCompression;
		public uint  biSizeImage;
		public int   biXPelsPerMeter;
		public int   biYPelsPerMeter;
		public uint  biClrUsed;
		public uint  biClrImportant;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ICONINFO
	{
		/// <summary>
		/// Specifies whether this structure defines an icon or a cursor.
		/// A value of TRUE specifies an icon; FALSE specifies a cursor
		/// </summary>
		[MarshalAs(UnmanagedType.Bool)]
		public bool fIcon;
		/// <summary>
		/// The x-coordinate of a cursor's hot spot
		/// </summary>
		public UInt32 xHotspot;
		/// <summary>
		/// The y-coordinate of a cursor's hot spot
		/// </summary>
		public UInt32 yHotspot;
		/// <summary>
		/// The icon bitmask bitmap
		/// </summary>
		public IntPtr hbmMask;
		/// <summary>
		/// A handle to the icon color bitmap.
		/// </summary>
		public IntPtr hbmColor;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct BITMAPV4HEADER
	{
		public uint        bV4Size;
		public int         bV4Width;
		public int         bV4Height;
		public ushort         bV4Planes;
		public ushort         bV4BitCount;
		public uint        bV4V4Compression;
		public uint        bV4SizeImage;
		public int         bV4XPelsPerMeter;
		public int         bV4YPelsPerMeter;
		public uint        bV4ClrUsed;
		public uint        bV4ClrImportant;
		public uint        bV4RedMask;
		public uint        bV4GreenMask;
		public uint        bV4BlueMask;
		public uint        bV4AlphaMask;
		public uint        bV4CSType;
//		CIEXYZTRIPLE bV4Endpoints; // https://stackoverflow.com/questions/20864752/how-is-defined-the-data-type-fxpt2dot30-in-the-bmp-file-structure
		public int  RedX;          /* X coordinate of red endpoint */
		public int  RedY;          /* Y coordinate of red endpoint */
		public int  RedZ;          /* Z coordinate of red endpoint */
		public int  GreenX;        /* X coordinate of green endpoint */
		public int  GreenY;        /* Y coordinate of green endpoint */
		public int  GreenZ;        /* Z coordinate of green endpoint */
		public int  BlueX;         /* X coordinate of blue endpoint */
		public int  BlueY;         /* Y coordinate of blue endpoint */
		public int  BlueZ;         /* Z coordinate of blue endpoint */

		public uint        bV4GammaRed;
		public uint        bV4GammaGreen;
		public uint        bV4GammaBlue;
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_Cursor
	{
		public SDL_Cursor* next;
		public void*       driverdata;
	}

	/// <summary>SDL2.dll uses MSVCRT.dll directly for calloc/malloc/free.</summary>
	internal static unsafe class MSVCRT
	{
		[DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl)]
		public static extern void* malloc(IntPtr size);

		[DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl)]
		public static extern void free(void* ptr);

		[DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl)]
		public static extern void* calloc(IntPtr num, IntPtr size);

		[DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl)]
		public static extern void* realloc(void* ptr, IntPtr size);
	}

	internal static class WinNativeMethods
	{
		static void SDL_assert(bool cond)
		{
			if (!cond)
			{
				throw new InvalidOperationException("Assertion failure.");
			}
		}

		unsafe static void SDL_memset(IntPtr ptr, byte value, int count)
		{
			byte* p = (byte*)ptr.ToPointer();
			for( int i = 0; i < count; i++ )
			{
				*p = 0xFF;
				p++;
			}
		}

		unsafe static void SDL_memcpy(IntPtr dest, IntPtr source, int count)
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
		public static unsafe IntPtr SDL_WIN_CreateCursor(SDL.SDL_Surface surface, int hot_x, int hot_y)
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

		private static SafeIconHandle CreateCursorIconFromSdlSurface(SDL.SDL_Surface surface, int hot_x, int hot_y)
		{
			int pad = (IntPtr.Size * 8);  /* 32 or 64, or whatever. */
			int maskbitslen;

//			SDL_zero(bmh);

			BITMAPV4HEADER bmh = new BITMAPV4HEADER
			{
				bV4Size = (uint)Marshal.SizeOf<BITMAPV4HEADER>(), // sizeof(BITMAPV4HEADER),
				bV4Width = surface.w,
				bV4Height = -surface.h, /* Invert the image */
				bV4Planes = 1,
				bV4BitCount = 32,
				bV4V4Compression = (uint)BitmapCompressionMode.BI_BITFIELDS,

				bV4AlphaMask     = 0xFF000000,
				bV4RedMask       = 0x00FF0000,
				bV4GreenMask     = 0x0000FF00,
				bV4BlueMask      = 0x000000FF
			};

			maskbitslen = ((surface.w + (pad - (surface.w % pad))) / 8) * surface.h;
//			maskbits = SDL_small_alloc(Uint8, maskbitslen, &isstack);
			IntPtr maskbits = Marshal.AllocHGlobal(maskbitslen);// SDL_small_alloc(Uint8, maskbitslen, &isstack);
			if (maskbits == IntPtr.Zero) {
//				SDL_OutOfMemory();
//				return NULL;
				return null;
			}

			/* AND the cursor against full bits: no change. We already have alpha. */
			SDL_memset( ptr: maskbits, value: 0xFF, count: maskbitslen );

			SafeDibHandle dib;
			IntPtr        dibPixelData;
			using( SafeDCHandle hDCEntireScreen = Gdi.GetDCForEntireScreen() )
			{
				BITMAPINFO bitmapInfo = new BITMAPINFO();
				bitmapInfo.bmiheader = bmh;
				bitmapInfo.bmiColors = default; // <-- So SDL doesn't send this, it casts `bmiheader` to BITMAPINFO directlly... though that should be okay as it sets `bmh.bV4Size = sizeof(bmh)`, not `sizeof(BITMAPINFO)`.

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

	class Sdl2HardwareCursorException : Exception
	{
		public Sdl2HardwareCursorException(string message)
			: base(message) { }

		public Sdl2HardwareCursorException(string message, Exception innerException)
			: base(message, innerException) { }
	}

	sealed class Sdl2HardwareCursor : IHardwareCursor
	{
		public IntPtr Cursor { get; private set; }
		IntPtr surface;

		public Sdl2HardwareCursor(Size size, byte[] data, int2 hotspot)
		{
			try
			{
				this.surface = SDL.SDL_CreateRGBSurface(flags: 0, width: size.Width, height: size.Height, depth: 32, Rmask: 0x00FF0000, Gmask: 0x0000FF00, Bmask: 0x000000FF, Amask: 0xFF000000);
				if (this.surface == IntPtr.Zero)
					throw new InvalidDataException($"Failed to create surface: {SDL.SDL_GetError()}");

				SDL.SDL_Surface sur = (SDL.SDL_Surface)Marshal.PtrToStructure(surface, typeof(SDL.SDL_Surface));
				Marshal.Copy(data, 0, sur.pixels, data.Length);

				// This call very occasionally fails on Windows, but often works when retried.
				for (var retries = 0; retries < 3 && Cursor == IntPtr.Zero; retries++)
				{
					this.Cursor = SDL.SDL_CreateColorCursor(surface, hotspot.X, hotspot.Y);
					if (this.Cursor == IntPtr.Zero)
					{
						int countNonZero = data.Count(b => b != 0x00);

						// Let's try this...
						this.Cursor = WinNativeMethods.SDL_WIN_CreateCursor(sur, hotspot.X, hotspot.Y);

						Debugger.Break();
					}
				}

				if (Cursor == IntPtr.Zero)
					throw new Sdl2HardwareCursorException($"Failed to create cursor: {SDL.SDL_GetError()}");
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (Cursor != IntPtr.Zero)
			{
				SDL.SDL_FreeCursor(Cursor);
				Cursor = IntPtr.Zero;
			}

			if (surface != IntPtr.Zero)
			{
				SDL.SDL_FreeSurface(surface);
				surface = IntPtr.Zero;
			}
		}
	}
}
