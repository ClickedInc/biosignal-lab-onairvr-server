using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditVaule : MonoBehaviour
{
    public int SpawnTiming;
    public int TargetPercent;//min 25% max 100%
    public float FallingSpeed;

    private void Start()
    {
        if (FallingSpeed<0){FallingSpeed = 0;}
        FruitsSpawn.fallingSpeed = FallingSpeed;
        FruitsSpawn.spawnTiming = SpawnTiming;
        if (TargetPercent < 25) { FruitsSpawn.targetPercent = 25; }
        FruitsSpawn.targetPercent = TargetPercent;
    }
}
