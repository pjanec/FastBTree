using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fbt.Serialization
{
    /// <summary>
    /// Represents the raw JSON structure of a behavior tree file.
    /// </summary>
    public class JsonTreeData
    {
        /// <summary>
        /// Name of the tree (e.g., "OrcCombat").
        /// </summary>
        public string TreeName { get; set; }

        /// <summary>
        /// Format version (default: 1).
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// The root node definition.
        /// </summary>
        public JsonNode Root { get; set; }
    }

    /// <summary>
    /// Represents a single node in the JSON hierarchy.
    /// Recursive structure.
    /// </summary>
    public class JsonNode
    {
        /// <summary>
        /// Node type name (e.g., "Sequence", "Action", "Wait").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Optional name for debugging/documentation.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Method name for Action/Condition nodes.
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// Alias for Action property (some tooling might use 'Method').
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Duration in seconds for Wait nodes.
        /// </summary>
        public float WaitTime { get; set; }

        /// <summary>
        /// Iteration count for Repeater nodes.
        /// </summary>
        public int RepeatCount { get; set; }

        /// <summary>
        /// Child nodes for composites/decorators.
        /// </summary>
        public JsonNode[] Children { get; set; }
        
        /// <summary>
        /// Flexible parameters dictionary for advanced nodes.
        /// </summary>
        public Dictionary<string, object> Params { get; set; }
    }
}
