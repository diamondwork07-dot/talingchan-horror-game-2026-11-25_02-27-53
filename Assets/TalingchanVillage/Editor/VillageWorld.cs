using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Talingchan.Sandbox;
using Talingchan.Village;

public static partial class VillageBuilder
{
    public const string ScenePath=Root+"/Scenes/BanRimKhlong.unity";
    static SandboxPlayer villagePlayer;
    [Serializable] class BuildStatus{public string phase,error;public bool complete;public int houses,lights;}
    static void Status(string phase,bool complete=false,string error=null)
    {
        File.WriteAllText(Root+"/BUILD_STATUS.json",JsonUtility.ToJson(new BuildStatus{phase=phase,complete=complete,error=error,houses=houseCount,lights=localLights.Count},true));
        Debug.Log("VILLAGE_BUILD: "+phase);
    }
    [MenuItem("Talingchan/Build Realistic Village")]
    public static void BuildFromMenu(){Build();}
    public static object Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before building the village.");
        if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return new{status="Existing village opened"};}
        var source=UnityEngine.Object.FindAnyObjectByType<SandboxPlayer>();
        if(source==null)throw new Exception("Open SmallTownSandbox first so the existing player systems can be retained.");
        if(SceneManager.GetActiveScene().isDirty)EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Directory.CreateDirectory(Root);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            houseCount=0;meshIndex=0;boxMeshes.Clear();localLights.Clear();streetLights.Clear();interiorLights.Clear();UnityEngine.Random.InitState(2711);
            Status("Preparing CC0 models and PBR materials");VillageAssetLibrary.Prepare();PrepareMaterials();
            var world=Group("BAN RIM KHLONG - realistic village",null,Vector3.zero);
            Status("Sculpting land and laying out roads");Terrain(world);Roads(world);
            Status("Building ten enterable homes");
            var homes=Group("Residential plots - 10 houses",world,Vector3.zero);
            int variant=0;
            foreach(float z in new[]{-35f,-15f,20f,49f})House("บ้าน "+(variant+1),homes,new Vector3(-21,0,z),-90,variant==2,variant++);
            foreach(float z in new[]{-35f,40f,68f})House("บ้าน "+(variant+1),homes,new Vector3(21,0,z),90,variant==5,variant++);
            House("บ้านสวน 8",homes,new Vector3(-51,0,-32),-90,true,variant++);
            House("บ้านสวน 9",homes,new Vector3(-51,0,31),-90,false,variant++);
            House("บ้านสวน 10",homes,new Vector3(51,0,-43),90,false,variant++);
            Status("Adding shops, market, hall and utilities");Shop(world);MarketAndHall(world);Utilities(world);Landscape(world);
            Status("Constructing the separate Thai temple compound");Temple(world);
            Status("Retaining the first-person player, phone and left flashlight");
            var playerObject=UnityEngine.Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(playerObject,scene);playerObject.name="Player - village FPS";
            villagePlayer=playerObject.GetComponent<SandboxPlayer>();villagePlayer.hudTitle="BAN RIM KHLONG";villagePlayer.thirdPerson=false;villagePlayer.allowViewSwitch=false;villagePlayer.enabled=true;
            playerObject.transform.SetPositionAndRotation(new Vector3(0,.15f,-52),Quaternion.identity);
            villagePlayer.view.localPosition=new Vector3(0,1.55f,0);villagePlayer.view.localRotation=Quaternion.identity;
            var camera=villagePlayer.view.GetComponent<Camera>();camera.farClipPlane=300;
            playerObject.GetComponent<SandboxPhone>().autoDemoCall=false;
            var lighting=world.gameObject.AddComponent<VillageLightingController>();lighting.player=playerObject.transform;lighting.localLights=localLights.ToArray();lighting.Refresh();
            var moon=Group("Village moonlight",world,Vector3.zero).gameObject.AddComponent<Light>();moon.type=LightType.Directional;moon.transform.rotation=Quaternion.Euler(35,-25,0);moon.shadows=LightShadows.Soft;
            var atmosphere=Group("Village atmosphere",world,Vector3.zero).gameObject.AddComponent<Volume>();atmosphere.isGlobal=true;atmosphere.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/TalingchanSandbox/Lighting/NightAtmosphere.asset");
            VillageVisualTuning.Apply();ApplyNight();
            var pipeline=GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;if(pipeline!=null)pipeline.shadowDistance=75;
            Status("Saving the playable village scene");AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var sv=SceneView.lastActiveSceneView;if(sv!=null){sv.sceneLighting=true;sv.LookAt(new Vector3(0,0,65),Quaternion.Euler(48,-32,0),190);}
            File.WriteAllText(Root+"/README.txt","BAN RIM KHLONG - playable village prototype\nScene: Scenes/BanRimKhlong.unity\n10 furnished enterable homes, grocery, market, community hall, road network, drainage, power poles and wires, gardens, water tank, pond and fields.\nTemple compound is OUTSIDE the village to the north; follow the road through the tree-lined approach. Includes an enterable ordination hall, Buddha altar, chedi and bell pavilion.\nWASD move, mouse look, Shift run, Space jump, F draw/stow flashlight, G flashlight power, R respawn, Esc cursor.\nT incoming call, E answer, Q decline/end. Automatic demo calls are disabled in this exploration scene.\nModels and PBR sources: see Sources/CREDITS.txt. Thai font: Sources/Fonts/OFL.txt.\nLighting uses Forward+ and a runtime shadow budget. Vegetation includes lower-detail distance models.\nThis is a realistic textured prototype, not a survey or reconstruction of a real Thai village or temple.\n");
            Status("Village ready",true);AssetDatabase.Refresh();
            return new{status="ready",scene=ScenePath,houses=houseCount,lights=localLights.Count};
        }
        catch(Exception e){Status("Build failed",false,e.ToString());throw;}
    }
    static void PrepareMaterials()
    {
        wood=VillageAssetLibrary.Surface("brown_planks_03");plaster=VillageAssetLibrary.Surface("damaged_plaster");roof=VillageAssetLibrary.Surface("clay_roof_tiles_02");iron=VillageAssetLibrary.Surface("corrugated_iron");concrete=VillageAssetLibrary.Surface("concrete_pavement");brick=VillageAssetLibrary.Surface("red_brick_03");soil=VillageAssetLibrary.Surface("brown_mud_dry");asphalt=VillageAssetLibrary.Surface("asphalt_01");
        gold=VillageAssetLibrary.Solid("Temple gold",new Color(.8f,.52f,.12f),.78f,.38f);
        dark=VillageAssetLibrary.Solid("Dark steel",new Color(.06f,.075f,.08f),.35f,.3f);
        white=VillageAssetLibrary.Tint("Temple lime plaster",plaster,new Color(.96f,.94f,.83f));
        cloth=VillageAssetLibrary.Solid("Woven fabric",new Color(.33f,.41f,.32f),0,.08f);
        glass=VillageAssetLibrary.Solid("Window glass",new Color(.32f,.46f,.48f,.23f),.1f,.9f);
        glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Cull",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
        glow=VillageAssetLibrary.Solid("Warm lamp glass",new Color(1,.8f,.5f),0,.35f);glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.68f,.3f)*3);
        water=VillageAssetLibrary.Solid("Pond water",new Color(.055f,.17f,.14f),.3f,.95f);
        thaiFont=AssetDatabase.LoadAssetAtPath<Font>(VillageAssetLibrary.Source+"/Fonts/NotoSansThai.ttf");if(thaiFont==null)throw new Exception("Thai sign font was not imported.");
    }
    static void Terrain(Transform parent)
    {
        var data=new TerrainData();data.heightmapResolution=257;data.size=new Vector3(220,15,330);
        var heights=new float[257,257];
        for(int z=0;z<257;z++)for(int x=0;x<257;x++)
        {
            float wx=-110+x/256f*220,wz=-95+z/256f*330;
            float edge=Mathf.Clamp01(Mathf.Max((Mathf.Abs(wx)-76)/28,(wz-207)/25));
            float elevation=edge*(1+Mathf.PerlinNoise(wx*.04f+20,wz*.04f+20)*3);
            float pond=Mathf.Sqrt(Mathf.Pow((wx+48)/19,2)+Mathf.Pow((wz-106)/22,2));
            if(pond<1)elevation-=1.6f*Mathf.Pow(1-pond*pond,2);
            heights[z,x]=(3+elevation)/15;
        }
        data.SetHeights(0,0,heights);
        var layers=new TerrainLayer[2];string[] ids={"grass_ground","brown_mud_dry"};
        for(int i=0;i<2;i++){layers[i]=new TerrainLayer();var m=VillageAssetLibrary.Surface(ids[i]);layers[i].diffuseTexture=m.GetTexture("_BaseMap") as Texture2D;layers[i].normalMapTexture=m.GetTexture("_BumpMap") as Texture2D;layers[i].maskMapTexture=m.GetTexture("_MetallicGlossMap") as Texture2D;layers[i].tileSize=Vector2.one*(i==0?5:3);AssetDatabase.CreateAsset(layers[i],Root+"/Terrain/Layer_"+i+".terrainlayer");}
        data.terrainLayers=layers;data.alphamapResolution=128;float[,,] alpha=new float[128,128,2];
        for(int z=0;z<128;z++)for(int x=0;x<128;x++)
        {
            float wx=-110+x/127f*220,wz=-95+z/127f*330;
            float dirt=Mathf.Max(Mathf.Clamp01(1-(Mathf.Abs(wx)-3)/4),Mathf.Abs(wx)<66?Mathf.Clamp01(1-(Mathf.Abs(wz)-2)/4):0);
            float pond=Mathf.Sqrt(Mathf.Pow((wx+48)/19,2)+Mathf.Pow((wz-106)/22,2));
            dirt=Mathf.Max(dirt,Mathf.Clamp01((1.15f-pond)*4));alpha[z,x,1]=dirt;alpha[z,x,0]=1-dirt;
        }
        data.SetAlphamaps(0,0,alpha);data.baseMapResolution=1024;AssetDatabase.CreateAsset(data,Root+"/Terrain/VillageTerrain.asset");
        var terrainObject=UnityEngine.Terrain.CreateTerrainGameObject(data);terrainObject.name="Grass, earth banks and village pond terrain";terrainObject.transform.SetParent(parent,false);terrainObject.transform.localPosition=new Vector3(-110,-3,-95);
        var terrain=terrainObject.GetComponent<UnityEngine.Terrain>();terrain.drawInstanced=true;terrain.basemapDistance=220;
        var mat=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));mat.name="Village terrain";AssetDatabase.CreateAsset(mat,Root+"/Materials/VillageTerrain.mat");terrain.materialTemplate=mat;
        var pondSurface=Group("Village pond and bank",parent,new Vector3(-48,-.18f,106));
        var mesh=new Mesh();var vertices=new Vector3[66];var uv=new Vector2[66];var tri=new int[64*3];vertices[0]=Vector3.zero;uv[0]=new Vector2(.5f,.5f);
        for(int i=0;i<=64;i++){float a=i/64f*Mathf.PI*2;vertices[i+1]=new Vector3(Mathf.Cos(a)*16.8f,0,Mathf.Sin(a)*19.7f);uv[i+1]=new Vector2(Mathf.Cos(a)*.5f+.5f,Mathf.Sin(a)*.5f+.5f);if(i<64){tri[i*3]=0;tri[i*3+1]=i+2;tri[i*3+2]=i+1;}}
        mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=tri;mesh.RecalculateNormals();MeshObject("Pond surface",pondSurface,Persist(mesh),water,false);
    }
    static void Roads(Transform parent)
    {
        var roads=Group("Village roads, lanes and drainage",parent,Vector3.zero);
        Box("Main village street",roads,new Vector3(0,.035f,10),new Vector3(6.8f,.07f,165),asphalt);
        Box("Temple approach road",roads,new Vector3(0,.03f,108),new Vector3(5.4f,.06f,39),asphalt);
        Box("Village cross street",roads,new Vector3(0,.04f,0),new Vector3(132,.08f,6.2f),asphalt);
        foreach(float x in new[]{-68f,68f})Box("Back lane",roads,new Vector3(x,.03f,0),new Vector3(3.8f,.06f,114),soil);
        foreach(float x in new[]{-35f,35f})Box("Residential access lane",roads,new Vector3(x,.03f,0),new Vector3(3.5f,.06f,114),soil);
        foreach(int side in new[]{-1,1})
        {
            Box("Earth shoulder",roads,new Vector3(side*4.0f,.018f,5),new Vector3(1.3f,.036f,155),soil);
            Box("Drain channel bed",roads,new Vector3(side*5.1f,.015f,5),new Vector3(.65f,.03f,155),dark);
            foreach(float offset in new[]{-.38f,.38f})Box("Concrete drainage curb",roads,new Vector3(side*5.1f+offset,.09f,5),new Vector3(.13f,.18f,155),concrete);
        }
        var paint=VillageAssetLibrary.Tint("Worn yellow road paint",asphalt,new Color(2.6f,1.9f,.55f));
        for(float z=-66;z<118;z+=7)if(Mathf.Abs(z)>5)Box("Faded center dash",roads,new Vector3(0,.08f,z),new Vector3(.10f,.008f,2.5f),paint,false);
        for(float x=-62;x<65;x+=8)if(Mathf.Abs(x)>5)Box("Cross street dash",roads,new Vector3(x,.087f,0),new Vector3(2.3f,.008f,.1f),paint,false);
        for(int side=-1;side<=1;side+=2)foreach(float z in new[]{-56f,-34f,-12f,10f,34f,59f,82f})StreetLamp(roads,new Vector3(side*6.4f,0,z),side*90);
        foreach(float z in new[]{100f,117f})StreetLamp(roads,new Vector3(4.4f,0,z),90);
        foreach(int side in new[]{-1,1})foreach(float x in new[]{38f,61f})StreetLamp(roads,new Vector3(side*x,0,4.5f),180);
        var gate=Group("Village entrance arch",roads,new Vector3(0,0,-64));
        foreach(int s in new[]{-1,1})Box("Village gate column",gate,new Vector3(s*5.6f,2.2f,0),new Vector3(.6f,4.4f,.6f),brick);
        Box("Village name beam",gate,new Vector3(0,4.15f,0),new Vector3(12,.7f,.5f),wood);Sign("บ้านริมคลอง",gate,new Vector3(0,4.1f,-.3f),9);
        Sign("วัดริมคลอง  ↑  150 ม.",roads,new Vector3(5.9f,2.2f,72),4.8f);
        Sign("ร้านชำ  →",roads,new Vector3(8.5f,2,3.5f),3.5f,0);
    }
    static void StreetLamp(Transform parent,Vector3 position,float angle)
    {
        var model=VillageAssetLibrary.Place("street_lamp_01",parent,position,angle,1);
        var light=Spot("Village street light",parent,position+new Vector3(0,4.8f,0),new Color(1,.77f,.47f),150,15,118);streetLights.Add(light);
        Sphere("Streetlamp illuminated bulb",parent,position+new Vector3(0,4.75f,0),new Vector3(.18f,.12f,.18f),glow);
    }
    static void Utilities(Transform world)
    {
        var utilities=Group("Utilities - electricity water and public services",world,new Vector3(0,0,3));
        Vector3? previous=null;
        for(float z=-65;z<=110;z+=25)
        {
            var point=new Vector3(-8,0,z);
            Cylinder("Concrete power pole",utilities,point+Vector3.up*4.2f,.13f,8.4f,concrete,true);
            Box("Power crossarm",utilities,point+new Vector3(0,7.9f,0),new Vector3(2.8f,.13f,.14f),dark,false);
            foreach(float x in new[]{-1f,0,1f})
            {
                Cylinder("Porcelain insulator",utilities,point+new Vector3(x,8.05f,0),.07f,.25f,white);
                if(previous.HasValue)
                {
                    var points=new Vector3[13];for(int i=0;i<points.Length;i++){float t=i/12f;points[i]=Vector3.Lerp(previous.Value,point,t)+new Vector3(x,8.2f-Mathf.Sin(t*Mathf.PI)*.8f,0);}Tube("Overhead distribution wire",utilities,points,.014f,dark);
                }
            }
            previous=point;
        }
        var tank=Group("Village water tower",utilities,new Vector3(-49,0,61));
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box("Tank tower leg",tank,new Vector3(x*1.2f,3.6f,z*1.2f),new Vector3(.18f,7.2f,.18f),dark);
        Cylinder("Water storage tank",tank,new Vector3(0,7.5f,0),1.8f,3.0f,iron,true);
        for(int i=0;i<9;i++)Box("Access ladder rung",tank,new Vector3(1.45f,.55f+i*.7f,1.4f),new Vector3(.55f,.05f,.07f),dark,false);
        var shelter=Group("Bus shelter",utilities,new Vector3(-12,0,-57));
        foreach(float x in new[]{-2.8f,2.8f})Cylinder("Shelter post",shelter,new Vector3(x,1.4f,0),.07f,2.8f,dark,true);
        Roof("Shelter roof",shelter,new Vector3(0,2.8f,0),7,3.7f,.5f,iron);VillageAssetLibrary.Place("painted_wooden_bench",shelter,Vector3.zero,0);
        Sign("จุดรับส่ง บ้านริมคลอง",shelter,new Vector3(0,2.4f,-.3f),5.8f);
        foreach(float x in new[]{-1.5f,1.5f})VillageAssetLibrary.Place("metal_trash_can",utilities,new Vector3(31+x,0,5),0);
        Table(utilities,new Vector3(35,0,48),2.1f,1.2f);
    }
    static void Landscape(Transform world)
    {
        var nature=Group("Trees gardens grass and fields",world,Vector3.zero);
        for(int i=0;i<54;i++)
        {
            float x=i<32?(i%2==0?-1:1)*UnityEngine.Random.Range(77,100):UnityEngine.Random.Range(-83,83);
            float z=i<32?UnityEngine.Random.Range(-72,218):UnityEngine.Random.Range(85,121);
            if(Mathf.Abs(x)<10||((x+48)*(x+48)/400+(z-106)*(z-106)/500)<1.15f)continue;
            VillageAssetLibrary.Place("tree_small_02",nature,new Vector3(x,0,z),UnityEngine.Random.Range(0,360),UnityEngine.Random.Range(.8f,1.6f));
        }
        for(int i=0;i<180;i++)
        {
            float x=UnityEngine.Random.Range(-97,97),z=UnityEngine.Random.Range(-76,213);
            if(Mathf.Abs(x)<7.2f||Mathf.Abs(z)<6||z>124&&Mathf.Abs(x)<37||Mathf.Abs(x)<62&&z<78)continue;
            float pond=(x+48)*(x+48)/361+(z-106)*(z-106)/484;if(pond<1)continue;
            VillageAssetLibrary.Place(i%4==0?"shrub_01":"grass_medium_01",nature,new Vector3(x,0,z),UnityEngine.Random.Range(0,360),UnityEngine.Random.Range(.7f,1.4f));
        }
        // Roadside weeds and clusters make the settlement less perfectly manicured.
        for(int i=0;i<95;i++)
        {
            float z=UnityEngine.Random.Range(-65,119),x=(i%2==0?-1:1)*UnityEngine.Random.Range(5.7f,7.6f);
            if(Mathf.Abs(z)<6)continue;VillageAssetLibrary.Place("grass_medium_01",nature,new Vector3(x,0,z),UnityEngine.Random.Range(0,360),UnityEngine.Random.Range(.5f,.95f));
        }
        for(int side=-1;side<=1;side+=2)
        {
            var field=Group("Vegetable field",nature,new Vector3(side*89,0,25));
            for(int row=0;row<9;row++)Box("Cultivated earth row",field,new Vector3((row-4)*1.7f,.06f,0),new Vector3(1.15f,.12f,25),soil);
            for(int row=0;row<9;row+=2)for(int z=-10;z<=10;z+=3)VillageAssetLibrary.Place("grass_medium_01",field,new Vector3((row-4)*1.7f,.12f,z),0,.8f);
        }
    }
    public static void ApplyNight()
    {
        RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.25f,.30f,.45f);RenderSettings.ambientEquatorColor=new Color(.14f,.18f,.27f);RenderSettings.ambientGroundColor=new Color(.05f,.07f,.1f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.045f,.065f,.11f);RenderSettings.fogStartDistance=85;RenderSettings.fogEndDistance=240;
        var light=GameObject.Find("Village moonlight").GetComponent<Light>();light.color=new Color(.48f,.62f,1);light.intensity=.48f;
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>()){camera.backgroundColor=new Color(.12f,.17f,.28f);camera.clearFlags=CameraClearFlags.SolidColor;}
    }
    public static void PreviewDay()
    {
        RenderSettings.fog=false;
        var light=GameObject.Find("Village moonlight").GetComponent<Light>();light.color=new Color(1,.95f,.84f);light.intensity=1.5f;
        RenderSettings.ambientSkyColor=new Color(.65f,.75f,.90f);RenderSettings.ambientEquatorColor=new Color(.48f,.52f,.56f);RenderSettings.ambientGroundColor=new Color(.28f,.27f,.25f);RenderSettings.fogColor=new Color(.57f,.68f,.8f);RenderSettings.fogStartDistance=150;RenderSettings.fogEndDistance=350;
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>())camera.backgroundColor=new Color(.46f,.65f,.88f);
    }
}
