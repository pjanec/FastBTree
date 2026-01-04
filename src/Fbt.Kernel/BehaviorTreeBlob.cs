using System;

namespace Fbt
{
    /// <summary>
    /// Compiled behavior tree asset (immutable, shared across entities).
    /// </summary>
    [Serializable]
    public class BehaviorTreeBlob
    {
        // ===== Metadata =====
        
        /// <summary>Name of this tree (e.g., "OrcCombat").</summary>
        public string TreeName;
        
        /// <summary>Version number for compatibility checking.</summary>
        public int Version = 1;
        
        /// <summary>
        /// Hash of node structure (types + hierarchy).
        /// Used for hot reload detection.
        /// </summary>
        public int StructureHash;
        
        /// <summary>
        /// Hash of parameters (floats, ints).
        /// Used for soft reload (parameter-only changes).
        /// </summary>
        public int ParamHash;
        
        // ===== Core Data =====
        
        /// <summary>
        /// The bytecode: flat array of nodes (depth-first order).
        /// </summary>
        public NodeDefinition[] Nodes;
        
        // ===== Lookup Tables =====
        
        /// <summary>
        /// Method names for Action/Condition nodes.
        /// PayloadIndex in NodeDefinition indexes into this.
        /// Example: ["Attack", "Patrol", "HasTarget"]
        /// </summary>
        public string[] MethodNames;
        
        /// <summary>
        /// Float parameters (e.g., Wait durations, ranges).
        /// Example: [2.0f, 5.0f, 10.0f]
        /// </summary>
        public float[] FloatParams;
        
        /// <summary>
        /// Integer parameters (e.g., repeat counts, thresholds).
        /// Example: [3, 10, 100]
        /// </summary>
        public int[] IntParams;
        
        /// <summary>
        /// Asset IDs for subtree references (if using runtime linking).
        /// Example: ["Patrol", "CombatTactics"]
        /// </summary>
        public string[] SubtreeAssetIds;
        
        // ===== Compiled Delegates (Optional) =====
        
        /// <summary>
        /// JIT-compiled delegate (if using JIT mode).
        /// Null in interpreter mode.
        /// </summary>
        [NonSerialized]
        public object CompiledDelegate; // Typed as object to avoid generic in blob
    }
}
