using System;
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using DHCPSharp.Common;
using DHCPSharp.Common.Enums;

namespace DHCPSharp.Common
{
    public class DhcpMessage
    {
        public int DHCPpacketSize = 300;
        public DhcpOperation OperationCode { get; set; }
        public HardwareType HardwareType { get; set; }
        public byte HardwareAddressLength { get; set; }
        public byte Hops { get; set; }
        public UInt32 TransactionId { get; set; }
        public UInt16 SecondsElapsed { get; set; }
        public UInt16 Flags { get; set; }
        public IPAddress ClientIPAddress { get; set; }
        public IPAddress YourIPAddress { get; set; }
        public IPAddress ServerIPAddress { get; set; }
        public IPAddress GatewayIPAddress { get; set; }
        //public PhysicalAddress ClientHardwareAddress { get; set; }
        public byte[] ClientHardwareAddress { get; set; }
        public byte[] sName { get; set; }
        public byte[] File { get; set; }
        public byte[] Cookie { get; set; }
        //public Dictionary<DhcpOptionCode, byte[]> Options { get; set; }

        public byte[] Options { get; set; }

        public void parse(ref byte[] dhcppacket)
        {
            OperationCode = (DhcpOperation)dhcppacket[0];
            HardwareType = (HardwareType)dhcppacket[1];
            HardwareAddressLength = dhcppacket[2];
            Hops = dhcppacket[3];
            TransactionId = BitConverter.ToUInt32(dhcppacket, 4);
            SecondsElapsed = BitConverter.ToUInt16(dhcppacket, 8);
            Flags = BitConverter.ToUInt16(dhcppacket, 10);
            ClientIPAddress = new IPAddress(BitConverter.GetBytes(BitConverter.ToUInt32(dhcppacket, 12)));
            YourIPAddress = new IPAddress(BitConverter.GetBytes(BitConverter.ToUInt32(dhcppacket, 16)));
            ServerIPAddress = new IPAddress(BitConverter.GetBytes(BitConverter.ToUInt32(dhcppacket, 20)));
            GatewayIPAddress = new IPAddress(BitConverter.GetBytes(BitConverter.ToUInt32(dhcppacket, 24)));
            ClientHardwareAddress = new byte[HardwareAddressLength];//source field 16 bytes
            Array.Copy(dhcppacket, 28, ClientHardwareAddress, 0, HardwareAddressLength);
            //skip
            //sName = new byte[64];
            //File = new byte[128];
            Cookie = new byte[4];
            Array.Copy(dhcppacket, 236, Cookie, 0, 4);
            //check copy options array
            int optoffset = 240;
            int offset = optoffset;
            if (dhcppacket[offset] != 0)
            {
                while (dhcppacket[offset] != 0xff)
                {
                    byte optcode = dhcppacket[offset++];
                    int optlen = dhcppacket[offset++];
                    offset += optlen;
                }
                Options = new byte[offset - optoffset + 1];
                Array.Copy(dhcppacket, optoffset, Options, 0, Options.Length);
            }
        }
        public byte[] build()
        {
            byte[] dhcppacket = new byte[DHCPpacketSize];
            dhcppacket[0] = (byte)OperationCode;
            dhcppacket[1] = (byte)HardwareType;
            dhcppacket[2] = HardwareAddressLength;
            dhcppacket[3] = Hops;
            BitConverter.GetBytes(TransactionId).CopyTo(dhcppacket, 4);
            BitConverter.GetBytes(SecondsElapsed).CopyTo(dhcppacket, 8);
            BitConverter.GetBytes(Flags).CopyTo(dhcppacket, 10);
            ClientIPAddress.GetAddressBytes().CopyTo(dhcppacket, 12);
            YourIPAddress.GetAddressBytes().CopyTo(dhcppacket, 16);
            ServerIPAddress.GetAddressBytes().CopyTo(dhcppacket, 20);
            GatewayIPAddress.GetAddressBytes().CopyTo(dhcppacket, 24);
            ClientHardwareAddress.CopyTo(dhcppacket, 28);
            //skip
            //sName = new byte[64];
            //File = new byte[128];
            Cookie.CopyTo(dhcppacket, 236);
            Options.CopyTo(dhcppacket, 240);
            //dhcppacket[240 + Options.Length] = 0xff;//will do it in Options builder
            return dhcppacket;
        }
        public void resetOptions()
        {
            Options = new byte[DHCPpacketSize - 240];
            Options[0] = 0xff;
        }
        public void addOption(ref byte[] optdata)
        {
            int offset = 0;
            while (Options[offset] != 0xff)
            {
                byte optcode = Options[offset++];
                int optlen = Options[offset++];
                offset += optlen;
            }
            optdata.CopyTo(Options, offset);
            Options[offset + optdata.Length] = 0xff;//set end of options
        }
        public void addOpt(DhcpOptionCode optType, byte[] optData)
        {
            byte[] optTyData = new byte[2+optData.Length];
            optTyData[0] = (byte)optType;
            optTyData[1] = (byte)optData.Length;
            optData.CopyTo(optTyData, 2);
            addOption(ref optTyData);
        }
        private byte[] build_type(DhcpMessageType acktype, string cip, string mask, string sip)
        {
            OperationCode = DhcpOperation.BootReply;
            //YourIPAddress = IPAddress.Parse("192.168.4.2");
            YourIPAddress = IPAddress.Parse(cip);
            resetOptions();
            addOpt(DhcpOptionCode.DhcpMessageType, new byte[] { (byte)acktype });
            //addOpt(DhcpOptionCode.SubnetMask, IPAddress.Parse("255.255.255.0").GetAddressBytes());
            //addOpt(DhcpOptionCode.DhcpAddress, IPAddress.Parse("192.168.4.1").GetAddressBytes());
            addOpt(DhcpOptionCode.SubnetMask, IPAddress.Parse(mask).GetAddressBytes());
            addOpt(DhcpOptionCode.DhcpAddress, IPAddress.Parse(sip).GetAddressBytes());
            return build();
        }
        public byte[] offer(string cip, string mask, string sip)
        {
            return build_type(DhcpMessageType.Offer, cip, mask, sip);
            /*
            OperationCode = DhcpOperation.BootReply;
            //YourIPAddress = IPAddress.Parse("192.168.4.2");
            YourIPAddress = IPAddress.Parse(cip);
            resetOptions();
            addOpt(DhcpOptionCode.DhcpMessageType, new byte[] { (byte)DhcpMessageType.Offer });
            //addOpt(DhcpOptionCode.SubnetMask, IPAddress.Parse("255.255.255.0").GetAddressBytes());
            //addOpt(DhcpOptionCode.DhcpAddress, IPAddress.Parse("192.168.4.1").GetAddressBytes());
            addOpt(DhcpOptionCode.SubnetMask, IPAddress.Parse(mask).GetAddressBytes());
            addOpt(DhcpOptionCode.DhcpAddress, IPAddress.Parse(sip).GetAddressBytes());
            return build();
            */
        }
        public byte[] ack(string cip, string mask, string sip)
        {
            return build_type(DhcpMessageType.Ack, cip, mask, sip);
        }

