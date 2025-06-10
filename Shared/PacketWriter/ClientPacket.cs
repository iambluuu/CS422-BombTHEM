using Shared;

namespace Shared.PacketWriter {
    public enum ClientParams : byte {
        // Byte
        Direction,
        BombType,
        PowerUpType,
        SlotNum,
        // Ushort
        X,
        Y,
        // Int
        PlayerId,
        // String
        Username,
        RoomId,
    }

    public class ClientPacket {

        public static Dictionary<byte, object> ReadContent(BinaryReader reader) {
            Dictionary<byte, object> data = [];
            while (true) {
                if (reader.BaseStream.Position >= reader.BaseStream.Length) {
                    break; // End of stream
                }
                byte key = reader.ReadByte();
                if (key == '|') {
                    break; // End of message marker
                }

                object value;
                switch ((ClientParams)key) {
                    case ClientParams.Direction:
                    case ClientParams.BombType:
                    case ClientParams.PowerUpType:
                    case ClientParams.SlotNum:
                        value = reader.ReadByte();
                        break;
                    case ClientParams.X:
                    case ClientParams.Y:
                        value = reader.ReadUInt16();
                        break;
                    case ClientParams.PlayerId:
                        value = reader.ReadInt32();
                        break;
                    case ClientParams.Username:
                    case ClientParams.RoomId:
                        value = reader.ReadString();
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported key: {key}");
                }
                data[key] = value;
            }
            return data;
        }
        public static void WriteContent(BinaryWriter writer, ClientMessageType messageType, Dictionary<byte, object> data) {
            foreach (var item in data) {
                writer.Write(item.Key);
                switch ((ClientParams)item.Key) {
                    // Expect byte (e.g., enums or small numbers)
                    case ClientParams.Direction or ClientParams.BombType or ClientParams.PowerUpType or ClientParams.SlotNum: {
                            byte value = Convert.ToByte(item.Value);
                            writer.Write(value);
                            break;
                        }

                    // Expect ushort (e.g., coordinates)
                    case ClientParams.X or ClientParams.Y: {
                            ushort value = Convert.ToUInt16(item.Value);
                            writer.Write(value);
                            break;
                        }

                    // Expect int (e.g., player ID)
                    case ClientParams.PlayerId: {
                            int value = Convert.ToInt32(item.Value);
                            writer.Write(value);
                            break;
                        }

                    // Expect string
                    case ClientParams.Username or ClientParams.RoomId: {
                            if (item.Value is not string s)
                                throw new InvalidCastException($"Expected string for {item.Key}, but got {item.Value?.GetType().Name}");
                            writer.Write(s);
                            break;
                        }

                    default:
                        throw new InvalidOperationException($"Unsupported key or type mismatch for parameter: {item.Key}");
                }
            }
        }

    }
}