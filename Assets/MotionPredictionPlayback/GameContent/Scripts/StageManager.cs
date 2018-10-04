using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StageManager : MonoBehaviour {

    [SerializeField] private ObjectPooler targetPooler;

    [SerializeField] private GameObject appleSpawnPos;
    [SerializeField] private Text stageText;

    [SerializeField] private UnityEvent stageStartEvents;
    [SerializeField] private UnityEvent stageClearEvents;

    [SerializeField] private GameObject target;
    [SerializeField] private GameObject dropableApple;

    [SerializeField] private Transform[] poolingPoses;

    //private List<GameObject> currentStageTargets = new List<GameObject>();
    private int remainTargetNum;
    private int stage;
    private bool isClear;

	// Use this for initialization
	void Awake () {
        isClear = true;
        
        targetPooler.MunaulPositionPool(target,27,poolingPoses);
        
    }

    private void Update()
    {
        Debug.Log(remainTargetNum);
        if (remainTargetNum <= 0 && !isClear)
        {
            StartCoroutine(ClearStage(2f));
        }
    }

    public void StageTargetRemove(GameObject gameObject)
    {
        remainTargetNum--;     
    }

    public void StartStage()
    {
        stage++;
        isClear = false;

        for (int i = 0; i < Random.Range(stage, stage +1); i++)
        {
            GameObject currentObject = targetPooler.GetPool();
            //currentObject.transform.position = new Vector3(Random.Range(-4.0f, 4.0f), Random.Range(1.0f, 1.5f), Random.Range(1.0f, 4.0f));

            remainTargetNum++;
        }
    }

    public IEnumerator ClearStage(float delay)
    {
        stageText.text = "Next stage : " + stage.ToString();

        isClear = true;

        stageClearEvents.Invoke();

        yield return new WaitForSeconds(delay);
     
        stageStartEvents.Invoke();
        
        StartStage();
    }
}
