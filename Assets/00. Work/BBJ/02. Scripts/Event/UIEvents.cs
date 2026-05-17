using Gamelib.EventSystem;

namespace BBJ.EventSystem
{
    public class MessageEvent : GameEvent
    {
        public string Message { get; }
        public MessageEvent(string message) => Message = message;
    }
}
