using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace Fbt.Demo.Visual
{
    public class RenderSystem
    {
        public void RenderAgents(List<Agent> agents, Agent? selectedAgent)
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
                
                // Draw status icon/text above head
                if (agent.CurrentNode.HasValue)
                {
                    // Raylib.DrawCircleV(agent.Position + new Vector2(0, -15), 3, Color.YELLOW);
                }
            }
        }
    }
}
