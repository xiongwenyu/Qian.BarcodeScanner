using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Qian.BarcodeScanner
{
    /// <summary>
    /// Represents a barcode scanner device and manages its interaction with WPF applications.
    /// Handles input from barcode scanners and integrates it with the WPF input system.
    /// </summary>
    public class BarcodeScannerDevice : InputDevice
    {
        private bool isRunning = false;
        private HwndSource hwndSource;

        private static WeakReference _focus;

        // Flags to track input source type.
        private static bool _unkownDeviceIsBarcodeScannerInput;
        private static bool _isBarcodeScannerInput;

        // List of known devices (keyboard or barcode scanner).
        private static readonly List<DeviceInfo> _knownDevices = new List<DeviceInfo>();
        // Cache for device input states to improve performance.
        private static readonly Dictionary<RawInputHeader, bool> _cacheDevices = new Dictionary<RawInputHeader, bool>();
        // Rule to determine if input is from a barcode scanner.
        private static BarcodeScannerKeyboardInputRule _BarcodeScannerKeyboardInputRule;

        // StringBuilder to accumulate scan codes from barcode scanner input.
        private static StringBuilder _scanCode = new StringBuilder();

        static BarcodeScannerDevice()
        {
            // Register event handlers for UIElement controls to handle input events.
            EventManager.RegisterClassHandler(typeof(UIElement), UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDownThunk));
            EventManager.RegisterClassHandler(typeof(UIElement), UIElement.PreviewTextInputEvent, new TextCompositionEventHandler(OnPreviewTextInputThunk));
            EventManager.RegisterClassHandler(typeof(UIElement), CommandManager.PreviewCanExecuteEvent, new CanExecuteRoutedEventHandler(OnPreviewCanExecuteThunk));
            EventManager.RegisterClassHandler(typeof(UIElement), Keyboard.PreviewGotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnPreviewGotKeyboardFocusThunk));

            // Initialize the keyboard input rule for barcode scanners.
            _BarcodeScannerKeyboardInputRule = new BarcodeScannerKeyboardInputRule();
        }

        /// <summary>
        /// Initializes a new instance of the BarcodeScannerDevice class for the specified FrameworkElement.
        /// </summary>
        public BarcodeScannerDevice(FrameworkElement element)
        {
            if (element.IsLoaded)
            {
                Init(element);
            }
            else
            {
                element.Loaded += OnLoadedThunk;
            }
        }

        /// <summary>
        /// Handles the Loaded event for the FrameworkElement to initialize the barcode scanner device.
        /// </summary>
        private void OnLoadedThunk(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            Init(element);
        }
        private static bool IsOnlyKeyboardInput(UIElement element)
        {
            var inputMode = BarcodeScannerManager.GetInputMode(element);
            return (inputMode & BarcodeScannerInputMode.KeyboardInput) == BarcodeScannerInputMode.KeyboardInput
                  &&
              (inputMode & BarcodeScannerInputMode.BarcodeScannerInput) != BarcodeScannerInputMode.BarcodeScannerInput;
        }

        private static bool IsOnlyBarcodeScannerInput(UIElement element)
        {
            var inputMode = BarcodeScannerManager.GetInputMode(element);
            return (inputMode & BarcodeScannerInputMode.BarcodeScannerInput) == BarcodeScannerInputMode.BarcodeScannerInput
                  &&
              (inputMode & BarcodeScannerInputMode.KeyboardInput) != BarcodeScannerInputMode.KeyboardInput;
        }

        /// <summary>
        /// Initializes the barcode scanner device for the specified FrameworkElement.
        /// </summary>
        private void Init(FrameworkElement element)
        {
            if (element == null)
            {
                return;
            }

            var inputMode = BarcodeScannerManager.GetInputMode(element);
            if (inputMode != BarcodeScannerInputMode.None && element.IsLoaded)
            {
                hwndSource = HwndSource.FromVisual(element) as HwndSource;
                if (hwndSource != null)
                {
                    bool isNewCreated;
                    var rawKeyboardInputProcessor = RawKeyboardInputProcessor.GetRawKeyboardInputProcessor(hwndSource, out isNewCreated);
                    if (isNewCreated)
                    {
                        rawKeyboardInputProcessor.RawInput += RawKeyboardInputProcessor_RawInput;
                    }

                    RegisterKeyboardDevice(element);

                    element.Unloaded += Element_Unloaded;
                    element.IsVisibleChanged += Element_IsVisibleChanged;
                }
            }
        }

        /// <summary>
        /// Handles visibility changes for the FrameworkElement to register or unregister the keyboard device.
        /// </summary>
        private void Element_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null)
            {
                if (element.IsVisible)
                {
                    RegisterKeyboardDevice(element);
                }
                else
                {
                    UnregisterKeyboardDevice(element);
                }
            }
        }

        /// <summary>
        /// Handles the Unloaded event for the FrameworkElement to unregister the keyboard device.
        /// </summary>
        private void Element_Unloaded(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null)
            {
                UnregisterKeyboardDevice(element);
            }
        }

        /// <summary>
        /// Registers the keyboard device for the specified FrameworkElement.
        /// </summary>
        private void RegisterKeyboardDevice(FrameworkElement element)
        {
            if (element == null || isRunning)
                return;

            if (hwndSource != null)
            {
                var rawKeyboardInputProcessor = RawKeyboardInputProcessor.GetRawInputProcessor(hwndSource.Handle) as RawKeyboardInputProcessor;
                if (rawKeyboardInputProcessor != null)
                {
                    rawKeyboardInputProcessor.RegisterKeyboardDevice(hwndSource);
                    isRunning = true;
                }
            }
        }

        /// <summary>
        /// Unregisters the keyboard device for the specified FrameworkElement.
        /// </summary>
        private void UnregisterKeyboardDevice(FrameworkElement element)
        {
            if (element == null || !isRunning)
                return;


            if (hwndSource != null)
            {
                var rawKeyboardInputProcessor = RawKeyboardInputProcessor.GetRawInputProcessor(hwndSource.Handle) as RawKeyboardInputProcessor;
                if (rawKeyboardInputProcessor != null)
                {
                    rawKeyboardInputProcessor.UnregisterKeyboardDevice(hwndSource);
                }
                isRunning = false;
                hwndSource = null;
            }
        }

        /// <summary>
        /// Gets the currently focused input element.
        /// </summary>
        private static IInputElement GetFocusElement()
        {
            if (_focus == null)
                return null;

            return _focus.Target as IInputElement;
        }
        private static void OnPreviewGotKeyboardFocusThunk(object sender, KeyboardFocusChangedEventArgs e)
        {
            Focus(e.NewFocus);
        }

        /// <summary>
        /// Handles the PreviewCanExecute event to determine if a command can be executed.
        /// </summary>
        private static void OnPreviewCanExecuteThunk(object sender, CanExecuteRoutedEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null)
            {
                return;
            }
            var inputMode = BarcodeScannerManager.GetInputMode(element);
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
        /// Handles the PreviewTextInput event to process text input from the barcode scanner.
        /// </summary>
        private static void OnPreviewTextInputThunk(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null)
            {
                return;
            }

            if (IsOnlyKeyboardInput(element))
            {
                if (_isBarcodeScannerInput)
                {
                    _scanCode.Append(e.Text);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Handles the PreviewKeyDown event to process key input from the barcode scanner.
        /// </summary>
        private static void OnPreviewKeyDownThunk(object sender, KeyEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null)
            {
                return;
            }
            if (IsOnlyKeyboardInput(element))
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
                            var element = GetFocusElement() as UIElement;
                            if (element != null)
                            {
                                var args = new ScanCodeEventArgs(scanCode);
                                element.RaiseEvent(args);
                            }
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
            else if (IsOnlyBarcodeScannerInput(element))
            {
                if (!_isBarcodeScannerInput && !_unkownDeviceIsBarcodeScannerInput)
                {
                    e.Handled = true;
                }
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// Checks if the input is from a cached barcode scanner device.
        /// </summary>
        private bool CachedIsBarcodeScannerInput(RawInputHeader rawInputDataHeader)
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
        /// Checks if the input is from a known barcode scanner device.
        /// </summary>
        private bool KnownDeviceIsBarcodeScannerInput(RawInputHeader header)
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
        private void KeyboardInputCheckBarcodeScanner(RawInputHandle handle, RawInputHeader header)
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
        /// Handles raw input messages to determine if input is from a barcode scanner.
        /// </summary>
        private void RawKeyboardInputProcessor_RawInput(object sender, RawInputMessageEventArgs e)
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
        /// <summary>
        /// Sets the focus to the specified input element.
        /// </summary>
        public static IInputElement Focus(IInputElement element)
        {
            DependencyObject dependencyObject = element as DependencyObject;
            if (dependencyObject != null)
            {
                var inputMode = BarcodeScannerManager.GetInputMode(dependencyObject);
                if ((inputMode & BarcodeScannerInputMode.BarcodeScannerInput) == BarcodeScannerInputMode.BarcodeScannerInput)
                {
                    _focus = new WeakReference(element);
                }
            }

            return _focus?.Target as IInputElement;
        }

        public override PresentationSource ActiveSource
        {
            get
            {
                return InputManager.Current?.PrimaryKeyboardDevice?.ActiveSource;
            }
        }

        public override IInputElement Target
        {
            get
            {
                return _focus?.Target as IInputElement;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the input is from a barcode scanner.
        /// </summary>
        public bool IsBarcodeScannerInput
        {
            get
            {
                return _isBarcodeScannerInput;
            }
        }
    }
}
