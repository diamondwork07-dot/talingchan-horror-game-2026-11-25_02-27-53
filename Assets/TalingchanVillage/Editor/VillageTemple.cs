using UnityEngine;
using Talingchan.Village;

public static partial class VillageBuilder
{
    static void Temple(Transform world)
    {
        var temple=Group("Wat Rim Khlong - OUTSIDE VILLAGE",world,new Vector3(0,0,163));
        Box("Temple courtyard",temple,new Vector3(0,.04f,0),new Vector3(70,.08f,76),concrete);
        // Boundary wall and separate entrance establish a distinct temple compound.
        foreach(int side in new[]{-1,1})
        {
            Box("Temple boundary",temple,new Vector3(side*35,1.0f,0),new Vector3(.32f,2,76),white);
            Box("Boundary coping",temple,new Vector3(side*35,2.05f,0),new Vector3(.48f,.12f,76),gold);
            for(float z=-37;z<=37;z+=6) {Box("Temple wall pier",temple,new Vector3(side*35,1.2f,z),new Vector3(.65f,2.4f,.65f),white);Lathe("Pier finial",temple,new Vector3(side*35,2.4f,z),new[]{new Vector2(.35f,0),new Vector2(.3f,.16f),new Vector2(.18f,.3f),new Vector2(0,.65f)},gold,16);}
            Box("Temple front wall",temple,new Vector3(side*20,1,-38),new Vector3(30,2,.32f),white);
            Box("Entrance pier",temple,new Vector3(side*5,2.1f,-38),new Vector3(.9f,4.2f,.9f),white);
        }
        Box("Rear wall",temple,new Vector3(0,1,38),new Vector3(70,2,.32f),white);
        Roof("Temple gateway roof",temple,new Vector3(0,4.2f,-38),13,4.3f,2.0f,roof,true);
        Sign("วัดริมคลอง",temple,new Vector3(0,3.8f,-38.55f),7.5f);
        var hall=Group("Ubosot - enterable ordination hall",temple,new Vector3(0,0,2));
        float fy=.78f,w=18,d=26;
        Box("Temple lower plinth",hall,new Vector3(0,.18f,0),new Vector3(21,.36f,29),white);
        Box("Temple upper plinth",hall,new Vector3(0,.54f,0),new Vector3(19.5f,.36f,27.5f),white);
        Box("Temple hall floor",hall,new Vector3(0,fy-.03f,0),new Vector3(w,.12f,d),concrete);
        // The front has a generous open central portal; pillars flank it.
        foreach(int side in new[]{-1,1})Box("Front temple wall",hall,new Vector3(side*5.35f,fy+2,-d/2),new Vector3(7.3f,4,.25f),white);
        Box("Temple entrance lintel",hall,new Vector3(0,fy+3.55f,-d/2),new Vector3(3.5f,.9f,.25f),white);
        Box("Rear sanctuary wall",hall,new Vector3(0,fy+2,d/2),new Vector3(w,4,.25f),white);
        foreach(int side in new[]{-1,1})
        {
            var wall=Group("Sanctuary side",hall,new Vector3(side*w/2,0,0));wall.localRotation=Quaternion.Euler(0,-side*90,0);Wall(wall,"Temple side windows",Vector3.zero,d,4,fy,white,false);
            foreach(float z in new[]{-11f,-5.5f,0,5.5f,11f})
            {
                Cylinder("Lotus column",hall,new Vector3(side*9.3f,fy+2,z),.22f,4.0f,white,true);
                Cylinder("Gilded column base",hall,new Vector3(side*9.3f,fy+.18f,z),.32f,.30f,gold,false);
                Cylinder("Gilded capital",hall,new Vector3(side*9.3f,fy+3.85f,z),.32f,.30f,gold,false);
            }
        }
        for(int i=0;i<6;i++){float height=fy*(i+1)/6;Box("Temple stair "+i,hall,new Vector3(0,height/2,-d/2-.3f-(5-i)*.34f),new Vector3(5,height,.36f),concrete);}
        Box("Sanctuary ceiling",hall,new Vector3(0,fy+4.05f,0),new Vector3(w,.10f,d),wood);
        Roof("Lower tier - glazed red tile",hall,new Vector3(0,fy+4.15f,0),22,30,3.15f,roof,true);
        Roof("Middle tier",hall,new Vector3(0,fy+6.0f,0),16.5f,25,2.5f,roof,true);
        Roof("Upper tier",hall,new Vector3(0,fy+7.65f,0),11.5f,20,2.0f,roof,true);
        foreach(int end in new[]{-1,1})
        {
            Tube("Chofa - gilded roof finial",hall,new[]{new Vector3(0,fy+9.5f,end*10),new Vector3(0,fy+10.15f,end*10.1f),new Vector3(0,fy+10.9f,end*9.9f),new Vector3(0,fy+11.45f,end*9.55f)},.105f,gold);
            var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-9,fy+4.1f,end*13.05f),new Vector3(9,fy+4.1f,end*13.05f),new Vector3(0,fy+7.15f,end*13.05f)};mesh.triangles=end<0?new[]{0,2,1}:new[]{0,1,2};mesh.uv=new[]{Vector2.zero,new Vector2(9,0),new Vector2(4.5f,3)};mesh.RecalculateNormals();MeshObject("Ornamented gable",hall,Persist(mesh),wood,false);
            Tube("Gable gold border",hall,new[]{new Vector3(-9,fy+4.15f,end*13.1f),new Vector3(0,fy+7.22f,end*13.1f),new Vector3(9,fy+4.15f,end*13.1f)},.09f,gold);
            for(int i=-4;i<=4;i++)Tube("Gable decorative rays",hall,new[]{new Vector3(0,fy+4.7f,end*13.14f),new Vector3(i*1.65f,fy+4.25f+Mathf.Max(0,3-Mathf.Abs(i))*.45f,end*13.14f)},.035f,gold);
            var wheel=Cylinder("Dharma wheel relief",hall,new Vector3(0,fy+5.35f,end*13.18f),.62f,.08f,gold);wheel.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        Buddha(hall,new Vector3(0,fy,8.2f));
        for(int i=-1;i<=1;i++)
        {
            var l=Spot("Sanctuary warm light",hall,new Vector3(i*5,fy+3.8f,2),new Color(1,.72f,.38f),120,14,140);interiorLights.Add(l);
        }
        var entry=hall.gameObject.AddComponent<VillageEntrance>();entry.label="Temple ordination hall";entry.outside=new Vector3(0,.1f,-17);entry.inside=new Vector3(0,fy+.06f,-9);
        Lathe("Chedi - white and gold stupa",temple,new Vector3(-24,.08f,12),new[]{new Vector2(3.4f,0),new Vector2(3.4f,.4f),new Vector2(2.9f,.4f),new Vector2(2.9f,.85f),new Vector2(2.4f,.85f),new Vector2(2.4f,1.3f),new Vector2(1.9f,1.55f),new Vector2(2.1f,1.9f),new Vector2(1.85f,2.3f),new Vector2(1.6f,3.0f),new Vector2(1.25f,4.1f),new Vector2(.72f,4.6f),new Vector2(.85f,4.85f),new Vector2(.6f,5.15f),new Vector2(.65f,5.35f),new Vector2(.45f,5.55f),new Vector2(.5f,5.85f),new Vector2(.28f,6.2f),new Vector2(.3f,6.6f),new Vector2(.15f,7.3f),new Vector2(0,8.7f)},gold);
        Box("Chedi foundation",temple,new Vector3(-24,.2f,12),new Vector3(8,.4f,8),white);
        BellPavilion(temple,new Vector3(24,0,-15));
        foreach(var pos in new[]{new Vector3(-25,0,-24),new Vector3(25,0,27),new Vector3(-26,0,29),new Vector3(27,0,9)})
            VillageAssetLibrary.Place("tree_small_02",temple,pos,Random.Range(0,360),1.15f);
        for(int side=-1;side<=1;side+=2)for(int row=0;row<3;row++)
        {
            VillageAssetLibrary.Place("painted_wooden_bench",temple,new Vector3(side*24,.08f,-3+row*7),side*90);
            VillageAssetLibrary.Place("planter_pot_clay",temple,new Vector3(side*13.0f,.08f,-20+row*6),0,1.25f);
            VillageAssetLibrary.Place("shrub_01",temple,new Vector3(side*13,.75f,-20+row*6),row*70,.65f);
        }
        for(int side=-1;side<=1;side+=2)foreach(float z in new[]{-31f,-8f,22f})StreetLamp(temple,new Vector3(side*30,0,z),side*90);
    }
    static void Buddha(Transform parent,Vector3 position)
    {
        var b=Group("Seated Buddha - custom sanctuary sculpture",parent,position);
        Box("Altar base",b,new Vector3(0,.22f,0),new Vector3(4.2f,.44f,3.0f),white);
        Box("Altar upper step",b,new Vector3(0,.53f,0),new Vector3(3.3f,.2f,2.4f),gold);
        Lathe("Lotus pedestal",b,new Vector3(0,.63f,0),new[]{new Vector2(1.4f,0),new Vector2(1.45f,.14f),new Vector2(1.28f,.30f),new Vector2(1.2f,.42f)},gold);
        Sphere("Crossed robe legs",b,new Vector3(0,1.15f,-.1f),new Vector3(2.4f,.55f,1.4f),gold);
        Lathe("Meditation torso",b,new Vector3(0,1.2f,.05f),new[]{new Vector2(.65f,0),new Vector2(.62f,.25f),new Vector2(.48f,.85f),new Vector2(.69f,1.15f),new Vector2(.56f,1.3f),new Vector2(.2f,1.42f)},gold);
        Sphere("Buddha head",b,new Vector3(0,2.95f,.04f),new Vector3(.7f,.92f,.67f),gold);
        Lathe("Ushnisha",b,new Vector3(0,3.32f,.04f),new[]{new Vector2(.25f,0),new Vector2(.22f,.18f),new Vector2(.12f,.33f),new Vector2(0,.58f)},gold,24);
        foreach(int side in new[]{-1,1})
        {
            Sphere("Elongated ear",b,new Vector3(side*.36f,2.86f,.04f),new Vector3(.13f,.45f,.18f),gold);
            Tube("Meditation arm",b,new[]{new Vector3(side*.54f,2.4f,0),new Vector3(side*.78f,1.8f,-.10f),new Vector3(side*.48f,1.45f,-.65f),new Vector3(0,1.45f,-.7f)},.13f,gold);
        }
        // The figure faces the open entrance at local -Z.
        Sphere("Nose",b,new Vector3(0,2.94f,-.31f),new Vector3(.10f,.17f,.12f),gold);
        Tube("Serene eyes",b,new[]{new Vector3(-.23f,3.04f,-.26f),new Vector3(-.10f,3.01f,-.29f)},.013f,dark);
        Tube("Serene eyes",b,new[]{new Vector3(.10f,3.01f,-.29f),new Vector3(.23f,3.04f,-.26f)},.013f,dark);
    }
    static void BellPavilion(Transform parent,Vector3 position)
    {
        var b=Group("Bell pavilion",parent,position);
        Box("Bell pavilion platform",b,new Vector3(0,.15f,0),new Vector3(7,.3f,7),white);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Cylinder("Bell tower column",b,new Vector3(x*2.5f,2.4f,z*2.5f),.18f,4.5f,white,true);
        Roof("Bell pavilion roof",b,new Vector3(0,4.6f,0),8,8,2,roof,true);
        Lathe("Bronze bell",b,new Vector3(0,2.25f,0),new[]{new Vector2(.85f,0),new Vector2(.8f,.15f),new Vector2(.62f,.35f),new Vector2(.48f,.9f),new Vector2(.35f,1.3f),new Vector2(.12f,1.45f)},gold);
        Tube("Bell hanger",b,new[]{new Vector3(0,3.7f,0),new Vector3(0,4.6f,0)},.035f,dark);
    }
}
