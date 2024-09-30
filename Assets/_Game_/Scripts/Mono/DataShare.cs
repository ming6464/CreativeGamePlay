using UnityEngine;

public class DataShare : MonoBehaviour
{
    public static DataShare Instance { get; private set; }
    //
    public Config config;
    //
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
        
}