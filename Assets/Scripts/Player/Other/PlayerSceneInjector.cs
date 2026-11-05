using UnityEngine;
using Zenject;

public class PlayerSceneInjector : MonoBehaviour
{
    private void Awake()
    {
        // NGO can spawn a player before SceneContextRegistry is initialized in Start.
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            var context = root.GetComponentInChildren<SceneContext>();
            if (context == null)
                continue;

            context.Container.InjectGameObject(gameObject);
            return;
        }

        throw new ZenjectException($"SceneContext not found in scene '{gameObject.scene.name}'");
    }
}