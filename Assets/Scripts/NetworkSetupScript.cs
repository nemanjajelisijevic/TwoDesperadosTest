using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;


namespace TwoDesperadosTest
{

    public class NetworkSetupScript : MonoBehaviour
    {

        [SerializeField]
        private Sprite nodeSprite;
        [SerializeField]
        public Text XP_ui;
        [SerializeField]
        public Text Nuke_ui;
        [SerializeField]
        public Text Trap_ui;
        [SerializeField]
        public Text Console;

        private Action<String, Color> consolePrinter = null;
        
        private RectTransform graphContainer;

        private NetworkConfigurator networkConfigurator;
        private HackingController hackingController;
        private IPathFinder pathFinder;

        private Dictionary<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerWithPathMap;

        private Dictionary<NetworkNode, GameObject> nodeToButtonMap;

        public GameObject actionPanel;

        private string xpUiTemplate = "var xp = {0};";
        private string nukesUiTemplate = "var nukes = {0};";
        private string trapsUiTemplate = "var traps = {0};";

        private void Awake()
        {            
            int noOfNodes = 20;
            int noOfTreasureNodes = 4;
            int noOfFirewallNodes = 3;
            int noOfSpamNodes = 2;

            int xp = 0;
            int tracerSpeedDecrease = 50;
            int trapDelay = 10;

            float padding = 10f;
            
            graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();

            consolePrinter = (consoleMessage, color) => { Console.text = "root@hacker.org:~# " + consoleMessage; Console.color = color; };

            nodeToButtonMap = new Dictionary<NetworkNode, GameObject>();

            try
            {
                networkConfigurator = new NetworkConfigurator(noOfNodes, noOfTreasureNodes, noOfFirewallNodes, noOfSpamNodes, new IncrementalTriangulationLinkGenerator());
                
                NetworkConfigurator.NetworkConfiguration networkConf 
                    = networkConfigurator.ConfigureNetwork(graphContainer.sizeDelta.x - padding, graphContainer.sizeDelta.y - padding);

                networkConf.links.ForEach(edge => DrawLink(edge));
                networkConf.nodes.ForEach(node => {
                    GameObject nodeButton = DrawNode(node);
                    nodeToButtonMap.Add(node, nodeButton);
                });
                
                //init controllers
                hackingController = new HackingController(
                    networkConf.nodes, 
                    networkConfigurator.treasureNodes.Count, 
                    xp, 
                    tracerSpeedDecrease, 
                    trapDelay, 
                    new LinkAnimator(graphContainer, this));
                
                tracerWithPathMap = new Dictionary<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>>();
                
                pathFinder = new DijkstraPathFinder();

                //configure tracer controllers
                networkConfigurator.firewallNodes.ForEach(firewallNode =>
                {

                    List<NetworkNode> cheapestPathToStart = pathFinder.FindShortestPath(firewallNode, networkConfigurator.startNode);
                    
                    TracerController tracer = new TracerController(new LinkAnimator(graphContainer, this), new TimeoutWaiter(this))
                        .SetTraceCompletedAction(() => {
                            consolePrinter("You've been traced! Police called.", Color.red);
                            hackingController.BlockSignal();
                            foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in tracerWithPathMap)
                                tracerPair.Value.Key.BlockTracer();
                        });

                    tracerWithPathMap.Add(
                        cheapestPathToStart[0], //firewall node
                        new KeyValuePair<TracerController, List<NetworkNode>>(tracer, cheapestPathToStart)
                        );
                });

                //configure hacking controller
                hackingController
                    .SetHackingCompletedAction(() => {

                        consolePrinter("Wow! Nice skills. Do you want a job?", Color.green);
                        foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in tracerWithPathMap)
                            tracerPair.Value.Key.BlockTracer();
                    })
                    .SetDrawLinkAction((start, end, color) => DrawLink(start, end, color))
                    .SetHackingDetectedAction(() => {

                        consolePrinter("Your IP address has been compromised! Hurry up!", Color.red);

                        foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in tracerWithPathMap)
                            tracerPair.Value.Key.TraceHackingSignal(tracerPair.Value.Value);

                    })
                    .SetFirewallHackedAction(firewallNode => {
                        consolePrinter("Firewall system hacked. Good job!", Color.green);
                        tracerWithPathMap[firewallNode].Key.BlockTracer();
                    })
                    .SetUpdateRewardAction((reward, count) => {

                        if (reward.Equals(HackingController.Reward.Nuke))
                        {
                            Nuke_ui.text = String.Format(nukesUiTemplate, count);
                            consolePrinter("Acquired a Nuke virus!", Color.green);
                        }
                        else if (reward.Equals(HackingController.Reward.Trap))
                        {
                            consolePrinter("Acquired a Trap root kit!", Color.green);
                            Trap_ui.text = String.Format(trapsUiTemplate, count); ;
                        }

                    })
                    .SetUpdateXpAction(Xp => XP_ui.text = (String.Format(xpUiTemplate, Xp)))
                    .SetSpamNodeHackedAction(decreaseTracerSpeedPercent => { 

                        consolePrinter("Spam node hacked! Recalculating Node hacking difficulties...", Color.green);


                        //recalculate cheapest paths from firewalls
                        networkConfigurator.firewallNodes.ForEach(firewallNode =>
                        {
                            List<NetworkNode> cheapestPathToStart = pathFinder.FindShortestPath(firewallNode, networkConfigurator.startNode);

                            tracerWithPathMap[firewallNode].Value.Clear();
                            cheapestPathToStart.ForEach(node => tracerWithPathMap[firewallNode].Value.Add(node));
                        });


                        foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in tracerWithPathMap)
                        {
                            tracerPair.Value.Key.DecreaseTracerSpeed(decreaseTracerSpeedPercent);
                            tracerPair.Value.Key.SetTracePath(tracerPair.Value.Value);
                        }

                        consolePrinter("Node hacking difficulties recalculated!", Color.green);

                    })
                    .SetChooseAttackAction((node, hackText, nukeText, trapText, difficulty) => {

                        actionPanel.transform.SetAsLastSibling();
                        actionPanel.SetActive(true);    
                        actionPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(node.GetPosition().x + 35, node.GetPosition().y);
                        Button[] buttons = actionPanel.GetComponentsInChildren<Button>();
                        
                        foreach (Button butt in buttons)//TODO fix
                        {
                            if (butt.name == "Difficulty")
                            {
                                butt.GetComponentInChildren<Text>().text = "Difficulty: " + (difficulty > -1 ? difficulty.ToString() : "HACKED");
                            }
                            else if (butt.name == "Hack")
                            {
                                if (!hackText.Equals(string.Empty))
                                {
                                    butt.enabled = true;
                                    butt.onClick.AddListener(() => { hackingController.HackNode(node); actionPanel.SetActive(false); });
                                }
                            } 
                            else if (butt.name == "Nuke")
                            {
                                if (!nukeText.Equals(string.Empty))
                                    butt.onClick.AddListener(() => { hackingController.NukeNode(node); actionPanel.SetActive(false);});
                            }
                            else if (butt.name == "Trap")
                            {
                                if (!trapText.Equals(string.Empty))
                                {
                                    butt.onClick.AddListener(() => {
                                        nodeToButtonMap[node].GetComponent<Image>().color = Color.magenta;
                                        consolePrinter("Trap kit set!", Color.green);
                                        hackingController.TrapNode(node);
                                        actionPanel.SetActive(false);
                                    });
                                }
                            }
                            else if (butt.name == "Close")
                            {
                                butt.onClick.AddListener(() => { actionPanel.SetActive(false); });
                            }
                        }
                    });

                //set controller loggers
                hackingController.consoleLog = message => consolePrinter(message, Color.green);
                foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> entry in tracerWithPathMap)
                    entry.Value.Key.consoleLog = message => consolePrinter(message, Color.red);
                
            }
            catch (Exception e)
            {
                Debug.Log("Error configuring network: " + e.Message + e.StackTrace);
                consolePrinter("Init error...", Color.red);
            }
        }


        //UI draw functions
        private GameObject DrawNode(NetworkNode node)
        {
            GameObject nodeObject = new GameObject("node", typeof(Button));
            nodeObject.transform.SetParent(graphContainer, false);
            nodeObject.AddComponent<Image>();
            nodeObject.GetComponent<Image>().sprite = nodeSprite;
            
            switch (node.GetNodeType())
            {
                case NetworkNode.Type.Start: 
                    nodeObject.GetComponent<Image>().color = Color.green;
                    break;
                case NetworkNode.Type.Firewall:
                    nodeObject.GetComponent<Image>().color = Color.red;
                    break;
                case NetworkNode.Type.Treasure:
                    nodeObject.GetComponent<Image>().color = Color.yellow;
                    break;
                case NetworkNode.Type.Spam:
                    nodeObject.GetComponent<Image>().color = Color.grey;
                    break;
                case NetworkNode.Type.Data:
                    nodeObject.GetComponent<Image>().color = Color.white;
                    break;
            }

            RectTransform nodeTransform = nodeObject.GetComponent<RectTransform>();
            nodeTransform.anchoredPosition = node.GetPosition();
            nodeTransform.sizeDelta = new Vector2(20, 20);
            nodeTransform.anchorMin = new Vector2(0, 0);
            nodeTransform.anchorMax = new Vector2(0, 0);
            nodeTransform.SetAsLastSibling();

            nodeObject.GetComponent<Button>().onClick.AddListener(() => {

                Button[] buttons = actionPanel.GetComponentsInChildren<Button>();

                foreach (Button butt in buttons)
                    butt.onClick.RemoveAllListeners();

                hackingController.SelectNode(node);
            });
           
            return nodeObject;
        }

        private void DrawLink(Link link)
        {
            DrawLink(link.GetNodes().Key.GetPosition(), link.GetNodes().Value.GetPosition(), new Color(1, 1, 1, 0.5f));
        }

        private void DrawLink(Vector2 dotPosA, Vector2 dotPosB, Color color)
        {
            GameObject edgeObject = new GameObject("link", typeof(Image));
            edgeObject.transform.SetParent(graphContainer, false);
            edgeObject.GetComponent<Image>().color = color;
            RectTransform edgeTransform = edgeObject.GetComponent<RectTransform>();

            Vector2 dir = (dotPosB - dotPosA).normalized;
            float distance = Vector2.Distance(dotPosA, dotPosB);

            edgeTransform.anchorMin = new Vector2(0, 0);
            edgeTransform.anchorMax = new Vector2(0, 0);
            edgeTransform.sizeDelta = new Vector2(distance, 3f);
            edgeTransform.anchoredPosition = dotPosA + dir * distance * 0.5f;

            edgeTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }
        
    }
}