using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace Qian.BarcodeScanner
{
    /// <summary>
    /// Manages barcode scanner input and integrates it with WPF applications.
    /// Provides functionality to handle input modes, manage known devices, and process raw input.
    /// </summary>
    public class BarcodeScannerManager
    {
        #region Fields
        // List of known devices (keyboard or barcode scanner).
        private static readonly List<DeviceInfo> _knownDevices = new List<DeviceInfo>();
        // Cache for device input states to improve performance.
        private static readonly Dictionary<RawInputHeader, bool> _cacheDevices = new Dictionary<RawInputHeader, bool>();
        // Routed event for barcode scanner input.
        public static readonly RoutedEvent ScanCodeEvent = EventManager.RegisterRoutedEvent("ScanCode", RoutingStrategy.Direct, typeof(ScanCodeRoutedEventHandler), typeof(BarcodeScannerManager));
        // StringBuilder to accumulate scan codes from barcode scanner input.
        private static StringBuilder _scanCode = new StringBuilder();
        // Flags to track input source type.
        private static bool _isBarcodeScannerInput;
        private static bool _unkownDeviceIsBarcodeScannerInput;
        // Rule to determine if input is from a barcode scanner.
        private static BarcodeScannerKeyboardInputRule _BarcodeScannerKeyboardInputRule;
        #endregion

        static BarcodeScannerManager()
        {
            // Register event handlers for TextBox controls to handle input events.
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDownThunk));
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewTextInputEvent, new TextCompositionEventHandler(OnPreviewTextInputThunk));
            EventManager.RegisterClassHandler(typeof(TextBox), CommandManager.PreviewCanExecuteEvent, new CanExecuteRoutedEventHandler(OnPreviewCanExecuteThunk));

            // Initialize the keyboard input rule for barcode scanners.
            _BarcodeScannerKeyboardInputRule = new BarcodeScannerKeyboardInputRule();
        }

        #region event handlers
        /// <summary>
        /// Handles the Loaded event for UI elements to start listening for barcode scanner input.
        /// </summary>
        private static void OnLoadedThunk(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }

            var inputMode = GetInputMode(element);
            if (inputMode != BarcodeScannerInputMode.None && element.IsLoaded)
            {
                ListenBarcodeScanner(element);
            }

            if (inputMode != BarcodeScannerInputMode.None)
            {
                element.Unloaded -= OnUnloadedThunk;
                element.Unloaded += OnUnloadedThunk;
                element.IsVisibleChanged -= Element_IsVisibleChanged;
                element.IsVisibleChanged += Element_IsVisibleChanged;
            }
        }

        /// <summary>
        /// Handles visibility changes for UI elements to stop listening for barcode scanner input when not visible.
        /// </summary>
        private static void Element_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }

            if (!element.IsLoaded)
            {
                element.IsVisibleChanged -= Element_IsVisibleChanged;
                TryStopListenBarcodeScanner(element);
            }
        }

        /// <summary>
        /// Handles the Unloaded event for UI elements to stop listening for barcode scanner input.
        /// </summary>
        private static void OnUnloadedThunk(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }
            TryStopListenBarcodeScanner(element);
        }

        /// <summary>
        /// Handles the PreviewCanExecute event to prevent command execution during barcode scanner input.
        /// </summary>
        private static void OnPreviewCanExecuteThunk(object sender, CanExecuteRoutedEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null)
            {
                return;
            }
            var inputMode = GetInputMode(element);
            if (inputMode != BarcodeScannerInputMode.None)
            {
                if (_isBarcodeScannerInput)
                {
                    e.CanExecute = false;
                    e.Handled = true;
                    e.ContinueRouting = true;
                }
            }
        }

        /// <summary>
        /// Handles the PreviewTextInput event to process text input from barcode scanners.
        /// </summary>
        private static void OnPreviewTextInputThunk(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null)
            {
                return;
            }

            var inputMode = GetInputMode(element);
            if (inputMode == BarcodeScannerInputMode.None)
                return;


            if ((inputMode & BarcodeScannerInputMode.BarcodeScannerInput) == BarcodeScannerInputMode.BarcodeScannerInput)
            {
                return;
            }

            if (_isBarcodeScannerInput)
            {
                _scanCode.Append(e.Text);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the PreviewKeyDown event to process key input from barcode scanners.
        /// </summary>
        private static void OnPreviewKeyDownThunk(object sender, KeyEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null)
            {
                return;
            }
            var inputMode = GetInputMode(element);
            if (inputMode == BarcodeScannerInputMode.None)
            {
                return;
            }

            if ((inputMode & BarcodeScannerInputMode.KeyboardInput) != BarcodeScannerInputMode.KeyboardInput)
            {
                if (!_isBarcodeScannerInput && !_unkownDeviceIsBarcodeScannerInput)
                {
                    e.Handled = true;
                    return;
                }
            }


            if ((inputMode & BarcodeScannerInputMode.BarcodeScannerInput) != BarcodeScannerInputMode.BarcodeScannerInput)
            {
                if (_isBarcodeScannerInput)
                {
                    if (InputMethod.GetIsInputMethodEnabled(element))
                        InputMethod.SetIsInputMethodEnabled(element, false);

                    if (e.Key == Key.Enter)
                    {
                        var scanCode = _scanCode.ToString();
                        _scanCode.Clear();
                        element.Dispatcher.BeginInvoke(() =>
                        {
                            var args = new ScanCodeEventArgs(scanCode);
                            OnScanCode(element, args);
                        });
                        e.Handled = true;
                    }
                }
                else
                {
                    if (!InputMethod.GetIsInputMethodEnabled(element))
                        InputMethod.SetIsInputMethodEnabled(element, true);
                }
            }

        }
        #endregion

        /// <summary>
        /// Adds a handler for the ScanCode event to a UI element.
        /// </summary>
        public static void AddScanCodeHandler(UIElement element, ScanCodeRoutedEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            element.AddHandler(ScanCodeEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the ScanCode event from a UI element.
        /// </summary>
        public static void RemoveScanCodeHandler(UIElement element, ScanCodeRoutedEventHandler handler)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            element.RemoveHandler(ScanCodeEvent, handler);
        }

        /// <summary>
        /// Raises the ScanCode event with the provided scan code.
        /// </summary>
        private static void OnScanCode(UIElement element, ScanCodeEventArgs eventArgs)
        {
            var hwndSource = HwndSource.FromVisual(element) as HwndSource;
            if (hwndSource == null)
            {
                return;
            }

            var root = hwndSource.RootVisual;
            if (root == null)
            {
                return;
            }

            var rawKeyboardInputProcessor = GetKeyboardInputProcessor(root);
            rawKeyboardInputProcessor.EventBridge.RaiseEvent(eventArgs);
        }

        /// <summary>
        /// Gets the input mode for a DependencyObject.
        /// </summary>
        public static BarcodeScannerInputMode GetInputMode(DependencyObject obj)
        {
            return (BarcodeScannerInputMode)obj.GetValue(InputModeProperty);
        }

        /// <summary>
        /// Sets the input mode for a DependencyObject.
        /// </summary>
        public static void SetInputMode(DependencyObject obj, BarcodeScannerInputMode value)
        {
            obj.SetValue(InputModeProperty, value);
        }

        public static readonly DependencyProperty InputModeProperty =
            DependencyProperty.RegisterAttached("InputMode", typeof(BarcodeScannerInputMode), typeof(BarcodeScannerManager), new PropertyMetadata(BarcodeScannerInputMode.None, OnInputModeChanged));

        /// <summary>
        /// Handles changes to the InputMode property to start or stop listening for barcode scanner input.
        /// </summary>
        private static void OnInputModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (!element.IsLoaded)
            {
                element.Loaded -= OnLoadedThunk;
                element.Loaded += OnLoadedThunk;
                return;
            }
            if (GetInputMode(d) != BarcodeScannerInputMode.None)
            {
                ListenBarcodeScanner((UIElement)d);
            }
            else
            {
                TryStopListenBarcodeScanner((UIElement)d);
            }
        }

        internal static int GetRegisterTimes(DependencyObject obj)
        {
            return (int)obj.GetValue(RegisterTimesProperty);
        }

        internal static void SetRegisterTimes(DependencyObject obj, int value)
        {
            obj.SetValue(RegisterTimesProperty, value);
        }

        internal static readonly DependencyProperty RegisterTimesProperty =
            DependencyProperty.RegisterAttached("RegisterTimes", typeof(int), typeof(BarcodeScannerManager), new PropertyMetadata(0));

        internal static RawKeyboardInputProcessor GetKeyboardInputProcessor(DependencyObject obj)
        {
            return (RawKeyboardInputProcessor)obj.GetValue(KeyboardInputProcessorProperty);
        }

        internal static void SetKeyboardInputProcessor(DependencyObject obj, RawKeyboardInputProcessor value)
        {
            obj.SetValue(KeyboardInputProcessorProperty, value);
        }

        internal static readonly DependencyProperty KeyboardInputProcessorProperty =
            DependencyProperty.RegisterAttached("KeyboardInputProcessor", typeof(RawKeyboardInputProcessor), typeof(BarcodeScannerManager), new PropertyMetadata(null));

        /// <summary>
        /// Starts listening for barcode scanner input on a UI element.
        /// </summary>
        private static void ListenBarcodeScanner(UIElement element)
        {
            var hwndSource = HwndSource.FromVisual(element) as HwndSource;
            if (hwndSource == null)
            {
                return;
            }

            var root = hwndSource.RootVisual;
            if (root == null)
            {
                throw new InvalidOperationException("RootVisual is null.");
            }

            var rawKeyboardInputProcessor = GetKeyboardInputProcessor(root);
            if (rawKeyboardInputProcessor == null)
            {
                rawKeyboardInputProcessor = RawKeyboardInputProcessor.GetRawKeyboardInputProcessor(hwndSource);
                SetKeyboardInputProcessor(root, rawKeyboardInputProcessor);
                rawKeyboardInputProcessor.RawInput += RawKeyboardInputProcessor_RawInput;
                rawKeyboardInputProcessor.RegisterKeyboardDevice(hwndSource);
            }
            rawKeyboardInputProcessor.EventBridge.AddListener(element);
            SetRegisterTimes(root, GetRegisterTimes(root) + 1);
        }
       
        /// <summary>
        /// Stops listening for barcode scanner input on a UI element.
        /// </summary>
        private static void TryStopListenBarcodeScanner(UIElement element)
        {
            var hwndSource = HwndSource.FromVisual(element) as HwndSource;
            if (hwndSource == null)
            {
                return;
            }

            var root = hwndSource.RootVisual;
            if (root == null)
            {
                throw new InvalidOperationException("RootVisual is null.");
            }

            var times = GetRegisterTimes(root);
            if (times > 1)
            {
                SetRegisterTimes(root, GetRegisterTimes(root) - 1);
                return;
            }

            var rawKeyboardInputProcessor = GetKeyboardInputProcessor(root);
            if (rawKeyboardInputProcessor != null)
            {
                rawKeyboardInputProcessor.RawInput -= RawKeyboardInputProcessor_RawInput;
                rawKeyboardInputProcessor.EventBridge.RemoveListener(element);
                rawKeyboardInputProcessor.UnregisterKeyboardDevice(hwndSource);
                SetKeyboardInputProcessor(root, null);
                SetRegisterTimes(root, 0);
            }
        }

        /// <summary>
        /// Stops listening for barcode scanner input on a specific HwndSource.
        /// </summary>
        public static void StopListenBarcodeScanner(HwndSource hwndSource)
        {
            if (hwndSource == null)
            {
                return;
            }
            var root = hwndSource.RootVisual;
            if (root == null)
            {
                throw new InvalidOperationException("RootVisual is null.");
            }

            var rawKeyboardInputProcessor = GetKeyboardInputProcessor(root);
            if (rawKeyboardInputProcessor != null)
            {
                rawKeyboardInputProcessor.RawInput -= RawKeyboardInputProcessor_RawInput;
                rawKeyboardInputProcessor.EventBridge.RemoveAllListener();
                rawKeyboardInputProcessor.UnregisterKeyboardDevice(hwndSource);
                SetKeyboardInputProcessor(root, null);
                SetRegisterTimes(root, 0);
            }
        }
       
        /// <summary>
        /// Adds a known keyboard device by its device path.
        /// </summary>
        public static void AddKeyboardKnownDevice(string devicePath)
        {
            if (!string.IsNullOrEmpty(devicePath)
                && devicePath.Contains("VID_")
                && devicePath.Contains("PID_"))
            {
                var deviceInfo = new DeviceInfo(devicePath);
                AddKnownDevice(deviceInfo);
            }
        }

        /// <summary>
        /// Adds a known barcode scanner device by its device path.
        /// </summary>
        public static void AddBarcodeScannerKnownDevice(string devicePath)
        {
            if (!string.IsNullOrEmpty(devicePath)
                && devicePath.Contains("VID_")
                && devicePath.Contains("PID_"))
            {
                var deviceInfo = new DeviceInfo(devicePath) { IsBarcodeScanner = true };
                AddKnownDevice(deviceInfo);
            }
        }

        /// <summary>
        /// Adds known devices to the list of managed devices.
        /// </summary>
        public static void AddKnownDevice(params DeviceInfo[] BarcodeScannerDevices)
        {
            _knownDevices.AddRange(BarcodeScannerDevices);
        }

        /// <summary>
        /// Removes known devices from the list of managed devices.
        /// </summary>
        public static void RemoveKnownDevice(params DeviceInfo[] BarcodeScannerDevices)
        {
            _knownDevices.RemoveAll(o => BarcodeScannerDevices.Any(p => o.PID == p.PID && o.VID == p.VID && o.MI == p.MI));
        }

        /// <summary>
        /// Checks if a device is a barcode scanner based on cached input states.
        /// </summary>
        private static bool CachedIsBarcodeScannerInput(RawInputHeader rawInputDataHeader)
        {
            bool state;
            if (_cacheDevices.TryGetValue(rawInputDataHeader, out state))
            {
                _isBarcodeScannerInput = state;
                _unkownDeviceIsBarcodeScannerInput = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a device is a barcode scanner based on known devices.
        /// </summary>
        private static bool KnownDeviceIsBarcodeScannerInput(RawInputHeader header)
        {
            RawInputDevice rawInputDevice = null;
            if (header.DeviceHandle != RawInputDeviceHandle.Zero)
            {
                rawInputDevice = RawInputDevice.FromHandle(header.DeviceHandle);
            }


            if (rawInputDevice != null
                && !string.IsNullOrEmpty(rawInputDevice.DevicePath)
                && rawInputDevice.DevicePath.Contains("VID_")
                && rawInputDevice.DevicePath.Contains("PID_"))
            {
                var deviceInfo = new DeviceInfo(rawInputDevice.DevicePath);
                foreach (var device in _knownDevices)
                {
                    if (device.VID == deviceInfo.VID && device.PID == deviceInfo.PID && device.MI == deviceInfo.MI)
                    {
                        _isBarcodeScannerInput = device.IsBarcodeScanner;
                        _unkownDeviceIsBarcodeScannerInput = false;
                        _cacheDevices.Add(header, _isBarcodeScannerInput); 
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determines if input is from a barcode scanner using keyboard input rules.
        /// </summary>
        private static void KeyboardInputCheckBarcodeScanner(RawInputHandle handle, RawInputHeader header)
        {
            var result = _BarcodeScannerKeyboardInputRule.Match(handle, header);
            if (result != null)
            {
                _isBarcodeScannerInput = result.GetValueOrDefault();
                _unkownDeviceIsBarcodeScannerInput = false;
                _cacheDevices.Add(header, _isBarcodeScannerInput); 
            }
            else
            {
                _unkownDeviceIsBarcodeScannerInput = true;
                _isBarcodeScannerInput = false;
            }
        }

        /// <summary>
        /// Processes raw input messages to determine if input is from a barcode scanner.
        /// </summary>
        private static void RawKeyboardInputProcessor_RawInput(object sender, RawInputMessageEventArgs e)
        {
            RawInputHandle handle = (RawInputHandle)e.LParam;
            RawInputHeader header = User32.GetRawInputDataHeader(handle);
            if (header.Type != RawInputDeviceType.Keyboard)
            {
                return;
            }

            if (CachedIsBarcodeScannerInput(header))
                return;

            if (KnownDeviceIsBarcodeScannerInput(header))
                return;

            KeyboardInputCheckBarcodeScanner(handle, header);
        }
    }
}
