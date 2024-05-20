using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AdBlockTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            this.Width = 900;
            this.Height = 400;
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

        private void proxy_Checked(object sender, RoutedEventArgs e)
        {
            ////to enable proxy
        }

        private void proxy_Unchecked(object sender, RoutedEventArgs e)
        {
            ////to disable proxy
        }
    }
}