////////////////////////////////////////////////////////////////////////
// BridgeObject.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


/// <summary>
/// Provides a bridge for configuring and addressing any GameObject via JSON
/// without modifying the original prefab or component structure
/// </summary>
public class BridgeObject : MonoBehaviour {


    ////////////////////////////////////////////////////////////////////////
    // Instance Variables


    [SerializeField] private string _id;
    
    // Dynamic properties storage
    protected Dictionary<string, object> _properties = new Dictionary<string, object>();
    
    // Component cache for performance
    private Dictionary<Type, Component> _componentCache = new Dictionary<Type, Component>();
    
    // Events
    public event Action<string, object> OnPropertyChanged;
    public event Action<Dictionary<string, object>> OnMessageReceived;
    
    public string id 
    { 
        get => _id;
        set => _id = value;
    }
    
    public string Id 
    { 
        get => _id;
        set => _id = value;
    }
    
    public JObject interests;
    public bool destroyed = false;
    public bool destroying = false;
    public Bridge bridge;


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    private void Awake()
    {
        // Make sure we have an ID
        if (string.IsNullOrEmpty(_id))
        {
            _id = System.Guid.NewGuid().ToString();
        }
    }


    public virtual void OnDestroy()
    {
        //Debug.Log("BridgeObject: OnDestroy: ==== this: " + this + " destroying: " + destroying + " destroyed: " + destroyed);

        if (destroyed) {
            return;
        }

        destroying = true;

        //Debug.Log("BridgeObject: OnDestroy: not destroyed so set destroying: " + destroying + " and calling DestroyObject. bridge: " + ((bridge == null) ? "NULL" : bridge.id));
        if (bridge != null) {
            bridge.DestroyObject(this);
        }
    }


    public void HandleEvents(JArray events)
    {
        if (events == null) {
            return;
        }

        foreach (JObject ev in events) {
            HandleEvent(ev);
        }

    }


