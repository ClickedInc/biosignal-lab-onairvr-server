using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrel : MonoBehaviour {

    private StageManager stageManager;
    private List<GameObject> gettingApples = new List<GameObject>();
    private ObjectPooler applePooler;

    private void Start()
    {
        applePooler = GameContentManager.Instance.ApplePooler;
        stageManager = GameContentManager.Instance.StageManger;
    }

    public void Put(GameObject gameObject)
    {
        if (gameObject.tag == "Apple")
        {
            if (GameContentManager.isGameOver)
                return;

            stageManager.StageTargetRemove(gameObject);
            gettingApples.Add(gameObject);
            GameContentManager.gettingAppleNum++;
        }
    }

    public void ClearApple()
    {
        foreach (GameObject item in gettingApples)
            applePooler.ReturnPool(item);

        gettingApples.Clear();
    }
}
