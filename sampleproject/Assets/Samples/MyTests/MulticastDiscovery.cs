using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace Network
{
    public class ReceiveData
    {
        public ReceiveData (IPAddress i, ushort s, bool b)
        {
            Ip = i;
            Port = s;
            IsServer = b;
        }

        public IPAddress Ip;
        public ushort Port;
        public bool IsServer;
    }

    /// <summary>
    /// UDP Multicast service
    /// Developed and tested on Windows and Android platform
    /// </summary>
    public class MulticastDiscovery : MonoBehaviour
    {
        public static MulticastDiscovery Instance;

        public delegate void ReceiveEvent(ReceiveData data);
        public event ReceiveEvent OnReceiveEvent;

        /// <summary>
        /// I choosed this IP based on wiki, where is marked as "Simple Service Discovery Protocol address"
        /// <see cref="https://en.wikipedia.org/wiki/Multicast_address"/>
        /// </summary>
        public string ip = "239.255.255.250";
        public int port;
        public bool log;

        bool keepThreads = true;
        public bool isHost = false;
        public bool ForceClient = false;

        AndroidJavaObject multicastLock;
        Queue<ReceiveData> received = new Queue<ReceiveData>();
        List<UdpClient> clients = new List<UdpClient>();

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            isHost = CheckIfHost();
            DebugManager.Instance.Print("Is Host: " + isHost);
            //if (Application.platform == RuntimePlatform.Android)
            //{
            //    MulticastLock();
            //}
            InitializeClients();
            StartCoroutine(ProcessReceived());

            if (Debug.isDebugBuild && log)
                StartCoroutine(ProcessErrors());
        }

        bool CheckIfHost()
        {
            return !XRDevice.isPresent && Application.platform != RuntimePlatform.Android && !ForceClient;
        }

        Queue<string> errors = new Queue<string>();

        IEnumerator ProcessErrors()
        {
            while (true)
            {
                yield return new WaitUntil(() => errors.Count > 0);
                string s = errors.Dequeue();
                Debug.Log(s);
                DebugManager.Instance.Print(s);
            }
        }

        /// <summary>
        /// Handle received IP addresses
        /// </summary>
        IEnumerator ProcessReceived()
        {
            while (true)
            {
                yield return new WaitUntil(() => received.Count > 0);
                OnReceiveEvent?.Invoke(received.Dequeue());
            }
        }

        /// <summary>
        /// Create UDP clients
        /// </summary>
        void InitializeClients()
        {
            if (!IPAddress.TryParse(ip, out IPAddress destination))
            {
                Debug.LogError("Wrong IP address format");
                return;
            }

            foreach (IPAddress local in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (local.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                var client = new UdpClient(AddressFamily.InterNetwork);
                //IPAddress multicastIpAddress = IPAddress.Parse("239.255.255.255");
                //client.JoinMulticastGroup(multicastIpAddress);
                client.ExclusiveAddressUse = false;
                client.MulticastLoopback = false;
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                //On linux we bind to 0.0.0.0 instead of the multicast port
                if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    client.Client.Bind(new IPEndPoint(local, port));
                    DebugManager.Instance.Print("Bind to: " + local);
                }
                else
                {
                    if (!IPAddress.TryParse("0.0.0.0", out IPAddress ip_linux))
                    {
                        DebugManager.Instance.Print("Wrong ip format for linux");
                        return;
                    }
                    client.Client.Bind(new IPEndPoint(ip_linux, port));
                    DebugManager.Instance.Print("Bind to: " + ip_linux);
                }

                client.JoinMulticastGroup(destination, local);
                clients.Add(client);

                new Thread(() =>
                {
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, port);

                    while (keepThreads)
                    {
                        try
                        {
                            byte[] b = client.Receive(ref from);
                            ReceiveData d = new ReceiveData(from.Address, (ushort)port, isHost);
                            if (!received.Contains(d))
                            {
                                received.Enqueue(d);                              
                                errors.Enqueue("Received " + from.Address);
                            }
                        }
                        catch (Exception e)
                        {
                            errors.Enqueue("Error receiving " + e.Message);
                            //background thread, you can't use Debug.Log
                        }

                        Thread.Sleep(1000);
                    }
                })
                {
                    IsBackground = true,
                    Priority = System.Threading.ThreadPriority.BelowNormal
                }.Start();

                new Thread(() =>
                {
                    IPEndPoint to = new IPEndPoint(destination, port);
                    var data = System.Text.Encoding.UTF8.GetBytes("HELLO");

                    while (keepThreads)
                    {
                        //You can add some condition here to broadcast only if it's needed, like app is running as server
                        //if (isHost)
                        //{                          
                            try
                            {
                                client.Send(data, data.Length, to);
                                errors.Enqueue("Sended");
                            }
                            catch (Exception e)
                            {
                                errors.Enqueue("Error sending " + e.Message);
                                //background thread, you can't use Debug.Log
                            }
                            Thread.Sleep(1000);
                        //}
                    }
                })
                {
                    IsBackground = true,
                    Priority = System.Threading.ThreadPriority.BelowNormal
                }.Start();
            }
        }

        /// <summary>
        /// Clean up at end
        /// </summary>
        private void OnDisable()
        {
            foreach (UdpClient client in clients)
                client.Close();

            multicastLock?.Call("release");
            keepThreads = false;
        }

        /// <summary>
        /// If you have problems with multicast lock on android, this method can help you
        /// </summary>
        void MulticastLock()
        {
            using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
                {
                    multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "lock");
                    multicastLock.Call("acquire");
                }
            }
        }
    }
}
