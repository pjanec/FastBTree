using System;
using System.Collections.Generic;

namespace Fbt.Serialization
{
    /// <summary>
    /// Intermediate representation of a node during compilation.
    /// Used to calculate subtree sizes and flatten the tree.
    /// </summary>
    public class BuilderNode
    {
        public NodeType Type { get; set; }
        public string MethodName { get; set; }
        public float WaitTime { get; set; }
        public int RepeatCount { get; set; }
        public List<BuilderNode> Children { get; } = new List<BuilderNode>();
        
        public BuilderNode(JsonNode jsonNode)
        {
            if (jsonNode == null) throw new ArgumentNullException(nameof(jsonNode));
            
            // Map string Type to NodeType enum
            Type = MapNodeType(jsonNode.Type);
            
            // Extract node-specific data
            // Support both 'Action' and 'Method' properties for flexibility
            if (Type == NodeType.Action || Type == NodeType.Condition)
            {
                MethodName = !string.IsNullOrEmpty(jsonNode.Action) ? jsonNode.Action : jsonNode.Method;
            }
            else if (Type == NodeType.Wait)
            {
                WaitTime = jsonNode.WaitTime;
                
                // Fallback to params if specialized property not set (or for consistency)
                if (WaitTime == 0 && jsonNode.Params != null && jsonNode.Params.TryGetValue("duration", out var val))
                {
                    WaitTime = Convert.ToSingle(val);
                }
            }
            else if (Type == NodeType.Repeater)
            {
                 RepeatCount = jsonNode.RepeatCount;
                 if (RepeatCount == 0 && jsonNode.Params != null && jsonNode.Params.TryGetValue("count", out var val))
                 {
                     RepeatCount = Convert.ToInt32(val);
                 }
            }
            
            // Recursively build children
            if (jsonNode.Children != null)
            {
                foreach (var child in jsonNode.Children)
                    Children.Add(new BuilderNode(child));
            }
        }
        
        public int CalculateSubtreeSize()
        {
            int size = 1; // Self
            foreach (var child in Children)
                size += child.CalculateSubtreeSize();
            return size;
        }
        
        private static NodeType MapNodeType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) throw new ArgumentException("Node Type cannot be empty");

            return typeName.ToLowerInvariant() switch
            {
                "root" => NodeType.Root,
                "sequence" => NodeType.Sequence,
                "selector" => NodeType.Selector,
                "fallback" => NodeType.Selector,
                "parallel" => NodeType.Parallel,
                "action" => NodeType.Action,
                "condition" => NodeType.Condition,
                "wait" => NodeType.Wait,
                "inverter" => NodeType.Inverter,
                "repeater" => NodeType.Repeater,
                "cooldown" => NodeType.Cooldown,
                "forcesuccess" => NodeType.ForceSuccess,
                "forcefailure" => NodeType.ForceFailure,
                "untilsuccess" => NodeType.UntilSuccess,
                "untilfailure" => NodeType.UntilFailure,
                "service" => NodeType.Service,
                "observer" => NodeType.Observer,
                "subtree" => NodeType.Subtree,
                _ => throw new ArgumentException($"Unknown node type: {typeName}")
            };
        }
    }
}
