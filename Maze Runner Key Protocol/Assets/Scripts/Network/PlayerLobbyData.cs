using System;
using Unity.Netcode;
using Unity.Collections;

/// <summary>
/// Serializable player data for the lobby NetworkList.
/// Tracks connected players and their display names.
/// </summary>
public struct PlayerLobbyData : INetworkSerializable, IEquatable<PlayerLobbyData>
{
    public ulong ClientId;
    public FixedString32Bytes DisplayName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref DisplayName);
    }

    public bool Equals(PlayerLobbyData other)
    {
        return ClientId == other.ClientId && DisplayName == other.DisplayName;
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerLobbyData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ClientId.GetHashCode();
    }
}
