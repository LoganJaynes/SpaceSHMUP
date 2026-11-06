using UnityEngine;

/// <summary>Heads-up display drawn with IMGUI (no extra packages needed).</summary>
public class HUD : MonoBehaviour
{
    GUIStyle label, right, center;

    void MakeStyles(int fs)
    {
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            right = new GUIStyle(label) { alignment = TextAnchor.UpperRight };
            center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            label.normal.textColor = right.normal.textColor = center.normal.textColor = Color.white;
        }
        label.fontSize = right.fontSize = center.fontSize = fs;
    }

    void OnGUI()
    {
        if (Main.S == null) return;
        int sw = Screen.width, sh = Screen.height;
        int fs = Mathf.Max(14, sh / 32);
        MakeStyles(fs);
        float pad = sh * 0.015f;

        GUI.Label(new Rect(pad, pad, sw, fs * 1.6f), "SCORE  " + Main.S.score, label);
        GUI.Label(new Rect(0, pad, sw - pad, fs * 1.6f), "BEST  " + Main.HighScore, right);
        GUI.Label(new Rect(0, pad, sw, fs * 1.6f), "LEVEL " + Main.S.Level, new GUIStyle(center) { alignment = TextAnchor.UpperCenter });

        // Shield bar (bottom-left) and weapon (bottom-right)
        Hero h = Hero.S;
        int shield = h != null ? h.shieldLevel : 0;
        int maxShield = h != null ? h.maxShield : 4;
        float segW = sw * 0.045f, segH = sh * 0.02f, y = sh - pad - segH - fs * 1.5f;
        GUI.Label(new Rect(pad, y - 0.2f * fs, sw, fs * 1.5f), "SHIELD", label);
        Color prev = GUI.color;
        for (int i = 0; i < maxShield; i++)
        {
            GUI.color = i < shield ? new Color(0.3f, 0.85f, 1f) : new Color(1f, 1f, 1f, 0.15f);
            GUI.DrawTexture(new Rect(pad + i * (segW + 4), y + fs * 1.4f, segW, segH), Texture2D.whiteTexture);
        }
        GUI.color = prev;

        if (h != null)
        {
            Weapon w0 = h.weapons[0];
            string wn = w0.type == eWeaponType.none ? "-" : w0.type.ToString().ToUpper();
            GUI.Label(new Rect(0, sh - pad - fs * 1.6f, sw - pad, fs * 1.6f),
                      wn + "  x" + h.ActiveWeaponCount(), new GUIStyle(right) { alignment = TextAnchor.LowerRight });
        }

        if (Main.S.elapsed < 8f && !Main.S.gameOver)
        {
            GUI.Label(new Rect(0, sh * 0.9f, sw, fs * 1.6f),
                      "ARROWS / WASD move   |   SPACE fire   |   grab the power-ups!", center);
        }

        if (Main.S.gameOver)
        {
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.DrawTexture(new Rect(0, sh * 0.33f, sw, sh * 0.30f), Texture2D.whiteTexture);
            GUI.color = prev;
            int big = fs * 2;
            center.fontSize = big;
            GUI.Label(new Rect(0, sh * 0.34f, sw, big * 1.5f), "GAME OVER", center);
            center.fontSize = fs;
            GUI.Label(new Rect(0, sh * 0.34f + big * 1.6f, sw, fs * 1.6f), "Final score: " + Main.S.score, center);
            GUI.Label(new Rect(0, sh * 0.34f + big * 1.6f + fs * 1.7f, sw, fs * 1.6f), "Press R to restart", center);
        }
    }
}
