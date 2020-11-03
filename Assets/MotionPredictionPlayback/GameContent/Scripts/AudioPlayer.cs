using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{

    public AudioClip sliceSound;

    private AudioSource audioSource;
    public static AudioPlayer instance;

    private void Awake()
    {
        if(AudioPlayer.instance==null)
        {
            AudioPlayer.instance = this;
        }
    }
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

    }

    public void PlaySliceSound()
    {
        audioSource.PlayOneShot(sliceSound);
    }


}
