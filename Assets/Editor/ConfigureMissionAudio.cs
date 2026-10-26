using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureMissionAudio {
    const string AudioFolder = "Assets/Audio/";
    static AudioClip Clip(string file) {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + file);
        if (!clip) throw new InvalidOperationException("Missing audio: " + file);
        return clip;
    }
    [MenuItem("Mission Demolition/Apply Custom Audio")]
    public static void ApplyToOpenScene() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before applying audio.");
        var game = UnityEngine.Object.FindObjectOfType<MissionDemolition>();
        if (!game) throw new InvalidOperationException("Open the MissionDemolition scene first.");
        Apply(game);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("CUSTOM_AUDIO_CONFIGURED");
    }
    public static void Apply(MissionDemolition game) {
        var sounds = game.GetComponent<MissionAudio>();
        if (!sounds) sounds = game.gameObject.AddComponent<MissionAudio>();
        game.sound = sounds;
        sounds.musicVolume = .3f;
        sounds.mapSongs = new [] {
            Clip("sound-baord-spongebob 1.mp3"),
            Clip("japanesedrumroll.mp3"),
            Clip("megalovania.mp3"),
            Clip("gangstar-torture-dance-jojos-golden-wind-ost-1.mp3")
        };
        sounds.secretSongs = new [] {Clip("1_in_100_chance.mp3"), Clip("pleasedontlowermygrade.mp3")};
        sounds.vineBooms = new [] {
            Clip("Vine_boom_XD.mp3"), Clip("vine-boom-bass-boost-sound-effect.mp3"),
            Clip("vine-boom-bass-boosted-man.mp3"), Clip("vine-boom-slow.mp3"), Clip("vine-boom.mp3")
        };
        if (!sounds.musicSource) sounds.musicSource = Source(game.transform, "Background Music", .3f, true);
        if (!sounds.rockSource) sounds.rockSource = Source(game.transform, "Rock Impacts", 1f, false);
        sounds.rockVolume = .1f;
        sounds.rockSource.volume = 1f;
        sounds.musicSource.volume = .3f;
        sounds.musicSource.playOnAwake = false;
        sounds.musicSource.loop = true;
        sounds.musicSource.spatialBlend = 0;
        game.successSound = Clip("meet-the-sniper-jingle.mp3");
        game.gameOverSound = Clip("street-fighter-ii-you-win-perfect.mp3");
        sounds.bigManTrack = Clip("big-man-blastoise.mp3");
        game.slingshot.snap = Clip("gun-sniper-rifle-shot.mp3");
        EditorUtility.SetDirty(game);
        EditorUtility.SetDirty(sounds);
        EditorUtility.SetDirty(game.slingshot);
        PrefabUtility.RecordPrefabInstancePropertyModifications(game.slingshot);
        // Patch existing prefabs in place: castle geometry and other edits are preserved.
        string slingPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(game.slingshot));
        if (!string.IsNullOrEmpty(slingPath)) {
            var sling = PrefabUtility.LoadPrefabContents(slingPath);
            try { sling.GetComponent<Slingshot>().snap = game.slingshot.snap; PrefabUtility.SaveAsPrefabAsset(sling, slingPath); }
            finally { PrefabUtility.UnloadPrefabContents(sling); }
        }
        foreach (var level in game.castles) {
            string path = AssetDatabase.GetAssetPath(level.prefab);
            var castle = PrefabUtility.LoadPrefabContents(path);
            try {
                foreach (var renderer in castle.GetComponentsInChildren<MeshRenderer>(true)) {
                    if (!renderer.sharedMaterial || renderer.sharedMaterial.name != "Castle Stone") continue;
                    if (renderer.GetComponent<Collider>() && !renderer.GetComponent<StoneImpactSound>()) renderer.gameObject.AddComponent<StoneImpactSound>();
                }
                PrefabUtility.SaveAsPrefabAsset(castle, path);
            } finally { PrefabUtility.UnloadPrefabContents(castle); }
        }
        string projectilePath = AssetDatabase.GetAssetPath(game.slingshot.projectilePrefab);
        var projectile = PrefabUtility.LoadPrefabContents(projectilePath);
        try {
            ConfigureProjectileFlightAudio(projectile.GetComponent<Projectile>());
            PrefabUtility.SaveAsPrefabAsset(projectile, projectilePath);
        } finally { PrefabUtility.UnloadPrefabContents(projectile); }
    }
    public static void ConfigureProjectileFlightAudio(Projectile projectile) {
        if (!projectile.flightAudio) {
            projectile.flightAudio = projectile.GetComponent<AudioSource>();
            if (!projectile.flightAudio) projectile.flightAudio = projectile.gameObject.AddComponent<AudioSource>();
        }
        projectile.flightClip = Clip("bomb-explosion-with-whistle.mp3");
        projectile.flightAudio.playOnAwake = false;
        projectile.flightAudio.loop = false;
        projectile.flightAudio.spatialBlend = 0;
        projectile.flightAudio.volume = 1f;
    }
    static AudioSource Source(Transform parent, string name, float volume, bool loop) {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var source = obj.AddComponent<AudioSource>();
        source.playOnAwake = false; source.loop = loop; source.volume = volume; source.spatialBlend = 0;
        return source;
    }
}
