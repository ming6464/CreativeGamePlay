using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // private static m_instance
    static T m_instance;

    // public static m_instance used to refer to Singleton (e.g. MyClass.Instance)
    public static T Instance
    {
        get
        {
            // if no m_instance is found, find the first GameObject of type T
            if (m_instance == null)
            {
                m_instance = GameObject.FindObjectOfType<T>();

                // if no m_instance exists in the Scene, create a new GameObject and add the Component T 
                if (m_instance == null)
                {
                    GameObject singleton = new GameObject(typeof(T).Name);
                    m_instance = singleton.AddComponent<T>();
                }
            }
            // return the singleton m_instance
            return m_instance;
        }
    }

    public virtual void Awake()
    {
        MakeSingleton();
    }

    public void MakeSingleton()
    {
        if (m_instance == null)
        {
            m_instance = this as T;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void DontDestroyOnLoad()
    {
        DontDestroyOnLoad(this);
    }
    
}