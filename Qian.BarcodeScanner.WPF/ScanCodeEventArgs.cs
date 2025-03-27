using System.Windows;

namespace Qian.BarcodeScanner
{
    public class ScanCodeEventArgs : RoutedEventArgs
    {
        public ScanCodeEventArgs(string scanCode) : base(BarcodeScannerManager.ScanCodeEvent)
        {
            this.ScanCode = scanCode;
        }
        public string ScanCode { get; private set; }
    }
    public delegate void ScanCodeRoutedEventHandler(object sender, ScanCodeEventArgs e);
}
