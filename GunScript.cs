using ML.SDK;
using System.Collections;
using UnityEngine;

public class GunScript : MonoBehaviour
{
    public MLGrab MLGrabComponent;
    public GameObject ShootPoint; // The point where the raycast originates
    public GameObject MuzzleFlash; // The muzzle flash effect
    public GameObject HitEffect; // The hit effect
    public GameObject weaponVisual; // The weapon visual object for recoil

    public int ammo = 10; // Initial ammo count
    public float fireRate = 0.2f; // Time between shots
    public float recoilAmount = 0.1f; // How far back the weapon moves when fired
    public float upwardRecoilAmount = 0.05f; // How far upward the weapon moves when fired
    public float recoilAngleRange = 5f; // Maximum angle for random recoil rotation
    public float recoilResetSpeed = 5f; // Speed at which the weapon returns to its original position

    private float nextFireTime = 0f; // Time until the weapon can fire again
    private Vector3 originalWeaponPosition; // Original position of the weapon visual
    private Quaternion originalWeaponRotation; // Original rotation of the weapon visual

    void Reload()
    {
        ammo = 10;
    }

    void Start()
    {
        // Store the weapon's original position and rotation
        originalWeaponPosition = weaponVisual.transform.localPosition;
        originalWeaponRotation = weaponVisual.transform.localRotation;

        // Add listener to the OnPrimaryTriggerDown event
        MLGrabComponent.OnPrimaryTriggerDown.AddListener(OnPrimaryTriggerDownFunction);
    }

    void Update()
    {
        // Smoothly reset weapon recoil to the original position and rotation
        weaponVisual.transform.localPosition = Vector3.Lerp(weaponVisual.transform.localPosition, originalWeaponPosition, Time.deltaTime * recoilResetSpeed);
        weaponVisual.transform.localRotation = Quaternion.Lerp(weaponVisual.transform.localRotation, originalWeaponRotation, Time.deltaTime * recoilResetSpeed);
    }

    void OnPrimaryTriggerDownFunction()
    {
        // Check if the weapon can fire
        if (Time.time >= nextFireTime && ammo > 0)
        {
            FireWeapon();
        }
        else if (ammo <= 0)
        {
            Debug.Log("Out of ammo!");
            Reload();
        }
    }

    void FireWeapon()
    {
        // Update the time for the next allowed shot
        nextFireTime = Time.time + fireRate;

        // Decrease ammo count
        ammo--;

        // Raycast from the shoot point
        RaycastHit hit;
        if (Physics.Raycast(ShootPoint.transform.position, ShootPoint.transform.forward, out hit))
        {
            // Instantiate the hit effect at the point of impact
            Instantiate(HitEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Debug.Log("Hit: " + hit.collider.name);
            var hitGameObject = hit.collider.gameObject;
            if (hitGameObject.name.Contains("Enemy"))
            {
                Debug.Log("Enemy hit!");
                EnemyAi.EnemyAi checkForReference = (EnemyAi.EnemyAi)hitGameObject.GetComponent(typeof(EnemyAi.EnemyAi));
                if (checkForReference != null)
                {
                    Debug.Log("Retrieved reference attempting to deal damage");
                    checkForReference.EnemyDamage(UnityEngine.Random.Range(15.0f, 35.0f));
                }
            }
        }

        // Instantiate the muzzle flash effect at the shoot point
        Instantiate(MuzzleFlash, ShootPoint.transform.position, ShootPoint.transform.rotation);

        // Apply recoil to the weapon visual
        weaponVisual.transform.localPosition -= new Vector3(0, upwardRecoilAmount, recoilAmount);

        // Apply a random recoil angle
        float randomAngle = Random.Range(-recoilAngleRange, recoilAngleRange);
        weaponVisual.transform.localRotation *= Quaternion.Euler(-randomAngle, randomAngle, 0);
    }

    void OnDestroy()
    {
        // Always unsubscribe from events to prevent memory leaks
        MLGrabComponent.OnPrimaryTriggerDown.RemoveListener(OnPrimaryTriggerDownFunction);
    }
}
