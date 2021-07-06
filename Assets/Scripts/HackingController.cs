using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace TwoDesperadosTest
{
    public class HackingController
    {
        //TODO check if this is needed
        private List<NetworkNode> nodeList;

        NetworkNode startNode = null;

        //structures for tracking nodes
        //sets and maps for faster lookup
        private HashSet<NetworkNode> undiscoveredNodes;
        private HashSet<NetworkNode> discoveredNodes;
        private Dictionary<NetworkNode, NetworkNode> nodesToHack; //keeps a parent node as a Value to start the link animator from

        //hacking link animator
        private LinkAnimator linkAnimator; //TODO check if link animator needs to call hackingDetectedAction before, during or after hacking
        private const float LINK_LINE_OFFSET = 2f;

        //treasure Nodes counter
        private int treasureNodesToHack;

        //attack types
        public const string HACK = "Hack";
        public const string NUKE = "Nuke";
        public const string SET_TRAP = "Set Trap";

        //aresnal counters
        private int nukesCount;
        private int trapsCount;

        //xp counter
        private int xp;

        //Reward types
        public enum Reward
        {
            Nuke,
            Trap
        }

        //tracer speed decrease
        private int tracerSpeedDecrease;

        //trap delay
        private int trapDelay;

        //update UI actions
        private Action<Reward, int> updateRewardAction = null;
        private Action<int> updateXpAction = null;
        
        private Action<NetworkNode, string, string, string, int> attackChoiceAction = null;

        //functors to activate external behaviour
        private Action<int> spamNodeHackedAction = null;
        private Action<int> spamNodeNukedAction = null;//slow the tracer
        private Action hackingDetectedAction = null;
        private Action hackingCompletedAction = null;
        private Action<NetworkNode> firewallHackedActon = null;
        private Action<Vector2, Vector2, Color> drawNukedLinkAction;
        
        //random generator for reward
        System.Random randomGenerator;

        //protection aganist simultanious API calls
        private bool hackingInProgress;
        
        public Action<string> consoleLog = null;

        public HackingController(
            List<NetworkNode> nodes, 
            int treasureNodesToHack, 
            int xp, 
            int tracerSpeedDecrease,
            int trapDelay,
            LinkAnimator linkAnimator)
        {
            if (nodes.Count == 0)
                throw new ArgumentException("nodes map must not be empty");

            this.nodeList = nodes;

            if (treasureNodesToHack < 1)
                throw new ArgumentException("treasureNodesToHack must be > 0");

            this.treasureNodesToHack = treasureNodesToHack;

            this.linkAnimator = linkAnimator;

            nukesCount = 0;
            trapsCount = 0;
            this.xp = xp;
            this.tracerSpeedDecrease = tracerSpeedDecrease;
            this.trapDelay = trapDelay;
            
            discoveredNodes = new HashSet<NetworkNode>();
            undiscoveredNodes = new HashSet<NetworkNode>();
            nodesToHack = new Dictionary<NetworkNode, NetworkNode>();

            //find start node
            nodes.ForEach(node =>
            {
                if (node.GetNodeType().Equals(NetworkNode.Type.Start))
                {
                    startNode = node;
                    //discoveredNodes.Add(node); //TODO check  if neccessary
                }
                else
                {
                    undiscoveredNodes.Add(node);
                }
            });
            
            //get initial nodes to hack and set start node as  their parent
            startNode.GetNieghbourNodes().ForEach(neighbourNode => nodesToHack.Add(neighbourNode, startNode));

            linkAnimator.SetStartPoint(startNode.GetPosition());

            randomGenerator = new System.Random();

            hackingInProgress = false;

            //TODO debug
            //PrintNodeStructures();
        }
        
        //action setters
        public HackingController SetHackingDetectedAction(Action hackingDetectedAction)
        {
            this.hackingDetectedAction = hackingDetectedAction;
            return this;
        }

        public HackingController SetHackingCompletedAction(Action hackingCompletedAction)
        {
            this.hackingCompletedAction = hackingCompletedAction;
            return this;
        }

        public HackingController SetChooseAttackAction(Action<NetworkNode, string, string, string, int> chooseAttackAction)
        {
            this.attackChoiceAction = chooseAttackAction;
            return this;
        }

        public HackingController SetUpdateRewardAction(Action<Reward, int> updateRewardAction)
        {
            this.updateRewardAction = updateRewardAction;
            return this;
        }

        public HackingController SetUpdateXpAction(Action<int> updateXpAction)
        {
            this.updateXpAction = updateXpAction;
            return this;
        }

        public HackingController SetSpamNodeHackedAction(Action<int> spamNodeHackedAction)
        {
            this.spamNodeHackedAction = spamNodeHackedAction;
            return this;
        }

        public HackingController SetSpamNodeNukedAction(Action<int> spamNodeNukedAction)
        {
            this.spamNodeNukedAction = spamNodeNukedAction;
            return this;
        }
        
        public HackingController SetFirewallHackedAction(Action<NetworkNode> firewallHackedAction)
        {
            this.firewallHackedActon = firewallHackedAction;
            return this;
        }

        public HackingController SetDrawNukedLinkAction(Action<Vector2, Vector2, Color> drawNukedLinkAction)
        {
            this.drawNukedLinkAction = drawNukedLinkAction;
            return this;
        }

        //API methods
        public void SelectNode(NetworkNode node)
        {
            if (node.GetNodeType().Equals(NetworkNode.Type.Start))
            {
                if (consoleLog != null)
                    consoleLog(String.Format("Selected Start node. Difficulty: {0}. Keep it safe :)", node.GetHackingDifficulty()));
            }
            else if (discoveredNodes.Contains(node) && trapsCount > 0)
            {
                attackChoiceAction(node, string.Empty, string.Empty, trapsCount > 0 ? SET_TRAP : string.Empty, - 1);
            }
            else if (nodesToHack.ContainsKey(node))
            {
                attackChoiceAction(node, HACK, nukesCount > 0 ? NUKE : string.Empty, string.Empty, node.GetHackingDifficulty());
            }
        }

        public void HackNode(NetworkNode node)
        {
            if (hackingInProgress)
                return;
            
            NetworkNode parent = null;
            if (nodesToHack.ContainsKey(node) && nodesToHack.TryGetValue(node, out parent))
            {
                hackingInProgress = true;

                int hackingDifficulty = node.GetHackingDifficulty();

                if (consoleLog != null)
                    consoleLog(String.Format("Hacking {0} node...", node.GetNodeType()));

                Vector2 startPos = new Vector2(parent.GetPosition().x + LINK_LINE_OFFSET, parent.GetPosition().y + LINK_LINE_OFFSET);
                Vector2 endPos = new Vector2(node.GetPosition().x + LINK_LINE_OFFSET, node.GetPosition().y + LINK_LINE_OFFSET);

                linkAnimator
                    .SetStartPoint(startPos)
                    .SetEndPoint(endPos)
                    .SetLinkColor(Color.green)
                    .SetHackingDuration(node.GetHackingDifficulty() / NetworkNode.MINIMUM_HACKING_DIFFICULTY)
                    .Start(() =>
                    {

                        if (consoleLog != null)
                            consoleLog(String.Format("{0} node hacked!", node.GetNodeType().ToString()));

                        //move node to correspondning structure 
                        undiscoveredNodes.Remove(node);
                        discoveredNodes.Add(node);
                        nodesToHack.Remove(node);

                        if (node.GetNodeType().Equals(NetworkNode.Type.Treasure))
                        {
                            treasureNodesToHack--;

                            if (treasureNodesToHack == 0) //WIN GAME!
                            {
                                hackingCompletedAction();
                                return;
                            }
                            else //Get those sweet rewards
                            {
                                GenerateRewards();
                            }

                            CalculateHackingDetection(node);

                        }
                        else if (node.GetNodeType().Equals(NetworkNode.Type.Firewall))
                        {
                            node.NeutralizeFirewall();
                            firewallHackedActon(node);
                            CalculateHackingDetection(node);
                        }
                        else if (node.GetNodeType().Equals(NetworkNode.Type.Spam))
                        {
                            //shuffle undiscovered node difficulties and trigger tracer immediately
                            foreach (NetworkNode undiscoveredNode in undiscoveredNodes)
                                undiscoveredNode.SetHackingDifficulty(randomGenerator.Next(NetworkNode.MINIMUM_HACKING_DIFFICULTY, 101));
                            
                            foreach (KeyValuePair<NetworkNode, NetworkNode> nodeToHack in nodesToHack)
                                nodeToHack.Key.SetHackingDifficulty(randomGenerator.Next(NetworkNode.MINIMUM_HACKING_DIFFICULTY, 101));

                            spamNodeHackedAction(tracerSpeedDecrease);
                            hackingDetectedAction();
                        }
                        else if (node.GetNodeType().Equals(NetworkNode.Type.Data))
                        {
                            CalculateHackingDetection(node);
                        }

                        //add new nodes to hack
                        MarkNeighborsAsHackable(node);

                        //update xp
                        xp += node.GetHackingDifficulty();
                        updateXpAction(xp);

                        hackingInProgress = false;
                    });

            }
            else {
                if (consoleLog != null)
                    consoleLog("Node not available yet!");
            }
            
            //DEBUG only
            //PrintNodeStructures();
        }

        public void NukeNode(NetworkNode node)
        {
            if (hackingInProgress)
                return;

            NetworkNode parent = null;
            if (nukesCount > 0 && nodesToHack.ContainsKey(node) && nodesToHack.TryGetValue(node, out parent))
            {

                hackingInProgress = true;

                updateRewardAction(Reward.Nuke, --nukesCount);

                Vector2 startPos = new Vector2(parent.GetPosition().x + LINK_LINE_OFFSET, parent.GetPosition().y + LINK_LINE_OFFSET);
                Vector2 endPos = new Vector2(node.GetPosition().x + LINK_LINE_OFFSET, node.GetPosition().y + LINK_LINE_OFFSET);

                drawNukedLinkAction(startPos, endPos, Color.green);
                
                //move node to correspondning structure 
                undiscoveredNodes.Remove(node);
                discoveredNodes.Add(node);
                nodesToHack.Remove(node);

                if (node.GetNodeType().Equals(NetworkNode.Type.Treasure))
                {
                    treasureNodesToHack--;

                    if (treasureNodesToHack == 0) //WIN GAME!
                    {
                        hackingCompletedAction();
                        return;
                    }
                    else //Get those sweet rewards
                    {
                        GenerateRewards();
                    }

                }
                else if (node.GetNodeType().Equals(NetworkNode.Type.Firewall))
                {
                    node.NeutralizeFirewall();
                    firewallHackedActon(node);
                }
                else if (node.GetNodeType().Equals(NetworkNode.Type.Spam))
                {
                    //do not shuffle undiscovered node diff with nuke
                    spamNodeNukedAction(tracerSpeedDecrease);
                }

                //add new nodes to hack
                MarkNeighborsAsHackable(node);

                //update xp
                xp += node.GetHackingDifficulty();
                updateXpAction(xp);

                hackingInProgress = false;
            }
        }

        public void TrapNode(NetworkNode node)
        {
            if (trapsCount > 0 && discoveredNodes.Contains(node))
            {
                node.SetTracerDelay(trapDelay);
                updateRewardAction(Reward.Trap, --trapsCount);
            }
        }

        public void BlockSignal()
        {
            if (hackingInProgress && !linkAnimator.IsInterrupted())
                linkAnimator.Stop();
        }

        private void CalculateHackingDetection(NetworkNode node)
        {
            if ((randomGenerator.Next(0, 101) < (node.GetHackingDifficulty() / NetworkNode.MINIMUM_HACKING_DIFFICULTY)))
            {
                hackingDetectedAction();
            }
        }

        private void GenerateRewards()
        {
            switch (randomGenerator.Next(0, 2))
            {
                case 0:
                    updateRewardAction(Reward.Nuke, ++nukesCount);
                    consoleLog("Acquired a Nuke virus!");
                    break;
                case 1:
                    updateRewardAction(Reward.Trap, ++trapsCount);
                    consoleLog("Acquired a Trap root kit!");
                    break;
            }
        }

        private void MarkNeighborsAsHackable(NetworkNode node)
        {
            node.GetNieghbourNodes().ForEach(neighbour =>
            {
                if (!nodesToHack.ContainsKey(neighbour))
                    nodesToHack.Add(neighbour, node);
                else
                    nodesToHack[neighbour] = node;
            });
        }

    }
}
