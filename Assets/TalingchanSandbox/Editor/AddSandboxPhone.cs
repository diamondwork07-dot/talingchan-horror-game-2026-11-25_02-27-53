using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Talingchan.Sandbox;

public static class AddSandboxPhone
{
    [MenuItem("Talingchan/Add Phone System")]
    public static void Install() { Main(); }
    public static object Main()
    {
        if (Application.isPlaying) throw new System.Exception("Stop Play Mode before adding the phone.");
        var player = UnityEngine.Object.FindAnyObjectByType<SandboxPlayer>();
        if (player == null || player.avatar == null) throw new System.Exception("Open the sandbox with its test mannequin first.");
        var existing = player.GetComponent<SandboxPhone>();
        if (existing != null) return new { installed = true, status = "Already installed" };
        Undo.RecordObject(player, "Add incoming phone system");
        var avatar = player.avatar.GetComponent<SandboxAvatar>();
        Transform shoulder = avatar.rightArm;
        var shirt=AssetDatabase.LoadAssetAtPath<Material>("Assets/TalingchanSandbox/Materials/Avatar_BlueShirt.mat");
        var skin=AssetDatabase.LoadAssetAtPath<Material>("Assets/TalingchanSandbox/Materials/Avatar_Skin.mat");
        var dark=Mat("Phone_Body",new Color(.035f,.045f,.06f));
        var screen=Mat("Phone_Screen",new Color(.12f,.65f,.72f));
        var silver=Mat("Phone_Trim",new Color(.36f,.40f,.43f));
        // Replace only the two original rigid arm meshes with a two-bone arm.
        foreach (string n in new[]{"Right arm mesh", "Hand"})
        { var old=shoulder.Find(n); if(old!=null) Undo.DestroyObjectImmediate(old.gameObject); }
        Shape("Upper arm",shoulder,new Vector3(0,-.14f,0),new Vector3(.17f,.14f,.17f),shirt,PrimitiveType.Capsule);
        Transform elbow=Group("Right elbow",shoulder,new Vector3(0,-.28f,0));
        Shape("Forearm",elbow,new Vector3(0,-.13f,0),new Vector3(.145f,.13f,.145f),shirt,PrimitiveType.Capsule);
        Transform hand=Group("Phone hand grip",elbow,new Vector3(0,-.26f,0));
        Shape("Right hand",hand,Vector3.zero,new Vector3(.16f,.17f,.14f),skin,PrimitiveType.Sphere);
        avatar.rightForearm=elbow;EditorUtility.SetDirty(avatar);
        Transform phone=Group("Phone - third person",player.avatar,new Vector3(.35f,.95f,.08f));
        BuildHandset(phone,dark,screen,silver);
        phone.gameObject.SetActive(false);
        Transform fp=Group("Phone - first person",player.view,new Vector3(.29f,-.57f,.58f));
        BuildHandset(fp,dark,screen,silver);
        Shape("First person hand",fp,new Vector3(.07f,-.10f,.01f),new Vector3(.12f,.17f,.15f),skin,PrimitiveType.Cube);
        Shape("First person sleeve",fp,new Vector3(.07f,-.31f,.02f),new Vector3(.14f,.30f,.14f),shirt,PrimitiveType.Cube);
        fp.gameObject.SetActive(false);
        var source=player.gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
        var system=Undo.AddComponent<SandboxPhone>(player.gameObject);
        system.player=player;system.shoulder=shoulder;system.forearm=elbow;system.hand=hand;system.handset=phone;system.firstPersonRig=fp;system.ringtone=source;
        EditorUtility.SetDirty(system);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=player.gameObject;
        return new {installed=true,status="Phone installed, automatic incoming call after six seconds"};
    }
    static Transform Group(string n,Transform p,Vector3 v) {var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=v;return t;}
    static Material Mat(string name,Color color)
    {
        string path="Assets/TalingchanSandbox/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",.35f);return m;
    }
    static void BuildHandset(Transform root,Material body,Material screen,Material trim)
    {
        Shape("Handset body",root,Vector3.zero,new Vector3(.035f,.29f,.145f),body,PrimitiveType.Cube);
        Shape("Screen",root,new Vector3(-.02f,0,0),new Vector3(.008f,.235f,.12f),screen,PrimitiveType.Cube);
        Shape("Speaker",root,new Vector3(-.025f,.126f,0),new Vector3(.008f,.012f,.055f),trim,PrimitiveType.Cube);
        Shape("Camera lens",root,new Vector3(.022f,.105f,.04f),new Vector3(.013f,.027f,.027f),trim,PrimitiveType.Sphere);
    }
    static void Shape(string name,Transform parent,Vector3 position,Vector3 scale,Material material,PrimitiveType type)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
    }
}
