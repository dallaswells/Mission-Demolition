using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowCam : MonoBehaviour {
    public MissionConfig config;
    public Slingshot slingshot;
    public Vector3 castleFocus = new Vector3(16, 5, -35);
    public bool Overview { get; private set; }
    Camera view;
    Vector3 smoothVelocity;
    void Awake() { view = GetComponent<Camera>(); }
    public void SetOverview(bool enabled) { Overview = enabled; }
    public void ShowCastle() {
        transform.position = castleFocus;
        view.orthographicSize = config.launchZoom;
        smoothVelocity = Vector3.zero;
    }
    public void Intro(float fraction) {
        transform.position = Vector3.Lerp(castleFocus, LaunchPosition(), Mathf.SmoothStep(0, 1, fraction));
        view.orthographicSize = config.launchZoom;
    }
    Vector3 LaunchPosition() { return new Vector3(slingshot.launchPoint.position.x + 4, 5.5f, -35); }
    void LateUpdate() {
        var game = MissionDemolition.Instance;
        if (!game || game.State == MissionDemolition.GameState.Intro) return;
        Vector3 desired = LaunchPosition();
        float size = config.launchZoom;
        if (Overview || game.State == MissionDemolition.GameState.GameOver) {
            desired = new Vector3(0, 9, -35);
            size = Mathf.Max(config.zoomOut, 32 / view.aspect);
        } else if (game.ActiveShot) {
            Vector3 p = game.ActiveShot.transform.position;
            desired = new Vector3(Mathf.Clamp(p.x + 4, -14, 21), Mathf.Max(5.5f, p.y + 1), -35);
            size = Mathf.Clamp(config.launchZoom + p.y * .08f, config.launchZoom, 15);
        }
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref smoothVelocity, .25f);
        view.orthographicSize = Mathf.Lerp(view.orthographicSize, size, 1 - Mathf.Exp(-5 * Time.deltaTime));
    }
}
