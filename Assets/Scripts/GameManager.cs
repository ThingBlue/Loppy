using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public List<GameObject> blocks;

    private void Awake()
    {
        Time.fixedDeltaTime = (1f / 60f);
    }
}
