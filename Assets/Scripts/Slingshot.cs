using UnityEngine;
using UnityEngine.EventSystems;

public class Slingshot : MonoBehaviour {
    public MissionConfig config;
    public Projectile projectilePrefab;
    public Transform launchPoint, bandLeft, bandRight;
    public LineRenderer rubberBand, trajectory;
    public AudioSource audioSource;
    public AudioClip snap;
    public Projectile Loaded { get; private set; }
    public bool IsDragging { get; private set; }
    public bool ShowTrajectory { get; set; } = true;
    public float PullFraction { get; private set; }
    public bool CanAim { get; set; }

    void Start() { ResetBand(); }
    public void Load() {
        CancelAim();
        if (Loaded) Destroy(Loaded.gameObject);
        Loaded = Instantiate(projectilePrefab, launchPoint.position, Quaternion.identity);
        Loaded.name = "Projectile (loaded)";
        Loaded.SetBigManMode(MissionDemolition.Instance && MissionDemolition.Instance.BigManMode);
        ResetBand();
    }
    void Update() {

        if (!CanAim || Loaded == null) { if (IsDragging) CancelAim(); return; }
        if (Input.GetMouseButtonDown(0) && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject())) {
            Vector3 mouse = MouseWorld();
            if (Vector2.Distance(mouse, Loaded.transform.position) < Mathf.Max(1.5f, Loaded.WorldRadius)) IsDragging = true;
        }
        if (!IsDragging) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) { CancelAim(); return; }
        Vector3 pull = Vector3.ClampMagnitude(MouseWorld() - launchPoint.position, config.maxPull);
        pull.z = 0;
        pull.x = Mathf.Min(0, pull.x);
        pull.y = Mathf.Max(pull.y, Loaded.WorldRadius + .02f - launchPoint.position.y);
        Loaded.transform.position = launchPoint.position + pull;
        PullFraction = pull.magnitude / config.maxPull;
        rubberBand.SetPosition(1, Loaded.transform.position + Vector3.back * .05f);
        DrawTrajectory(Loaded.transform.position, -pull * config.velocityMultiplier);
        if (Input.GetMouseButtonUp(0)) {
            if (pull.magnitude < .12f) { CancelAim(); return; }
            Fire(-pull * config.velocityMultiplier);
        }
    }
    Vector3 MouseWorld() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : launchPoint.position;
    }
    public Projectile Fire(Vector3 velocity) {
        if (!Loaded || !CanAim) return null;
        Projectile shot = Loaded;
        Loaded = null;
        IsDragging = false;
        PullFraction = 0;
        trajectory.positionCount = 0;
        ResetBand();
        shot.Launch(velocity);
        if (audioSource && snap) audioSource.PlayOneShot(snap);
        MissionDemolition.Instance.ShotLaunched(shot);
        return shot;
    }
    public void CancelAim() {
        IsDragging = false;
        PullFraction = 0;
        if (Loaded) Loaded.transform.position = launchPoint.position;
        if (trajectory) trajectory.positionCount = 0;
        ResetBand();
    }
    void ResetBand() {
        if (!rubberBand || !launchPoint) return;
        rubberBand.positionCount = 3;
        rubberBand.SetPositions(new [] { bandLeft.position, launchPoint.position, bandRight.position });
    }
    void DrawTrajectory(Vector3 position, Vector3 velocity) {
        trajectory.positionCount = 0;
        if (!ShowTrajectory) return;
        Vector3 previous = position;
        for (int i = 0; i < 45; i++) {
            float t = i * .065f;
            Vector3 next = position + velocity * t + .5f * Physics.gravity * t * t;
            float radius = Loaded ? Loaded.WorldRadius : .45f;
            if (next.y < radius) break;
            Vector3 segment = next - previous;
            if (i > 0 && Physics.SphereCast(previous, radius, segment.normalized, out RaycastHit hit, segment.magnitude, 1 << 8, QueryTriggerInteraction.Ignore)) break;
            trajectory.positionCount = i + 1;
            trajectory.SetPosition(i, next + Vector3.back * .6f);
            previous = next;
        }
    }
    void OnApplicationFocus(bool focus) { if (!focus) CancelAim(); }
}
