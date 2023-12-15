using UnityEngine;

public class DontDestroyUtility : MonoBehaviour
{
    
    void Start()
    {
        DontDestroyOnLoad(this);
    }

 
}
