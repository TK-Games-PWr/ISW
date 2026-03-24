using System.Collections;
using System.Linq;
using JetBrains.Annotations;
using TK_Shared._3DPlayerMovement;
using UnityEngine;

namespace PlayerShootingSystem
{
    public class Gun : MonoBehaviour
    {
        static readonly int ReloadAnimation = Animator.StringToHash("Reload");
        public GunInfo gunInfo;
        public int ammoInMag;
        [SerializeField] AudioSource fireSound;
        [SerializeField] public Transform scopeQuad;
        AudioSource[] fireSoundInstances =  new AudioSource[5];
        int fireSoundIter = 0;
        [SerializeField] AudioSource reloadStartSound;
        [SerializeField] AudioSource reloadEndSound;
        [SerializeField] Animator animator;
        
        [Header("Gun Recoil")]
        public float recoilAmount = 8f;       
        public float recoilSide = 2f;         
        public float recoilSpeed = 12f;       
        public float returnSpeed = 5f;

        [Header("Slide Recoil")] 
        [SerializeField] [CanBeNull] ParticleSystem gunParticle;
        public Transform slide;
        public float slideBackAmount = 0.12f;
        public float slideBackSpeed = 25f;
        public float slideReturnSpeed = 15f;
        
        [SerializeField] LayerMask damageLayerMask;
        public Nade equippedNade;
        Vector3 _originalSlidePos;
        Quaternion _restRot;
        public bool IsRecoiling { get; private set; }
        Coroutine _slideCoroutine;
        Coroutine _recoilCoroutine;
        Coroutine _cookCoroutine;

        void Start()
        {
            if (slide != null)
            {
                _originalSlidePos = slide.localPosition;
            }
            if(fireSound)
            {
                fireSoundInstances[0] = fireSound;
                for (int i = 0; i < 4; i++)
                {
                    GameObject copy = Instantiate(fireSound.gameObject, fireSound.transform.parent);
                    copy.GetComponent<AudioSource>().pitch = Random.Range(0.9f, 1.1f);
                    fireSoundInstances[i+1] = copy.GetComponent<AudioSource>();
                }
            }

            TryGetComponent(out animator);
        }
        public void SetRestRotation(Quaternion rot)
        {
            _restRot = rot;
        }
        public void PickedUp()
        {
            _restRot = transform.localRotation;
        }
        public void PerformShoot()
        {
            if (gunParticle) gunParticle.Play();
            if (fireSound) PlayShootSound();
            if (_slideCoroutine != null)
                StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlideRecoilCoroutine());
            if (_recoilCoroutine != null) StopCoroutine(_recoilCoroutine);
            _recoilCoroutine = StartCoroutine(RecoilCoroutine());
        }

        public void Reload(float reloadTime=1f)
        {
            if (!animator) return;
            animator.speed = 1f / reloadTime;
            animator.SetTrigger(ReloadAnimation);
        }

        public void ReloadStartAnimCallback()
        {
            reloadStartSound.Play();
        }
        
        public void ReloadEndAnimCallback()
        {
            reloadEndSound.Play();
        }

        public void CookNade()
        {
            StartCoroutine(equippedNade.CookCoroutine(gunInfo, damageLayerMask));
        }
        IEnumerator RecoilCoroutine()
        {
            IsRecoiling = true;
            float side = UnityEngine.Random.Range(-recoilSide, recoilSide);
            
            Quaternion kickRotation = Quaternion.Euler(recoilAmount, side, 0);

            float elapsed = 0;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                transform.localRotation = Quaternion.Slerp(transform.localRotation, _restRot * kickRotation, Time.deltaTime * recoilSpeed);
                yield return null;
            }

            while (Quaternion.Angle(transform.localRotation, _restRot) > 0.1f)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, _restRot, Time.deltaTime * returnSpeed);
                yield return null;
            }

            transform.localRotation = _restRot;
            IsRecoiling = false;
        }

        IEnumerator SlideRecoilCoroutine()
        {
            if (!slide) yield break;

            Vector3 backPos = _originalSlidePos + Vector3.forward * slideBackAmount;

            while (Vector3.Distance(slide.localPosition, backPos) > 0.001f)
            {
                slide.localPosition = Vector3.Lerp(slide.localPosition, backPos, Time.deltaTime * slideBackSpeed);
                yield return null;
            }
            slide.localPosition = backPos;

            while (Vector3.Distance(slide.localPosition, _originalSlidePos) > 0.001f)
            {
                slide.localPosition = Vector3.Lerp(slide.localPosition, _originalSlidePos, Time.deltaTime * slideReturnSpeed);
                yield return null;
            }
            slide.localPosition = _originalSlidePos;
        }

        private void PlayShootSound()
        {
            fireSoundInstances[fireSoundIter].Play();
            fireSoundIter = (fireSoundIter + 1) % fireSoundInstances.Length;
        }
    }
}
