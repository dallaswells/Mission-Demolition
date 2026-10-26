using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour {
    public Rigidbody Body { get; private set; }
    public bool Launched { get; private set; }
    public TrailRenderer trail;
    public AudioSource flightAudio;
    public AudioClip flightClip;
    public bool HasHitObject { get; private set; }
    Vector3 normalScale;
    public float WorldRadius { get { return GetComponent<SphereCollider>().radius * transform.lossyScale.x; } }
    void Awake() { Body = GetComponent<Rigidbody>(); normalScale = transform.localScale; }
    public void SetBigManMode(bool enabled) {
        transform.localScale = normalScale * (enabled ? 5f : 1f);
        if (enabled && transform.position.y < WorldRadius + .02f) {
            Vector3 position = transform.position;
            position.y = WorldRadius + .02f;
            transform.position = position;
            Body.position = position;
        }
    }
    public void Launch(Vector3 velocity) {
        Launched = true;
        HasHitObject = false;
        if (flightAudio && flightClip) {
            flightAudio.Stop();
            flightAudio.clip = flightClip;
            flightAudio.loop = false;
            flightAudio.Play();
        }
        Body.isKinematic = false;
        Body.velocity = velocity;
        if (trail) { trail.Clear(); trail.emitting = true; }
    }
    void OnCollisionEnter(Collision collision) { StopFlightOnFirstHit(); }
    void OnTriggerEnter(Collider other) { StopFlightOnFirstHit(); }
    void StopFlightOnFirstHit() {
        if (!Launched || HasHitObject) return;
        HasHitObject = true;
        if (flightAudio) flightAudio.Stop();
    }
    void OnDisable() { if (flightAudio) flightAudio.Stop(); }
}
