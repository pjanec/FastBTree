using System;

namespace Fbt.Runtime
{
    /// <summary>
    /// Interprets and executes a behavior tree.
    /// </summary>
    public class Interpreter<TBlackboard, TContext> : ITreeRunner<TBlackboard, TContext>
        where TBlackboard : struct
        where TContext : struct, IAIContext
    {
        private readonly BehaviorTreeBlob _blob;
        private readonly NodeLogicDelegate<TBlackboard, TContext>[] _actionDelegates;

        public Interpreter(BehaviorTreeBlob blob, ActionRegistry<TBlackboard, TContext> registry)
        {
            _blob = blob ?? throw new ArgumentNullException(nameof(blob));
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            
            _actionDelegates = BindActions(blob, registry);
        }

        public NodeStatus Tick(
            ref TBlackboard blackboard,
            ref BehaviorTreeState state,
            ref TContext context)
        {
            // === HOT RELOAD CHECK (Stub for now) ===
            // Will implement in BATCH-04
            
            // === EXECUTE TREE ===
            if (_blob.Nodes.Length == 0) return NodeStatus.Success; // Empty tree safety

            var result = ExecuteNode(0, ref blackboard, ref state, ref context);
            
            // === CLEANUP ===
            if (result != NodeStatus.Running)
            {
                state.RunningNodeIndex = 0;
            }
            
            return result;
        }

        private NodeStatus ExecuteNode(
            int nodeIndex,
            ref TBlackboard bb,
            ref BehaviorTreeState state,
            ref TContext ctx)
        {
            ref var node = ref _blob.Nodes[nodeIndex];
            
            switch (node.Type)
            {
                case NodeType.Sequence:
                    return ExecuteSequence(nodeIndex, ref node, ref bb, ref state, ref ctx);
                case NodeType.Selector:
                    return ExecuteSelector(nodeIndex, ref node, ref bb, ref state, ref ctx);
                case NodeType.Action:
                case NodeType.Condition:
                    return ExecuteAction(nodeIndex, ref node, ref bb, ref state, ref ctx);
                case NodeType.Inverter:
                    return ExecuteInverter(nodeIndex, ref node, ref bb, ref state, ref ctx);
                case NodeType.Wait:
                    return ExecuteWait(nodeIndex, ref node, ref bb, ref state, ref ctx);
                case NodeType.Repeater:
                    return ExecuteRepeater(nodeIndex, ref node, ref bb, ref state, ref ctx);
                default:
                    return NodeStatus.Failure; // Unknown/Unimplemented node type
            }
        }

        private NodeStatus ExecuteWait(
            int nodeIndex,
            ref NodeDefinition node,
            ref TBlackboard bb,
            ref BehaviorTreeState state,
            ref TContext ctx)
        {
            // Get duration from FloatParams
            float duration = _blob.FloatParams[node.PayloadIndex];
            
            // Check if we're resuming a wait
            // Use ushort cast to match RunningNodeIndex type
            if (state.RunningNodeIndex == nodeIndex)
            {
                // Unpack async token
                var token = new AsyncToken(state.AsyncData);
                float startTime = token.FloatA;
                
                // Check if duration has elapsed
                float elapsed = ctx.Time - startTime;
                if (elapsed >= duration)
                {
                    state.RunningNodeIndex = 0;
                    return NodeStatus.Success;
                }
                
                return NodeStatus.Running;
            }
            else
            {
                // First execution - pack start time
                var token = AsyncToken.FromFloat(ctx.Time, 0);
                state.AsyncData = token.PackedValue;
                state.RunningNodeIndex = (ushort)nodeIndex;
                return NodeStatus.Running;
            }
        }

        private NodeStatus ExecuteRepeater(
            int nodeIndex,
            ref NodeDefinition node,
            ref TBlackboard bb,
            ref BehaviorTreeState state,
            ref TContext ctx)
        {
            int repeatCount = _blob.IntParams[node.PayloadIndex];
            
            unsafe
            {
                ref int currentIteration = ref state.LocalRegisters[0];
                
                // If not running, start fresh
                if (state.RunningNodeIndex == 0)
                {
                    currentIteration = 0;
                }
                
                while (currentIteration < repeatCount)
                {
                    // Repeater has exactly one child
                    int childIndex = nodeIndex + 1;
                    var result = ExecuteNode(childIndex, ref bb, ref state, ref ctx);
                    
                    if (result == NodeStatus.Running)
                    {
                        return NodeStatus.Running;
                    }
                    
                    if (result == NodeStatus.Failure)
                    {
                        currentIteration = 0; // Reset on failure
                        return NodeStatus.Failure;
                    }
                    
                    // Child succeeded, increment counter
                    currentIteration++;
                    
                    // If more iterations remain, continue
                    if (currentIteration < repeatCount)
                    {
                        // Reset child for next iteration
                        // Since child returned Success, RunningNodeIndex is already 0.
                        // We loop again, ExecuteNode will start child fresh.
                        continue;
                    }
                }
                
                // All iterations complete
                currentIteration = 0;
                state.RunningNodeIndex = 0;
                return NodeStatus.Success;
            }
        }

        private NodeStatus ExecuteSequence(
            int nodeIndex,
            ref NodeDefinition node,
            ref TBlackboard bb,
            ref BehaviorTreeState state,
            ref TContext ctx)
        {
            int childCount = node.ChildCount;
            int currentChildIndex = nodeIndex + 1;

            for (int i = 0; i < childCount; i++)
            {
                ref var childNode = ref _blob.Nodes[currentChildIndex];

                // Resume logic: if running node passes this child's subtree, it means this child already SUCCEEDED
                if (state.RunningNodeIndex > 0 && 
                    state.RunningNodeIndex >= (currentChildIndex + childNode.SubtreeOffset))
                {
                    // Skip this child (it succeeded in previous tick)
                    currentChildIndex += childNode.SubtreeOffset;
                    continue;
                }

                var status = ExecuteNode(currentChildIndex, ref bb, ref state, ref ctx);

                if (status == NodeStatus.Running)
                {
                    return NodeStatus.Running;
                }
                
                if (status == NodeStatus.Failure)
                {
                    return NodeStatus.Failure;
                }

                // If success, proceed to next child
                currentChildIndex += childNode.SubtreeOffset;
            }

            return NodeStatus.Success;
        }

        private NodeStatus ExecuteSelector(
            int nodeIndex,
            ref NodeDefinition node,
            ref TBlackboard bb,
            ref BehaviorTreeState state,
            ref TContext ctx)
        {
            int childCount = node.ChildCount;
            int currentChildIndex = nodeIndex + 1;

            for (int i = 0; i < childCount; i++)
            {
                ref var childNode = ref _blob.Nodes[currentChildIndex];

                // Resume logic: if running node passes this child's subtree, it means this child already FAILED
                if (state.RunningNodeIndex > 0 && 
                    state.RunningNodeIndex >= (currentChildIndex + childNode.SubtreeOffset))
                {
                    // Skip this child (it failed in previous tick)
                    currentChildIndex += childNode.SubtreeOffset;
                    continue;
                }

                var status = ExecuteNode(currentChildIndex, ref bb, ref state, ref ctx);

                if (status == NodeStatus.Running)
                {
                    return NodeStatus.Running;
                }
                
                if (status == NodeStatus.Success)
                {
                    return NodeStatus.Success;
                }

                // If failure, proceed to next child
                currentChildIndex += childNode.SubtreeOffset;
            }

            return NodeStatus.Failure;
        }

        private NodeStatus ExecuteAction(
            int nodeIndex,
            ref NodeDefinition node,
            ref TBlackboard bb,
            ref BehaviorTreeState state,
            ref TContext ctx)
        {
            // Safety check for payload index
            if (node.PayloadIndex < 0 || node.PayloadIndex >= _actionDelegates.Length)
                return NodeStatus.Failure;

            var actionDelegate = _actionDelegates[node.PayloadIndex];
            var status = actionDelegate(ref bb, ref state, ref ctx, node.PayloadIndex);
            
            if (status == NodeStatus.Running)
            {
                state.RunningNodeIndex = (ushort)nodeIndex;
            }
            else if (state.RunningNodeIndex == nodeIndex)
            {
                state.RunningNodeIndex = 0;
            }
            
            return status;
        }

        private NodeStatus ExecuteInverter(
            int nodeIndex,
            ref NodeDefinition node,
            ref TBlackboard bb,
            ref BehaviorTreeState state,
            ref TContext ctx)
        {
            var childIndex = nodeIndex + 1;
            var result = ExecuteNode(childIndex, ref bb, ref state, ref ctx);
            
            return result switch
            {
                NodeStatus.Success => NodeStatus.Failure,
                NodeStatus.Failure => NodeStatus.Success,
                _ => result // Running stays Running
            };
        }

        private NodeLogicDelegate<TBlackboard, TContext>[] BindActions(
            BehaviorTreeBlob blob, 
            ActionRegistry<TBlackboard, TContext> registry)
        {
            if (blob.MethodNames == null) return Array.Empty<NodeLogicDelegate<TBlackboard, TContext>>();

            var delegates = new NodeLogicDelegate<TBlackboard, TContext>[blob.MethodNames.Length];
            var fallback = new NodeLogicDelegate<TBlackboard, TContext>((ref TBlackboard bb, ref BehaviorTreeState st, ref TContext ctx, int p) => NodeStatus.Failure);

            for (int i = 0; i < blob.MethodNames.Length; i++)
            {
                string name = blob.MethodNames[i];
                if (registry.TryGetAction(name, out var action))
                {
                    delegates[i] = action;
                }
                else
                {
                    Console.WriteLine($"[FastBTree] Warning: Action '{name}' not found in registry. Using fallback Failure.");
                    delegates[i] = fallback;
                }
            }

            return delegates;
        }
    }
}
