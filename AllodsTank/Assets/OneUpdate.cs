using System.Linq;
using UnityEngine;

internal class OneUpdate : MonoBehaviour
{
    private IUpdatable[] updatableScripts;

    private void Awake() => updatableScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IUpdatable>().ToArray();

    private void FixedUpdate()
    {
        foreach (var script in updatableScripts)
        {
            script.CustomFixedUpdate();
        }
    }

    internal interface IUpdatable
    {
        void CustomFixedUpdate();
    }
}
