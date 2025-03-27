using System;

namespace Qian.BarcodeScanner
{
    /// <summary>
    /// Defines the input modes for barcode scanner and keyboard input.
    /// </summary>
    [Flags]
    public enum BarcodeScannerInputMode
    {
        /// <summary>
        /// No input mode is enabled, meaning no control is applied.
        /// </summary>
        None = 0,

        /// <summary>
        /// Enables keyboard input only.
        /// </summary>
        KeyboardInput = 1,

        /// <summary>
        /// Enables barcode scanner input only.
        /// </summary>
        BarcodeScannerInput = 2,

        /// <summary>
        /// Enables both keyboard and barcode scanner input.
        /// </summary>
        BothInput = KeyboardInput | BarcodeScannerInput,
    }
}
