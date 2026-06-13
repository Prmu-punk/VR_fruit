using UnityEngine;

public class Fruit : MonoBehaviour
{
    public GameObject whole;
    public GameObject sliced;

    private Rigidbody fruitRigidbody;
    private Collider fruitCollider;
    private bool hasBeenSliced;

    public int points = 1;

    private void Awake()
    {
        fruitRigidbody = GetComponent<Rigidbody>();
        fruitCollider = GetComponent<Collider>();
        DownloadedArtLibrary.ApplyFruitVisuals(gameObject, whole, sliced);
        DisableLegacyJuiceEffects();
    }

    private void Slice(Vector3 direction, Vector3 position, Quaternion bladeRotation, float force)
    {
        if (hasBeenSliced)
            return;

        hasBeenSliced = true;
        GameManager.Instance.IncreaseScore(points);

        // Disable the whole fruit
        fruitCollider.enabled = false;
        whole.SetActive(false);

        // Enable the sliced fruit
        sliced.SetActive(true);

        Vector3 sliceDirection = direction.sqrMagnitude > 0.0001f ? direction : bladeRotation * Vector3.up;
        sliced.transform.rotation = Quaternion.LookRotation(bladeRotation * Vector3.forward, sliceDirection.normalized);
        DownloadedArtLibrary.PlayFruitSliceVfx(position, sliceDirection, gameObject);

        Rigidbody[] slices = sliced.GetComponentsInChildren<Rigidbody>();
        Vector3 separationAxis = bladeRotation * Vector3.right;
        if (separationAxis.sqrMagnitude < 0.0001f)
            separationAxis = Vector3.right;
        separationAxis.Normalize();

        float separationForce = Mathf.Max(force * 0.32f, 0.85f);
        float spinForce = Mathf.Max(force * 0.045f, 0.12f);
        float separationOffset = Mathf.Max(transform.lossyScale.x * 0.16f, 0.035f);

        // Add a force to each slice based on the blade direction
        for (int i = 0; i < slices.Length; i++)
        {
            Rigidbody slice = slices[i];
            float side = i == 0 ? -1f : 1f;
            slice.WakeUp();
            slice.transform.position += separationAxis * side * separationOffset;
            slice.velocity = fruitRigidbody.velocity + separationAxis * side * separationForce + sliceDirection.normalized * Mathf.Max(force * 0.06f, 0.12f);
            slice.angularVelocity = separationAxis * side * spinForce;
            slice.AddForceAtPosition(sliceDirection * force * 0.12f, position, ForceMode.Impulse);
            slice.AddForce(separationAxis * side * separationForce * 0.45f, ForceMode.Impulse);
            slice.AddTorque(separationAxis * side * spinForce, ForceMode.Impulse);
        }

        Destroy(gameObject, 3f);
    }

    private void DisableLegacyJuiceEffects()
    {
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particle in particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = false;
            particle.gameObject.SetActive(false);
        }
    }

    public void BeginLifetime(float lifetime)
    {
        StartCoroutine(LifetimeRoutine(lifetime));
    }

    private System.Collections.IEnumerator LifetimeRoutine(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (!hasBeenSliced && GameManager.Instance != null) {
            GameManager.Instance.ReportMissedFruit();
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
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

            if (blade != null) {
                Slice(blade.Direction, blade.Position, blade.Rotation, blade.SliceForce);
            }
        }
    }

}
