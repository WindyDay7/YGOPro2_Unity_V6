using UnityEngine;

public class partical_scaler : MonoBehaviour
{
    public float scale = 1f;
    private float prevScale = 1f;

    void Update()
    {
        if (scale != prevScale)
        {
            transform.localScale = new Vector3(scale, scale, scale);
            
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem system in systems)
            {
                var main = system.main;
                main.startSpeedMultiplier *= scale / prevScale;
                main.startSizeMultiplier *= scale / prevScale;
            }
            
            prevScale = scale;
        }
    }
}
