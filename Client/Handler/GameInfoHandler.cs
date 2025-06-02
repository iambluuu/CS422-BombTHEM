using System;

using Shared;
using Client.Scene;
using Client.Screen;
using Client.Audio;

namespace Client.Handler {

    public class GameInfoHandler(MapRenderInfo map) : IHandler {
        public void Handle(NetworkMessage message) {
            string mapString = message.Data["map"];

            int duration = int.Parse(message.Data["duration"]);
            int[] playerIds = Array.ConvertAll(message.Data["playerIds"].Split(';'), int.Parse);
            string[] usernames = message.Data["usernames"].Split(';');
            Position[] playerPositions = Array.ConvertAll(message.Data["playerPositions"].Split(';'), Position.FromString);

            map.InitMap(mapString, playerIds, playerPositions, usernames, duration);
            MusicPlayer.Play("Fight");
            ScreenManager.Instance.StopLoading();
        }
    }
}