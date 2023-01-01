using Loppy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager instance;

    #region Game settings

    public float fps = 60f;

    #endregion

    public List<GameObject> blocks;

    private void Awake()
    {
        // Singleton
        if (instance == null) instance = this;
        else Destroy(this);
    }

    private void Update()
    {
        Time.fixedDeltaTime = (1f / fps);
    }
}
