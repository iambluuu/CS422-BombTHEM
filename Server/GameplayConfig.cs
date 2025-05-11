
namespace Server {
    public static class GameplayConfig {
        public static int MaxPlayers { get; set; } = 4;
        public static int MaxScore { get; set; } = 10;
        public static int GameDuration { get; set; } = 300; // in seconds
        public static int RespawnTime { get; set; } = 5; // in seconds
        public static float BombDuration { get; set; } = 2000f; // in milliseconds
        public static int PowerUpDuration { get; set; } = 10; // in seconds
        public static float PowerUpSpawnChance { get; set; } = 0.25f; // 10% chance to spawn a power-up
        public static float ItemExistDuration { get; set; } = 20000f; // stays for 20 seconds before disappearing
    }
}