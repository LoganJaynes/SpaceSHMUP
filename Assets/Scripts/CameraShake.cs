using UnityEngine;

/// <summary>Simple trauma-based screen shake. Call CameraShake.Add(0.3f) from anywhere.</summary>
public class CameraShake : MonoBehaviour
{
    static CameraShake S;
    public float maxOffset = 2.5f;
    public float decayPerSecond = 1.6f;
    float trauma;
    Vector3 origin;

    void Awake() { S = this; origin = transform.position; }

    public static void Add(float amount)
    {
        if (S != null) S.trauma = Mathf.Clamp01(S.trauma + amount);
    }

    void LateUpdate()
    {
        trauma = Mathf.Max(0f, trauma - decayPerSecond * Time.deltaTime);
        float s = trauma * trauma * maxOffset;
        transform.position = origin + new Vector3(Random.Range(-1f, 1f) * s, Random.Range(-1f, 1f) * s, 0f);
    }
}
