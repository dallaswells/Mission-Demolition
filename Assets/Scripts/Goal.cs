using UnityEngine;
public class Goal : MonoBehaviour {
    public bool Reached { get; private set; }
    void OnTriggerEnter(Collider other) {
        Projectile projectile = other.GetComponentInParent<Projectile>();
        if (Reached || projectile == null || !projectile.Launched) return;
        Reached = true;
        if (MissionDemolition.Instance) MissionDemolition.Instance.GoalReached();
    }
}
