using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public abstract class LightDetectionBase : MonoBehaviour
{
    //TODO: work on lights
    protected Light light;
    protected float detectionMultiplier;
    protected FirstPersonController fpc;
    protected abstract void detectPlayer();
    public abstract float calculateLightLevel();

}
