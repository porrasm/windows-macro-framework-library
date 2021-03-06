﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MacroFramework.Commands {
    /// <summary>
    /// Base class for all macro functionality
    /// </summary>
    public abstract class Command {

        #region fields
        /// <summary>
        /// Callback for command actions
        /// </summary>
        public delegate void CommandCallback();

        /// <summary>
        /// Container for the set of <see cref="CommandActivator"/> instances of this command
        /// </summary>
        protected CommandActivatorGroup commandActivators;
        internal CommandActivatorGroup CommandActivators => commandActivators;

        /// <summary>
        /// The deleget bool used to determine whether a <see cref="Command"/> instance is active
        /// </summary>
        /// <returns></returns>
        public delegate bool CommandContext();

        /// <summary>
        /// The default context used in all <see cref="Command"/> instances. Returns true on default but can be changed.
        /// </summary>
        public static CommandContext DefaultContext = () => true;
        #endregion

        #region initialization
        /// <summary>
        /// Creates a new <see cref="Command"/> instance
        /// </summary>
        public Command() {
            InitializeActivators(out commandActivators);
            InitializeAttributeActivators();
        }

        private void InitializeAttributeActivators() {
            try {
                MethodInfoAttributeCont[] methods = GetAttributeMethods();
                foreach (MethodInfoAttributeCont cont in methods) {
                    commandActivators.AddActivator(cont.Attribute.GetCommandActivator(this, cont.Method));
                }
            } catch (Exception e) {
                throw new Exception("Unable to load Attributes from Assembly on type " + GetType(), e);
            }
        }

        private MethodInfoAttributeCont[] GetAttributeMethods() {
            return GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttributes(typeof(ActivatorAttribute), false).Length > 0)
                .Select(m => new MethodInfoAttributeCont(m, m.GetCustomAttribute<ActivatorAttribute>())).ToArray();
        }


        /// <summary>
        /// Abstract method for initializing <see cref="Commands.IActivator"/> and class functionality. Use this like you would use a constructor. CommandActivators array mustn't be null and has to have at least 1 activator.
        /// </summary>
        protected virtual void InitializeActivators(out CommandActivatorGroup activator) {
            activator = new CommandActivatorGroup(this);
        }
        #endregion
        /// <summary>
        /// Override this method to create custom contexts for your command. If false is returned, none of the activators in <see cref="CommandActivators"/> are active eiher and this <see cref="Command"/> instance is effectively disabled for the moment.
        /// </summary>
        /// <returns></returns>
        public CommandContext IsActive = () => true;

        /// <summary>
        /// Called before the execution of any <see cref="IActivator"/> starts
        /// </summary>
        protected internal virtual void OnExecuteStart() { }

        /// <summary>
        /// Called after the execution of every <see cref="IActivator"/>
        /// </summary>
        protected internal virtual void OnExecutionComplete() { }

        /// <summary>
        /// Called after <see cref="Macros.Start(Setup)"/>
        /// </summary>
        public virtual void OnStart() { }

        /// <summary>
        /// Called after <see cref="Macros.Stop"/>
        /// </summary>
        public virtual void OnClose() { }

        /// <summary>
        /// This method is called whenever a text command is executed
        /// </summary>
        /// <param name="command">The text command which was executed</param>
        /// <param name="commandWasAccepted">True if any <see cref="TextActivator"/> instance executed the text command. False if command was not executed.</param>
        public virtual void OnTextCommand(string command, bool commandWasAccepted) { }

        /// <summary>
        /// Nicer syntax for creating array of <see cref="VKey"/> elements
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        protected KKey[] Keys(params KKey[] keys) {
            if (keys == null || keys.Length == 0) {
                throw new Exception("You need to add 1 or more keys.");
            }
            return keys;
        }
    }
}
