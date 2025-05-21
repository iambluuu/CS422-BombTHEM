
namespace Shared {
    public static class GameplayConfig {
        public static int MaxPlayers { get; set; } = 4;
        public static int GameDuration { get; set; } = 300; // in seconds
        public static int RespawnTime { get; set; } = 5; // in seconds
        public static int MaxBombs { get; set; } = 3;
        public static float BombDuration { get; set; } = 2000f; // in milliseconds
        // public static float PowerUpDuration { get; set; } = 5000f;// in milliseconds
        public static float PowerUpSpawnChance { get; set; } = 0.25f; // 10% chance to spawn a power-up
        public static float ItemExistDuration { get; set; } = 20000f; // stays for 20 seconds before disappearing
        public static readonly Dictionary<PowerName, float> PowerUpDurations = new Dictionary<PowerName, float> {
            { PowerName.MoreBombs, 5000f },
            { PowerName.Shield, 5000f },
            { PowerName.Nuke, -1},
        };

        public static readonly Dictionary<PowerName, int> PowerUpQuantity = new Dictionary<PowerName, int> {
            { PowerName.MoreBombs, 1 },
            { PowerName.Shield, 1 },
            { PowerName.Teleport, 1 },
            { PowerName.Nuke, 3 },
        };
    }

};