#pragma warning disable SX1101 // Do not prefix local calls with 'this.'
#pragma warning disable IDE0007 // Use implicit type

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

				this.Cursor = SDL.SDL_CreateColorCursor(surface, hotspot.X, hotspot.Y);
				if (this.Cursor == IntPtr.Zero)
				{
					Console.WriteLine("SDL's " + nameof(SDL.SDL_CreateColorCursor) + " failed. Now using .NET reimplementation."); // Is there a better way to log instead of writing directly to the console?

					// This call very occasionally fails on Windows, but often works when retried.
					// Better idea: C# reimplementation!

					// The SDL `SDL_CreateColorCursor` function is here: https://github.com/libsdl-org/SDL/blob/c59d4dcd38c382a1e9b69b053756f1139a861574/src/events/SDL_mouse.c
					// It's mostly just some argument validation logic over `mouse->CreateCursor`
					// ...and `mouse->CreateCursor` is set to `WIN_CreateCursor` defined here: https://github.com/libsdl-org/SDL/blob/c59d4dcd38c382a1e9b69b053756f1139a861574/src/video/windows/SDL_windowsmouse.c

					// ...and this is the C# reimplementation.
					// If this reimplementation ever does fail at the same place (Win32's `CreateIconIndirect`) then an exception will be thrown - so we don't need to check `this.Cursor == IntPtr.Zero` here.
					this.Cursor = OpenRA.Platforms.Win32.SdlReimplementation.SDL_WIN_CreateCursor(sur, hotspot.X, hotspot.Y);

					Console.WriteLine(".NET reimplementation of " + nameof(OpenRA.Platforms.Win32.SdlReimplementation.SDL_WIN_CreateCursor) + " succeeded.");
				}
				else
				{
					Console.WriteLine("SDL's " + nameof(SDL.SDL_CreateColorCursor) + " succeeded on the first try.");
				}

				if (Cursor == IntPtr.Zero)
				{
					throw new Sdl2HardwareCursorException($"Failed to create cursor: {SDL.SDL_GetError()}");
				}
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

#pragma warning disable IDE0060 // Remove unused parameter
		void Dispose(bool disposing)
#pragma warning restore IDE0060 // Remove unused parameter
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
