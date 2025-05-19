using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Server;
using Shared;

namespace Client.PowerUps {
    public class Nuke(MapRenderInfo map) : PowerUp {
        public override PowerName PowerName => PowerName.Nuke;

        public override void Apply(Dictionary<string, object> parameters) {
            if (!parameters.TryGetValue("x", out var xObj) || !parameters.TryGetValue("y", out var yObj) || !parameters.TryGetValue("byPlayerId", out var byPlayerIdObj)) {
                return;
            }

            if (!int.TryParse(xObj.ToString(), out int x) || !int.TryParse(yObj.ToString(), out int y) || !int.TryParse(byPlayerIdObj.ToString(), out int byPlayerId)) {
                return;
            }

            map.UnlockTile(x, y);
            if (parameters.TryGetValue("invalid", out var invalid)) {
                if (bool.TryParse(invalid.ToString(), out bool isInvalid) && isInvalid) {
                    return;
                }
            }

            BombType type = Enum.Parse<BombType>(parameters["type"].ToString());
            bool isCounted = bool.Parse(parameters["isCounted"].ToString());
            map.BombPlaced(x, y, type, byPlayerId, isCounted);
        }

        public override Dictionary<string, object> Use() {
            Position nearestCell = map.GetNearestCell();
            if (nearestCell != null) {
                if (map.BombCount >= GameplayConfig.MaxBombs && !map.HasActivePowerUp(PowerName.MoreBombs) && !map.LockTile(nearestCell.X, nearestCell.Y)) {
                    return null;
                }

                return new() {
                    { "x", nearestCell.X.ToString() },
                    { "y", nearestCell.Y.ToString() },
                };
            }

            return null;
        }
    }
}