    public virtual void HandleEvent(JObject ev)
    {
        //Debug.Log("BridgeObject: HandleEvent: this: " + this + " ev: " + ev, this);

        string eventName = (string)ev["event"];
        //Debug.Log("BridgeObject: HandleEvent: eventName: " + eventName, this);

        if (string.IsNullOrEmpty(eventName)) {
            Debug.LogError("BridgeObject: HandleEvent: missing event name in ev: " + ev);
            return;
        }

        JToken data = ev["data"];
        //Debug.Log("BridgeObject: HandleEvent: eventName: " + eventName, this);

        switch (eventName) {

            case "Log": {
                string line = (string)data["line"];
                Debug.Log ("BridgeObject: HandleEvent: Log: this: " + this + " line: " + line);
                break;
            }

            case "Destroy": {
                bridge.DestroyObject(this);
                break;
            }

            case "Update": {
                JObject update = (JObject)data;
                LoadUpdate(update);
                break;
            }

            case "UpdateInterests": {
                JObject newInterests = (JObject)data;
                UpdateInterests(newInterests);
                break;
            }

            case "Animate": {
                JArray dataArray = (JArray)data;
                AnimateData(dataArray);
                break;
            }

            case "SetGlobals": {
                JObject dataObject = (JObject)data;
                JObject globals = (JObject)dataObject["globals"];
                //Debug.Log("BridgeObject: HandleEvent: SetGlobals: dataObject: " + dataObject + " globals: " + globals + " bridge: " + bridge);
                bridge.SetGlobals(this, globals);
                break;
            }

            case "AddComponent": {
                // TODO: AddComponent
                //JObject dataObject = (JObject)data;
                //string className = (string)dataObject["className"];
                //Debug.Log("BridgeObject: HandleEvent: AddComponent: className: " + className);
                break;
            }

            case "DestroyAfter": {
                JObject dataObject = (JObject)data;
                float delay = (float)dataObject["delay"];
                //Debug.Log("BridgeObject: HandleEvent: DestroyAfter: delay: " + delay + " this: " + this);
                UnityEngine.Object.Destroy(gameObject, delay);
                break;
            }

            case "AssignTo": {
                JObject dataObject = (JObject)data;
                string path = (string)dataObject["path"];
                //Debug.Log("BridgeObject: HandleEvent: AssignTo: path: " + path + " this: " + this);

                Accessor accessor = null;
                if (!Accessor.FindAccessor(
                        this,
                        path,
                        ref accessor)) {

                    Debug.LogError("BridgeObject: HandleEvent: AssignTo: can't find accessor for this: " + this + " path: " + path);

                } else {

                    if (!accessor.Set(this) &&
                        !accessor.conditional) {
                        Debug.LogError("BridgeObject: HandleEvent: AssignTo: can't set accessor: " + accessor + " this: " + this + " path: " + path);
                    }

                }
                break;
            }

            case "SetParent": {
                JObject dataObject = (JObject)data;
                //Debug.Log("BridgeObject: HandleEvent: SetParent: this: " + this + " data: " + data);
                string path = (string)dataObject["path"];
                //Debug.Log("BridgeObject: HandleEvent: SetParent: path: " + path + " this: " + this);

                if (string.IsNullOrEmpty(path)) {

                    transform.SetParent(null);

                } else {

                    Accessor accessor = null;
                    if (!Accessor.FindAccessor(
                            this,
                            path,
                            ref accessor)) {

                        Debug.LogError("BridgeObject: HandleEvent: SetParent: can't find accessor for this: " + this + " path: " + path);

                    } else {

                        object obj = null;
                        if (!accessor.Get(ref obj)) {

                            if (!accessor.conditional) {
                                Debug.LogError("BridgeObject: HandleEvent: SetParent: can't get accessor: " + accessor + " this: " + this + " path: " + path);
                            }

                        } else {

                            Component component = obj as Component;
                            if (component == null) {

                                if (!accessor.conditional) {
                                    Debug.LogError("BridgeObject: HandleEvent: SetParent: expected Component obj: " + obj + " this: " + this + " path: " + path);
                                }

                            } else {

                                GameObject go = component.gameObject;
                                Transform xform = go.transform;
                                bool worldPositionStays = data.GetBoolean("worldPositionStays", true);
                                transform.SetParent(xform, worldPositionStays);

                            }

                        }
                    }

                }

                break;

            }

        }

    }


    public void LoadUpdate(JObject update)
    {
        //Debug.Log("BridgeObject: LoadUpdate: this: " + this + " update: " + update);

        foreach (var item in update) {
            string key = item.Key;
            JToken value = (JToken)item.Value;

            //Debug.Log("BridgeObject: LoadUpdate: this: " + this + " SetProperty: " + key + ": " + value);

            Accessor.SetProperty(this, key, value);
        }

    }


    public virtual void AddInterests(JObject newInterests)
    {
        interests = newInterests;
    }


    public virtual void UpdateInterests(JObject newInterests)
    {
        //Debug.Log("BridgeObject: UpdateInterests: newInterests: " + newInterests, this);

        // TODO: Should we support multiple interests on the same event name?

        if (interests == null) {
            return;
        }

        foreach (var item in newInterests) {
            string eventName = item.Key;
            JToken interestUpdate = (JToken)item.Value;

            JObject interest = 
                (JObject)interests[eventName];

            if (interestUpdate == null) {

                if (interest != null) {
                    interests.Remove(eventName);
                }

            } else if (interestUpdate.Type == JTokenType.Boolean) {

                if (interest != null) {

                    bool disabled = 
                        !(bool)interestUpdate; // disabled = !enabled

                    interest["disabled"] = disabled;

                }

            } else if (interestUpdate.Type == JTokenType.Object) {

                if (interest == null) {

                    interests[eventName] = interestUpdate;

                } else {

                    foreach (var item2 in (JObject)interestUpdate) {
                        var key = item2.Key;
                        interest[key] = interestUpdate[key];
                    }

                }

            }

        }

    }


