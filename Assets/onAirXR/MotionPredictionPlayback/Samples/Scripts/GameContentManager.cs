using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameContentManager : MonoBehaviour {

    private static GameContentManager instance;
    public static GameContentManager Instance
    {
        get
        {
            return instance;
        }
    }
    [SerializeField] private StageManager stageManager;
    public StageManager StageManger
    {
        get
        {
            return stageManager;
        }
    }
    [SerializeField] private ObjectPooler targetPooler;
    public ObjectPooler TargetPooler
    {
        get
        {
            return targetPooler;
        }
    }
    [SerializeField] private ObjectPooler applePooler;
    public ObjectPooler ApplePooler
    {
        get
        {
            return applePooler;
        }
    }
    [SerializeField] private Barrel barrel;
    public Barrel Barrel
    {
        get
        {
            return barrel;
        }
    }

    [SerializeField] private UnityEvent gameStartEvents;
    [SerializeField] private UnityEvent gameOverEvents;

    public static int gettingAppleNum;
    public static bool isGameOver;

    void Awake()
    {
        instance = this;
        targetPooler = new ObjectPooler();
        applePooler = new ObjectPooler();
    }

    void Start () {

        StartGame();
    }

    public void StartGame()
    {
        stageManager.StartStage();
    }

    public void GameOver()
    {
        gameOverEvents.Invoke();
        isGameOver = true;
    }

    public void ReStart()
    {
        isGameOver = true;
        StageManager.isClear = false;
        targetPooler.RetureAllPool();
        stageManager.Stage = 0;
        barrel.ClearApple();
        gettingAppleNum = 0;
        isGameOver = false;

        StartGame();
    }

}
