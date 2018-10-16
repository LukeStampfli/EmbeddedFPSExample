using UnityEngine;
using System.Collections;
using System;
using System.Xml.Linq;
using System.Collections.Specialized;

namespace DarkRift.Server.Unity
{
    [AddComponentMenu("DarkRift/Server")]
    public class XmlUnityServer : MonoBehaviour
    {
        /// <summary>
        ///     The actual server.
        /// </summary>
        public DarkRiftServer Server { get; private set; }

        [SerializeField]
        [Tooltip("The configuration file to use.")]
        TextAsset configuration;

        [SerializeField]
        [Tooltip("Indicates whether the server will be created in the OnEnable method.")]
        bool createOnEnable = true;

        [SerializeField]
        [Tooltip("Indicates whether the server events will be routed through the dispatcher or just invoked.")]
        bool eventsFromDispatcher = true;

        void OnEnable()
        {
            //If createOnEnable is selected create a server
            if (createOnEnable)
                Create();
        }

        void Update()
        {
            //Execute all queued dispatcher tasks
            if (Server != null)
                Server.ExecuteDispatcherTasks();
        }

        /// <summary>
        ///     Creates the server.
        /// </summary>
        public void Create()
        {
            Create(new NameValueCollection());
        }

        /// <summary>
        ///     Creates the server.
        /// </summary>
        public void Create(NameValueCollection variables)
        {
            if (Server != null)
                throw new InvalidOperationException("The server has already been created! (Is CreateOnEnable enabled?)");
            
            if (configuration != null)
            {
                //Create spawn data from config
                ServerSpawnData spawnData = ServerSpawnData.CreateFromXml(XDocument.Parse(configuration.text), variables);

                //Inaccessible from xml, set from inspector
                spawnData.EventsFromDispatcher = eventsFromDispatcher;

                //Unity is broken, work around it...
                spawnData.Server.UseFallbackNetworking = true;

                //Add types
                spawnData.PluginSearch.PluginTypes.AddRange(UnityServerHelper.SearchForPlugins());
                spawnData.PluginSearch.PluginTypes.Add(typeof(UnityConsoleWriter));

                //Create server
                Server = new DarkRiftServer(spawnData);
                Server.Start();
            }
            else
                Debug.LogError("No configuration file specified!");
        }

        void OnDisable()
        {
            Close();
        }

        void OnApplicationQuit()
        {
            Close();
        }

        /// <summary>
        ///     Closes the server.
        /// </summary>
        public void Close()
        {
            if (Server != null)
                Server.Dispose();
        }
    }
}
