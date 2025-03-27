using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;
using System.Collections.Generic;
using System.Diagnostics;

namespace Qian.BarcodeScanner
{
    /// <summary>
    /// Defines rules to determine whether input is from a barcode scanner or a keyboard.
    /// Uses timing and key press patterns to differentiate between the two input sources.
    /// </summary>
    internal class BarcodeScannerKeyboardInputRule
    {
        public const int EnterKey = 13; // Virtual key code for the Enter key.
        private int _pressedVirutalKey = 0; // Tracks the currently pressed virtual key.
        private bool _isKeyPressed = false; // Indicates whether a key is currently pressed.
        private Stopwatch _keySpan = new Stopwatch(); // Measures the time between key presses.
        private Dictionary<RawInputHeader, KeyboardState> _keyboards = new Dictionary<RawInputHeader, KeyboardState>(); // Tracks the state of keyboards.

        /// <summary>
        /// Initializes a new instance of the BarcodeScannerKeyboardInputRule class with default settings.
        /// </summary>
        public BarcodeScannerKeyboardInputRule()
        {
            KeyInterval = 50; // Default interval (in milliseconds) to distinguish between inputs.
            KeyFaultCount = 3; // Default fault tolerance for determining input type.
        }

        /// <summary>
        /// Gets or sets the maximum interval (in milliseconds) between key presses to classify input as barcode scanner input.
        /// </summary>
        public int KeyInterval { get; set; }

        /// <summary>
        /// Gets or sets the fault tolerance for determining input type.
        /// </summary>
        private int KeyFaultCount { get; set; }

        /// <summary>
        /// Matches the input against the rules to determine if it is from a barcode scanner or a keyboard.
        /// </summary>
        /// <param name="handle">The raw input handle.</param>
        /// <param name="header">The raw input header.</param>
        /// <returns>
        /// True if the input is from a barcode scanner, false if it is from a keyboard, or null if undetermined.
        /// </returns>
        public bool? Match(RawInputHandle handle, RawInputHeader header)
        {
            KeyboardState keyboardState;

            if (!_keyboards.TryGetValue(header, out keyboardState) || keyboardState == null)
            {
                keyboardState = new KeyboardState(KeyFaultCount);
                _keyboards[header] = keyboardState;
            }

            RawInputHeader rawInputHeader;
            var rawKeyboard = User32.GetRawInputKeyboardData(handle, out rawInputHeader);
            var keyboardData = new RawInputKeyboardData(header, rawKeyboard);

            // Ignore Shift key input.
            if (keyboardData.Keyboard.VirutalKey == 0x10) 
            {
                return null;
            }

            // Handle key press events.
            if ((keyboardData.Keyboard.Flags & RawKeyboardFlags.Up) == RawKeyboardFlags.None)
            {
                if (_isKeyPressed)
                {
                    return Keyboard(header);
                }

                _pressedVirutalKey = keyboardData.Keyboard.VirutalKey;
                _isKeyPressed = true;
                return null;
            }

            // Handle key release events.
            if ((keyboardData.Keyboard.Flags & RawKeyboardFlags.Up) == RawKeyboardFlags.Up)
            {
                _keySpan.Stop();
                var elapsed = _keySpan.ElapsedMilliseconds;
                _keySpan.Start();

                if (_pressedVirutalKey != keyboardData.Keyboard.VirutalKey
                    || _pressedVirutalKey == 0
                    || !_isKeyPressed)
                {
                    return Keyboard(header);
                }
                if (keyboardData.Keyboard.VirutalKey != EnterKey)
                {
                    if (elapsed > KeyInterval)
                    {
                        keyboardState.SetKeyboardState();
                    }
                    else
                    {
                        keyboardState.SetBarcodeScannerState();
                    }

                    if (keyboardState.IsBarcodeScannerInput == true)
                    {
                        return BarcodeScanner(header);
                    }
                    else if (keyboardState.IsBarcodeScannerInput == false)
                    {
                        return Keyboard(header);
                    }
                    else
                    {
                        _isKeyPressed = false;
                        return null;
                    }
                }
                else
                {
                    if (keyboardState.Times > 0)
                    {
                        keyboardState.SetBarcodeScannerState();

                        if (keyboardState.IsBarcodeScannerInput == true)
                        {
                            return BarcodeScanner(header);
                        }
                        else if (keyboardState.IsBarcodeScannerInput == false)
                        {
                            return Keyboard(header);
                        }
                        else
                        {
                            _isKeyPressed = false;
                            return null;
                        }
                    }
                    else
                    {
                        return Keyboard(header);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Resets the state and classifies the input as keyboard input.
        /// </summary>
        private bool Keyboard(RawInputHeader header)
        {
            _isKeyPressed = false;
            _pressedVirutalKey = 0;
            _keyboards.Remove(header);
            return false;
        }

        /// <summary>
        /// Resets the state and classifies the input as barcode scanner input.
        /// </summary>
        private bool BarcodeScanner(RawInputHeader header)
        {
            _isKeyPressed = false;
            _pressedVirutalKey = 0;
            _keyboards.Remove(header);
            return true;
        }

        /// <summary>
        /// Represents the state of a keyboard for determining input type.
        /// </summary>
        private class KeyboardState
        {
            private int times = 0; // Total number of key events processed.
            private int counter = 0; // Counter to track barcode scanner vs keyboard input.
            private int keyFaultCount; // Fault tolerance for determining input type.

            public KeyboardState(int keyFaultCount)
            {
                this.keyFaultCount = keyFaultCount;
            }

            /// <summary>
            /// Marks the input as barcode scanner input.
            /// </summary>
            public void SetBarcodeScannerState()
            {
                counter++;
                times++;
            }

            /// <summary>
            /// Marks the input as keyboard input.
            /// </summary>
            public void SetKeyboardState()
            {
                counter--;
                times++;
            }

            /// <summary>
            /// Gets the total number of key events processed.
            /// </summary>
            public int Times
            {
                get
                {
                    return times;
                }
            }

            /// <summary>
            /// Determines if the input is from a barcode scanner.
            /// Returns true for barcode scanner input, false for keyboard input, or null if undetermined.
            /// </summary>
            public bool? IsBarcodeScannerInput
            {
                get
                {
                    if (counter >= keyFaultCount)
                    {
                        return true;
                    }
                    else if (counter <= -keyFaultCount)
                    {
                        return false;
                    }
                    return null;
                }
            }
        }
    }
}
