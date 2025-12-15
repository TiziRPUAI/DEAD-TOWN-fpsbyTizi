using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    // El daño que hará esta bala; lo establece Weapon al instanciarla
    public float damage = 20f;

    // Owner evita que la bala dañe al que la dispara
    public GameObject owner;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;

        GameObject colliderObj = collision.collider != null ? collision.collider.gameObject : collision.gameObject;

        // Evitar dañar al owner o a cualquiera de sus hijos (p.ej. arma, jugador)
        if (owner != null)
        {
            if (colliderObj == owner || colliderObj.transform.IsChildOf(owner.transform))
            {
                // Opcional: destruir la bala sin aplicar daño
                Destroy(gameObject);
                return;
            }
        }

        // Buscar etiquetas en el propio collider o en sus padres (solo las etiquetas usadas: "Beer" y "Enemy")
        GameObject beerTagged = FindInParentsWithTag(colliderObj, "Beer");
        GameObject enemyTagged = FindInParentsWithTag(colliderObj, "Enemy");

        
        
        if (beerTagged != null)
        {
            BeerBottle beerBottle = beerTagged.GetComponentInParent<BeerBottle>();
            if (beerBottle != null)
            {
                
                beerBottle.Shatter();
            }
        }

        // Aplicar daño a cualquier IHittable en el objeto o en sus padres
        var hittable = colliderObj.GetComponentInParent<IHittable>();
        if (hittable != null)
        {
            hittable.TakeDamage(damage);
        }

        // Determinar el prefab de impacto según la etiqueta encontrada en padres/hijo
        GameObject prefab = null;
        if (enemyTagged != null)
        {
            prefab = GlobalReferences.Instance != null ? GlobalReferences.Instance.zombieImpactEffectPrefab : null;
        }
        else
        {
            prefab = GlobalReferences.Instance != null ? GlobalReferences.Instance.bulletImpactEffectPrefab : null;
        }

        if (GlobalReferences.Instance == null)
        {
            Debug.LogWarning("GlobalReferences.Instance es null. Asegúrate de tener el objeto GlobalReferences en la escena.");
        }

        if (prefab == null)
        {
            Debug.LogWarning("Prefab de impacto seleccionado es null. Comprueba que esté asignado en GlobalReferences.");
        }

        // Crear efecto de impacto si hay prefab y existe contacto
        if (prefab != null && collision.contacts != null && collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            Debug.Log($"Instantiating impact prefab at {contact.point} normal {contact.normal}");

            GameObject hole = Instantiate(
                prefab,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );

            // Parentear al transform impactado (si existe)
            if (colliderObj != null)
                hole.transform.SetParent(colliderObj.transform, true);
        }

        // Siempre destruir la bala al impactar
        Destroy(gameObject);
    }

    // Busca hacia arriba en la jerarquía un GameObject que tenga la etiqueta dada
    private GameObject FindInParentsWithTag(GameObject obj, string tag)
    {
        if (obj == null) return null;
        Transform t = obj.transform;
        while (t != null)
        {
            if (t.gameObject.CompareTag(tag)) return t.gameObject;
            t = t.parent;
        }
        return null;
    }
}
