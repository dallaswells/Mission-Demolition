using UnityEngine;

// Music has its own source so goal and collision sounds never replace its clip.
public class MissionAudio : MonoBehaviour {
    public AudioSource musicSource;
    public AudioSource rockSource;
    public AudioClip bigManTrack;
    bool bigManMode;
    AudioClip stageTrack;
    float stageResumeTime;
    public AudioClip[] mapSongs = new AudioClip[4];
    public AudioClip[] secretSongs = new AudioClip[2];
    public AudioClip[] vineBooms = new AudioClip[5];
    [Range(0, 1)] public float musicVolume = .3f;
    [Range(0, 1)] public float rockVolume = .1f;
    public AudioClip CurrentMusic { get { return musicSource ? musicSource.clip : null; } }

    public AudioClip SelectMusic(int levelIndex) {
        if (mapSongs == null || levelIndex < 0 || levelIndex >= mapSongs.Length) return null;
        // Integer Range excludes its upper bound: exactly one winning result out of 100.
        // One roll per map start/restart; the two secrets share that 1% chance.
        bool secret = Random.Range(0, 100) == 0;
        if (secret && secretSongs != null && secretSongs.Length > 0) {
            AudioClip choice = secretSongs[Random.Range(0, secretSongs.Length)];
            if (choice) return choice;
        }
        return mapSongs[levelIndex];
    }
    public void RestartMusic(int levelIndex) {
        if (rockSource) rockSource.Stop();
        stageTrack = SelectMusic(levelIndex);
        stageResumeTime = 0;
        PlayMusic(bigManMode && bigManTrack ? bigManTrack : stageTrack, 0);
    }
    public void SetBigManMode(bool enabled) {
        if (bigManMode == enabled) return;
        if (enabled && musicSource && musicSource.clip == stageTrack) stageResumeTime = musicSource.time;
        bigManMode = enabled;
        // Mode toggles preserve the chosen stage/secret song; only map starts reroll it.
        PlayMusic(enabled && bigManTrack ? bigManTrack : stageTrack, enabled ? 0 : stageResumeTime);
    }
    void PlayMusic(AudioClip clip, float fromTime) {
        if (!musicSource) return;
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        if (clip) {
            musicSource.time = Mathf.Clamp(fromTime, 0, Mathf.Max(0, clip.length - .01f));
            musicSource.Play();
        }
    }
    public void StopForGameOver() {
        if (musicSource) musicSource.Stop();
        if (rockSource) rockSource.Stop();
    }
    public AudioClip PlayRockImpact() {
        if (!rockSource || vineBooms == null || vineBooms.Length == 0) return null;
        AudioClip clip = vineBooms[Random.Range(0, vineBooms.Length)];
        // One-shots overlap naturally; new hits do not stop previous impacts.
        if (clip) rockSource.PlayOneShot(clip, rockVolume);
        return clip;
    }
}
