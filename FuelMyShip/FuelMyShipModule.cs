using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Linq;

namespace FuelMyShip
{    
    class FuelMyShipModule : PartModule
    {        
        public static void Log(string message)
        {
            //Debug.Log("FuelMyShipLog -------------------- " + ":" + message);
        }

        private bool stationIsParent;
        private Part stationDockingPort = null;
        private double wiggleRoom = 0.004;
        private bool stopTransfer = false;
        private Coroutine activeCoroutine = null;
        private List<Part> stationTanks = new List<Part>();
        private List<Part> shipTanks = new List<Part>();

       
        public void Stop()
        {
            if(activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }
            stopTransfer = true;
            showButton("FillMeUpButtonClick");
            hideButton("StopTransfer");

        }

        [KSPEvent(guiName = "Stop Transfer", guiActive = false, active = false)]
        public void StopTransfer()
        {
            var allFuelMyShipModules = vessel.FindPartModulesImplementing<FuelMyShipModule>();
            allFuelMyShipModules.ForEach(fmm => fmm.Stop());
        }

        private void showButton(string eventName)
        {
            var fillButton = Events[eventName];
            fillButton.active = true;
            fillButton.guiActive = true;
        }

        private void hideButton(string eventName)
        {
            var stopButton = Events[eventName];
            stopButton.active = false;
            stopButton.guiActive = false;
        }

        [KSPEvent(guiName="Fill Me Up, Scotty", guiActive=true, active=true)]
        public void FillMeUpButtonClick()
        {
            //don't do multiple transfers all at once
            StopTransfer();
            
            stopTransfer = false;

            hideButton("FillMeUpButtonClick");
            showButton("StopTransfer");
            
            ////Figure out which direction the station is, ala parent or a child off the docking node            
            ClassifyStation();

            //Classify all the resource tanks
            ClassifyTanks();

            //Transfer the fuel
            activeCoroutine = StartCoroutine(TransferTheFuel());
        }

        private void ClassifyStation()
        {
            Log("ClassifyStation");
            ModuleDockingNode node = this.part.FindModuleImplementing<ModuleDockingNode>();

            var otherNode = node.FindOtherNode();

            if (otherNode == null)
            {
                otherNode = node.otherNode;
            }


            if (otherNode == null)
            {
                Log("Not Docked");
            }
            else
            {
                Log(otherNode.part.partName);
                stationDockingPort = otherNode.part;

                                //if this part's parent is othernode part then the station is the parent
                if (this.part.parent != null && this.part.parent == otherNode.part)
                {
                    stationIsParent = true;
                }
                
                //if the other part's parent is this part then the station is a child
                if (otherNode.part.parent != null && otherNode.part.parent == this.part)
                {
                    stationIsParent = false;
                }
            }            
        }

        private void ClassifyTanks()
        {
            Log("ClassifyTanks");
            if (stationDockingPort == null)
            {
                Log("No Station Docking Port");
                return;
            }
            if(stationIsParent)
            {
                shipTanks.AddRange(GetChildrenTanks(this.part));

                stationTanks.AddRange(GetParentTanks(stationDockingPort, this.part));
            }
            else//ship is parent
            {
                shipTanks.AddRange(GetParentTanks(this.part, stationDockingPort));

                stationTanks.AddRange(GetChildrenTanks(stationDockingPort));
            }                       
        }

        private void LogResourceTank(Part tank)
        {
            Log(tank.partName + "--------- ");
            foreach (var resource in tank.Resources)
            {
                Log(tank.partName + ": " + resource.resourceName + " : " + resource.amount);
            }
        }

        private IEnumerable<Part> GetParentTanks(Part part, Part previousPart)
        {
            var parentTanks = new List<Part>();
            if (part.Resources.Count > 0)
            {
                parentTanks.Add(part);
            }
            if (part.parent != null)
            {
                parentTanks.AddRange(GetParentTanks(part.parent, part));
            }
            foreach (var child in part.children)
            {
                if(child != previousPart)
                {
                    parentTanks.AddRange(GetChildrenTanks(child));
                }
            }
            return parentTanks;
        }
        

        private IEnumerable<Part> GetChildrenTanks(Part part)
        {
            var childrenTanks = new List<Part>();
            //if this part has resources add it to the list
            if(part.Resources.Count > 0)
            {
                childrenTanks.Add(part);
            }

            //add the tanks from the children
            foreach (var child in part.children)
            {
                childrenTanks.AddRange(GetChildrenTanks(child));
            }

            return childrenTanks;
        }

