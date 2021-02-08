﻿using MacroFramework.Commands;
using MacroFramework.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MacroFramework.Input {
    // Soon to be rewritten, code is smelly
    /// <summary>
    /// The static class handling all input key events.
    /// </summary>
    public static class KeyEvents {

        #region fields
        /// <summary>
        /// Returns the current KeyEvent. This can be used by Command classes to access every KeyEvent.
        /// </summary>
        public static KeyEvent CurrentKeyEvent { get; private set; }

        internal static bool CommandMode { get; private set; }

        internal static bool GeneralKeyBind { get; private set; }

        private static Queue<KeyEvent> keyEventQueue;

        private static HashSet<VKey> blockKeys;

        public delegate bool KeyCallbackFunc(VKey key, bool state);

        /// <summary>
        /// This delegate is invoked at every keypress, before it is registered by the <see cref="KeyState"/>. Return true to intercept key from other applications and the <see cref="MacroFramework"/> itself. This delegate is blocking and slow execution will cause OS wide latency for key events.
        /// </summary>
        public static KeyCallbackFunc KeyCallback { get; set; }
        #endregion

        static KeyEvents() {
            keyEventQueue = new Queue<KeyEvent>();
            blockKeys = new HashSet<VKey>();
        }

        /// <summary>
        /// Set the block status for a given key. If true is set, the key is blocked and other applications won't get the keyevent. The blocking of the key is not absolutely certain.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="status"></param>
        internal static void SetKeyBlockStatus(VKey key, bool status) {
            if (status) {
                blockKeys.Add(key);
            } else {
                blockKeys.Remove(key);
            }
        }

        #region hook
        /// <summary>
        /// Handles the key press event. When true is returned the key event is intercepted.
        /// </summary>
        internal static bool OnHookKeyPress(VKey key, bool state) {

            if (KeyCallback?.Invoke(key, state) ?? false) {
                return true;
            }

            if (KeyState.IsInvalidKey(key)) {
                return false;
            }
            if (key == Setup.Instance.Settings.GeneralBindKey) {
                key = VKey.GENERAL_BIND_KEY;
            }

            HandleQueuedKeyEventsNonBlocking();

            long timeSincePreviousEvent = KeyState.TimeSinceLastKeyPress();
            KeyState.AddAbsoluteEvent(key, state);
            KeyEvent k = new KeyEvent(key, state, KeyState.GetCurrentActivationEventType());

            #region check enabled
            //if (k.Key == Setup.Instance.Settings.ListenerEnableKey) {
            //    MacroSettings.KeyListenerEnabled = true;
            //    KeyState.ResetKeys();
            //    return true;
            //} else if (k.Key == Setup.Instance.Settings.ListenerDisableKey) {
            //    MacroSettings.KeyListenerEnabled = false;
            //    KeyState.ResetKeys();
            //    return true;
            //}
            //if (!MacroSettings.KeyListenerEnabled) {
            //    return false;
            //}
            #endregion

            keyEventQueue.Enqueue(k);

            #region blocking keys
            if (k.Key == VKey.GENERAL_BIND_KEY && Setup.Instance.Settings.InterceptGeneralBindKey) {
                GeneralKeyBind = k.KeyState;
                return true;
            }
            if (k.Key == Setup.Instance.Settings.CommandKey && !CommandMode) {
                if (k.KeyState) {
                    CommandKeyPress(true, true);
                }
                return true;
            }
            #endregion

            #region special modes
            // General bind mode
            if (GeneralKeyBind) {
                return true;
            }

            // Text command mode
            if (CommandMode) {
                if (timeSincePreviousEvent >= Setup.Instance.Settings.TextCommandTimeout) {
                    Console.WriteLine("End timeout: " + timeSincePreviousEvent);
                    CommandKeyPress(false, false);
                    return false;
                }
                return true;
            }
            #endregion

            return blockKeys.Contains(k.Key);
        }
        #endregion

        #region keyevent
        internal static async void HandleQueuedKeyEventsNonBlocking() {
            await Task.Delay(1);
            HandleQueuedKeyEvents();
        }
        internal static void HandleQueuedKeyEvents() {
            while (keyEventQueue.Count > 0) {
                OnKeyEvent(keyEventQueue.Dequeue());
            }
        }

        private static void OnKeyEvent(KeyEvent k) {

            bool unique = KeyState.IsUniqueEvent(k);

            if (CommandMode) {
                KeyState.AddKeyEvent(k);
                OnCommandMode(k, unique);
                CommandContainer.UpdateActivators(typeof(KeyActivator), typeof(BindActivator));
                return;
            }

            if (k.KeyState) {
                KeyState.AddKeyEvent(k);
            }

            CurrentKeyEvent = k;

            if (unique) {
                CommandContainer.UpdateActivators(typeof(KeyActivator), typeof(BindActivator));
            }

            if (!k.KeyState) {
                KeyState.AddKeyEvent(k);
            }
        }

        private static void OnCommandMode(KeyEvent k, bool isUniqueKeyPress) {

            if (k.Key == Setup.Instance.Settings.CommandKey) {
                return;
            }

            if (!k.KeyState) {
                return;
            }

            if (k.Key == Setup.Instance.Settings.CommandActivateKey) {
                CommandKeyPress(false, true);
                return;
            }

            if (VKeysToCommand.KeyToChar(k.Key) == '\0' && isUniqueKeyPress) {
                Console.WriteLine("End wrong key");
                CommandKeyPress(false, false);
                return;
            }

            TextCommandCreator.CommandKeyEvent(k.Key, k.KeyState, isUniqueKeyPress);
        }
        private static void CommandKeyPress(bool state, bool acceptCommand) {
            if (state && !CommandMode) {
                Console.WriteLine("\n Command mode start");
                TextCommandCreator.StartCommand();
            } else if (!state && CommandMode) {
                Console.WriteLine("\n Command mode end");
                TextCommandCreator.EndCommand(acceptCommand);
            }
            CommandMode = state;
        }
        #endregion
    }
}
