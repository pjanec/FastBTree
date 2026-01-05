using Fbt;
using Fbt.Runtime;
using Fbt.Serialization;
using System;
using System.Numerics;

namespace Fbt.Demo.Visual
{
    public interface IAgentStatusProvider
    {
        string GetAgentStatus(Agent agent, BehaviorTreeBlob blob);
    }
    
    public class DefaultStatusProvider : IAgentStatusProvider
    {
        public string GetAgentStatus(Agent agent, BehaviorTreeBlob blob)
        {
            // If tree not running, show role
            if (blob.Nodes == null || 
                agent.State.RunningNodeIndex < 0 || 
                agent.State.RunningNodeIndex >= blob.Nodes.Length)
            {
                return agent.Role.ToString();
            }
            
            var runningNode = blob.Nodes[agent.State.RunningNodeIndex];
            return BuildStatusString(agent, blob, runningNode, agent.State.RunningNodeIndex);
        }
        
        private unsafe string BuildStatusString(
            Agent agent, 
            BehaviorTreeBlob blob, 
            NodeDefinition node, 
            int nodeIndex)
        {
            // For Action/Condition nodes - show the method name
            if (node.Type == NodeType.Action || node.Type == NodeType.Condition)
            {
                string actionName = GetActionName(blob, node);
                
                // Custom formatting based on action name
                return actionName switch
                {
                    "FindPatrolPoint" => "Picking patrol point",
                    "MoveToTarget" => "Moving...",
                    "FindResource" => "Looking for resources",
                    "Gather" => "Gathering",
                    "ReturnToBase" => "Returning to base",
                    "ScanForEnemy" => "Scanning for enemies",
                    "HasEnemy" => "Checking for enemies",
                    "FindRandomPoint" => "Wandering",
                    "ChaseEnemy" => $"Chasing Agent #{agent.Blackboard.TargetAgentId}",
                    "Attack" => "⚔️ ATTACKING!",
                    _ => actionName
                };
            }
            
            // For Wait nodes - show status
            if (node.Type == NodeType.Wait)
            {
                float duration = GetWaitDuration(blob, node);
                return $"Waiting ({duration:F1}s)";
            }
            
            // For Repeater - show iteration
            if (node.Type == NodeType.Repeater)
            {
                int currentCount = agent.State.LocalRegisters[0];
                int maxCount = GetRepeaterMax(blob, node);
                if (maxCount < 0)
                    return $"Loop #{currentCount}";
                return $"Loop {currentCount}/{maxCount}";
            }
            
            // For Cooldown
            if (node.Type == NodeType.Cooldown)
            {
                return "On cooldown";
            }
            
            // Default: show node type
            return node.Type.ToString();
        }
        
        private string GetActionName(BehaviorTreeBlob blob, NodeDefinition node)
        {
            if (node.PayloadIndex >= 0 && node.PayloadIndex < (blob.MethodNames?.Length ?? 0))
                return blob.MethodNames![node.PayloadIndex];
            return "Unknown";
        }
        
        private float GetWaitDuration(BehaviorTreeBlob blob, NodeDefinition node)
        {
            if (node.PayloadIndex >= 0 && node.PayloadIndex < (blob.FloatParams?.Length ?? 0))
                return blob.FloatParams![node.PayloadIndex];
            return 0f;
        }
        
        private int GetRepeaterMax(BehaviorTreeBlob blob, NodeDefinition node)
        {
            if (node.PayloadIndex >= 0 && node.PayloadIndex < (blob.IntParams?.Length ?? 0))
                return blob.IntParams![node.PayloadIndex];
            return -1;
        }
    }
}
