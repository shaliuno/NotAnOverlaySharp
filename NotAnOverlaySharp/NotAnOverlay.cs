#region License

// ====================================================
// NotAnOverlay by https://github.com/PierreCiholas/NotAnOverlay
// NotAnOverlaySharp by Shaliuno
// Attempt to port C++ to C# with C++ styles, so HWND and stuff
// are typedef`d somehow here for learning purposes. And can be removed when necessary.
// 
// ====================================================

#endregion License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

/* Почитать
 * https://www.pinvoke.net/
 * https://stackoverflow.com/questions/30986829/createwindowex-and-registerclass-c-sharp
 * Хорошая реализация:
 * https://www.codeproject.com/Articles/3024/Capturing-the-Screen-Image-in-C
 * 
 * It's funny how MS has gone from typedef'ing everything in sight in C/C++ to
 * creating a language that doesn't allow it at all. (Peter Thornqvist) https://bytes.com/topic/c-sharp/answers/560206-type-alias-c
 * https://stackoverflow.com/questions/3259267/how-to-alias-a-built-in-type-in-c
 */

namespace NotAnOverlaySharp
{
    internal delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public class NotAnOverlay
    {
        /// https://www.pinvoke.net/default.aspx/Enums.TernaryRasterOperations
        private const uint SRCCOPY = 0x00CC0020;
        private const int PM_REMOVE = 0x0001;
        private const int CW_USEDEFAULT = unchecked((int)0x80000000);

        // TODO fullscreen borderless on dclick
        const uint WM_DESTROY = 2;
        const uint WM_PAINT = 0x0f;
        const uint WM_LBUTTONUP = 0x0202;
        const uint WM_LBUTTONDBLCLK = 0x0203;

        /// https://github.com/jonkoops/pirate/tree/master/XPTable/Win32
        /// https://www.pinvoke.net/default.aspx/Enums.WindowStyles

        private HWND m_captureTarget;
        private HWND m_hwndNotOverlay;
        private POINT m_screenResolution;

        private RECT m_windowSize;
        private HDC m_hdcSource;
        private HDC m_hdcDestination;
        private HDC m_hdcSrcTemp;
        private HDC m_hdcDstTemp;

        private HBITMAP m_hBitmapSource;
        private HBITMAP m_hBitmapTemp;

        private WndProc delegWndProc = myWndProc;

        /* This is calculated to remove window caption,border and invisible border of int 3 for the border, which isn't included in the size returned by $SM_CYCAPTION from the capture.
            There are several ways to get it.
            Using GetSystemMetrics
            Using GetThemeSysSize
            But I`m afraid different Win version could make different values. I doubt that Win7 or less will be used but I decided to use different way.

            I use this one via ClientToScreen:
            https://stackoverflow.com/questions/21566307/how-to-get-the-title-bar-height-and-width-for-aero-and-basic-design
        */

        private int windowMetricsCorrectionX;
        private int windowMetriscCorrectionY;

        // For equal scaling.
        private double scaleAspectRatio;

        public NotAnOverlay(string processName = "")
        {
            if (processName != "")
            {
                Console.WriteLine("NotAnOverlay\t\tGetting handle for: {0}", processName);
                m_captureTarget = WinGetHandle(processName);
            }
            else
            {
                m_captureTarget = NativeMethods.GetDesktopWindow();                               // Primary monitor screen

                /// m_captureTarget = NativeMethods.GetForegroundWindow();                        // Currently active window
                /// m_captureTarget = IntPtr.Zero;                                                // Full screen not matter what is open
                /// m_captureTarget = NativeMethods.FindWindow("Notepad", "Untitled - Notepad");  // Specified window
            }

            // What to capture.
            m_hdcSource = NativeMethods.GetDC(m_captureTarget);
            m_hdcSrcTemp = NativeMethods.CreateCompatibleDC(m_hdcSource);
            Console.WriteLine("NotAnOverlay\t\tConstructor : m_captureTarget.hwnd {0} ", m_captureTarget.Handle.ToString());
        }

        ~NotAnOverlay()
        {
            NativeMethods.DeleteObject(m_hBitmapSource);
            NativeMethods.DeleteObject(m_hBitmapTemp);
            NativeMethods.DeleteDC(m_hdcDestination);
            NativeMethods.DeleteDC(m_hdcDstTemp);
            NativeMethods.DeleteDC(m_hdcSrcTemp);
            NativeMethods.ReleaseDC(IntPtr.Zero, m_hdcSource);
        }

        /* StretchBlt() Modes */
        internal enum StretchBltModes
        {
            BLACKONWHITE = 1,
            WHITEONBLACK = 2,
            COLORONCOLOR = 3,
            HALFTONE = 4,
            MAXSTRETCHBLTMODE = 4,
        }

        public void InitOverlay(bool useCaptureTargetSize)
        {
            if (!GetScreenResolution(useCaptureTargetSize))
            {
                Console.WriteLine("InitOverlay\t\tUnable to get screen resolution.");
                return;
            }

            m_hwndNotOverlay = SpawnOverlayWindow();

            while (true)
            {
                ///    perfs.AddTick();
                CloneArea(0, 0, m_screenResolution.x, m_screenResolution.y, true);
                TreatWindowMessageQueue();
            }
        }

        public IntPtr WinGetHandle(string wName)
        {
            IntPtr hwnd = IntPtr.Zero;
            foreach (Process pList in Process.GetProcesses())
            {
                Console.WriteLine(pList.MainWindowTitle.ToString());
                if ((pList.MainWindowTitle.ToLower()).Contains(wName.ToLower()) && !pList.ToString().Contains("(dwm)"))
                {
                    Console.WriteLine("WinGetHandle\t\tFound window with process: {0} {1}", pList.ToString(), pList.MainWindowTitle.ToLower());
                    hwnd = pList.MainWindowHandle;
                }
            }

            Console.WriteLine("WinGetHandle\t\tHandle : {0}", hwnd.ToString("X"));
            return hwnd;
        }

        private bool GetScreenResolution(bool useCaptureTargetSize)
        {
            // This sets resolution ('inner') of your overlay screen. That will result in upscaling, downscaling the picture. Don`t forget that window caption also add extra size and if it doesn`t fit = downscale or use borderless.

            HWND hDesktop;

            if (useCaptureTargetSize)
            {
                // If we use same resolution as our target size
                hDesktop = m_captureTarget;
                Console.WriteLine("GetScreenResolution\t\t : Using Capture Target size");
            }
            else
            {
                // Use whole desktop on primary screen.
                hDesktop = NativeMethods.GetDesktopWindow();
                Console.WriteLine("GetScreenResolution\t\t : Using desktop on primary screen");
            }

            // Get capture area.
            if (!NativeMethods.GetWindowRect(hDesktop, out RECT desktop))
            {
                return false;
            }

            Console.WriteLine("GetScreenResolution\t\tUsing Capture Target size (m_captureTarget) : x{0} y{1} x{2} y{3}", desktop.left, desktop.top, desktop.right, desktop.bottom);

            // We need to get real area of what we need to capture, excluding window title border etc. So getting the correction.
            NativeMethods.ClientToScreen(hDesktop, out POINT pnt);
            windowMetricsCorrectionX = (pnt.x - desktop.left) * 2; // left and right borders
            windowMetriscCorrectionY = (pnt.y - desktop.top) + (pnt.x - desktop.left); // caption + bottom border

            Console.WriteLine("GetScreenResolution\t\tResulting  windowMetricsCorrection : x{0} y{1}", windowMetricsCorrectionX, windowMetriscCorrectionY);

            // Calculate the window size taking offsets from 0,0 and also applying correction.
            m_screenResolution.x = (desktop.right - desktop.left) - windowMetricsCorrectionX;
            m_screenResolution.y = (desktop.bottom - desktop.top) - windowMetriscCorrectionY;
            Console.WriteLine("GetScreenResolution\t\tResulting  size (m_screenResolution) : x{0} y{1} x{2} y{3}", 0, 0, m_screenResolution.x, m_screenResolution.y);

            return true;
        }

        private HWND SpawnOverlayWindow(string windowClassName = "Control", string windowTitle = "")
        {
            Console.WriteLine("SpawnOverlayWindow : RegisterWindowClass");
            if (!RegisterWindowClass(windowClassName))
            {
                Console.WriteLine("RegisterWindowClass failed");
                return IntPtr.Zero;
            }

            HWND hwnd;
            if (m_captureTarget != IntPtr.Zero)
            {
                // (uint)(WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE | WindowStyles.WS_SYSMENU) no border title etc
                /* Also creating window with windowMetricsCorrection to match window sizes.
                   +10 is some hardcoded offset, I don`t know how to get it right so I found it help for window size fit source to dest. */
                hwnd = NativeMethods.CreateWindowEx(0, windowClassName, windowTitle, (uint)(WindowStyles.WS_OVERLAPPEDWINDOW | WindowStyles.WS_VISIBLE), 1977, 118, m_screenResolution.x + windowMetricsCorrectionX + 10, m_screenResolution.y + windowMetriscCorrectionY + 10, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                Console.WriteLine("Creating capture window with {0} {1}", m_screenResolution.x + 10, m_screenResolution.y + 10);
            }
            else
            {
                hwnd = NativeMethods.CreateWindowEx(0, windowClassName, windowTitle, (uint)(WindowStyles.WS_OVERLAPPEDWINDOW | WindowStyles.WS_VISIBLE), 0, 0, CW_USEDEFAULT, CW_USEDEFAULT, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                Console.WriteLine("Creating capture-default window with {0} {1}", CW_USEDEFAULT, CW_USEDEFAULT);
            }

            Console.WriteLine("SpawnOverlayWindow: hwnd " + hwnd.Handle.ToString() + " : " + (uint)(WindowStyles.WS_OVERLAPPEDWINDOW | WindowStyles.WS_VISIBLE));

            if (hwnd == ((IntPtr)0))
            {
                uint error = NativeMethods.GetLastError();
                Console.WriteLine("SpawnOverlayWindow: error : " + error);
                return hwnd;
            }

            return hwnd;
        }

        private bool RegisterWindowClass(string windowClassName)
        {
            Console.WriteLine("RegisterWindowClass with className : " + windowClassName);
            // https://msdn.microsoft.com/en-us/library/windows/desktop/ms633577(v=vs.85).aspx
            WNDCLASSEX wind_class = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = (int)(ClassStyles.CS_HREDRAW | ClassStyles.CS_VREDRAW),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(delegWndProc),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = Marshal.GetHINSTANCE(GetType().Module),
                hIcon = NativeMethods.LoadIcon(IntPtr.Zero, (int)SystemIcons.IDI_WARNING),
                hCursor = NativeMethods.LoadCursor(IntPtr.Zero, (int)IDC_STANDARD_CURSORS.IDC_ARROW),

                // (IntPtr)COLOR.ACTIVECAPTION; Zero pointer will cause background to be 'null' that will help to avoid blinks. IntPtr.Zero
                hbrBackground = (IntPtr)COLOR.BACKGROUND + 2,
                lpszMenuName = windowClassName,
                lpszClassName = windowClassName,
                hIconSm = IntPtr.Zero
            };

            ushort regResult = NativeMethods.RegisterClassEx(ref wind_class);
            Console.WriteLine("RegisterWindowClass: " + regResult);

            if (regResult == 0)
            {
                uint error = NativeMethods.GetLastError();
                Console.WriteLine("RegisterWindowClass: error : " + error);
                return false;
            }

            return true;
        }

        private void CloneArea(int x, int y, int w, int h, bool preserveAspectRatio = false)
        {
            CaptureScreenArea(x, y, w, h, ref m_hBitmapSource);

            // Invalidate but don`t erase, it helps to avoid blinks caused by background color. We also set it to null just in case.
            NativeMethods.InvalidateRect(m_hwndNotOverlay, IntPtr.Zero, false); // Tells that the window should be repainted
            m_hdcDestination = NativeMethods.BeginPaint(m_hwndNotOverlay, out PAINTSTRUCT m_paintStruct);
            m_hdcDstTemp = NativeMethods.CreateCompatibleDC(m_hdcDestination);
            m_hBitmapTemp = NativeMethods.SelectObject(m_hdcDstTemp, m_hBitmapSource);

            // No resizing (ideal when same resolution)
            //// NativeMethods.BitBlt(m_hdcDestination, 0, 0, w, h, m_hdcDstTemp, 0, 0, SRCCOPY);

            // Resizing (ideal when resolutions are different)
            NativeMethods.GetClientRect(m_hwndNotOverlay, ref m_windowSize);
            NativeMethods.SetStretchBltMode(m_hdcDestination, (int)Enum.Parse(typeof(StretchBltModes), "COLORONCOLOR")); // Use HALFTONE for better image quality (but slower)

            // https://www.experts-exchange.com/questions/24774563/StretchBlt-in-C-Csharp-trying-to-copy-a-bitmap-image-to-screen.html

            if (preserveAspectRatio)
            {
                if (scaleAspectRatio == 0)
                {
                    scaleAspectRatio = Math.Round((double)w / h, 2);
                }

                int m_windowSizeNewX;
                int m_windowSizeNewY;  

                if (m_windowSize.right > m_windowSize.bottom)
                {
                    m_windowSizeNewY = Math.Min(m_windowSize.bottom, (int)Math.Round(m_windowSize.right / scaleAspectRatio));
                    m_windowSizeNewX = (int)Math.Round(m_windowSizeNewY * scaleAspectRatio, 2);
                }
                else
                {
                    m_windowSizeNewX = Math.Min(m_windowSize.right, (int)Math.Round(m_windowSize.bottom * scaleAspectRatio));
                    m_windowSizeNewY = (int)Math.Round(m_windowSizeNewX / scaleAspectRatio, 2);
                }

                NativeMethods.StretchBlt(m_hdcDestination, 0, 0, m_windowSizeNewX, m_windowSizeNewY, m_hdcDstTemp, x, y, w, h, SRCCOPY);
            }
            else
            {
                NativeMethods.StretchBlt(m_hdcDestination, 0, 0, m_windowSize.right, m_windowSize.bottom, m_hdcDstTemp, x, y, w, h, SRCCOPY);
            }

            NativeMethods.EndPaint(m_hwndNotOverlay, ref m_paintStruct);
            NativeMethods.UpdateWindow(m_hwndNotOverlay); // Force direct repaint window

            // Without it memory leak will occurr. C++ version needs this too.
            NativeMethods.DeleteObject(m_hBitmapSource);
            NativeMethods.DeleteObject(m_hBitmapTemp);
            NativeMethods.DeleteDC(m_hdcDstTemp);
        }

        private void CaptureScreenArea(int x, int y, int w, int h, ref HBITMAP hBitmap)
        {
            hBitmap = NativeMethods.CreateCompatibleBitmap(m_hdcSource, w, h);
            m_hBitmapTemp = NativeMethods.SelectObject(m_hdcSrcTemp, hBitmap);
            NativeMethods.BitBlt(m_hdcSrcTemp, 0, 0, w, h, m_hdcSource, x, y, SRCCOPY);
            hBitmap = NativeMethods.SelectObject(m_hdcSrcTemp, m_hBitmapTemp);
        }

        private void TreatWindowMessageQueue()
        {
            while (NativeMethods.PeekMessage(out MSG msg, IntPtr.Zero, 0, 0, PM_REMOVE))
            {
                NativeMethods.TranslateMessage(msg);
                NativeMethods.DispatchMessage(msg);
            }
        }

        private static IntPtr myWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                // All GUI painting must be done here
                case WM_PAINT:
                    break;

                case WM_LBUTTONDBLCLK:
                    ///MessageBox.Show("Doubleclick");
                    break;

                case WM_DESTROY:
                    ///DestroyWindow(hWnd);

                    // If you want to shutdown the application, call the next function instead of DestroyWindow
                    Environment.Exit(0);
                    break;

                default:
                    break;

            }
            return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
