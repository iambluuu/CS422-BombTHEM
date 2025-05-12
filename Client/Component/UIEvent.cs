using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Client.Component {
    public enum UIEventType {
        MouseClick,
        MouseOver,
        MouseOut,
        MouseDown,
        MouseUp,
        KeyPress,
        KeyRelease,
        MouseMove,
        MouseWheel,
        Resize,
        FocusIn,
        FocusOut,
        Drag,
        Drop,
        Scroll,
        TextInput,
    }

    public class UIEvent {
        public UIEventType Type { get; }
        public Point MousePosition { get; }
        public Keys Key { get; }
        public char Character { get; }
        public bool CtrlDown { get; }

        public UIEvent(UIEventType type, Point mousePosition = default, Keys key = Keys.None, char character = '\0', bool ctrlDown = false) {
            Type = type;
            MousePosition = mousePosition;
            Key = key;
            Character = character;
            CtrlDown = ctrlDown;
        }
    }
}