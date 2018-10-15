using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Net;

namespace DarkRift.Server.Unity
{
#pragma warning disable 0618
    [CustomEditor(typeof(UnityServer))]
#pragma warning restore 0618
    [CanEditMultipleObjects]
    public class UnityClientEditor : Editor
    {
#pragma warning disable 0618
        UnityServer server;
#pragma warning restore 0618

        SerializedProperty createOnEnable;
        SerializedProperty eventsFromDispatcher;

        string address;
        SerializedProperty port;
        SerializedProperty ipVersion;
        SerializedProperty maxStrikes;

        SerializedProperty dataDirectory;

        SerializedProperty logToFile;
        SerializedProperty logFileString;
        SerializedProperty logToUnityConsole;
        SerializedProperty logToDebug;

        SerializedProperty loadByDefault;

        SerializedProperty maxCachedWriters;
        SerializedProperty maxCachedReaders;
        SerializedProperty maxCachedMessages;
        SerializedProperty maxCachedSocketAsyncEventArgs;
        SerializedProperty maxCachedActionDispatcherTasks;

        bool showServer, showData, showLogging, showPlugins, showDatabases, showCache;

        void OnEnable()
        {
#pragma warning disable 0618
            server = (UnityServer)serializedObject.targetObject;
#pragma warning restore 0618

            createOnEnable  = serializedObject.FindProperty("createOnEnable");
            eventsFromDispatcher = serializedObject.FindProperty("eventsFromDispatcher");

            address         = server.Address.ToString();
            port            = serializedObject.FindProperty("port");
            ipVersion       = serializedObject.FindProperty("ipVersion");
            maxStrikes      = serializedObject.FindProperty("maxStrikes");

            dataDirectory   = serializedObject.FindProperty("dataDirectory");

            logToFile       = serializedObject.FindProperty("logToFile");
            logFileString   = serializedObject.FindProperty("logFileString");
            logToUnityConsole = serializedObject.FindProperty("logToUnityConsole");
            logToDebug      = serializedObject.FindProperty("logToDebug");

            loadByDefault   = serializedObject.FindProperty("loadByDefault");

            maxCachedWriters                = serializedObject.FindProperty("maxCachedWriters");
            maxCachedReaders                = serializedObject.FindProperty("maxCachedReaders");
            maxCachedMessages               = serializedObject.FindProperty("maxCachedMessages");
            maxCachedSocketAsyncEventArgs   = serializedObject.FindProperty("maxCachedSocketAsyncEventArgs");
            maxCachedActionDispatcherTasks  = serializedObject.FindProperty("maxCachedActionDispatcherTasks");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(createOnEnable);

            //Alert to changes when this is unticked!
            bool old = eventsFromDispatcher.boolValue;
            EditorGUILayout.PropertyField(eventsFromDispatcher);

            if (eventsFromDispatcher.boolValue != old && !eventsFromDispatcher.boolValue)
            {
                eventsFromDispatcher.boolValue = !EditorUtility.DisplayDialog(
                    "Danger!",
                    "Unchecking " + eventsFromDispatcher.displayName + " will cause DarkRift to fire events from the .NET thread pool. Unless you are confident using multithreading with Unity you should not disable this.\n\nAre you sure you want to proceed?",
                    "Yes",
                    "No (Save me!)"
                );
            }

            if (showServer = EditorGUILayout.Foldout(showServer, "Server Setttings"))
            {
                EditorGUI.indentLevel++;

                //Display IP address
                address = EditorGUILayout.TextField(new GUIContent("Address", "The address the client will connect to."), address);

                try
                {
                    server.Address = IPAddress.Parse(address);
                }
                catch (FormatException)
                {
                    EditorGUILayout.HelpBox("Invalid IP address.", MessageType.Error);
                }
                
                EditorGUILayout.PropertyField(port);

                //Draw IP versions manually else it gets formatted as "Ip Version" and "I Pv4" -_-
                ipVersion.enumValueIndex = EditorGUILayout.Popup(new GUIContent("IP Version", "The IP protocol version the server will listen on."), ipVersion.enumValueIndex, Array.ConvertAll(ipVersion.enumNames, i => new GUIContent(i)));

                EditorGUILayout.PropertyField(maxStrikes);

                EditorGUI.indentLevel--;
            }

            if (showData = EditorGUILayout.Foldout(showData, "Data Setttings"))
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(dataDirectory);

                EditorGUI.indentLevel--;
            }

            if (showLogging = EditorGUILayout.Foldout(showLogging, "Logging Setttings"))
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(logToFile);

                EditorGUI.indentLevel++;
                if (logToFile.boolValue)
                    EditorGUILayout.PropertyField(logFileString);
                EditorGUI.indentLevel--;

                EditorGUILayout.PropertyField(logToUnityConsole);

                EditorGUILayout.PropertyField(logToDebug);

                EditorGUI.indentLevel--;
            }

            //Draw plugins list
            if (showPlugins = EditorGUILayout.Foldout(showPlugins, "Plugin Setttings"))
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(loadByDefault);
                
                IEnumerable<Type> types = UnityServerHelper.SearchForPlugins();

                foreach (Type type in types)
                {
                    ServerSpawnData.PluginsSettings.PluginSettings plugin = server.Plugins.SingleOrDefault(p => p.Type == type.Name);

                    if (plugin == null)
                    {
                        plugin = new ServerSpawnData.PluginsSettings.PluginSettings
                        {
                            Type = type.Name,
                            Load = true
                        };

                        server.Plugins.Add(plugin);
                    }

                    EditorGUILayout.HelpBox("The following are plugins in your project, tick those to be loaded.", MessageType.Info);

                    plugin.Load = EditorGUILayout.Toggle(type.Name, plugin.Load);

                    EditorGUILayout.Space();
                }

                EditorGUI.indentLevel--;
            }

            //Draw databases manually
            if (showDatabases = EditorGUILayout.Foldout(showDatabases, "Databases"))
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < server.Databases.Count; i++)
                {
                    ServerSpawnData.DatabaseSettings.DatabaseConnectionData database = server.Databases[i];

                    database.Name = EditorGUILayout.TextField("Name", database.Name);

                    database.ConnectionString = EditorGUILayout.TextField("Connection String", database.ConnectionString);

                    Rect removeRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());        //So indent level affects the button
                    if (GUI.Button(removeRect, "Remove"))
                    {
                        server.Databases.Remove(database);
                        i--;
                    }

                    EditorGUILayout.Space();
                }

                Rect addRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(true));
                if (GUI.Button(addRect, "Add Database"))
                    server.Databases.Add(new ServerSpawnData.DatabaseSettings.DatabaseConnectionData("NewDatabase", "Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;"));

                EditorGUI.indentLevel--;
            }

            if (showCache = EditorGUILayout.Foldout(showCache, "Cache"))
            {
                EditorGUILayout.PropertyField(maxCachedWriters);
                EditorGUILayout.PropertyField(maxCachedReaders);
                EditorGUILayout.PropertyField(maxCachedMessages);
                EditorGUILayout.PropertyField(maxCachedSocketAsyncEventArgs);
                EditorGUILayout.PropertyField(maxCachedActionDispatcherTasks);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
