using Xunit;
using Fbt.Serialization;

namespace Fbt.Tests.Unit
{
    public class TreeValidatorTests
    {
        [Fact]
        public void TreeValidator_ValidTree_NoErrors()
        {
            var blob = new BehaviorTreeBlob
            {
                Nodes = new[] 
                { 
                    new NodeDefinition { Type = NodeType.Action, SubtreeOffset = 1, PayloadIndex = 0 } 
                },
                MethodNames = new[] { "A" }
            };

            var result = TreeValidator.Validate(blob);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void TreeValidator_InvalidSubtreeOffset_ReportsError()
        {
            var blob = new BehaviorTreeBlob
            {
                Nodes = new[] 
                { 
                    new NodeDefinition { Type = NodeType.Action, SubtreeOffset = 0 } // Zero offset is invalid
                }
            };

            var result = TreeValidator.Validate(blob);
            Assert.False(result.IsValid);
            Assert.Contains("SubtreeOffset is zero", result.Errors[0]);
        }

        [Fact]
        public void TreeValidator_OffsetOutOfBounds_ReportsError()
        {
             var blob = new BehaviorTreeBlob
            {
                Nodes = new[] 
                { 
                    new NodeDefinition { Type = NodeType.Action, SubtreeOffset = 5 } // Length is 1
                }
            };

            var result = TreeValidator.Validate(blob);
            Assert.False(result.IsValid);
            Assert.Contains("exceeds tree bounds", result.Errors[0]);
        }

        [Fact]
        public void TreeValidator_InvalidPayloadIndex_ReportsError()
        {
            var blob = new BehaviorTreeBlob
            {
                Nodes = new[] 
                { 
                    new NodeDefinition { Type = NodeType.Action, SubtreeOffset = 1, PayloadIndex = 99 } // No methods
                },
                MethodNames = new string[0]
            };

            var result = TreeValidator.Validate(blob);
            Assert.False(result.IsValid);
            Assert.Contains("Invalid method PayloadIndex", result.Errors[0]);
        }
    }
}