        private void LogPart(Part part)
        {
            Log("PartName: " + part.partName);
            Log("Name: " + part.name);
            Log("Resource Count: " + part.Resources.Count );
            
            foreach (var resource in part.Resources)
            {
                LogResource(resource);
            }
        }

        private void LogResource(PartResource resource)
        {
            var spacing = "     ";
            Log(spacing + "ResourceName: " + resource.resourceName);
            Log(spacing + spacing + "Resource Amount: " + resource.amount + " / " + resource.maxAmount);

            Log(spacing + spacing + "info?.resourceFlowMode: " + resource.info?.resourceFlowMode.ToString() );
            Log(spacing + spacing + "info?.resourceTransferMode: " + resource.info?.resourceTransferMode.ToString());

            Log(spacing + spacing + "GetInfo: " + resource.GetInfo());
        }

        private static double maxTransferSpeedPerSecond = 190;
        private IEnumerator TransferTheFuel()
        {
            // only get available station resources that are not empty and are not locked
            var stationResources = 
                    stationTanks.SelectMany(st => 
                        st.Resources.Where(r => r.flowState && r.info.resourceTransferMode != ResourceTransferMode.NONE && r.amount > 0));

            // only get ship resources that are not full
            var shipResources = 
                shipTanks.SelectMany(st => 
                    st.Resources.Where(r => r.flowState && r.info.resourceTransferMode != ResourceTransferMode.NONE && r.amount <= r.maxAmount - wiggleRoom));

            var resourceTypes = shipResources.Select(r => r.resourceName).Distinct().ToList();

            while (
                    !stopTransfer 
                    && shipResources.Any(r => r.amount < r.maxAmount - wiggleRoom
                    && stationResources.Any(sr => sr.resourceName == r.resourceName && sr.amount > 0))
                  )
            {                
                resourceTypes = shipResources
                    .Where(sr => sr.amount < sr.maxAmount - wiggleRoom)
                    .Select(r => r.resourceName).Distinct().ToList();

                //get the max amount to transfer
                double transferLimit = Time.deltaTime * maxTransferSpeedPerSecond;

                foreach (var resourceType in resourceTypes)
                {                    
                    //divide that by each ship resource that still not full
                    var shipResoucesStillNotFull = shipResources.Where(sr => sr.amount < sr.maxAmount 
                        && sr.resourceName == resourceType
                        && sr.flowState);

                    var transferAmountPerShipResource = transferLimit / shipResoucesStillNotFull.Count();

                    
                    //divide that by the number of station resources that are not empty
                    var stationResourcesThatAreNotEmpty = stationResources.Where(sr => sr.amount > 0 
                        && sr.resourceName == resourceType
                        && sr.flowState);

                    var transferAmountPerStationResource = transferAmountPerShipResource / stationResourcesThatAreNotEmpty.Count();
                    
                    foreach (var shipResource in shipResoucesStillNotFull)
                    {
                        foreach (var stationResource in stationResourcesThatAreNotEmpty)
                        {
                            var amountTransferred = TryToTransfer(stationResource, shipResource, transferAmountPerStationResource);
                        }
                    }
                }
                yield return null; 
            }
            StopTransfer();          
        }

        /// <summary>
        /// Tries to transfer the requested amount from the fromResource to the toResource
        /// </summary>
        /// <param name="fromResource">the resource to tranfer from</param>
        /// <param name="toResource">the resource to transfer to</param>
        /// <param name="amount">how much to transfer</param>
        /// <returns>the amount it was able to transfer</returns>
        private double TryToTransfer(PartResource fromResource, PartResource toResource, double amount)
        {
            
            var actualAmount = amount;

            //bandaid to prevent overfilling
            if (toResource.amount >= toResource.maxAmount)
            {
                actualAmount = 0;
            }

            //if there is not enough in the fromResource, just transfer all it has
            if (fromResource.amount < actualAmount)
            {
                actualAmount = fromResource.amount;
            }

            // if we are trying to transfer more than the toResource can handle then only take what it can take
            var toResourceAvailableRoom = toResource.maxAmount - toResource.amount;
            if (actualAmount > toResourceAvailableRoom)
            {
                actualAmount = toResourceAvailableRoom;
            }

            fromResource.amount -= actualAmount;
            toResource.amount += actualAmount;
            
            return actualAmount;
        }
    }
}
