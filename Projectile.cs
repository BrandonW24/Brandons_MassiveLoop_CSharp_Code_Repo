using UnityEngine;

public class Projectile : MonoBehaviour
{
    private int damageMin;
    private int damageMax;
    private string elementalType;
    private int headShotMultiplier;
    private GameObject hitEffect;
    private GameObject headShotEffect;
    private string thisProjectileOwner;


    public void Initialize(int damageMin, int damageMax, string elementalType, int headShotMultiplier, GameObject hitEffect, GameObject headShotEffect, string Owner)
    {
        this.damageMin = damageMin;
        this.damageMax = damageMax;
        this.elementalType = elementalType;
        this.headShotMultiplier = headShotMultiplier;
        this.hitEffect = hitEffect;
        this.headShotEffect = headShotEffect;
        this.thisProjectileOwner = Owner;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var hit = collision.contacts[0]; // Get the first contact point
        GameObject hitGameObject = collision.gameObject;

        // Instantiate hit effect
        Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));

        if (hitGameObject.name.Contains("Enemy"))
        {
            Debug.Log($"Enemy hit! Part : {hitGameObject.name}");
            var enemyScript = (EnemyAi.EnemyAi)hitGameObject.GetComponent(typeof(EnemyAi.EnemyAi));
            if (enemyScript != null)
            {
                if (hitGameObject.name.Contains("Head"))
                {
                    Debug.Log("Headshot detected");
                    Instantiate(headShotEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    enemyScript.EnemyDamage(Random.Range(damageMin * headShotMultiplier, damageMax * headShotMultiplier), elementalType, true, thisProjectileOwner);
                }
                else
                {
                    enemyScript.EnemyDamage(Random.Range(damageMin, damageMax), elementalType, false, thisProjectileOwner);
                }
            }
        }
        else if (hitGameObject.name.Contains("Head"))
        {
            Transform parentTransform = hitGameObject.transform.parent;

            while (parentTransform != null && parentTransform.parent != null)
            {
                parentTransform = parentTransform.parent;
            }

            if (parentTransform != null)
            {
                var enemyScript = (EnemyAi.EnemyAi)parentTransform.GetComponent(typeof(EnemyAi.EnemyAi));
                if (enemyScript != null)
                {
                    Debug.Log("Overall parent EnemyAi found for the head!");
                    Instantiate(headShotEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    enemyScript.EnemyDamage(Random.Range(damageMin * headShotMultiplier, damageMax * headShotMultiplier), elementalType, true, thisProjectileOwner);
                }
            }
        }

        // Destroy the projectile after collision
        Destroy(gameObject);
    }
}
