using System.Linq;
using UnityEngine;

public class MapLocalManager : MonoBehaviour
{
    public EventTargets allEventTargets;

    [NaughtyAttributes.Button]
    public void AutoFindEventTargets()
    {
        allEventTargets = new();
        EventControllableEntity[] entityEvControllers = FindObjectsOfType<EventControllableEntity>();
        for (int i = 0; i < entityEvControllers.Length; i++)
        {
            if (entityEvControllers[i].gameObject.scene == gameObject.scene)
                allEventTargets.targetEntities[entityEvControllers[i].ID] = entityEvControllers[i];
        }

        EventControllableAnimation[] animEvControllers = FindObjectsOfType<EventControllableAnimation>();
        for (int i = 0; i < animEvControllers.Length; i++)
        {
            if (animEvControllers[i].gameObject.scene == gameObject.scene)
                allEventTargets.targetAnimations[animEvControllers[i].ID] = animEvControllers[i];
        }
    }

    public void Initialize()
    {
        EventControllableEntity[] entities = allEventTargets.targetEntities.Values.ToArray();
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].Initialize();
        }

        EventControllableAnimation[] animations = allEventTargets.targetAnimations.Values.ToArray();
        for (int i = 0; i < animations.Length; i++)
        {
            animations[i].Initialize();
        }
    }
}