using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static PInvoke.User32;

namespace BorderlessWindowWpf
{
    public enum HitTest : int
    {
        HTERROR = -2,
        HTTRANSPARENT = -1,
        HTNOWHERE = 0,
        HTCLIENT = 1,
        HTCAPTION = 2,
        HTSYSMENU = 3,
        HTGROWBOX = 4,
        HTSIZE = HTGROWBOX,
        HTMENU = 5,
        HTHSCROLL = 6,
        HTVSCROLL = 7,
        HTMINBUTTON = 8,
        HTMAXBUTTON = 9,
        HTLEFT = 10,
        HTRIGHT = 11,
        HTTOP = 12,
        HTTOPLEFT = 13,
        HTTOPRIGHT = 14,
        HTBOTTOM = 15,
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17,
        HTBORDER = 18,
        HTREDUCE = HTMINBUTTON,
        HTZOOM = HTMAXBUTTON,
        HTSIZEFIRST = HTLEFT,
        HTSIZELAST = HTBOTTOMRIGHT,
        HTOBJECT = 19,
        HTCLOSE = 20,
        HTHELP = 21,
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(Window.GetWindow(this)).Handle;
            //_ = SetWindowLong(hwnd, WindowLongIndexFlags.GWL_STYLE, (SetWindowLongFlags)(GetWindowLong(hwnd, WindowLongIndexFlags.GWL_STYLE) & ~(int)SetWindowLongFlags.WS_CAPTION & ~(int)SetWindowLongFlags.WS_SIZEBOX));
            // | (uint)SetWindowLongFlags.WS_POPUP
            _ = SetWindowLong(hwnd, WindowLongIndexFlags.GWL_STYLE, (SetWindowLongFlags)((int)SetWindowLongFlags.WS_MAXIMIZEBOX | (int)SetWindowLongFlags.WS_MINIMIZEBOX | (int)SetWindowLongFlags.WS_THICKFRAME | (int)SetWindowLongFlags.WS_VISIBLE | (uint)SetWindowLongFlags.WS_POPUP));
            _ = SetWindowLong(hwnd, WindowLongIndexFlags.GWL_EXSTYLE, (SetWindowLongFlags)((int)SetWindowLongFlags.WS_EX_OVERLAPPEDWINDOW | (int)SetWindowLongFlags.WS_EX_LAYERED | (int)SetWindowLongFlags.WS_EX_LEFT | (int)SetWindowLongFlags.WS_EX_LTRREADING));

            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            {
                hwndSource.AddHook(new HwndSourceHook(this.WndProc));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            internal RECT(int X, int Y, int Width, int Height)
            {
                this.Left = X;
                this.Top = Y;
                this.Right = Width;
                this.Bottom = Height;
            }
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NCCALCSIZE_PARAMS
        {
            internal RECT rect0, rect1, rect2;
            internal IntPtr lppos;
        }

        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_NCCALCSIZE = 0x0083;
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_NCACTIVATE = 0x0086;
        private readonly int agWidth = 16;
        private readonly int bThickness = 12;
        private Point mousePoint = new Point();
        private int TitleBarHeight = 36;
        private int SystemButtonWidth = 40;

        protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            double scale = 1;
            double left = this.Left * scale;
            double top = this.Top * scale;
            double width = this.Width * scale;
            double height = this.Height * scale;
            switch (msg)
            {
                //case WM_ERASEBKGND:
                //    return (IntPtr)1;
                //case WM_NCACTIVATE:
                //    lParam = (IntPtr)(-1);
                //    return (IntPtr)0;
                case 133://WM_NCPAINT
                    Debug.WriteLine("NCPAINT skiped....");
                    handled = true;
                    return IntPtr.Zero;
                case WM_NCCALCSIZE:
                    if (wParam != IntPtr.Zero)
                    {
                        NCCALCSIZE_PARAMS nc = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure(lParam, typeof(NCCALCSIZE_PARAMS));
                        //Debug.WriteLine($"---------------------{nc.rect0.Left} {nc.rect0.Top} {nc.rect0.Right} {nc.rect0.Bottom}");
                        //Debug.WriteLine($"---------------------{nc.rect1.Left} {nc.rect1.Top} {nc.rect1.Right} {nc.rect1.Bottom}");
                        //Debug.WriteLine($"---------------------{nc.rect2.Left} {nc.rect2.Top} {nc.rect2.Right} {nc.rect2.Bottom}");
                        //nc.rect0.Top -= 20;
                        //var border = nc.rect2.Left - nc.rect0.Left;
                        //nc.rect0.Top += 1;
                        nc.rect0.Bottom -= 1;
                        //nc.rect0.Left += border;
                        //nc.rect0.Right -= border;
                        nc.rect1 = nc.rect0;
                        Marshal.StructureToPtr(nc, lParam, false);
                        handled = true;
                        return (IntPtr)0x0400;
                    }
                    else
                        return IntPtr.Zero;
                case WM_NCHITTEST:
                    {
                        this.mousePoint.X = (lParam.ToInt32() & 0xFFFF);
                        this.mousePoint.Y = (lParam.ToInt32() >> 16);

                        handled = true;
                        //Console.WriteLine("this.ActualWidth: " + this.ActualWidth.ToString()+" this.Left: " + this.Left.ToString()+" X: "+ this.mousePoint.X.ToString()+" Y: "+ this.mousePoint.Y.ToString()+" WorkingAreaWidth: "+System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width.ToString()+" TitleBarHeight " + TitleBarHeight.ToString() + " SystemButtonWidth " + SystemButtonWidth.ToString());
                        if (this.WindowState == WindowState.Maximized)
                        {
                            if (this.mousePoint.Y <= TitleBarHeight && this.mousePoint.X < (SystemParameters.WorkArea.Width - SystemButtonWidth))
                            {
                                return new IntPtr((int)HitTest.HTCAPTION);
                            }
                            else
                                return new IntPtr((int)HitTest.HTCLIENT);
                        }

                        #region TestMouse
                        if (this.mousePoint.Y - top <= this.agWidth && this.mousePoint.X - left <= this.agWidth)
                        {
                            return new IntPtr((int)HitTest.HTTOPLEFT);
                        }
                        else if (height + top - this.mousePoint.Y <= this.agWidth && this.mousePoint.X - left <= this.agWidth)
                        {
                            return new IntPtr((int)HitTest.HTBOTTOMLEFT);
                        }
                        else if (this.mousePoint.Y - top <= this.agWidth && width + left - this.mousePoint.X <= this.agWidth)
                        {
                            return new IntPtr((int)HitTest.HTTOPRIGHT);
                        }
                        else if (width + left - this.mousePoint.X <= this.agWidth && height + top - this.mousePoint.Y <= this.agWidth)
                        {
                            return new IntPtr((int)HitTest.HTBOTTOMRIGHT);
                        }
                        else if (this.mousePoint.X - left <= this.bThickness)
                        {
                            return new IntPtr((int)HitTest.HTLEFT);
                        }
                        else if (width + left - this.mousePoint.X <= this.bThickness)
                        {
                            return new IntPtr((int)HitTest.HTRIGHT);
                        }
                        else if (this.mousePoint.Y - top <= this.bThickness)
                        {
                            return new IntPtr((int)HitTest.HTTOP);
                        }
                        else if (height + top - this.mousePoint.Y <= this.bThickness)
                        {
                            return new IntPtr((int)HitTest.HTBOTTOM);
                        }
                        else if (this.mousePoint.X - left < width - SystemButtonWidth && this.mousePoint.Y - top <= TitleBarHeight + bThickness)
                        {
                            Debug.WriteLine("HTCAPTION");
                            return new IntPtr((int)HitTest.HTCAPTION);
                        }
                        else
                            return new IntPtr((int)HitTest.HTCLIENT);
                        #endregion
                    }
                default:
                    break;
            }
            return IntPtr.Zero;
        }

    }
}
