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

    [SerializeField] private AirVRCameraRig rig;
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

    public static int targetPercent;
    public static int spawnTiming;
    public enum chkF
    {
        사과, 바나나, 배, 살구
    }

    // Start is called before the first frame update
    void Start()
    {

        fruit = Random.Range(0, 4);
        piece = Random.Range(0, 3);
        rg = Random.Range(0, 4);
    }

    // Update is called once per frame
    void Update()
    {


        float _x = Random.Range(0.1f, 1f);
        float _z = Random.Range(0.1f, 1f);




        if (GameManager.currentTime > spawnTiming)
        {
            int R_Value = Random.Range(0, 100);

            if (chk)
            {
                mission.text = $"자를 횟수: {piece + 1}";
                pFruits = Instantiate(fruits[fruit]);
                pFruits.transform.position = GameObject.Find("P.Fruits").transform.position;
                pFruits.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
                for (int i = 0; i < 4; i++)
                {
                    Destroy(pFruits.transform.GetChild(i).GetComponent<ConstantForce>());

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
                for (int i = 0; i < 4; i++)
                {
                    currentFruits.transform.GetChild(i).GetComponent<ConstantForce>().force = new Vector3(0f,-fallingSpeed,0f);
                }
                Physics.gravity = new Vector3(0f, -fallingSpeed, 0f);

                spawn = false;

            }
            GameManager.currentTime = 0;

            btCount = 0;




        }
        if (Input.GetKeyDown(KeyCode.Space) || AirVRInput.GetDown(rig, AirVRInput.Button.A))
        {

            if (!gameover && !spawn && !bound)
            {
                {
                    btCount++;
                    AudioPlayer.instance.PlaySliceSound();
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
                            //currentFruits.transform.GetChild(0).gameObject.tag = "Piece";

                            currentFruits.transform.GetChild(2).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                            currentFruits.transform.GetChild(2).GetComponent<Rigidbody>().AddForce(_x * -0.3f, 0, 0f, ForceMode.Impulse);
                            //currentFruits.transform.GetChild(1).gameObject.tag = "Piece";


                            currentFruits.transform.GetChild(0).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                            currentFruits.transform.GetChild(0).GetComponent<Rigidbody>().AddForce(_x * 0.3f, 0, 0f, ForceMode.Impulse);
                           // currentFruits.transform.GetChild(2).gameObject.tag = "Piece";

                            currentFruits.transform.GetChild(1).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                            currentFruits.transform.GetChild(1).GetComponent<Rigidbody>().AddForce(_x * 0.3f, 0,0f, ForceMode.Impulse);
                            //currentFruits.transform.GetChild(3).gameObject.tag = "Piece";
                        }
                        else
                        {

                            currentFruits.transform.GetChild(0).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                            currentFruits.transform.GetChild(0).GetComponent<Rigidbody>().AddForce(_x * -0.3f, 0, _z * 0.3f, ForceMode.Impulse);
                            //currentFruits.transform.GetChild(0).gameObject.tag = "Piece";

                            currentFruits.transform.GetChild(1).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                            currentFruits.transform.GetChild(1).GetComponent<Rigidbody>().AddForce(_x * -0.3f, 0, _z * 0.3f, ForceMode.Impulse);
                            //currentFruits.transform.GetChild(1).gameObject.tag = "Piece";



                            // = null;
                            currentFruits.transform.GetChild(2).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                            currentFruits.transform.GetChild(2).GetComponent<Rigidbody>().AddForce(_x * 0.3f, 0, _z * -0.3f, ForceMode.Impulse);
                            //currentFruits.transform.GetChild(2).gameObject.tag = "Piece";

                            currentFruits.transform.GetChild(3).GetComponent<Rigidbody>().AddExplosionForce(30f, transform.position, 10f, 10f);
                            currentFruits.transform.GetChild(3).GetComponent<Rigidbody>().AddForce(_x * 0.3f, 0, _z * -0.3f, ForceMode.Impulse);
                            //currentFruits.transform.GetChild(3).gameObject.tag = "Piece";
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
}
