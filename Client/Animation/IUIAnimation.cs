using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;

using Client.Component;

namespace Client.Animation {
    public interface IUIAnimation {
        void OnStart(IComponent component);
        void Update(IComponent component, GameTime gameTime);
        bool IsFinished { get; }
    }
}