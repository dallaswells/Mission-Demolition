using UnityEngine;

[CreateAssetMenu(menuName = "Mission Demolition/Configuration")]
public class MissionConfig : ScriptableObject {
    [Header("Camera (class lecture)")]
    [Min(0)] public float initialMoveTime = 2.5f;
    [Min(0)] public float finalShowTime = 1.2f;
    [Min(1)] public float zoomOut = 19f;
    [Min(1)] public float launchZoom = 10f;
    [Header("Slingshot")]
    [Min(.1f)] public float maxPull = 3f;
    [Min(.1f)] public float velocityMultiplier = 10f;
    [Min(1)] public float shotTimeout = 12f;
}
