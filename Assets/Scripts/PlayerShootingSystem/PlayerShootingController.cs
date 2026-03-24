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
        /*
         * Classification data section
         */
        [HideInInspector] 
        public int shots=0;
        [HideInInspector]
        public int hits = 0;
        
        [SerializeField] Transform cameraTransform;
        [SerializeField] GameObject crosshair;
        Camera fpsCam;
        [SerializeField] UICrosshair uiCrosshair;
        [SerializeField] AudioSource hitDing;
        private AudioSource[] hitDingInstances =  new AudioSource[5];
        private int hitDingIter = 0;
        [Header("Aiming")]
        [SerializeField] float aimSpeed = 12f;
        [SerializeField] float aimDistance = 0.35f;
        
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
        Vector3 _hipfireLocalPos;
        Quaternion _hipfireLocalRot;
        bool _isAiming;
        Quaternion _scopeLocalRot;
        Vector3 _scopeLocalPos;
        Coroutine _aimCoroutine;
        bool _isHoldingShoot;
        Coroutine _autoFireCoroutine;
        Coroutine _reloadCoroutine;
        bool _isActuallyAiming;
        private float currentSpread;
        
        void Awake()
        {
            PlayerActionsController.OnPickedUp += HandlePickup;
        }

        private void Start()
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
        }

        private void Update()
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
                    Vector3 throwingForce = fpsCam.transform.forward * throwForce + transform.up * throwUpwardForce;
                    rb.AddForce(throwingForce, ForceMode.Impulse);
                    currentGun.equippedNade = null;
                }
            }
        }

        public void OnReloadInput(InputValue value)
        {
            if (value.isPressed)
            {
                if (currentGun.gunInfo.ammoType == AmmoType.Nade)
                {
                    GameObject granade = Instantiate(nadePrefab, currentGun.transform.position, currentGun.transform.rotation);
                    granade.transform.SetParent(currentGun.transform);
                    Nade nade = granade.GetComponent<Nade>();
                    currentGun.slide = nade.pin;
                    currentGun.equippedNade = nade;
                }
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
                    crosshair.SetActive(false);
                    _hipfireLocalPos = currentGun.transform.localPosition;
                    _hipfireLocalRot = currentGun.transform.localRotation;
                    _scopeLocalRot = Quaternion.Inverse(currentGun.transform.rotation) * currentGun.scopeQuad.rotation;
                    _scopeLocalPos = currentGun.transform.InverseTransformPoint(currentGun.scopeQuad.position);
                }
                else
                {
                    crosshair.SetActive(true);
                }
            }
            else
            {
                crosshair.SetActive(true);
            }
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
            if(currentGun)
                currentGun.gameObject.SetActive(false);
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
                if (hit.transform.TryGetComponent(out EnemySystem.EnemyHealth enemy))
                {
                    BulletImpactManager.Instance.SpawnImpact(hit.point, hit.normal, BulletImpactManager.ImpactType.Flesh);
                    if (enemy.IsDead) return;
                    if (hitDing) PlayHitSound();
                    uiCrosshair.ShowHit();
                    float distance = hit.distance;
                    float multiplier = currentGun.gunInfo.damageFalloff.Evaluate(distance / 100f);
                    float finalDamage = currentGun.gunInfo.flatDamage * multiplier;
                    enemy.Damage(finalDamage);
                    hits++;
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
            yield return new WaitForSecondsRealtime(reloadTime);
            Reload();
            _reloadCoroutine = null;
        }
        private void PlayHitSound()
        {
            hitDingInstances[hitDingIter].Play();
            hitDingIter = (hitDingIter + 1) % hitDingInstances.Length;
        }
    }
}