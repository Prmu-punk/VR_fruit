using UnityEngine;

public class FloatingMenuFruit : MonoBehaviour
{
    [SerializeField] private float floatAmplitude = 0.12f;
    [SerializeField] private float floatSpeed = 1.4f;
    [SerializeField] private float spinSpeed = 24f;

    private Vector3 startPosition;
    private float phase;

    private void Awake()
    {
        startPosition = transform.position;
        phase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * floatSpeed + phase) * floatAmplitude;
        transform.position = startPosition + Vector3.up * offset;
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }
}
