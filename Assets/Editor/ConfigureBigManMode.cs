using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class ConfigureBigManMode {
    [MenuItem("Mission Demolition/Apply Big Man Mode UI")]
    public static void ApplyOpenScene() {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var game=Object.FindObjectOfType<MissionDemolition>();
        Apply(game);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        AssetDatabase.SaveAssets();
    }
    public static void Apply(MissionDemolition game) {
        game.gameOverSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/street-fighter-ii-you-win-perfect.mp3");
        game.sound.bigManTrack=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/big-man-blastoise.mp3");
        var hud=Object.FindObjectOfType<MissionHUD>();
        Transform parent=hud.transform.Find("Controls");
        var existing=parent.Find("Big Man Mode");
        GameObject obj=existing ? existing.gameObject : new GameObject("Big Man Mode",typeof(RectTransform),typeof(Image),typeof(Button));
        var rect=obj.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(63,36);rect.sizeDelta=new Vector2(110,64);
        obj.GetComponent<Image>().color=new Color(.17f,.27f,.34f,.35f);
        hud.bigManButton=obj.GetComponent<Button>();
        if(!existing) {
            var icon=new GameObject("Large and Small Ball",typeof(RectTransform),typeof(BigBallIcon));
            var ir=icon.GetComponent<RectTransform>();ir.SetParent(rect,false);ir.anchorMin=ir.anchorMax=new Vector2(0,.5f);ir.anchoredPosition=new Vector2(33,0);ir.sizeDelta=new Vector2(60,60);
            hud.bigManIcon=icon.GetComponent<BigBallIcon>();hud.bigManIcon.raycastTarget=false;
            var key=new GameObject("B key",typeof(RectTransform),typeof(Text));
            var kr=key.GetComponent<RectTransform>();kr.SetParent(rect,false);kr.anchorMin=kr.anchorMax=new Vector2(1,.5f);kr.anchoredPosition=new Vector2(-21,0);kr.sizeDelta=new Vector2(32,44);
            hud.bigManKey=key.GetComponent<Text>();hud.bigManKey.font=Resources.GetBuiltinResource<Font>("Arial.ttf");hud.bigManKey.fontSize=28;hud.bigManKey.fontStyle=FontStyle.Bold;hud.bigManKey.alignment=TextAnchor.MiddleCenter;hud.bigManKey.text="B";hud.bigManKey.raycastTarget=false;
        }else {hud.bigManIcon=obj.GetComponentInChildren<BigBallIcon>();hud.bigManKey=obj.GetComponentInChildren<Text>();}
        if(!hud.bigManIcon.GetComponent<CanvasRenderer>())hud.bigManIcon.gameObject.AddComponent<CanvasRenderer>();
        hud.bigManIcon.SetAllDirty();
        EditorUtility.SetDirty(game);EditorUtility.SetDirty(game.sound);EditorUtility.SetDirty(hud);
    }
}
