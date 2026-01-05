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

        [Fact]
        public void Validate_NestedParallel_ReportsWarning()
        {
            string json = @"{
                ""TreeName"": ""NestedParallel"",
                ""Root"": {
                    ""Type"": ""Parallel"",
                    ""Policy"": 0,
                    ""Children"": [
                        {
                            ""Type"": ""Sequence"",
                            ""Children"": [
                                {
                                    ""Type"": ""Parallel"",
                                    ""Policy"": 0,
                                    ""Children"": [
                                        { ""Type"": ""Action"", ""Action"": ""A"" }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            }";
            
            var blob = TreeCompiler.CompileFromJson(json);
            var result = TreeValidator.Validate(blob);
            
            Assert.True(result.IsValid); // No errors
            Assert.True(result.HasWarnings); // But has warnings!
            Assert.Contains("Nested Parallel", result.Warnings[0]);
        }

        [Fact]
        public void Validate_NestedRepeater_ReportsWarning()
        {
            string json = @"{
                ""TreeName"": ""NestedRepeater"",
                ""Root"": {
                    ""Type"": ""Repeater"",
                    ""RepeatCount"": 2,
                    ""Children"": [
                        {
                            ""Type"": ""Sequence"",
                            ""Children"": [
                                {
                                    ""Type"": ""Repeater"",
                                    ""RepeatCount"": 2,
                                    ""Children"": [
                                        { ""Type"": ""Action"", ""Action"": ""A"" }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            }";
            
            var blob = TreeCompiler.CompileFromJson(json);
            var result = TreeValidator.Validate(blob);
            
            Assert.True(result.IsValid);
            Assert.True(result.HasWarnings);
            Assert.Contains("Nested Repeater", result.Warnings[0]);
        }

        [Fact]
        public void Validate_ParallelTooManyChildren_ReportsWarning()
        {
            // Create Parallel with 17 children
             var children = new System.Collections.Generic.List<string>();
             for(int i=0; i<17; i++) 
                children.Add(@"{ ""Type"": ""Action"", ""Action"": ""A"" }");

            string childrenJson = string.Join(",", children);

            string json = $@"{{
                ""TreeName"": ""BigParallel"",
                ""Root"": {{
                    ""Type"": ""Parallel"",
                    ""Policy"": 0,
                    ""Children"": [ {childrenJson} ]
                }}
            }}";
            
            var blob = TreeCompiler.CompileFromJson(json);
            var result = TreeValidator.Validate(blob);
            
            Assert.True(result.IsValid);
            Assert.True(result.HasWarnings);
            Assert.Contains("Parallel has 17 children", result.Warnings[0]);
        }
    }
}
