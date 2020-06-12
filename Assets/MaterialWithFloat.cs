using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="wall")]
public class MaterialWithFloat : ScriptableObject
{
    [SerializeField] Material[] mats;
    [SerializeField] float[] quantatives;
    public Dictionary<Material, float> wallWithFloat;
    private void OnEnable()
    {
        wallWithFloat = new Dictionary<Material, float>();
        for(int i=0; i < mats.Length; ++i)
        {
            wallWithFloat.Add(mats[i], quantatives[i]);
        }
    }
    public float FindSTC(Material mat)
    {
        if (wallWithFloat.ContainsKey(mat))
        {
            return wallWithFloat[mat];
        }
        return 0;
    }
    public float normalizedDecibel(float value)
    {
        return (value + 80) / 80;
    }
}