        private int OptionsFindKey(DhcpOptionCode LookOpt)
        {
            int offset = 0;
            if (Options[offset] != 0)
            {
                while (Options[offset] != 0xff)
                {
                    byte optcode = Options[offset++];
                    int optlen = Options[offset++];
                    if ((DhcpOptionCode)optcode == LookOpt)
                    {
                        return offset - 2;
                    }
                    offset += optlen;
                }
            }
            return -1;
        }
        private bool OptionsContainsKey(DhcpOptionCode LookOpt)
        {
            return OptionsFindKey(LookOpt) == -1 ? false : true;
        }
        private byte[] OptionsGetKey(DhcpOptionCode LookOpt)
        {
            int optofs = OptionsFindKey(LookOpt);
            if (optofs == -1)
            { return null; }
            byte[] OptVal = new byte[Options[optofs + 1]];
            Array.Copy(Options, optofs + 2, OptVal, 0, OptVal.Length);
            return OptVal;
        }
        private bool isOptionsInvalid()
        {
            if (Options != null)
            {
                if (Options.Length > 0)
                {
                    return false;
                }
            }
            return true;
        }
        public DhcpMessageType DhcpMessageType
        {
            get
            {
                if (isOptionsInvalid())
                {
                    return DhcpMessageType.Unknown;
                }

                if (OptionsContainsKey(DhcpOptionCode.DhcpMessageType))
                {
                    var data = OptionsGetKey(DhcpOptionCode.DhcpMessageType)[0];
                    return (DhcpMessageType)data;
                }
                return DhcpMessageType.Unknown;
            }
        }
        
        public string HostName
        {
            get
            {
                if (isOptionsInvalid())
                {
                    return string.Empty;
                }
                if (OptionsContainsKey(DhcpOptionCode.Hostname))
                {
                    var data = OptionsGetKey(DhcpOptionCode.Hostname);
                    return Encoding.UTF8.GetString(data, 0, data.Length);
                }
                return string.Empty;
            }
        }

        public string RequestedIpAddress
        {
            get
            {
                if (isOptionsInvalid())
                {
                    return string.Empty;
                }
                if (OptionsContainsKey(DhcpOptionCode.RequestedIpAddress))
                {
                    var data = OptionsGetKey(DhcpOptionCode.RequestedIpAddress);
                    return new IPAddress(data).ToString();
                }
                return string.Empty;
            }
        }
        public string DhcpAddress
        {
            get
            {
                if (isOptionsInvalid())
                {
                    return string.Empty;
                }
                if (OptionsContainsKey(DhcpOptionCode.DhcpAddress))
                {
                    var data = OptionsGetKey(DhcpOptionCode.DhcpAddress);
                    return new IPAddress(data).ToString();
                }
                return string.Empty;
            }
        }
    }
}
