namespace Fbt
{
    /// <summary>
    /// Token for async operations (pathfinding, raycasts, etc.).
    /// Packs request ID with tree version for zombie-request detection.
    /// </summary>
    public readonly struct AsyncToken
    {
        /// <summary>The actual request ID from external system.</summary>
        public readonly int RequestID;
        
        /// <summary>TreeVersion when this request was made.</summary>
        public readonly uint Version;
        
        public AsyncToken(int requestId, uint version)
        {
            RequestID = requestId;
            Version = version;
        }
        
        /// <summary>Pack into ulong for storage in AsyncHandles[].</summary>
        public ulong Pack() => ((ulong)Version << 32) | (uint)RequestID;
        
        /// <summary>Unpack from ulong storage.</summary>
        public static AsyncToken Unpack(ulong packed)
        {
            int id = (int)(packed & 0xFFFFFFFF);
            uint version = (uint)(packed >> 32);
            return new AsyncToken(id, version);
        }
        
        /// <summary>Check if this token is valid for current tree version.</summary>
        public bool IsValid(uint currentTreeVersion)
            => RequestID != 0 && Version == currentTreeVersion;
    }
}
