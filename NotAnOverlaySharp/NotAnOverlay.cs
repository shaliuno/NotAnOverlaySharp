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

        const uint WM_DESTROY = 2;
        const uint WM_PAINT = 0x0f;
        const uint WM_LBUTTONUP = 0x0202;
        const uint WM_LBUTTONDBLCLK = 0x0203;

        /// https://github.com/jonkoops/pirate/tree/master/XPTable/Win32
        /// https://www.pinvoke.net/default.aspx/Enums.WindowStyles

        private HANDLE m_hThreadNotAnOverlay;
        private HWND m_hwndNotOverlay;
        private POINT m_screenResolution;

        private RECT m_windowSize; /// keep for now
        private PAINTSTRUCT m_paintStruct;
        private HDC m_hdcSource;
        private HDC m_hdcDestination;
        private HDC m_hdcSrcTemp;
        private HDC m_hdcDstTemp;

        private HBITMAP m_hBitmapSource;
        private HBITMAP m_hBitmapTemp;

        private WndProc delegWndProc = myWndProc;

        public NotAnOverlay()
        {
            //HWND hwnd = IntPtr.Zero; // Full screen not matter what is open
            HWND hwnd = NativeMethods.GetDesktopWindow(); // Currently active window
            //HWND hwnd = NativeMethods.GetForegroundWindow(); // Currently active window

            /// HWND hwnd = NativeMethods.FindWindow("Notepad", "Untitled - Notepad"); // Specified window
            m_hdcSource = NativeMethods.GetDC(hwnd);
            m_hdcSrcTemp = NativeMethods.CreateCompatibleDC(m_hdcSource);
            Console.WriteLine("NotAnOverlay : Constructor : hwnd " + hwnd.Handle.ToString());
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

        public void InitOverlay()
        {
            if (!GetScreenResolution())
            {
                Console.WriteLine("Unable to get screen resolution. NotAnOverlay.cs:161");
                return;
            }

            m_hwndNotOverlay = SpawnOverlayWindow();

            while (true)
            {
                ///    perfs.AddTick();
                CloneArea(0, 0, m_screenResolution.x, m_screenResolution.y);
                TreatWindowMessageQueue();
            }
        }

        private bool GetScreenResolution()
        {
            HWND hDesktop = NativeMethods.GetDesktopWindow();
            if (!NativeMethods.GetWindowRect(hDesktop, out RECT desktop))
            {
                return false;
            }

            m_screenResolution.x = desktop.right;
            m_screenResolution.y = desktop.bottom;
            Console.WriteLine("GetScreenResolution : x " + m_screenResolution.x + " : y " + m_screenResolution.y);
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

            HWND hwnd = NativeMethods.CreateWindowEx(0, windowClassName, windowTitle, (uint)(WindowStyles.WS_OVERLAPPEDWINDOW | WindowStyles.WS_VISIBLE), 0, 0, CW_USEDEFAULT, CW_USEDEFAULT, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            Console.WriteLine("SpawnOverlayWindow: hwnd " + hwnd.Handle.ToString());

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
            WNDCLASSEX wind_class = new WNDCLASSEX();
            wind_class.cbSize = Marshal.SizeOf(typeof(WNDCLASSEX));
            wind_class.style = (int)(ClassStyles.CS_HREDRAW | ClassStyles.CS_VREDRAW);
            wind_class.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(delegWndProc);
            wind_class.cbClsExtra = 0;
            wind_class.cbWndExtra = 0;
            wind_class.hInstance = Marshal.GetHINSTANCE(GetType().Module);
            wind_class.hIcon = NativeMethods.LoadIcon(IntPtr.Zero, (int)SystemIcons.IDI_APPLICATION);
            wind_class.hCursor = NativeMethods.LoadCursor(IntPtr.Zero, (int)IDC_STANDARD_CURSORS.IDC_ARROW);
            wind_class.hbrBackground = (IntPtr)COLOR.ACTIVECAPTION;
            wind_class.lpszMenuName = windowClassName;
            wind_class.lpszClassName = windowClassName;
            wind_class.hIconSm = IntPtr.Zero;

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

        private void CloneArea(int x, int y, int w, int h)
        {
            CaptureScreenArea(x, y, w, h, ref m_hBitmapSource);
            NativeMethods.InvalidateRect(m_hwndNotOverlay, IntPtr.Zero, true); // Tells that the window should be repainted
            m_hdcDestination = NativeMethods.BeginPaint(m_hwndNotOverlay, out m_paintStruct);
            m_hdcDstTemp = NativeMethods.CreateCompatibleDC(m_hdcDestination);
            m_hBitmapTemp = NativeMethods.SelectObject(m_hdcDstTemp, m_hBitmapSource);

            // No resizing (ideal when same resolution)
            //NativeMethods.BitBlt(m_hdcDestination, 0, 0, w, h, m_hdcDstTemp, 0, 0, SRCCOPY);

            // Resizing (ideal when resolutions are different)
            NativeMethods.GetClientRect(m_hwndNotOverlay, ref m_windowSize);
            NativeMethods.SetStretchBltMode(m_hdcDestination, (int)Enum.Parse(typeof(StretchBltModes), "COLORONCOLOR")); // Use HALFTONE for better image quality (but slower)
            // https://www.experts-exchange.com/questions/24774563/StretchBlt-in-C-Csharp-trying-to-copy-a-bitmap-image-to-screen.html
            NativeMethods.StretchBlt(m_hdcDestination, 0, 0, m_windowSize.right, m_windowSize.bottom, m_hdcDstTemp, x, y, w, h, SRCCOPY);
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
            MSG msg;
            while (NativeMethods.PeekMessage(out msg, IntPtr.Zero, 0, 0, PM_REMOVE))
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
                    //MessageBox.Show("Doubleclick");
                    break;

                case WM_DESTROY:
                    //DestroyWindow(hWnd);

                    //If you want to shutdown the application, call the next function instead of DestroyWindow
                    Environment.Exit(0);
                    break;

                default:
                    break;
            }
            return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
