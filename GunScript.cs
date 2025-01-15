using ML.SDK;
using System;
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
    public GameObject ProjectileHitEffect;

    public int ammo = 10;
    public int MaxAmmo;
    public float fireRate = 0.2f;
    public float recoilAmount = 0.1f;
    public float upwardRecoilAmount = 0.05f;
    public float recoilAngleRange = 5f;
    public float recoilResetSpeed = 5f;
    public int DamageRange_min;
    public int DamageRange_max;
    public int HeadShotMultiplyer;
    public float reloadTime = 2f; // Time it takes to reload
    public float spinSpeed = 360f; // Speed of the spin (degrees per second)
    private bool isReloading = false;

    [SerializeField]
    public float spreadAmount = 2.1f; // Adjustable spread value through the Inspector

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
    private bool isFiring = false;
    private bool triggerHeld = false;

    private string hand_L_Or_R;


    const string EVENT_ID_SHOOT = "ShootEvent";
    EventToken GunShoot_Token;


    const string EVENT_ID_RELOAD = "ReloadEvent";
    EventToken GunReload_Token;

    const string EVENT_ROLL_FOR_DAMAGE = "RollForDamageEvent";
    EventToken RollForDamage_Token;

    private int DamageCalcuated;

    public void OnRollForDamage(object[] args)
    {
        int damageRangeMinimum = (int)args[0];
        int damageRangeMaximum = (int)args[1];
        bool isHeadshot = (bool)args[2]; // Pass headshot info as part of the event arguments
        int headshotMultiplier = (int)args[3]; // Headshot multiplier as an argument
        int randomSeed = (int)args[4]; // Seed passed to sync random numbers
        GameObject enemy = (GameObject)args[5];
        string currentUserName = (string)args[6];

        // Initialize the Unity random generator with the provided seed
        UnityEngine.Random.InitState(randomSeed);

        // Calculate damage using Unity's Random
        int damage = isHeadshot
            ? UnityEngine.Random.Range(damageRangeMinimum * headshotMultiplier, damageRangeMaximum * headshotMultiplier + 1)
            : UnityEngine.Random.Range(damageRangeMinimum, damageRangeMaximum + 1);

        // Log the calculated damage for debugging
        Debug.Log($"Damage calculated: {damage}, Headshot: {isHeadshot}, Seed: {randomSeed}");

        // Apply damage to the enemy
        EnemyAi.EnemyAi enemyAI = (EnemyAi.EnemyAi)enemy.GetComponent(typeof(EnemyAi.EnemyAi));
        if (enemyAI != null)
        {
            enemyAI.EnemyDamage(damage, ElementalTypePlaceholder, isHeadshot, currentUserName);
        }
    }




    public void OnGunShoot(object[] args)
    {
        // Check if 'args' array and relevant items are not null
        if (args == null || args.Length == 0 || args[0] == null)
        {
            Debug.LogError("Invalid arguments passed to OnGunShoot.");
            return;
        }

        // Check if 'player' is null before accessing its properties
        if (currentGunUser == null)
        {
            // Debug.LogError("Player object is null.");
            return;
        }

        // Check if the player ActorId is valid
        int actorIdToCheck;
        try
        {
            actorIdToCheck = (int)args[0];
        }
        catch (InvalidCastException)
        {
            Debug.LogError("Invalid type for args[0]. Expected an int.");
            return;
        }

        if (currentGunUser.ActorId == actorIdToCheck)
        {
            nextFireTime = Time.time + fireRate;
            ammo--;

            if (useProjectile)
            {
                GameObject bullet = Instantiate(bulletPrefab, ShootPoint.transform.position, ShootPoint.transform.rotation);
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                Collider bulletCollider = bullet.GetComponent<Collider>();
                Collider gunCollider = GetComponentInChildren<Collider>(); // Assuming the gun has a collider attached to one of its children

                if (bulletCollider != null && gunCollider != null)
                {
                    Physics.IgnoreCollision(bulletCollider, gunCollider);
                    Physics.IgnoreCollision(bulletCollider, this.gameObject.GetComponent<Collider>());
                }

                if (rb != null)
                {
                    rb.AddForce(ShootPoint.transform.forward * projectileForce, ForceMode.Impulse);
                }

                // Assign the elemental type and damage values to the projectile
                Projectile projectileScript = bullet.GetComponent(typeof(Projectile)) as Projectile;
                if (projectileScript != null)
                {
                    projectileScript.Initialize(DamageRange_min, DamageRange_max, ElementalTypePlaceholder, HeadShotMultiplyer, ProjectileHitEffect, HeadShotEffect, currentGunUser.NickName);
                }
            }
            else
            {
                ProcessHit(ShootPoint.transform.position, ShootPoint.transform.forward);
            }

            ApplyRecoil();
            InstantiateMuzzleFlash();
        }

        Debug.Log("Fire event Done");
    }


    void OnPrimaryGrabBegin()
    {
        currentGunUser = MLGrabComponent.CurrentUser;

        Debug.Log($"Hand : {MLGrabComponent.PrimaryHand}");

        // Convert PrimaryHand to string and check if it's not null or empty
        string primaryHandString = MLGrabComponent.PrimaryHand.ToString();
        if (!string.IsNullOrEmpty(primaryHandString))
        {
            // Extract the first character and convert it back to string if needed
            hand_L_Or_R = primaryHandString[0].ToString();
            Debug.Log($"Hand L or R: {hand_L_Or_R}");
        }
        else
        {
            Debug.LogWarning("MLGrabComponent.PrimaryHand is null or empty!");
        }
    }

    void OnPrimaryGrabEnd()
    {
        currentGunUser = null;
    }
    public int GenerateGlobalSeed()
    {
        // You can choose to generate a random seed or use a fixed seed
        // Random example: (Make sure this is consistent across all clients)
        return 123456;  // Fixed seed for testing purposes (you could change this to something more dynamic if needed)
    }
    void Start()
    {
        originalWeaponPosition = weaponVisual.transform.localPosition;
        originalWeaponRotation = weaponVisual.transform.localRotation;
        MLGrabComponent.OnPrimaryTriggerDown.AddListener(OnPrimaryTriggerDownFunction);
        MLGrabComponent.OnPrimaryGrabBegin.AddListener(OnPrimaryGrabBegin);
        MLGrabComponent.OnPrimaryGrabEnd.AddListener(OnPrimaryGrabEnd);

        GunShoot_Token = this.AddEventHandler(EVENT_ID_SHOOT, OnGunShoot);
        RollForDamage_Token = this.AddEventHandler(EVENT_ROLL_FOR_DAMAGE, OnRollForDamage);

        int sharedSeed = GenerateGlobalSeed(); // Generate and sync this value across clients
        UnityEngine.Random.InitState(sharedSeed);
        //  GunReload_Token = this.AddEventHandler(EVENT_ID_RELOAD, OnGunReload);

    }

    void Update()
    {
        // Smoothly reset weapon visual position and rotation
        weaponVisual.transform.localPosition = Vector3.Lerp(weaponVisual.transform.localPosition, originalWeaponPosition, Time.deltaTime * recoilResetSpeed);
        weaponVisual.transform.localRotation = Quaternion.Lerp(weaponVisual.transform.localRotation, originalWeaponRotation, Time.deltaTime * recoilResetSpeed);

        if (currentGunUser != null && currentGunUser.UserInput != null)
        {
            // Handle TriggerPress1
            if (currentGunUser.UserInput.TriggerPress1 && MassiveLoopClient.IsInDesktopMode)
            {
                if (Time.time >= nextFireTime && ammo > 0)
                {
                    switch (currentFireMode)
                    {
                        case FireMode.Automatic:
                            if (!isFiring)
                            {
                                isFiring = true; // Start firing automatically
                                StartCoroutine(FireAutomatic());
                            }
                            break;

                        case FireMode.SemiAutomatic:
                            if (!triggerHeld)
                            {
                                triggerHeld = true; // Ensure one fire per click
                                FireWeapon(currentGunUser.ActorId);
                            }
                            break;

                        case FireMode.Shotgun:
                            if (!triggerHeld)
                            {
                                triggerHeld = true; // Ensure one fire per click
                                FireShotgun();
                            }
                            break;
                    }
                }
                else if (ammo <= 0)
                {
                    Debug.Log("Out of ammo!");
                    Reload();
                }
                //handle vr mode case, TODO, add in  functionality that tells which hand is the main grab hand
            }
            else if (currentGunUser.UserInput.TriggerPress2 && !MassiveLoopClient.IsInDesktopMode && hand_L_Or_R == "R")
            {
                if (Time.time >= nextFireTime && ammo > 0)
                {
                    switch (currentFireMode)
                    {
                        case FireMode.Automatic:
                            if (!isFiring)
                            {
                                isFiring = true; // Start firing automatically
                                StartCoroutine(FireAutomatic());
                            }
                            break;

                        case FireMode.SemiAutomatic:
                            if (!triggerHeld)
                            {
                                triggerHeld = true; // Ensure one fire per click
                                FireWeapon(currentGunUser.ActorId);
                            }
                            break;

                        case FireMode.Shotgun:
                            if (!triggerHeld)
                            {
                                triggerHeld = true; // Ensure one fire per click
                                FireShotgun();
                            }
                            break;
                    }
                }
                else if (ammo <= 0)
                {
                    Debug.Log("Out of ammo!");
                    Reload();
                }
            }
            else if (currentGunUser.UserInput.TriggerPress1 && !MassiveLoopClient.IsInDesktopMode && hand_L_Or_R == "L")
            {
                if (Time.time >= nextFireTime && ammo > 0)
                {
                    switch (currentFireMode)
                    {
                        case FireMode.Automatic:
                            if (!isFiring)
                            {
                                isFiring = true; // Start firing automatically
                                StartCoroutine(FireAutomatic());
                            }
                            break;

                        case FireMode.SemiAutomatic:
                            if (!triggerHeld)
                            {
                                triggerHeld = true; // Ensure one fire per click
                                FireWeapon(currentGunUser.ActorId);
                            }
                            break;

                        case FireMode.Shotgun:
                            if (!triggerHeld)
                            {
                                triggerHeld = true; // Ensure one fire per click
                                FireShotgun();
                            }
                            break;
                    }
                }
                else if (ammo <= 0)
                {
                    Debug.Log("Out of ammo!");
                    Reload();
                }
            }
            else
            {
                // Stop automatic firing when the trigger is released
                isFiring = false;
                triggerHeld = false;
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
        if (isReloading) return; // Prevent multiple reloads at once

        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;

        float elapsedTime = 0f;
        Vector3 originalRotation = weaponVisual.transform.localEulerAngles;

        // Spin the weaponVisual during reload
        while (elapsedTime < reloadTime)
        {
            float rotationAmount = spinSpeed * Time.deltaTime;
            weaponVisual.transform.Rotate(Vector3.right, rotationAmount, Space.Self);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset rotation (optional: comment this out if you want to keep the rotation continuous)
        weaponVisual.transform.localEulerAngles = originalRotation;

        // Complete the reload
        ammo = MaxAmmo;

        isReloading = false;
    }

    IEnumerator FireAutomatic()
    {
        while (isFiring && ammo > 0)
        {
            FireWeapon(currentGunUser.ActorId);
            yield return new WaitForSeconds(fireRate);
        }
    }

    void FireShotgun()
    {
        int pellets = 8; // Number of pellets in the shotgun blast
        for (int i = 0; i < pellets; i++)
        {
            // Calculate spread based on the spreadAmount
            Vector3 spread = ShootPoint.transform.forward + new Vector3(
                UnityEngine.Random.Range(-spreadAmount, spreadAmount),
                UnityEngine.Random.Range(-spreadAmount, spreadAmount),
                0);
            ProcessHit(ShootPoint.transform.position, spread.normalized);
        }

        ApplyRecoil();
        InstantiateMuzzleFlash();
    }

    void FireWeapon(int z)
    {
        Debug.Log("Fire event invoked");
        Debug.Log("Player actor ID : " + z);
        this.InvokeNetwork(EVENT_ID_SHOOT, EventTarget.All, null, z);
    }


    void ProcessHit(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit))
        {
            // Instantiate the hit effect
            GameObject instantiatedHitEffect = Instantiate(HitEffect, hit.point, Quaternion.LookRotation(hit.normal));

          //  UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

            // Destroy the hit effect after a delay
            float destroyDelay = 2.0f; // Adjust this delay as needed
            Destroy(instantiatedHitEffect, destroyDelay);

            // Process the hit target
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

    //TODO : Make each client see the same number roll
    void HandleEnemyHit(GameObject enemy, RaycastHit hit)
    {
        EnemyAi.EnemyAi checkForReference = (EnemyAi.EnemyAi)enemy.GetComponent(typeof(EnemyAi.EnemyAi));
        Debug.Log("Enemy hit!");
        if (checkForReference != null)
        {
            bool isHeadshot = enemy.name.Contains("Head");
            int randomSeed = UnityEngine.Random.Range(0, int.MaxValue); // Generate a random seed

            // Trigger the OnRollForDamage event with the seed
            /*
            this.InvokeNetwork(EVENT_ROLL_FOR_DAMAGE, EventTarget.All, null,
                DamageRange_min,
                DamageRange_max,
                isHeadshot,
                HeadShotMultiplyer,
                randomSeed,
                enemy,
                currentGunUser.NickName
            );
            */

            if (checkForReference != null)
            {
                checkForReference.EnemyDamage(UnityEngine.Random.Range(DamageRange_min, DamageRange_max), ElementalTypePlaceholder, false, currentGunUser.NickName);

            }

            if (isHeadshot)
            {
                Instantiate(HeadShotEffect, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
    }


    void HandleHeadshot(GameObject head, RaycastHit hit)
    {
        // Traverse the hierarchy to find the topmost parent
        Transform parentTransform = head.transform.parent;
        while (parentTransform?.parent != null)
        {
            parentTransform = parentTransform.parent;
        }

        if (parentTransform != null)
        {
            // Get the EnemyAi component
            EnemyAi.EnemyAi enemyReference = (EnemyAi.EnemyAi)parentTransform.GetComponent(typeof(EnemyAi.EnemyAi));
            if (enemyReference != null)
            {
                Debug.Log("Headshot detected!");

                // Generate a random seed for consistent damage calculation across clients
                int randomSeed = UnityEngine.Random.Range(0, int.MaxValue);
                /*
                // Trigger the OnRollForDamage event for all clients
                this.InvokeNetwork(EVENT_ROLL_FOR_DAMAGE, EventTarget.All, null,
                    DamageRange_min,               // Minimum damage range
                    DamageRange_max,               // Maximum damage range
                    1,                             // Headshot (pass as int: 1 = true)
                    HeadShotMultiplyer,            // Headshot multiplier
                    randomSeed,                    // Random seed for sync
                    parentTransform.gameObject,    // Enemy GameObject reference
                    currentGunUser.NickName        // Username of the current gun user
                );
                */
             //   EnemyAi.EnemyAi enemyAI = (EnemyAi.EnemyAi)enemy.GetComponent(typeof(EnemyAi.EnemyAi));
                if (enemyReference != null)
                {
                    enemyReference.EnemyDamage(UnityEngine.Random.Range(DamageRange_min * HeadShotMultiplyer, DamageRange_max * HeadShotMultiplyer), ElementalTypePlaceholder, true, currentGunUser.NickName);

                }

                // Instantiate the headshot effect at the hit point
                Instantiate(HeadShotEffect, hit.point, Quaternion.LookRotation(hit.normal));
            }
            else
            {
                Debug.LogWarning($"EnemyAI component not found on the parent object '{parentTransform.gameObject.name}'.");
            }
        }
        else
        {
            Debug.LogWarning("No valid parent object found for the headshot.");
        }
    }



    void ApplyRecoil()
    {
        weaponVisual.transform.localPosition -= new Vector3(0, upwardRecoilAmount, recoilAmount);
        float randomAngle = UnityEngine.Random.Range(-recoilAngleRange, recoilAngleRange);
        weaponVisual.transform.localRotation *= Quaternion.Euler(-randomAngle, randomAngle, 0);
    }

    void InstantiateMuzzleFlash()
    {
        GameObject muzzleFlash = DefaultMuzzleFlash;

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

        // Instantiate the muzzle flash
        GameObject instantiatedFlash = Instantiate(muzzleFlash, ShootPoint.transform.position, ShootPoint.transform.rotation);

        instantiatedFlash.transform.parent = this.gameObject.transform;

        // Destroy the muzzle flash after a delay
        float destroyDelay = 1.0f; // Adjust the delay as needed
        Destroy(instantiatedFlash, destroyDelay);
    }


    void OnDestroy()
    {
        MLGrabComponent.OnPrimaryTriggerDown.RemoveListener(OnPrimaryTriggerDownFunction);
    }
}
