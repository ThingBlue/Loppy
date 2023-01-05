using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
{
    public class CursorManager : MonoBehaviour
    {
        public Texture2D cursorTexture;

        private void Awake()
        {
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
            Cursor.lockState = CursorLockMode.None;
        }

        /*
        private void Update()
        {
            //Vector2 hotspot = new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f);
            //Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
        }
        */
    }
}
