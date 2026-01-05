using ImGuiNET;
using Fbt.Serialization;
using System.Numerics;

namespace Fbt.Demo.Visual.UI
{
    public class TreeVisualPanel
    {
        public void Render(Agent agent, BehaviorTreeBlob blob)
        {
            ImGui.Begin($"Agent Inspector - ID: {agent.Id}");
            
            // Agent Details
            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), "Agent Details");
            ImGui.Text($"Role: {agent.Role}");
            ImGui.Text($"Position: ({agent.Position.X:F0}, {agent.Position.Y:F0})");
            ImGui.Text($"Target: ({agent.TargetPosition.X:F0}, {agent.TargetPosition.Y:F0})");
            ImGui.Text($"Speed: {agent.Speed:F1}");
            
            // Distance to target
            float distToTarget = Vector2.Distance(agent.Position, agent.TargetPosition);
            ImGui.Text($"Distance to Target: {distToTarget:F1}");
            
            ImGui.Separator();
            
            // Full Blackboard State
            ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.3f, 1.0f), "Blackboard State");
            ImGui.Indent();
            ImGui.Text($"PatrolPointIndex: {agent.Blackboard.PatrolPointIndex}");
            ImGui.Text($"ResourceCount: {agent.Blackboard.ResourceCount}");
            ImGui.Text($"HasTarget: {agent.Blackboard.HasTarget}");
            ImGui.Text($"LastPatrolTime: {agent.Blackboard.LastPatrolTime:F2}s");
            
            // Role-specific interpretation
            if (agent.Role == AgentRole.Combat)
            {
                ImGui.Text($"State: {(agent.Blackboard.HasTarget ? "CHASING ENEMY" : "Wandering/Scanning")}");
            }
            else if (agent.Role == AgentRole.Gather)
            {
                ImGui.Text($"Gathering Progress: {agent.Blackboard.ResourceCount} items");
            }
            ImGui.Unindent();
            
            ImGui.Separator();
            
            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"Behavior Tree: {blob.TreeName}");
            // Show running node index. Note: RunningNodeIndex is internal to the state but accessible.
            // If tree is finished (Success/Failure), RunningNodeIndex might be -1 or reset.
            ImGui.Text($"Running Node Index: {agent.State.RunningNodeIndex}");
            
            ImGui.Separator();
            
            ImGui.BeginChild("TreeScroll", new Vector2(0, 300), ImGuiChildFlags.Borders);
            
            // Render tree hierarchy
            RenderNode(blob, 0, agent.State.RunningNodeIndex, 0);
            
            ImGui.EndChild();
            
            ImGui.End();
        }
        
        private void RenderNode(BehaviorTreeBlob blob, int index, int runningIndex, int depth)
        {
            if (index >= blob.Nodes.Length) return;
            
            var node = blob.Nodes[index];
            string indent = new string(' ', depth * 4); // Indent with spaces for now, or use TreeNodes
            
            // Highlight running node
            // Note: If we use ImGui.TreeNode, it's interactive. If we just use Text, it's static.
            // Let's use Text to keep it simple and aligned.
            
            Vector4 color = (index == runningIndex)
                ? new Vector4(1f, 1f, 0f, 1f) // Yellow for running
                : new Vector4(1f, 1f, 1f, 1f); // White otherwise
            
            // Optional: show extra info (payload)
            string extra = "";
            if (node.Type == NodeType.Action || node.Type == NodeType.Condition)
            {
                 if (node.PayloadIndex >= 0 && node.PayloadIndex < blob.MethodNames.Length)
                    extra = $" \"{blob.MethodNames[node.PayloadIndex]}\"";
            }
            else if (node.Type == NodeType.Wait || node.Type == NodeType.Cooldown)
            {
                if (node.PayloadIndex >= 0 && node.PayloadIndex < blob.FloatParams.Length)
                    extra = $" ({blob.FloatParams[node.PayloadIndex]}s)";
            }
            
            ImGui.TextColored(color, $"{indent}[{index}] {node.Type}{extra}");
            
            // Render children
            int childIndex = index + 1;
            for (int i = 0; i < node.ChildCount; i++)
            {
                RenderNode(blob, childIndex, runningIndex, depth + 1);
                childIndex += blob.Nodes[childIndex].SubtreeOffset;
            }
        }
    }
}
