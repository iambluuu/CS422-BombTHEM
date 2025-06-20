using Shared;
using Shared.PacketWriter;

namespace Server.PowerHandler {
    public class ShieldHandler : PowerUpHandler {
        public override PowerName PowerName => PowerName.Shield;
        public override Dictionary<byte, object>? Apply(ServerMap map, int playerId, Dictionary<string, object>? parameters = null, int slotNum = -1) {
            base.Apply(map, playerId, parameters, slotNum);
            // if already active, renew time and signal the client not to change anything
            foreach (var activePowerUp in map.PlayerInfos[playerId].ActivePowerUps) {
                // Console.WriteLine($"Active power: {activePowerUp.PowerType}");
                if (activePowerUp.PowerType == PowerName.Shield) {
                    activePowerUp.StartTime = DateTime.Now;
                    activePowerUp.SlotNum = slotNum;
                    return new Dictionary<byte, object> {
                        { (byte)ServerParams.PlayerId, playerId },
                        { (byte)ServerParams.NeedToChange, false }
                    };
                }
            }

            // if not active, add it to the list and signal the client to display the shield
            map.PlayerInfos[playerId].ActivePowerUps.Add(new ActivePowerUp(PowerName.Shield, DateTime.Now, slotNum));
            return new Dictionary<byte, object> {
                { (byte)ServerParams.PlayerId, playerId },
                { (byte)ServerParams.NeedToChange, true }
            };
        }
    }
}