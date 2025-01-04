using UnityEngine;

public class Projectile : MonoBehaviour
{
    private int damageMin;
    private int damageMax;
    private string elementalType;
    private int headShotMultiplier;
    private GameObject hitEffect;
    private GameObject headShotEffect;

    public void Initialize(int damageMin, int damageMax, string elementalType, int headShotMultiplier, GameObject hitEffect, GameObject headShotEffect)
    {
        this.damageMin = damageMin;
        this.damageMax = damageMax;
        this.elementalType = elementalType;
        this.headShotMultiplier = headShotMultiplier;
        this.hitEffect = hitEffect;
        this.headShotEffect = headShotEffect;
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
                    enemyScript.EnemyDamage(Random.Range(damageMin * headShotMultiplier, damageMax * headShotMultiplier), elementalType, true);
                }
                else
                {
                    enemyScript.EnemyDamage(Random.Range(damageMin, damageMax), elementalType, false);
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
                    enemyScript.EnemyDamage(Random.Range(damageMin * headShotMultiplier, damageMax * headShotMultiplier), elementalType, true);
                }
            }
        }

        // Destroy the projectile after collision
        Destroy(gameObject);
    }
}
