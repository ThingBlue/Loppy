using Loppy.GameCore;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Loppy.UI
{
    [Serializable]
    public class Monologue
    {
        public string name;
        [TextArea(3, 10)]
        public List<string> sentences;
        public List<DialogueOption> options;
    }

    [Serializable]
    public class DialogueOption
    {
        public string optionText;
        public List<Monologue> dialogue;
    }

    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager instance;

        #region Inspector members

        public GameObject dialogueCanvas;
        public TMP_Text nameText;
        public TMP_Text dialogueText;
        public Image dialogueBox;

        public GameObject dialogueOptionButtonPrefab;

        public float textOutputDelay = 0.01f;
        public float fadeSpeed = 0.1f;

        #endregion

        private Queue<Monologue> monologues;
        private Queue<string> sentences;
        private Queue<char> currentSentence;

        private List<DialogueOption> options;
        private List<GameObject> optionButtons;

        public delegate void OutputCompleteDelegate();
        private OutputCompleteDelegate currentOutputCompleteCallback;

        private bool inDialogue = false;
        private bool advanceDialogueKeyDown;

        private float textOutputTimer = 0;

        private float dialogueAlpha = 0;
        private float targetAlpha = 0;

        private void Awake()
        {
            // Singleton
            if (instance == null) instance = this;
            else Destroy(this);
        }

        private void Start()
        {
            // Initialize dialogue queue
            monologues = new Queue<Monologue>();
            sentences = new Queue<string>();
            currentSentence = new Queue<char>();
            options = new List<DialogueOption>();
            optionButtons = new List<GameObject>();
        }

        private void Update()
        {
            // Handle timers
            textOutputTimer += Time.deltaTime;

            // Handle input
            if (InputManager.instance.getKeyDown("advanceDialogue"))
            {
                advanceDialogueKeyDown = true;
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                createOptionPrefab(0, "Option 1");
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                createOptionPrefab(1, "Option 2");
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                createOptionPrefab(2, "Option 3");
            }
        }

        private void FixedUpdate()
        {
            // Check for input
            if (advanceDialogueKeyDown && inDialogue)
            {
                // Unfinished sentence
                if (currentSentence.Count > 0)
                {
                    // Immediately complete current sentence
                    while (currentSentence.Count > 0)
                    {
                        // Dequeue and output next character
                        char nextChar = currentSentence.Dequeue();
                        dialogueText.text += nextChar;
                    }
                }
                // Current sentence finished
                else
                {
                    // Go to next sentence
                    nextSentence();
                }
            }

            // Output text
            if (currentSentence.Count > 0)
            {
                if (textOutputTimer >= textOutputDelay)
                {
                    // Dequeue and output next character
                    char nextChar = currentSentence.Dequeue();
                    dialogueText.text += nextChar;

                    // Decrement timer
                    textOutputTimer -= textOutputDelay;
                }
            }
            // Sentence already completed
            else
            {
                // Keep timer at 0
                textOutputTimer = 0;
            }

            // Handle alpha
            dialogueAlpha = Mathf.MoveTowards(dialogueAlpha, targetAlpha, fadeSpeed);
            nameText.color = new Color(255, 255, 255, dialogueAlpha);
            dialogueText.color = new Color(255, 255, 255, dialogueAlpha);
            dialogueBox.color = new Color(0, 0, 0, dialogueAlpha * 0.6f);

            // Reset key down
            advanceDialogueKeyDown = false;
        }

        public void triggerDialogue(List<Monologue> dialogue, OutputCompleteDelegate outputCompleteCallback = null)
        {
            // Handle callback if there is an existing dialogue
            if (monologues.Count > 0 && currentOutputCompleteCallback != null) currentOutputCompleteCallback();

            // Clean and enqueue new dialogue
            monologues.Clear();
            for (int i = 0; i < dialogue.Count; i++)
            {
                monologues.Enqueue(dialogue[i]);
            }

            // Set dialogue target alpha
            targetAlpha = 1;

            // Set callback
            currentOutputCompleteCallback = outputCompleteCallback;

            // Begin dialogue
            inDialogue = true;
            nextMonologue();
        }

        public void nextMonologue()
        {
            // Check for end of dialogue
            if (monologues.Count == 0)
            {
                endDialogue();
                return;
            }

            // Clean and enqueue new monologue
            Monologue monologue = monologues.Dequeue();
            sentences.Clear();
            foreach (string sentence in monologue.sentences)
            {
                sentences.Enqueue(sentence);
            }
            options.Clear();
            if (monologue.options.Count > 0) options = new List<DialogueOption>(monologue.options);

            // Begin monologue
            nameText.text = monologue.name;
            nextSentence();
        }

        public void nextSentence()
        {
            // Check for end of monologue
            if (sentences.Count == 0)
            {
                nextMonologue();
                return;
            }

            // Clean existing dialogue
            dialogueText.text = "";
            currentSentence.Clear();

            // Fetch and enqueue new sentence
            string sentence = sentences.Dequeue();
            for (int i = 0; i < sentence.Length; i++)
            {
                currentSentence.Enqueue(sentence[i]);
            }

            return;
        }

        public void endDialogue()
        {
            inDialogue = false;

            // Clear queues
            monologues.Clear();
            sentences.Clear();
            currentSentence.Clear();

            // Check for options
            if (options.Count > 0)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    createOptionPrefab(i, options[i].optionText);
                }
                return;
            }

            // Reset dialogue target alpha
            targetAlpha = 0;

            // Handle callback
            if (currentOutputCompleteCallback != null)
            {
                currentOutputCompleteCallback();
                currentOutputCompleteCallback = null;
            }
        }

        private void createOptionPrefab(int optionIndex, string optionText)
        {
            GameObject newOptionPrefab = Instantiate(dialogueOptionButtonPrefab, dialogueCanvas.transform);

            // Set local position
            newOptionPrefab.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, optionIndex * -80);

            // Set text
            newOptionPrefab.GetComponentInChildren<TMP_Text>().text = optionText;

            // Set on press callback
            newOptionPrefab.GetComponent<Button>().onClick.AddListener(() => onDialogueOptionButtonPressed(optionIndex));
        
            // Add to list of buttons
            optionButtons.Add(newOptionPrefab);
        }

        private void onDialogueOptionButtonPressed(int optionIndex)
        {
            // Begin new dialogue
            triggerDialogue(options[optionIndex].dialogue, currentOutputCompleteCallback);

            // Clear options
            options.Clear();
            foreach (GameObject optionButton in optionButtons)
            {
                Destroy(optionButton);
            }
        }
    }
}
