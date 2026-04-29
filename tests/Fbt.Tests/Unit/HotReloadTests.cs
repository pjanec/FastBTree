using System;
using Fbt;
using Fbt.Compiler;
using Fbt.HotReload;
using Fbt.Runtime;
using Fbt.Tests.TestFixtures;

namespace Fbt.Tests.Unit
{
    public class HotReloadTests
    {
        // ---- Shared helpers ----

        private static BehaviorTreeBlob MakeBlob(int structureHash, int paramHash)
            => new BehaviorTreeBlob { StructureHash = structureHash, ParamHash = paramHash };

        private static NodeStatus AlwaysSuccess(
            ref TestBlackboard bb, ref BehaviorTreeState state, ref MockContext ctx, int p)
            => NodeStatus.Success;

        private struct SimpleState
        {
            public int Value;
        }

        // ---- FBT-020: BTreeHotReloadManager tests ----

        [Fact]
        public void TryReload_NewTree_ReturnsNewTree()
        {
            var manager = new BTreeHotReloadManager();
            var blob = MakeBlob(structureHash: 1, paramHash: 1);

            var result = manager.TryReload("tree", blob, Span<SimpleState>.Empty, null);

            Assert.Equal(ReloadResult.NewTree, result);
        }

        [Fact]
        public void TryReload_NoChange_WhenHashesIdentical()
        {
            var manager = new BTreeHotReloadManager();
            var blob1 = MakeBlob(structureHash: 10, paramHash: 20);
            var blob2 = MakeBlob(structureHash: 10, paramHash: 20);

            manager.TryReload("tree", blob1, Span<SimpleState>.Empty, null);
            var result = manager.TryReload("tree", blob2, Span<SimpleState>.Empty, null);

            Assert.Equal(ReloadResult.NoChange, result);
        }

        [Fact]
        public void TryReload_SoftReload_WhenOnlyParamHashDiffers()
        {
            var manager = new BTreeHotReloadManager();
            var blob1 = MakeBlob(structureHash: 10, paramHash: 20);
            var blob2 = MakeBlob(structureHash: 10, paramHash: 99);

            manager.TryReload("tree", blob1, Span<SimpleState>.Empty, null);
            var result = manager.TryReload("tree", blob2, Span<SimpleState>.Empty, null);

            Assert.Equal(ReloadResult.SoftReload, result);
        }

        [Fact]
        public void TryReload_HardReset_WhenStructureHashDiffers()
        {
            var manager = new BTreeHotReloadManager();
            var blob1 = MakeBlob(structureHash: 10, paramHash: 20);
            var blob2 = MakeBlob(structureHash: 99, paramHash: 20);

            manager.TryReload("tree", blob1, Span<SimpleState>.Empty, null);
            var result = manager.TryReload("tree", blob2, Span<SimpleState>.Empty, null);

            Assert.Equal(ReloadResult.HardReset, result);
        }

        [Fact]
        public void TryReload_HardReset_CallsHardResetAction_OnAllInstances()
        {
            var manager = new BTreeHotReloadManager();
            var blob1 = MakeBlob(structureHash: 1, paramHash: 1);
            var blob2 = MakeBlob(structureHash: 2, paramHash: 1);

            manager.TryReload("tree", blob1, Span<SimpleState>.Empty, null);

            var instances = new SimpleState[]
            {
                new SimpleState { Value = 42 },
                new SimpleState { Value = 43 },
                new SimpleState { Value = 44 },
            };

            SpanResetAction<SimpleState> resetAll = (span, i) => span[i] = default;
            var result = manager.TryReload("tree", blob2, instances.AsSpan(), resetAll);

            Assert.Equal(ReloadResult.HardReset, result);
            Assert.Equal(0, instances[0].Value);
            Assert.Equal(0, instances[1].Value);
            Assert.Equal(0, instances[2].Value);
        }

