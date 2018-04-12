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

namespace NotAnOverlaySharp
{
    public static class Program
    {
        [STAThread]
        private static void Main()
        {
            NotAnOverlay notAnOverlay = new NotAnOverlay();// "BlueStacks App Player");
            notAnOverlay.InitOverlay(true);
            Console.Read();
        }
    }
}
