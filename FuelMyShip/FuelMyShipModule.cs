
using UnityEngine;

namespace FuelMyShip
{
    class FuelMyShipModule : PartModule
    {
        [KSPEvent(guiName = "Fill Me Up Scotty", guiActive = true)]
        public void DoNothing()
        {
            //FuelMyShip.Log("Full!");
        }
    }
}
