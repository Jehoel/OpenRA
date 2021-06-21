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
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1005 // Single line comments should begin with single space
#pragma warning disable SA1514 // Element documentation header should be preceded by blank line
#pragma warning disable SA1515 // Single-line comment should be preceded by blank line

#define INLINE_CIEXYZTRIPLE_INSTEAD

using System;
using System.Runtime.InteropServices;

using BipCmp = OpenRA.Platforms.Win32.BitmapCompressionMode;

namespace OpenRA.Platforms.Win32
{
	public enum DibUsage : UInt32
	{
		DIB_RGB_COLORS = 0,
		DIB_PAL_COLORS = 1,
	}

	public enum BitmapCompressionMode : int
	{
		BI_RGB       = 0x0000,
		BI_RLE8      = 0x0001,
		BI_RLE4      = 0x0002,
		BI_BITFIELDS = 0x0003,
		BI_JPEG      = 0x0004,
		BI_PNG       = 0x0005,
		BI_CMYK      = 0x000B,
		BI_CMYKRLE8  = 0x000C,
		BI_CMYKRLE4  = 0x000D
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct BITMAPINFO
	{
		public static readonly uint SizeOf = (uint)Marshal.SizeOf<BITMAPINFO>();

//		public BITMAPINFOHEADER bmiheader;
		public BITMAPV4HEADER   bmiheader;
		public RGBQUAD          bmiColors;
	}

	/*
	[StructLayout(LayoutKind.Sequential)]
	public struct BITMAPINFOHEADER
	{
		public uint    biSize;
		public int     biWidth;
		public int     biHeight;
		public ushort  biPlanes;
		public ushort  biBitCount;
		public BipCmp  biCompression;
		public uint    biSizeImage;
		public int     biXPelsPerMeter;
		public int     biYPelsPerMeter;
		public uint    biClrUsed;
		public uint    biClrImportant;
	}
	*/

	[StructLayout(LayoutKind.Sequential)]
	public struct BITMAPV4HEADER
	{
		public static readonly uint SizeOf = (uint)Marshal.SizeOf<BITMAPV4HEADER>();

		public uint   bV4Size;
		public int    bV4Width;
		public int    bV4Height;
		public ushort bV4Planes;
		public ushort bV4BitCount;
		public BipCmp bV4V4Compression;
		public uint   bV4SizeImage;
		public int    bV4XPelsPerMeter;
		public int    bV4YPelsPerMeter;
		public uint   bV4ClrUsed;
		public uint   bV4ClrImportant;
		public uint   bV4RedMask;
		public uint   bV4GreenMask;
		public uint   bV4BlueMask;
		public uint   bV4AlphaMask;
		public uint   bV4CSType;
#if INLINE_CIEXYZTRIPLE_INSTEAD
		public int    RedX;          /* X coordinate of red endpoint */
		public int    RedY;          /* Y coordinate of red endpoint */
		public int    RedZ;          /* Z coordinate of red endpoint */
		public int    GreenX;        /* X coordinate of green endpoint */
		public int    GreenY;        /* Y coordinate of green endpoint */
		public int    GreenZ;        /* Z coordinate of green endpoint */
		public int    BlueX;         /* X coordinate of blue endpoint */
		public int    BlueY;         /* Y coordinate of blue endpoint */
		public int    BlueZ;         /* Z coordinate of blue endpoint */
#else
		CIEXYZTRIPLE bV4Endpoints; // https://stackoverflow.com/questions/20864752/how-is-defined-the-data-type-fxpt2dot30-in-the-bmp-file-structure
#endif
		public uint   bV4GammaRed;
		public uint   bV4GammaGreen;
		public uint   bV4GammaBlue;
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
	public struct ICONINFO
	{
		public static readonly int SizeOf = Marshal.SizeOf<BITMAPINFO>();

		/// <summary>
		/// Specifies whether this structure defines an icon or a cursor.
		/// A value of TRUE specifies an icon; FALSE specifies a cursor
		/// </summary>
		[MarshalAs(UnmanagedType.Bool)]
		public bool fIcon;
		/// <summary>The x-coordinate of a cursor's hot spot</summary>
		public UInt32 xHotspot;
		/// <summary>The y-coordinate of a cursor's hot spot</summary>
		public UInt32 yHotspot;
		/// <summary>The icon bitmask bitmap</summary>
		public IntPtr hbmMask;
		/// <summary>A handle to the icon color bitmap.</summary>
		public IntPtr hbmColor;
	}
}
