using UnityEngine;

/// <summary>Enemy_1: weaves left and right in a sine wave while moving down.</summary>
public class Enemy_1 : Enemy
{
    [Header("Enemy_1 Inscribed")]
    public float waveFrequency = 2;   // seconds per full wave
    public float waveWidth = 4;
    public float waveRotY = 45;

    float x0;
    float birthTime;

    void Start()
    {
        x0 = pos.x;
        birthTime = Time.time;
    }

    public override void Move()
    {
        Vector3 tempPos = pos;
        float age = Time.time - birthTime;
        float theta = Mathf.PI * 2 * age / waveFrequency;
        float sin = Mathf.Sin(theta);
        tempPos.x = x0 + waveWidth * sin;
        pos = tempPos;

        transform.rotation = Quaternion.Euler(0, sin * waveRotY, 0);
        base.Move(); // handles the downward motion
    }
}
