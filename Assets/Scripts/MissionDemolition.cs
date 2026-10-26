using System;
using System.Collections;
using UnityEngine;

public class MissionDemolition : MonoBehaviour {
    public enum GameState { Intro, Ready, Flying, LevelComplete, GameOver }
    public static MissionDemolition Instance { get; private set; }
    public MissionConfig config;
    public CastleDefinition[] castles;
    public Slingshot slingshot;
    public FollowCam followCam;
    public AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip gameOverSound;
    public bool BigManMode { get; private set; }
    public MissionAudio sound;
    public GameState State { get; private set; }
    public int LevelIndex { get; private set; }
    public int Shots { get; private set; }
    public int TotalShots { get; private set; }
    public int[] Results { get; private set; }
    public Projectile ActiveShot { get; private set; }
    public event Action Changed; // Observer: the HUD listens without controlling the game.
    GameObject castle;
    float launchedAt, settledTime, nextLevelAt;

    void Awake() { Instance = this; Results = new int[castles.Length]; }
    void Start() { BeginLevel(0); }
    void OnDestroy() { if (Instance == this) Instance = null; }
    void SetState(GameState state) {
        bool enteringGameOver = state == GameState.GameOver && State != GameState.GameOver;
        State = state;
        if (enteringGameOver) {
            if (sound) sound.StopForGameOver();
            if (audioSource && gameOverSound) {
                audioSource.Stop();
                audioSource.clip = gameOverSound;
                audioSource.loop = false;
                audioSource.Play();
            }
        }
        slingshot.CanAim = state == GameState.Ready;
        Changed?.Invoke();
    }
    public void BeginLevel(int index) {
        StopAllCoroutines();
        foreach (Projectile p in FindObjectsOfType<Projectile>()) Destroy(p.gameObject);
        if (castle) Destroy(castle);
        LevelIndex = index;
        if (sound) sound.RestartMusic(index);
        Shots = 0;
        ActiveShot = null;
        settledTime = 0;
        castle = Instantiate(castles[index].prefab, new Vector3(16, 0, 0), Quaternion.identity);
        castle.name = castles[index].castleName;
        followCam.SetOverview(false);
        SetState(GameState.Intro);
        StartCoroutine(OpeningMove());
    }
    IEnumerator OpeningMove() {
        followCam.ShowCastle();
        yield return new WaitForSeconds(config.finalShowTime);
        float elapsed = 0;
        while (elapsed < config.initialMoveTime) {
            elapsed += Time.deltaTime;
            followCam.Intro(Mathf.Clamp01(elapsed / Mathf.Max(.01f, config.initialMoveTime)));
            yield return null;
        }
        slingshot.Load();
        SetState(GameState.Ready);
    }
    public void SkipIntro() {
        if (State != GameState.Intro) return;
        StopAllCoroutines();
        followCam.Intro(1);
        slingshot.Load();
        SetState(GameState.Ready);
    }
    public void ShotLaunched(Projectile projectile) {
        if (State != GameState.Ready) return;
        Shots++;
        TotalShots++;
        ActiveShot = projectile;
        launchedAt = Time.time;
        settledTime = 0;
        SetState(GameState.Flying);
    }
    void Update() {
        if (Input.GetKeyDown(KeyCode.B)) ToggleBigManMode();
        if (Input.GetKeyDown(KeyCode.V)) ToggleView();
        if (Input.GetKeyDown(KeyCode.T)) ToggleTrajectory();
        if (Input.GetKeyDown(KeyCode.Space)) { if (State == GameState.Intro) SkipIntro(); else if (State == GameState.Flying) Reload(); }
        if (Input.GetKeyDown(KeyCode.R) && State != GameState.GameOver) RetryLevel();
        if (State == GameState.LevelComplete && Time.time >= nextLevelAt) {
            if (LevelIndex + 1 == castles.Length) SetState(GameState.GameOver);
            else BeginLevel(LevelIndex + 1);
        }
        if (State != GameState.Flying) return;
        if (!ActiveShot) { Reload(); return; }
        if (ActiveShot.Body.velocity.sqrMagnitude < .12f) settledTime += Time.deltaTime;
        else settledTime = 0;
        Vector3 position = ActiveShot.transform.position;
        if (settledTime > 1.1f || Time.time - launchedAt > config.shotTimeout || position.y < -5 || position.x > 65 || position.x < -45) Reload();
    }
    public void Reload() {
        if (State != GameState.Flying) return;
        if (ActiveShot) Destroy(ActiveShot.gameObject);
        ActiveShot = null;
        slingshot.Load();
        SetState(GameState.Ready);
    }
    public void GoalReached() {
        if (State != GameState.Flying) return;
        Results[LevelIndex] = Shots;
        nextLevelAt = Time.time + 2;
        if (audioSource && successSound) audioSource.PlayOneShot(successSound);
        SetState(GameState.LevelComplete);
    }
    public void RetryLevel() {
        if (State == GameState.GameOver || State == GameState.LevelComplete) return;
        // Retried shots remain in the total, so resetting cannot improve the run total.
        // no cheating L XD
        BeginLevel(LevelIndex);
    }
    public void PlayAgain() {
        BigManMode = false;
        if (sound) sound.SetBigManMode(false);
        if (audioSource) audioSource.Stop();
        TotalShots = 0;
        Results = new int[castles.Length];
        BeginLevel(0);
    }
    public void ToggleBigManMode() {
        if (State == GameState.GameOver || State == GameState.LevelComplete) return;
        BigManMode = !BigManMode;
        slingshot.CancelAim();
        if (slingshot.Loaded) slingshot.Loaded.SetBigManMode(BigManMode);
        if (ActiveShot) ActiveShot.SetBigManMode(BigManMode);
        if (sound) sound.SetBigManMode(BigManMode);
        Changed?.Invoke();
    }
    public void ToggleView() { if (slingshot.IsDragging) return; followCam.SetOverview(!followCam.Overview); Changed?.Invoke(); }
    public void ToggleTrajectory() { slingshot.ShowTrajectory = !slingshot.ShowTrajectory; Changed?.Invoke(); }
    public string Medal(int level, int shots) {
        if (shots <= 0) return "-";
        return shots <= castles[level].goldShots ? "GOLD" : shots <= castles[level].silverShots ? "SILVER" : "BRONZE";
    }
}
