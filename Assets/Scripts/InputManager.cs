using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
{
    public class InputManager : MonoBehaviour
    {
        // Singleton
        public static InputManager instance;

        public Dictionary<string, List<KeyCode>> keyMap;

        private void Awake()
        {
            // Singleton
            if (instance == null)
                instance = this;
            else
                Destroy(this);
        }

        private void Start()
        {
            // Initialize key map
            keyMap = new Dictionary<string, List<KeyCode>>();

            // SET DEFAULT KEYS FOR NOW
            addKeyToMap("left", KeyCode.A);
            addKeyToMap("left", KeyCode.LeftArrow);
            addKeyToMap("right", KeyCode.D);
            addKeyToMap("right", KeyCode.RightArrow);
            addKeyToMap("jump", KeyCode.W);
            addKeyToMap("jump", KeyCode.UpArrow);
            addKeyToMap("jump", KeyCode.Space);
            addKeyToMap("down", KeyCode.S);
            addKeyToMap("down", KeyCode.DownArrow);

            // Todo:
            // Read key mapping from config file
        }

        // Getters for specfic key states
        public bool getKey(string key)
        {
            foreach (KeyCode keyCode in keyMap[key])
            {
                if (Input.GetKey(keyCode))
                    return true;
            }
            return false;
        }

        public bool getKeyDown(string key)
        {
            foreach (KeyCode keyCode in keyMap[key])
            {
                if (Input.GetKeyDown(keyCode))
                    return true;
            }
            return false;
        }

        public bool getKeyUp(string key)
        {
            foreach (KeyCode keyCode in keyMap[key])
            {
                if (Input.GetKeyUp(keyCode))
                    return true;
            }
            return false;
        }

        // Getter for key map
        public List<KeyCode> GetKeysInMap(string key) { return keyMap[key]; }

        // Setters for key map
        public void clearKeyListInMap(string key) { keyMap[key].Clear(); }

        public void addKeyToMap(string key, KeyCode value)
        {
            // Create new keycode mapping if it doesn't exist
            if (!keyMap.ContainsKey(key))
                keyMap.Add(key, new List<KeyCode>());

            // Check if current value already exists in the list
            if (keyMap[key].Contains(value))
                return;

            keyMap[key].Add(value);
        }

        public void setKeyListInMap(string key, List<KeyCode> value) { keyMap[key] = value; }
    }
}
