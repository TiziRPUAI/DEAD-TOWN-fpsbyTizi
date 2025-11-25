using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public bool isShooting, readyToShoot;
    public float shootingDelay = 0.5f;

    public int bulletsPerBurst = 0;
    public int burstBulletsLeft;

    // Spread
    public float spreadInstensity;

    // Bullet
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 500;
    public float bulletPrefabLifetime = 3f;

    // Nuevo: daño por bala configurable por arma
    [Header("Damage")]
    public float bulletDamage = 20f;

    public GameObject muzzleEffect;
    private Animator animator;

    // Nombre del state Idle configurable por arma (puedes poner Idle_M4_8 en el inspector para la M4_8)
    public string idleStateName = "Idle_M1911";

    // Nombre del state Recoil configurable por arma (antes const para M1911)
    public string recoilStateName = "Recoil_M1911";

    //Loading
    public float reloadTime;
    public int magazineSize, bulletsLeft;
    public bool isReloading;

    public enum WeaponModel
    {
        Pistol1911,
        M4_8
    }

    public WeaponModel thisWeaponModel;

    public enum ShootingMode
    {
        Single,
        Burst,
        Auto
    }

    public ShootingMode currentShootingMode;

    // Coroutine que gestiona el retorno a Idle tras el recoil
    private Coroutine recoilCoroutine;

    private void Awake()
    {
        readyToShoot = true;
        burstBulletsLeft = bulletsPerBurst;
        animator = GetComponent<Animator>();

        bulletsLeft = magazineSize;
    }
    void Update()
    {
        if (bulletsLeft == 0 && isShooting) 
        {
            SoundManager.Instance.emptyMagazineSoundM1911.Play();
        }


        if (currentShootingMode == ShootingMode.Auto)
        {
            isShooting = Input.GetKey(KeyCode.Mouse0);
        }
        else if (currentShootingMode == ShootingMode.Single ||
            currentShootingMode == ShootingMode.Burst)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                isShooting = true;
            }
            else
            {
                isShooting = false;
            }
        }
        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && isReloading == false)
        {
            Reload();
        }
        if (bulletsLeft <= 0 && isReloading == false)
        {
           // Reload();
            //return;
        }

        if (readyToShoot && isShooting && bulletsLeft > 0)
        {
            burstBulletsLeft = bulletsPerBurst;
            FireWeapon();
        }
        if (AmmoManager.Instance.ammoDisplay != null && bulletsPerBurst > 0)
        {
            AmmoManager.Instance.ammoDisplay.text = $"{bulletsLeft/bulletsPerBurst}/{magazineSize/bulletsPerBurst}";
        }
    }

    void FireWeapon()
    {

        bulletsLeft--;

        //SoundManager.Instance.shootingSoundM1911.Play();

        SoundManager.Instance.PlayShootingSound(thisWeaponModel);

        readyToShoot = false;

        if (muzzleEffect != null)
        {
            var ps = muzzleEffect.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
        }

        PlayRecoilAnimation();

        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);
        bullet.transform.forward = shootingDirection;
        var rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);
        }

        // Pasar el daño configurado al script Bullet (si existe) y establecer owner para evitar self-hit
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = bulletDamage;
            // Establece owner como el jugador si existe, sino el propio objeto que dispara
            var playerGO = GetComponentInParent<PlayerHealth>()?.gameObject;
            bulletScript.owner = playerGO != null ? playerGO : gameObject;
        }

        StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifetime));

        Invoke("ResetShot", shootingDelay);

        if (currentShootingMode == ShootingMode.Burst && burstBulletsLeft > 1)
        {
            burstBulletsLeft--;
            Invoke("FireWeapon", shootingDelay);
        }
    }

    private void Reload()
    {
        SoundManager.Instance.PlayReloadingSound(thisWeaponModel);

        animator.SetTrigger("Reload");
        isReloading = true;
        Invoke("ReloadCompleted", reloadTime);
    }

    private void ReloadCompleted()
    {
        bulletsLeft = magazineSize;
        isReloading = false;
    }
    private void PlayRecoilAnimation()
    {
        if (animator == null) return;

        int recoilLayer = FindStateLayer(recoilStateName);
        if (recoilLayer >= 0)
        {
            int recoilHash = Animator.StringToHash(recoilStateName);
            animator.ResetTrigger("Recoil");

            // Reproducir directamente el state de recoil en la capa encontrada
            animator.Play(recoilHash, recoilLayer, 0f);

            // Cancelar cualquier coroutine pendiente para evitar solapamientos
            if (recoilCoroutine != null)
            {
                StopCoroutine(recoilCoroutine);
                recoilCoroutine = null;
            }

            // Intentar obtener la duración real del clip
            float recoilDuration = GetClipLength(recoilStateName);
            if (recoilDuration <= 0f)
            {
                // Si no se encontró por nombre, intentar leer el clip actual en la capa
                var clips = animator.GetCurrentAnimatorClipInfo(recoilLayer);
                if (clips != null && clips.Length > 0 && clips[0].clip != null)
                {
                    recoilDuration = clips[0].clip.length;
                }
            }

            if (recoilDuration > 0f)
            {
                int idleLayer = FindStateLayer(idleStateName);
                int idleHash = Animator.StringToHash(idleStateName);
                float wait = recoilDuration / Mathf.Max(0.0001f, animator.speed);
                recoilCoroutine = StartCoroutine(PlayStateAfterCrossfade(idleLayer, idleHash, wait, 0.05f));
            }
            return;
        }

        // Fallback: usar trigger si no existe el state directo
        animator.SetTrigger("Recoil");

        // Asegurar retorno a idle en fallback: cancelar cualquiera y lanzar una garantía de retorno corto
        if (recoilCoroutine != null)
        {
            StopCoroutine(recoilCoroutine);
            recoilCoroutine = null;
        }
        recoilCoroutine = StartCoroutine(EnsureReturnToIdleFallback(0.25f));
    }

    private IEnumerator PlayStateAfterCrossfade(int layer, int stateHash, float delay, float crossfadeTime)
    {
        yield return new WaitForSeconds(delay);
        if (animator == null) yield break;

        if (layer >= 0 && layer < animator.layerCount && animator.HasState(layer, stateHash))
        {
            animator.CrossFade(stateHash, crossfadeTime, layer, 0f);
        }
    }

    // Garantía de retorno cuando usamos trigger (fallback) y no podemos calcular duración
    private IEnumerator EnsureReturnToIdleFallback(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator == null) yield break;

        int idleLayer = FindStateLayer(idleStateName);
        int idleHash = Animator.StringToHash(idleStateName);
        if (idleLayer >= 0 && animator.HasState(idleLayer, idleHash))
        {
            animator.CrossFade(idleHash, 0.05f, idleLayer, 0f);
        }
        else
        {
            // Si no existe idle por nombre, intentar resetear triggers para forzar transición
            animator.ResetTrigger("Recoil");
        }
    }

    // Busca la capa donde existe un state con ese nombre y devuelve el índice de la capa o -1.
    private int FindStateLayer(string stateName)
    {
        if (animator == null) return -1;
        int hash = Animator.StringToHash(stateName);
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (animator.HasState(i, hash)) return i;
        }
        return -1;
    }

    private float GetClipLength(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
        var clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name.Equals(clipName, StringComparison.Ordinal))
            {
                return clips[i].length;
            }
        }
        return 0f;
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }
    public Vector3 CalculateDirectionAndSpread()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100);
        }
        Vector3 direction = targetPoint - bulletSpawn.position;

        float x = UnityEngine.Random.Range(-spreadInstensity, spreadInstensity);
        float y = UnityEngine.Random.Range(-spreadInstensity, spreadInstensity);

        return direction + new Vector3(x, y, 0);
    }
    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(bullet);
    }
}