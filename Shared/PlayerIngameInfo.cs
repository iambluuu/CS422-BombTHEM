

using Shared;

namespace Server {
    public class PlayerIngameInfo {
        public string Name { get; set; }
        public int Score { get; set; }
        public Position Position { get; set; }
        private PowerName[] _powerUps = { PowerName.None, PowerName.None };
        private List<PowerName> _activePowerUp;

        public PlayerIngameInfo(string name, Position position, int score = 0) {
            Name = name;
            Score = score;
            Position = position ?? new Position(0, 0);
            _powerUps = new PowerName[2];
            _activePowerUp = [];
        }

        public bool PickUpItem(PowerName powerUp) {
            for (int i = 0; i < _powerUps.Length; i++) {
                if (_powerUps[i] == PowerName.None) {
                    _powerUps[i] = powerUp;
                    return true;
                }
            }

            return false;
        }

        public bool HasPowerUp(PowerName powerUp) {
            return _powerUps.Contains(powerUp);
        }

        public void UsePowerUp(PowerName powerUp) {
            for (int i = 0; i < _powerUps.Length; i++) {
                if (_powerUps[i] == powerUp) {
                    _powerUps[i] = PowerName.None;
                    _activePowerUp.Add(powerUp);
                    break;
                }
            }
        }
    }
}