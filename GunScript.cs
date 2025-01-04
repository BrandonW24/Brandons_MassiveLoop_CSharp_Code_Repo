using ML.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunScript : MonoBehaviour
{
    public MLGrab MLGrabComponent;
    public GameObject ShootPoint;
    public GameObject DefaultMuzzleFlash;
    public GameObject HitEffect;
    public GameObject HeadShotEffect;
    public GameObject weaponVisual;

    public int ammo = 10;
    public float fireRate = 0.2f;
    public float recoilAmount = 0.1f;
    public float upwardRecoilAmount = 0.05f;
    public float recoilAngleRange = 5f;
    public float recoilResetSpeed = 5f;
    public int DamageRange_min;
    public int DamageRange_max;
    public int HeadShotMultiplyer;

    public enum FireMode { Automatic, SemiAutomatic, Shotgun }
    public FireMode currentFireMode;

    public bool useProjectile = false;
    public GameObject bulletPrefab;
    public float projectileForce = 500f;

    public string ElementalTypePlaceholder;

   // public List<GameObject> ElementalMuzzleFlashes; // List of different muzzle flashes for elemental types

    public GameObject FireMuzzle;
    public GameObject IceMuzzle;
    public GameObject NormalMuzzle;
    public GameObject PoisonMuzzle;
    public GameObject ThunderMuzzle;

    private float nextFireTime = 0f;
    private Vector3 originalWeaponPosition;
    private Quaternion originalWeaponRotation;

    private MLPlayer currentGunUser;

    void OnPrimaryGrabBegin()
    {
        currentGunUser = MLGrabComponent.CurrentUser;
    }

    void OnPrimaryGrabEnd()
    {
        currentGunUser = null;
    }

    void Start()
    {
        originalWeaponPosition = weaponVisual.transform.localPosition;
        originalWeaponRotation = weaponVisual.transform.localRotation;
        MLGrabComponent.OnPrimaryTriggerDown.AddListener(OnPrimaryTriggerDownFunction);
        MLGrabComponent.OnPrimaryGrabBegin.AddListener(OnPrimaryGrabBegin);
        MLGrabComponent.OnPrimaryGrabEnd.AddListener(OnPrimaryGrabEnd);
    }

    void Update()
    {
        weaponVisual.transform.localPosition = Vector3.Lerp(weaponVisual.transform.localPosition, originalWeaponPosition, Time.deltaTime * recoilResetSpeed);
        weaponVisual.transform.localRotation = Quaternion.Lerp(weaponVisual.transform.localRotation, originalWeaponRotation, Time.deltaTime * recoilResetSpeed);

        if(currentGunUser != null)
        {
            if(currentGunUser.UserInput != null)
            {
                if (currentGunUser.UserInput.TriggerPress1 == true)
                {

                    if (Time.time >= nextFireTime && ammo > 0)
                    {
                        FireWeapon();
                    }
                    else if (ammo <= 0)
                    {
                        Debug.Log("Out of ammo!");
                        Reload();
                    }
                    //Debug.Log("user is pressing trigger");

                }
            }

            
        }
    }

    void OnPrimaryTriggerDownFunction()
    {
        /*
        if (Time.time >= nextFireTime && ammo > 0)
        {
            switch (currentFireMode)
            {
                case FireMode.Automatic:
                    StartCoroutine(FireAutomatic());
                    break;
                case FireMode.SemiAutomatic:
                    FireWeapon();
                    break;
                case FireMode.Shotgun:
                    FireShotgun();
                    break;
            }
        }
        else if (ammo <= 0)
        {
            Debug.Log("Out of ammo!");
            Reload();
        }*/
    }

    void Reload()
    {
        ammo = 10;
    }

    IEnumerator FireAutomatic()
    {
        while (ammo > 0 && Time.time >= nextFireTime)
        {
            FireWeapon();
            yield return new WaitForSeconds(fireRate);
        }
    }

    void FireShotgun()
    {
        int pellets = 8; // Number of pellets in the shotgun blast
        for (int i = 0; i < pellets; i++)
        {
            Vector3 spread = ShootPoint.transform.forward +
                             new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0);
            ProcessHit(ShootPoint.transform.position, spread.normalized);
        }

        ApplyRecoil();
        InstantiateMuzzleFlash();
    }

    void FireWeapon()
    {
        nextFireTime = Time.time + fireRate;
        ammo--;

        if (useProjectile)
        {
            GameObject bullet = Instantiate(bulletPrefab, ShootPoint.transform.position, ShootPoint.transform.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(ShootPoint.transform.forward * projectileForce, ForceMode.Impulse);
            }

            // Assign the elemental type and damage values to the projectile
            Projectile projectileScript = bullet.GetComponent(typeof(Projectile)) as Projectile;
            if (projectileScript != null)
            {
                projectileScript.Initialize(DamageRange_min, DamageRange_max, ElementalTypePlaceholder, HeadShotMultiplyer, HitEffect, HeadShotEffect);
            }

        }
        else
        {
            ProcessHit(ShootPoint.transform.position, ShootPoint.transform.forward);
        }

        ApplyRecoil();
        InstantiateMuzzleFlash();
    }

    void ProcessHit(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit))
        {
            Instantiate(HitEffect, hit.point, Quaternion.LookRotation(hit.normal));
            var hitGameObject = hit.collider.gameObject;

            if (hitGameObject.name.Contains("Enemy"))
            {
                HandleEnemyHit(hitGameObject, hit);
            }
            else if (hitGameObject.name.Contains("Head"))
            {
                HandleHeadshot(hitGameObject, hit);
            }
        }
    }

    void HandleEnemyHit(GameObject enemy, RaycastHit hit)
    {
        EnemyAi.EnemyAi checkForReference = (EnemyAi.EnemyAi)enemy.GetComponent(typeof(EnemyAi.EnemyAi));
        if (checkForReference != null)
        {
            bool isHeadshot = enemy.name.Contains("Head");
            int damage = isHeadshot
                ? Random.Range(DamageRange_min * HeadShotMultiplyer, DamageRange_max * HeadShotMultiplyer)
                : Random.Range(DamageRange_min, DamageRange_max);

            if (isHeadshot)
            {
                Instantiate(HeadShotEffect, hit.point, Quaternion.LookRotation(hit.normal));
            }

            checkForReference.EnemyDamage(damage, ElementalTypePlaceholder, isHeadshot);
        }
    }

    void HandleHeadshot(GameObject head, RaycastHit hit)
    {
        Transform parentTransform = head.transform.parent;
        while (parentTransform?.parent != null)
        {
            parentTransform = parentTransform.parent;
        }

        if (parentTransform != null)
        {
            EnemyAi.EnemyAi enemyReference = (EnemyAi.EnemyAi)parentTransform.GetComponent(typeof(EnemyAi.EnemyAi));
            if (enemyReference != null)
            {
                Instantiate(HeadShotEffect, hit.point, Quaternion.LookRotation(hit.normal));
                int damage = Random.Range(DamageRange_min * HeadShotMultiplyer, DamageRange_max * HeadShotMultiplyer);
                enemyReference.EnemyDamage(damage, ElementalTypePlaceholder, true);
            }
        }
    }

    void ApplyRecoil()
    {
        weaponVisual.transform.localPosition -= new Vector3(0, upwardRecoilAmount, recoilAmount);
        float randomAngle = Random.Range(-recoilAngleRange, recoilAngleRange);
        weaponVisual.transform.localRotation *= Quaternion.Euler(-randomAngle, randomAngle, 0);
    }

    void InstantiateMuzzleFlash()
    {
        GameObject muzzleFlash = DefaultMuzzleFlash;

        /*
        // Select an elemental muzzle flash if available
        foreach (GameObject flash in ElementalMuzzleFlashes)
        {
            if (flash.name.Contains(ElementalTypePlaceholder))
            {
                muzzleFlash = flash;
                break;
            }
        }
        */

        switch (this.ElementalTypePlaceholder)
        {
            case "Fire":
                muzzleFlash = FireMuzzle;
                break;
            case "Ice":
                muzzleFlash = IceMuzzle;
                break;
            case "Poison":
                muzzleFlash = PoisonMuzzle;
                break;
            case "Thunder":
                muzzleFlash = ThunderMuzzle;
                break;
            default:
                muzzleFlash = NormalMuzzle; // Default to NormalMuzzle if no match
                break;
        }

        Instantiate(muzzleFlash, ShootPoint.transform.position, ShootPoint.transform.rotation);
    }


    void OnDestroy()
    {
        MLGrabComponent.OnPrimaryTriggerDown.RemoveListener(OnPrimaryTriggerDownFunction);
    }
}
