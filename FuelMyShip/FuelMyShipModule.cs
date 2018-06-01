
using UnityEngine;
using System.Collections.Generic;

using System;
using System.Linq;

namespace FuelMyShip
{
    
    class FuelMyShipModule : PartModule
    {
        
        public static void Log(string message)
        {
            Debug.Log("FuelMyShipLog -------------------- " + ":" + message);
        }

        private bool stationIsParent;
        private Part stationDockingPort = null;
        private List<Part> stationTanks = new List<Part>();
        private List<Part> shipTanks = new List<Part>();

        [KSPEvent(guiName="Fill Me Up, Scotty", guiActive=true, active=true)]
        public void FillMeUpButtonClick()
        {
            ////Figure out which direction the station is, ala parent or a child off the docking node            
            ClassifyStation();

            //Classify all the resource tanks
            ClassifyTanks();

            //Transfer the fuel
            TransferTheFuel();
        }

        private void ClassifyStation()
        {
            Log("ClassifyStation");
            ModuleDockingNode node = this.part.FindModuleImplementing<ModuleDockingNode>();

            var otherNode = node.FindOtherNode();
            //if (otherNode != null)
            //{
            //    Log("node.FindOtherNode");             
            //}

            if (otherNode == null)
            {
                otherNode = node.otherNode;
                //Log("node.otherNode");

            }


            if (otherNode == null)
            {
                Log("Not Docked");
            }
            else
            {
                Log(otherNode.part.partName);
                //Log("here 2");
                stationDockingPort = otherNode.part;


               // Log("hypothesis this.part.parent is null yes? " + (this.part.parent == null).ToString());

                //if this part's parent is othernode part then the station is the parent
                if (this.part.parent != null && this.part.parent == otherNode.part)
                {
                    stationIsParent = true;
                }

                //Log("here 3");
                //if the other part's parent is this part then the station is a child
                if (otherNode.part.parent != null && otherNode.part.parent == this.part)
                {
                    stationIsParent = false;
                }
                //Log("here 4");
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

                stationTanks.AddRange(GetChildrenTanks(stationDockingPort));//todo: need the station's docking port, the part this dock is docked too
            }

            //Log(" ");
            //Log("ShipTanks: ");
            //foreach (var tank in shipTanks)
            //{
            //    LogResourceTank(tank);
            //}


            //Log(" ");
            //Log("StationTanks: ");
            //foreach (var tank in stationTanks)
            //{
            //    LogResourceTank(tank);
            //}
            
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
        }

        private void TransferTheFuel()
        {
            Log("TransferTheFuel");

            //for each ship tank, 
            //look at the resource value and for each resource 
            //top off the values from as many or all of the station tanks as needed
            Log("Transferring==========================================");

            //////find a test resource that need at least 10 units
            ////var shipTank = shipTanks.FirstOrDefault(st => st.Resources.Any(str => str.amount < str.maxAmount - 10));
            ////var shipResource = shipTank.Resources.FirstOrDefault(str => str.amount < str.maxAmount - 10);
            ////Log("attempting to fill:");
            ////LogPart(shipTank);
            ////LogResource(shipResource);




            ////var stationTank = stationTanks.FirstOrDefault(st => 
            ////    st.Resources.Any(str => 
            ////        str.resourceName == shipResource.resourceName
            ////        && str.amount >= shipResource.maxAmount - shipResource.amount                
            ////    ));

            ////var stationResource = stationTank.Resources.FirstOrDefault(str =>
            ////        str.resourceName == shipResource.resourceName
            ////        && str.amount >= (shipResource.maxAmount - shipResource.amount)
            ////    );
            ////Log("using the following");
            ////LogPart(stationTank);
            ////LogResource(stationResource);

            ////Log("attempt");

            //////stationTank.TransferResource(stationResource, shipResource.maxAmount - shipResource.amount, shipTank);
            ////stationResource.amount -= shipResource.maxAmount - shipResource.amount;
            ////shipResource.amount += shipResource.maxAmount - shipResource.amount;

            ////Log("result");
            ////LogResource(shipResource);

            foreach (var shipTank in shipTanks)//todo: only look at tanks that are not filled up
            {
                foreach (var shipResource in shipTank.Resources)//todo: only look at resources that are not filled up
                {
                    //if the resource is not filled up
                    if (shipResource.amount < shipResource.maxAmount)
                    {
                        var neededAmount = shipResource.maxAmount - shipResource.amount;
                        foreach (var stationTank in stationTanks)//todo: only look at tanks with the right resource
                        {
                            if (neededAmount == 0)
                            {
                                break;
                            }
                            foreach (var stationResource in stationTank.Resources)
                            {
                                if (neededAmount == 0)
                                {
                                    break;
                                }
                                if (stationResource.resourceName == shipResource.resourceName)
                                {
                                    //if we need more than the station tank has, give us everything
                                    if(neededAmount >= stationResource.amount)
                                    {
                                        shipResource.amount += stationResource.amount;
                                        stationResource.amount -= stationResource.amount;
                                        neededAmount -= stationResource.amount;
                                    }
                                    else
                                    {
                                        shipResource.amount += neededAmount;
                                        stationResource.amount -= neededAmount;
                                        neededAmount -= neededAmount;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
