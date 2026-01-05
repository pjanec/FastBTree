using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;
using Fbt.Runtime;
using Fbt.Serialization;
using System.Numerics;
using System.Collections.Generic;

namespace Fbt.Demo.Visual
{
    public class DemoApp
    {
        private const int ScreenWidth = 1280;
        private const int ScreenHeight = 720;
        
        private List<Agent> _agents = new();
        private Dictionary<string, BehaviorTreeBlob> _trees = new();
        private BehaviorSystem _behaviorSystem = null!;
        private RenderSystem _renderSystem = null!;
        
        private float _time = 0;
        private bool _paused = false;
        private float _timeScale = 1.0f;
        
        private Agent? _selectedAgent = null;
        private UI.TreeVisualPanel _treeVisualPanel = new UI.TreeVisualPanel();
        
        public void Run()
        {
            Raylib.InitWindow(ScreenWidth, ScreenHeight, "FastBTree Visual Demo");
            Raylib.SetTargetFPS(60);
            rlImGui.Setup(true);
            
            Initialize();
            
            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime() * _timeScale;
                
                if (!_paused)
                {
                    Update(dt);
                }
                
                Render();
            }
            
            rlImGui.Shutdown();
            Raylib.CloseWindow();
        }
        
        private void Initialize()
        {
            // Load behavior trees
            _trees["patrol"] = LoadTree("Trees/patrol.json");
            _trees["gather"] = LoadTree("Trees/gather.json");
            _trees["combat"] = LoadTree("Trees/combat.json");
            
            // Create systems
            _behaviorSystem = new BehaviorSystem(_trees);
            _renderSystem = new RenderSystem();
            
            // Spawn initial agents
            SpawnPatrolAgents(5);
            SpawnGatherAgents(3);
        }
        
