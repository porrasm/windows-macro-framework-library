﻿using MacroFramework.Tools;
using System;
using System.Collections.Generic;

namespace MacroFramework.Commands {
    /// <summary>
    /// A static class for handling all active <see cref="Command"/> instances
    /// </summary>
    public static class CommandContainer {

        #region fields
        /// <summary>
        /// List of active commands. You should not modify this collection.
        /// </summary>
        public static List<Command> Commands { get; private set; }
        private static Dictionary<Type, List<IActivator>> staticActivators;
        private static Dictionary<Type, List<IDynamicActivator>> dynamicActivators;
        #endregion

        static CommandContainer() {
            Deinitialize();
        }

        private static void Initialize() {
            List<Command> setupCommands = Setup.Instance.GetActiveCommands();
            if (Setup.Instance.CommandAssembly != null && setupCommands == null) {
                foreach (Command c in ReflectiveEnumerator.GetEnumerableOfType<Command>(Setup.Instance.CommandAssembly)) {
                    AddCommand(c);
                }
            } else {
                foreach (Command c in setupCommands) {
                    AddCommand(c);
                }
            }
        }

        private static void Deinitialize() {
            Commands = new List<Command>();
            staticActivators = new Dictionary<Type, List<IActivator>>();
            dynamicActivators = new Dictionary<Type, List<IDynamicActivator>>();
        }

        /// <summary>
        /// Executes all activatos of certain type. This may call multiple activators from a single command instance.
        /// </summary>
        /// <param name="types">The list of types to update which implement <see cref="IActivator"/>"/></param>
        public static void UpdateActivators(params Type[] types) {
            if (types == null) {
                return;
            }
            foreach (Type t in types) {
                UpdateActivators(t);
            }
        }
        /// <summary>
        /// Executes all activatos of certain type. This may call multiple activators from a single command instance.
        /// </summary>
        /// <typeparam name="T">The type to update which implement <see cref="IActivator"/></typeparam>
        public static void UpdateActivators<T>() where T : IActivator {
            UpdateActivators(typeof(T));
        }

        private static void UpdateActivators(Type t) {
            if (Macros.Paused) {
                return;
            }

            if (!typeof(IActivator).IsAssignableFrom(t)) {
                throw new NotSupportedException("Invalid type argument given: " + t);
            }

            UpdateStaticActivators(t);
            UpdateDynamicActivators(t);
        }

        private static void UpdateStaticActivators(Type t) {
            if (!staticActivators.ContainsKey(t)) {
                return;
            }

            foreach (IActivator act in staticActivators[t]) {
                if (act.IsActive()) {
                    ExecuteActivator(act);
                }
            }
        }
        private static void ExecuteActivator(IActivator activator) {
            try {
                activator.Execute();
            } catch (Exception e) {
                Console.WriteLine($"Error on {activator.Owner?.GetType()} Execute: {e.Message}");
            }
        }


        private static void UpdateDynamicActivators(Type t) {
            if (!dynamicActivators.ContainsKey(t)) {
                return;
            }

            List<IDynamicActivator> acts = dynamicActivators[t];
            for (int i = 0; i < acts.Count; i++) {
                IDynamicActivator task = acts[i];

                if (task.IsCanceled) {
                    RemoveFromList(acts, ref i);
                    continue;
                }

                if (task.Activator.IsActive()) {
                    try {
                        task.Execute();
                    } catch (Exception e) {
                        Console.WriteLine($"Error on task finish, {task.Activator.Owner?.GetType()} Execute: {e.Message}");
                    }
                    if (task.RemoveAfterExecution()) {
                        RemoveFromList(acts, ref i);
                    }
                }
            }
        }
        private static void RemoveFromList<T>(List<T> list, ref int index) {
            list.RemoveAt(index);
            index--;
        }

        internal static void Exit() {
            foreach (Command c in Commands) {
                try {
                    c.OnClose();
                } catch (Exception e) {
                    Console.WriteLine($"Error on {c.GetType()} OnClose: {e.Message}");
                }
            }
            Deinitialize();
        }

        internal static void Start() {
            Initialize();
            foreach (Command c in Commands) {
                try {
                    c.OnStart();
                } catch (Exception e) {
                    Console.WriteLine($"Error on {c.GetType()} OnStart: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Adds a command to the active command pool
        /// </summary>
        /// <param name="c"></param>
        public static void AddCommand(Command c) {
            Commands.Add(c);
            AddActivators(c);
        }

        private static void AddActivators(Command c) {
            foreach (IActivator act in c.CommandActivators.Activators) {
                Type t = act.GetType();
                if (staticActivators.ContainsKey(t)) {
                    staticActivators[t].Add(act);
                } else {
                    staticActivators.Add(t, new List<IActivator>());
                    staticActivators[t].Add(act);
                }
            }
        }

        /// <summary>
        /// Can be used to add <see cref="IActivator"/> (wrapped inside <see cref="IDynamicActivator"/>) instances to the framework during runtime. Useful for e.g. events that should run only once.
        /// </summary>
        /// <param name="act">The dynamic activator to add</param>
        public static void AddDynamicActivator(IDynamicActivator act) {
            Type t = act.Activator.GetType();
            if (dynamicActivators.ContainsKey(t)) {
                dynamicActivators[t].Add(act);
            } else {
                dynamicActivators.Add(t, new List<IDynamicActivator>());
                dynamicActivators[t].Add(act);
            }
        }

        /// <summary>
        /// Clears the dynamic activators
        /// </summary>
        public static void ClearDynamicActivators() {
            dynamicActivators.Clear();
        }
    }
}
