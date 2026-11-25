using System;
using UnityEngine;

namespace Talingchan.Village
{
    public class VillageLightingController : MonoBehaviour
    {
        public Transform player;
        public Light[] localLights;
        public float visibleDistance = 85;
        public float shadowDistance = 28;
        public int shadowBudget = 6;
        float nextUpdate;
        void Start(){Refresh();}
        void Update(){if(Time.unscaledTime>=nextUpdate){nextUpdate=Time.unscaledTime+.3f;Refresh();}}
        public void Refresh()
        {
            if(player==null||localLights==null)return;
            Array.Sort(localLights,(a,b)=>Distance(a).CompareTo(Distance(b)));
            int shadows=0;
            foreach(var light in localLights)
            {
                if(light==null)continue;
                float distance=Distance(light);
                light.enabled=distance<visibleDistance*visibleDistance;
                bool shadow=light.enabled&&distance<shadowDistance*shadowDistance&&shadows<shadowBudget;
                light.shadows=shadow?LightShadows.Soft:LightShadows.None;
                if(shadow)shadows++;
            }
        }
        float Distance(Light light){return light==null?float.MaxValue:(light.transform.position-player.position).sqrMagnitude;}
    }
}
