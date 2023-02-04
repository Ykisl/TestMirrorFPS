using UnityEngine;

namespace Game.Weapon
{
    public interface IHitable
    {
        bool TryHit(GameObject sender);
    }
}
