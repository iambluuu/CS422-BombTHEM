using Microsoft.Xna.Framework;

using Client.Component;

namespace Client.Animation {
    public interface IUIAnimation {
        void OnStart(IComponent component);
        void Update(IComponent component, GameTime gameTime);
        bool IsFinished { get; }
    }
}
