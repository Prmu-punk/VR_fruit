using UnityEngine;

public class Bomb : MonoBehaviour
{
    private bool hasBeenHit;

    private void Awake()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        DownloadedArtLibrary.ApplyBombVisuals(gameObject);
        AddPenaltyLabel();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IBladeSliceSource blade = null;
            MonoBehaviour[] behaviours = other.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                blade = behaviour as IBladeSliceSource;
                if (blade != null)
                    break;
            }

            Vector3 direction = blade != null && blade.Direction.sqrMagnitude > 0.0001f ? blade.Direction.normalized : Vector3.up;
            Vector3 hitPosition = blade != null ? blade.Position : transform.position;
            Hit(direction, hitPosition);
        }
    }

    public bool TrySlice(IBladeSliceSource blade)
    {
        if (hasBeenHit || blade == null)
            return false;

        Vector3 direction = blade.Direction.sqrMagnitude > 0.0001f ? blade.Direction.normalized : Vector3.up;
        Hit(direction, blade.Position);
        return true;
    }

    private void Hit(Vector3 direction, Vector3 position)
    {
        if (hasBeenHit)
            return;

        hasBeenHit = true;

        foreach (Collider bombCollider in GetComponentsInChildren<Collider>())
            bombCollider.enabled = false;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;

        DownloadedArtLibrary.PlayBombHitVfx(position, direction);
        GameManager.Instance.HandleBombHit(position);
        Destroy(gameObject, 0.25f);
    }

    private void AddPenaltyLabel()
    {
        GameObject labelObject = new GameObject("Bomb Penalty Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.8f, 0f);

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = "-10";
        label.fontSize = 72;
        label.characterSize = 0.06f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(1f, 0.12f, 0.05f);
    }

}
