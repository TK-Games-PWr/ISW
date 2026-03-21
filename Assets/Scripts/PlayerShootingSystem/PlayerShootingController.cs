using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] Transform holdPivot;
        [SerializeField] PlayerResources playerResources;
        [SerializeField] TMP_Text magAmmoAmount;
        [SerializeField] TMP_Text maxAmmoAmount;
        [SerializeField] GameObject nadePrefab;
        [SerializeField] Gun nadeRoot;
        int _currentSlot=0;
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
            _currentSlot = 0;
            SwitchWeapons(playerResources.weapons[0]);
            UpdateUI();
        }

        public void OnInvSlot2(InputValue input)
        {
            _currentSlot = 1;
            SwitchWeapons(playerResources.weapons[1]);
            UpdateUI();
        }

        public void OnInvSlot3(InputValue input)
        {
            _currentSlot = 2;
            SwitchWeapons(playerResources.weapons[2]);
            UpdateUI();
        }

        public void OnInvSlot4(InputValue input)
        {
            _currentSlot = 3;
            SwitchWeapons(playerResources.weapons[3]); 
            UpdateUI();
        }

        void SwitchWeapons(Gun gun)
        {
            if(currentGun)
                currentGun.gameObject.SetActive(false);
            if (!gun) return;
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
                        BulletImpactManager.Instance.SpawnImpact(hit.point, hit.normal, BulletImpactManager.ImpactType.Flesh);
                        float distance = hit.distance;
                        float multiplier = currentGun.gunInfo.damageFalloff.Evaluate(distance / 100f);
                        float finalDamage = currentGun.gunInfo.flatDamage * multiplier;
                        enemy.Damage(finalDamage);
                    }
                    else
                    {
                        BulletImpactManager.Instance.SpawnImpact(hit.point, hit.normal, BulletImpactManager.ImpactType.Ground);
                    }
                }
        }

        void UpdateUI()
        {
            if (!currentGun)
            {
                maxAmmoAmount.text = 0.ToString();
                magAmmoAmount.text = 0.ToString();
                return;
            }

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
                Gun weaponInCurrentSlot = playerResources.weapons[_currentSlot];
                if (weaponInCurrentSlot)
                {
                    if (!weaponInCurrentSlot.gunInfo.isExplosive)
                        weaponInCurrentSlot.GetComponent<GrabbableObject>().Drop();
                    else if (weaponInCurrentSlot.gunInfo.isExplosive)
                    {
                        DropNade(weaponInCurrentSlot);
                    }
                }

                playerResources.weapons[_currentSlot] = gun;
                playerResources.PutWeaponInInventoryObject(gun.gameObject);
                currentGun = null;
                if(playerResources.weapons[_currentSlot])
                   playerResources.weapons[_currentSlot].GetComponent<GrabbableObject>().Drop();
                playerResources.weapons[_currentSlot] = gun;
                playerResources.PutWeaponInInventoryObject(gun.gameObject);
                SwitchWeapons(playerResources.weapons[_currentSlot]);
                gun.PickedUp();

                if (_autoFireCoroutine != null)
                {
                    StopCoroutine(_autoFireCoroutine);
                    _autoFireCoroutine = null;
                }
            }
            else if (pickedObject.TryGetComponent(out AmmoPickup ammoPickup))
            {
                AmmoEntry ammoEntry = playerResources.playerAmmo.Find(a => a.ammoType == ammoPickup.ammoType);
                ammoEntry.amount += ammoPickup.amount;
                UpdateUI();
                Destroy(pickedObject.gameObject);
            }

            else if (pickedObject.TryGetComponent(out Nade nade))
            {
                nadeRoot.equippedNade = nade;
                Gun weaponInCurrentSlot= playerResources.weapons[_currentSlot];
                if (weaponInCurrentSlot)
                {
                    if (!weaponInCurrentSlot.gunInfo.isExplosive)
                        weaponInCurrentSlot.GetComponent<GrabbableObject>().Drop();
                    else
                    {
                        DropNade(weaponInCurrentSlot);
                    }
                }

                if(playerResources.weapons.Any(a=> a != null && 
                                                a.gunInfo != null && 
                                                a.gunInfo.isExplosive))
                {
                    AmmoEntry ammoEntry = playerResources.playerAmmo.Find(a => a.ammoType == AmmoType.Nade);
                    ammoEntry.amount++;
                    UpdateUI();
                }
                else
                {
                    playerResources.weapons[_currentSlot] = nadeRoot;
                    currentGun = null;
                    SwitchWeapons(playerResources.weapons[_currentSlot]);
                    nadeRoot.PickedUp();
                }
                Destroy(pickedObject.gameObject);
                
            }
        }

        void DropNade(Gun weaponInCurrentSlot)
        {
            weaponInCurrentSlot.transform.parent = transform;
            if (weaponInCurrentSlot.ammoInMag > 0)
            {
                weaponInCurrentSlot.ammoInMag = 0;
                AmmoEntry ammoEntry = playerResources.playerAmmo.Find(a => a.ammoType == AmmoType.Nade);
                ammoEntry.amount++;
                GameObject equippedNade=weaponInCurrentSlot.equippedNade.gameObject;
                equippedNade.transform.parent = null;
                Rigidbody rb = equippedNade.GetComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
                equippedNade.GetComponent<Collider>().enabled = true;

            }
        }
    }
}