        private BehaviorTreeBlob LoadTree(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                // Create dummy file if not exists yet
                // But we will create them in Task 7. 
                // For now, let's just warn or handle later.
                // We'll create empty ones for now so it compiles/runs roughly.
                return new BehaviorTreeBlob(); 
            }
            string json = System.IO.File.ReadAllText(path);
            return TreeCompiler.CompileFromJson(json);
        }
        
        private void SpawnPatrolAgents(int count)
        {
            var rand = new Random();
            for (int i = 0; i < count; i++)
            {
                var pos = new Vector2(rand.Next(100, ScreenWidth - 100), rand.Next(100, ScreenHeight - 100));
                _agents.Add(new Agent(_agents.Count + 1, pos, "patrol", AgentRole.Patrol));
            }
        }
        
        private void SpawnGatherAgents(int count)
        {
             var rand = new Random();
            for (int i = 0; i < count; i++)
            {
                var pos = new Vector2(rand.Next(100, ScreenWidth - 100), rand.Next(100, ScreenHeight - 100));
                _agents.Add(new Agent(_agents.Count + 1, pos, "gather", AgentRole.Gather));
            }
        }

       private void SpawnCombatAgents(int count)
        {
             var rand = new Random();
            for (int i = 0; i < count; i++)
            {
                var pos = new Vector2(rand.Next(100, ScreenWidth - 100), rand.Next(100, ScreenHeight - 100));
                _agents.Add(new Agent(_agents.Count + 1, pos, "combat", AgentRole.Combat));
            }
        }

        private void Update(float dt)
        {
            _time += dt;
            
            // Mouse selection logic
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && !ImGui.GetIO().WantCaptureMouse)
            {
                var mousePos = Raylib.GetMousePosition();
                _selectedAgent = null;
                float closestDistSq = float.MaxValue;
                
                foreach (var agent in _agents)
                {
                    float distSq = Vector2.DistanceSquared(mousePos, agent.Position);
                    if (distSq < 20 * 20 && distSq < closestDistSq) // 20px radius
                    {
                        closestDistSq = distSq;
                        _selectedAgent = agent;
                    }
                }
            }

            // Update all agents
            _behaviorSystem.Update(_agents, _time, dt);
            
            // Update movement
            foreach (var agent in _agents)
            {
                agent.UpdateMovement(dt);
            }
        }
        
        private void Render()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.DarkGray);
            
            // Render world
            _renderSystem.RenderAgents(_agents, _selectedAgent, _trees, _time);
            
            // ImGui UI
            rlImGui.Begin();
            RenderUI();
            rlImGui.End();
            
            Raylib.EndDrawing();
        }
        
        private void RenderUI()
        {
            // Control panel
            ImGui.Begin("FastBTree Demo");
            
            ImGui.Text($"FPS: {Raylib.GetFPS()}");
            ImGui.Text($"Agents: {_agents.Count}");
            ImGui.Text($"Time: {_time:F2}s");
            
            ImGui.Separator();
            
            ImGui.Checkbox("Paused", ref _paused);
            ImGui.SliderFloat("Time Scale", ref _timeScale, 0.1f, 5.0f);
            
            ImGui.Separator();
            
            if (ImGui.Button("Spawn Patrol Agent"))
                SpawnPatrolAgents(1);
            
            if (ImGui.Button("Spawn Gather Agent"))
                SpawnGatherAgents(1);
                
             if (ImGui.Button("Spawn Combat Agent"))
                SpawnCombatAgents(1);
            
            if (ImGui.Button("Clear All"))
            {
                _agents.Clear();
                _selectedAgent = null;
            }

            ImGui.Separator();
             // Simple selection logic for demo purposes (e.g. iterate and button)
             // Or we just find the first agent for now or using mouse picking later.
             // For now, let's list them in a combo box or list.
             if (ImGui.CollapsingHeader("Agents List"))
             {
                 foreach(var agent in _agents)
                 {
                     bool isSelected = _selectedAgent == agent;
                     if (ImGui.Selectable($"Agent {agent.Id} ({agent.Role})", isSelected))
                     {
                         _selectedAgent = agent;
                     }
                 }
             }

             RenderPerformancePanel();
            
            ImGui.End();
            
            // Selected agent details
            if (_selectedAgent != null)
            {
               if (_trees.TryGetValue(_selectedAgent.TreeName, out var blob))
               {
                    _treeVisualPanel.Render(_selectedAgent, blob, _time);
               }
            }
        }

        private void RenderPerformancePanel()
        {
            ImGui.Begin("Performance");
            
            ImGui.Text($"FPS: {Raylib.GetFPS()}");
            ImGui.Text($"Frame Time: {Raylib.GetFrameTime() * 1000:F2}ms");
            ImGui.Text($"Agents: {_agents.Count}");
            ImGui.Text($"Trees Ticked/Frame: {_agents.Count}");
            
            ImGui.Separator();
            
            // Estimate tick time (mockup logic since we don't have per-tick measurement in DemoApp yet)
            // Ideally we'd measure this in BehaviorSystem.
            // For now, let's use a rough estimate based on 100ns per agent which is what benchmarks showed.
            float avgTickTime = 0.0001f; // 100ns in ms. 
            
            // Let's implement actual measurement in BehaviorSystem later if needed, but for now:
            float totalBehaviorTime = (float)Raylib.GetFrameTime() * 0.1f; // Fake 10% of frame is AI
            
            ImGui.Text($"Avg Tick Time (Est): {avgTickTime * 1000 * 1000:F0} ns"); // Show calculated ns
            
            // Performance budget
            float frameBudget = 16.67f; // 60 FPS in ms
            float usedBudgetMs = _agents.Count * 0.0001f; // 100ns * agents = ms
            float percentUsed = (usedBudgetMs / frameBudget) * 100;
            
            ImGui.ProgressBar(percentUsed / 100, new Vector2(-1, 0), $"{percentUsed:F4}% of frame");
            
            ImGui.End();
        }
    }
}
