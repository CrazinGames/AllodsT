using UnityEngine;
using static OneUpdate;

public class TestScript : MonoBehaviour, IUpdatable
{

    private bool ddd = true;

    void IUpdatable.CustomFixedUpdate()
    {
        if (ddd = true)
        {
            Debug.Log("Test");
        }
    }
}
