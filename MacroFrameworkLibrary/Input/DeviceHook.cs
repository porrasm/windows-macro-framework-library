﻿using MacroFramework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MacroFramework.Input {
    /// <summary>
    /// Static class for receiving low level input from keyboard or mouse
    /// </summary>
    public static class DeviceHook {

        #region fields
        /// <summary>
        /// Delegate for receiving low level input
        /// </summary>
        public delegate IntPtr MessageProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Windows hook IDs
        /// </summary>
        private const int KEYBOARD_HOOK_ID = 13, MOUSE_HOOK_ID = 14;

        /// <summary>
        /// Returns true if the keyboard hook is running
        /// </summary>
        public static bool KeyboardHookRunning { get; private set; }

        /// <summary>
        /// Returns true if the mouse hook is running
        /// </summary>
        public static bool MouseHookRunning { get; private set; }

        /// <summary>
        /// Keyboard callback delegate
        /// </summary>
        private static MessageProc keyboardProc = KeyboardCallback;

        /// <summary>
        /// Mouse callback delegate
        /// </summary>
        private static MessageProc mouseProc = MouseCallback;

        private static IntPtr KeyboardHook, MouseHook = IntPtr.Zero;

        /// <summary>
        /// The <see cref="IntPtr"/> to return to intercept a keyevent
        /// </summary>
        private static IntPtr BlockCode => new IntPtr(-1);
        #endregion

        #region imports
        /// <summary>
        /// Initialize a low level hook
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, MessageProc lpfn, IntPtr hMod, int dwThreadId);

        /// <summary>
        /// Dispose of a low level hook
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// Call the next hook in a chain
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        #endregion

        #region managing
        /// <summary>
        /// Start hooking keyboard
        /// </summary>
        public static void StartKeyboardHook() {
            if (!KeyboardHookRunning) {
                KeyboardHook = SetHook(keyboardProc, KEYBOARD_HOOK_ID);
                KeyboardHookRunning = true;
            }
        }

        /// <summary>
        /// Stop hooking keyboard
        /// </summary>
        public static void StopKeyboardHook() {
            if (KeyboardHookRunning) {
                UnhookWindowsHookEx(KeyboardHook);
                KeyboardHookRunning = false;
            }
        }

        /// <summary>
        /// Start hooking mouse
        /// </summary>
        public static void StartMouseHook() {
            if (!MouseHookRunning) {
                MouseHook = SetHook(mouseProc, MOUSE_HOOK_ID);
                MouseHookRunning = true;
            }
        }
        /// <summary>
        /// Stop hooking mouse
        /// </summary>
        public static void StopMouseHook() {
            if (MouseHookRunning) {
                UnhookWindowsHookEx(MouseHook);
                MouseHookRunning = false;
            }
        }

        private static IntPtr SetHook(MessageProc proc, int hookID) {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule) {
                return SetWindowsHookEx(hookID, proc, curModule.BaseAddress, 0);
            }
        }
        #endregion

        #region callbacks
        private static IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0) {
                KBDLLHOOKSTRUCT raw = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                // Can received undefined keycode, in this case ignore
                if (raw.vkCode != (uint)VKey.UNDEFINED) {
                    KeyEvent k = new KeyEvent(wParam, raw);

                    if (KeyEvents.RegisterHookKeyEvent(k)) {
                        return BlockCode;
                    }
                }
            }

            return CallNextHookEx(KeyboardHook, nCode, wParam, lParam);
        }

        private static IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0) {
                //var raw = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                //var input = new MouseInput(wParam, raw);

                //if (InputEvent(input)) {
                //    return BlockCode;
                //}
            }

            return CallNextHookEx(MouseHook, nCode, wParam, lParam);
        }
        #endregion
    }
}
