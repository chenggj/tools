using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AdBlockerTool
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private AppWindow m_appWindow;
        public MainWindow()
        {
            this.InitializeComponent();
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            m_appWindow = AppWindow.GetFromWindowId(myWndId);
            if (m_appWindow != null)
            {
                m_appWindow.Title = "AdBlocker Tool";
            }
            var ScreenHeight = DisplayArea.Primary.WorkArea.Height;
            var ScreenWidth = DisplayArea.Primary.WorkArea.Width;
            m_appWindow.MoveAndResize(new RectInt32(ScreenWidth / 2 - 520, (int)(ScreenHeight / 2 - 300), 1140, 700));
        }

        private string GetExtensionsID(bool isEdge = true)
        {
            string extensionsID = "";
            //if (this.adblock.IsChecked == true)
            //{
            //    extensionsID = this.Edge.IsChecked.Value ? "ndcileolkflehcjpmjnfbnaibdcgglog" : "gighmmpiobklfepjocnamgkkbiglidom";
            //}
            //else if (this.adblockPlus.IsChecked == true)
            //{
            //    extensionsID = isEdge ? "gmgoamodcdcjnbaobigkjelfplakmdhh" : "cfhdojbkjhnklbpkdaibdccddilifddb";
            //}
            //else
            //{
            //    extensionsID = this.Edge.IsChecked.Value ? "pdffkfellgipmhklpdmokmckkkfcopbh" : "bgnkhhnnamicmpeenaelnjfhikgbkllg";
            //}

            extensionsID = isEdge ? "gmgoamodcdcjnbaobigkjelfplakmdhh" : "cfhdojbkjhnklbpkdaibdccddilifddb";

            return extensionsID;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.status.Text = "";
            if (this.ForceInstall.IsChecked == true)
            {
                MyRegisterManager.AddKeyToPolicy(this.GetExtensionsID(), this.Edge.IsChecked.Value);
            }
            else
            {
                MyRegisterManager.AddKeyToNormalAddress(this.GetExtensionsID(), this.Edge.IsChecked.Value);
            }

            this.status.Text = "安装完成";
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.status.Text = "";
            if (this.ForceInstall.IsChecked == true)
            {
                MyRegisterManager.UninstallPolicyKey(this.GetExtensionsID(), this.Edge.IsChecked.Value);
            }
            else
            {
                MyRegisterManager.UninstallNormalAddress(this.GetExtensionsID(), this.Edge.IsChecked.Value);
            }


            this.status.Text = "卸载完成";
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            MyRegisterManager.AddKeyToNormalAddress(this.GetExtensionsID(true), true);

            MyRegisterManager.AddKeyToNormalAddress(this.GetExtensionsID(false), false);
            this.Switch.Content = "Disable";
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            MyRegisterManager.UninstallNormalAddress(this.GetExtensionsID(isEdge: true), isEdge: true);
            MyRegisterManager.UninstallNormalAddress(this.GetExtensionsID(isEdge: false), isEdge: false);

            this.Switch.Content = "Enable";
        }

        private void ToggleButton1_Checked(object sender, RoutedEventArgs e)
        {
            MyRegisterManager.AddKeyToPolicy(this.GetExtensionsID(true), true);

            MyRegisterManager.AddKeyToPolicy(this.GetExtensionsID(false), false);
            this.Switch2.Content = "Disable";
        }

        private void ToggleButton1_Unchecked(object sender, RoutedEventArgs e)
        {
            MyRegisterManager.UninstallPolicyKey(this.GetExtensionsID(isEdge: true), isEdge: true);
            MyRegisterManager.UninstallPolicyKey(this.GetExtensionsID(isEdge: false), isEdge: false);

            this.Switch2.Content = "Enable";
        }
    }
}
