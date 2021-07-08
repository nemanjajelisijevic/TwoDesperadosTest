using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace TwoDesperadosTest
{
    public class NetworkConfigurator
    {

        public class ConfigInput
        {
            public int nodeCount { get; }
            public int treasureCount { get; }
            public int firewallCount { get; }
            public int spamCount { get; }
            public int spamDecrease { get; }
            public int trapDelay { get; }

            public ConfigInput(
                int nodeCount,
                int treasureCount,
                int firewallCount,
                int spamCount,
                int spamDecrease,
                int trapDelay)
            {
                this.nodeCount = nodeCount;
                this.treasureCount = treasureCount;
                this.firewallCount = firewallCount;
                this.spamCount = spamCount;
                this.spamDecrease = spamDecrease;
                this.trapDelay = trapDelay;
            }
        }

        private const int numberOfStartNodes = 1;

        private Dictionary<NetworkNode.Type, int> nodeTypeAmount;
        
        private ILinkGenerator linkGenerator;

        private System.Random randomNoGenerator;

        
        //disconnect dense links attribute
        public static float disconnectionLinksAngle = 12f;

        public class NetworkConfiguration
        {
            public List<NetworkNode> nodes {get;}
            public List<Link> links { get; set; }
            public NetworkNode startNode { get; set; }

            public List<NetworkNode> firewallNodes;
            public List<NetworkNode> treasureNodes;
            public List<NetworkNode> spamNodes;

            public NetworkConfiguration()
            {
                nodes = new List<NetworkNode>();
                firewallNodes = new List<NetworkNode>();
                treasureNodes = new List<NetworkNode>();
                spamNodes = new List<NetworkNode>();
            }
        }

        //to be called from settings
        public static void ValidateNodeTypeAmounts(int noOfNodes, int noOfTreasures, int noOfFirewalls, int noOfSpams)
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
        }

        public NetworkConfigurator(ConfigInput configInput, ILinkGenerator linkGenerator)
        {
            // validate params and throw errors
            ValidateNodeTypeAmounts(configInput.nodeCount, configInput.treasureCount, configInput.firewallCount, configInput.spamCount);

            nodeTypeAmount = new Dictionary<NetworkNode.Type, int>();

            nodeTypeAmount.Add(NetworkNode.Type.Data, configInput.nodeCount);
            nodeTypeAmount.Add(NetworkNode.Type.Start, numberOfStartNodes);
            nodeTypeAmount.Add(NetworkNode.Type.Treasure, configInput.treasureCount);
            nodeTypeAmount.Add(NetworkNode.Type.Firewall, configInput.firewallCount);
            nodeTypeAmount.Add(NetworkNode.Type.Spam, configInput.spamCount);

            

            randomNoGenerator = new System.Random();

            this.linkGenerator = linkGenerator;
        }
        
        public NetworkConfiguration ConfigureNetwork(float areaWidth, float areaHeight)
        {

            NetworkConfiguration ret = new NetworkConfiguration();

            ret.firewallNodes = new List<NetworkNode>();
            ret.treasureNodes = new List<NetworkNode>();
            ret.spamNodes = new List<NetworkNode>();

            int numberOfNodes = nodeTypeAmount[NetworkNode.Type.Data];

            float fieldWidth = areaWidth / numberOfNodes;
            float fieldHeight = areaHeight / numberOfNodes;

            //divide area into the matrix for discrete node positions
            Vector2[,] fieldCenterMatrix = new Vector2[numberOfNodes, numberOfNodes];
            bool[,] nodePresenceMatrix = new bool[numberOfNodes, numberOfNodes];

            for (int i = 0; i < numberOfNodes; ++i)
            {
                for (int j = 0; j < numberOfNodes; ++j)
                {
                    if (j % 2 == 0)
                        fieldCenterMatrix[j, i] = new Vector2((fieldWidth / 2) + (j * fieldWidth), (fieldHeight / 4) + (i * fieldHeight));
                    else
                        fieldCenterMatrix[j, i] = new Vector2((fieldWidth / 2) + (j * fieldWidth), (fieldHeight * 3 / 4) + (i * fieldHeight));

                    nodePresenceMatrix[j, i] = false;
                }
            }
            
            int cnt = numberOfNodes;

            //Fix for the triangulation problem when 3 nodes are in the same column or row
            int[] rowNodeCount = new int[numberOfNodes];
            int[] columnNodeCount = new int[numberOfNodes];

            while (cnt > 0)
            {
                
                int row = randomNoGenerator.Next(0, numberOfNodes);
                int column = randomNoGenerator.Next(0, numberOfNodes);
                
                // get random field repeatedly until a free field is found
                while (rowNodeCount[row] > 1 || columnNodeCount[column] >  1 || nodePresenceMatrix[row, column]) 
                {
                    row = randomNoGenerator.Next(0, numberOfNodes);
                    column = randomNoGenerator.Next(0, numberOfNodes);
                }

                rowNodeCount[row] += 1;
                columnNodeCount[column] += 1;

                int hackingDiff = randomNoGenerator.Next(NetworkNode.MINIMUM_HACKING_DIFFICULTY, 100);

                if (nodeTypeAmount[NetworkNode.Type.Start] > 0)//TODO refactor
                {
                    ret.startNode = new NetworkNode(fieldCenterMatrix[row, column], NetworkNode.Type.Start)
                        .SetHackingDifficulty(hackingDiff);
                    ret.nodes.Add(ret.startNode);
                    nodeTypeAmount[NetworkNode.Type.Start] = nodeTypeAmount[NetworkNode.Type.Start] - 1;
                }
                else if (nodeTypeAmount[NetworkNode.Type.Firewall] > 0)
                {
                    NetworkNode firewall = new NetworkNode(fieldCenterMatrix[row, column], NetworkNode.Type.Firewall)
                        .SetHackingDifficulty(hackingDiff);
                    ret.firewallNodes.Add(firewall);
                    ret.nodes.Add(firewall);
                    nodeTypeAmount[NetworkNode.Type.Firewall] = nodeTypeAmount[NetworkNode.Type.Firewall] - 1;
                }
                else if (nodeTypeAmount[NetworkNode.Type.Treasure] > 0)
                {
                    NetworkNode treasure = new NetworkNode(fieldCenterMatrix[row, column], NetworkNode.Type.Treasure)
                        .SetHackingDifficulty(hackingDiff);
                    ret.treasureNodes.Add(treasure);
                    ret.nodes.Add(treasure);
                    nodeTypeAmount[NetworkNode.Type.Treasure] = nodeTypeAmount[NetworkNode.Type.Treasure] - 1;
                }
                else if (nodeTypeAmount[NetworkNode.Type.Spam] > 0)
                {
                    NetworkNode spam = new NetworkNode(fieldCenterMatrix[row, column], NetworkNode.Type.Spam)
                        .SetHackingDifficulty(hackingDiff);
                    ret.spamNodes.Add(spam);
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
            ret.startNode.GetNieghbourNodes().ForEach(neighbor => {
                if (neighbor.GetNodeType().Equals(NetworkNode.Type.Firewall) && neighbor.GetNoOfLinks() > 1 && ret.startNode.GetNoOfLinks() > 1)
                {
                    Link startToFirewallLink = ret.startNode.GetLinkToNode(neighbor);
                    startToFirewallLink.Disconnect();
                    ret.links.Remove(startToFirewallLink);
                }
            });

            return ret;
        }

        private void DisconnectDenseLinks(List<Link> links)
        {
            //cut the edges that are too dense and belong to the same node
            List<Link> linksToDisconnect = new List<Link>();

            for (int k = 0; k < links.Count; ++k)
            {
                for (int i = 0; i < links.Count; ++i)
                {
                    if (k == i)
                        continue;

                    float angle = Link.GetAngleBetweenLinks(links[k], links[i]);

                    if ((links[k].IsConnectedToNode(links[i].GetNodes().Key) || (links[k].IsConnectedToNode(links[i].GetNodes().Value)))
                        && links[k].GetNodes().Key.GetNoOfLinks() > 1
                        && links[k].GetNodes().Value.GetNoOfLinks() > 1
                        && links[i].GetNodes().Key.GetNoOfLinks() > 1
                        && links[i].GetNodes().Value.GetNoOfLinks() > 1
                        && (angle < disconnectionLinksAngle))//|| angle > (360f - angleDiff)))
                    {
                        Link longer = links[k].GetLinkLength() >= links[i].GetLinkLength() ? links[k] : links[i];

                        if (!(longer.GetNodes().Key.GetNodeType().Equals(NetworkNode.Type.Start) && longer.GetNodes().Value.GetNodeType().Equals(NetworkNode.Type.Firewall))
                            && !(longer.GetNodes().Key.GetNodeType().Equals(NetworkNode.Type.Firewall) && longer.GetNodes().Value.GetNodeType().Equals(NetworkNode.Type.Start))
                            && !linksToDisconnect.Contains(longer))
                            linksToDisconnect.Add(longer);
                    }
                }

            }
            
            //disconnect links
            linksToDisconnect.ForEach(link => {
                link.Disconnect();
                links.Remove(link);
            });
            
        }

    }
}