using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public static class BuildMissionProject {
    static Material stone, wood, ground, gold, ball, band, guide, green;
    static PhysicMaterial friction;
    static Font font;
    const string Base = "Assets/";

    [MenuItem("Mission Demolition/Rebuild Sample Scene and Prefabs")]
    public static void Build() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (string dir in new[]{"Scenes","Prefabs","Materials","Settings"}) Directory.CreateDirectory(Base + dir);
        stone = Mat("Castle Stone", new Color(.61f,.65f,.70f));
        wood = Mat("Slingshot Wood", new Color(.43f,.22f,.09f));
        ground = Mat("Grass", new Color(.32f,.57f,.22f));
        gold = Mat("Castle Trim", new Color(.85f,.62f,.24f));
        ball = Mat("Projectile Red", new Color(.82f,.12f,.08f));
        band = Mat("Rubber Band", new Color(.15f,.07f,.035f), "Sprites/Default");
        guide = Mat("Aim Guide", new Color(1,.91f,.46f,.85f), "Sprites/Default");
        green = Mat("Goal Green", new Color(.25f,1,.3f,.35f), "Sprites/Default");
        friction = new PhysicMaterial("Stone Friction") { dynamicFriction=.6f, staticFriction=.6f, bounciness=.03f, frictionCombine=PhysicMaterialCombine.Average };
        friction = Save(friction, "Materials/StoneFriction.physicMaterial");
        MissionConfig config = ScriptableObject.CreateInstance<MissionConfig>(); config = Save(config,"Settings/MissionConfig.asset");
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        ConfigureProject();
        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type=LightType.Directional; light.intensity=.8f; light.transform.rotation=Quaternion.Euler(35,-25,0);
        light.shadows=LightShadows.Soft;
        RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.45f,.48f,.52f);
        var floor=Block("Ground",null,new Vector3(0,-10,0),new Vector3(160,20,8),ground,false);
        floor.layer=8;
        CreateClouds();
        Projectile projectile=CreateProjectile();
        Slingshot sling=CreateSlingshot(config,projectile);
        sling.transform.position=new Vector3(-18,0,0);
        sling.rubberBand.SetPositions(new[]{sling.bandLeft.position,sling.launchPoint.position,sling.bandRight.position});
        var cameraObject=new GameObject("Main Camera"); cameraObject.tag="MainCamera";
        var camera=cameraObject.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=10;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.48f,.75f,.93f);
        camera.nearClipPlane=.1f; camera.farClipPlane=150;
        camera.transform.position=new Vector3(-14,7,-35);
        cameraObject.AddComponent<AudioListener>();
        FollowCam follow=cameraObject.AddComponent<FollowCam>();follow.config=config;follow.slingshot=sling;
        MissionDemolition game=new GameObject("MissionDemolition").AddComponent<MissionDemolition>();
        game.config=config;game.slingshot=sling;game.followCam=follow;
        game.audioSource=game.gameObject.AddComponent<AudioSource>();game.audioSource.playOnAwake=false;
        game.successSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/meet-the-sniper-jingle.mp3");
        game.castles=new CastleDefinition[4];
        string[] names={"Gatehouse","Twin Towers","High Keep","Citadel"};
        string[] hints={"Break the gate or arc over it. Gold: 1 shot.","Two walls protect the goal. Gold: 2 shots.","The goal sits above the ground. Aim higher! Gold: 2 shots.","Layered defenses guard the heart of the castle. Gold: 3 shots."};
        for(int i=0;i<4;i++) {
            var definition=ScriptableObject.CreateInstance<CastleDefinition>();definition.castleName=names[i];definition.hint=hints[i];
            definition.prefab=CreateCastle(i);definition.goldShots=new[]{1,2,2,3}[i];definition.silverShots=new[]{3,4,5,6}[i];
            definition = Save(definition,"Settings/Level"+(i+1)+".asset");game.castles[i]=definition;
        }
        CreateUI(game);
        ConfigureMissionAudio.Apply(game);
        ConfigureBigManMode.Apply(game);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/MissionDemolition.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/MissionDemolition.unity",true)};
        AssetDatabase.SaveAssets();
        Debug.Log("MISSION_BUILD_OK: scene, four castle prefabs, slingshot, projectile, configuration and HUD saved.");
    }
    static void ConfigureProject() {
        PlayerSettings.companyName="Mission Demolition";PlayerSettings.productName="Mission Demolition";
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
        PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Gamma;
        QualitySettings.vSyncCount=1;QualitySettings.antiAliasing=4;
        Physics.gravity=new Vector3(0,-9.81f,0);Physics.defaultSolverIterations=12;Time.fixedDeltaTime=.02f;
        var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue="Castle";tags.ApplyModifiedProperties();
    }
    static T Save<T>(T asset,string path) where T : UnityEngine.Object {
        string target=Base+path;
        var existing=AssetDatabase.LoadAssetAtPath<T>(target);
        if(existing) {EditorUtility.CopySerialized(asset,existing);UnityEngine.Object.DestroyImmediate(asset);return existing;}
        else AssetDatabase.CreateAsset(asset,target);
        return asset;
    }
    static Material Mat(string name,Color color,string shader="Standard") {
        string path="Assets/Materials/"+name+".mat";
        Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) {material=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(material,path);}
        material.color=color;
        if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.15f);
        return material;
    }
    static GameObject Block(string name,Transform parent,Vector3 position,Vector3 scale,Material mat,bool dynamic) {
        GameObject obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;
        obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
        obj.GetComponent<Renderer>().sharedMaterial=mat;obj.GetComponent<Collider>().sharedMaterial=friction;
        if(dynamic) {
            obj.layer=8;
            Rigidbody body=obj.AddComponent<Rigidbody>();body.mass=Mathf.Max(.6f,scale.x*scale.y*.35f);
            body.constraints=RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationY;
            body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.Continuous;
        }
        return obj;
    }
    static Projectile CreateProjectile() {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name="Projectile";obj.transform.localScale=Vector3.one*.9f;
        obj.GetComponent<Renderer>().sharedMaterial=ball;obj.GetComponent<Collider>().sharedMaterial=friction;
        var body=obj.AddComponent<Rigidbody>();body.mass=3;body.isKinematic=true;
        body.constraints=RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationY;
        body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
        var script=obj.AddComponent<Projectile>();
        var trail=obj.AddComponent<TrailRenderer>();trail.sharedMaterial=guide;trail.time=2;trail.startWidth=.12f;trail.endWidth=.015f;trail.emitting=false;
        script.trail=trail;
        ConfigureMissionAudio.ConfigureProjectileFlightAudio(script);
        var prefab=PrefabUtility.SaveAsPrefabAsset(obj,"Assets/Prefabs/Projectile.prefab");UnityEngine.Object.DestroyImmediate(obj);
        return prefab.GetComponent<Projectile>();
    }
    static Slingshot CreateSlingshot(MissionConfig config,Projectile projectile) {
        var obj=new GameObject("Slingshot");
        Block("Pole",obj.transform,new Vector3(0,1.3f,.4f),new Vector3(.65f,2.6f,.65f),wood,false);
        var left=Block("Fork left",obj.transform,new Vector3(-.55f,2.9f,.4f),new Vector3(.45f,1.8f,.55f),wood,false);left.transform.localRotation=Quaternion.Euler(0,0,35);
        var right=Block("Fork right",obj.transform,new Vector3(.55f,2.9f,.4f),new Vector3(.45f,1.8f,.55f),wood,false);right.transform.localRotation=Quaternion.Euler(0,0,-35);
        foreach(var collider in obj.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
        var sling=obj.AddComponent<Slingshot>();sling.config=config;sling.projectilePrefab=projectile;
        sling.launchPoint=Point("LaunchPoint",obj.transform,new Vector3(0,3.65f,0));
        sling.bandLeft=Point("BandLeft",obj.transform,new Vector3(-1.05f,3.6f,-.2f));
        sling.bandRight=Point("BandRight",obj.transform,new Vector3(1.05f,3.6f,-.2f));
        sling.rubberBand=Line("Rubber Band",obj.transform,band,.13f);sling.rubberBand.numCapVertices=3;sling.rubberBand.numCornerVertices=3;
        sling.rubberBand.positionCount=3;sling.rubberBand.SetPositions(new[]{sling.bandLeft.position,sling.launchPoint.position,sling.bandRight.position});
        sling.trajectory=Line("Trajectory Preview",obj.transform,guide,.055f);sling.trajectory.positionCount=0;
        sling.audioSource=obj.AddComponent<AudioSource>();sling.audioSource.playOnAwake=false;sling.snap=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/gun-sniper-rifle-shot.mp3");
        var prefab=PrefabUtility.SaveAsPrefabAsset(obj,"Assets/Prefabs/Slingshot.prefab");UnityEngine.Object.DestroyImmediate(obj);
        return ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<Slingshot>();
    }
    static Transform Point(string name,Transform parent,Vector3 p) {var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=p;return o.transform;}
    static LineRenderer Line(string name,Transform parent,Material material,float width) {
        var line=Point(name,parent,Vector3.zero).gameObject.AddComponent<LineRenderer>();line.sharedMaterial=material;line.startWidth=width;line.endWidth=width;line.useWorldSpace=true;return line;
    }
    static void Pier(Transform parent,float x,int courses,float y=0) {
        for(int i=0;i<courses;i++)Block("Stone course",parent,new Vector3(x,y+.75f+i*1.51f,0),new Vector3(1.15f,1.5f,1.4f),stone,true);
    }
    static void Beam(Transform parent,float x,float y,float width) {Block("Lintel",parent,new Vector3(x,y,0),new Vector3(width,.55f,1.5f),gold,true);}
    static GameObject CreateCastle(int index) {
        var root=new GameObject("Castle_"+(index+1));float goalY=1;
        if(index==0) {Pier(root.transform,-2.3f,2);Pier(root.transform,2.3f,2);Beam(root.transform,0,3.32f,6.1f);}
        if(index==1) {
            Pier(root.transform,-4.3f,3);Pier(root.transform,-1.6f,3);Beam(root.transform,-2.95f,4.82f,4.15f);
            Pier(root.transform,2.4f,2);Pier(root.transform,4.8f,2);Beam(root.transform,3.6f,3.32f,3.8f);
        }
        if(index==2) {
            // Broad static plinth supports an elevated goal; the upper walls remain destructible.
            Block("Keep foundation",root.transform,new Vector3(0,1,0),new Vector3(8,2,1.6f),stone,false).layer=8;
            Pier(root.transform,-2.6f,3,2);Pier(root.transform,2.6f,3,2);Beam(root.transform,0,6.82f,6.7f);goalY=3;
            Pier(root.transform,-5.1f,2);
        }
        if(index==3) {
            Pier(root.transform,-6.5f,3);Pier(root.transform,-3.8f,3);Beam(root.transform,-5.15f,4.82f,4.2f);
            Pier(root.transform,-1.8f,4);Pier(root.transform,2.4f,4);Beam(root.transform,.3f,6.33f,5.8f);
            Pier(root.transform,5.1f,3);Beam(root.transform,3.8f,4.82f,3.8f);
        }
        GameObject target=Block("Goal (projectile trigger)",root.transform,new Vector3(.3f,goalY,0),new Vector3(1.5f,1.7f,1.8f),green,false);
        target.GetComponent<Collider>().isTrigger=true;target.AddComponent<Goal>();
        // Green outline keeps the target easy to recognize through a collapsing castle.
        var outline=Line("Goal outline",target.transform,Mat("Goal Outline",new Color(.1f,.85f,.2f),"Sprites/Default"),.06f);
        outline.useWorldSpace=false;outline.loop=true;outline.positionCount=4;
        outline.SetPositions(new[]{new Vector3(-.5f,-.5f,-.52f),new Vector3(-.5f,.5f,-.52f),new Vector3(.5f,.5f,-.52f),new Vector3(.5f,-.5f,-.52f)});
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Castle_"+(index+1)+".prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
    }
    static void CreateClouds() {
        var root=new GameObject("Clouds");
        for(int i=0;i<9;i++) {
            string path="Assets/Textures & Sprites/cloud"+(i%5+1)+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer!=null) {importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.spritePixelsPerUnit=100;importer.SaveAndReimport();}
            Sprite sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!sprite)continue;
            var cloud=Point("Cloud "+(i+1),root.transform,new Vector3(-48+i*12,13+(i%3)*4,12)).gameObject;
            var render=cloud.AddComponent<SpriteRenderer>();render.sprite=sprite;render.color=new Color(1,1,1,.8f);render.sortingOrder=-10;
            float width=6+i%3*2;cloud.transform.localScale=Vector3.one*(width/sprite.bounds.size.x);
            cloud.AddComponent<CloudDrift>().speed=.15f+(i%3)*.07f;
        }
    }
    static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 pos,Vector2 size) {
        var o=new GameObject(name,typeof(RectTransform));var r=o.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.anchoredPosition=pos;r.sizeDelta=size;return r;
    }
    static Text Label(string name,Transform parent,string value,int size,Vector2 min,Vector2 max,Vector2 pos,Vector2 dimensions,TextAnchor align=TextAnchor.MiddleLeft) {
        var r=Rect(name,parent,min,max,pos,dimensions);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=Color.white;t.alignment=align;t.raycastTarget=false;return t;
    }
    static Image Panel(string name,Transform parent,Vector2 min,Vector2 max,Vector2 pos,Vector2 size,Color color) {
        var r=Rect(name,parent,min,max,pos,size);var im=r.gameObject.AddComponent<Image>();im.color=color;return im;
    }
    static Button Button(string name,Transform parent,string caption,Vector2 pos,Vector2 size) {
        var bg=Panel(name,parent,new Vector2(.5f,0),new Vector2(.5f,0),pos,size,new Color(.17f,.27f,.34f));
        var b=bg.gameObject.AddComponent<Button>();var c=b.colors;c.highlightedColor=new Color(.8f,.9f,1);c.pressedColor=new Color(.5f,.7f,.8f);b.colors=c;
        Label("Label",bg.transform,caption,18,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,TextAnchor.MiddleCenter);return b;
    }
    static void CreateUI(MissionDemolition game) {
        var obj=new GameObject("HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        obj.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=obj.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        var hud=obj.AddComponent<MissionHUD>();hud.game=game;
        var top=Panel("Header",obj.transform,new Vector2(0,1),Vector2.one,new Vector2(0,-48),new Vector2(0,96),new Color(.08f,.15f,.19f,.95f));
        Label("Title",top.transform,"MISSION DEMOLITION",30,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(230,16),new Vector2(410,42));
        hud.levelText=Label("Level",top.transform,"",18,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(305,-22),new Vector2(560,32));
        hud.shotsText=Label("Shots",top.transform,"",23,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-210,14),new Vector2(370,38),TextAnchor.MiddleRight);
        hud.hintText=Label("Hint",top.transform,"",16,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-305,-22),new Vector2(560,32),TextAnchor.MiddleRight);
        var bottom=Panel("Controls",obj.transform,Vector2.zero,new Vector2(1,0),new Vector2(0,56),new Vector2(0,112),new Color(.08f,.15f,.19f,.95f));
        hud.statusText=Label("Status",bottom.transform,"",20,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,27),new Vector2(1100,32),TextAnchor.MiddleCenter);
        hud.viewButton=Button("Switch View",bottom.transform,"",new Vector2(-390,31),new Vector2(230,42));hud.viewText=hud.viewButton.GetComponentInChildren<Text>();
        hud.trajectoryButton=Button("Trajectory Toggle",bottom.transform,"",new Vector2(-130,31),new Vector2(230,42));hud.trajectoryText=hud.trajectoryButton.GetComponentInChildren<Text>();
        hud.nextShotButton=Button("Next Shot",bottom.transform,"",new Vector2(130,31),new Vector2(230,42));
        hud.retryButton=Button("Retry Level",bottom.transform,"Retry level  [R]",new Vector2(390,31),new Vector2(230,42));
        var power=Panel("Power background",obj.transform,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(42,0),new Vector2(16,160),new Color(.08f,.15f,.19f,.6f));
        hud.powerFill=Panel("Power fill",power.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(1,.74f,.2f));
        // Image.Filled needs a sprite even when it is just a white pixel.
        var texture=new Texture2D(1,1);texture.SetPixel(0,0,Color.white);texture.Apply();
        File.WriteAllBytes("Assets/Materials/White.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset("Assets/Materials/White.png");
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Materials/White.png");importer.textureType=TextureImporterType.Sprite;importer.SaveAndReimport();
        hud.powerFill.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Materials/White.png");hud.powerFill.type=Image.Type.Filled;hud.powerFill.fillMethod=Image.FillMethod.Vertical;hud.powerFill.fillAmount=0;
        var overlay=Panel("Game Over",obj.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.03f,.08f,.12f,.94f));hud.gameOverPanel=overlay.gameObject;
        Label("Game Over title",overlay.transform,"GAME OVER",48,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,210),new Vector2(850,70),TextAnchor.MiddleCenter);
        hud.resultsText=Label("Results",overlay.transform,"",24,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,5),new Vector2(900,300),TextAnchor.MiddleCenter);
        hud.playAgainButton=Button("Play Again",overlay.transform,"PLAY AGAIN",new Vector2(0,120),new Vector2(260,58));overlay.gameObject.SetActive(false);
    }
}
