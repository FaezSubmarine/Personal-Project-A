using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ScriptableFloat : ScriptableObject
{
    [SerializeField] float originalPoints;
    private float _point;
    public bool changed;
    public float point
    {
        get { return _point; }
        set
        {
            if (value != _point)
            {
                changed = true;
            }
            _point = value;
        }
    }
    public void OnEnable()
    {
        _point = originalPoints;
    }
}
