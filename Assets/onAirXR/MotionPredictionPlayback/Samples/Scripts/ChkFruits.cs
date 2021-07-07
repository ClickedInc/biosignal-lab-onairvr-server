using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Runtime.Remoting;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;

public class ChkFruits : MonoBehaviour
{
    private AirXRPredictiveCameraRig _cameraRig;
    public Text result;
    // Start is called before the first frame update
    int count = 0;

    private void Awake() {
        _cameraRig = FindObjectOfType<AirXRPredictiveCameraRig>();
    }

    private void Update()
    {

        if (!FruitsSpawn.gameover && !FruitsSpawn.spawn && FruitsSpawn.bound)
        {
            if (FruitsSpawn.piece == FruitsSpawn.btCount && FruitsSpawn.fruit == FruitsSpawn.rg)
            {
                if (_cameraRig?.gameEventEmitter != null) {
                    _cameraRig.gameEventEmitter.EmitEvent(_cameraRig.gameEventEmitter.gameEventTimestamp, AirXRGameEventEmitter.Type.Fruit, FruitsSpawn.GetFruitId(FruitsSpawn.rg), "cleared");
                }

                ChekingFruits();

            }
            else if (FruitsSpawn.fruit != FruitsSpawn.rg && FruitsSpawn.btCount != 0
                || FruitsSpawn.fruit == FruitsSpawn.rg && FruitsSpawn.btCount != 0 && FruitsSpawn.piece != FruitsSpawn.btCount
                || FruitsSpawn.fruit == FruitsSpawn.rg && FruitsSpawn.btCount == 0)
            {
                if (_cameraRig?.gameEventEmitter != null) {
                    _cameraRig.gameEventEmitter.EmitEvent(_cameraRig.gameEventEmitter.gameEventTimestamp, AirXRGameEventEmitter.Type.Fruit, FruitsSpawn.GetFruitId(FruitsSpawn.rg), "missed");
                }

                GameoverText();
                Restart();
            }

            else
            {
                FruitsSpawn.spawn = true;
            }
        }
    }

    async void Restart()
    {
        FruitsSpawn.gameover = true;

        await Task.Delay(3000);

        if (Application.isPlaying == false) {
            return;
        }

        Destroy(FruitsSpawn.pFruits);

        count = 0;
        result.text = "현재점수 : " + count.ToString();
        GameObject.Find("Purpose").GetComponent<Text>().text = "";
        FruitsSpawn.piece = Random.Range(FruitsSpawn.minimumNumberOfCuts, FruitsSpawn.maximumNumberOfCuts);
        FruitsSpawn.fruit = Random.Range(0, 4);
        FruitsSpawn.chk = true;
        FruitsSpawn.spawn = true;
        FruitsSpawn.gameover = false;

    }
    void ChekingFruits()
    {
        GameObject.Find("Purpose").GetComponent<Text>().text = "Great!!";
        AudioPlayer.instance.CorrectSound();
        count++;
        if (PlayerPrefs.GetInt("Best Score", 0) < count)
        { PlayerPrefs.SetInt("Best Score", count); }
        result.text = "현재점수 : " + count.ToString();
        FruitsSpawn.piece = Random.Range(FruitsSpawn.minimumNumberOfCuts, FruitsSpawn.maximumNumberOfCuts);
        FruitsSpawn.fruit = Random.Range(0, 4);
        FruitsSpawn.chk = true;
        Destroy(FruitsSpawn.pFruits);
        FruitsSpawn.rg = Random.Range(0, 4);
        FruitsSpawn.spawn = true;

    }
    void GameoverText()
    {
        GameObject.Find("Purpose").GetComponent<Text>().text = "Game Over";

    }

}
