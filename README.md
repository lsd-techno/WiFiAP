based on
# WiFi Soft AP sample
from nanoframework

Major changes are:
1. changed to use with ESP32
2. changed to nanoFramework.System.Device.WiFi.1.4.0-preview.6 package naming style
3. add very dirty implementation of DHCP server when in AP mode to make it easy to connect from mobile
   * - there is some limitations in DHCP it is not store any client address information, whenever make new connection it will assign next IP address
   * - first dynamic IP address start with next to AP IP address
   * - it is not sending gateway IP to Client to not block mobile device network connection while connected to AP
   * - there is some changes in WebServer implementation WiFi config moved to http://192.168.4.1/config

-- rest part of readme is not changed

Shows how to use various APIs related with WiFi Soft AP. Starts a Soft AP (Hot spot) and runs a simple web server to configure the Wireless connection parameters.

The sample uses GPIO pin 5 to switch back into the SoftAP configuration mode. When pulled to ground on boot will switch into Soft AP mode.  
You can connect to the web server via the url http://192.168.4.1/

Once you save the wireless configuration the next boot the device will automatically connect to that Access Point
and the Soft AP will be disabled.

## Hardware requirements

An hardware device with WiFi networking capabilities running a nanoFramework image.

## Related topics

### Reference

- [SYstem.Net.NetworkInformation](http://docs.nanoframework.net/api/System.Net.NetworkInformation.html)

## Build the sample

1. Start Microsoft Visual Studio 2019 (VS 2017 should be OK too) and select `File > Open > Project/Solution`.
1. Starting in the folder where you unzipped the samples/cloned the repository, go to the subfolder for this specific sample. Double-click the Visual Studio Solution (.sln) file.
1. Press `Ctrl+Shift+B`, or select `Build > Build Solution`.

## Run the sample

The next steps depend on whether you just want to deploy the sample or you want to both deploy and run it.

### Deploying the sample

- Select `Build > Deploy Solution`.

### Deploying and running the sample

- To debug the sample and then run it, press F5 or select `Debug > Start Debugging`.

> **Important**: Before deploying or running the sample, please make sure your device is visible in the Device Explorer.

> **Tip**: To display the Device Explorer, go to Visual Studio menus: `View > Other Windows > Device Explorer`.
