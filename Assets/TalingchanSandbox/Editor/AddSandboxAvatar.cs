using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Talingchan.Sandbox;

public static class AddSandboxAvatar
{
    public static object Main()
    {
        if (Application.isPlaying) throw new System.Exception("Stop Play Mode first.");
        var player = UnityEngine.Object.FindAnyObjectByType<SandboxPlayer>();
        if(player == null) throw new System.Exception("Open SmallTownSandbox first.");
        if(player.avatar != null) throw new System.Exception("Avatar already exists.");
        Transform root = new GameObject("Avatar - test mannequin").transform;
        root.SetParent(player.transform, false);
        Material shirt=Mat("Avatar_BlueShirt",new Color(.08f,.38f,.80f));
        Material skin=Mat("Avatar_Skin",new Color(.90f,.67f,.43f));
        Material pants=Mat("Avatar_Trousers",new Color(.10f,.16f,.22f));
        Material orange=Mat("Avatar_Cap",new Color(1,.40f,.12f));
        Material shoes=Mat("Avatar_Shoes",new Color(.08f,.08f,.09f));
        Material eyes=Mat("Avatar_Eyes",Color.white);
        Shape("Torso",root,new Vector3(0,1.08f,0),new Vector3(.52f,.60f,.32f),shirt,PrimitiveType.Cube);
        Shape("Hips",root,new Vector3(0,.75f,0),new Vector3(.46f,.20f,.30f),pants,PrimitiveType.Cube);
        Shape("Neck",root,new Vector3(0,1.42f,0),new Vector3(.16f,.14f,.16f),skin,PrimitiveType.Sphere);
        Shape("Head",root,new Vector3(0,1.60f,0),new Vector3(.37f,.40f,.34f),skin,PrimitiveType.Sphere);
        Shape("Cap",root,new Vector3(0,1.76f,0),new Vector3(.39f,.15f,.36f),orange,PrimitiveType.Sphere);
        Shape("Cap brim",root,new Vector3(0,1.73f,.16f),new Vector3(.38f,.04f,.23f),orange,PrimitiveType.Cube);
        foreach(float x in new[]{-.075f,.075f})
        {
            Shape("Eye",root,new Vector3(x,1.62f,.155f),new Vector3(.075f,.065f,.035f),eyes,PrimitiveType.Sphere);
            Shape("Pupil",root,new Vector3(x,1.62f,.175f),new Vector3(.035f,.045f,.025f),shoes,PrimitiveType.Sphere);
        }
        var animation=root.gameObject.AddComponent<SandboxAvatar>();
        animation.leftArm=Limb("Left arm",root,new Vector3(-.35f,1.34f,0),.52f,.17f,shirt,skin,false);
        animation.rightArm=Limb("Right arm",root,new Vector3(.35f,1.34f,0),.52f,.17f,shirt,skin,false);
        animation.leftLeg=Limb("Left leg",root,new Vector3(-.145f,.72f,0),.61f,.20f,pants,shoes,true);
        animation.rightLeg=Limb("Right leg",root,new Vector3(.145f,.72f,0),.61f,.20f,pants,shoes,true);
        player.avatar=root;player.thirdPerson=true;
        player.view.localPosition=new Vector3(0,2.26f,-3.33f);
        player.view.localRotation=Quaternion.Euler(12,0,0);
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=player.gameObject;
        return new {avatar=root.name,thirdPerson=player.thirdPerson,scene=player.gameObject.scene.path};
    }
    static Material Mat(string name,Color color)
    {
        string path="Assets/TalingchanSandbox/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",.15f);return m;
    }
    static Transform Limb(string name,Transform parent,Vector3 position,float length,float width,Material material,Material end,bool leg)
    {
        var pivot=new GameObject(name).transform;pivot.SetParent(parent,false);pivot.localPosition=position;
        Shape(name+" mesh",pivot,new Vector3(0,-length/2,0),new Vector3(width,length/2,width),material,PrimitiveType.Capsule);
        Shape(leg?"Shoe":"Hand",pivot,new Vector3(0,-length,leg?.07f:0),leg?new Vector3(.23f,.14f,.36f):new Vector3(.16f,.18f,.15f),end,leg?PrimitiveType.Cube:PrimitiveType.Sphere);
        return pivot;
    }
    static void Shape(string name,Transform parent,Vector3 position,Vector3 scale,Material material,PrimitiveType type)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
    }
}
