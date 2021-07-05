using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace TwoDesperadosTest
{
    public class NetworkConfigurator
    {

        private const int numberOfStartNodes = 1;

        private Dictionary<NetworkNode.Type, int> nodeTypeAmount;
        
        private ILinkGenerator linkGenerator;

        private System.Random randomNoGenerator;

        public NetworkNode startNode = null;
        public List<NetworkNode> firewallNodes;
        public List<NetworkNode> treasureNodes;
        public List<NetworkNode> spamNodes;

        public int[,] gridGraphRepresentation;//Neded for tracer pathfindiing

        public class NetworkConfiguration
        {
            public List<NetworkNode> nodes {get;}
            public List<Link> links { get; set; }

            public NetworkConfiguration()
            {
                nodes = new List<NetworkNode>();
            }
        }

        public NetworkConfigurator(int noOfNodes, int noOfTreasures, int noOfFirewalls, int noOfSpams)
        {
            // validate params and throw errors
            if (noOfNodes < 5) 
                throw new ArgumentException("Invalid number of nodes. Min 5");

            if (noOfTreasures < 1 || noOfTreasures > (noOfNodes - 4))
                throw new ArgumentException(String.Format("Invalid number of treasure nodes. Min 1, max {0}", noOfNodes - 4));
            
            if (noOfFirewalls < 1 || noOfFirewalls > (noOfNodes - noOfTreasures - numberOfStartNodes - 2))
                throw new ArgumentException(String.Format("Invalid number of firewall nodes. Min 1, max {0}", noOfNodes - noOfTreasures - numberOfStartNodes - 2));

            if (noOfSpams < 1 || noOfSpams > (noOfNodes - numberOfStartNodes - noOfTreasures - noOfFirewalls - 1))
                throw new ArgumentException(String.Format("Invalid number of spam nodes. Min 1, max {0}", noOfNodes - numberOfStartNodes - noOfTreasures - noOfFirewalls - 1));

            nodeTypeAmount = new Dictionary<NetworkNode.Type, int>();

            nodeTypeAmount.Add(NetworkNode.Type.Data, noOfNodes);
            nodeTypeAmount.Add(NetworkNode.Type.Start, numberOfStartNodes);
            nodeTypeAmount.Add(NetworkNode.Type.Treasure, noOfTreasures);
            nodeTypeAmount.Add(NetworkNode.Type.Firewall, noOfFirewalls);
            nodeTypeAmount.Add(NetworkNode.Type.Spam, noOfSpams);

            firewallNodes = new List<NetworkNode>();
            treasureNodes = new List<NetworkNode>();
            spamNodes = new List<NetworkNode>();

            randomNoGenerator = new System.Random();

            linkGenerator = new IncrementalTriangulationLinkGenerator();
        }
        
        public NetworkConfiguration ConfigureNetwork(float areaWidth, float areaHeight)
        {

            NetworkConfiguration ret = new NetworkConfiguration();

            int numberOfNodes = nodeTypeAmount[NetworkNode.Type.Data];

            float fieldWidth = areaWidth / numberOfNodes;
            float fieldHeight = areaHeight / numberOfNodes;

            //divide area into the matrix for discrete node positions
            Vector2[,] fieldCenterMatrix = new Vector2[numberOfNodes, numberOfNodes];
            gridGraphRepresentation = new int[numberOfNodes, numberOfNodes];

            for (int i = 0; i < numberOfNodes; ++i)
            {
                for (int j = 0; j < numberOfNodes; ++j)
                {
                    if (j % 2 == 0)
                        fieldCenterMatrix[j, i] = new Vector2((fieldWidth / 2) + (j * fieldWidth), (fieldHeight / 4) + (i * fieldHeight));
                    else
                        fieldCenterMatrix[j, i] = new Vector2((fieldWidth / 2) + (j * fieldWidth), (fieldHeight * 3 / 4) + (i * fieldHeight));
                }
            }

            bool[,] nodePresenceMatrix = new bool[numberOfNodes, numberOfNodes];

            for (int i = 0; i < numberOfNodes; ++i)
            {
                for (int j = 0; j < numberOfNodes; ++j)
                {
                    nodePresenceMatrix[j, i] = false;
                }
            }

            //TODO first 3 nodes should be in different rows and columns to fix the 

            int cnt = numberOfNodes;

            int startNodeRow = -1;
            int startNodeColumn = -1;
            
            while (cnt > 0)
            {

                
                int row = randomNoGenerator.Next(0, numberOfNodes);
                int column = randomNoGenerator.Next(0, numberOfNodes);

                // get random field repeatedly until a free field is found
                while (nodePresenceMatrix[row, column])
                {
                    row = randomNoGenerator.Next(0, numberOfNodes);
                    column = randomNoGenerator.Next(0, numberOfNodes);
                }

                if (cnt == numberOfNodes - 1 && row == startNodeRow && column == startNodeColumn) //FIX FOR TRIANGULATION PROBLEM
                {
                    if (row == numberOfNodes - 1)
                        row--;
                    else
                        row++;

                    if (column == numberOfNodes - 1)
                        column--;
                    else
                        column++;
                }

                int hackingDiff = randomNoGenerator.Next(NetworkNode.MINIMUM_HACKING_DIFFICULTY, 100);
                gridGraphRepresentation[row, column] = hackingDiff;

                if (nodeTypeAmount[NetworkNode.Type.Start] > 0)//TODO refactor
                {
                    startNodeRow = row;
                    startNodeColumn = column;

                    startNode = new NetworkNode(fieldCenterMatrix[row, column], NetworkNode.Type.Start)
                        .SetHackingDifficulty(hackingDiff);
                    ret.nodes.Add(startNode);
                    nodeTypeAmount[NetworkNode.Type.Start] = nodeTypeAmount[NetworkNode.Type.Start] - 1;
                }
                else if (nodeTypeAmount[NetworkNode.Type.Firewall] > 0)
                {
                    NetworkNode firewall = new NetworkNode(fieldCenterMatrix[row, column], NetworkNode.Type.Firewall)
                        .SetHackingDifficulty(hackingDiff);
                    firewallNodes.Add(firewall);
                    ret.nodes.Add(firewall);
                    nodeTypeAmount[NetworkNode.Type.Firewall] = nodeTypeAmount[NetworkNode.Type.Firewall] - 1;
                    //TODO delete link between adjecant start and firewall
                }
                else if (nodeTypeAmount[NetworkNode.Type.Treasure] > 0)
                {
                    NetworkNode treasure = new NetworkNode(fieldCenterMatrix[row, column], NetworkNode.Type.Treasure)
                        .SetHackingDifficulty(hackingDiff);
                    treasureNodes.Add(treasure);
                    ret.nodes.Add(treasure);
                    nodeTypeAmount[NetworkNode.Type.Treasure] = nodeTypeAmount[NetworkNode.Type.Treasure] - 1;
                }
                else if (nodeTypeAmount[NetworkNode.Type.Spam] > 0)
                {
                    NetworkNode spam = new NetworkNode(fieldCenterMatrix[row, column], NetworkNode.Type.Spam)
                        .SetHackingDifficulty(hackingDiff);
                    spamNodes.Add(spam);
                    ret.nodes.Add(spam);
                    nodeTypeAmount[NetworkNode.Type.Spam] = nodeTypeAmount[NetworkNode.Type.Spam] - 1;
                }
                else if (nodeTypeAmount[NetworkNode.Type.Data] > 0)
                {
                    ret.nodes.Add(new NetworkNode(fieldCenterMatrix[row, column], NetworkNode.Type.Data)
                        .SetHackingDifficulty(hackingDiff));
                    nodeTypeAmount[NetworkNode.Type.Data] = nodeTypeAmount[NetworkNode.Type.Data] - 1;
                }

                nodePresenceMatrix[row, column] = true;
                cnt--;
            };
            
            ret.links = linkGenerator.GenerateLinks(ret.nodes);
            
            DisconnectDenseLinks(ret.links);

            //disconnect firewall node if next to start node
            startNode.GetNieghbourNodes().ForEach(neighbor => {
                if (neighbor.GetNodeType().Equals(NetworkNode.Type.Firewall) && neighbor.GetNoOfLinks() > 1 && startNode.GetNoOfLinks() > 1)
                {
                    Link startToFirewallLink = startNode.GetLinkToNode(neighbor);
                    startToFirewallLink.Disconnect();
                    ret.links.Remove(startToFirewallLink);
                }
            });

            //Debug.LogFormat("Nodes end count: {0}", ret.nodes.Count);
            //Debug.LogFormat("Edges end count: {0}", ret.links.Count);

            return ret;
        }

        private void DisconnectDenseLinks(List<Link> links)
        {
            //Debug.Log(String.Format("Initial no of links: {0}", links.Count));
            
            //cut the edges that are too dense and belong to the same node
            List<Link> linksToDisconnect = new List<Link>();

            for (int k = 0; k < links.Count; ++k)
            {
                for (int i = 0; i < links.Count; ++i)
                {
                    if (k == i)
                        continue;

                    float angle = Link.GetAngleBetweenLinks(links[k], links[i]);
                    float angleDiff = 5f;

                    if ((links[k].IsConnectedToNode(links[i].GetNodes().Key) || (links[k].IsConnectedToNode(links[i].GetNodes().Value)))
                        && links[i].GetNodes().Key.GetNoOfLinks() > 1
                        && links[i].GetNodes().Value.GetNoOfLinks() > 1
                        && (angle < angleDiff))//|| angle > (360f - angleDiff)))
                    {
                        Link longer = links[k].GetLinkLength() >= links[i].GetLinkLength() ? links[k] : links[i];

                        if (!(longer.GetNodes().Key.GetNodeType().Equals(NetworkNode.Type.Start) && longer.GetNodes().Value.GetNodeType().Equals(NetworkNode.Type.Firewall))
                            && !(longer.GetNodes().Key.GetNodeType().Equals(NetworkNode.Type.Firewall) && longer.GetNodes().Value.GetNodeType().Equals(NetworkNode.Type.Start))
                            && !linksToDisconnect.Contains(longer))
                            linksToDisconnect.Add(longer);
                    }
                }

            }

            //Debug.LogFormat("Links to disconnect: {0}", linksToDisconnect.Count);

            linksToDisconnect.ForEach(link => {

                //Debug.Log(String.Format("Link To Remove - Node1(x:{0}, y:{1}) - Node2(x:{2}, y:{3})",
                //    link.GetNodes().Key.GetPosition().x,
                //    link.GetNodes().Key.GetPosition().y,
                //    link.GetNodes().Value.GetPosition().x,
                //    link.GetNodes().Value.GetPosition().y));

                link.Disconnect();
                links.Remove(link);
                
            });
            
        }

    }
}