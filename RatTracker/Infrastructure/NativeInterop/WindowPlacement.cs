using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Newtonsoft.Json;

// Native user32.dll interop
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace RatTracker.Infrastructure.NativeInterop
{
  // RECT structure required by WINDOWPLACEMENT structure
  [Serializable]
  [StructLayout(LayoutKind.Sequential)]
  public struct RECT
  {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public RECT(int left, int top, int right, int bottom)
    {
      Left = left;
      Top = top;
      Right = right;
      Bottom = bottom;
    }
  }

  // POINT structure required by WINDOWPLACEMENT structure
  [Serializable]
  [StructLayout(LayoutKind.Sequential)]
  public struct POINT
  {
    public int X;
    public int Y;

    public POINT(int x, int y)
    {
      X = x;
      Y = y;
    }
  }

  // WINDOWPLACEMENT stores the position, size, and state of a window
  [Serializable]
  [StructLayout(LayoutKind.Sequential)]
  public struct WINDOWPLACEMENT
  {
    public int length;
    public int flags;
    public int showCmd;
    public POINT minPosition;
    public POINT maxPosition;
    public RECT normalPosition;
  }

  public static class WindowPlacement
  {
    private const int SW_SHOWNORMAL = 1;
    private const int SW_SHOWMINIMIZED = 2;

    public static void SetPlacement(IntPtr windowHandle, string placementXml)
    {
      if (string.IsNullOrEmpty(placementXml)) { return; }

      try
      {
        var placement = JsonConvert.DeserializeObject<WINDOWPLACEMENT>(placementXml);

        placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
        placement.flags = 0;
        placement.showCmd = placement.showCmd == SW_SHOWMINIMIZED ? SW_SHOWNORMAL : placement.showCmd;
        SetWindowPlacement(windowHandle, ref placement);
      }
      catch (InvalidOperationException)
      {
      }
    }

    public static string GetPlacement(IntPtr windowHandle)
    {
      GetWindowPlacement(windowHandle, out var placement);
      return JsonConvert.SerializeObject(placement);
    }
    public static void SetPlacement(this Window window, string placementXml)
    {
      SetPlacement(new WindowInteropHelper(window).Handle, placementXml);
    }

    public static string GetPlacement(this Window window)
    {
      return GetPlacement(new WindowInteropHelper(window).Handle);
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    private static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);
  }
}