using System;
using System.Collections.Generic;

namespace Fbt.Runtime
{
    /// <summary>
    /// Registry for mapping method names to action delegates.
    /// </summary>
    public class ActionRegistry<TBlackboard, TContext>
        where TBlackboard : struct
        where TContext : struct, IAIContext
    {
        private readonly Dictionary<string, NodeLogicDelegate<TBlackboard, TContext>> _actions 
            = new Dictionary<string, NodeLogicDelegate<TBlackboard, TContext>>();

        /// <summary>
        /// Register an action delegate with a name.
        /// </summary>
        public void Register(string methodName, NodeLogicDelegate<TBlackboard, TContext> action)
        {
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentNullException(nameof(methodName));
            
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _actions[methodName] = action;
        }

        /// <summary>
        /// Try to retrieve an action delegate by name.
        /// </summary>
        public bool TryGetAction(string methodName, out NodeLogicDelegate<TBlackboard, TContext> action)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                action = null!;
                return false;
            }

            return _actions.TryGetValue(methodName, out action);
        }
    }
}
