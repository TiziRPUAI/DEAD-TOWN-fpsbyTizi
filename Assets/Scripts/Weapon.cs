using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    // Función: Gestiona estado del arma, disparo, recarga, animaciones de recoil y creación de balas.
    [Header("Weapon State")]
    public bool isEquipped = false;

    public bool isShooting, readyToShoot;   
    public float shootingDelay = 0.5f;

    public int bulletsPerBurst = 0;
    public int burstBulletsLeft;

    public float spreadInstensity;

    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 500;
    public float bulletPrefabLifetime = 3f;

    [Header("Damage")]
    public float bulletDamage = 20f;

    public GameObject muzzleEffect;
    internal Animator animator;

    public string idleStateName = "Idle_M1911";
    public string recoilStateName = "Recoil_M1911";

    public float reloadTime;
    public int magazineSize, bulletsLeft;
    public bool isReloading;

    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

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

    private Coroutine recoilCoroutine;

    // Función: Inicializa estados básicos del arma.
    private void Awake()
    {
        readyToShoot = true;
        burstBulletsLeft = bulletsPerBurst;
        animator = GetComponent<Animator>();

        bulletsLeft = magazineSize;
    }

    // Función: Procesa entrada de disparo/recarga y lanza disparos cuando procede.
    void Update()
    {
        if (!isEquipped)
            return;

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
                isShooting = true;
            else
                isShooting = false;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
           
        }

        if (readyToShoot && isShooting && bulletsLeft > 0)
        {
            burstBulletsLeft = bulletsPerBurst;
            FireWeapon();
        }
    }

    // Función: Marca el arma como equipada/no equipada y habilita/deshabilita animator.
    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;

        if (animator != null)
        {
            animator.enabled = equipped;
        }
    }

    // Función: Ejecuta un disparo: instanciar bala, aplicar fuerza, efecto y pasar daño.
    void FireWeapon()
    {
        bulletsLeft--;

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

        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = bulletDamage;
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

    // Función: Inicia la recarga (animación + temporizador).
    private void Reload()
    {
        SoundManager.Instance.PlayReloadingSound(thisWeaponModel);

        animator.SetTrigger("Reload");
        isReloading = true;
        Invoke("ReloadCompleted", reloadTime);
    }

    // Función: Completa la recarga, mueve munición del inventario al cargador y actualiza HUD.
    private void ReloadCompleted()
    {
        if (WeaponManager.Instance == null)
        {
            isReloading = false;
            return;
        }

        int needed = magazineSize - bulletsLeft;
        int available = WeaponManager.Instance.CheckAmmmoLeftFor(thisWeaponModel);

        int toLoad = Math.Min(needed, available);
        bulletsLeft += toLoad;

        if (toLoad > 0)
        {
            WeaponManager.Instance.DecreaseTotalAmmo(toLoad, thisWeaponModel);
        }

        isReloading = false;
        HUDManager.Instance?.RefreshHUD();
    }

    // Función: Reproduce la animación de recoil y programa retorno a idle.
    private void PlayRecoilAnimation()
    {
        if (animator == null) return;

        int recoilLayer = FindStateLayer(recoilStateName);
        if (recoilLayer >= 0)
        {
            int recoilHash = Animator.StringToHash(recoilStateName);
            animator.ResetTrigger("Recoil");
            animator.Play(recoilHash, recoilLayer, 0f);

            if (recoilCoroutine != null)
            {
                StopCoroutine(recoilCoroutine);
                recoilCoroutine = null;
            }

            float recoilDuration = GetClipLength(recoilStateName);
            if (recoilDuration <= 0f)
            {
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

        animator.SetTrigger("Recoil");

        if (recoilCoroutine != null)
        {
            StopCoroutine(recoilCoroutine);
            recoilCoroutine = null;
        }
        recoilCoroutine = StartCoroutine(EnsureReturnToIdleFallback(0.25f));
    }

    // Función: Espera y crossfade a un estado concreto.
    private IEnumerator PlayStateAfterCrossfade(int layer, int stateHash, float delay, float crossfadeTime)
    {
        yield return new WaitForSeconds(delay);
        if (animator == null) yield break;

        if (layer >= 0 && layer < animator.layerCount && animator.HasState(layer, stateHash))
        {
            animator.CrossFade(stateHash, crossfadeTime, layer, 0f);
        }
    }

    // Función: Fallback que fuerza retorno a idle si usamos trigger.
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
            animator.ResetTrigger("Recoil");
        }
    }

    // Función: Busca la capa del Animator donde existe un state por nombre.
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

    // Función: Obtiene la duración de un clip por nombre desde el RuntimeAnimatorController.
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

    // Función: Resetea el estado para permitir nuevo disparo.
    private void ResetShot()
    {
        readyToShoot = true;
    }

    // Función: Calcula la dirección de disparo aplicando spread.
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

    // Función: Destruye la bala tras un tiempo.
    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(bullet);
    }
}