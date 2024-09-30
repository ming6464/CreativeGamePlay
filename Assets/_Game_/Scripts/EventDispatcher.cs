using UnityEngine;
using System.Collections.Generic;
using System;

public class EventDispatcher : MonoBehaviour
{
    #region Singleton

    private static EventDispatcher m_instance;

    public static EventDispatcher Instance
    {
        get
        {
            // instance not exist, then create new one
            if (m_instance == null)
            {
                // create new Gameobject, and add EventDispatcher component
                var singletonObject = new GameObject();
                m_instance = singletonObject.AddComponent<EventDispatcher>();
                singletonObject.name = "Singleton - EventDispatcher";
                //Common.Log("Create singleton : {0}", singletonObject.name);
            }

            return m_instance;
        }
    }

    public static bool HasInstance()
    {
        return m_instance != null;
    }

    private void Awake()
    {
        // check if there's another instance already exist in scene
        if (m_instance != null && m_instance.GetInstanceID() != GetInstanceID())
        {
            // Destroy this instances because already exist the singleton of EventsDispatcer
            // Common.Log("An instance of EventDispatcher already exist : <{1}>, So destroy this instance : <{2}>!!", m_instance.name, name);
            Destroy(gameObject);
        }
        else
        {
            // set instance
            m_instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }


    private void OnDestroy()
    {
        ClearAllListener();
        //m_instance = null;
        // reset this static var to null if it's the singleton instance
        //if (m_instance == this)
        //{
        //    ClearAllListener();
        //    m_instance = null;
        //}
    }

    #endregion


    #region Fields

    /// Store all "listener"
    private Dictionary<EventID, Action<object>> _listeners = new Dictionary<EventID, Action<object>>();

    #endregion


    #region Add Listeners, Post events, Remove listener

    public void RegisterListener(EventID eventID, Action<object> callback)
    {
        // check if listener exist in distionary
        if (_listeners.ContainsKey(eventID))
        {
            // add callback to our collection
            _listeners[eventID] += callback;
        }
        else
        {
            // add new key-value pair
            _listeners.Add(eventID, null);
            _listeners[eventID] += callback;
        }
    }

    public void PostEvent(EventID eventID, object param = null)
    {
        if (!_listeners.ContainsKey(eventID))
            //Common.Log("No listeners for this event : {0}", eventID);
            return;
        // posting event
        var callbacks = _listeners[eventID];
        // if there's no listener remain, then do nothing
        if (callbacks != null)
            callbacks(param);
        else
            //Common.Log("PostEvent {0}, but no listener remain, Remove this key", eventID);
            _listeners.Remove(eventID);
    }

    /// <summary>
    /// Removes the listener. Use to Unregister listener
    /// </summary>
    /// <param name="eventID">EventID.</param>
    /// <param name="callback">Callback.</param>
    public void RemoveListener(EventID eventID, Action<object> callback)
    {
        // checking params
        // Common.Assert(callback != null, "RemoveListener, event {0}, callback = null !!", eventID.ToString());
        // Common.Assert(eventID != EventID.None, "AddListener, event = None !!");

        if (_listeners.ContainsKey(eventID))
        {
            _listeners[eventID] -= callback;
        }
        else
        {
            // Common.Warning(false, "RemoveListener, not found key : " + eventID);
        }
    }

    /// <summary>
    /// Clears all the listener.
    /// </summary>
    public void ClearAllListener()
    {
        _listeners = new Dictionary<EventID, Action<object>>();
    }

    #endregion

#if UNITY_EDITOR
    private void OnApplicationQuit()
    {
        ClearAllListener();
    }
#endif
    
}


#region Extension class

/// <summary>
/// Delare some "shortcut" for using EventDispatcher easier
/// </summary>
public static class EventDispatcherExtension
{
    /// Use for registering with EventsManager
    public static void RegisterListener(this MonoBehaviour listener, EventID eventID, Action<object> callback)
    {
        EventDispatcher.Instance.RegisterListener(eventID, callback);
    }

    /// Post event with param
    public static void PostEvent(this MonoBehaviour listener, EventID eventID, object param)
    {
        EventDispatcher.Instance.PostEvent(eventID, param);
    }

    /// Post event with no param (param = null)
    public static void PostEvent(this MonoBehaviour sender, EventID eventID)
    {
        EventDispatcher.Instance.PostEvent(eventID, null);
    }
}

#endregion

#region eventID

[Serializable]
public enum EventID
{
    None = 0,
    ChangeCamMode
}

#endregion