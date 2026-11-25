using UnityEngine;

namespace Talingchan.Sandbox
{
    public class SandboxNightLighting : MonoBehaviour
    {
        public Light[] streetLights;
        public Light[] interiorLights;
        public Renderer[] streetBulbs;
        public bool streetLightsOn = true;
        public bool interiorLightsOn = true;
        void Awake() { SetStreetLights(streetLightsOn);SetInteriorLights(interiorLightsOn); }
        public void SetStreetLights(bool enabled)
        {
            streetLightsOn=enabled;
            if(streetLights!=null)foreach(var light in streetLights)if(light!=null)light.enabled=enabled;
            if(streetBulbs!=null)
            {
                var block=new MaterialPropertyBlock();block.SetColor("_EmissionColor",enabled?new Color(1,.68f,.32f)*4:Color.black);
                foreach(var bulb in streetBulbs)if(bulb!=null)bulb.SetPropertyBlock(block);
            }
        }
        public void SetInteriorLights(bool enabled)
        {
            interiorLightsOn=enabled;
            if(interiorLights!=null)foreach(var light in interiorLights)if(light!=null)light.enabled=enabled;
        }
    }
}
