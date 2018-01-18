using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace RatTracker.Infrastructure.NativeInterop
{
  // Suppress ReSharper warning for this file as it calls unmanaged code.
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  [SuppressMessage("ReSharper", "UnusedMember.Local")]
  [SuppressMessage("ReSharper", "ConvertToConstant.Local")]
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  public class Monitor
  {
    public static HandleRef nullHandleRef = new HandleRef(null, IntPtr.Zero);

    private Monitor(IntPtr monitor)
    {
      var info = new MonitorInfoEx();
      GetMonitorInfo(new HandleRef(null, monitor), info);
      Bounds = new System.Windows.Rect(
        info.rcMonitor.left, info.rcMonitor.top,
        info.rcMonitor.right - info.rcMonitor.left,
        info.rcMonitor.bottom - info.rcMonitor.top);
      WorkingArea = new System.Windows.Rect(
        info.rcWork.left, info.rcWork.top,
        info.rcWork.right - info.rcWork.left,
        info.rcWork.bottom - info.rcWork.top);
      IsPrimary = (info.dwFlags & MonitorinfofPrimary) != 0;
      Name = new string(info.szDevice).TrimEnd((char)0);
    }

    public System.Windows.Rect Bounds { get; }
    public System.Windows.Rect WorkingArea { get; }
    public string Name { get; }

    public bool IsPrimary { get; }

    public static IEnumerable<Monitor> AllMonitors
    {
      get
      {
        var closure = new MonitorEnumCallback();
        var proc = new MonitorEnumProc(closure.Callback);
        EnumDisplayMonitors(nullHandleRef, IntPtr.Zero, proc, IntPtr.Zero);
        return closure.Monitors.Cast<Monitor>();
      }
    }

    private class MonitorEnumCallback
    {
      public MonitorEnumCallback()
      {
        Monitors = new ArrayList();
      }

      public ArrayList Monitors { get; }

      public bool Callback(IntPtr monitor, IntPtr hdc,
        IntPtr lprcMonitor, IntPtr lparam)
      {
        Monitors.Add(new Monitor(monitor));
        return true;
      }
    }

    #region Dll imports

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [ResourceExposure(ResourceScope.None)]
    private static extern bool GetMonitorInfo
      (HandleRef hmonitor, [In] [Out] MonitorInfoEx info);

    [DllImport("user32.dll", ExactSpelling = true)]
    [ResourceExposure(ResourceScope.None)]
    private static extern bool EnumDisplayMonitors
      (HandleRef hdc, IntPtr rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    private delegate bool MonitorEnumProc
      (IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
      public readonly int left;
      public readonly int top;
      public readonly int right;
      public readonly int bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
    private class MonitorInfoEx
    {
      internal int cbSize = Marshal.SizeOf(typeof(MonitorInfoEx));
      internal Rect rcMonitor = new Rect();
      internal Rect rcWork = new Rect();
      internal readonly int dwFlags = 0;

      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] internal readonly char[] szDevice = new char[32];
    }

    private const int MonitorinfofPrimary = 0x00000001;

    #endregion
  }
}