    public void SendEventName(string eventName, JObject data = null)
    {
        //Debug.Log("BridgeObject: SendEventName: eventName: " + eventName + " data: " + data + " interests: " + interests);

        if (bridge == null) {
            Debug.LogError("BridgeObject: SendEventName: bridge is null!");
            return;
        }

        bool foundInterest = false;
        bool doNotSend = false;

        if (interests != null) {

            JObject interest = interests[eventName] as JObject;
            //Debug.Log("BridgeObject: SendEventName: eventName: " + eventName + " interest: " + interest, this);
            if (interest != null) {

                bool disabled = false;
                JToken disabledToken = interest["disabled"];
                if (disabledToken != null && disabledToken.Type == JTokenType.Boolean)
                {
                    disabled = (bool)disabledToken;
                }
                
                if (!disabled) {

                    foundInterest = true;
                    //Debug.Log("BridgeObject: SendEventName: foundInterest: eventName: " + eventName + " interest: " + interest, this);

                    JObject update = interest["update"] as JObject;
                    if (update != null) {

                        //Debug.Log("BridgeObject: SendEventName: event interest update: " + update);

                        LoadUpdate(update);
                    }

                    JArray events = interest["events"] as JArray;
                    if (events != null) {

                        //Debug.Log("BridgeObject: SendEventName: event interest events: " + events);

                        HandleEvents(events);
                    }

                    JToken doNotSendToken = interest["doNotSend"];
                    if (doNotSendToken != null && doNotSendToken.Type == JTokenType.Boolean)
                    {
                        doNotSend = (bool)doNotSendToken;
                    }

                    if (doNotSend) {
                        //Debug.Log("BridgeObject: SendEventName: doNotSend: interest: " + interest);
                    }

                    if (!doNotSend) {

                        JObject query = interest["query"] as JObject;
                        if (query != null) {

                            //Debug.Log("BridgeObject: SendEventName: event interest query: " + query);

                            if (data == null) {
                                data = new JObject();
                            }

                            bridge.AddQueryData(this, query, data);
                            //Debug.Log("BridgeObject: SendEventName: event interest query data: " + dagta);

                        }

                    }

                }

            }

        }

        // Always send Created and Destroyed events.
        if ((!doNotSend) &&
            (foundInterest ||
             (eventName == "Created") ||
             (eventName == "Destroyed"))) {

            JObject ev = new JObject();

            ev.Add("event", eventName);
            ev.Add("id", id);

            if (data != null) {
                ev.Add("data", data);
            }

            //Debug.Log("BridgeObject: SendEventName: ev: " + ev, this);

            bridge.SendEvent(ev);
        }


    }


    public virtual void AnimateData(JArray data)
    {
        //Debug.Log("BridgeObject: AnimateData: data: " + data, this);

#if USE_LEANTWEEN
        LeanTweenBridge.AnimateData(this, data);
#else
        // Animation disabled - LeanTween not available
        Debug.Log("Animation not available - LeanTween functionality is disabled");
#endif
    }


