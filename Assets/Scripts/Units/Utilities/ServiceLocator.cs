using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal type-based service locator for scene-level singleton-style services.
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

    public static void Register<T>(T service)
        where T : class
    {
        if (service == null)
        {
            return;
        }

        services[typeof(T)] = service;
    }

    public static bool TryResolve<T>(out T service)
        where T : class
    {
        Type serviceType = typeof(T);
        if (services.TryGetValue(serviceType, out object registeredService)
            && registeredService is T typedService)
        {
            if (typedService is UnityEngine.Object unityObject && unityObject == null)
            {
                services.Remove(serviceType);
                service = null;
                return false;
            }

            service = typedService;
            return true;
        }

        service = null;
        return false;
    }

    public static void Unregister<T>(T service)
        where T : class
    {
        Type serviceType = typeof(T);
        if (!services.TryGetValue(serviceType, out object registeredService))
        {
            return;
        }

        if (ReferenceEquals(registeredService, service))
        {
            services.Remove(serviceType);
        }
    }
}
