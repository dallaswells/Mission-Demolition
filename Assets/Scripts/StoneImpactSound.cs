using UnityEngine;

// Attach to each grey castle block. This also catches falling rock-to-rock impacts.
[RequireComponent(typeof(Collider))]
public class StoneImpactSound : MonoBehaviour {
    [Min(0)] public float minimumImpactSpeed = 1f;
    [Min(0)] public float retriggerDelay = .1f;
    float lastImpact = float.NegativeInfinity;
    void OnCollisionEnter(Collision collision) {
        var game = MissionDemolition.Instance;
        if (!game || !game.sound ||
            (game.State != MissionDemolition.GameState.Flying && game.State != MissionDemolition.GameState.LevelComplete)) return;
        if (collision.relativeVelocity.magnitude < minimumImpactSpeed || Time.time - lastImpact < retriggerDelay) return;
        // Unity sends the same rock-to-rock collision to both blocks.
        // Play it only once.
        StoneImpactSound other = collision.collider.GetComponent<StoneImpactSound>();
        if (other && GetInstanceID() > other.GetInstanceID()) return;
        lastImpact = Time.time;
        game.sound.PlayRockImpact();
    }
}
