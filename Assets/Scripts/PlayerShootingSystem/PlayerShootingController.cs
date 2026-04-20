using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnemySystem;
using NUnit.Framework;
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
        /*
         * Classification data section
         */
        [HideInInspector] 
        public int shots=0;
        [HideInInspector]
        public int hits = 0;
        
        [SerializeField] Transform cameraTransform;
        Camera fpsCam;
        [SerializeField] UICrosshair uiCrosshair;
        [SerializeField] AudioSource hitDing;
        [Header("Aiming")]
        [SerializeField] float aimSpeed = 12f;
        [SerializeField] float aimDistance = 0.35f;
        AudioSource[] hitDingInstances =  new AudioSource[5];
        int hitDingIter = 0;
        
        [Header("Melee")]
        [SerializeField] GunInfo meleeInfo;
        [SerializeField] float meleeRange = 2.5f;
        [SerializeField] Animation meleeAnim;
        [SerializeField] float stealthKillBehindThreshold = -0.4f; // -1 is exactly behind, 0 is exactly to the side
        [Space(8)]
        [SerializeField] float throwForce;
        [SerializeField] float throwUpwardForce;
        [SerializeField] Transform holdPivot;
        [SerializeField] PlayerResources playerResources;
        [SerializeField] TMP_Text magAmmoAmount;
        [SerializeField] TMP_Text maxAmmoAmount;
        int _currentSlot=0;
        public Gun currentGun;
        public ThrowableInfo currentThrowable;
        int currentThrowableIndex;
        Vector3 _hipfireLocalPos;
        Quaternion _hipfireLocalRot;
        bool _isAiming;
        Quaternion _scopeLocalRot;
        Vector3 _scopeLocalPos;
        Coroutine _aimCoroutine;
        bool _isHoldingShoot;
        bool _isHoldingThrow;
        Coroutine _autoFireCoroutine;
        Coroutine _reloadCoroutine;
        bool _isActuallyAiming;
        float currentSpread;
        
        void Awake()
        {
            PlayerActionsController.OnPickedUp += HandlePickup;
        }

        void Start()
        {
            fpsCam = LevelManager.Instance.playerCamera;
            if(hitDing)
            {
                hitDingInstances[0] = hitDing;
                for (int i = 0; i < 4; i++)
                {
                    GameObject copy = Instantiate(hitDing.gameObject, hitDing.transform.parent);
                    copy.GetComponent<AudioSource>().pitch = UnityEngine.Random.Range(0.6f, 0.8f);
                    hitDingInstances[i+1] = copy.GetComponent<AudioSource>();
                }
            }
            Assert.True(playerResources.throwables.Count > 0, "Player must have at least one throwable in resources");
            currentThrowable=playerResources.throwables[0];
            currentThrowableIndex=0;
        }

        void Update()
        {
            if (!currentGun) return;
            // TODO: more spread variables, like reducing it when crouching or scoping
            currentSpread = currentGun.gunInfo.spread + Mathf.Clamp01(PlayerActionsController.Speed/6f) * currentGun.gunInfo.movementSpreadPenalty;
            uiCrosshair.SetSpread(currentSpread);
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

            if (!currentGun.gunInfo.isMelee)
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
        }

        public void OnMeleeInput(InputValue value)
        {
            if (value.isPressed)
            {
                meleeAnim.Play();
            }
        }

        internal void OnMeleeAnimEnd()
        {
            if(Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, meleeRange))
            {
                if (hit.transform.TryGetComponent(out EnemyHitbox hitbox))
                {
                    EnemyHealth enemy = hitbox.GetEnemyHealth();
                    if (enemy.IsDead) return;
                    if (hitDing) PlayHitSound();
                    uiCrosshair.ShowHit();
                    
                    Vector3 dirFromEnemyToPlayer = (transform.position - enemy.transform.position).normalized;
                    float dotProduct = Vector3.Dot(enemy.transform.forward, dirFromEnemyToPlayer);
                    bool isStealthKill = dotProduct < stealthKillBehindThreshold;
                    if (isStealthKill)
                    {
                        enemy.StealthKill();
                    }
                    else
                    {
                        enemy.Damage(meleeInfo.flatDamage);
                    }
                    hits++;
                }
            }
        }

        public void OnReloadInput(InputValue value)
        {
            if (!currentGun) return;
            if (value.isPressed)
            {
                    _reloadCoroutine ??= StartCoroutine(ReloadCoroutine(currentGun.gunInfo.reloadTime));

            }
        }
        public void Zoom(InputValue value)
        {
            if (currentGun && currentGun.gunInfo.ammoType == AmmoType.Snipe)
            {
                _isActuallyAiming = value.isPressed;
        
                if(_isActuallyAiming)
                {
                    uiCrosshair.SetActive(false);
                    _hipfireLocalPos = currentGun.transform.localPosition;
                    _hipfireLocalRot = currentGun.transform.localRotation;
                    _scopeLocalRot = Quaternion.Inverse(currentGun.transform.rotation) * currentGun.scopeQuad.rotation;
                    _scopeLocalPos = currentGun.transform.InverseTransformPoint(currentGun.scopeQuad.position);
                }
                else
                {
                    uiCrosshair.SetActive(true);
                }
            }
            else
            {
                uiCrosshair.SetActive(true);
            }
        }
        public void OnThrowInput(InputValue value)
        {
            if (value.isPressed)
            {
                if (playerResources.playerAmmo.Find(a => a.ammoType == currentThrowable.ammoType).amount > 0)
                {
                    GameObject thrownThrowable=Instantiate(currentThrowable.throwablePrefab,holdPivot.position, Quaternion.identity);
                    thrownThrowable.GetComponent<Rigidbody>()
                        .AddForce((cameraTransform.forward * throwForce) + (Vector3.up * throwUpwardForce), ForceMode.VelocityChange);
                    thrownThrowable.GetComponent<IThrowable>().Thrown(currentThrowable);
                    playerResources.playerAmmo.Find(a => a.ammoType == currentThrowable.ammoType).amount--;
                    //UpdateUI();
                }
            }

        }
        public void OnSwapThrowableInput(InputValue value)
        {
            currentThrowableIndex = (currentThrowableIndex + 1) % playerResources.throwables.Count;
            currentThrowable = playerResources.throwables[currentThrowableIndex];
        }
        void LateUpdate()
        {
            if (!currentGun || currentGun.gunInfo.ammoType != AmmoType.Snipe) return;
    
            Vector3 targetWorldPos;
            Quaternion targetWorldRot;

            if (_isActuallyAiming)
            {
                targetWorldRot = fpsCam.transform.rotation * Quaternion.Inverse(_scopeLocalRot);
                Vector3 targetScopeWorldPos = fpsCam.transform.position + fpsCam.transform.forward * aimDistance;
                targetWorldPos = targetScopeWorldPos - (targetWorldRot * _scopeLocalPos);
        
                currentGun.SetRestRotation(Quaternion.Inverse(holdPivot.rotation) * targetWorldRot);
            }
            else
            {
                targetWorldPos = holdPivot.TransformPoint(_hipfireLocalPos);
                targetWorldRot = holdPivot.rotation * _hipfireLocalRot;
                currentGun.SetRestRotation(_hipfireLocalRot);
            }
            float interpolationFactor = _isActuallyAiming ? aimSpeed : aimSpeed * 1.4f;
            currentGun.transform.position = Vector3.Lerp(currentGun.transform.position, targetWorldPos, Time.deltaTime * interpolationFactor);

            if (!currentGun.IsRecoiling)
            {
                currentGun.transform.rotation = Quaternion.Slerp(currentGun.transform.rotation, targetWorldRot, Time.deltaTime * interpolationFactor);
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
            if (currentGun)
            {
                currentGun.gameObject.SetActive(false);
                if(_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
                currentGun = null;
            }

            if (!gun) return;
            gun.gameObject.SetActive(true);
            currentGun = gun;
            gun.GetComponent<GrabbableObject>().Grab(holdPivot);
            if(_reloadCoroutine!=null)
                StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
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
            if (_reloadCoroutine != null)
                return;
            if (!currentGun) return;
            if (currentGun.ammoInMag <= 0) return;
            currentGun.PerformShoot();
            currentGun.ammoInMag -= 1;
            shots++;
            UpdateUI();
            
            Ray ray = fpsCam.ScreenPointToRay(uiCrosshair.crosshairRect.position);
            Vector3 direction = ray.direction;
            
            // spread
            float x = UnityEngine.Random.Range(-currentSpread, currentSpread);
            float y = UnityEngine.Random.Range(-currentSpread, currentSpread);
            direction += fpsCam.transform.right * x + fpsCam.transform.up * y;
            direction.Normalize();
            
            if (Physics.Raycast(ray.origin, direction, out RaycastHit hit))
            {
                if (hit.transform.TryGetComponent(out EnemyHitbox hitbox))
                {
                    BulletImpactManager.Instance.SpawnImpact(hit.point, hit.normal, BulletImpactManager.ImpactType.Flesh);

                    EnemyHealth enemy = hitbox.GetEnemyHealth();
                    if (!(enemy == null || enemy.IsDead))
                    {
                        if (hitDing) PlayHitSound();
                        if(uiCrosshair.isActiveAndEnabled)
                            uiCrosshair.ShowHit();

                        float distance = hit.distance;
                        float falloff = currentGun.gunInfo.damageFalloff.Evaluate(distance / 100f);
                        float finalDamage = currentGun.gunInfo.flatDamage * falloff * hitbox.GetDamageMultiplier();
                        Debug.Log(hitbox.hitboxType);
                        enemy.Damage(finalDamage);
                        hits++;
                    }
                }
                else
                {
                    BulletImpactManager.Instance.SpawnImpact(hit.point, hit.normal, BulletImpactManager.ImpactType.Ground);
                }
            }
            // apply recoil after shooting, so first shoot is accurate
            uiCrosshair.ApplyRecoil(currentGun.gunInfo);
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
                
                Gun weaponInCurrentSlot = playerResources.weapons[_currentSlot];
                if (weaponInCurrentSlot)
                {
                    int freeSlot = WhatSlotAvailable();
                    if (freeSlot != -1)
                    {
                        playerResources.weapons[freeSlot] = gun;
                        playerResources.PutWeaponInInventoryObject(gun.gameObject);
                        gun.gameObject.SetActive(false);
                    }
                    else
                    {
                        weaponInCurrentSlot.GetComponent<GrabbableObject>().Drop();
                    }
                }
                else
                {
                    playerResources.weapons[_currentSlot] = gun;
                    playerResources.PutWeaponInInventoryObject(gun.gameObject);
                    SwitchWeapons(playerResources.weapons[_currentSlot]);
                    gun.PickedUp();

                }

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
        void Reload()
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
            
            UpdateUI();
        }
        IEnumerator ReloadCoroutine(float reloadTime)
        {
            currentGun.Reload(reloadTime);
            yield return new WaitForSecondsRealtime(reloadTime);
            Reload();
            _reloadCoroutine = null;
        }

        void PlayHitSound()
        {
            hitDingInstances[hitDingIter].Play();
            hitDingIter = (hitDingIter + 1) % hitDingInstances.Length;
        }

        int WhatSlotAvailable()
        {
            for (int i = 0; i < playerResources.weapons.Count; i++)
            {
                if (!playerResources.weapons[i])
                    return i;
            }
            return -1;
        }
    }
}