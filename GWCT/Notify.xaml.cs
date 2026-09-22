using GWCT.Tool;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace GWCT
{
    /// <summary>
    /// Notify.xaml 的交互逻辑
    /// </summary>
    public partial class Notify : Window
    {
        /// <summary>
        /// 初始化提示窗口
        /// </summary>
        /// <param name="owner">窗口所有者</param>
        /// <param name="infos">窗口显示信息</param>
        public Notify(Window owner, string infos)
        {
            InitializeComponent();
            this.Owner = owner;
            this.InfoStd.Text = infos;
            if (owner != null)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = owner.Left + owner.Width - this.Width - 10;
                this.Top = owner.Top + 30 + 10;
            }
            this.MouseLeftButtonDown += (s, e) => MLBD = e.OriginalSource;
            this.MouseRightButtonDown += (s, e) => MRBD = e.OriginalSource;
            this.CLOSE.PreviewMouseDown += (s, e) =>
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        MLBD = s;
                        break;
                    case MouseButton.Right:
                        MRBD = s;
                        break;
                }
                e.Handled = true;
            };
            this.CLOSE.PreviewMouseUp += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && MLBD == e.OriginalSource) this.Close();
                e.Handled = true;
            };
            IntPtr handle = IntPtr.Zero;
            this.Loaded += (s, e) =>
            {
                handle = new WindowInteropHelper(this).Handle;
                try
                {
                    handle.Set(false, true, DWM.TO_COLORREF(((Application.Current.Resources["DarkBg"]) as SolidColorBrush).Color), DWM.TO_COLORREF((Application.Current.Resources["WhiteText"] as SolidColorBrush).Color), true);
                }
                catch (Exception) { }
            };
            this.ContentRendered += (s, e) =>
            {
                App.RedrawWindow(handle, IntPtr.Zero, IntPtr.Zero, App.RDW_INVALIDATE | App.RDW_ALLCHILDREN);
            };
        }

        /// <summary>
        /// 存储左键点击记录
        /// </summary>
        private object MLBD = null;
        /// <summary>
        /// 存储左键点击记录
        /// </summary>
        private object MRBD = null;
    }
}
