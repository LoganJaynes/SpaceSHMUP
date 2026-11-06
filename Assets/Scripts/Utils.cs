using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    /// <summary>Returns an instance of every material on the object and its children.</summary>
    public static Material[] GetAllMaterials(GameObject go)
    {
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        List<Material> mats = new List<Material>();
        foreach (Renderer r in rends) mats.Add(r.material);
        return mats.ToArray();
    }

    public static void SetColor(GameObject go, Color c)
    {
        foreach (Material m in GetAllMaterials(go)) m.color = c;
    }
}
