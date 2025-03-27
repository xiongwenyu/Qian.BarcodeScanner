# BarcodeScanner

## 项目简介

`BarcodeScanner` 是一个用于处理键盘和扫码枪输入的 WPF 库。它提供了灵活的输入模式支持，包括仅键盘输入、仅扫码枪输入以及两者兼容的输入模式。通过该库，开发者可以轻松地集成扫码枪功能到 WPF 应用程序中。

---

## 功能特性

- **支持多种输入模式**:
  - 键盘输入 (`KeyboardInput`)
  - 扫码枪输入 (`BarcodeScannerInput`)
  - 键盘和扫码枪混合输入 (`BothInput`)

- **设备管理**:
  - 添加和移除已知设备。
  - 缓存设备输入状态以提高性能。

- **事件支持**:
  - 提供 `ScanCode` 路由事件，用于处理扫码枪输入。

- **易于集成**:
  - 通过附加属性和事件处理程序快速绑定到 WPF 控件。

---

## 快速开始

### 1. 安装

将 `Qian.BarcodeScanner.WPF` 项目添加到您的解决方案中，并在您的 WPF 项目中引用它。

---

### 2. 使用示例

#### XAML 示例

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

#### 代码隐藏示例

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
            txtScanCode.Text = $"扫描结果: {e.ScanCode}";
        }
    }
}
```

---

## 输入模式

- **`KeyboardInput`**: 仅支持键盘输入。
- **`BarcodeScannerInput`**: 仅支持扫码枪输入。
- **`BothInput`**: 支持键盘和扫码枪输入。

---

## 事件

- **`ScanCode`**:  
  当扫码枪输入完成时触发，提供扫描的字符串。

---

## 设备管理

### 添加已知设备

```csharp
BarcodeScannerManager.AddBarcodeScannerKnownDevice("DevicePath");
BarcodeScannerManager.AddKeyboardKnownDevice("DevicePath");
```

### 移除已知设备

```csharp
BarcodeScannerManager.RemoveKnownDevice(deviceInfo);
```

---

## 贡献

欢迎提交问题和贡献代码！

---

## 许可证

此项目基于 MIT 许可证。