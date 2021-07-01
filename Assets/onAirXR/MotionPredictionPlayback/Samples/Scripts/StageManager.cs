using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StageManager : MonoBehaviour {

    public static bool isClear;

    [SerializeField] private GameObject appleSpawnPos;
    [SerializeField] private Text stageText;

    [SerializeField] private UnityEvent stageStartEvents;
    [SerializeField] private UnityEvent stageClearEvents;

    [SerializeField] private GameObject target;
    [SerializeField] private GameObject dropableApple;

    [SerializeField] private Transform[] poolingPoses;

    private int remainTargetNum;
    private float remainTime;
    private ObjectPooler objectPooler;

    private int stage;
    public int Stage
    {
        get
        {
            return stage;
        }
        set
        {
            stage = value;
        }
    }

	void Awake () {
        isClear = true;
        remainTime = 20;
    }

    void Start()
    {
        objectPooler = GameContentManager.Instance.TargetPooler;

        objectPooler.Pool(target, 20);
    }

    private void Update()
    {
        
        if (remainTime <= 0)
        {
            GameContentManager.Instance.GameOver();
        }

        if (remainTargetNum <= 0 && !isClear)
        {
            if (GameContentManager.isGameOver)
                return;

            StartCoroutine(ClearStage(2f));
        }
    }

    public void OnTimeChange(float time)
    {
        remainTime = time;
    }

    public void StageTargetRemove(GameObject gameObject)
    {
        if (gameObject.tag == "Apple")
            remainTargetNum--;     
    }

    public void StartStage()
    {
        if (GameContentManager.isGameOver)
            return;

        stage++;
        isClear = false;
        remainTargetNum = 0;

        for (int i = 0; i < Random.Range(stage, stage +1); i++)
        {
            GameObject currentObject = objectPooler.GetPool();
            currentObject.transform.position = poolingPoses[Random.Range(0, poolingPoses.Length - 1)].transform.position;

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
