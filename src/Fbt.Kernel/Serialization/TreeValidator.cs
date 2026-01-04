using System.Collections.Generic;

namespace Fbt.Serialization
{
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new List<string>();
    }

    public static class TreeValidator
    {
        public static ValidationResult Validate(BehaviorTreeBlob blob)
        {
            var result = new ValidationResult();
            
            if (blob.Nodes == null || blob.Nodes.Length == 0)
            {
                result.Errors.Add("Tree has no nodes");
                return result;
            }
            
            for (int i = 0; i < blob.Nodes.Length; i++)
            {
                var node = blob.Nodes[i];
                
                // Validate subtree offset
                if (node.SubtreeOffset == 0)
                {
                    result.Errors.Add($"Node {i}: SubtreeOffset is zero");
                }
                else if (i + node.SubtreeOffset > blob.Nodes.Length)
                {
                    result.Errors.Add($"Node {i}: SubtreeOffset {node.SubtreeOffset} exceeds tree bounds");
                }
                
                // Validate payload index
                if (node.Type == NodeType.Action || node.Type == NodeType.Condition)
                {
                    if (node.PayloadIndex < 0 || node.PayloadIndex >= (blob.MethodNames?.Length ?? 0))
                    {
                        result.Errors.Add($"Node {i}: Invalid method PayloadIndex {node.PayloadIndex}");
                    }
                }
                else if (node.Type == NodeType.Wait)
                {
                    if (node.PayloadIndex < 0 || node.PayloadIndex >= (blob.FloatParams?.Length ?? 0))
                    {
                        result.Errors.Add($"Node {i}: Invalid float PayloadIndex {node.PayloadIndex}");
                    }
                }
                else if (node.Type == NodeType.Repeater)
                {
                    if (node.PayloadIndex < 0 || node.PayloadIndex >= (blob.IntParams?.Length ?? 0))
                    {
                        result.Errors.Add($"Node {i}: Invalid int PayloadIndex {node.PayloadIndex}");
                    }
                }
            }
            
            return result;
        }
    }
}
