
using UnityEngine;

namespace FuelMyShip
{
    //might want a vessel module?

    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class FuelMyShip : MonoBehaviour
    {

        public static void Log(string message)
        {
            Debug.Log("FuelMyShipLog -------------------- " + ":" + message);
        }
        
        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {

                for (var i = 0; i < FlightGlobals.Vessels.Count; i++)
                {
                    for (var j = 0; j < FlightGlobals.Vessels[i].parts.Count; j++)
                    {
                        Log("   ");
                        Log(FlightGlobals.Vessels[i].parts[j].name);
                        for (int k = 0; k < FlightGlobals.Vessels[i].parts[j].Modules.Count; k++)
                        {

                            if(FlightGlobals.Vessels[i].parts[j].Modules[k].moduleName == "ModuleDockingNode")
                            {

                                for (int l = 0; l < FlightGlobals.Vessels[i].parts[j].Modules.Count; l++)
                                {
                                    Log(FlightGlobals.Vessels[i].parts[j].Modules[l].moduleName);
                                }
                                FlightGlobals.Vessels[i].parts[j].AddModule("FuelMyShipModule", false);
                                
                                for (int l = 0; l < FlightGlobals.Vessels[i].parts[j].Modules.Count; l++)
                                {
                                    Log(FlightGlobals.Vessels[i].parts[j].Modules[l].moduleName);
                                }
                            }
                        }
                    }
                }

                

            }
        }
        
    }
}
