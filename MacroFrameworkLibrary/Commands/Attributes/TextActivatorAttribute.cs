﻿using MacroFramework.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MacroFramework.Commands.Attributes {
    /// <summary>
    /// <see cref="Attribute"/> for easily creating a <see cref="TextActivator"/>. Attribute methods are parameterless, use <see cref="Commands.TextCommands.CurrentTextCommand"/> to get the current text command.
    /// </summary>
    public class TextActivatorAttribute : ActivatorAttribute {

        #region fields
        private string match;
        private MatchType type;

        /// <summary>
        /// Determines whether to use the matcher string as a regex or a string match
        /// </summary>
        public enum MatchType {
            /// <summary>
            /// Use string match
            /// </summary>
            StringMatch,
            /// <summary>
            /// Use regex match
            /// </summary>
            RegexPattern
        }
        #endregion

        /// <summary>
        /// Creates a new <see cref="TextActivator"/> instance at the start of the application from this method
        /// </summary>
        /// <param name="match">The exact string match or regex pattern</param>
        /// <param name="type"><see cref="MatchType"/></param>
        public TextActivatorAttribute(string match, MatchType type = MatchType.StringMatch) {
            this.match = match;
            this.type = type;
        }

        public override IActivator GetCommandActivator(Command command, MethodInfo assignedMethod) {
            if (type == MatchType.StringMatch) {
                return new TextActivator((s) => assignedMethod?.Invoke(command, null), match);
            } else {
                return new TextActivator((s) => assignedMethod?.Invoke(command, null), new RegexWrapper(new Regex(match)));
            }
        }
    }
}