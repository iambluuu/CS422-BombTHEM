
namespace Shared {
    public static class GameplayConfig {
        public static int MaxPlayers { get; set; } = 4;
        public static int GameDuration { get; set; } = 300; // in seconds
        public static int RespawnTime { get; set; } = 5; // in seconds
        public static int MaxBombs { get; set; } = 3;
        public static float BombDuration { get; set; } = 2000f; // in milliseconds
        public static float PowerUpDuration { get; set; } = 5000f;// in milliseconds
        public static float PowerUpSpawnChance { get; set; } = 0.25f; // 10% chance to spawn a power-up
        public static float ItemExistDuration { get; set; } = 20000f; // stays for 20 seconds before disappearing
    }
}