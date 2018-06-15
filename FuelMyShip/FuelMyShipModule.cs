
using UnityEngine;
using System.Collections;
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
            //TransferTheFuel();
            StartCoroutine(TransferTheFuel());
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
        private bool ContainsTransferableResources(Part part)
        {
            var containsTransferableResources = false;

            if(part.Resources.Count == 0)
            {
                return false;
            }

            foreach (var resource in part.Resources)
            {
                LogResource(resource);
                if(resource.info.resourceTransferMode == ResourceTransferMode.PUMP)
                {

                }
            }

            return containsTransferableResources;
        }

        private IEnumerable<Part> GetChildrenTanks(Part part)
        {
            var childrenTanks = new List<Part>();
            //if this part has resources add it to the list
            if(part.Resources.Count > 0 /*&& ContainsTransferableResources(part)*/)
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
            //todo: flow at some rate
            double transferAmountExample = Time.deltaTime * maxTransferSpeedPerSecond;
            Log("TransferTheFuel");

            //for each ship tank, 
            //look at the resource value and for each resource 
            //top off the values from as many or all of the station tanks as needed
            Log("Transferring==========================================");
            

            foreach (var shipTank in shipTanks)//todo: only look at tanks that are not filled up
            {
                foreach (var shipResource in shipTank.Resources.Where(r => r.info.resourceTransferMode != ResourceTransferMode.NONE))//todo: only look at resources that are not filled up
                {                    
                    //if the resource is not filled up
                    if (shipResource.amount < shipResource.maxAmount)
                    {
                        var neededAmount = shipResource.maxAmount - shipResource.amount;
                        foreach (var stationTank in stationTanks)//todo: only look at tanks with the right resource
                        {
                            if (neededAmount <= 0)
                            {
                                yield return null;
                                break;
                            }
                            foreach (var stationResource in stationTank.Resources)
                            {
                                
                                Log("For each is on ");
                                LogResource(stationResource);

                                Log("we still need: " + neededAmount);
                                if (neededAmount <= 0)
                                {
                                    yield return null;
                                    break;
                                }
                                if (stationResource.resourceName == shipResource.resourceName)
                                {

                                    Log("Found station resouce " + stationResource.resourceName + " in amount " + stationResource.amount);

                                    double transferAmount = 0;
                                    //if we need more than the station tank has, give us everything
                                    if (neededAmount >= stationResource.amount)
                                    {
                                        Log("needed (" + neededAmount + ")is greater or equal to " + stationResource.amount);
                                        transferAmount = stationResource.amount;

                                    }
                                    else
                                    {
                                        Log("needed (" + neededAmount + ")is NOT greater or equal to " + stationResource.amount);
                                        transferAmount = neededAmount;
                                        
                                    }
                                    if(transferAmount == 0)
                                    {
                                        break;
                                    }
                                    Log("transfer amount: " + transferAmount);
                                    Log("stationResource.amount: " + transferAmount);
                                    Log("shipResource.amount: " + transferAmount);
                                    Log("moving");
                                    shipResource.amount += transferAmount;

                                    Log("stationResource.amount: " + transferAmount);
                                    Log("shipResource.amount: " + transferAmount);
                                    Log("Update station tank");
                                    stationResource.amount -= transferAmount;

                                    Log("stationResource.amount: " + transferAmount);
                                    Log("shipResource.amount: " + transferAmount);
                                    Log("update needed amount");
                                    neededAmount -= transferAmount;

                                    Log("needed amount: " + neededAmount);
                                }
                                yield return null;
                            }
                            yield return null;
                        }
                    }
                    yield return null;
                }
                yield return null;
            }
        }
    }
}
