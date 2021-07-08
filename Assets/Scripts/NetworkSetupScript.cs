using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
        
        public static NetworkConfigurator.ConfigInput configInput;

        private Action<String, Color> consolePrinter = null;
        
        private RectTransform graphContainer;

        public static NetworkConfigurator.NetworkConfiguration networkConf;
        public static bool tryAgain = false;

        private NetworkConfigurator networkConfigurator;
        private HackingController hackingController;
        private IPathFinder pathFinder;

        //map firewall nodes to their corresponding TracerController and cheapest path to start
        private Dictionary<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> firewallTracerPathMap;

        //a map for getting node GameObjects to change their properties
        private Dictionary<NetworkNode, GameObject> nodeToButtonMap;

        public GameObject actionPanel;
        public GameObject endPanel;

        //ui text templates
        private const string xpUiTemplate = "var xp = {0};";
        private const string nukesUiTemplate = "var nukes = {0};";
        private const string trapsUiTemplate = "var traps = {0};";
        private const string consoleUiTemplate = "root@hacker.org:~# ";

        private const float padding = 10f;

        private const string PP_XP = "xp";
        private const string PP_NUKES = "nukes";
        private const string PP_TRAPS = "traps";

        private void SaveXpPowerUps(int xp, int nukesCount, int trapsCount)
        {
            PlayerPrefs.SetInt(PP_XP, xp);
            PlayerPrefs.SetInt(PP_NUKES, nukesCount);
            PlayerPrefs.SetInt(PP_TRAPS, trapsCount);
            PlayerPrefs.Save();
        }

        private void ShowEndPanel()
        {
            endPanel.transform.SetAsLastSibling();
            endPanel.SetActive(true);
        }

        //end panel functions
        public void OnTryAgain()
        {
            SceneManager.LoadScene("HackingGameScene");
            NetworkSetupScript.tryAgain = true;
            TracerController.tracerCount = 0;
        }

        public void OnGenerateNewLevel()
        {
            SceneManager.LoadScene("HackingGameScene");
            NetworkSetupScript.tryAgain = false;
            TracerController.tracerCount = 0;
        }

        public void OnMainMenu()
        {
            SceneManager.LoadScene("Menu");
        }
        
        private void Awake()
        {   

            int xp = PlayerPrefs.GetInt(PP_XP);
            int nukes = PlayerPrefs.GetInt(PP_NUKES);
            int traps = PlayerPrefs.GetInt(PP_TRAPS);

            int tracerSpeedDecrease = configInput.spamDecrease;
            int trapDelay = configInput.trapDelay;
            
            graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();

            //xp, nuke, trap ui init
            XP_ui.text = String.Format(xpUiTemplate, xp);
            Nuke_ui.text = String.Format(nukesUiTemplate, nukes);
            Trap_ui.text = String.Format(trapsUiTemplate, traps);

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
                networkConfigurator = new NetworkConfigurator(configInput, new IncrementalTriangulationLinkGenerator());
                

                if (!tryAgain)
                    networkConf = networkConfigurator.ConfigureNetwork(graphContainer.sizeDelta.x - padding, graphContainer.sizeDelta.y - padding);

                networkConf.links.ForEach(edge => DrawLink(edge));
                networkConf.nodes.ForEach(node => {
                    GameObject nodeButton = DrawNode(node);
                    nodeToButtonMap.Add(node, nodeButton);
                });
                
                //init controllers
                hackingController = new HackingController(
                    networkConf.nodes, 
                    configInput.treasureCount, 
                    xp, 
                    nukes,
                    traps,
                    tracerSpeedDecrease, 
                    trapDelay, 
                    new LinkAnimator(graphContainer, this));
                
                firewallTracerPathMap = new Dictionary<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>>();
                
                pathFinder = new DijkstraPathFinder();

                //configure tracer controllers
                networkConf.firewallNodes.ForEach(firewallNode =>
                {
                    List<NetworkNode> cheapestPathToStart = pathFinder.FindShortestPath(firewallNode, networkConf.startNode);
                    
                    TracerController tracer = new TracerController(new LinkAnimator(graphContainer, this), new TimeoutWaiter(this))
                        .SetTraceCompletedAction(() => {
                            consolePrinter("You've been traced! Police called. Run!", Color.red);
                            hackingController.BlockSignal();
                            foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in firewallTracerPathMap)
                                tracerPair.Value.Key.BlockTracer();

                            //GAME OVER
                            SaveXpPowerUps(hackingController.xp, hackingController.nukesCount, hackingController.trapsCount);
                            ShowEndPanel();
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

                        //GAME OVER
                        SaveXpPowerUps(hackingController.xp, hackingController.nukesCount, hackingController.trapsCount);
                        ShowEndPanel();
                    })
                    .SetDrawNukedLinkAction((start, end, color) => DrawLink(start, end, color))
                    .SetHackingDetectedAction(() => {
                        
                        consolePrinter("Your IP address has been compromised! Hurry up!", Color.red);

                        foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in firewallTracerPathMap)
                            tracerPair.Value.Key.TraceHackingSignal(tracerPair.Value.Value);
                    })
                    .SetFirewallHackedAction(firewallNode => {

                        consolePrinter(String.Format("Firewall system {0} hacked. Good job!", firewallTracerPathMap[firewallNode].Key.GetTracerNumber()), Color.green);

                        //block hacked firewall tracer
                        firewallTracerPathMap[firewallNode].Key.BlockTracer();

                        //destroy traced link UI objects
                        firewallTracerPathMap[firewallNode].Key.GetTracedLinkGameObjects().ForEach(linkGameObject => Destroy(linkGameObject));
                    })
                    .SetUpdateRewardAction((reward, count) => {

                        if (reward.Equals(HackingController.Reward.Nuke))
                            Nuke_ui.text = String.Format(nukesUiTemplate, count);
                        else if (reward.Equals(HackingController.Reward.Trap))
                            Trap_ui.text = String.Format(trapsUiTemplate, count);

                    })
                    .SetUpdateXpAction(Xp => XP_ui.text = (String.Format(xpUiTemplate, Xp)))
                    .SetSpamNodeHackedAction(decreaseTracerSpeedPercent => { 

                        consolePrinter("Spam node hacked! Recalculated Node hacking difficulties...", Color.green);
                        
                        //decrease tracers speed
                        foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in firewallTracerPathMap)
                            tracerPair.Value.Key.DecreaseTracerSpeed(decreaseTracerSpeedPercent);
                        
                        //recalculate cheapest paths for each tracer
                        foreach (KeyValuePair<NetworkNode, KeyValuePair<TracerController, List<NetworkNode>>> tracerPair in firewallTracerPathMap)
                        {
                            NetworkNode currentTracingNode = tracerPair.Value.Key.IsActive() ? tracerPair.Value.Key.GetCurrentTracingNode() : tracerPair.Key;

                            List<NetworkNode> cheapestPathToStart = pathFinder.FindShortestPath(currentTracingNode, networkConf.startNode);
                                
                            firewallTracerPathMap[tracerPair.Key].Value.Clear();
                            cheapestPathToStart.ForEach(node => firewallTracerPathMap[tracerPair.Key].Value.Add(node));

                            //set new path to tracer controller
                            if (tracerPair.Value.Key.IsActive())
                                firewallTracerPathMap[tracerPair.Key].Key.SetTracePath(cheapestPathToStart);
                                
                            consolePrinter(String.Format("Tracer {0} recalculated to start.", tracerPair.Value.Key.GetTracerNumber()), Color.red);
                        }
                        
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
                                node.GetPosition().x < graphContainer.sizeDelta.x / 2 ? node.GetPosition().x + 50 : node.GetPosition().x - 50, 
                                node.GetPosition().y < graphContainer.sizeDelta.y / 2 ? node.GetPosition().y + 50 : node.GetPosition().y - 50);

                        Button[] buttons = actionPanel.GetComponentsInChildren<Button>();
                        
                        foreach (Button butt in buttons)
                        {
                            if (butt.name == "Difficulty")
                            {
                                butt.GetComponentInChildren<Text>().text = node.GetNodeType() + ": " + difficulty;
                            }
                            else if (butt.name == "Hack")
                            {
                                if (!hackText.Equals(string.Empty))
                                {
                                    butt.GetComponentInChildren<Text>().color = Color.green;
                                    butt.onClick.AddListener(() => { hackingController.HackNode(node); actionPanel.SetActive(false); });
                                }
                                else
                                    butt.GetComponentInChildren<Text>().color = Color.red;
                            } 
                            else if (butt.name == "Nuke")
                            {
                                if (!nukeText.Equals(string.Empty))
                                {
                                    butt.GetComponentInChildren<Text>().color = Color.green;
                                    butt.onClick.AddListener(() => { hackingController.NukeNode(node); actionPanel.SetActive(false); });
                                }
                                else
                                    butt.GetComponentInChildren<Text>().color = Color.red;
                            }
                            else if (butt.name == "Trap")
                            {
                                if (!trapText.Equals(string.Empty))
                                {
                                    butt.GetComponentInChildren<Text>().color = Color.green;
                                    butt.onClick.AddListener(() => {
                                        nodeToButtonMap[node].GetComponent<Image>().color = Color.magenta;
                                        consolePrinter("Trap kit set!", Color.green);
                                        hackingController.TrapNode(node);
                                        actionPanel.SetActive(false);
                                    });
                                }
                                else
                                    butt.GetComponentInChildren<Text>().color = Color.red;
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


                consolePrinter(String.Format("Start node hacking difficulty: {0}. Keep it safe :)", networkConf.startNode.GetHackingDifficulty()), Color.green);
            }
            catch (Exception e)
            {
                Debug.Log("Error configuring network: " + e.Message + "\n" + e.StackTrace);
                consolePrinter("Init error...", Color.red);
            }
        }
        
        //Node and link draw functions
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