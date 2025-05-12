

using Shared;

namespace Server {
    public class PlayerIngameInfo {
        public string Name { get; set; }
        public int Score { get; set; }
        public Position Position { get; set; }
        private PowerName[] _powerUps = { PowerName.None, PowerName.None };
        public List<ActivePowerUp> ActivePowerUps { get; set; } = new List<ActivePowerUp>();

        public PlayerIngameInfo(string name, Position position, int score = 0) {
            Name = name;
            Score = score;
            Position = position ?? new Position(0, 0);
            _powerUps = new PowerName[2];
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

        public bool UsePowerUp(PowerName powerUp) {
            for (int i = 0; i < _powerUps.Length; i++) {
                if (_powerUps[i] == powerUp) {
                    _powerUps[i] = PowerName.None;
                    return true;
                }
            }

            return false;
        }

        public void ExpireActivePowerUp(PowerName powerUp) {
            for (int i = 0; i < ActivePowerUps.Count; i++) {
                if (ActivePowerUps[i].PowerType == powerUp) {
                    ActivePowerUps.RemoveAt(i);
                    break;
                }
            }
        }
    }
}