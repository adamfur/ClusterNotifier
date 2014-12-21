namespace Notifier
{
    public enum EventType
    {
        PromoteSelf, // become master
        Notify, // something has happend,
        Heartbeat // master tells it's still around
    }
}