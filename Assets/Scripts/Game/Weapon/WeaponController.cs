using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Weapon
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [Space]
        [SerializeField] private float distance;

        public void Shoot()
        {
            TryShoot();
        }

        public bool TryShoot()
        {
            return TryShoot(out var hitable);
        }

        public bool TryShoot(out IHitable hitable)
        {
            hitable = null;

            if (!CanShoot())
            {
                return false;
            }

            if (!Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward, out var hit, distance))
            {
                return false;
            }

            var hitGameObject = hit.collider.gameObject;
            if (!hitGameObject.TryGetComponent<IHitable>(out hitable))
            {
                return false;
            }

            return hitable.TryHit(gameObject);
        }

        public virtual bool CanShoot()
        {
            return true;
        }
    }
}
