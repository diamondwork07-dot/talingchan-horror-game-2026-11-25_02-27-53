using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static partial class VillageBuilder
{
    const string Root=VillageAssetLibrary.Root;
    static int meshIndex;
    static Dictionary<string,Mesh> boxMeshes=new Dictionary<string,Mesh>();
    static Material wood,plaster,roof,iron,concrete,brick,soil,asphalt,dark,gold,white,glass,cloth,glow,water;
    static Font thaiFont;
    static List<Light> localLights=new List<Light>();
    static List<Light> streetLights=new List<Light>();
    static List<Light> interiorLights=new List<Light>();
    static Transform Group(string name,Transform parent,Vector3 position)
    {
        var t=new GameObject(name).transform;if(parent!=null)t.SetParent(parent,false);t.localPosition=position;return t;
    }
    static Mesh Persist(Mesh mesh)
    {
        mesh.name="VillageMesh_"+(meshIndex++).ToString("D4");
        AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+mesh.name+".asset");return mesh;
    }
    static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material,bool collider)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial=material;if(collider)g.AddComponent<MeshCollider>().sharedMesh=mesh;g.isStatic=true;return g;
    }
    static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool collide=true)
    {
        string key=size.ToString("F3");
        if(!boxMeshes.TryGetValue(key,out var mesh))
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            Vector3 h=size*.5f;
            Action<Vector3,Vector3,Vector3,Vector3,float,float> face=(a,b,c,d,w,hh)=>
            {int n=v.Count;v.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{new Vector2(0,0),new Vector2(w,0),new Vector2(w,hh),new Vector2(0,hh)});tri.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});};
            face(new Vector3(-h.x,-h.y,-h.z),new Vector3(h.x,-h.y,-h.z),new Vector3(h.x,h.y,-h.z),new Vector3(-h.x,h.y,-h.z),size.x,size.y);
            face(new Vector3(h.x,-h.y,h.z),new Vector3(-h.x,-h.y,h.z),new Vector3(-h.x,h.y,h.z),new Vector3(h.x,h.y,h.z),size.x,size.y);
            face(new Vector3(-h.x,-h.y,h.z),new Vector3(-h.x,-h.y,-h.z),new Vector3(-h.x,h.y,-h.z),new Vector3(-h.x,h.y,h.z),size.z,size.y);
            face(new Vector3(h.x,-h.y,-h.z),new Vector3(h.x,-h.y,h.z),new Vector3(h.x,h.y,h.z),new Vector3(h.x,h.y,-h.z),size.z,size.y);
            face(new Vector3(-h.x,h.y,-h.z),new Vector3(h.x,h.y,-h.z),new Vector3(h.x,h.y,h.z),new Vector3(-h.x,h.y,h.z),size.x,size.z);
            face(new Vector3(-h.x,-h.y,h.z),new Vector3(h.x,-h.y,h.z),new Vector3(h.x,-h.y,-h.z),new Vector3(-h.x,-h.y,-h.z),size.x,size.z);
            mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();Persist(mesh);boxMeshes[key]=mesh;
        }
        var g=MeshObject(name,parent,mesh,material,false);g.transform.localPosition=position;
        if(collide)g.AddComponent<BoxCollider>().size=size;return g;
    }
    static GameObject Cylinder(string name,Transform parent,Vector3 position,float radius,float height,Material material,bool collide=false)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=new Vector3(radius*2,height/2,radius*2);g.GetComponent<Renderer>().sharedMaterial=material;
        if(!collide)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return g;
    }
    static GameObject Sphere(string name,Transform parent,Vector3 position,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return g;
    }
    static GameObject Tube(string name,Transform parent,Vector3[] points,float radius,Material material,bool collide=false)
    {
        const int sides=8;var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();float length=0;
        for(int i=0;i<points.Length;i++)
        {
            if(i>0)length+=Vector3.Distance(points[i],points[i-1]);
            Vector3 tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(i-1,0)]).normalized;
            Vector3 axis=Vector3.Cross(tangent,Mathf.Abs(Vector3.Dot(tangent,Vector3.up))>.9f?Vector3.right:Vector3.up).normalized;
            Vector3 other=Vector3.Cross(tangent,axis).normalized;
            for(int j=0;j<sides;j++)
            {
                float angle=j*Mathf.PI*2/sides;v.Add(points[i]+(axis*Mathf.Cos(angle)+other*Mathf.Sin(angle))*radius);uv.Add(new Vector2((float)j/sides,length));
                if(i>0){int a=(i-1)*sides+j,b=(i-1)*sides+(j+1)%sides,c=i*sides+j,d=i*sides+(j+1)%sides;tri.AddRange(new[]{a,b,c,b,d,c});}
            }
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return MeshObject(name,parent,Persist(mesh),material,collide);
    }
    static GameObject Lathe(string name,Transform parent,Vector3 position,Vector2[] profile,Material material,int segments=40)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
        for(int i=0;i<profile.Length;i++)for(int j=0;j<=segments;j++)
        {
            float angle=(float)j/segments*Mathf.PI*2;v.Add(new Vector3(Mathf.Cos(angle)*profile[i].x,profile[i].y,Mathf.Sin(angle)*profile[i].x));uv.Add(new Vector2((float)j/segments*4,profile[i].y));
            if(i>0&&j<segments){int a=(i-1)*(segments+1)+j,b=a+1,c=i*(segments+1)+j,d=c+1;tri.AddRange(new[]{a,c,b,b,c,d});}
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var g=MeshObject(name,parent,Persist(mesh),material,false);g.transform.localPosition=position;return g;
    }
    static void Roof(string name,Transform parent,Vector3 center,float width,float depth,float rise,Material material,bool thai=false)
    {
        for(int side=-1;side<=1;side+=2)
        {
            int segments=thai?12:1;var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            for(int i=0;i<=segments;i++)
            {
                float t=(float)i/segments;float y=rise*(1-Mathf.Pow(t,.78f))+(thai?.4f*Mathf.Pow(t,8):0);
                for(int z=0;z<2;z++){v.Add(center+new Vector3(side*t*width/2,y,(z==0?-1:1)*depth/2));uv.Add(new Vector2(t*width/2,(z==0?0:depth)));}
                if(i>0){int a=(i-1)*2,b=a+1,c=i*2,d=c+1;if(side==1)tri.AddRange(new[]{a,b,c,b,d,c});else tri.AddRange(new[]{a,c,b,b,c,d});}
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();MeshObject(name,parent,Persist(mesh),material,true);
            for(int end=-1;end<=1;end+=2)
            {
                var p=new Vector3[(thai?12:1)+1];for(int i=0;i<p.Length;i++){float t=(float)i/(p.Length-1);p[i]=center+new Vector3(side*t*width/2,rise*(1-Mathf.Pow(t,.78f))+(thai?.4f*Mathf.Pow(t,8):0),end*depth/2);}
                Tube("Roof fascia",parent,p,thai?.09f:.065f,thai?gold:wood);
            }
            Tube("Eave",parent,new[]{center+new Vector3(side*width/2,thai?.4f:0,-depth/2),center+new Vector3(side*width/2,thai?.4f:0,depth/2)},thai?.08f:.05f,thai?gold:wood);
        }
        Tube("Ridge cap",parent,new[]{center+new Vector3(0,rise,-depth/2),center+new Vector3(0,rise,depth/2)},thai?.14f:.1f,thai?gold:roof);
    }
    static void Sign(string text,Transform parent,Vector3 position,float width,float angle=0,bool board=true)
    {
        var root=Group("Sign - "+text,parent,position);root.localRotation=Quaternion.Euler(0,angle,0);
        if(board)Box("Signboard",root,Vector3.zero,new Vector3(width,.75f,.10f),dark,false);
        var t=Group("Thai lettering",root,new Vector3(0,0,-.065f));var tm=t.gameObject.AddComponent<TextMesh>();tm.text=text;tm.font=thaiFont;tm.GetComponent<Renderer>().sharedMaterial=thaiFont.material;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.fontSize=72;tm.characterSize=Mathf.Min(.1f,width/Mathf.Max(1,text.Length)*.22f);tm.color=new Color(.95f,.90f,.69f);
    }
    static Light Spot(string name,Transform parent,Vector3 position,Color color,float intensity,float range,float angle=120)
    {
        var t=Group(name,parent,position);t.localRotation=Quaternion.Euler(90,0,0);var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=color;l.intensity=intensity;l.range=range;l.spotAngle=angle;l.innerSpotAngle=angle*.6f;l.shadows=LightShadows.None;l.shadowBias=.035f;l.shadowNormalBias=.18f;localLights.Add(l);return l;
    }
}
