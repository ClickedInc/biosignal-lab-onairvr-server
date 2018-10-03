using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StageManager : MonoBehaviour {

    [SerializeField] private ObjectPooler objectPooler;
    [SerializeField] private UnityEvent stageStartEvents;
    [SerializeField] private UnityEvent stageClearEvents;
    [SerializeField] private GameObject appleSpawnPos;
    [SerializeField] private Text stageText;

    public GameObject target;

    private List<GameObject> currentStageTargets = new List<GameObject>();
    private int stage;
    private bool isClear;

	// Use this for initialization
	void Awake () {
        isClear = true;
        
        objectPooler.Pool(target, 20);
        StartStage();
    }

    private void Update()
    {
        if (currentStageTargets.Count == 0 && !isClear)
        {
            StartCoroutine(ClearStage(2f));
        }
    }

    public void DestroyTarget(GameObject gameObject)
    {
        currentStageTargets.Remove(gameObject);
    }

    public GameObject dropableApple;
    public void SpawnDropableApple()
    {
        Instantiate(dropableApple, appleSpawnPos.transform.position, Quaternion.identity);
    }

    public void StartStage()
    {
        stage++;
        isClear = false;

        for (int i = 0; i < Random.Range(stage, stage + 2); i++)
        {
            GameObject currentObject = objectPooler.GetPool();
            currentObject.transform.position = new Vector3(Random.Range(-3, 3), Random.Range(2, 3), Random.Range(-3, 3));

            currentStageTargets.Add(currentObject);
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
