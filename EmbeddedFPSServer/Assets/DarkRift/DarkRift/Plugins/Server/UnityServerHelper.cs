using DarkRift.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
///     Various shared methods for use in UnityServer and UnityServerEditor.
/// </summary>
public static class UnityServerHelper
{
    /// <summary>
    ///     Searches the app domain for plugin types. 
    /// </summary>
    /// <returns>The plugin types in the app domain.</returns>
    public static IEnumerable<Type> SearchForPlugins()
    {
        //Omit DarkRift server assembly so internal plugins aren't loaded twice
        Assembly[] omit = new Assembly[]
        {
            Assembly.GetAssembly(typeof(DarkRiftServer))
        };

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Except(omit))
        {
            IEnumerable<Type> types = new Type[0];
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                Debug.LogWarning("An assembly could not be loaded while searching for plugins. This could be because it is an unmanaged library.");
            }

            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(Plugin)) && !type.IsAbstract)
                    yield return type;
            }
        }
    }
}
