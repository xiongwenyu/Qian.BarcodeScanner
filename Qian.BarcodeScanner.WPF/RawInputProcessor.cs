using Linearstar.Windows.RawInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Interop;

namespace Qian.BarcodeScanner
{
    public class RawInputProcessor
    {
        public const int WM_INPUT = 255;

        protected readonly HwndSourceHook _hwndSourceHook;

        protected RawInputProcessor(IntPtr hwnd)
        {
            Hwnd = hwnd;
            _hwndSourceHook = new HwndSourceHook(WindowWndProc);
        }
        public IntPtr Hwnd { get; protected set; }

        protected virtual IntPtr WindowWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (RawInput != null && !handled && msg == WM_INPUT)
            {
                var eventArgs = new RawInputMessageEventArgs(hwnd, msg, wParam, lParam);
                RawInput.Invoke(this, eventArgs);
                handled = eventArgs.Handled;
                return eventArgs.Return;
            }
            return IntPtr.Zero;
        }
        public event EventHandler<RawInputMessageEventArgs> RawInput;
    }
    public class RawKeyboardInputProcessor : RawInputProcessor
    {

        private EventBridge eventBridge;
        private int _count = 0;
        protected static readonly Dictionary<IntPtr, WeakReference> _rawInputProcessors = new Dictionary<IntPtr, WeakReference>();
        private RawKeyboardInputProcessor(IntPtr hwnd) : base(hwnd)
        {
            eventBridge = new EventBridge();
        }

        internal EventBridge EventBridge { get { return eventBridge; } }

        public static RawKeyboardInputProcessor GetRawInputProcessor(IntPtr hwnd)
        {
            lock (((ICollection)_rawInputProcessors).SyncRoot)
            {
                WeakReference processor;
                _rawInputProcessors.TryGetValue(hwnd, out processor);
                return processor?.Target as RawKeyboardInputProcessor;
            }
        }
        public static RawKeyboardInputProcessor GetRawKeyboardInputProcessor(HwndSource hwndSource)
        {
            bool isNewCreated;
            return GetRawKeyboardInputProcessor(hwndSource, out isNewCreated);
        }

        public static RawKeyboardInputProcessor GetRawKeyboardInputProcessor(HwndSource hwndSource, out bool isNewCreated)
        {
            var hwnd = hwndSource.Handle;
            if (hwnd == IntPtr.Zero)
            {
                throw new ArgumentException("hwndSource.Handle is IntPtr.Zero");
            }

            lock (((ICollection)_rawInputProcessors).SyncRoot)
            {
                var keys = _rawInputProcessors.Keys.ToArray();
                foreach (var key in keys)
                {
                    if (!_rawInputProcessors[key].IsAlive)
                    {
                        _rawInputProcessors.Remove(key);
                        continue;
                    }

                    if (key == hwnd)
                    {
                        isNewCreated = false;
                        return (RawKeyboardInputProcessor)_rawInputProcessors[key].Target;
                    }
                }

                var processor = new RawKeyboardInputProcessor(hwnd);
                _rawInputProcessors.Add(hwnd, new WeakReference(processor));

                isNewCreated = true;
                return processor;
            }
        }

        public void RegisterKeyboardDevice(HwndSource hwndSource)
        {
            if (Interlocked.Increment(ref _count) > 1)
            {
                return;
            }
            if (hwndSource != null)
            {
                Hwnd = hwndSource.Handle;

                hwndSource.AddHook(_hwndSourceHook);
                RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard, RawInputDeviceFlags.None, hwndSource.Handle);
            }
        }

        public void UnregisterKeyboardDevice(HwndSource hwndSource)
        {
            if (Interlocked.Decrement(ref _count) <= 0 && hwndSource != null)
            {
                hwndSource.RemoveHook(_hwndSourceHook);
                RawInputDevice.UnregisterDevice(HidUsageAndPage.Keyboard);
            }
        }

    }
    public class RawInputMessageEventArgs : EventArgs
    {
        public RawInputMessageEventArgs(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            this.Hwnd = hwnd;
            this.Msg = msg;
            this.WParam = wParam;
            this.LParam = lParam;
        }
        public IntPtr Hwnd { get; private set; }
        public int Msg { get; private set; }
        public IntPtr WParam { get; private set; }
        public IntPtr LParam { get; private set; }
        public bool Handled { get; set; }
        public IntPtr Return { get; set; }
    }
}
