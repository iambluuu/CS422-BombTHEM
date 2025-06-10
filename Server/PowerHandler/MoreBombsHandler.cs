using Shared;
using Shared.PacketWriter;

namespace Server.PowerHandler {
    public class MoreBombsHandler : PowerUpHandler {
        public override PowerName PowerName => PowerName.MoreBombs;
        public override Dictionary<byte, object>? Apply(Map map, int playerId, Dictionary<string, object>? parameters = null, int slotNum = -1) {
            base.Apply(map, playerId, parameters, slotNum);
            foreach (var activePowerUp in map.PlayerInfos[playerId].ActivePowerUps) {
                if (activePowerUp.PowerType == PowerName.MoreBombs) {
                    activePowerUp.StartTime = DateTime.Now;
                    activePowerUp.SlotNum = slotNum;
                    return new Dictionary<byte, object> {
                        { (byte)ServerParams.PlayerId, playerId },
                        { (byte)ServerParams.NeedToChange, false }
                    };
                }
            }

            map.PlayerInfos[playerId].ActivePowerUps.Add(new ActivePowerUp(PowerName.MoreBombs, DateTime.Now, slotNum));

            return new Dictionary<byte, object> {
                { (byte)ServerParams.PlayerId, playerId },
                { (byte)ServerParams.NeedToChange, true }
            };
        }
    }
}