using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Talingchan.Sandbox;

public static class SandboxMapBuilder
{
    const string Root = "Assets/TalingchanSandbox";
    const string ScenePath = Root + "/Scenes/SmallTownSandbox.unity";
    static Material grass, asphalt, concrete, white, yellow, dark, wood, glass, roof, teal, cream, brick;

    [MenuItem("Talingchan/Create Small Town Sandbox")]
    public static void Build()
    {
        if (File.Exists(ScenePath)) throw new System.InvalidOperationException("Sandbox scene already exists; open it instead of overwriting it.");
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before building.");
        // Create additively first, so an unsaved user scene is never discarded.
        Scene previous = SceneManager.GetActiveScene();
        bool previousDirty = previous.isDirty;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        Directory.CreateDirectory(Root + "/Scenes");
        Directory.CreateDirectory(Root + "/Materials");
        AssetDatabase.Refresh();
        grass = Mat("Grass", new Color(.32f,.43f,.28f));
        asphalt = Mat("Asphalt", new Color(.13f,.16f,.18f));
        concrete = Mat("Concrete", new Color(.66f,.68f,.63f));
        white = Mat("Ivory", new Color(.92f,.90f,.79f));
        yellow = Mat("RoadPaint", new Color(1,.74f,.22f));
        dark = Mat("Charcoal", new Color(.12f,.19f,.22f));
        wood = Mat("Wood", new Color(.45f,.27f,.15f));
        glass = Mat("WindowBlue", new Color(.22f,.55f,.64f));
        roof = Mat("Terracotta", new Color(.53f,.22f,.15f));
        teal = Mat("HouseTeal", new Color(.32f,.60f,.56f));
        cream = Mat("HouseSand", new Color(.86f,.70f,.44f));
        brick = Mat("Office", new Color(.64f,.40f,.30f));

        var map = new GameObject("SMALL TOWN - editable geometry").transform;
        Box("Ground", map, new Vector3(0,-.25f,0), new Vector3(50,.5f,52), grass);
        var roads = Group("Road and sidewalks", map);
        Box("Main street", roads, new Vector3(0,.015f,0), new Vector3(7,.03f,48), asphalt);
        Box("Cross street", roads, new Vector3(0,.02f,-18), new Vector3(46,.04f,7), asphalt);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Sidewalk", roads, new Vector3(side*4.6f,.09f,4), new Vector3(2.1f,.18f,37), concrete);
            Box("Curb", roads, new Vector3(side*3.58f,.105f,4), new Vector3(.18f,.21f,37), white);
            Box("Cross street sidewalk", roads, new Vector3(side*14,.09f,-22.6f), new Vector3(20,.18f,2.1f), concrete);
        }
        for (float z=-12;z<23;z+=4) Box("Center dash",roads,new Vector3(0,.04f,z),new Vector3(.13f,.02f,1.9f),yellow);
        for (float x=-21;x<22;x+=4) if(Mathf.Abs(x)>4) Box("Cross street dash",roads,new Vector3(x,.05f,-18),new Vector3(1.9f,.02f,.13f),yellow);
        for(int i=0;i<7;i++) Box("Crosswalk",roads,new Vector3(-2.7f+i*.9f,.055f,-12.5f),new Vector3(.5f,.025f,2.2f),white);

        Building("House 01", map, new Vector3(-12,0,5), -90, 9, 9, false, teal);
        Building("House 02", map, new Vector3(-12,0,-8), -90, 9, 9, false, cream);
        Building("Building 01 - two floors", map, new Vector3(13,0,4), 90, 12, 14, true, brick);
        Box("House 01 entry path", map, new Vector3(-6.5f,.08f,5), new Vector3(3,.16f,2.5f), concrete);
        Box("House 02 entry path", map, new Vector3(-6.5f,.08f,-8), new Vector3(3,.16f,2.5f), concrete);
        Box("Office entry plaza", map, new Vector3(7,.08f,4), new Vector3(4,.16f,5), concrete);

