using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static class VillageAssetLibrary
{
    public const string Root="Assets/TalingchanVillage";
    public const string Source=Root+"/Sources";
    [Serializable] class MaterialInfo{public string name,baseColor,normal,mask;public bool alphaClip;public float[] color;}
    [Serializable] class ModelInfo{public string id;public MaterialInfo[] materials;public int triangles,lowTriangles;}
    static Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();
    static Dictionary<string,Material> surfaces=new Dictionary<string,Material>();
    public static void Prepare()
    {
        foreach(var folder in new[]{"Materials","Prefabs","Meshes","Scenes","Terrain"})Directory.CreateDirectory(Root+"/"+folder);
        AssetDatabase.Refresh();
        foreach(string folder in Directory.GetDirectories(Source+"/Surfaces"))
        {
            string id=Path.GetFileName(folder);
            string prefix=folder.Replace('\\','/');
            surfaces[id]=MakeMaterial("PBR_"+id,prefix+"/diff.jpg",prefix+"/normal.jpg",prefix+"/mask.png",false);
            surfaces[id].mainTextureScale=Vector2.one*.5f;
            EditorUtility.SetDirty(surfaces[id]);
        }
        var heights=new Dictionary<string,float>{{"tree_small_02",7.5f},{"shrub_01",1.2f},{"grass_medium_01",.45f},{"street_lamp_01",5.2f},{"painted_wooden_bench",.9f},{"plastic_monobloc_chair_01",.88f},{"plastic_crate_01",.36f},{"metal_trash_can",.92f},{"planter_pot_clay",.60f},{"covered_car",1.6f}};
        foreach(string folder in Directory.GetDirectories(Source+"/Models"))
        {
            string prefix=folder.Replace('\\','/');
            var info=JsonUtility.FromJson<ModelInfo>(File.ReadAllText(prefix+"/materials.json"));
            var mats=new Dictionary<string,Material>();
            foreach(var m in info.materials)
                mats[m.name]=MakeMaterial(info.id+"_"+Safe(m.name),FilePath(prefix,m.baseColor),FilePath(prefix,m.normal),FilePath(prefix,m.mask),m.alphaClip);
            string path=Root+"/Prefabs/"+info.id+".prefab";
            var container=new GameObject(info.id);
            var high=ModelChild(prefix+"/"+info.id+".fbx",container.transform,mats,heights[info.id]);
            string lowPath=prefix+"/"+info.id+"_LOD1.fbx";
            if(File.Exists(lowPath))
            {
                var low=ModelChild(lowPath,container.transform,mats,heights[info.id]);
                var lod=container.AddComponent<LODGroup>();
                lod.SetLODs(new[]{new LOD(.11f,high.GetComponentsInChildren<Renderer>()),new LOD(.008f,low.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
            }
            if(info.id=="tree_small_02")
            {
                var c=container.AddComponent<CapsuleCollider>();c.radius=.22f;c.height=3;c.center=new Vector3(high.localPosition.x,1.5f,high.localPosition.z);
            }
            else if(info.id!="shrub_01"&&info.id!="grass_medium_01")
            {
                foreach(var filter in high.GetComponentsInChildren<MeshFilter>())filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
            }
            prefabs[info.id]=PrefabUtility.SaveAsPrefabAsset(container,path);
            UnityEngine.Object.DestroyImmediate(container);
        }
        AssetDatabase.SaveAssets();
    }
    static string FilePath(string prefix,string relative){return string.IsNullOrEmpty(relative)?null:prefix+"/"+relative;}
    static string Safe(string name){return System.Text.RegularExpressions.Regex.Replace(name,"[^a-zA-Z0-9_]","_");}
    static Transform ModelChild(string path,Transform parent,Dictionary<string,Material> materials,float height)
    {
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(importer!=null)
        {
            importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
        }
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(source==null)throw new Exception("Model missing: "+path);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);
        instance.transform.SetParent(parent,false);
        var renderers=instance.GetComponentsInChildren<Renderer>();
        foreach(var r in renderers)
        {
            r.sharedMaterials=r.sharedMaterials.Select(m=>m!=null&&materials.TryGetValue(m.name,out var replacement)?replacement:materials.Values.First()).ToArray();
            r.gameObject.isStatic=true;
        }
        Bounds bounds=BoundsOf(renderers);
        instance.transform.localScale*=height/Mathf.Max(.01f,bounds.size.y);
        bounds=BoundsOf(renderers);
        instance.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        return instance.transform;
    }
    public static Bounds BoundsOf(Renderer[] renderers)
    {
        if(renderers.Length==0)return new Bounds(Vector3.zero,Vector3.one);
        Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;
    }
    public static GameObject Place(string id,Transform parent,Vector3 localPosition,float angle=0,float scale=1)
    {
        if(!prefabs.TryGetValue(id,out var prefab))prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+id+".prefab");
        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.transform.SetParent(parent,false);g.transform.localPosition=localPosition;g.transform.localRotation=Quaternion.Euler(0,angle,0);g.transform.localScale=Vector3.one*scale;return g;
    }
    public static Material Surface(string id){return surfaces.TryGetValue(id,out var m)?m:AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/PBR_"+id+".mat");}
    public static Material Tint(string name,Material source,Color color)
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(source);AssetDatabase.CreateAsset(m,path);}m.color=color;m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    public static Material Solid(string name,Color color,float metallic=0,float smoothness=.25f)
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static Material MakeMaterial(string name,string color,string normal,string mask,bool alpha)
    {
        var material=Solid(name,Color.white);
        if(!string.IsNullOrEmpty(color))material.SetTexture("_BaseMap",Texture(color,false,false,alpha));
        if(!string.IsNullOrEmpty(normal)){material.SetTexture("_BumpMap",Texture(normal,true,true,false));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.8f);}
        if(!string.IsNullOrEmpty(mask))
        {
            var map=Texture(mask,false,true,false);material.SetTexture("_MetallicGlossMap",map);material.SetTexture("_OcclusionMap",map);material.EnableKeyword("_METALLICSPECGLOSSMAP");material.EnableKeyword("_OCCLUSIONMAP");material.SetFloat("_Smoothness",1);
        }
        if(alpha){material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.38f);material.SetFloat("_Cull",0);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;material.SetOverrideTag("RenderType","TransparentCutout");}
        return material;
    }
    static Texture2D Texture(string path,bool normal,bool linear,bool alpha)
    {
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new Exception("Texture missing: "+path);
        bool changed=importer.textureType!=(normal?TextureImporterType.NormalMap:TextureImporterType.Default)||importer.sRGBTexture==linear||importer.alphaIsTransparency!=alpha||importer.maxTextureSize!=2048;
        if(changed){importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!linear;importer.alphaIsTransparency=alpha;importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.anisoLevel=8;importer.SaveAndReimport();}
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
