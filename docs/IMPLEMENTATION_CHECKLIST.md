# FastBTree Implementation Checklist

**Version:** 1.0.0  
**Last Updated:** 2026-01-04

This checklist tracks implementation progress for FastBTree v1.0.

---

## Phase 1: Core (Weeks 1-3)

### Week 1: Foundation

**Data Structures**
- [ ] Create `Fbt.Kernel` project (.NET 8, AllowUnsafeBlocks)
- [ ] `NodeType` enum
- [ ] `NodeStatus` enum
- [ ] `NodeDefinition` struct (8 bytes, validate with `Unsafe.SizeOf`)
- [ ] `BehaviorTreeState` struct (64 bytes, explicit layout)
- [ ] `BehaviorTreeBlob` class
- [ ] `AsyncToken` struct (pack/unpack methods)
- [ ] Unit tests: DataStructuresTests.cs
  - [ ] Size validation (8 bytes, 64 bytes)
  - [ ] Reset behavior
  - [ ] AsyncToken round-trip
  - [ ] Stack push/pop

**Context Interface**
- [ ] `IAIContext` interface
- [ ] Result structs (RaycastResult, PathResult, OverlapResult)
- [ ] `NodeLogicDelegate<TBB, TCtx>` signature
- [ ] `ActionRegistry<TBB, TCtx>` class (delegate storage)

**Test Framework**
- [ ] Create `Fbt.Tests` project (xUnit)
- [ ] Test fixtures: MockContext, TestBlackboard, TestActions
- [ ] CI/CD configuration (.github/workflows/test.yml)

---

### Week 2: Interpreter

**Core Interpreter**
- [ ] `ITreeRunner<TBB, TCtx>` interface
- [ ] `Interpreter<TBB, TCtx>` class
- [ ] `Tick()` method with hot reload check
- [ ] `ExecuteNode()` dispatcher (switch on NodeType)

**Composite Nodes**
- [ ] `ExecuteSequence()` with resume optimization
- [ ] `ExecuteSelector()` with resume optimization
- [ ] `ExecuteParallel()` (optional for v1.0)

**Leaf Nodes**
- [ ] `ExecuteAction()` with delegate invocation
- [ ] `ExecuteCondition()` (same as Action)
- [ ] `ExecuteWait()` using GenericTimer

**Decorators**
- [ ] `ExecuteInverter()` (flip child result)
- [ ] `ExecuteRepeater()` with count parameter
- [ ] `ExecuteCooldown()` (optional for v1.0)

**Observer Aborts**
- [ ] `ExecuteObserverSelector()` with guard re-evaluation
- [ ] Tree version increment on abort

**Delegate Binding**
- [ ] `BindActions()` method (cache delegates at startup)
- [ ] Error handling for missing actions

**Unit Tests**
- [ ] InterpreterTests.cs
  - [ ] Sequence: AllSucceed → Success
  - [ ] Sequence: FirstFails → Failure
  - [ ] Selector: FirstSucceeds → Success
  - [ ] Selector: AllFail → Failure
  - [ ] Running node resumption
  - [ ] Observer abort interrupt
  - [ ] Delegate binding

---

### Week 3: Serialization

**JSON Parsing**
- [ ] `JsonTreeData` / `JsonNode` classes
- [ ] `TreeCompiler.CompileFromJson()` entry point
- [ ] `ParseJsonNode()` recursive parser
- [ ] Node type mapping (string → NodeType)

**Flattening**
- [ ] `BuilderNode` intermediate structure
- [ ] `BuildBlob()` depth-first flattener
- [ ] SubtreeOffset calculation (backpatching)
- [ ] Method registry (deduplication)
- [ ] Float/Int parameter registries

**Hashing**
- [ ] `CalculateStructureHash()` (MD5 of node types)
- [ ] `CalculateParamHash()` (MD5 of parameters)
- [ ] Integration with hot reload logic

**Binary Serialization**
- [ ] `BinaryTreeSerializer.Save()`
- [ ] `BinaryTreeSerializer.Load()`
- [ ] Magic bytes validation
- [ ] Version checking

**Validation**
- [ ] `TreeValidator.Validate()`
- [ ] SubtreeOffset bounds checking
- [ ] PayloadIndex validation
- [ ] ChildCount consistency

**Dependency Tracking**
- [ ] `DependencyDatabase` class
- [ ] `RegisterDependency()` method
- [ ] `GetDependents()` query
- [ ] Save/Load to JSON

