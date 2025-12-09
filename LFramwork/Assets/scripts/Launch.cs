using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launch : MonoBehaviour
{

    [RuntimeInitializeOnLoadMethod]
    static void OnLaunch()
    {
        print("OnLaunch");
    }
    void Start()
    {
        print("sss");
    }

}
