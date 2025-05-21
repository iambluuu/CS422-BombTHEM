

using Shared;

namespace Server {
    public class PlayerIngameInfo {
        public string Name { get; set; }
        public int Score { get; set; }
        private int _bombCount { get; set; } = 0;
        public Position Position { get; set; }
        private (PowerName, int)[] _powerUps = [(PowerName.None, 0), (PowerName.None, 0)];
        public List<ActivePowerUp> ActivePowerUps { get; set; } = [];

        public PlayerIngameInfo(string name, Position position, int score = 0) {
            Name = name;
            Score = score;
            Position = position ?? new Position(0, 0);
        }

        public bool PickUpItem(PowerName powerUp) {
            for (int i = 0; i < _powerUps.Length; i++) {
                if (_powerUps[i].Item1 == PowerName.None) {
                    _powerUps[i] = new(powerUp, GameplayConfig.PowerUpQuantity[powerUp]);
                    return true;
                }
            }

            return false;
        }

        public bool HasPowerUp(PowerName powerUp) {
            return ActivePowerUps.Exists(p => p.PowerType == powerUp);
        }

        public ActivePowerUp? TryGetActivePowerUp(PowerName powerUp) {
            for (int i = 0; i < ActivePowerUps.Count; i++) {
                if (ActivePowerUps[i].PowerType == powerUp) {
                    return ActivePowerUps[i];
                }
            }
            return null;
        }

        public bool CanUsePowerUp(PowerName powerUp, int slotNum) {
            // Console.WriteLine($"Power at slotNum {slotNum}: {_powerUps[slotNum].Item1}");
            if (slotNum < 0 || slotNum >= _powerUps.Length) {
                return false;
            }

            if (_powerUps[slotNum].Item1 == powerUp && _powerUps[slotNum].Item2 > 0) {
                return true;
            }
            return false;
        }

        public bool UsePowerUp(PowerName powerUp, int slotNum) {
            if (CanUsePowerUp(powerUp, slotNum)) {
                _powerUps[slotNum].Item2--;
                if (_powerUps[slotNum].Item2 == 0) {
                    _powerUps[slotNum] = (PowerName.None, 0);
                }
                return true;
            }

            return false;
        }

        public void DecreaseBombCount() {
            // Console.WriteLine($"Bomb exploded, current bomb count: {_bombCount}");
            _bombCount = Math.Max(_bombCount - 1, 0);
        }

        public void ExpireActivePowerUp(PowerName powerUp, int slotNum = -1) {
            for (int i = 0; i < ActivePowerUps.Count; i++) {
                if (ActivePowerUps[i].PowerType == powerUp && ActivePowerUps[i].SlotNum == slotNum) {
                    ActivePowerUps.RemoveAt(i);
                    break;
                }
            }
        }

        public bool CanPlaceBomb() {
            if (HasPowerUp(PowerName.MoreBombs)) {
                return true;
            }
            if (_bombCount < GameplayConfig.MaxBombs) {
                _bombCount = Math.Min(_bombCount + 1, GameplayConfig.MaxBombs);
                return true;
            }

            return false;
        }
    }
}