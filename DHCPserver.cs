using System;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using nanoFramework.Networking;
using DHCPSharp.Common;
using DHCPSharp.Common.Enums;

namespace WiFiAP
{
    public class DHCPserver
    {
        // Constants
        private const int DHCP_PORT = 67;
        private const int DHCP_CLIENT_PORT = 68;

        //
        byte[] dhcp_ip;
        static Socket _listenerDHCP;
        static Socket _sender;
        Thread _DHCPserverThread;
        bool listening;
        public bool Start()
        {
            if (_listenerDHCP == null)
            {
                try
                {
                    //listen socket
                    _listenerDHCP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    IPAddress dsip = new IPAddress(0xFFFFFFFF);
                    IPEndPoint ep = new IPEndPoint(dsip, DHCP_PORT);
                    _listenerDHCP.Bind(ep);
                    //send socket
                    _sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    _sender.Bind(new IPEndPoint(IPAddress.Parse(WirelessAP.GetIP()), 0));
                    _sender.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.Broadcast, true);
                    _sender.Connect(new IPEndPoint(IPAddress.Parse("255.255.255.255"), DHCP_CLIENT_PORT));

                    //make dirty dynamic ip pool
                    dhcp_ip = IPAddress.Parse(WirelessAP.GetIP()).GetAddressBytes();

                    //start server thread
                    _DHCPserverThread = new Thread(RunServer);
                    _DHCPserverThread.Start();
                    return true;
                }
                catch (SocketException ex)
                {
                    Debug.WriteLine($"DHCP: ** Socket exception occurred: {ex.Message} error code {ex.ErrorCode}!**");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DHCP: ** Exception occurred: {ex.Message}!**");
                }
                return false;
            }
            return true;
        }
        public void Stop()
        {
            listening = false;
        }
        private void RunServer()
        {
            listening = true;

            // setup buffer to read data from socket
            byte[] buffer = new byte[1024];

            Debug.WriteLine($"DHCP: start listen...");
            //_listener.Listen(1);
            while (listening)
            {
                //check if socket have any bytes to read
                int bytes = _listenerDHCP.Available;

                if (bytes > 0)
                {
                    Debug.WriteLine($"DHCP: Have {bytes} bytes");
                    bytes = _listenerDHCP.Receive(buffer);
                    Debug.WriteLine($"DHCP: <- Read {bytes} bytes from {(IPEndPoint)_listenerDHCP.LocalEndPoint }");
                    // we have data!
                    // output as string
                    //Debug.WriteLine(new String(Encoding.UTF8.GetChars(buffer, 0, bytes)));
                    //Debug.WriteLine(BitConverter.ToString(buffer, 0, bytes));
                    DhcpMessage dhcp_req = new DhcpMessage();
                    dhcp_req.parse(ref buffer);
                    string sname = dhcp_req.HostName;
                    switch (dhcp_req.DhcpMessageType)
                    {
                        case (DhcpMessageType.Discover):
                            {
                                //increment dynamic ip
                                if (dhcp_ip[3] < 254)
                                { dhcp_ip[3]++; }
                                else
                                { dhcp_ip[3] = 2; }
                                Debug.WriteLine($"DHCP: Discover from host: {sname}");
                                _sender.Send(dhcp_req.offer(new IPAddress(dhcp_ip).ToString(), "255.255.255.0", WirelessAP.GetIP()));
                                Debug.WriteLine($"DHCP: -> Sent Offer {dhcp_req.DHCPpacketSize} bytes to client");
                                //Debug.WriteLine(BitConverter.ToString(dhcp_req.offer()));
                                break;
                            }
                        case (DhcpMessageType.Request):
                            {
                                Debug.WriteLine($"DHCP: Request from host: {sname}");
                                Debug.WriteLine($"DHCP Request: Requested address {dhcp_req.RequestedIpAddress}");
                                Debug.WriteLine($"DHCP Request: Server Identifier {dhcp_req.DhcpAddress}");
                                string confirmip = dhcp_req.RequestedIpAddress;
                                _sender.Send(dhcp_req.ack(confirmip, "255.255.255.0", WirelessAP.GetIP()));
                                //Debug.WriteLine(BitConverter.ToString(buffer, 0, bytes));
                                break;
                            }
                        default:
                            {
                                Debug.WriteLine($"DHCP: Unknown ({dhcp_req.DhcpMessageType}) from host: {sname}");
                                break;
                            }
                    }
                }
                else
                {
                    //free cpu time if no bytes in socket
                    Thread.Sleep(100);
                }

            }
            try
            {
                _listenerDHCP.Close();
                _sender.Close();
            }
            catch { }

            _listenerDHCP = null;
            _sender = null;
            Debug.WriteLine($"DHCP: stoped");
        }
    }
}
