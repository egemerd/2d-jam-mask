using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    WINSOUND,
    CONFETTISOUND,
    BRUSHSOUND,

}
public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioClip[] soundList;
    private static AudioManager instance;
    AudioSource audioSource;

    private void Awake()
    {
        instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume, float pitch)
    {
        instance.audioSource.pitch = pitch;
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], volume);
    }








}
