using Unity.Netcode;
using Unity.Collections;

[System.Serializable]
public struct LobbyPlayerInfo : INetworkSerializable, System.IEquatable<LobbyPlayerInfo>
{
    public ulong ClientId;
    public FixedString64Bytes PlayerName;

    // Реализация IEquatable<LobbyPlayerInfo>
    public bool Equals(LobbyPlayerInfo other)
    {
        return ClientId == other.ClientId && PlayerName.Equals(other.PlayerName);
    }

    // Переопределение Equals и GetHashCode для корректной работы
    public override bool Equals(object obj)
    {
        return obj is LobbyPlayerInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ClientId.GetHashCode() ^ PlayerName.GetHashCode();
    }

    // Реализация INetworkSerializable
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
    }

    // Для удобства создания
    public static LobbyPlayerInfo Create(ulong clientId, string playerName)
    {
        return new LobbyPlayerInfo
        {
            ClientId = clientId,
            PlayerName = new FixedString64Bytes(playerName)
        };
    }
}