using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    public float rotationSpeed = 1f; // 每秒旋转多少度

    void Update()
    {
        if (RenderSettings.skybox != null)
        {
            float rotation = Time.time * rotationSpeed;
            RenderSettings.skybox.SetFloat("_Rotation", rotation);
        }
    }
}