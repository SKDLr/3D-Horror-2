using UnityEngine;

public class TerrainValleyMarker : MonoBehaviour
{
    public Color gizmoColor = Color.yellow;
    public float gizmoRadius = 1.5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 4f);
    }
}