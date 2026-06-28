using UnityEngine;

/// <summary>
/// Marker for cover/hiding points in the generated fog terrain scene.
/// Your player hiding script can search for this component near the player.
/// </summary>
public class HideSpotMarker : MonoBehaviour
{
    public float safeRadius = 3.5f;
    public string hideType = "Cover";

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, safeRadius);
    }
}
