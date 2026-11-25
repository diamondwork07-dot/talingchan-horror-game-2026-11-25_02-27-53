using UnityEngine;
using UnityEditor;

public static class VillageVisualTuning
{
    public static void Apply()
    {
        const string root="Assets/TalingchanVillage";
        var white=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/Temple lime plaster.mat");
        if(white!=null){white.SetTexture("_BaseMap",null);white.color=new Color(.88f,.86f,.77f);EditorUtility.SetDirty(white);}
        foreach(string id in new[]{"tree_small_02_tree_small_02_leaves","tree_small_02_tree_small_02_branches","shrub_01_shrub_01","grass_medium_01_grass_medium_01"})
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/"+id+".mat");if(m==null)continue;
            m.SetFloat("_SpecularHighlights",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.SetFloat("_EnvironmentReflections",0);m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");m.SetTexture("_MetallicGlossMap",null);m.DisableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",0);
            // The decimated leaves use their actual geometric silhouettes rather than atlas edge pixels.
            if(id.EndsWith("_leaves")){m.SetTexture("_BaseMap",null);m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");m.DisableKeyword("_ALPHATEST_ON");m.SetFloat("_AlphaClip",0);m.renderQueue=2000;m.color=new Color(.24f,.38f,.12f);}
            EditorUtility.SetDirty(m);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{root+"/Materials"}))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if(m.name.StartsWith("Plaster_variant")||m.name=="PBR_damaged_plaster"){m.mainTextureScale=Vector2.one*.12f;EditorUtility.SetDirty(m);}
        }
        var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(root+"/Terrain/Layer_0.terrainlayer");if(layer!=null){layer.diffuseRemapMax=new Vector4(.65f,1.2f,.65f,1);EditorUtility.SetDirty(layer);}
    }
}
