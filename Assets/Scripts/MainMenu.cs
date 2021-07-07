using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using TMPro;

namespace TwoDesperadosTest
{
    public class MainMenu : MonoBehaviour
    {
        public GameObject nodeCountInput;
        public GameObject treasureCountInput;
        public GameObject firewallCountInput;
        public GameObject spamCountInput;
        public GameObject spamDecreaseInput;
        public GameObject trapDelayInput;

        public GameObject validationResultText;

        public const string NODE_COUNT_KEY = "NodeCount";
        public const string TREASURE_COUNT_KEY = "TreasureCount";
        public const string FIREWALL_COUNT_KEY = "FirewallCount";
        public const string SPAM_COUNT_KEY = "SpamCount";
        public const string SPAM_DECREASE_KEY = "SpamDecrease";
        public const string TRAP_DELAY_KEY = "TrapDelay";

        private void SaveConfigInPlayerPrefs(NetworkConfigurator.ConfigInput config)
        {
            PlayerPrefs.SetInt(NODE_COUNT_KEY, config.nodeCount);
            PlayerPrefs.SetInt(TREASURE_COUNT_KEY, config.treasureCount);
            PlayerPrefs.SetInt(FIREWALL_COUNT_KEY, config.firewallCount);
            PlayerPrefs.SetInt(SPAM_COUNT_KEY, config.spamCount);
            PlayerPrefs.SetInt(SPAM_DECREASE_KEY, config.spamDecrease);
            PlayerPrefs.SetInt(TRAP_DELAY_KEY, config.trapDelay);
            PlayerPrefs.Save();
        }

        private NetworkConfigurator.ConfigInput LoadConfigFromPlayerPrefs()
        {
            return new NetworkConfigurator.ConfigInput(
                PlayerPrefs.GetInt(NODE_COUNT_KEY),
                PlayerPrefs.GetInt(TREASURE_COUNT_KEY),
                PlayerPrefs.GetInt(FIREWALL_COUNT_KEY),
                PlayerPrefs.GetInt(SPAM_COUNT_KEY),
                PlayerPrefs.GetInt(SPAM_DECREASE_KEY),
                PlayerPrefs.GetInt(TRAP_DELAY_KEY));
        }

        private void Awake()
        {

            NetworkSetupScript.configInput = LoadConfigFromPlayerPrefs();
            
            if (NetworkSetupScript.configInput.nodeCount == 0)
            {
                NetworkSetupScript.configInput = new NetworkConfigurator.ConfigInput(
                    5,
                    1,
                    1,
                    1,
                    10,
                    2);
            }
            else
            {
                nodeCountInput.GetComponent<TMP_InputField>().text = String.Format("{0}", NetworkSetupScript.configInput.nodeCount);
                treasureCountInput.GetComponent<TMP_InputField>().text = String.Format("{0}", NetworkSetupScript.configInput.treasureCount);
                firewallCountInput.GetComponent<TMP_InputField>().text = String.Format("{0}", NetworkSetupScript.configInput.firewallCount);
                spamCountInput.GetComponent<TMP_InputField>().text = String.Format("{0}", NetworkSetupScript.configInput.spamCount);
                spamDecreaseInput.GetComponent<TMP_InputField>().text = String.Format("{0}", NetworkSetupScript.configInput.spamDecrease);
                trapDelayInput.GetComponent<TMP_InputField>().text = String.Format("{0}", NetworkSetupScript.configInput.trapDelay);
            }
        }
        
        public void PlayGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void ValidateAndSaveSettings()
        {

            int nodeCount;
            int treasureCount;
            int firewallCount;
            int spamCount;
            int spamDecrease;
            int trapDelay;


            string nodeCountString = nodeCountInput.GetComponent<TMP_InputField>().text;
            if (!Int32.TryParse(nodeCountString, out nodeCount))
            {
                validationResultText.GetComponent<TMP_Text>().text = "Node Count Input not an integer!";
            }
            else
            {
                string treasureCountString = treasureCountInput.GetComponent<TMP_InputField>().text;
                if (!Int32.TryParse(treasureCountString, out treasureCount))
                {
                    validationResultText.GetComponent<TMP_Text>().text = "Treasure Node Count Input not an integer!";
                }
                else
                {
                    string firewallCountString = firewallCountInput.GetComponent<TMP_InputField>().text;
                    if (!Int32.TryParse(firewallCountString, out firewallCount))
                    {
                        validationResultText.GetComponent<TMP_Text>().text = "Firewall Node Count Input not an integer!";
                    }
                    else
                    {
                        string spamCountString = spamCountInput.GetComponent<TMP_InputField>().text;
                        if (!Int32.TryParse(spamCountString, out spamCount))
                        {
                            validationResultText.GetComponent<TMP_Text>().text = "Spam Node Count Input not an integer!";
                        }
                        else
                        {
                            string spamDecreaseString = spamDecreaseInput.GetComponent<TMP_InputField>().text;
                            if (!Int32.TryParse(spamDecreaseString, out spamDecrease))
                            {
                                validationResultText.GetComponent<TMP_Text>().text = "Spam Node Decrease Input not an integer!";
                            }
                            else
                            {
                                string trapDelayString = trapDelayInput.GetComponent<TMP_InputField>().text;
                                if (!Int32.TryParse(trapDelayString, out trapDelay))
                                {
                                    validationResultText.GetComponent<TMP_Text>().text = "Trap Delay Time Input not an integer!";
                                }
                                else
                                {
                                    try
                                    {
                                        NetworkConfigurator.ValidateNodeTypeAmounts(nodeCount, treasureCount, firewallCount, spamCount);

                                        if (spamDecrease < 10 || spamDecrease > 90)
                                            validationResultText.GetComponent<TMP_Text>().text = "Spam Decrease should be >= 10 && <= 90";
                                        else
                                        {
                                            if (trapDelay < 1)
                                                validationResultText.GetComponent<TMP_Text>().text = "Trap Delay Time should be > 1";
                                            else
                                            {
                                                NetworkSetupScript.configInput = new NetworkConfigurator.ConfigInput(
                                                    nodeCount,
                                                    treasureCount,
                                                    firewallCount,
                                                    spamCount,
                                                    spamDecrease,
                                                    trapDelay);

                                                SaveConfigInPlayerPrefs(NetworkSetupScript.configInput);
                                                validationResultText.GetComponent<TMP_Text>().text = "Settings saved successfully";
                                            }
                                        }
                                    }
                                    catch (ArgumentException e)
                                    {
                                        validationResultText.GetComponent<TMP_Text>().text = e.Message;
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