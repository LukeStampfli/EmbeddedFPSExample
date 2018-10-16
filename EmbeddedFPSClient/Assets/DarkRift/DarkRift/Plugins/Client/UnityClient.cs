using DarkRift.Dispatching;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace DarkRift.Client.Unity
{
    [AddComponentMenu("DarkRift/Client")]
	public sealed class UnityClient : MonoBehaviour
	{
        /// <summary>
        ///     The IP address this client connects to.
        /// </summary>
        public IPAddress Address
        {
            get { return IPAddress.Parse(address); }
            set { address = value.ToString(); }
        }

        [SerializeField]
        [Tooltip("The address of the server to connect to.")]
        string address = IPAddress.Loopback.ToString();                 //Unity requires a serializable backing field so use string

        /// <summary>
        ///     The port this client connects to.
        /// </summary>
        public ushort Port
        {
            get { return port; }
            set { port = value; }
        }

		[SerializeField]
		[Tooltip("The port the server is listening on.")]
		ushort port = 4296;

        /// <summary>
        ///     The IP version to connect with.
        /// </summary>
        public IPVersion IPVersion
        {
            get { return ipVersion; }
            set { ipVersion = value; }
        }

        [SerializeField]
        [Tooltip("The IP protocol version to connect using.")]          //Declared in custom editor
        IPVersion ipVersion = IPVersion.IPv4;

        [SerializeField]
        [Tooltip("Indicates whether the client will connect to the server in the Start method.")]
        bool autoConnect = true;

        [SerializeField]
        [Tooltip("Specifies that DarkRift should take care of multithreading and invoke all events from Unity's main thread.")]
        volatile bool invokeFromDispatcher = true;

        [SerializeField]
        [Tooltip("Specifies whether DarkRift should log all data to the console.")]
        volatile bool sniffData = false;

        #region Cache settings

        /// <summary>
        ///     The maximum number of <see cref="DarkRiftWriter"/> instances stored per thread.
        /// </summary>
        public int MaxCachedWriters
        {
            get
            {
                return maxCachedWriters;
            }
        }

        [SerializeField]
        [Tooltip("The maximum number of DarkRiftWriter instances stored per thread.")]
        int maxCachedWriters = 2;

        /// <summary>
        ///     The maximum number of <see cref="DarkRiftReader"/> instances stored per thread.
        /// </summary>
        public int MaxCachedReaders
        {
            get
            {
                return maxCachedReaders;
            }
        }

        [SerializeField]
        [Tooltip("The maximum number of DarkRiftReader instances stored per thread.")]
        int maxCachedReaders = 2;

        /// <summary>
        ///     The maximum number of <see cref="Message"/> instances stored per thread.
        /// </summary>
        public int MaxCachedMessages
        {
            get
            {
                return maxCachedMessages;
            }
        }

        [SerializeField]
        [Tooltip("The maximum number of Message instances stored per thread.")]
        int maxCachedMessages = 8;

        /// <summary>
        ///     The maximum number of <see cref="System.Net.Sockets.SocketAsyncEventArgs"/> instances stored per thread.
        /// </summary>
        public int MaxCachedSocketAsyncEventArgs
        {
            get
            {
                return maxCachedSocketAsyncEventArgs;
            }
        }

        [SerializeField]
        [Tooltip("The maximum number of SocketAsyncEventArgs instances stored per thread.")]
        int maxCachedSocketAsyncEventArgs = 32;

        /// <summary>
        ///     The maximum number of <see cref="ActionDispatcherTask"/> instances stored per thread.
        /// </summary>
        public int MaxCachedActionDispatcherTasks
        {
            get
            {
                return maxCachedActionDispatcherTasks;
            }
        }
        
        [SerializeField]
        [Tooltip("The maximum number of ActionDispatcherTask instances stored per thread.")]
        int maxCachedActionDispatcherTasks = 16;
        #endregion

        /// <summary>
        ///     Event fired when a message is received.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        ///     Event fired when we disconnect form the server.
        /// </summary>
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        /// <summary>
        ///     The ID the client has been assigned.
        /// </summary>
        public ushort ID
        {
            get
            {
                return Client.ID;
            }
        }

        /// <summary>
        ///     Returns whether or not this client is connected to the server.
        /// </summary>
        public bool Connected
        {
            get
            {
                return Client.Connected;
            }
        }

		/// <summary>
		/// 	The actual client connecting to the server.
		/// </summary>
		/// <value>The client.</value>
        public DarkRiftClient Client
        {
            get
            {
                return client;
            }
        }

        DarkRiftClient client;

        /// <summary>
        ///     The dispatcher for moving work to the main thread.
        /// </summary>
        public Dispatcher Dispatcher { get; private set; }
        
        void Awake()
        {
            client = new DarkRiftClient(maxCachedWriters, maxCachedReaders, maxCachedMessages, maxCachedSocketAsyncEventArgs, maxCachedActionDispatcherTasks);

            //Setup dispatcher
            Dispatcher = new Dispatcher(true);

            //Setup routing for events
            Client.MessageReceived += Client_MessageReceived;
            Client.Disconnected += Client_Disconnected;
        }

        void Start()
		{
            //If auto connect is true then connect to the server
            if (autoConnect)
			    Connect(Address, port, ipVersion);
		}

        void Update()
        {
            //Execute all the queued dispatcher tasks
            Dispatcher.ExecuteDispatcherTasks();
        }

        void OnDestroy()
        {
            //Remove resources
            Close();
            Debug.Log("Destroy");
        }

        void OnApplicationQuit()
        {
            //Remove resources
            Close();
            Debug.Log("Quit");
        }

        /// <summary>
        ///     Connects to a remote server.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="port">The port of the server.</param>
        public void Connect(IPAddress ip, int port, IPVersion ipVersion)
        {
            Client.Connect(ip, port, ipVersion);

            if (Connected)
                Debug.Log("Connected to " + ip + " on port " + port + " using " + ipVersion + ".");
            else
                Debug.Log("Connection failed to " + ip + " on port " + port + " using " + ipVersion + ".");
        }

        /// <summary>
        ///     Connects to a remote asynchronously.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="port">The port of the server.</param>
        /// <param name="callback">The callback to make when the connection attempt completes.</param>
        public void ConnectInBackground(IPAddress ip, int port, IPVersion ipVersion, DarkRiftClient.ConnectCompleteHandler callback = null)
        {
            Client.ConnectInBackground(
                ip,
                port, 
                ipVersion, 
                delegate (Exception e)
                {
                    if (callback != null)
                    {
                        if (invokeFromDispatcher)
                            Dispatcher.InvokeAsync(() => callback(e));
                        else
                            callback.Invoke(e);
                    }
                    
                    if (Connected)
                        Debug.Log("Connected to " + ip + " on port " + port + " using " + ipVersion + ".");
                    else
                        Debug.Log("Connection failed to " + ip + " on port " + port + " using " + ipVersion + ".");
                }
            );
        }

        /// <summary>
        ///     Sends a message to the server.
        /// </summary>
        /// <param name="message">The message template to send.</param>
        /// <returns>Whether the send was successful.</returns>
        public bool SendMessage(Message message, SendMode sendMode)
        {
            return Client.SendMessage(message, sendMode);
        }

        /// <summary>
        ///     Invoked when DarkRift receives a message from the server.
        /// </summary>
        /// <param name="sender">THe client that received the message.</param>
        /// <param name="e">The arguments for the event.</param>
        void Client_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //If we're handling multithreading then pass the event to the dispatcher
            if (invokeFromDispatcher)
            {
                if (sniffData)
                    Debug.Log("Message Received");      //TODO more information!

                Dispatcher.InvokeAsync(
                    () => 
                        {
                            EventHandler<MessageReceivedEventArgs> handler = MessageReceived;
                            if (handler != null)
                            {
                                handler.Invoke(sender, e);
                            }
                        }
                );
            }
            else
            {
                if (sniffData)
                    Debug.Log("Message Received");      //TODO more information!

                EventHandler<MessageReceivedEventArgs> handler = MessageReceived;
                if (handler != null)
                {
                    handler.Invoke(sender, e);
                }
            }
        }

        void Client_Disconnected(object sender, DisconnectedEventArgs e)
        {
            //If we're handling multithreading then pass the event to the dispatcher
            if (invokeFromDispatcher)
            {
                if (!e.LocalDisconnect)
                    Debug.Log("Disconnected from server, error: " + e.Error);

                Dispatcher.InvokeAsync(
                    () =>
                    {
                        EventHandler<DisconnectedEventArgs> handler = Disconnected;
                        if (handler != null)
                        {
                            handler.Invoke(sender, e);
                        }
                    }
                );
            }
            else
            {
                if (!e.LocalDisconnect)
                    Debug.Log("Disconnected from server, error: " + e.Error);
                
                EventHandler<DisconnectedEventArgs> handler = Disconnected;
                if (handler != null)
                {
                    handler.Invoke(sender, e);
                }
            }
        }

        /// <summary>
        ///     Disconnects this client from the server.
        /// </summary>
        /// <returns>Whether the disconnect was successful.</returns>
        public bool Disconnect()
        {
            return Client.Disconnect();
        }

        /// <summary>
        ///     Closes this client.
        /// </summary>
        public void Close()
        {
            Client.MessageReceived -= Client_MessageReceived;
            Client.Disconnected -= Client_Disconnected;

            Client.Dispose();
            Dispatcher.Dispose();
        }
	}
}
