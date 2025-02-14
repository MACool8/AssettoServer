﻿namespace AssettoServer.Shared.Network.Packets.Incoming;

public class CSPHandshakeOut : IIncomingNetworkPacket
{
    public uint Version;
    public bool IsWeatherFxActive;
    public InputMethod InputMethod;
    public bool IsRainFxActive;
    public ulong UniqueKey;
    
    public void FromReader(PacketReader reader)
    {
        Version = reader.Read<uint>();
        IsWeatherFxActive = reader.Read<bool>();
        InputMethod = reader.Read<InputMethod>();
        IsRainFxActive = reader.Read<bool>();
        reader.Read<byte>(); // Padding
        UniqueKey = reader.Read<ulong>();
    }
}

public enum InputMethod : byte
{
    Unknown = 0,
    Keyboard = 1,
    Gamepad = 2,
    Wheel = 3
}
