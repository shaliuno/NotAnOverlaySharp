﻿#region License

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
using System.Text;
using System.Threading.Tasks;

namespace NotAnOverlaySharp
{
    public static class Program
    {
        [STAThread]
        private static void Main()
        {
            NotAnOverlay notAnOverlay = new NotAnOverlay();
            notAnOverlay.InitOverlay();
            //Console.WriteLine("Press any key...");
            //Console.Read();
        }
    }
}