**Unit Tests**
- [ ] SerializationTests.cs
  - [ ] JSON parsing simple tree
  - [ ] Binary round-trip
  - [ ] Structure hash change detection
  - [ ] Param hash calculation
  - [ ] Validation errors

**Integration Tests**
- [ ] TreeExecutionTests.cs
  - [ ] Load JSON → Execute → Verify
  - [ ] Full patrol tree execution
  - [ ] Combat tree transitions

---

## Phase 2: Demo & Testing (Weeks 4-6)

### Week 4: Context & Async

**GameContext**
- [ ] `GameContext` struct
- [ ] Batched raycast system
- [ ] Batched pathfinding system
- [ ] `ProcessBatch()` implementation
- [ ] Parameter lookup (GetFloatParam, GetIntParam)

**MockContext**
- [ ] `MockContext` struct for tests
- [ ] Pre-programmed results
- [ ] Call tracking
- [ ] Immediate mode (no batching)

**ReplayContext**
- [ ] `ReplayContext` struct
- [ ] Frame-by-frame playback
- [ ] Query result replay (FIFO queues)
- [ ] Desync detection

**Async Safety**
- [ ] TreeVersion increment on abort
- [ ] AsyncToken validation in actions
- [ ] Zombie request test case

**Hot Reload**
- [ ] Hash checking in Tick()
- [ ] Hard reload (structure change)
- [ ] Soft reload (param-only change)
- [ ] Integration test

**Unit Tests**
- [ ] ContextTests.cs
  - [ ] Batching behavior
  - [ ] Async token validation
  - [ ] Hot reload detection

---

### Week 5: Demo Application

**Project Setup**
- [ ] Create `FastBTreeDemo` project
- [ ] Add Raylib-cs NuGet package
- [ ] Add ImGui.NET NuGet package
- [ ] Reference Fbt.Kernel

**Application Core**
- [ ] `DemoApp` main class
- [ ] Raylib initialization
- [ ] ImGui controller integration
- [ ] Main loop (Update/Render)

**Simple ECS**
- [ ] `Entity` class (component dictionary)
- [ ] `Transform` component
- [ ] `Sprite` component
- [ ] `Health` component
- [ ] `BehaviorComponent` wrapper

**Systems**
- [ ] `BehaviorSystem` (tick all BTs)
- [ ] `MovementSystem` (basic movement)
- [ ] `RenderSystem` (sprite drawing)
- [ ] `CombatSystem` (damage, death)

**Scene Infrastructure**
- [ ] `IScene` interface
- [ ] `SceneManager` (scene switching)
- [ ] Scene menu UI

**Patrol Scene**
- [ ] `PatrolScene` implementation
- [ ] Patrol points setup
- [ ] Agent spawning
- [ ] Waypoint visualization
- [ ] Scene UI (agent count slider)

**Combat Scene**
- [ ] `CombatScene` implementation
- [ ] Player-controlled entity
- [ ] Orc agents with combat AI
- [ ] Health bars
- [ ] Aggro radius visualization
- [ ] Scene UI

**Tree Visualizer**
- [ ] `TreeVisualizer` UI component
- [ ] Recursive tree rendering
- [ ] Color coding (running=yellow)
- [ ] Node context menu

**Entity Inspector**
- [ ] `EntityInspector` UI component
- [ ] Component display
- [ ] Blackboard inspection
- [ ] Register/handle display

**Assets**
- [ ] patrol.json tree
- [ ] combat.json tree
- [ ] Sprite textures (or colored circles)

---

### Week 6: Recording & Profiling

**Performance Monitoring**
- [ ] `PerformanceMonitor` class
- [ ] Frame time tracking
- [ ] BT tick time tracking
- [ ] Entity/tree counts
- [ ] `PerformancePanel` UI

**Golden Run Recorder**
- [ ] `GoldenRunRecording` data class
- [ ] `FrameRecord` data class
- [ ] `GoldenRunRecorder` implementation
- [ ] `RecordingContext` (spy wrapper)
- [ ] File save/load (JSON)

**Playback**
- [ ] `PlaybackScene` implementation
- [ ] Frame-by-frame stepping
- [ ] Playback controls UI
- [ ] State visualization

**Recorder UI**
- [ ] `RecorderPanel` component
- [ ] Start/stop recording
- [ ] Recording list
- [ ] Load for playback

