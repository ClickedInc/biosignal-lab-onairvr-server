using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using TMPro;
using UnityEditor.Tilemaps;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class FruitsSpawn : MonoBehaviour
{
    public static string GetFruitId(int index) {
        switch (index) {
            case 0:
                return "apple";
            case 1:
                return "banana";
            case 2:
                return "pear";
            case 3:
                return "apricot";
            default:
                return "unknown";
        }
    }

    [SerializeField] private AirVRStereoCameraRig rig;
    public Text mission;
    static public GameObject pFruits;
    public List<GameObject> fruits;

    GameObject currentFruits;
    GameObject chkFruit;
    static public int btCount, fruit, rg, dp, piece;
    static public bool chk = true;
    static public bool spawn = true;
    static public bool gameover = false;
    static public bool bound = false;
    public static float fallingSpeed;
    public static int minimumNumberOfCuts;
    public static int maximumNumberOfCuts;
    public static float targetPercent;
    public static float spawnInterval;
    public static bool usePredictiveInput;
    public enum chkF
    {
        사과, 바나나, 배, 살구
    }

    // Start is called before the first frame update
    void Start()
    {

        fruit = Random.Range(0, 4);
        piece = Random.Range(minimumNumberOfCuts, maximumNumberOfCuts);
        rg = Random.Range(0, 4);
    }

    // Update is called once per frame
    void LateUpdate()
    {


        float _x = Random.Range(0.1f, 1f) * (fallingSpeed/2);
        float _z = Random.Range(0.1f, 1f) * (fallingSpeed/2);
        
       



        if (GameManager.currentTime > spawnInterval)
        {
            int R_Value = Random.Range(0, 100);
         
          
            if (chk)
            {
                mission.text = $"자를 횟수: {piece}";
                pFruits = Instantiate(fruits[fruit]);
                pFruits.AddComponent<MoveFruits>();
                pFruits.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
                
                for (int i = 0; i < 4; i++)
                {
                    Destroy(pFruits.transform.GetChild(i).GetComponent<ConstantForce>());
                    pFruits.transform.GetChild(i).GetComponent<Rigidbody>().isKinematic = true;
                }
                chk = false;

            }



            if (spawn)
            {
                Physics.IgnoreLayerCollision(LayerMask.NameToLayer("apple"), LayerMask.NameToLayer("apple"), true);

                bound = false;
                if (R_Value <= targetPercent) { rg = fruit; }
                currentFruits = Instantiate(fruits[rg]);
                currentFruits.transform.position = transform.position;
                currentFruits.transform.localScale = new Vector3(0.013f, 0.013f, 0.013f);
                if(rg==1){currentFruits.transform.localScale = new Vector3(0.014f, 0.014f, 0.014f);}


                for (int i = 0; i < 4; i++)
                {
                    currentFruits.transform.GetChild(i).GetComponent<ConstantForce>().force = new Vector3(0f, -fallingSpeed, 0f);
                }

                spawn = false;

                rig.gameEventEmitter.EmitEvent(rig.gameEventEmitter.gameEventTimestamp, AirVRGameEventEmitter.Type.Fruit, GetFruitId(rg), "appear");

            }
            GameManager.currentTime = 0;

            btCount = 0;




        }

        var gameEventTimestamp = rig.gameEventEmitter.gameEventTimestamp;

        if (rig.predictedMotionProvider.GetButtonDown(false)) {
            rig.gameEventEmitter.EmitEvent(gameEventTimestamp, AirVRGameEventEmitter.Type.Input, "0:0", "actual_press");
        }
        if (rig.predictedMotionProvider.GetButtonUp(false)) {
            rig.gameEventEmitter.EmitEvent(gameEventTimestamp, AirVRGameEventEmitter.Type.Input, "0:0", "actual_release");
        }
        if (rig.predictedMotionProvider.GetButtonDown(true)) {
            rig.gameEventEmitter.EmitEvent(gameEventTimestamp, AirVRGameEventEmitter.Type.Input, "0:0", "predicted_press");
        }
        if (rig.predictedMotionProvider.GetButtonUp(true)) {
            rig.gameEventEmitter.EmitEvent(gameEventTimestamp, AirVRGameEventEmitter.Type.Input, "0:0", "predicted_release");
        }

        if (Input.GetKeyDown(KeyCode.Space) || rig.predictedMotionProvider.GetButtonDown(usePredictiveInput)) {
            if (!gameover && !spawn && !bound)
            {
                
                btCount++;
                AudioPlayer.instance.PlaySliceSound();

                if (btCount <= 3) {
                    rig.gameEventEmitter.EmitEvent(gameEventTimestamp, AirVRGameEventEmitter.Type.Fruit, GetFruitId(rg), "hit");
                }

                if (btCount == 1)
                {
                    Destroy(currentFruits.transform.GetChild(0).GetComponent<FixedJoint>());

                    Destroy(currentFruits.transform.GetChild(2).GetComponent<FixedJoint>());
                    Physics.IgnoreLayerCollision(LayerMask.NameToLayer("apple"), LayerMask.NameToLayer("apple"), false);

                    for (int i = 0; i < 4; i++) { currentFruits.transform.GetChild(i).GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation; }


                    if (rg == 0)
                    {
                        _x = Math.Abs(_x); _z = Math.Abs(_z);
                        currentFruits.transform.GetChild(3).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                        currentFruits.transform.GetChild(3).GetComponent<Rigidbody>().AddForce(_x * -0.3f, 0, 0f, ForceMode.Impulse);
                        currentFruits.transform.GetChild(2).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                        currentFruits.transform.GetChild(2).GetComponent<Rigidbody>().AddForce(_x * -0.3f, 0, 0f, ForceMode.Impulse);


                        currentFruits.transform.GetChild(0).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                        currentFruits.transform.GetChild(0).GetComponent<Rigidbody>().AddForce(_x * 0.3f, 0, 0f, ForceMode.Impulse);

                        currentFruits.transform.GetChild(1).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                        currentFruits.transform.GetChild(1).GetComponent<Rigidbody>().AddForce(_x * 0.3f, 0,0f, ForceMode.Impulse);
                    }
                    else
                    {

                        currentFruits.transform.GetChild(0).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                        currentFruits.transform.GetChild(0).GetComponent<Rigidbody>().AddForce(_x * -0.3f, 0, _z * 0.3f, ForceMode.Impulse);

                        currentFruits.transform.GetChild(1).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                        currentFruits.transform.GetChild(1).GetComponent<Rigidbody>().AddForce(_x * -0.3f, 0, _z * 0.3f, ForceMode.Impulse);



                        currentFruits.transform.GetChild(2).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                        currentFruits.transform.GetChild(2).GetComponent<Rigidbody>().AddForce(_x * 0.3f, 0, _z * -0.3f, ForceMode.Impulse);

                        currentFruits.transform.GetChild(3).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                        currentFruits.transform.GetChild(3).GetComponent<Rigidbody>().AddForce(_x * 0.3f, 0, _z * -0.3f, ForceMode.Impulse);
                    }

                }
                else if (btCount == 2)
                {

                    currentFruits.transform.GetChild(0).GetComponent<Rigidbody>().AddExplosionForce(25f, transform.position, 10f, 10f);
                    currentFruits.transform.GetChild(0).GetComponent<Rigidbody>().AddForce(_x * -0.2f, 0, _z * -0.2f, ForceMode.Impulse);
                    currentFruits.transform.GetChild(1).GetComponent<Rigidbody>().AddExplosionForce(25f, transform.position, 10f, 10f);
                    currentFruits.transform.GetChild(1).GetComponent<Rigidbody>().AddForce(_x * 0.2f, 0, _z * 0.2f, ForceMode.Impulse);
                    currentFruits.transform.GetChild(1).gameObject.tag = "Piece";
                    currentFruits.transform.GetChild(1).GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                    Destroy(currentFruits.transform.GetChild(1).GetComponent<FixedJoint>());

                }
                else if (btCount == 3)
                {


                    currentFruits.transform.GetChild(2).GetComponent<Rigidbody>().AddExplosionForce(20f, transform.position, 10f, 10f);
                    currentFruits.transform.GetChild(3).GetComponent<Rigidbody>().AddExplosionForce(20f, transform.position, 10f, 10f);
                    currentFruits.transform.GetChild(2).GetComponent<Rigidbody>().AddForce(_x * -0.2f, 0, _z * -0.2f, ForceMode.Impulse);
                    currentFruits.transform.GetChild(3).GetComponent<Rigidbody>().AddForce(_x * 0.2f, 0, _z * 0.2f, ForceMode.Impulse);
                    currentFruits.transform.GetChild(2).gameObject.tag = "Piece";
                    currentFruits.transform.GetChild(3).gameObject.tag = "Piece";
                    currentFruits.transform.GetChild(2).GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                    currentFruits.transform.GetChild(3).GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                    Destroy(currentFruits.transform.GetChild(3).GetComponent<FixedJoint>());




                }
                else
                {
                    btCount = 3;
                }                
            }
        }
    }
}
