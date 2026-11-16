using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation;

namespace frm_avast_uninstaller
{
    internal static class UiDriver
    {
        private static readonly Regex RemoveRx = new Regex(
            @"^\s*(remove|uninstall|gỡ|xóa|loại bỏ|odinstal|desinstal|entfernen|supprimer)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Welcome screen: fixed layout, "Remove" button sits at horizontal centre, ~66% down.
        private const double RemoveXRatio = 0.50;
        private const double RemoveYRatio = 0.665;

        // Uninstall is judged finished when the registry entry is gone and the Avast file
        // footprint has stopped changing for a while (the final screen just waits for Restart).
        private const int StableSeconds = 60;
        private const int MinSecondsAfterClick = 90;

        internal static bool DriveAvastClear(Process proc, int timeoutMs, Func<string> progressSignature)
        {
            var sw = Stopwatch.StartNew();
            bool clicked = false;
            bool loggedWindow = false;
            DateTime clickedAt = DateTime.MinValue;
            string lastSig = null;
            DateTime sigSince = DateTime.MinValue;

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (proc.HasExited) return clicked;

                try
                {
                    if (!clicked)
                    {
                        IntPtr hwnd = FindAvastClearWindow();
                        if (hwnd != IntPtr.Zero)
                        {
                            RECT r;
                            GetWindowRect(hwnd, out r);
                            if (!loggedWindow)
                            {
                                loggedWindow = true;
                                Program.Log("UI window: '" + GetTitle(hwnd) + "' rect=" + r.Left + "," + r.Top + "-" + r.Right + "," + r.Bottom);
                            }

                            // Give the web UI a moment to render after the window appears.
                            Thread.Sleep(2500);

                            if (TryUiaClick(hwnd))
                            {
                                clicked = true;
                                Program.Log("UI: clicked Remove via UIAutomation");
                            }
                            else if (r.Right - r.Left > 200 && r.Bottom - r.Top > 200)
                            {
                                int x = r.Left + (int)((r.Right - r.Left) * RemoveXRatio);
                                int y = r.Top + (int)((r.Bottom - r.Top) * RemoveYRatio);
                                SetForegroundWindow(hwnd);
                                Thread.Sleep(300);
                                MouseClick(x, y);
                                clicked = true;
                                clickedAt = DateTime.UtcNow;
                                Program.Log("UI: clicked Remove by position (" + x + "," + y + ")");
                            }
                        }
                    }
                    else if (progressSignature != null)
                    {
                        string sig = progressSignature();
                        if (sig != lastSig) { lastSig = sig; sigSince = DateTime.UtcNow; }

                        bool stable = (DateTime.UtcNow - sigSince).TotalSeconds >= StableSeconds;
                        bool afterClick = (DateTime.UtcNow - clickedAt).TotalSeconds >= MinSecondsAfterClick;
                        if (stable && afterClick && sig.StartsWith("entry=0"))
                        {
                            Program.Log("UI: uninstall finished (" + sig + " stable " + StableSeconds + "s); closing avastclear.");
                            try { proc.Kill(); } catch { }
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Log("UI driver: " + ex.Message);
                }

                Thread.Sleep(clicked ? 5000 : 1500);
            }

            return clicked;
        }

        private static bool TryUiaClick(IntPtr hwnd)
        {
            try
            {
                var win = AutomationElement.FromHandle(hwnd);
                var buttons = win.FindAll(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                var names = new List<string>();
                AutomationElement target = null;
                foreach (AutomationElement b in buttons)
                {
                    string n = SafeName(b);
                    names.Add(n ?? "");
                    if (target == null && !string.IsNullOrEmpty(n) && RemoveRx.IsMatch(n)) target = b;
                }
                Program.Log("UIA buttons (" + names.Count + "): " + string.Join(" | ", names));
                if (target == null) return false;

                object p;
                if (target.TryGetCurrentPattern(InvokePattern.Pattern, out p))
                {
                    ((InvokePattern)p).Invoke();
                    return true;
                }
                var rc = target.Current.BoundingRectangle;
                if (rc.IsEmpty) return false;
                MouseClick((int)(rc.Left + rc.Width / 2), (int)(rc.Top + rc.Height / 2));
                return true;
            }
            catch (Exception ex)
            {
                Program.Log("UIA: " + ex.Message);
                return false;
            }
        }

        private static IntPtr FindAvastClearWindow()
        {
            return FindWindowByTitle(t => t.IndexOf("Avast", StringComparison.OrdinalIgnoreCase) >= 0
                                       && t.IndexOf("Clear", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static IntPtr FindWindowByTitle(Func<string, bool> match)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((h, l) =>
            {
                if (!IsWindowVisible(h)) return true;
                string t = GetTitle(h);
                if (t.Length > 0 && match(t)) { found = h; return false; }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private static string GetTitle(IntPtr h)
        {
            var sb = new StringBuilder(256);
            GetWindowText(h, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string SafeName(AutomationElement e)
        {
            try { return e.Current.Name; } catch { return null; }
        }

        private static void MouseClick(int x, int y)
        {
            SetCursorPos(x, y);
            Thread.Sleep(120);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(60);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder sb, int max);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    }
}
