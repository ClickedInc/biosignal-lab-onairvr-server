using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    [SerializeField]
    private float _spawnInterval = 3.0f;

    [SerializeField][Min(0.5f)]
    private float _gravity = 2.0f;

    [SerializeField][Range(0.25f, 1.0f)]
    private float _targetMatchingProbability = 0.8f;

    [SerializeField]
    private bool _usePredictiveInput = true;

    [SerializeField][Range(0, 3)]
    private int _minNumberOfCuts = 1;

    [SerializeField][Range(1, 3)]
    private int _maxNumberOfCuts = 1;

    private void Start()
    {
        FruitsSpawn.fallingSpeed = _gravity;
        FruitsSpawn.spawnInterval = _spawnInterval;
        FruitsSpawn.targetPercent = _targetMatchingProbability * 100;
        FruitsSpawn.usePredictiveInput = _usePredictiveInput;
        FruitsSpawn.minimumNumberOfCuts = _minNumberOfCuts;
        FruitsSpawn.maximumNumberOfCuts = Mathf.Max(_minNumberOfCuts, _maxNumberOfCuts) + 1;
    }
}
