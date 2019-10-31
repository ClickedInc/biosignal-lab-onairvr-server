using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour {
   
    public AudioClip catchSound;
    public AudioClip pickSound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayCatchSound()
    {
        audioSource.PlayOneShot(catchSound);
    }

    public void PlayPickSound()
    {
        audioSource.PlayOneShot(pickSound);
    }
}
