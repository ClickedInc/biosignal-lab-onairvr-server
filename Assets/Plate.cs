using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class Plate : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if(FruitsSpawn.btCount==0)
        {
            //collision.gameObject.GetComponent<Rigidbody>().isKinematic = true;
        }
        FruitsSpawn.bound = true;
        collision.transform.parent.gameObject.AddComponent<DestroySelf>();
        collision.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        //if(FruitsSpawn.btCount == 0)
        
        //{
        //    if (collision.gameObject.tag == "Target")
        //    {

        //        for (int i = 0; i < 4; i++)
        //        {
        //            Destroy(collision.transform.GetChild(i).GetComponent<Rigidbody>());
               
        //        }
        //        //collision.gameObject.GetComponent<MeshCollider>().isTrigger = false;
        //    }
        //}
        //if (collision.gameObject.tag == "fruits"|| collision.gameObject.tag == "Piece")
        //{
        //    FruitsSpawn.bound = true;
        //    collision.gameObject.GetComponent<MeshCollider>().isTrigger = false;
        //    collision.gameObject.AddComponent<DestroySelf>();
        //}
    }
    private void OnTriggerEnter(Collider other)
      
    { FruitsSpawn.bound = true;
        //if (other.gameObject.tag == "fruits" && other.gameObject.tag!="Target") //|| other.gameObject.tag == "Piece")
        //{
        //    FruitsSpawn.bound = true;
        //    other.transform.parent.gameObject.AddComponent<DestroySelf>();
        //    if (FruitsSpawn.btCount == 0)
        //    {

        //        for (int i = 0; i < 4; i++)
        //        {
        //            Debug.Log(other.transform.parent.GetChild(i));
        //            other.transform.parent.GetChild(i).GetComponent<MeshCollider>().isTrigger = false;
        //            //other.transform.parent.GetChild(i).GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

        //        }
        //    }
        //    if(FruitsSpawn.btCount==1)
        //    {
        //        for (int i = 1; i < 4; i++)
        //        {
        //            Debug.Log(other.transform.parent.GetChild(i));
        //            other.transform.parent.GetChild(i).GetComponent<MeshCollider>().isTrigger = false;
        //            other.transform.parent.GetChild(i).GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

        //        }

        //    }
        //    if (FruitsSpawn.btCount == 2)
        //    {
        //        for (int i = 2; i < 4; i++)
        //        {
        //            Debug.Log(other.transform.parent.GetChild(i));
        //            other.transform.parent.GetChild(i).GetComponent<MeshCollider>().isTrigger = false;
        //            other.transform.parent.GetChild(i).GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

        //        }

        //    }
        //    if (FruitsSpawn.btCount == 3)
        //    {

        //            Debug.Log(other.transform.parent.GetChild(3));
        //            other.transform.parent.GetChild(3).GetComponent<MeshCollider>().isTrigger = false;
        //            other.transform.parent.GetChild(3).GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;



        //    }

        //}
        //else
        //{
        //    other.transform.GetComponent<MeshCollider>().isTrigger = false;
        //}


    }
    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.gameObject.tag == "Piece")
    //    {
    //        FruitsSpawn.bound = true;
    //        Debug.Log("피스");
    //        //other.transform.position += new Vector3(0, -0.1f, 0);

    //        other.gameObject.GetComponent<MeshCollider>().isTrigger = false;
    //        other.gameObject.GetComponent<Rigidbody>().drag = 3;
    //        other.gameObject.AddComponent<DestroySelf>();

    //    }

    //    else if (other.gameObject.tag == "fruits")
    //    {
    //        FruitsSpawn.bound = true;

    //        Debug.Log(other.name);
    //        Debug.Log(other.transform.GetSiblingIndex());

    //        //for (int i = 3; i>=0; i--)
    //{
    //    if (other.transform.parent.GetChild(i).tag!="Piece")
    //    {
    //        other.transform.parent.GetChild(i).GetComponent<Rigidbody>().AddExplosionForce(30f, other.transform.parent.GetChild(i).transform.position, 0f, 3f);
    //        other.transform.parent.GetChild(i).gameObject.AddComponent<DestroySelf>();
    //    }


    //}   
    //Destroy(other.GetComponent<Rigidbody>());



}

  
