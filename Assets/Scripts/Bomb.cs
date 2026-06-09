using UnityEngine;

public class Bomb : MonoBehaviour
{
    private void Awake()
    {
        AddPenaltyLabel();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GetComponent<Collider>().enabled = false;
            GameManager.Instance.HandleBombHit(transform.position);
            Destroy(gameObject, 0.1f);
        }
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
