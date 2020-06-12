using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "floor")]
public class FloorNoise : ScriptableObject
{
    [System.Serializable]
    public class ClipClass
    {
        public AudioClip[] clips;
    }
    
    [SerializeField] Material[] mats;
    //[SerializeField] AudioClip[][] clips;
    [SerializeField] ClipClass[] clipClass;
    public Dictionary<Material, ClipClass> matclip;

    int oldN = -1;
    private void OnEnable()
    {
        matclip = new Dictionary<Material, ClipClass>();
        for(int i = 0; i < mats.Length; ++i)
        {
            matclip.Add(mats[i], clipClass[i]);
        }
    }

    public AudioClip getClip(Material mat)
    {
        if (matclip.ContainsKey(mat))
        {
            int n = -1;
            do
            {
                n = Random.Range(0, matclip[mat].clips.Length);
            } while (n == oldN);
            oldN = n;
            return matclip[mat].clips[n];
        }
        int n2 = -1;
        do
        {
            n2 = Random.Range(0, clipClass[0].clips.Length);
        } while (n2== oldN);
        oldN = n2;
        return clipClass[0].clips[n2];
    }
}
