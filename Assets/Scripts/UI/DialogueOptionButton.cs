using Loppy.GameCore;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loppy.UI
{
    public class DialogueOptionButton : MonoBehaviour
    {
        #region Inspector members

        public RectTransform rectTransform;
        public Image image;
        public TMP_Text text;

        public float positionMoveSpeed = 20f;
        public float alphaMoveSpeed = 0.025f;
        public float textAlphaMoveSpeed = 0.05f;

        #endregion

        private float targetPosition;
        private float targetAlpha;
        private float targetTextAlpha;

        private bool destroyWhenInvisible;

        private void Start()
        {
            // Subscribe to events
            EventManager.instance.dialogueTriggered.AddListener(onDialogueTriggered);
            EventManager.instance.dialogueOptionSelected.AddListener(onDialogueOptionSelected);

            // Set initial position and alpha
            rectTransform.anchoredPosition = new Vector2(-200, rectTransform.anchoredPosition.y);
            image.color = new Color(0, 0, 0, 0);

            // Set target position and alpha
            targetPosition = 200;
            targetAlpha = 0.5f;
            targetTextAlpha = 1;
        }

        private void FixedUpdate()
        {
            // Move position and alpha towards target
            float position = Mathf.MoveTowards(rectTransform.anchoredPosition.x, targetPosition, positionMoveSpeed);
            float alpha = Mathf.MoveTowards(image.color.a, targetAlpha, alphaMoveSpeed);
            float textAlpha = Mathf.MoveTowards(text.color.a, targetTextAlpha, textAlphaMoveSpeed);
            rectTransform.anchoredPosition = new Vector2(position, rectTransform.anchoredPosition.y);
            image.color = new Color(0, 0, 0, alpha);
            text.color = new Color(255, 255, 255, textAlpha);

            // Destroy once invisible
            if (destroyWhenInvisible && alpha <= 0) Destroy(gameObject);
        }

        public void onDialogueTriggered()
        {
            destroyWhenInvisible = true;
            targetPosition = -100;
            targetAlpha = 0;
            targetTextAlpha = 0;
        }

        public void onDialogueOptionSelected()
        {
            destroyWhenInvisible = true;
            targetPosition = -100;
            targetAlpha = 0;
            targetTextAlpha = 0;
        }
    }
}
