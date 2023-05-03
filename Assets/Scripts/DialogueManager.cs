using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;


namespace Loppy
{
    [System.Serializable]
    public class Monologue
    {
        public string name;
        public List<string> sentences;
    }

    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager instance;

        #region Inspector members

        public float textOutputDelay = 0.01f;

        public TMP_Text nameText;
        public TMP_Text dialogueText;

        public Animator dialogueBoxAnimator;

        #endregion

        public Queue<Monologue> monologues;
        public Queue<string> sentences;

        private bool advanceDialogueKeyDown;

        private bool inDialogue = false;
        private bool sentenceComplete = true;

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
        }

        private void Update()
        {
            if (InputManager.instance.getKeyDown("advanceDialogue"))
            {
                advanceDialogueKeyDown = true;
            }
        }

        private void FixedUpdate()
        {
            if (advanceDialogueKeyDown)
            {
                if (sentenceComplete)
                {
                    nextSentence();
                }
                else
                {
                    // DEBUG
                    sentenceComplete = true;
                }
            }

            // Reset key down
            advanceDialogueKeyDown = false;
        }

        public void triggerDialogue(List<Monologue> dialogue)
        {
            // Clean and enqueue new dialogue
            monologues.Clear();
            for (int i = 0; i < dialogue.Count; i++)
            {
                monologues.Enqueue(dialogue[i]);
            }

            // Set dialogue state
            inDialogue = true;
            sentenceComplete = false;
            dialogueBoxAnimator.SetBool("inDialogue", inDialogue);

            // Begin dialogue
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
            sentences.Clear();
            Monologue monologue = monologues.Dequeue();
            for (int i = 0; i < monologue.sentences.Count; i++)
            {
                sentences.Enqueue(monologue.sentences[i]);
            }

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

            // Fetch and display next sentence
            string sentence = sentences.Dequeue();

            // Output letters one by one
            StopAllCoroutines();
            StartCoroutine(outputSentence(sentence));
        }

        IEnumerator outputSentence(string sentence)
        {
            dialogueText.text = "";

            for (int i = 0; i < sentence.Length; i++)
            {
                dialogueText.text += sentence[i];
                yield return new WaitForSeconds(textOutputDelay);
            }

            sentenceComplete = true;
        }

        public void endDialogue()
        {
            // Reset dialogue state
            inDialogue = false;
            sentenceComplete = true;
            dialogueBoxAnimator.SetBool("inDialogue", inDialogue);
        }
    }
}