        [Fact]
        public void TryReload_NullBlob_ReturnsNoChange()
        {
            var manager = new BTreeHotReloadManager();

            var result = manager.TryReload<SimpleState>("tree", null, Span<SimpleState>.Empty, null);

            Assert.Equal(ReloadResult.NoChange, result);
        }

        [Fact]
        public void TryReload_SoftReload_DoesNotCallHardResetAction()
        {
            var manager = new BTreeHotReloadManager();
            var blob1 = MakeBlob(structureHash: 10, paramHash: 1);
            var blob2 = MakeBlob(structureHash: 10, paramHash: 2);

            manager.TryReload("tree", blob1, Span<SimpleState>.Empty, null);

            bool actionCalled = false;
            SpanResetAction<SimpleState> flagAction = (span, i) => { actionCalled = true; };
            var result = manager.TryReload("tree", blob2, Span<SimpleState>.Empty, flagAction);

            Assert.Equal(ReloadResult.SoftReload, result);
            Assert.False(actionCalled);
        }

        [Fact]
        public void TryReload_EmptySpan_HardReset_DoesNotThrow()
        {
            var manager = new BTreeHotReloadManager();
            var blob1 = MakeBlob(structureHash: 1, paramHash: 1);
            var blob2 = MakeBlob(structureHash: 2, paramHash: 1);

            manager.TryReload("tree", blob1, Span<SimpleState>.Empty, null);

            SpanResetAction<SimpleState> resetAction = (span, i) => span[i] = default;
            var result = manager.TryReload("tree", blob2, Span<SimpleState>.Empty, resetAction);

            Assert.Equal(ReloadResult.HardReset, result);
        }

        // ---- FBT-021: Interpreter hot reload safety check tests ----

        [Fact]
        public void Interpreter_HotReloadCheck_ResetsState_WhenRunningIndexOutOfBounds()
        {
            // Single-action blob: Nodes.Length == 1
            var builder = new BTreeBuilder<TestBlackboard, MockContext>();
            var blob = builder
                .Action(AlwaysSuccess)
                .Compile("SingleAction");
            var registry = builder.GetRegistry();
            var interpreter = new Interpreter<TestBlackboard, MockContext>(blob, registry);

            var bb = new TestBlackboard();
            var state = new BehaviorTreeState();
            var ctx = new MockContext();

            // blob.Nodes.Length == 1; index 5 is out of bounds
            state.RunningNodeIndex = 5;
            uint versionBefore = state.TreeVersion;

            // Should not throw
            interpreter.Tick(ref bb, ref state, ref ctx);

            // Bounds check fires: TreeVersion incremented, RunningNodeIndex reset
            Assert.True(state.TreeVersion > versionBefore, "TreeVersion should have been incremented by bounds check");
            Assert.NotEqual((ushort)5, state.RunningNodeIndex);
        }

        [Fact]
        public void Interpreter_HotReloadCheck_DoesNotResetState_WhenRunningIndexValid()
        {
            // Sequence + Action = 2 nodes; index 1 is within bounds
            var builder = new BTreeBuilder<TestBlackboard, MockContext>();
            var blob = builder
                .Sequence(s => s
                    .Action(AlwaysSuccess))
                .Compile("SeqOneAction");
            var registry = builder.GetRegistry();
            var interpreter = new Interpreter<TestBlackboard, MockContext>(blob, registry);

            var bb = new TestBlackboard();
            var state = new BehaviorTreeState();
            var ctx = new MockContext();

            // blob.Nodes.Length == 2; index 1 is in bounds (1 < 2)
            state.RunningNodeIndex = 1;
            uint versionBefore = state.TreeVersion; // 0

            // Should not throw; bounds check must NOT fire
            interpreter.Tick(ref bb, ref state, ref ctx);

            // TreeVersion not incremented by bounds check
            Assert.Equal(versionBefore, state.TreeVersion);
        }
    }
}
