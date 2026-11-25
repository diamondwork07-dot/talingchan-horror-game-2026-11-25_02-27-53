using System;
using UnityEngine;
using Talingchan.Village;

public static partial class VillageBuilder
{
    static int houseCount;
    static void Wall(Transform parent,string name,Vector3 center,float length,float height,float floor,Material material,bool door,bool windows=true)
    {
        var wall=Group(name,parent,center);
        // Front-facing panels use local X; openings remain real gaps in the collision.
        float doorWidth=1.6f;
        var holes=new System.Collections.Generic.List<Vector4>();
        if(door)holes.Add(new Vector4(-doorWidth/2,doorWidth/2,0,2.45f));
        if(windows)
        {
            float x=length*.30f;
            holes.Add(new Vector4(-x-.7f,-x+.7f,1.05f,2.25f));
            holes.Add(new Vector4(x-.7f,x+.7f,1.05f,2.25f));
        }
        holes.Sort((a,b)=>a.x.CompareTo(b.x));
        float cursor=-length/2;
        foreach(var hole in holes)
        {
            if(hole.x>cursor)Box("Wall panel",wall,new Vector3((cursor+hole.x)/2,floor+height/2,0),new Vector3(hole.x-cursor,height,.18f),material);
            if(hole.z>0)Box("Below window",wall,new Vector3((hole.x+hole.y)/2,floor+hole.z/2,0),new Vector3(hole.y-hole.x,hole.z,.18f),material);
            Box("Lintel",wall,new Vector3((hole.x+hole.y)/2,floor+(hole.w+height)/2,0),new Vector3(hole.y-hole.x,height-hole.w,.18f),material);
            if(hole.z>0)
            {
                float cx=(hole.x+hole.y)/2,cy=floor+(hole.z+hole.w)/2;
                Box("Window glass",wall,new Vector3(cx,cy,-.015f),new Vector3(hole.y-hole.x,.02f+hole.w-hole.z,.035f),glass,false).GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                foreach(float sx in new[]{hole.x,hole.y})Box("Window frame",wall,new Vector3(sx,cy,-.12f),new Vector3(.09f,hole.w-hole.z+.12f,.1f),wood,false);
                foreach(float sy in new[]{floor+hole.z,floor+hole.w})Box("Window frame",wall,new Vector3(cx,sy,-.12f),new Vector3(hole.y-hole.x+.12f,.09f,.1f),wood,false);
                Box("Window mullion",wall,new Vector3(cx,cy,-.13f),new Vector3(.055f,hole.w-hole.z,.09f),wood,false);
                Box("Window sill",wall,new Vector3(cx,floor+hole.z,-.19f),new Vector3(hole.y-hole.x+.25f,.08f,.32f),wood,false);
            }
            cursor=hole.y;
        }
        if(cursor<length/2)Box("Wall panel",wall,new Vector3((cursor+length/2)/2,floor+height/2,0),new Vector3(length/2-cursor,height,.18f),material);
        if(door)
        {
            foreach(int s in new[]{-1,1})Box("Door jamb",wall,new Vector3(s*(doorWidth/2+.03f),floor+1.22f,-.12f),new Vector3(.11f,2.5f,.12f),wood,false);
            var hinge=Group("Open front door",wall,new Vector3(-doorWidth/2,floor+1.2f,0));hinge.localRotation=Quaternion.Euler(0,-82,0);
            Box("Door leaf",hinge,new Vector3(doorWidth/2,0,0),new Vector3(doorWidth,2.4f,.065f),wood);
            Sphere("Door handle",hinge,new Vector3(doorWidth-.13f,0,-.06f),Vector3.one*.065f,gold);
        }
    }
    static Transform House(string name,Transform parent,Vector3 position,float angle,bool raised,int variant,bool plot=true)
    {
        houseCount++;
        var h=Group(name,parent,position);h.localRotation=Quaternion.Euler(0,angle,0);
        float w=10.2f,d=11,floor=raised?1.1f:.20f,height=2.9f;
        Material exterior=variant%3==0?wood:VillageAssetLibrary.Tint("Plaster_variant_"+variant,plaster,new[]{new Color(.75f,.78f,.69f),new Color(.69f,.72f,.64f),new Color(.77f,.67f,.53f),new Color(.52f,.64f,.65f)}[variant%4]);
        if(!raised)Box("Masonry plinth",h,new Vector3(0,.08f,0),new Vector3(w+.12f,.16f,d+.12f),brick);
        else foreach(float x in new[]{-4.6f,0,4.6f})foreach(float z in new[]{-4.8f,0,4.8f})Cylinder("Stilt footing",h,new Vector3(x,floor/2,z),.17f,floor,concrete,true);
        Box("Timber floor",h,new Vector3(0,floor-.065f,0),new Vector3(w,.13f,d),wood);
        Wall(h,"Front facade",new Vector3(0,0,-d/2),w,height,floor,exterior,true);
        var rear=Group("Rear facade",h,new Vector3(0,0,d/2));rear.localRotation=Quaternion.Euler(0,180,0);Wall(rear,"Rear walls",Vector3.zero,w,height,floor,exterior,false);
        foreach(int side in new[]{-1,1}){var r=Group("Side facade",h,new Vector3(side*w/2,0,0));r.localRotation=Quaternion.Euler(0,-side*90,0);Wall(r,"Side walls",Vector3.zero,d,height,floor,exterior,false);}
        Box("Ceiling",h,new Vector3(0,floor+height+.025f,0),new Vector3(w,.10f,d),plaster);
        Roof("Weathered roof",h,new Vector3(0,floor+height+.08f,0),w+1.8f,d+1.9f,1.85f,variant%3==0?iron:roof);
        // Wood gables close the triangular space beneath the roof ridge.
        for(int side=-1;side<=1;side+=2)
        {
            var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-w/2,floor+height,side*d/2),new Vector3(w/2,floor+height,side*d/2),new Vector3(0,floor+height+1.75f,side*d/2)};mesh.uv=new[]{new Vector2(0,0),new Vector2(w,0),new Vector2(w/2,1.75f)};mesh.triangles=side<0?new[]{0,2,1}:new[]{0,1,2};mesh.RecalculateNormals();MeshObject("Timber gable",h,Persist(mesh),wood,false);
        }
        Box("Front veranda",h,new Vector3(0,floor-.09f,-d/2-.85f),new Vector3(6.8f,.18f,1.8f),wood);
        Roof("Veranda awning",h,new Vector3(0,floor+2.5f,-d/2-.75f),7.1f,2.2f,.45f,iron);
        foreach(int side in new[]{-1,1})Cylinder("Veranda post",h,new Vector3(side*3.05f,floor+1.2f,-d/2-1.55f),.065f,2.4f,wood,true);
        int steps=raised?8:1;float run=.29f;
        for(int i=0;i<steps;i++){float hh=floor*(i+1)/steps;Box("Entry step "+i,h,new Vector3(0,hh/2,-d/2-1.85f-(steps-1-i)*run),new Vector3(2.4f,hh,run+.025f),concrete);}
        Sign("บ้านเลขที่ "+(houseCount+100),h,new Vector3(-3.3f,floor+2.35f,-d/2-.13f),2.1f);
        Box("Gutter",h,new Vector3(-w/2-.75f,floor+height-.05f,0),new Vector3(.12f,.12f,d+2),dark,false);
        Cylinder("Rain downpipe",h,new Vector3(-w/2-.75f,(floor+height)/2,d/2+.6f),.045f,floor+height,dark,false);
        // Furnished rooms leave the central entrance route free.
        Box("Bedroom partition",h,new Vector3(-1.4f,floor+1.4f,3.25f),new Vector3(.13f,2.8f,4.3f),plaster);
        Box("Bed frame",h,new Vector3(-3.2f,floor+.22f,3.3f),new Vector3(2.6f,.44f,3.5f),wood);
        Box("Mattress",h,new Vector3(-3.2f,floor+.49f,3.3f),new Vector3(2.5f,.13f,3.35f),cloth);
        Box("Pillow",h,new Vector3(-3.2f,floor+.62f,4.45f),new Vector3(1.65f,.17f,.6f),white,false);
        Box("Kitchen counter",h,new Vector3(3.7f,floor+.46f,4.4f),new Vector3(2.2f,.92f,1.0f),concrete);
        Box("Stove top",h,new Vector3(3.7f,floor+.94f,4.4f),new Vector3(1.4f,.06f,.8f),dark,false);
        foreach(float x in new[]{3.3f,4.0f})Cylinder("Cooker ring",h,new Vector3(x,floor+.99f,4.4f),.19f,.035f,dark,false);
        Table(h,new Vector3(2.3f,floor,-.3f),1.5f,1.1f);
        VillageAssetLibrary.Place("plastic_monobloc_chair_01",h,new Vector3(2.3f,floor,-1.6f),180,.95f);
        VillageAssetLibrary.Place("plastic_crate_01",h,new Vector3(4.2f,floor,2.4f),12);
        VillageAssetLibrary.Place("painted_wooden_bench",h,new Vector3(-2.1f,floor,-6.35f),0,.95f);
        VillageAssetLibrary.Place("planter_pot_clay",h,new Vector3(4.1f,0,-6.7f),25,1.45f);
        var inside=Spot("House interior light",h,new Vector3(.7f,floor+2.65f,-.2f),new Color(1,.78f,.5f),65,9,145);interiorLights.Add(inside);
        Box("Ceiling lamp",h,new Vector3(.7f,floor+2.76f,-.2f),new Vector3(.55f,.07f,.22f),glow,false);
        var entrance=h.gameObject.AddComponent<VillageEntrance>();entrance.label=name;entrance.outside=new Vector3(0,.08f,-14);entrance.inside=new Vector3(0,floor+.05f,-3.5f);
        if(plot)
        {
            YardFence(h);
            Box("Entrance path",h,new Vector3(0,.025f,-12.9f),new Vector3(2.65f,.05f,7.8f),concrete);
            VillageAssetLibrary.Place("metal_trash_can",h,new Vector3(5.9f,0,-10.5f),30,.95f);
            VillageAssetLibrary.Place("tree_small_02",h,new Vector3(-6,0,7),variant*47,.7f+variant%3*.1f);
            VillageAssetLibrary.Place("shrub_01",h,new Vector3(6.4f,0,4),variant*17,1.1f);
            VillageAssetLibrary.Place("covered_car",h,new Vector3(5.6f,0,-4),0,.95f);
            foreach(float x in new[]{-5.5f,-4.1f})
            {
                VillageAssetLibrary.Place("planter_pot_clay",h,new Vector3(x,0,-8.5f),0,.75f);
                VillageAssetLibrary.Place("shrub_01",h,new Vector3(x,.4f,-8.5f),35,.45f);
            }
        }
        return h;
    }
    static void Table(Transform parent,Vector3 position,float w,float d)
    {
        Box("Table",parent,position+Vector3.up*.76f,new Vector3(w,.1f,d),wood);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box("Table leg",parent,position+new Vector3(x*(w/2-.12f),.35f,z*(d/2-.12f)),new Vector3(.09f,.7f,.09f),wood);
    }
    static void YardFence(Transform parent)
    {
        for(int side=-1;side<=1;side+=2)
        {
            Box("Garden wall",parent,new Vector3(side*8,.40f,0),new Vector3(.16f,.8f,22.6f),plaster);
            for(float z=-11.3f;z<=11.3f;z+=2.8f)Box("Fence post",parent,new Vector3(side*8,.75f,z),new Vector3(.28f,1.5f,.28f),concrete);
            for(float z=-10.9f;z<11;z+=.32f)Box("Fence picket",parent,new Vector3(side*8,1.08f,z),new Vector3(.07f,.65f,.055f),dark,false);
            Box("Gate side wall",parent,new Vector3(side*5.1f,.4f,-11.3f),new Vector3(5.8f,.8f,.18f),plaster);
            Box("Gatepost",parent,new Vector3(side*2.1f,.8f,-11.3f),new Vector3(.34f,1.6f,.34f),concrete);
        }
        Box("Rear boundary",parent,new Vector3(0,.45f,11.3f),new Vector3(16,.9f,.18f),plaster);
    }
    static void Shop(Transform parent)
    {
        var shop=Group("Village grocery - ร้านชำ",parent,new Vector3(22,0,13));
        Box("Shop floor",shop,new Vector3(0,.08f,0),new Vector3(13,.16f,10),concrete);
        Box("Rear wall",shop,new Vector3(0,1.6f,5),new Vector3(13,3.2f,.2f),plaster);
        foreach(int s in new[]{-1,1})Box("Side wall",shop,new Vector3(s*6.5f,1.6f,0),new Vector3(.2f,3.2f,10),brick);
        Roof("Shop roof",shop,new Vector3(0,3.3f,0),14,12,1.3f,iron);
        Box("Shop fascia",shop,new Vector3(0,2.9f,-5),new Vector3(13,.65f,.22f),wood);
        Sign("ร้านชำ ริมคลอง",shop,new Vector3(0,3.05f,-5.15f),7);
        Box("Counter",shop,new Vector3(-3.6f,.58f,-2),new Vector3(4.2f,1.15f,1.0f),wood);
        for(int row=0;row<3;row++)
        {
            Box("Shelf",shop,new Vector3(0,.55f+row*.65f,3.8f),new Vector3(10,.09f,.85f),wood);
            for(int i=0;i<10;i++)Box("Packaged goods",shop,new Vector3(-4.5f+i,.8f+row*.65f,3.8f),new Vector3(.3f,.4f,.24f),i%2==0?white:cloth,false);
        }
        foreach(float x in new[]{-4.5f,4.5f})for(int row=0;row<2;row++)VillageAssetLibrary.Place("plastic_crate_01",shop,new Vector3(x,row*.37f,-3.5f),15);
        VillageAssetLibrary.Place("plastic_monobloc_chair_01",shop,new Vector3(-4.4f,.16f,-.3f),30);
        Box("Shop entry apron",shop,new Vector3(0,.03f,-6.5f),new Vector3(13,.06f,3),concrete);
        var light=Spot("Shop fluorescent light",shop,new Vector3(0,2.9f,0),new Color(.84f,.95f,1),95,11,145);interiorLights.Add(light);
        Box("Fluorescent fixture",shop,new Vector3(0,3.05f,0),new Vector3(1.4f,.07f,.18f),glow,false);
        var entry=shop.gameObject.AddComponent<VillageEntrance>();entry.label="Village grocery";entry.outside=new Vector3(1,.08f,-7);entry.inside=new Vector3(1,.21f,-2);
    }
    static void MarketAndHall(Transform parent)
    {
        var market=Group("Covered village market",parent,new Vector3(45,0,-13));
        market.localRotation=Quaternion.Euler(0,180,0);
        for(int booth=0;booth<3;booth++)
        {
            float x=(booth-1)*4.5f;
            foreach(int dx in new[]{-1,1})foreach(int dz in new[]{-1,1})Cylinder("Market canopy post",market,new Vector3(x+dx*1.9f,1.45f,dz*2),.055f,2.9f,dark,true);
            var canopy=Box("Canvas awning",market,new Vector3(x,3.05f,0),new Vector3(4.25f,.06f,4.5f),booth%2==0?cloth:iron,false);canopy.transform.localRotation=Quaternion.Euler(7,0,0);
            Table(market,new Vector3(x,0,-.4f),3.5f,1.25f);
            for(int i=0;i<3;i++)VillageAssetLibrary.Place("plastic_crate_01",market,new Vector3(x+(i-1),.82f,-.4f),0,.75f);
            VillageAssetLibrary.Place("plastic_monobloc_chair_01",market,new Vector3(x,0,1.5f),0);
        }
        Sign("ตลาดเย็น บ้านริมคลอง",market,new Vector3(0,3.45f,-2.3f),9);
        var hall=Group("Community hall - ศาลาประชาคม",parent,new Vector3(47,0,44));
        Box("Hall platform",hall,new Vector3(0,.2f,0),new Vector3(17,.4f,13),concrete);
        foreach(float x in new[]{-7.5f,0,7.5f})foreach(int z in new[]{-1,1})Cylinder("Hall column",hall,new Vector3(x,2.2f,z*5.5f),.15f,4,concrete,true);
        Roof("Hall roof",hall,new Vector3(0,4.3f,0),19,15,2,roof);
        Box("Hall front step",hall,new Vector3(0,.10f,-7),new Vector3(6,.2f,1),concrete);
        Sign("ศาลาประชาคม",hall,new Vector3(0,3.8f,-5.65f),7);
        for(int x=-1;x<=1;x++)for(int z=0;z<2;z++)VillageAssetLibrary.Place("plastic_monobloc_chair_01",hall,new Vector3(x*2.7f,.4f,z*2.4f),180);
        var light=Spot("Hall light",hall,new Vector3(0,3.9f,0),new Color(1,.9f,.7f),140,14,145);interiorLights.Add(light);
    }
}
