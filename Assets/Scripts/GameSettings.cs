using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    [SerializeField]
    private float _spawnInterval;

    [SerializeField][Min(0.5f)]
    private float _gravity;

    [SerializeField][Range(0.25f, 1.0f)]
    private float _targetMatchingProbability;

    [SerializeField]
    private bool _usePredictiveInput = true;

    private void Start()
    {
        FruitsSpawn.fallingSpeed = _gravity;
        FruitsSpawn.spawnInterval = _spawnInterval;
        FruitsSpawn.targetPercent = _targetMatchingProbability * 100;
        FruitsSpawn.usePredictiveInput = _usePredictiveInput;
    }
}
