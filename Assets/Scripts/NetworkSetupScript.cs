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
        [SerializeField]
        public Text ConsoleBeforelast;
        [SerializeField]
        public Text ConsoleTwoBeforeLast;

        private Action<String, Color> consolePrinter = null;
        
        private RectTransform graphContainer;

        private NetworkConfigurator networkConfigurator;
        private HackingController hackingController;
        private IPathFinder pathFinder;

        //map firewall nodes to their corresponding TracerController and cheapest path to start
        private Dictionary<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> firewallTracerPathMap;

        //a map for getting node GameObjects to change their properties
        private Dictionary<NetworkNode, GameObject> nodeToButtonMap;

        public GameObject actionPanel;

        //ui text templates
        private const string xpUiTemplate = "var xp = {0};";
        private const string nukesUiTemplate = "var nukes = {0};";
        private const string trapsUiTemplate = "var traps = {0};";
        private const string consoleUiTemplate = "root@hacker.org:~# ";

        private const float padding = 10f;

        private bool hackingDetectedHelperFlag = false;

        private void Awake()
        {            
            int noOfNodes = 18;
            int noOfTreasureNodes = 3;
            int noOfFirewallNodes = 3;
            int noOfSpamNodes = 4;

            int xp = 0;
            int tracerSpeedDecrease = 50;
            int trapDelay = 10;
            
            graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();

            //console init
            ConsoleTwoBeforeLast.text = consoleUiTemplate;
            ConsoleBeforelast.text = string.Empty;
            Console.text = string.Empty;

            //console printing function
            consolePrinter = (consoleMessage, color) => {

                if (ConsoleTwoBeforeLast.text.Equals(consoleUiTemplate))
                {
                    ConsoleTwoBeforeLast.text = consoleUiTemplate + consoleMessage;
                    Console.color = color;
                }
                else if (ConsoleBeforelast.text.Equals(string.Empty))
                {
                    ConsoleBeforelast.text = consoleUiTemplate + consoleMessage;
                    Console.color = color;
                }
                else if (Console.text.Equals(string.Empty))
                {
                    Console.text = consoleUiTemplate + consoleMessage;
                    Console.color = color;
                }
                else {

                    ConsoleTwoBeforeLast.text = ConsoleBeforelast.text;
                    ConsoleTwoBeforeLast.color = ConsoleBeforelast.color;

                    ConsoleBeforelast.text = Console.text;
                    ConsoleBeforelast.color = Console.color;

                    Console.text = consoleUiTemplate + consoleMessage;
                    Console.color = color;
                }
            };
            
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
                
                firewallTracerPathMap = new Dictionary<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>>();
                
                pathFinder = new DijkstraPathFinder();

                //configure tracer controllers
                networkConfigurator.firewallNodes.ForEach(firewallNode =>
                {
                    List<NetworkNode> cheapestPathToStart = pathFinder.FindShortestPath(firewallNode, networkConfigurator.startNode);
                    
                    TracerController tracer = new TracerController(new LinkAnimator(graphContainer, this), new TimeoutWaiter(this))
                        .SetTraceCompletedAction(() => {
                            consolePrinter("You've been traced! Police called. Run!", Color.red);
                            hackingController.BlockSignal();
                            foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in firewallTracerPathMap)
                                tracerPair.Value.Key.BlockTracer();
                        });

                    firewallTracerPathMap.Add(
                        firewallNode,
                        new KeyValuePair<TracerController, List<NetworkNode>>(tracer, cheapestPathToStart)
                        );
                });

                //configure hacking controller
                hackingController
                    .SetHackingCompletedAction(() => {

                        consolePrinter("Wow! Nice skills. Do you want a job?", Color.green);
                        foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in firewallTracerPathMap)
                            tracerPair.Value.Key.BlockTracer();
                    })
                    .SetDrawNukedLinkAction((start, end, color) => DrawLink(start, end, color))
                    .SetHackingDetectedAction(() => {

                        hackingDetectedHelperFlag = true;
                        consolePrinter("Your IP address has been compromised! Hurry up!", Color.red);

                        foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in firewallTracerPathMap)
                            tracerPair.Value.Key.TraceHackingSignal(tracerPair.Value.Value);

                    })
                    .SetFirewallHackedAction(firewallNode => {
                        consolePrinter("Firewall system hacked. Good job!", Color.green);
                        firewallTracerPathMap[firewallNode].Key.BlockTracer();
                    })
                    .SetUpdateRewardAction((reward, count) => {

                        if (reward.Equals(HackingController.Reward.Nuke))
                            Nuke_ui.text = String.Format(nukesUiTemplate, count);
                        else if (reward.Equals(HackingController.Reward.Trap))
                            Trap_ui.text = String.Format(trapsUiTemplate, count); ;

                    })
                    .SetUpdateXpAction(Xp => XP_ui.text = (String.Format(xpUiTemplate, Xp)))
                    .SetSpamNodeHackedAction(decreaseTracerSpeedPercent => { 

                        consolePrinter("Spam node hacked! Recalculating Node hacking difficulties...", Color.green);
                        
                        //decrease tracers speed
                        foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in firewallTracerPathMap)
                            tracerPair.Value.Key.DecreaseTracerSpeed(decreaseTracerSpeedPercent);

                        if (!hackingDetectedHelperFlag)
                        {
                            //recalculate cheapest paths from firewalls
                            foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in firewallTracerPathMap)
                            {
                                NetworkNode firewallNode = tracerPair.Key;

                                List<NetworkNode> cheapestPathToStart = pathFinder.FindShortestPath(firewallNode, networkConfigurator.startNode);

                                firewallTracerPathMap[firewallNode].Value.Clear();
                                cheapestPathToStart.ForEach(node => firewallTracerPathMap[firewallNode].Value.Add(node));

                                //set cheapest path to tracer
                                tracerPair.Value.Key.SetTracePath(tracerPair.Value.Value);
                            }
                        }
                        
                        consolePrinter("Node hacking difficulties recalculated!", Color.green);

                    })
                    .SetSpamNodeNukedAction(decreaseTracerSpeedPercent => {

                        consolePrinter("Spam node nuked! They dont know what hit them!", Color.green);

                        foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in firewallTracerPathMap)
                            tracerPair.Value.Key.DecreaseTracerSpeed(decreaseTracerSpeedPercent);

                    })
                    .SetChooseAttackAction((node, hackText, nukeText, trapText, difficulty) => {

                        actionPanel.transform.SetAsLastSibling();
                        actionPanel.SetActive(true);    
                        actionPanel.GetComponent<RectTransform>().anchoredPosition = 
                            new Vector2(
                                node.GetPosition().x < graphContainer.sizeDelta.x / 2 ? node.GetPosition().x + 50 : node.GetPosition().x - 50 , 
                                node.GetPosition().y < graphContainer.sizeDelta.y / 2 ? node.GetPosition().y + 50 : node.GetPosition().y - 50
                                );
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
                foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> entry in firewallTracerPathMap)
                    entry.Value.Key.consoleLog = message => consolePrinter(message, Color.red);
                
            }
            catch (Exception e)
            {
                Debug.Log("Error configuring network: " + e.Message + "\n" + e.StackTrace);
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
            nodeTransform.sizeDelta = new Vector2(15, 15);
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

        private GameObject DrawLink(Link link)
        {
           return DrawLink(link.GetNodes().Key.GetPosition(), link.GetNodes().Value.GetPosition(), new Color(1, 1, 1, 0.5f));
        }

        private GameObject DrawLink(Vector2 dotPosA, Vector2 dotPosB, Color color)
        {
            GameObject linkObject = new GameObject("link", typeof(Image));
            linkObject.transform.SetParent(graphContainer, false);
            linkObject.GetComponent<Image>().color = color;
            RectTransform linkTransform = linkObject.GetComponent<RectTransform>();

            Vector2 dir = (dotPosB - dotPosA).normalized;
            float distance = Vector2.Distance(dotPosA, dotPosB);

            linkTransform.anchorMin = new Vector2(0, 0);
            linkTransform.anchorMax = new Vector2(0, 0);
            linkTransform.sizeDelta = new Vector2(distance, 3f);
            linkTransform.anchoredPosition = dotPosA + dir * distance * 0.5f;

            linkTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            
            return linkObject;
        }
        
    }
}