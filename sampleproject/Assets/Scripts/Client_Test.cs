using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace Network
{
    public class Client_Test : MonoBehaviour
    {
        public MulticastDiscovery Multicast;

        private void Awake()
        {
            Multicast.OnReceive.AddListener(MyAction);
        }

        private void MyAction(IPAddress arg0)
        {
            throw new NotImplementedException();
        }
    }
}