        var props = Group("Street furniture", map);
        foreach(float z in new[]{-9f,3f,16f}) foreach(int side in new[]{-1,1})
        {
            float x = side*5.2f;
            Cylinder("Lamp post",props,new Vector3(x,2.1f,z),new Vector3(.12f,2.1f,.12f),dark);
            Box("Lamp head",props,new Vector3(x,4.25f,z),new Vector3(.65f,.22f,.65f),white);
        }
        foreach(var p in new[]{new Vector3(-21,0,-9),new Vector3(-21,0,7),new Vector3(-18,0,19),new Vector3(17,0,20),new Vector3(22,0,-8),new Vector3(-8,0,20)})
        {
            Cylinder("Tree trunk",props,p+Vector3.up*1.1f,new Vector3(.32f,1.1f,.32f),wood);
            var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere); crown.name="Tree canopy";crown.transform.SetParent(props);crown.transform.position=p+Vector3.up*3;crown.transform.localScale=new Vector3(3.2f,3.6f,3.2f);crown.GetComponent<Renderer>().sharedMaterial=grass;
        }
        Box("Bench seat",props,new Vector3(5,.6f,14),new Vector3(1,.18f,2.5f),wood);
        Box("Bench back",props,new Vector3(5.4f,1,14),new Vector3(.15f,.9f,2.5f),wood);
        foreach(float z in new[]{13.2f,14.8f}) Box("Bench leg",props,new Vector3(5,.25f,z),new Vector3(.65f,.5f,.12f),dark);
        Box("North boundary",map,new Vector3(0,.65f,25.5f),new Vector3(50,1.3f,.3f),dark);
        Box("South boundary",map,new Vector3(0,.65f,-25.5f),new Vector3(50,1.3f,.3f),dark);
        Box("West boundary",map,new Vector3(-24.5f,.65f,0),new Vector3(.3f,1.3f,51),dark);
        Box("East boundary",map,new Vector3(24.5f,.65f,0),new Vector3(.3f,1.3f,51),dark);

        var sun = new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional;sun.intensity=1.3f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(45,-35,0);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.65f,.76f,.85f);RenderSettings.ambientEquatorColor=new Color(.54f,.58f,.59f);RenderSettings.ambientGroundColor=new Color(.32f,.34f,.30f);
        RenderSettings.fog=false;

        var player=new GameObject("Player - WASD Mouse Space");player.transform.position=new Vector3(0,.15f,-10);
        var body=player.AddComponent<CharacterController>();body.height=1.8f;body.radius=.3f;body.center=new Vector3(0,.9f,0);body.stepOffset=.32f;body.slopeLimit=50;body.skinWidth=.035f;
        var cameraObject=new GameObject("Player Camera");cameraObject.tag="MainCamera";cameraObject.transform.SetParent(player.transform,false);cameraObject.transform.localPosition=new Vector3(0,1.65f,0);
        var camera=cameraObject.AddComponent<Camera>();camera.fieldOfView=75;camera.nearClipPlane=.06f;camera.farClipPlane=160;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.66f,.78f,.84f);
        cameraObject.AddComponent<AudioListener>();
        player.AddComponent<SandboxPlayer>().view=cameraObject.transform;
        EditorSceneManager.SaveScene(scene,ScenePath);
        AddSandboxAvatar.Main();
        AddSandboxPhone.Main();
        AssetDatabase.SaveAssets();
        if(!previousDirty) EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        else { EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous); }
        var sceneView=SceneView.lastActiveSceneView;
        if(sceneView!=null) sceneView.LookAt(new Vector3(0,0,0),Quaternion.Euler(51,-34,0),45);
        File.WriteAllText(Root+"/README.txt","SMALL TOWN SANDBOX\nOpen Scenes/SmallTownSandbox.unity and press Play.\nWASD / arrows move, mouse look, Shift run, Space jump, V toggles first/third person, R respawn, Esc release cursor.\nIncludes an animated test mannequin.\nTwo enterable houses, one two-storey office with stairs, road and sidewalks.\nRuntime: Scripts/SandboxPlayer.cs and Scripts/SandboxAvatar.cs. Scene geometry is editable in the Hierarchy.\nOriginal scene preserved. No third-party assets used.\n");
        AssetDatabase.Refresh();
        Debug.Log("SANDBOX_READY: "+ScenePath);
    }

    static Transform Group(string name,Transform parent) {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static Material Mat(string name,Color color)
    {
        string path=Root+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null) {m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",.15f);return m;
    }
    static GameObject Box(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;return g;
    }
    static void Cylinder(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;
    }
    static void Label(string text,Transform parent,Vector3 position)
    {
        var go=new GameObject("Sign - "+text);go.transform.SetParent(parent,false);go.transform.localPosition=position;
        var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=56;label.characterSize=.085f;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.97f,.92f,.72f);
    }
    static void Building(string name,Transform parent,Vector3 position,float angle,float w,float d,bool office,Material walls)
    {
        var b=Group(name,parent);b.localPosition=position;b.localRotation=Quaternion.Euler(0,angle,0);
        float level=3.3f;int floors=office?2:1;float door=2.1f;
        Box("Floor",b,new Vector3(0,.04f,0),new Vector3(w,.08f,d),concrete);
        for(int floor=0;floor<floors;floor++)
        {
            float y=floor*level;
            // Front wall has a real unobstructed doorway, 2.1m wide and 2.5m high.
            if(floor==0)
            {
                foreach(int side in new[]{-1,1}) Box("Door side wall",b,new Vector3(side*(w+door)/4,y+1.65f,-d/2),new Vector3((w-door)/2,level,.2f),walls);
                Box("Door header",b,new Vector3(0,y+2.95f,-d/2),new Vector3(door,.7f,.2f),walls);
                Box("Entry canopy",b,new Vector3(0,2.65f,-d/2-.7f),new Vector3(3.4f,.15f,1.5f),dark);
                foreach(int side in new[]{-1,1}) Box("Door frame",b,new Vector3(side*1.08f,1.25f,-d/2-.12f),new Vector3(.12f,2.5f,.18f),white);
            }
            else Box("Upper front wall",b,new Vector3(0,y+1.65f,-d/2),new Vector3(w,level,.2f),walls);
            Box("Rear wall",b,new Vector3(0,y+1.65f,d/2),new Vector3(w,level,.2f),walls);
            foreach(int side in new[]{-1,1})
            {
                // Side windows built as openings around glass instead of paint on solid walls.
                float x=side*w/2;
                Box("Window sill wall",b,new Vector3(x,y+.55f,0),new Vector3(.2f,1.1f,d),walls);
                Box("Window top wall",b,new Vector3(x,y+2.8f,0),new Vector3(.2f,1,d),walls);
                foreach(float z in new[]{-d/2+1,d/2-1}) Box("Window side wall",b,new Vector3(x,y+1.7f,z),new Vector3(.2f,1.2f,2),walls);
                Box("Blue window",b,new Vector3(x,y+1.7f,0),new Vector3(.06f,1.2f,d-4),glass);
                for(float z=-d/2+2;z<=d/2-2;z+=2)Box("Window mullion",b,new Vector3(x,y+1.7f,z),new Vector3(.26f,1.3f,.08f),white);
            }
            foreach(float x in new[]{-w*.3f,w*.3f}) Box("Front window",b,new Vector3(x,y+1.75f,-d/2-.13f),new Vector3(1.5f,1.2f,.06f),glass);
        }
        Label(office?"03 / WORKSHOP":"0"+(name.Contains("01")?"1 / HOUSE":"2 / HOUSE"),b,new Vector3(0,2.96f,-d/2-.14f));
        if(office)
        {
            // Open stairwell along the right wall; upper slab leaves this strip open.
            Box("Upper floor",b,new Vector3(-1.05f,level,0),new Vector3(w-2.1f,.18f,d),concrete);
            Box("Stair landing",b,new Vector3(w/2-1.05f,level,d/2-1.6f),new Vector3(2.1f,.18f,3.2f),concrete);
            int steps=18;float rise=level/steps,run=.4f;
            for(int i=0;i<steps;i++)Box("Stair "+(i+1),b,new Vector3(w/2-1.1f,(i+1)*rise/2,-3.1f+i*run),new Vector3(1.9f,(i+1)*rise,run+.015f),wood);
            Box("Roof",b,new Vector3(0,level*2+.08f,0),new Vector3(w+.6f,.2f,d+.6f),dark);
            // Rail along the exposed slab edge, with a gap at the upper landing.
            Box("Stair rail",b,new Vector3(w/2-2.15f,level+.9f,-1.2f),new Vector3(.1f,.12f,9.4f),dark);
            for(float z=-5.8f;z<3.5f;z+=1.2f)Box("Rail post",b,new Vector3(w/2-2.15f,level+.45f,z),new Vector3(.08f,.9f,.08f),dark);
            Box("Work table upper floor",b,new Vector3(-2,level+.8f,2),new Vector3(2.8f,.15f,1.4f),wood);
            foreach(float x in new[]{-3f,-1f})foreach(float z in new[]{1.5f,2.5f})Box("Table leg",b,new Vector3(x,level+.4f,z),new Vector3(.12f,.8f,.12f),dark);
        }
        else
        {
            // Two sloping panels form a low gable roof with full standing room below.
            foreach(int side in new[]{-1,1})
            {
                var panel=Box("Sloping roof",b,new Vector3(side*w/4,level+.5f,0),new Vector3(w/2+.6f,.18f,d+.7f),roof);
                panel.transform.localRotation=Quaternion.Euler(0,0,-side*12);
            }
            Box("Ceiling",b,new Vector3(0,level+.02f,0),new Vector3(w,.12f,d),white);
        }
        Box("Interior table",b,new Vector3(-1.8f,.78f,1.5f),new Vector3(2,.14f,1.2f),wood);
        foreach(float x in new[]{-2.5f,-1.1f})foreach(float z in new[]{1.1f,1.9f})Box("Table leg",b,new Vector3(x,.38f,z),new Vector3(.1f,.76f,.1f),dark);
        Box("Storage crate",b,new Vector3(-w/2+1,.5f,d/2-1),new Vector3(1,1,1),wood);
        for(int floor=0;floor<floors;floor++)
        {
            var lamp=new GameObject("Interior light");lamp.transform.SetParent(b,false);lamp.transform.localPosition=new Vector3(0,2.8f+floor*level,0);var l=lamp.AddComponent<Light>();l.type=LightType.Point;l.range=12;l.intensity=2.5f;l.color=new Color(1,.89f,.7f);
        }
    }
}
