using System;

namespace Fbt.Kernel
{
    /// <summary>
    /// Marks a static method as a shared AI condition usable from both BTree and HSM doctrines.
    /// Signature: static bool MethodName(ref TValue dto, Entity self, EntityRepository repo)
    /// TValue must be the type of the field <see cref="FieldName"/> on <see cref="DtoType"/>.
    /// The source generator computes the byte offset of that field within the parent DTO via
    /// Roslyn's semantic model and emits adapters keyed as "{MethodName}@{computedOffset}".
    /// Apply multiple times on the same method to share it across different parent DTOs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SharedAiConditionAttribute : Attribute
    {
        /// <summary>The parent DTO struct that contains the projected field.</summary>
        public Type DtoType { get; }

        /// <summary>Name of the field within <see cref="DtoType"/> that TValue is projected from.</summary>
        public string FieldName { get; }

        public SharedAiConditionAttribute(Type dtoType, string fieldName)
        {
            DtoType   = dtoType;
            FieldName = fieldName;
        }
    }

    /// <summary>
    /// Marks a static method as a shared AI action usable from both BTree and HSM doctrines.
    /// Signature: static NodeStatus MethodName(ref TValue dto, Entity self, EntityRepository repo)
    /// HSM adapter discards the NodeStatus return (HSM is event-driven, not polling).
    /// Apply multiple times on the same method to share it across different parent DTOs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SharedAiActionAttribute : Attribute
    {
        /// <summary>The parent DTO struct that contains the projected field.</summary>
        public Type DtoType { get; }

        /// <summary>Name of the field within <see cref="DtoType"/> that TValue is projected from.</summary>
        public string FieldName { get; }

        public SharedAiActionAttribute(Type dtoType, string fieldName)
        {
            DtoType   = dtoType;
            FieldName = fieldName;
        }
    }

    /// <summary>
    /// Annotates a BTree action or HSM action method to declare that it writes to an actuator
    /// channel. The source generator uses this to emit failure-cleanup wrappers (BTree) and
    /// exit-cleanup thunks (HSM), plus a channel-safety registry.
    /// AllowMultiple = true so a method that writes to several channels can be annotated once
    /// per channel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class WritesChannelAttribute : Attribute
    {
        public ChannelKind Channel { get; }

        public WritesChannelAttribute(ChannelKind channel)
        {
            Channel = channel;
        }
    }

    /// <summary>
    /// Identifies an actuator channel component.  Must live in Fbt.Kernel so that both
    /// Fbt.SourceGen and Fhsm.SourceGen can reference it by fully qualified name.
    /// </summary>
    public enum ChannelKind
    {
        Locomotion,
        Weapon,
        Interaction,
    }
}
