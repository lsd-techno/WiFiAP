using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Threading;
using System.Net;
using nanoFramework.Runtime.Native;

namespace WiFiAP
{
    public class WebServer
    {
        HttpListener _listener;
        Thread _serverThread;

        public void Start()
        {
            if (_listener == null)
            {
                _listener = new HttpListener("http");
                _serverThread = new Thread(RunServer);
                _serverThread.Start();
            }
        }

        public void Stop()
        {
            if (_listener != null)
                _listener.Stop();
        }
        private void RunServer()
        {
            _listener.Start();

            while (_listener.IsListening)
            {
                var context = _listener.GetContext();
                if (context != null)
                    ProcessRequest(context);
            }
            _listener.Close();

            _listener = null;
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string responseString;
            string ssid = null;
            string password = null;
            bool isApSet = false;

            Debug.WriteLine($"reqest methode: {request.HttpMethod}");
            Debug.WriteLine($"reqest url: {request.RawUrl}");
            string[] url = request.RawUrl.Split('?');
            switch (request.HttpMethod)
            {
                case "GET":
                    if (url[0] == "/favicon.ico")
                    {
                        response.ContentType = "image/png";
                        byte[] responseBytes = Resources.GetBytes(Resources.BinaryResources.favicon);
                        OutPutByteResponse(response, responseBytes);
                    }
                    else
                    {
                        response.ContentType = "text/html";
                        if (url[0] == "/config")
                        {
                            responseString = ReplaceMessage(Resources.GetString(Resources.StringResources.config), "", "");
                        }
                        else
                        {
                            responseString = ReplaceMessage(Resources.GetString(Resources.StringResources.main), $"{DateTime.UtcNow.AddHours(8)}", "");
                        }
                        OutPutResponse(response, responseString);
                    }
                    break;

                case "POST":
                    if (url[0] == "/config")
                    {
                        // Pick up POST parameters from Input Stream
                        Hashtable hashPars = ParseParamsFromStream(request.InputStream);

                        ssid = (string)hashPars["ssid"];
                        password = (string)hashPars["password"];

                        Debug.WriteLine($"Wireless parameters SSID:{ssid} PASSWORD:{password}");

                        string message = "<p>SSID can not be empty</p>";
                        if (ssid != null)
                        {
                            if (ssid.Length >= 1)
                            {
                                message = "<p>New settings saved.</p><p>Rebooting device to put into normal mode</p>";

                                //responseString = CreateMainPage(message);

                                isApSet = true;
                            }
                        }
                        responseString = ReplaceMessage(Resources.GetString(Resources.StringResources.config), message, ssid);
                    }
                    else
                    {
                        responseString = ReplaceMessage(Resources.GetString(Resources.StringResources.main), $"{DateTime.UtcNow.AddHours(8)}", "");
                    }
                    OutPutResponse(response, responseString);
                    break;
            }

            response.Close();

            if (isApSet && (!string.IsNullOrEmpty(ssid)) && (!string.IsNullOrEmpty(password)))
            {
                // Enable the Wireless station interface
                Wireless80211.Configure(ssid, password);

                // Disable the Soft AP
                WirelessAP.Disable();
                Thread.Sleep(200);
                Power.RebootDevice();
            }
        }
        public static class MyTags
        {
            public static string ssid = "{ssid}";
            public static string message = "{message}";
        }
        static string ReplaceMessage(string page, string message, string ssid)
        {
            string retpage;
            int index = page.IndexOf(MyTags.ssid);
            if (index >= 0)
            {
                retpage = page.Substring(0, index) + ssid + page.Substring(index + MyTags.ssid.Length);
            }
            else
            {
                retpage = page;
            }

            index = retpage.IndexOf(MyTags.message);
            if (index >= 0)
            {
                return retpage.Substring(0, index) + message + retpage.Substring(index + MyTags.message.Length);
            }

            return retpage;
        }

        static void OutPutResponse(HttpListenerResponse response, string responseString)
        {
            var responseBytes = System.Text.Encoding.UTF8.GetBytes(responseString);
            OutPutByteResponse(response, System.Text.Encoding.UTF8.GetBytes(responseString));
        }
        static void OutPutByteResponse(HttpListenerResponse response, Byte[] responseBytes)
        {
            response.ContentLength64 = responseBytes.Length;
            response.OutputStream.Write(responseBytes, 0, responseBytes.Length);

        }

        static Hashtable ParseParamsFromStream(Stream inputStream)
        {
            byte[] buffer = new byte[inputStream.Length];
            inputStream.Read(buffer, 0, (int)inputStream.Length);

            return ParseParams(System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length));
        }

        static Hashtable ParseParams(string rawParams)
        {
            Hashtable hash = new Hashtable();

            string[] parPairs = rawParams.Split('&');
            foreach (string pair in parPairs)
            {
                string[] nameValue = pair.Split('=');
                hash.Add(nameValue[0], nameValue[1]);
            }

            return hash;
        }
        static string CreateMainPage(string message)
        {

            return "<!DOCTYPE html><html><body>" +
                    "<h1>NanoFramework</h1>" +
                    "<form method='POST'>" +
                    "<fieldset><legend>Wireless configuration</legend>" +
                    "Ssid:</br><input type='input' name='ssid' value='' ></br>" +
                    "Password:</br><input type='password' name='password' value='' >" +
                    "<br><br>" +
                    "<input type='submit' value='Save'>" +
                    "</fieldset>" +
                    "<b>" + message + "</b>" +
                    "</form></body></html>";
        }
    }
}