    /// <summary>
    /// Configure this object from a JSON string
    /// </summary>
    public virtual void ConfigureFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        
        try
        {
            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (settings != null)
            {
                foreach (var kvp in settings)
                {
                    SetProperty(kvp.Key, kvp.Value);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error configuring BridgeObject from JSON: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Set a property with type inference
    /// </summary>
    public void SetProperty<T>(string key, T value)
    {
        _properties[key] = value;
        OnPropertyChanged?.Invoke(key, value);
        
        // Try to apply the property to any matching component property
        ApplyToComponents(key, value);
    }
    
    /// <summary>
    /// Get a property with type casting
    /// </summary>
    public T GetProperty<T>(string key, T defaultValue = default)
    {
        if (_properties.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;
            
            try
            {
                // Try conversion for primitive types
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch 
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Check if a property exists
    /// </summary>
    public bool HasProperty(string key)
    {
        return _properties.ContainsKey(key);
    }
    
    /// <summary>
    /// Remove a property
    /// </summary>
    public void RemoveProperty(string key)
    {
        if (_properties.ContainsKey(key))
        {
            _properties.Remove(key);
            OnPropertyChanged?.Invoke(key, null);
        }
    }
    
    /// <summary>
    /// Clear all properties
    /// </summary>
    public void ClearProperties()
    {
        _properties.Clear();
        OnPropertyChanged?.Invoke("*", null);
    }
    
    /// <summary>
    /// Handle a JSON message
    /// </summary>
    public void HandleMessage(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        
        try
        {
            var message = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (message != null)
            {
                // Extract action
                if (message.TryGetValue("action", out var actionObj) && actionObj is string action)
                {
                    // Handle standard actions
                    switch (action)
                    {
                        case "setProperty":
                            if (message.TryGetValue("key", out var keyObj) && 
                                message.TryGetValue("value", out var valueObj) &&
                                keyObj is string key)
                            {
                                SetProperty(key, valueObj);
                            }
                            break;
                                
                        case "getProperty":
                            // Would need a callback mechanism to return value
                            break;
                                
                        case "invokeMethod":
                            if (message.TryGetValue("method", out var methodObj) && 
                                methodObj is string method)
                            {
                                object[] args = null;
                                if (message.TryGetValue("args", out var argsObj) && 
                                    argsObj is object[] argsArray)
                                {
                                    args = argsArray;
                                }
                                
                                InvokeMethod(method, args);
                            }
                            break;
                    }
                }
                
                // Notify subscribers
                OnMessageReceived?.Invoke(message);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling message: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Apply a property to matching components
    /// </summary>
    private void ApplyToComponents<T>(string propertyName, T value)
    {
        // Get all components
        var components = GetComponentsInChildren<Component>();
        foreach (var component in components)
        {
            if (component == null) continue;
            
            try
            {
                // Try to set property via reflection
                var propInfo = component.GetType().GetProperty(propertyName);
                if (propInfo != null && propInfo.CanWrite)
                {
                    // Check if types are compatible
                    if (propInfo.PropertyType.IsAssignableFrom(typeof(T)))
                    {
                        propInfo.SetValue(component, value);
                    }
                    else if (value is IConvertible)
                    {
                        // Try conversion for primitive types
                        var convertedValue = Convert.ChangeType(value, propInfo.PropertyType);
                        propInfo.SetValue(component, convertedValue);
                    }
                }
                
                // Try field as fallback
                var fieldInfo = component.GetType().GetField(propertyName);
                if (fieldInfo != null)
                {
                    if (fieldInfo.FieldType.IsAssignableFrom(typeof(T)))
                    {
                        fieldInfo.SetValue(component, value);
                    }
                    else if (value is IConvertible)
                    {
                        var convertedValue = Convert.ChangeType(value, fieldInfo.FieldType);
                        fieldInfo.SetValue(component, convertedValue);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignore reflection errors
                Debug.LogWarning($"Failed to set {propertyName} on {component.GetType().Name}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Invoke a method on a component by name
    /// </summary>
    private void InvokeMethod(string methodName, object[] args = null)
    {
        // Get all components
        var components = GetComponentsInChildren<Component>();
        foreach (var component in components)
        {
            if (component == null) continue;
            
            try
            {
                var methodInfo = component.GetType().GetMethod(methodName);
                if (methodInfo != null)
                {
                    methodInfo.Invoke(component, args);
                    break; // Only invoke on first match
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to invoke {methodName} on {component.GetType().Name}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Get a component with caching for performance
    /// </summary>
    public T GetCachedComponent<T>() where T : Component
    {
        var type = typeof(T);
        if (_componentCache.TryGetValue(type, out var component))
        {
            return component as T;
        }
        
        var newComponent = GetComponent<T>();
        if (newComponent != null)
        {
            _componentCache[type] = newComponent;
        }
        
        return newComponent;
    }

}