**Crowd Scene**
- [ ] `CrowdScene` implementation
- [ ] Dynamic agent spawning
- [ ] LOD system (optional distance-based ticking)
- [ ] Performance metrics display

**Golden Run Tests**
- [ ] Create sample recordings
  - [ ] combat_basic_001.json
  - [ ] patrol_loop_001.json
- [ ] `GoldenRunTests.cs`
  - [ ] Replay determinism test
  - [ ] State matching assertions

---

## Phase 3: Polish (Weeks 7-8)

### Week 7: Advanced Features

**Decorators**
- [ ] `Cooldown` decorator
- [ ] `ForceSuccess` decorator
- [ ] `ForceFailure` decorator
- [ ] `UntilSuccess` decorator
- [ ] `UntilFailure` decorator

**Service Nodes**
- [ ] Service node type
- [ ] Periodic execution logic
- [ ] Integration with composites

**Parallel Composite**
- [ ] Parallel execution logic
- [ ] Success/failure policies
- [ ] Unit tests

**Observer Decorator**
- [ ] Standalone observer node
- [ ] Abort mode configuration
- [ ] Integration tests

**Subtree Support**
- [ ] Subtree node type
- [ ] Monolithic baking
- [ ] Dependency tracking integration
- [ ] Optional runtime linking

**Tests**
- [ ] All decorators tested
- [ ] Service node tests
- [ ] Parallel composite tests
- [ ] Subtree tests

---

### Week 8: Optimization & Documentation

**Performance**
- [ ] Profile interpreter with crowd scene
- [ ] Identify bottlenecks
- [ ] Optimize hot paths
- [ ] Memory allocation audit (should be zero)
- [ ] Benchmark suite

**Memory**
- [ ] Validate 64-byte state alignment
- [ ] Check cache friendliness
- [ ] Reduce allocations if any found

**API Documentation**
- [ ] XML doc comments on all public APIs
- [ ] Generate API docs (DocFX or similar)
- [ ] Publish to docs/api/

**User Guide**
- [ ] Getting started guide
- [ ] Creating your first tree
- [ ] Defining actions
- [ ] Testing strategies
- [ ] Performance tips

**Example Trees**
- [ ] examples/Trees/simple_patrol.json
- [ ] examples/Trees/combat_tactics.json
- [ ] examples/Trees/guard_post.json
- [ ] examples/Trees/fleeing_behavior.json

**Cleanup**
- [ ] Code review
- [ ] Remove TODOs
- [ ] Consistent naming
- [ ] Final test pass

---

## Phase 4: JIT Compiler (Optional/Future)

### IL Generation

- [ ] `TreeCompiler.Compile()` entry point
- [ ] `DynamicMethod` creation
- [ ] Label generation for all nodes
- [ ] Resume switch emission
- [ ] Sequence IL emission
- [ ] Selector IL emission
- [ ] Action call emission
- [ ] Guard clause injection

### Testing

- [ ] JIT vs Interpreter correctness tests
- [ ] Performance comparison

### Integration

- [ ] Runtime mode selection (env var or config)
- [ ] Cache compiled delegates

---

## Acceptance Criteria (v1.0 Release)

**Functionality**
- ✅ All core node types working
- ✅ Full test coverage (unit + integration + golden run)
- ✅ Demo app with 4 scenes
- ✅ Recording/replay functional
- ✅ Hot reload working

**Performance**
- ✅ < 0.1ms per tick (simple tree, interpreter)
- ✅ 10K entities @ 60 FPS (crowd scene)
- ✅ Zero allocations during tick
- ✅ 64-byte state verified

**Quality**
- ✅ 100% test coverage on core logic
- ✅ All tests passing on CI
- ✅ No compiler warnings
- ✅ API documentation complete

**Deliverables**
- ✅ Fbt.Kernel library (NuGet-ready)
- ✅ FastBTreeDemo application (runnable)
- ✅ Complete design docs
- ✅ User guide
- ✅ Example trees

---

## Progress Tracking

**Overall: 0%**

- [ ] Phase 1: Core (0%)
- [ ] Phase 2: Demo & Testing (0%)
- [ ] Phase 3: Polish (0%)
- [ ] Phase 4: JIT (Future)

---

## Notes

- Mark items with ✅ when complete
- Update percentage after each week
- Track blockers in project issues
- Review checklist weekly

**Next Action:** Begin Phase 1, Week 1 - Create Fbt.Kernel project and implement data structures.
