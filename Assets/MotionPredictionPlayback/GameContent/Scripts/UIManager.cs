using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour {

    [SerializeField] private GameObject gameTipUI;
    [SerializeField] private GameObject gameStartUI;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private Text harvestedAppleText;
    [SerializeField] private Text clearStageText;
    [SerializeField] private Slider slider;

    private StageManager stageManager;
    private ObjectPooler objectPooler;
    private Barrel barrel;

    private void Start()
    {
        stageManager = GameContentManager.Instance.StageManger;    
    }

    public void OnGameClearUI(bool active)
    {
        gameStartUI.SetActive(active);
    }

    public void OnGameOverUI(bool active)
    {
        harvestedAppleText.text = "Harvested apples : " + GameContentManager.gettingAppleNum;
        clearStageText.text = "Current stage : " + stageManager.Stage;
        gameTipUI.SetActive(!active);
        gameOverUI.SetActive(active);
    }

    public void OnRetry()
    {
        gameOverUI.SetActive(false);
        gameTipUI.SetActive(true);
        slider.value = slider.maxValue;

        GameContentManager.Instance.ReStart();      
    }

    public void OnTimeMinus()
    {
        stageManager.OnTimeChange(slider.value);
    }

    public void OnTimePlus()
    {
        slider.value = slider.maxValue;
        stageManager.OnTimeChange(slider.maxValue);
    }
}
