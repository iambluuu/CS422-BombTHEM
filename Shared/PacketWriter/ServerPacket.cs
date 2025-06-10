

using Shared;

namespace Shared.PacketWriter {
    public enum ServerParams : byte {
        // Byte
        Direction,
        BombType,
        PowerUpType,
        SlotNum,
        // Bool
        IsHost,
        IsCounted,
        Invalid,
        NeedToChange,
        // Ushort
        X,
        Y,
        OldX,
        OldY,
        PlayerCount,
        // Int
        PlayerId,
        ByPlayerId,
        HostId,
        Duration,
        // String
        Username,
        RoomId,
        Message,
        Map,
        // Bool array
        InGames,
        // Ushort array
        Scores,
        // Int array
        PlayerIds,
        // String array
        Usernames,
        // (ushort, ushort) array
        Positions,
    }

    public class ServerPacket {

        public static Dictionary<byte, object> ReadContent(BinaryReader reader) {
            Dictionary<byte, object> data = new();
            while (true) {
                if (reader.BaseStream.Position >= reader.BaseStream.Length) {
                    break; // End of stream
                }
                byte key = reader.ReadByte();
                if (key == '|') {
                    break; // End of message marker
                }

                object value;
                switch ((ServerParams)key) {
                    case ServerParams.Direction:
                    case ServerParams.BombType:
                    case ServerParams.PowerUpType:
                    case ServerParams.SlotNum:
                        value = reader.ReadByte();
                        break;
                    case ServerParams.X:
                    case ServerParams.Y:
                    case ServerParams.OldX:
                    case ServerParams.OldY:
                    case ServerParams.PlayerCount:
                        value = reader.ReadUInt16();
                        break;
                    case ServerParams.PlayerId:
                    case ServerParams.ByPlayerId:
                    case ServerParams.HostId:
                    case ServerParams.Duration:
                        value = reader.ReadInt32();
                        break;
                    case ServerParams.Username:
                    case ServerParams.RoomId:
                    case ServerParams.Message:
                    case ServerParams.Map:
                        value = reader.ReadString();
                        break;
                    case ServerParams.IsHost:
                    case ServerParams.IsCounted:
                    case ServerParams.Invalid:
                    case ServerParams.NeedToChange:
                        value = reader.ReadBoolean();
                        break;
                    case ServerParams.InGames: {
                            ushort length = reader.ReadUInt16();
                            bool[] boolArray = new bool[length];
                            for (int i = 0; i < length; i++) {
                                boolArray[i] = reader.ReadBoolean();
                            }
                            value = boolArray;
                            break;
                        }
                    case ServerParams.Scores: {
                            ushort length = reader.ReadUInt16();
                            ushort[] scores = new ushort[length];
                            for (int i = 0; i < length; i++) {
                                scores[i] = reader.ReadUInt16();
                            }
                            value = scores;
                            break;
                        }
                    case ServerParams.PlayerIds: {
                            ushort length = reader.ReadUInt16();
                            int[] playerIds = new int[length];
                            for (int i = 0; i < length; i++) {
                                playerIds[i] = reader.ReadInt32();
                            }
                            value = playerIds;
                            break;
                        }
                    case ServerParams.Usernames: {
                            ushort length = reader.ReadUInt16();
                            string[] usernames = new string[length];
                            for (int i = 0; i < length; i++) {
                                usernames[i] = reader.ReadString();
                            }
                            value = usernames;


                            break;
                        }
                    case ServerParams.Positions: {
                            ushort length = reader.ReadUInt16();
                            Position[] positions = new Position[length];
                            for (int i = 0; i < length; i++) {
                                ushort x = reader.ReadUInt16();
                                ushort y = reader.ReadUInt16();
                                positions[i] = new Position(x, y);
                            }
                            value = positions;
                            break;
                        }
                    default:
                        throw new InvalidOperationException($"Unsupported key: {key}");
                }
                data[key] = value;
            }
            return data;
        }
        public static void WriteContent(BinaryWriter writer, ServerMessageType messageType, Dictionary<byte, object>? data = null) {
            foreach (var item in data ?? []) {
                writer.Write(item.Key);
                switch ((ServerParams)item.Key) {
                    // Expect byte (e.g., enums or small numbers)
                    case ServerParams.Direction:
                    case ServerParams.BombType:
                    case ServerParams.PowerUpType:
                    case ServerParams.SlotNum:
                        writer.Write(Convert.ToByte(item.Value));
                        break;
                    // Expect bool
                    case ServerParams.IsHost:
                    case ServerParams.IsCounted:
                    case ServerParams.Invalid:
                    case ServerParams.NeedToChange:
                        writer.Write(Convert.ToBoolean(item.Value));
                        break;
                    // Expect ushort
                    case ServerParams.X:
                    case ServerParams.Y:
                    case ServerParams.OldX:
                    case ServerParams.OldY:
                    case ServerParams.PlayerCount:
                        writer.Write(Convert.ToUInt16(item.Value));
                        break;
                    // Expect int
                    case ServerParams.PlayerId:
                    case ServerParams.ByPlayerId:
                    case ServerParams.HostId:
                    case ServerParams.Duration:
                        writer.Write(Convert.ToInt32(item.Value));
                        break;
                    // Expect string
                    case ServerParams.Username:
                    case ServerParams.RoomId:
                    case ServerParams.Message:
                    case ServerParams.Map:
                        writer.Write(Convert.ToString(item.Value) ?? string.Empty);
                        break;
                    // Expect bool array
                    case ServerParams.InGames:
                        var boolArray = (bool[])item.Value;
                        writer.Write((ushort)boolArray.Length);
                        foreach (var b in boolArray) {
                            writer.Write(b);
                        }
                        break;
                    // Expect ushort array
                    case ServerParams.Scores:
                        var scores = (int[])item.Value;
                        writer.Write((ushort)scores.Length);
                        foreach (var score in scores) {
                            writer.Write((ushort)score);
                        }
                        break;
                    // Expect int array
                    case ServerParams.PlayerIds:
                        var playerIds = (int[])item.Value;
                        writer.Write((ushort)playerIds.Length);
                        foreach (var id in playerIds) {
                            writer.Write(id);
                        }
                        break;
                    // Expect string array
                    case ServerParams.Usernames:
                        var usernames = (string[])item.Value;
                        writer.Write((ushort)usernames.Length);
                        foreach (var username in usernames) {
                            writer.Write(username);
                        }
                        break;
                    // Expect (ushort, ushort) array
                    case ServerParams.Positions:
                        var positions = (Position[])item.Value;
                        writer.Write((ushort)positions.Length);
                        foreach (var pos in positions) {
                            writer.Write((ushort)pos.X);
                            writer.Write((ushort)pos.Y);
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported key: {item.Key}");
                }
            }
        }
    }
}