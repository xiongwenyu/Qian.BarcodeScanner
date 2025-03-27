using System;
using System.Collections.Generic;
using System.Windows;

namespace Qian.BarcodeScanner
{
    /// <summary>
    /// Provides a mechanism to manage and raise events for multiple UIElement targets.
    /// Ensures that events are dispatched to the correct UI thread and handles weak references to prevent memory leaks.
    /// </summary>
    internal class EventBridge
    {
        // List of weak references to UIElement targets.
        private readonly List<WeakReference<UIElement>> targets = new();
        // Synchronization object for thread safety.
        private readonly object asyncState = new();

        /// <summary>
        /// Adds a UIElement as a listener for events.
        /// </summary>
        /// <param name="target">The UIElement to add as a listener.</param>
        internal void AddListener(UIElement target)
        {
            lock (asyncState)
            {
                // Remove any invalid or duplicate listeners before adding the new one.
                RemoveListener(null);
                targets.Add(new WeakReference<UIElement>(target));
            }
        }

        /// <summary>
        /// Removes a UIElement from the list of event listeners.
        /// </summary>
        /// <param name="target">The UIElement to remove. If null, removes invalid references.</param>
        internal void RemoveListener(UIElement target)
        {
            lock (asyncState)
            {
                // Remove all invalid or matching listeners.
                targets.RemoveAll(wr => !wr.TryGetTarget(out var t) || t == target);
            }
        }

        /// <summary>
        /// Raises the specified RoutedEventArgs on all registered UIElement targets.
        /// </summary>
        /// <param name="e">The RoutedEventArgs to raise.</param>
        internal void RaiseEvent(RoutedEventArgs e)
        {
            lock (asyncState)
            {
                foreach (var wr in targets)
                {
                    if (wr.TryGetTarget(out var target) && target != null)
                    {
                        RaiseEventOnTarget(target, e);
                        // Stop propagation if the event is handled.
                        if (e.Handled)
                        {
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Raises the specified RoutedEventArgs on a specific UIElement target.
        /// Ensures the event is dispatched to the correct UI thread.
        /// </summary>
        /// <param name="target">The UIElement to raise the event on.</param>
        /// <param name="e">The RoutedEventArgs to raise.</param>
        private void RaiseEventOnTarget(UIElement target, RoutedEventArgs e)
        {
            var dispatcher = target.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            // Dispatch the event to the correct thread if necessary.
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => RaiseEventOnTarget(target, e));
                return;
            }

            // Skip raising the event if the target is not visible.
            if (!target.IsVisible)
                return;

            // Set the event source and raise the event.
            e.Source = target;
            target.RaiseEvent(e);
            e.Source = e.OriginalSource;
        }

        /// <summary>
        /// Removes all registered listeners.
        /// </summary>
        internal void RemoveAllListener()
        {
            lock (asyncState)
            {
                targets.Clear();
            }
        }
    }
}
