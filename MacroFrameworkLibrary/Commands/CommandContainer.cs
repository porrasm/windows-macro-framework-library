﻿using MacroFramework.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroFramework.Commands {
    public static class CommandContainer {

        #region fields
        public static List<Command> Commands { get; private set; }
        private static Dictionary<Type, List<ICommandActivator>> TypeActivators { get; set; }

        private static Queue<QueueCallback> queueCallbacks;

        public delegate void QueueCallback();
        #endregion

        static CommandContainer() {
            Deinitialize();
        }

        private static void Initialize() {
            List<Command> setupCommands = Setup.Instance.GetActiveCommands();
            if (Setup.Instance.MainAssembly != null && setupCommands == null) {
                foreach (Command c in ReflectiveEnumerator.GetEnumerableOfType<Command>(Setup.Instance.MainAssembly)) {
                    AddCommand(c);
                }
            } else {
                foreach (Command c in setupCommands) {
                    AddCommand(c);
                }
            }

            queueCallbacks = new Queue<QueueCallback>();
        }

        private static void Deinitialize() {
            Commands = new List<Command>();
            TypeActivators = new Dictionary<Type, List<ICommandActivator>>();
            queueCallbacks = null;
        }

        /// <summary>
        /// Executes all commands and binds which are active. Calling without care may cause duplicate activations.
        /// </summary>
        public static void UpdateAllCommandss() {
            foreach (Command c in Commands) {
                try {
                    c.ExecuteIfActive();
                } catch (Exception e) {
                    Console.WriteLine("Error executing command of type " + c.GetType() + ": " + e.Message);
                }
            }
        }
        /// <summary>
        /// Executes all activatos of certain type. This may call multiple activators from a single command instance
        /// </summary>
        /// <typeparam name="T">The type of activators which are checked</typeparam>
        /// <param name="executeCount">The count of activators to be executed. Set as 0 to execute every active activator</param>
        public static void UpdateActivators<T>() where T : ICommandActivator {
            Type t = typeof(T);
            if (!TypeActivators.ContainsKey(t)) {
                return;
            }

            foreach (ICommandActivator act in TypeActivators[t]) {
                if (act.IsActive()) {
                    try {
                        act.Owner?.OnExecuteStart();
                        act.Execute();
                        act.Owner?.OnExecutionComplete();
                    } catch (Exception e) {
                        Console.WriteLine("Error executing command of type " + act.Owner.GetType() + ": " + e.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Calls the <see cref="Command.OnTextCommand(string, bool)"/> on every <see cref="Command"/> instance
        /// </summary>
        /// <param name="command"></param>
        /// <param name="commandWasAccepted"></param>
        internal static void CallOnTextCommand(string command, bool commandWasAccepted) {
            foreach (Command c in Commands) {
                c.OnTextCommand(command, commandWasAccepted);
            }
            TextCommandCreator.CallRemainingCommands(false);
            TextCommandCreator.Clear();
        }

        public static void OnClose() {
            foreach (Command c in Commands) {
                c.OnClose();
            }
        }

        /// <summary>
        /// Queues a job which will be executed on the primary thread. You queue a job from a secondary thread.
        /// </summary>
        /// <param name="cb"></param>
        public static void EnqueuePrimaryThreadJob(QueueCallback cb) {
            queueCallbacks.Enqueue(cb);
        }


        public static void ExecuteSecondaryThreadJobs() {
            while (queueCallbacks.Count > 0) {
                queueCallbacks.Dequeue()?.Invoke();
            }
        }

        internal static void Start() {
            Initialize();
            foreach (Command c in Commands) {
                c.OnStart();
            }
        }
        internal static void Exit() {
            foreach (Command c in Commands) {
                c.OnClose();
            }
            Deinitialize();
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
            foreach (ICommandActivator act in c.Activator.Activators) {
                Type t = act.GetType();
                if (TypeActivators.ContainsKey(t)) {
                    TypeActivators[t].Add(act);
                } else {
                    TypeActivators.Add(t, new List<ICommandActivator>());
                    TypeActivators[t].Add(act);
                }
            }
        }
    }
}
