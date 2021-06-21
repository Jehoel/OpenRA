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

using System;
using System.Runtime.InteropServices;

namespace OpenRA.Platforms.Win32
{
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
}
