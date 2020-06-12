using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: The Sun gets the player automatically. If there is an unobsructed raycast between the player
//and the 
public class TheSun : LightDetectionBase
{
    const float lightBaseAddition = 100;
    bool added;
    public float finalLight { get; private set; }
    private void Awake()
    {
        light = GetComponent<Light>();
        fpc = FindObjectOfType<FirstPersonController>();
    }
    protected override void detectPlayer()
    {
        Ray ray = new Ray(fpc.transform.position,transform.rotation*-Vector3.forward);
        RaycastHit hit;
        if (!Physics.Raycast(ray,out hit, Vector3.SqrMagnitude(fpc.transform.position-transform.position)))
        {
            if (!added)
            {
                finalLight = lightBaseAddition / Vector3.Angle(Vector3.up, ray.direction);
                fpc.listOfLightDetection.Add(this);
                added = true;
            }

        }
        else
        {
            if (added)
            {

                added = false;
                fpc.listOfLightDetection.Remove(this);
            }
        }
    }
    public override float calculateLightLevel()
    {
        return finalLight;
    }
    // Update is called once per frame
    void Update()
    {
        detectPlayer();
    }
}
