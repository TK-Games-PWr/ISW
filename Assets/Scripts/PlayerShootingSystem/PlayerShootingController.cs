using System;
using System.Collections;
using System.Collections.Generic;
using TK_Shared._3DPlayerMovement;
using TK_Shared.ObjectInteractions3D;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerShootingSystem
{
    public class PlayerShootingController : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [SerializeField] float throwForce;
        [SerializeField] float throwUpwardForce;
        [SerializeField] List<Gun> guns;
        [SerializeField] Transform holdPivot;
        [SerializeField] PlayerResources playerResources;
        [SerializeField] TMP_Text magAmmoAmount;
        [SerializeField] TMP_Text maxAmmoAmount;
        [SerializeField] GameObject nadePrefab;
        public Gun currentGun;

        bool _isHoldingShoot;
        Coroutine _autoFireCoroutine;

        void Awake()
        {
            PlayerActionsController.OnPickedUp += HandlePickup;
        }

        void OnDestroy()
        {
            PlayerActionsController.OnPickedUp -= HandlePickup;
            if (_autoFireCoroutine != null) StopCoroutine(_autoFireCoroutine);
        }

        public void OnShootInput(InputValue value)
        {
            _isHoldingShoot = value.isPressed;

            if (currentGun == null) return;

            if (!currentGun.gunInfo.isExplosive && !currentGun.gunInfo.isMelee)
            {
                if (_isHoldingShoot)
                {
                    TryShootOnce();

                    if (currentGun.gunInfo.isAutomatic && _autoFireCoroutine == null)
                    {
                        _autoFireCoroutine = StartCoroutine(AutomaticFireLoop());
                    }
                }
                else
                {
                    if (_autoFireCoroutine != null)
                    {
                        StopCoroutine(_autoFireCoroutine);
                        _autoFireCoroutine = null;
                    }
                }
            }
            else if(currentGun.gunInfo.isExplosive)
            {
                if(currentGun.equippedNade==null) return;
                if (_isHoldingShoot)
                {
                    Rigidbody rb=currentGun.slide.AddComponent<Rigidbody>();
                    currentGun.slide.AddComponent<BoxCollider>();
                    currentGun.slide.transform.parent = null;
                    Vector3 forceToUnpin= rb.transform.forward * throwForce;
                    rb.AddForce(forceToUnpin, ForceMode.Impulse);
                    Cook();
                }
                else
                {
                    Rigidbody rb=currentGun.equippedNade.GetComponent<Rigidbody>();
                    rb.useGravity = true;
                    rb.isKinematic = false;
                    currentGun.equippedNade.GetComponent<BoxCollider>().enabled = true;
                    currentGun.equippedNade.transform.parent = null;
                    Vector3 throwingForce = cameraTransform.forward * throwForce + transform.up * throwUpwardForce;
                    rb.AddForce(throwingForce, ForceMode.Impulse);
                    currentGun.equippedNade = null;
                }
            }
        }

        public void OnReloadInput(InputValue value)
        {
            if (value.isPressed)
            {
                int neededAmount = currentGun.gunInfo.maxAmmo - currentGun.ammoInMag;
                if (neededAmount <= 0)
                    return;

                AmmoEntry ammoEntry = playerResources.playerAmmo.Find(a => a.ammoType == currentGun.gunInfo.ammoType);
        
                if (ammoEntry == null)
                    return;

                int reloadAmount = Mathf.Min(neededAmount, ammoEntry.amount);
                if (reloadAmount <= 0)
                    return;

                ammoEntry.amount -= reloadAmount;
                currentGun.ammoInMag += reloadAmount;

                if (currentGun.gunInfo.ammoType == AmmoType.Nade)
                {
                    GameObject granade = Instantiate(nadePrefab, currentGun.transform.position, currentGun.transform.rotation);
                    granade.transform.SetParent(currentGun.transform);
                    Nade nade = granade.GetComponent<Nade>();
                    currentGun.slide = nade.pin;
                    currentGun.equippedNade = nade;
                }

                UpdateUI();
            }
        }
        public void OnInvSlot1(InputValue input)
        {
            SwitchWeapons(guns[0]);
        }

        public void OnInvSlot2(InputValue input)
        {
            SwitchWeapons(guns[1]);
        }

        public void OnInvSlot3(InputValue input)
        {
            SwitchWeapons(guns[2]);
        }

        public void OnInvSlot4(InputValue input)
        {
            SwitchWeapons(guns[3]); 
        }

        void SwitchWeapons(Gun gun)
        {
            if(currentGun)
                currentGun.gameObject.SetActive(false);
            gun.gameObject.SetActive(true);
            currentGun = gun;
            gun.GetComponent<GrabbableObject>().Grab(holdPivot);
            UpdateUI();
        }

        void Cook()
        {
            if(currentGun.ammoInMag<=0) return;
            currentGun.ammoInMag -= 1;
            UpdateUI();
            currentGun.CookNade();
        }
        void TryShootOnce()
        {

                if (!currentGun) return;
                if (currentGun.ammoInMag <= 0) return;
                currentGun.PerformShoot();
                UpdateUI();
                if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit))
                {
                    if (hit.transform.TryGetComponent(out EnemySystem.EnemyHealth enemy))
                    {
                        float distance = hit.distance;
                        float multiplier = currentGun.gunInfo.damageFalloff.Evaluate(distance / 100f);
                        float finalDamage = currentGun.gunInfo.flatDamage * multiplier;
                        enemy.Damage(finalDamage);
                    }
                }
        }

        void UpdateUI()
        {
            if(!currentGun)
                return;
            AmmoEntry ammoEntry = playerResources.playerAmmo.Find(a => a.ammoType == currentGun.gunInfo.ammoType);
            maxAmmoAmount.text=ammoEntry.amount.ToString();
            magAmmoAmount.text=currentGun.ammoInMag.ToString();
            
        }
        IEnumerator AutomaticFireLoop()
        {
            while (_isHoldingShoot && currentGun && currentGun.gunInfo.isAutomatic)
            {
                yield return new WaitForSeconds(currentGun.gunInfo.fireRate);
                TryShootOnce();
            }

            _autoFireCoroutine = null;
        }

        void HandlePickup(Transform pickedObject)
        {
            if (pickedObject.TryGetComponent(out Gun gun))
            {
                currentGun = gun;
                gun.PickedUp();

                if (_autoFireCoroutine != null)
                {
                    StopCoroutine(_autoFireCoroutine);
                    _autoFireCoroutine = null;
                }
            }

            if (pickedObject.TryGetComponent(out AmmoPickup ammoPickup))
            {
                AmmoEntry ammoEntry = playerResources.playerAmmo.Find(a => a.ammoType == ammoPickup.ammoType);
                ammoEntry.amount += ammoPickup.amount;
                UpdateUI();
                Destroy(pickedObject.gameObject);
            }
        }
    }
}