using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Talingchan.Sandbox;

public static class SandboxNightSetup
{
    const string Root="Assets/TalingchanSandbox";
    [MenuItem("Talingchan/Install Night and Left Flashlight")]
    public static void Install(){Main();}
    public static object Main()
    {
        if(Application.isPlaying)throw new System.Exception("Stop Play Mode first.");
        var player=UnityEngine.Object.FindAnyObjectByType<SandboxPlayer>();
        if(player==null)throw new System.Exception("Open the sandbox scene first.");
        if(player.GetComponent<SandboxFlashlight>()!=null){TuneLighting();return new{installed=true,status="Night lighting refreshed"};}
        ConfigurePipeline();
        player.thirdPerson=false;player.allowViewSwitch=false;
        player.avatar.gameObject.SetActive(false);
        player.view.localPosition=new Vector3(0,1.55f,0);player.view.localRotation=Quaternion.identity;
        var camera=player.view.GetComponent<Camera>();camera.backgroundColor=new Color(.035f,.055f,.10f);camera.farClipPlane=100;camera.allowHDR=true;
        RenderSettings.skybox=null;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.10f,.14f,.23f);
        RenderSettings.ambientEquatorColor=new Color(.055f,.075f,.12f);
        RenderSettings.ambientGroundColor=new Color(.018f,.025f,.04f);
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.009f,.016f,.032f);RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=24;RenderSettings.fogEndDistance=72;
        var sun=GameObject.Find("Sun").GetComponent<Light>();sun.name="Moon";sun.intensity=.22f;sun.color=new Color(.42f,.56f,1);sun.transform.rotation=Quaternion.Euler(28,-35,0);sun.shadows=LightShadows.Soft;

        var dark=Mat("Flashlight_body",new Color(.055f,.07f,.08f));
        var metal=Mat("Flashlight_metal",new Color(.26f,.29f,.30f));
        var lensMaterial=Mat("Flashlight_lens",new Color(.6f,.72f,.76f));lensMaterial.EnableKeyword("_EMISSION");
        var bulbMaterial=Mat("Streetlamp_glow",new Color(1,.76f,.42f));bulbMaterial.EnableKeyword("_EMISSION");bulbMaterial.SetColor("_EmissionColor",new Color(1,.68f,.32f)*4);
        var sleeve=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Avatar_BlueShirt.mat");
        var skin=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Avatar_Skin.mat");
        var rig=Group("Left Hand - Flashlight",player.view,new Vector3(-.35f,-.75f,.35f));
        Shape("Flashlight barrel",rig,Vector3.zero,new Vector3(.075f,.16f,.075f),dark,PrimitiveType.Cylinder,Quaternion.Euler(90,0,0));
        Shape("Flashlight head",rig,new Vector3(0,0,.16f),new Vector3(.108f,.045f,.108f),metal,PrimitiveType.Cylinder,Quaternion.Euler(90,0,0));
        var lens=Shape("Flashlight lens",rig,new Vector3(0,0,.207f),new Vector3(.092f,.006f,.092f),lensMaterial,PrimitiveType.Cylinder,Quaternion.Euler(90,0,0)).GetComponent<Renderer>();
        Shape("Tail cap",rig,new Vector3(0,0,-.16f),new Vector3(.079f,.018f,.079f),metal,PrimitiveType.Cylinder,Quaternion.Euler(90,0,0));
        Shape("Left palm",rig,new Vector3(0,-.072f,-.035f),new Vector3(.13f,.14f,.17f),skin,PrimitiveType.Cube,Quaternion.identity);
        Shape("Left forearm",rig,new Vector3(-.02f,-.25f,-.10f),new Vector3(.15f,.26f,.16f),sleeve,PrimitiveType.Cube,Quaternion.Euler(22,0,-8));
        var lightObject=Group("Flashlight beam",rig,new Vector3(0,0,.22f));
        var beam=lightObject.gameObject.AddComponent<Light>();beam.type=LightType.Spot;beam.color=new Color(1,.94f,.82f);beam.spotAngle=48;beam.innerSpotAngle=28;beam.range=28;beam.intensity=250;beam.shadows=LightShadows.Soft;beam.shadowBias=.025f;beam.shadowNormalBias=.15f;beam.shadowNearPlane=.04f;beam.enabled=false;
        var fill=Group("Hands fill light",player.view,new Vector3(0,.08f,.25f)).gameObject.AddComponent<Light>();fill.type=LightType.Point;fill.intensity=.3f;fill.range=.9f;fill.color=new Color(.62f,.72f,1);fill.shadows=LightShadows.None;
        var flashlight=Undo.AddComponent<SandboxFlashlight>(player.gameObject);flashlight.player=player;flashlight.firstPersonRig=rig;flashlight.beam=beam;flashlight.lens=lens;rig.gameObject.SetActive(false);

        var lightingRoot=Group("Night Lighting",null,Vector3.zero);
        var streetLights=new List<Light>();var bulbs=new List<Renderer>();var interiors=new List<Light>();
        var props=GameObject.Find("Street furniture").transform;
        foreach(var renderer in props.GetComponentsInChildren<Renderer>())
        {
            if(renderer.name!="Lamp head")continue;
            renderer.sharedMaterial=bulbMaterial;bulbs.Add(renderer);
            StreetLight(lightingRoot,renderer.transform.position,streetLights);
        }
        foreach(float x in new[]{-20f,-10f,10f,20f})
        {
            var post=Group("Cross street lamp",lightingRoot,new Vector3(x,0,-22.6f));
            Shape("Lamp pole",post,new Vector3(0,2.1f,0),new Vector3(.12f,2.1f,.12f),dark,PrimitiveType.Cylinder,Quaternion.identity);
            var head=Shape("Lamp head",post,new Vector3(0,4.25f,0),new Vector3(.65f,.22f,.65f),bulbMaterial,PrimitiveType.Cube,Quaternion.identity);
            bulbs.Add(head.GetComponent<Renderer>());StreetLight(lightingRoot,head.transform.position,streetLights);
        }
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())
        {
            if(light.gameObject.name!="Interior light")continue;
            light.type=LightType.Spot;light.transform.localRotation=Quaternion.Euler(90,0,0);light.spotAngle=150;light.innerSpotAngle=100;light.intensity=65;light.range=11;light.color=new Color(1,.78f,.47f);light.shadows=LightShadows.Soft;light.shadowBias=.025f;light.shadowNormalBias=.12f;
            Shape("Ceiling fixture",light.transform,new Vector3(0,0,-.06f),new Vector3(.65f,.08f,.32f),bulbMaterial,PrimitiveType.Cube,Quaternion.Euler(-90,0,0));
            interiors.Add(light);
        }
        var lighting=lightingRoot.gameObject.AddComponent<SandboxNightLighting>();lighting.streetLights=streetLights.ToArray();lighting.streetBulbs=bulbs.ToArray();lighting.interiorLights=interiors.ToArray();
        var volumeObject=Group("Night atmosphere",lightingRoot,Vector3.zero);
        var volume=volumeObject.gameObject.AddComponent<Volume>();volume.isGlobal=true;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom=profile.Add<Bloom>();bloom.intensity.Override(.25f);bloom.threshold.Override(1.1f);bloom.scatter.Override(.55f);
        var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);
        AssetDatabase.CreateAsset(profile,Root+"/Lighting/NightAtmosphere.asset");
        AssetDatabase.AddObjectToAsset(bloom,profile);AssetDatabase.AddObjectToAsset(tone,profile);volume.sharedProfile=profile;
        var cameraData=camera.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;
        EditorUtility.SetDirty(player);EditorUtility.SetDirty(flashlight);EditorUtility.SetDirty(lighting);
        TuneLighting();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);
        return new{installed=true,streetLights=streetLights.Count,interiorLights=interiors.Count,firstPerson=!player.thirdPerson,scene=player.gameObject.scene.path};
    }
    public static void TuneLighting()
    {
        var player=Object.FindAnyObjectByType<SandboxPlayer>();
        var flashlight=player.GetComponent<SandboxFlashlight>();
        flashlight.beamIntensity=250;
        var lighting=Object.FindAnyObjectByType<SandboxNightLighting>();
        foreach(var l in lighting.streetLights){l.intensity=120;l.range=14;}
        foreach(var l in lighting.interiorLights)l.intensity=65;
        RenderSettings.ambientSkyColor=new Color(.25f,.30f,.45f);
        RenderSettings.ambientEquatorColor=new Color(.14f,.18f,.27f);
        RenderSettings.ambientGroundColor=new Color(.05f,.07f,.10f);
        RenderSettings.fogColor=new Color(.045f,.065f,.11f);
        GameObject.Find("Moon").GetComponent<Light>().intensity=.35f;
        player.view.GetComponent<Camera>().backgroundColor=new Color(.12f,.17f,.28f);
        if(player.view.Find("Hands fill light")==null)
        {
            var fill=Group("Hands fill light",player.view,new Vector3(0,.08f,.25f)).gameObject.AddComponent<Light>();
            fill.type=LightType.Point;fill.intensity=.3f;fill.range=.9f;fill.color=new Color(.62f,.72f,1);fill.shadows=LightShadows.None;
        }
        EditorUtility.SetDirty(flashlight);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
    }
    static void StreetLight(Transform root,Vector3 head,List<Light> lights)
    {
        var t=Group("Street light - warm pool",root,head+Vector3.down*.18f);
        t.rotation=Quaternion.Euler(90,0,0);
        var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(1,.72f,.36f);l.range=14;l.spotAngle=120;l.innerSpotAngle=75;l.intensity=120;l.shadows=LightShadows.Soft;l.shadowBias=.035f;l.shadowNormalBias=.18f;
        lights.Add(l);
    }
    static void ConfigurePipeline()
    {
        Directory.CreateDirectory(Root+"/Lighting");AssetDatabase.Refresh();
        var active=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if(active==null)throw new System.Exception("This setup expects the sandbox URP pipeline.");
        var pipeline=Object.Instantiate(active);pipeline.name="Sandbox Night Pipeline";
        AssetDatabase.CreateAsset(pipeline,Root+"/Lighting/NightPipeline.asset");
        var settings=new SerializedObject(pipeline);
        var renderers=settings.FindProperty("m_RendererDataList");
        for(int i=0;i<renderers.arraySize;i++)
        {
            var source=renderers.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
            if(source==null)continue;
            var renderer=Object.Instantiate(source);renderer.name="Night Renderer "+i;
            if(renderer is UniversalRendererData universal)universal.renderingMode=RenderingMode.ForwardPlus;
            AssetDatabase.CreateAsset(renderer,Root+"/Lighting/NightRenderer_"+i+".asset");
            renderers.GetArrayElementAtIndex(i).objectReferenceValue=renderer;
        }
        settings.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;
        settings.FindProperty("m_AdditionalLightsRenderingMode").intValue=(int)LightRenderingMode.PerPixel;
        settings.FindProperty("m_SoftShadowsSupported").boolValue=true;
        settings.ApplyModifiedPropertiesWithoutUndo();
        pipeline.maxAdditionalLightsCount=8;pipeline.additionalLightsShadowmapResolution=4096;pipeline.shadowDistance=60;
        GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;EditorUtility.SetDirty(pipeline);
    }
    static Transform Group(string n,Transform p,Vector3 pos){var t=new GameObject(n).transform;if(p!=null)t.SetParent(p,false);t.localPosition=pos;return t;}
    static Material Mat(string n,Color c)
    {
        string path=Root+"/Materials/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=c;m.SetFloat("_Smoothness",.25f);return m;
    }
    static GameObject Shape(string n,Transform p,Vector3 pos,Vector3 scale,Material material,PrimitiveType primitive,Quaternion rotation)
    {
        var g=GameObject.CreatePrimitive(primitive);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localRotation=rotation;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;
        Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
}
