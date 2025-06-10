using System;

using Shared;
using Client.Scene;
using Client.Screen;
using Client.Audio;
using Shared.PacketWriter;

namespace Client.Handler {

    public class GameInfoHandler(MapRenderInfo map) : IHandler {
        public void Handle(NetworkMessage message) {
            string mapString = message.Data[(byte)ServerParams.Map] as string;
            int duration = message.Data[(byte)ServerParams.Duration] as int? ?? 0;
            int[] playerIds = message.Data[(byte)ServerParams.PlayerIds] as int[] ?? Array.Empty<int>();
            string[] usernames = message.Data[(byte)ServerParams.Usernames] as string[] ?? Array.Empty<string>();
            Position[] playerPositions = message.Data[(byte)ServerParams.Positions] as Position[] ?? Array.Empty<Position>();
            
            if (mapString == null || playerIds.Length == 0 || usernames.Length == 0 || playerPositions.Length == 0) {
                throw new Exception("Invalid game info data received.");
            }

            map.InitMap(mapString, playerIds, playerPositions, usernames, duration);
            MusicPlayer.Play("Fight");
            ScreenManager.Instance.StopLoading();
        }
    }
}