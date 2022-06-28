using System;
using System.Diagnostics;
using System.Threading;
using System.Net.NetworkInformation;
using nanoFramework.Runtime.Native;
using System.Device.Gpio;
using nanoFramework.Networking;

namespace WiFiAP
{
    public class Program
    {
        // Start Simple WebServer
        static WebServer server = new WebServer();

        // Prepare Simple DHCPServer
        static DHCPserver dhcpserver = new DHCPserver();

        // Connected Station count
        static int connectedCount = 0;

        const int LED_PIN = 2;//for regular boards
        //const int LED_PIN = 16;//for board with battery
        // GPIO pin used to put device into AP set-up mode
        const int SETUP_PIN = 5;

        static GpioPin led;
        public static void Main()
        {
            Debug.WriteLine("Welcome to nF...");
            Debug.WriteLine($"Main FW: {SystemInfo.Version}");

            GpioPin setupButton = new GpioController().OpenPin(SETUP_PIN, PinMode.InputPullUp);

            led = new GpioController().OpenPin(LED_PIN, PinMode.Output);
            led.Write(PinValue.High);
            Timer AliveLed = new Timer(CheckStatusTimerCallback, null, 500, 1000);
            // If Wireless station is not enabled then start Soft AP to allow Wireless configuration
            // or Button pressed
            if (!Wireless80211.IsEnabled() || (setupButton.Read() == PinValue.Low))
            {

                Wireless80211.Disable();
                if (WirelessAP.Setup() == false)
                {
                    // Reboot device to Activate Access Point on restart
                    Debug.WriteLine($"Setup Soft AP, Rebooting device");
                    Power.RebootDevice();
                }

                Debug.WriteLine($"Running Soft AP, waiting for client to connect");
                Debug.WriteLine($"Soft AP IP address :{WirelessAP.GetIP()}");


                if (true)
                {
                    // Link up Network event to show Stations connecting/disconnecting to Access point.
                    NetworkChange.NetworkAPStationChanged += NetworkChange_NetworkAPStationChanged;
                }
                else
                {
                    // Now that the normal Wifi is deactivated, that we have setup a static IP
                    // We can start the Web server
                    server.Start();
                }
            }
            else
            {
                Debug.WriteLine($"Running in normal mode, connecting to Access point");
                var conf = Wireless80211.GetConfiguration();
                bool success;
                // For devices like STM32, the password can't be read
                if (string.IsNullOrEmpty(conf.Password))
                {
                    // In this case, we will let the automatic connection happen
                    success = WifiNetworkHelper.Reconnect(requiresDateTime: true, token: new CancellationTokenSource(60000).Token);
                    //success = NetworkHelper.ReconnectWifi(setDateTime: true, token: new CancellationTokenSource(60000).Token);
                }
                else
                {
                    // If we have access to the password, we will force the reconnection
                    // This is mainly for ESP32 which will connect normaly like that.
                    success = WifiNetworkHelper.ConnectDhcp(conf.Ssid, conf.Password, requiresDateTime: true, token: new CancellationTokenSource(60000).Token);
                    //success = NetworkHelper.ConnectWifiDhcp(conf.Ssid, conf.Password, token: new CancellationTokenSource(60000).Token);
                }
                Debug.WriteLine($"Connection is {success}");
                if (success)
                {
                    string IpAdr = Wireless80211.WaitIP();
                    Debug.WriteLine($"Connected as {IpAdr}");
                    // We can even wait for a DateTime now
                    //success = NetworkHelper.WaitForValidIPAndDate(true, NetworkInterfaceType.Wireless80211, new CancellationTokenSource(60000).Token);
                    Thread.Sleep(100);
                    success = (WifiNetworkHelper.Status == NetworkHelperStatus.NetworkIsReady);
                    if (success)
                    {
                        if (DateTime.UtcNow.Year > DateTime.MinValue.Year)
                        {
                            Debug.WriteLine($"We have a valid date: {DateTime.UtcNow}");
                            Debug.WriteLine($"We have a valid date: +8 {DateTime.UtcNow.AddHours(8)}");
                        }
                        else
                        {
                            Debug.WriteLine($"We have a invalid date!!! ( {DateTime.UtcNow} )");
                        }
                        Debug.WriteLine($"Starting http server...");
                        server.Start();
                    }
                }
                else
                {
                    Debug.WriteLine($"Something wrong happened, can't connect at all");
                }
            }


            // Just wait for now
            // Here you would have the reset of your program using the client WiFI link
            Thread.Sleep(Timeout.Infinite);
        }
        private static void CheckStatusTimerCallback(object state)
        {
            led.Toggle();
        }

        /// <summary>
        /// Event handler for Stations connecting or Disconnecting
        /// </summary>
        /// <param name="NetworkIndex">The index of Network Interface raising event</param>
        /// <param name="e">Event argument</param>
        private static void NetworkChange_NetworkAPStationChanged(int NetworkIndex, NetworkAPStationEventArgs e)
        {
            Debug.WriteLine($"NetworkAPStationChanged event Index:{NetworkIndex} Connected:{e.IsConnected} Station:{e.StationIndex} ");

            // if connected then get information on the connecting station 
            if (e.IsConnected)
            {
                WirelessAPConfiguration wapconf = WirelessAPConfiguration.GetAllWirelessAPConfigurations()[0];
                WirelessAPStation station = wapconf.GetConnectedStations(e.StationIndex);

                string macString = BitConverter.ToString(station.MacAddress);
                Debug.WriteLine($"Station mac {macString} Rssi:{station.Rssi} PhyMode:{station.PhyModes} ");

                connectedCount++;

                // Start web server when it connects otherwise the bind to network will fail as 
                // no connected network. Start web server when first station connects 
                if (connectedCount == 1)
                {
                    // Wait for Station to be fully connected before starting web server
                    // other you will get a Network error
                    Thread.Sleep(2000);
                    dhcpserver.Start();
                    server.Start();
                }
            }
            else
            {
                // Station disconnected. When no more station connected then stop web server
                if (connectedCount > 0)
                {
                    connectedCount--;
                    if (connectedCount == 0)
                    {
                        dhcpserver.Stop();
                        server.Stop();
                    }
                }
            }
        }
    }
}
