using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Networking.Transport;
using UnityEngine;

namespace Network
{
    public class LANClient : MonoBehaviour
    {
        private UdpNetworkDriver m_ClientDriver;
        private NetworkConnection m_clientToServerConnection;

        #region Unity Methods

        private void OnEnable()
        {
            MulticastDiscovery.Instance.OnReceiveEvent += InitClient;
        }

        private void OnDisable()
        {
            MulticastDiscovery.Instance.OnReceiveEvent -= InitClient;
        }

        void OnDestroy()
        {
            m_ClientDriver.Dispose();
        }


        void FixedUpdate()
        {
            if (!m_ClientDriver.IsCreated)
            {
                return;
            }

            // Update the NetworkDriver. It schedules a job so we must wait for that job with Complete
            m_ClientDriver.ScheduleUpdate().Complete();          

            DataStreamReader strm;
            NetworkEvent.Type cmd;
            // Process all events on the connection. If the connection is invalid it will return Empty immediately
            while ((cmd = m_clientToServerConnection.PopEvent(m_ClientDriver, out strm)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    // When we get the connect message we can start sending data to the server
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    //Process data sent from server
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    // If the server disconnected us we clear out connection
                    m_clientToServerConnection = default(NetworkConnection);
                }
            }
        }

        #endregion

        void CloseConnection()
        {
            m_clientToServerConnection.Disconnect(m_ClientDriver);
            m_clientToServerConnection = default(NetworkConnection);
        }

        #region Event Callbacks

        private void InitClient(ReceiveData d)
        {
            // Create a NetworkDriver for the client. We could bind to a specific address but in this case we rely on the
            // implicit bind since we do not need to bing to anything special
            m_ClientDriver = new UdpNetworkDriver(new INetworkParameter[0]);
            Debug.Log("Server address: " + d.Ip.ToString());
            NetworkEndPoint ServerEndPoint = NetworkEndPoint.Parse(d.Ip.ToString(), d.Port);

            m_clientToServerConnection = m_ClientDriver.Connect(PingClientUIBehaviour.ServerEndPoint);
        }

        #endregion
    }
}

