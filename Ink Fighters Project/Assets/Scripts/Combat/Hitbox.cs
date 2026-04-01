using UnityEngine;
using System.Collections.Generic;

public class Hitbox : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private Collider hitboxCollider;
    [SerializeField] private LayerMask targetLayer;

    [Header("VFX")]
    [SerializeField] private GameObject hitImpactPrefab;


    private GameObject myOwner;

    private List<GameObject> alreadyHitObjects = new List<GameObject>();

    public void Initialize(GameObject owner)
    {
        myOwner = owner;
        hitboxCollider.enabled = false;
    }

    public void EnableHitbox()
    {
        alreadyHitObjects.Clear();
        hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        hitboxCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            Hurtbox hurtbox = other.GetComponent<Hurtbox>();

            if (hurtbox != null)
            {
                GameObject targetRoot = hurtbox.GetOwnerObject();
                if (targetRoot == myOwner) return;

                if (alreadyHitObjects.Contains(targetRoot)) return;

                bool wasBlocked = hurtbox.OnHit(damage);

                // [AI]Informacja dla kontrolera
                var myFighter = myOwner.GetComponent<FighterController>();
                if (myFighter != null)
                {
                    // [AI]Rejestracja trafienia
                    myFighter.RegisterHit();

                    // [AI]Logika nagrod
                    if (myFighter.myAgent != null)
                    {
                        if (wasBlocked)
                        {
                            // Nagroda za presjê (nawet jak blokuje)
                            myFighter.myAgent.AddReward(1.5f);
                        }
                        else
                        {

                            float hitReward = 20f;

                            // 2. Bonus za dobijanie
                            var enemyController = targetRoot.GetComponent<FighterController>();
                            if (enemyController != null)
                            {
                                float enemyHpPercent = enemyController.GetCurrentHealth() / enemyController.GetMaxHealth();

                                // Jeœli wróg ma mniej ni¿ po³owê (0.5), to dostajemy bonus +10 (razem 40)
                                if (enemyHpPercent < 0.5f)
                                {
                                    hitReward += 5f;
                                }
                            }

                            myFighter.myAgent.AddReward(hitReward);
                        }
                    }
                }

                if (!wasBlocked && hitImpactPrefab != null)
                {
                    Vector3 contactPoint = other.ClosestPoint(transform.position);
                    GameObject vfx = Instantiate(hitImpactPrefab, contactPoint, Quaternion.identity);
                    Destroy(vfx, 1.0f);
                }

                alreadyHitObjects.Add(targetRoot);
            }
        }
    }
}
