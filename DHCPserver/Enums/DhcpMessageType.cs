namespace DHCPSharp.Common.Enums
{
    public enum DhcpMessageType : byte
    {
        Unknown = 0x00,
        Discover,
        Offer,
        Request,
        Decline,
        Ack,
        Nak,
        Release,
        Inform,
        ForceRenew,
        LeaseQuery,
        LeaseUnassigned,
        LeaseUnknown,
        LeaseActive
    }
}
