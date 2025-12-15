using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Weapon;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; set; }

    public AudioSource ShootingChannel;

    public AudioClip M4_8Shot;
    public AudioClip M1911Shot;

    public AudioSource reloadingSoundM1911;
    public AudioSource emptyMagazineSoundM1911;
    public AudioSource reloadingSoundM4_8;

    public AudioClip zombieWalking;
    public AudioClip zombieChase;
    public AudioClip zombieAttack;
    public AudioClip zombieHurt;
    public AudioClip zombieDeath;

    public AudioSource zombieChannel;

    // Cola y coroutine para reproducir sonidos de zombie uno a uno
    private Queue<AudioClip> zombieQueue = new Queue<AudioClip>();
    private Coroutine zombieQueueRoutine;

    // Pequeño buffer para evitar cortes bruscos entre clips
    private const float clipBufferSeconds = 0.05f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void PlayShootingSound(WeaponModel weapon)
    {
        switch(weapon)
        {
            case WeaponModel.Pistol1911:
                ShootingChannel.PlayOneShot(M1911Shot);
                break;
            case WeaponModel.M4_8:
                ShootingChannel.PlayOneShot(M4_8Shot);
                break;
        }
    }

    public void PlayReloadingSound(WeaponModel weapon)
    {
        switch (weapon)
        {
            case WeaponModel.Pistol1911:
                reloadingSoundM1911.Play();
                break;
            case WeaponModel.M4_8:
               reloadingSoundM4_8.Play();
                break;
        }
    }

    // Public API para reproducir sonidos de zombie de forma no solapada.
    public void PlayZombieSound(AudioClip clip)
    {
        if (clip == null) return;
        zombieQueue.Enqueue(clip);
        if (zombieQueueRoutine == null)
        {
            zombieQueueRoutine = StartCoroutine(ProcessZombieQueue());
        }
    }

    // Reproduce un sonido inmediatamente: vacía la cola y lo reproduce sin espera.
    public void PlayZombieSoundImmediate(AudioClip clip)
    {
        if (clip == null) return;
        // Detener la cola en curso
        if (zombieQueueRoutine != null)
        {
            StopCoroutine(zombieQueueRoutine);
            zombieQueueRoutine = null;
        }
        zombieQueue.Clear();

        if (zombieChannel != null)
        {
            zombieChannel.PlayOneShot(clip);
        }
    }

    // Opciones auxiliares para facilitar llamadas desde otros scripts
    public void PlayZombieWalking() => PlayZombieSound(zombieWalking);
    public void PlayZombieChase() => PlayZombieSound(zombieChase);
    public void PlayZombieAttack() => PlayZombieSound(zombieAttack);
    public void PlayZombieHurt() => PlayZombieSound(zombieHurt);
    public void PlayZombieDeath() => PlayZombieSound(zombieDeath);

    // Variante inmediata para muerte/sonidos urgentes
    public void PlayZombieDeathImmediate() => PlayZombieSoundImmediate(zombieDeath);

    private IEnumerator ProcessZombieQueue()
    {
        while (zombieQueue.Count > 0)
        {
            var clip = zombieQueue.Dequeue();
            if (zombieChannel == null || clip == null) continue;

            // Reproducir usando PlayOneShot
            zombieChannel.PlayOneShot(clip);

            // Calcular tiempo de espera en base a la duración del clip y el pitch del AudioSource.
            float pitch = (zombieChannel != null && zombieChannel.pitch != 0f) ? zombieChannel.pitch : 1f;
            float waitTime = (clip.length / Mathf.Abs(pitch)) + clipBufferSeconds;

            // Usamos WaitForSecondsRealtime para que la reproducción no dependa de timeScale.
            yield return new WaitForSecondsRealtime(waitTime);
        }

        zombieQueueRoutine = null;
    }
}
