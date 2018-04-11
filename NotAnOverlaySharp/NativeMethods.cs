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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NotAnOverlaySharp
{
    // https://www.pinvoke.net/default.aspx/Constants.Window%20styles
    [Flags]
    internal enum WindowStyles : uint
    {
        WS_OVERLAPPED = 0x00000000,
        WS_POPUP = 0x80000000,
        WS_CHILD = 0x40000000,
        WS_MINIMIZE = 0x20000000,
        WS_VISIBLE = 0x10000000,
        WS_DISABLED = 0x08000000,
        WS_CLIPSIBLINGS = 0x04000000,
        WS_CLIPCHILDREN = 0x02000000,
        WS_MAXIMIZE = 0x01000000,
        WS_CAPTION = 0x00C00000,     /*  WS_CAPTION = WS_BORDER | WS_DLGFRAME, */
        WS_BORDER = 0x00800000,
        WS_DLGFRAME = 0x00400000,
        WS_VSCROLL = 0x00200000,
        WS_HSCROLL = 0x00100000,
        WS_SYSMENU = 0x00080000,
        WS_THICKFRAME = 0x00040000,
        WS_GROUP = 0x00020000,
        WS_TABSTOP = 0x00010000,

        WS_MINIMIZEBOX = 0x00020000,
        WS_MAXIMIZEBOX = 0x00010000,

        WS_TILED = WS_OVERLAPPED,
        WS_ICONIC = WS_MINIMIZE,
        WS_SIZEBOX = WS_THICKFRAME,
        WS_TILEDWINDOW = WS_OVERLAPPEDWINDOW,

        WS_OVERLAPPEDWINDOW =
            (WS_OVERLAPPED |
              WS_CAPTION |
              WS_SYSMENU |
              WS_THICKFRAME |
              WS_MINIMIZEBOX |
              WS_MAXIMIZEBOX),

        WS_POPUPWINDOW =
            (WS_POPUP |
              WS_BORDER |
              WS_SYSMENU),

        WS_CHILDWINDOW = WS_CHILD,
    }

    [Flags]
    internal enum ClassStyles : uint
    {
        ByteAlignClient = 0x1000,
        CS_VREDRAW = 0x0001,
        CS_HREDRAW = 0x0002,
        CS_DBLCLKS = 0x0008,
        CS_OWNDC = 0x0020,
        CS_CLASSDC = 0x0040,
        CS_PARENTDC = 0x0080,
        CS_NOCLOSE = 0x0200,
        CS_SAVEBITS = 0x0800,
        CS_BYTEALIGNCLIENT = 0x1000,
        CS_BYTEALIGNWINDOW = 0x2000,
        CS_GLOBALCLASS = 0x4000,
        CS_IME = 0x00010000,
        CS_DROPSHADOW = 0x00020000
    }

    internal enum IDC_STANDARD_CURSORS
    {
        IDC_ARROW = 32512,
        IDC_IBEAM = 32513,
        IDC_WAIT = 32514,
        IDC_CROSS = 32515,
        IDC_UPARROW = 32516,
        IDC_SIZE = 32640,
        IDC_ICON = 32641,
        IDC_SIZENWSE = 32642,
        IDC_SIZENESW = 32643,
        IDC_SIZEWE = 32644,
        IDC_SIZENS = 32645,
        IDC_SIZEALL = 32646,
        IDC_NO = 32648,
        IDC_HAND = 32649,
        IDC_APPSTARTING = 32650,
        IDC_HELP = 32651
    }

    internal enum SystemIcons
    {
        IDI_APPLICATION = 32512,
        IDI_HAND = 32513,
        IDI_QUESTION = 32514,
        IDI_EXCLAMATION = 32515,
        IDI_ASTERISK = 32516,
        IDI_WINLOGO = 32517,
        IDI_WARNING = IDI_EXCLAMATION,
        IDI_ERROR = IDI_HAND,
        IDI_INFORMATION = IDI_ASTERISK,
    }

    internal enum COLOR : int
    {
        SCROLLBAR = 0,
        BACKGROUND = 1,
        DESKTOP = 1,
        ACTIVECAPTION = 2,
        INACTIVECAPTION = 3,
        MENU = 4,
        WINDOW = 5,
        WINDOWFRAME = 6,
        MENUTEXT = 7,
        WINDOWTEXT = 8,
        CAPTIONTEXT = 9,
        ACTIVEBORDER = 10,
        INACTIVEBORDER = 11,
        APPWORKSPACE = 12,
        HIGHLIGHT = 13,
        HIGHLIGHTTEXT = 14,
        BTNFACE = 15,
        THREEDFACE = 15,
        BTNSHADOW = 16,
        THREEDSHADOW = 16,
        GRAYTEXT = 17,
        BTNTEXT = 18,
        INACTIVECAPTIONTEXT = 19,
        BTNHIGHLIGHT = 20,
        TREEDHIGHLIGHT = 20,
        THREEDHILIGHT = 20,
        BTNHILIGHT = 20,
        THREEDDKSHADOW = 21,
        THREEDLIGHT = 22,
        INFOTEXT = 23,
        INFOBK = 24
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr wParam;
        public UIntPtr lParam;
        public uint time;
        public POINT pt;
    }

    // http://vbcity.com/forums/t/100117.aspx
    [StructLayout(LayoutKind.Sequential)]
    internal struct PAINTSTRUCT
    {
        internal IntPtr hdc;
        internal bool fErase;
        internal RECT rcPaint;
        internal bool fRestore;
        internal bool fIncUpdate;
    }

    // as it is just IntPtr we can either using HWND = System.IntPtr; but leaving it for clarity with c++, helps to learn, https://bytes.com/topic/c-sharp/answers/560206-type-alias-c
    internal struct HWND
    {
        public HWND(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }

        public static implicit operator IntPtr(HWND hwnd)
        {
            return hwnd.Handle;
        }

        public static implicit operator HWND(IntPtr ptr)
        {
            return new HWND(ptr);
        }
    }

    // same as above
    internal struct HANDLE
    {
        public HANDLE(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }

        public static implicit operator IntPtr(HANDLE handle)
        {
            return handle.Handle;
        }

        public static implicit operator HANDLE(IntPtr ptr)
        {
            return new HANDLE(ptr);
        }
    }

    // same as above
    internal struct HICON
    {
        public HICON(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }

        public static implicit operator IntPtr(HICON hicon)
        {
            return hicon.Handle;
        }

        public static implicit operator HICON(IntPtr ptr)
        {
            return new HICON(ptr);
        }
    }

    // same as above
    internal struct HDC
    {
        public HDC(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }

        public static implicit operator IntPtr(HDC hdc)
        {
            return hdc.Handle;
        }

        public static implicit operator HDC(IntPtr ptr)
        {
            return new HDC(ptr);
        }
    }

    // same as above
    internal struct HBITMAP
    {
        public HBITMAP(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; set; }

        public static implicit operator IntPtr(HBITMAP hbitmap)
        {
            return hbitmap.Handle;
        }

        public static implicit operator HBITMAP(IntPtr ptr)
        {
            return new HBITMAP(ptr);
        }
    }

    // same as above
    internal struct HGDIOBJ
    {
        public HGDIOBJ(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }

        public static implicit operator IntPtr(HGDIOBJ hgdiobj)
        {
            return hgdiobj.Handle;
        }

        public static implicit operator HGDIOBJ(IntPtr ptr)
        {
            return new HGDIOBJ(ptr);
        }
    }

    // easily overcomed by using System Drawing Point
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd162897(v=vs.85).aspx
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    // easily overcomed by using System Drawing Rectanble
    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd162805(v=vs.85).aspx
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WNDCLASSEX
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms633577(v=vs.85).aspx
        [MarshalAs(UnmanagedType.U4)]
        public int cbSize;
        [MarshalAs(UnmanagedType.U4)]
        public int style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
        public IntPtr hIconSm;
    }
    
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        internal static extern uint GetLastError();

        //[DllImport("kernel32.dll", EntryPoint = "RtlSecureZeroMemory")]
        //internal static extern void SecureZeroMemory(IntPtr ptr, IntPtr cnt);

        [DllImport("user32.dll")] ///, EntryPoint = "GetDesktopWindow")] remember :D
        internal static extern HWND GetDesktopWindow();

        [DllImport("user32.dll", EntryPoint = "FindWindowA")]
        internal static extern HWND FindWindow(
            string windowClassName,
            string windowTitleName);

        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadIcon(IntPtr hInstance, int lpIconName);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern HWND CreateWindowEx(
            int dwExStyle,
            [MarshalAs(UnmanagedType.LPWStr)]
            string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)]
            string lpWindowName,
            uint dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", EntryPoint = "InvalidateRect")]
        internal static extern bool InvalidateRect(IntPtr hwnd, IntPtr rect, bool erase);

        [DllImport("user32.dll")]
        internal static extern IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT lpPaint);

        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        internal static extern HWND GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern HDC GetDC(HWND hwnd);

        [DllImport("user32.dll")]
        internal static extern HDC ReleaseDC(HWND hwnd, HDC hdc);
        
        [DllImport("user32.dll")]
        internal static extern bool GetClientRect(HWND hwnd, ref RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern ushort RegisterClassEx(ref WNDCLASSEX lpWndClass);

        [DllImport("user32.dll")]
        internal static extern bool EndPaint(HWND hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        internal static extern bool UpdateWindow(HWND hWnd);

        [DllImport("user32.dll")]
        internal static extern bool PeekMessage(out MSG msg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        internal static extern bool TranslateMessage(MSG msg);

        [DllImport("user32.dll")]
        internal static extern bool DispatchMessage(MSG msg);

        [DllImport("user32.dll")]
        internal static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("gdi32.dll")]
        internal static extern HBITMAP CreateCompatibleBitmap(HDC hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        internal static extern HBITMAP SelectObject(HDC hdc, HBITMAP hgdiobj);

        [DllImport("gdi32.dll")]
        internal static extern bool BitBlt(
            HDC hdcDest,
            int nXDest,
            int nYDest,
            int nWidth,
            int nHeight,
            HDC hdcSrc,
            int nXSrc,
            int nYSrc,
            uint dwRop);

        [DllImport("gdi32.dll")]
        internal static extern HDC CreateCompatibleDC(HDC hdc);

        [DllImport("gdi32.dll")]
        internal static extern int SetStretchBltMode(HDC hdc, int iStretchMode);

        [DllImport("gdi32.dll")]
        internal static extern bool StretchBlt(
            HDC hdcDest,
            int nXDest,
            int nYDest,
            int nDestWidth,
            int nDestHeight,
            HDC hdcSrc,
            int nXSrc,
            int nYSrc,
            int nSrcWidth,
            int nSrcHeight,
            uint dwRop);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(HBITMAP hObject);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteDC(HDC hdc);
    }
}
