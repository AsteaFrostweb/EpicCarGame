using System.Collections.Generic;
using UnityEngine;

public class BotCarCollisionIgnoreGroup : MonoBehaviour
{
    private static readonly List<BotCarCollisionIgnoreGroup> ActiveGroups = new List<BotCarCollisionIgnoreGroup>();

    [SerializeField] private bool ignoreBotCollisions = true;

    private readonly List<Collider> colliders = new List<Collider>();

    private void OnEnable()
    {
        RefreshColliders();
        ActiveGroups.Add(this);
        UpdateIgnoredCollisions();
    }

    private void OnDisable()
    {
        ActiveGroups.Remove(this);
        SetIgnoredCollisions(false);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            RefreshColliders();
        }
    }

    [ContextMenu("Refresh Bot Collision Ignore Group")]
    public void RefreshColliders()
    {
        colliders.Clear();
        GetComponentsInChildren(true, colliders);
    }

    private void UpdateIgnoredCollisions()
    {
        if (!ignoreBotCollisions)
        {
            return;
        }

        for (int i = 0; i < ActiveGroups.Count; i++)
        {
            BotCarCollisionIgnoreGroup otherGroup = ActiveGroups[i];
            if (otherGroup == this || !otherGroup.ignoreBotCollisions)
            {
                continue;
            }

            SetIgnoredCollisions(otherGroup, true);
        }
    }

    private void SetIgnoredCollisions(bool ignored)
    {
        for (int i = 0; i < ActiveGroups.Count; i++)
        {
            BotCarCollisionIgnoreGroup otherGroup = ActiveGroups[i];
            if (otherGroup != this)
            {
                SetIgnoredCollisions(otherGroup, ignored);
            }
        }
    }

    private void SetIgnoredCollisions(BotCarCollisionIgnoreGroup otherGroup, bool ignored)
    {
        for (int i = 0; i < colliders.Count; i++)
        {
            Collider ownCollider = colliders[i];
            if (ownCollider == null)
            {
                continue;
            }

            for (int j = 0; j < otherGroup.colliders.Count; j++)
            {
                Collider otherCollider = otherGroup.colliders[j];
                if (otherCollider != null)
                {
                    Physics.IgnoreCollision(ownCollider, otherCollider, ignored);
                }
            }
        }
    }
}
