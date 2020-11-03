using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using Random = UnityEngine.Random;

public class ChkFruits : MonoBehaviour
{
    public Text result;
    // Start is called before the first frame update
    int count = 0;
    
    private void Update()
    {

        if (!FruitsSpawn.spawn&&FruitsSpawn.bound)
        {
            if (FruitsSpawn.piece + 1 == FruitsSpawn.btCount && FruitsSpawn.fruit == FruitsSpawn.rg)
            {

                ChekingFruits();
                

            }
            else if (FruitsSpawn.fruit != FruitsSpawn.rg && FruitsSpawn.btCount != 0
                || FruitsSpawn.fruit == FruitsSpawn.rg && FruitsSpawn.btCount != 0 && FruitsSpawn.piece + 1 != FruitsSpawn.btCount
                || FruitsSpawn.fruit == FruitsSpawn.rg && FruitsSpawn.btCount == 0)
            {
                GameoverText();
                Invoke("restart", 3f);

            }

            else
            {
                FruitsSpawn.spawn = true;
            }
        }
    }
    void restart()
    {
        FruitsSpawn.gameover = true;
        SceneManager.LoadScene("test");
        FruitsSpawn.chk = true;
        FruitsSpawn.spawn = true;
        FruitsSpawn.gameover = false;



    }
    void ChekingFruits()
    {
        count++;
        if (PlayerPrefs.GetInt("Best Score", 0) < count)
        { PlayerPrefs.SetInt("Best Score", count); }
        result.text = "현재점수 : " + count.ToString();
        FruitsSpawn.piece = Random.Range(0, 3);
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
