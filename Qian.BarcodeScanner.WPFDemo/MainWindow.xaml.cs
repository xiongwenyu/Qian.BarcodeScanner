using System.Windows;
using System.Windows.Controls;
using Qian.BarcodeScanner;

namespace Qian.BarcodeScanner.WPFDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_ScanCode(object sender, ScanCodeEventArgs e)
        {
            txtScanCode.Text = "ScanCode:" + e.ScanCode;
            var txt = sender as TextBox;
            txt.Focus();
            txt.Text = e.ScanCode;
            txt.SelectAll();
        }
    }
}
