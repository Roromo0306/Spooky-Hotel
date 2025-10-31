using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void Register<T>(T service) where T : class
    {
        var t = typeof(T);
        if (_services.ContainsKey(t)) _services[t] = service;
        else _services.Add(t, service);
    }

    public static void Unregister<T>() where T : class
    {
        var t = typeof(T);
        if (_services.ContainsKey(t)) _services.Remove(t);
    }

    public static T Get<T>() where T : class
    {
        var t = typeof(T);
        if (_services.TryGetValue(t, out var svc)) return svc as T;
        return null;
    }
}

