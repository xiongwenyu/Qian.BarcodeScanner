# BarcodeScanner

## Project Overview

`BarcodeScanner` is a WPF library for handling input from keyboards and barcode scanners. It provides flexible input mode support, including keyboard-only input, barcode scanner-only input, and a combined input mode. This library allows developers to easily integrate barcode scanner functionality into WPF applications.

---

## Features

- **Supports Multiple Input Modes**:
  - Keyboard Input (`KeyboardInput`)
  - Barcode Scanner Input (`BarcodeScannerInput`)
  - Combined Input (`BothInput`)

- **Device Management**:
  - Add and remove known devices.
  - Cache device input states for improved performance.

- **Event Support**:
  - Provides the `ScanCode` routed event for handling barcode scanner input.

- **Easy Integration**:
  - Quickly bind to WPF controls using attached properties and event handlers.

---

## Quick Start

### 1. Installation

Add the `Qian.BarcodeScanner.WPF` project to your solution and reference it in your WPF project.

---

### 2. Usage Example

#### XAML Example

```xml
<Window
    x:Class="YourNamespace.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:qian="clr-namespace:Qian.BarcodeScanner;assembly=Qian.BarcodeScanner"
    Title="Barcode Scanner Demo"
    Width="525"
    Height="350"
    qian:BarcodeScannerManager.ScanCode="Window_ScanCode">
    <StackPanel>
        <TextBlock>BarcodeScannerInput:</TextBlock>
        <TextBox
            Width="300"
            Height="30"
            Margin="10"
            qian:BarcodeScannerManager.InputMode="BarcodeScannerInput" />
        
        <TextBlock>KeyboardInput:</TextBlock>
        <TextBox
            Width="300"
            Height="30"
            Margin="10"
            qian:BarcodeScannerManager.InputMode="KeyboardInput" />
        
        <TextBlock>BothInput:</TextBlock>
        <TextBox
            Width="300"
            Height="30"
            Margin="10"
            qian:BarcodeScannerManager.InputMode="BothInput" />
        
        <TextBlock x:Name="txtScanCode" />
    </StackPanel>
</Window>
```

#### Code-Behind Example

```csharp
using System.Windows;
using Qian.BarcodeScanner;

namespace YourNamespace
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_ScanCode(object sender, ScanCodeEventArgs e)
        {
            txtScanCode.Text = $"Scanned Code: {e.ScanCode}";
        }
    }
}
```

---

## Input Modes

- **`KeyboardInput`**: Supports keyboard input only.
- **`BarcodeScannerInput`**: Supports barcode scanner input only.
- **`BothInput`**: Supports both keyboard and barcode scanner input.

---

## Events

- **`ScanCode`**:  
  Triggered when barcode scanner input is completed, providing the scanned string.

---

## Device Management

### Add Known Devices

```csharp
BarcodeScannerManager.AddBarcodeScannerKnownDevice("DevicePath");
BarcodeScannerManager.AddKeyboardKnownDevice("DevicePath");
```

### Remove Known Devices

```csharp
BarcodeScannerManager.RemoveKnownDevice(deviceInfo);
```

---

## Contribution

Feel free to submit issues and contribute to the code!

---

## License

This project is licensed under the MIT License.