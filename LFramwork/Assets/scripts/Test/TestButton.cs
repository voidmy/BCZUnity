using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class TestButton : MonoBehaviour
{

    [MethodButton("TestButton")]
    public void OnClick()
    {
        print("OnClick");
    }
}
