using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;
using Fbt.Serialization;

namespace Fbt.Demo.Visual
{
    public class RenderSystem
    {
        private IAgentStatusProvider _statusProvider = new DefaultStatusProvider();
        
        public void RenderAgents(List<Agent> agents, Agent? selectedAgent, Dictionary<string, BehaviorTreeBlob> trees)
        {
            foreach (var agent in agents)
            {
                // Draw target line if moving
                if (agent.Velocity.LengthSquared() > 0)
                {
                     Raylib.DrawLineV(agent.Position, agent.TargetPosition, new Color(200, 200, 200, 100));
                }

                // Draw agent body
                float radius = 8;
                if (agent == selectedAgent)
                {
                    Raylib.DrawCircleV(agent.Position, radius + 4, Color.White); // Selection ring
                }
                
                // Flash white when attacking
                Color agentColor = agent.Color;
                if (agent.AttackFlashTimer > 0)
                {
                    agentColor = Color.White;
                }
                
                Raylib.DrawCircleV(agent.Position, radius, agentColor);
                
                // Draw direction indicator
                var direction = new Vector2(MathF.Cos(agent.Rotation), MathF.Sin(agent.Rotation));
                var endPos = agent.Position + direction * 12;
                Raylib.DrawLineV(agent.Position, endPos, Color.Black);
                
                // Draw attack effect - expanding yellow ring
                if (agent.AttackFlashTimer > 0)
                {
                    float attackRadius = (0.3f - agent.AttackFlashTimer) * 60f;
                    byte alpha = (byte)(agent.AttackFlashTimer / 0.3f * 200);
                    Raylib.DrawCircleLines(
                        (int)agent.Position.X,
                        (int)agent.Position.Y,
                        (int)attackRadius,
                        Raylib.ColorAlpha(Color.Yellow, alpha / 255f));
                }
                
                // NEW: Render status label above agent
                if (trees.TryGetValue(agent.TreeName, out var blob))
                {
                    string status = _statusProvider.GetAgentStatus(agent, blob);
                    RenderAgentLabel(agent.Position, status, agent == selectedAgent);
                }
            }
        }
        
        private void RenderAgentLabel(Vector2 position, string text, bool isSelected)
        {
            // Position above agent
            Vector2 labelPos = position + new Vector2(0, -28);
            
            // Measure text
            int fontSize = 12;
            int spacing = 1;
            int textWidth = Raylib.MeasureText(text, fontSize);
            
            // Background box
            Color bgColor = isSelected 
                ? new Color(50, 50, 50, 220)   // Darker for selected
                : new Color(0, 0, 0, 180);      // Semi-transparent black
            
            int padding = 4;
            Raylib.DrawRectangle(
                (int)(labelPos.X - textWidth / 2 - padding),
                (int)(labelPos.Y - padding),
                textWidth + padding * 2,
                fontSize + padding * 2,
                bgColor);
            
            // Text
            Color textColor = isSelected ? Color.Yellow : Color.White;
            Raylib.DrawText(
                text,
                (int)(labelPos.X - textWidth / 2),
                (int)labelPos.Y,
                fontSize,
                textColor);
        }
    }
}
