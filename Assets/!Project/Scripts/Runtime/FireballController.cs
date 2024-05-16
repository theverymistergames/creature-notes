using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Core;
using UnityEngine;
using UnityEngine.UI;


public class FireballController : MonoBehaviour, IActorComponent {
    public float chargeTime = 4;
    public float burnTime = 10;
    public float burnedTime = 10;
    public GameObject scale;
    public GameObject scaleBurning;
    public GameObject scaleContainer;
    public Color chargingColor;
    public Color chargedColor;
    public GameObject projectile;

    [SerializeField] private ActorAction _onFireAction;
    
    [SerializeField] private AudioSource fireSound;
    [SerializeField] private AudioSource chargedSound;
    [SerializeField] private AudioSource throwSound;

    private IActor _actor;
    private CancellationTokenSource _enableCts;

    private bool _charging, _burning, _burned;
    private float _chargeProgressTime = 0, _burnProgressTime = 0;
    private RectTransform _scaleTransform, _scaleBurningTransform;
    private Image _scaleImage;
    private Vector2 _scaleSize;

    private Vector2 _tmp;

    private Light _light;
    private float lightIntensity;

    void IActorComponent.OnAwake(IActor actor) {
        _actor = actor;
    }

    private void Start() {
        _scaleImage = scale.GetComponent<Image>();
        _scaleTransform = scale.GetComponent<RectTransform>();
        _scaleSize = _scaleTransform.sizeDelta;
        _scaleImage.color = chargingColor;
        _scaleBurningTransform = scaleBurning.GetComponent<RectTransform>();
        
        scaleContainer.SetActive(false);

        _light = GetComponent<Light>();
        lightIntensity = _light.intensity;
        _light.intensity = 0;
    }

    private void ProceedUpdate() {
        var currentProgress = _chargeProgressTime / chargeTime;
        _light.intensity = currentProgress * lightIntensity;
        _tmp.Set(_scaleSize[0] * currentProgress, _scaleSize[1]);
        _scaleTransform.sizeDelta = _tmp;

        if (currentProgress == 1) {
            _scaleImage.color = chargedColor;
        } else {
            _scaleImage.color = chargingColor;
        }

        if (currentProgress == 0) {
            scaleContainer.SetActive(false);
        }
        
        _tmp.Set(_scaleSize[0] * _burnProgressTime / burnTime, _scaleSize[1]);
        _scaleBurningTransform.sizeDelta = _tmp;
    }

    IEnumerator BurnedRoutine() {
        _chargeProgressTime = 0;
        
        for (int i = 0; i < 5; i++) {
            scaleContainer.SetActive(false);
            yield return new WaitForSeconds(burnedTime / (5 * 2));
            
            scaleContainer.SetActive(true);
            yield return new WaitForSeconds(burnedTime / (5 * 2));
        }

        Clear();
        _charging = false;
    }

    void Clear() {
        _chargeProgressTime = 0;
        _burnProgressTime = 0;
        _burned = false;
        _burning = false;
    }

    void Strike() {
        throwSound.Play();
        
        _onFireAction.Apply(_actor, _enableCts.Token).Forget();
        
        Clear();
        
        var container = Instantiate(projectile, gameObject.transform);
        container.transform.localPosition = new Vector3(0, 0, 0.1f);
        container.transform.SetParent(null);

        var strikeContainer = container.GetComponent<ProjectileContainer>();
        
        var direction = (container.transform.position - gameObject.transform.position).normalized;
        
        strikeContainer.Strike(direction);
    }

    private void Update() {
        if (_burned) {
            return;
        }
        
        if (Input.GetMouseButtonDown(0) && _chargeProgressTime == chargeTime) {
            Strike();
        }
        
        if (Input.GetMouseButtonDown(1)) {
            if (!_charging) {
                fireSound.Play();
            }

            scaleContainer.SetActive(true);
            _charging = true;
        }
        
        if (Input.GetMouseButtonUp(1)) {
            _charging = false;
            Clear();
        }

        if (_burning && _charging) {
            _burnProgressTime += Time.deltaTime;

            if (_burnProgressTime >= burnTime) {
                StartCoroutine(BurnedRoutine());
                _burned = true;
            }
        } else {
            if (_burnProgressTime > 0) {
                _burnProgressTime -= Time.deltaTime * 3;
                _burnProgressTime = Mathf.Max(0, _burnProgressTime);
            }

            if (_burnProgressTime == 0) {
                _burning = false;
                _chargeProgressTime += Time.deltaTime * (_charging ? 1 : -3);
                _chargeProgressTime = Mathf.Clamp(_chargeProgressTime, 0, chargeTime);
            }
        }
        
        ProceedUpdate();

        if (_chargeProgressTime == chargeTime) {
            if (!_burning) chargedSound.Play();
            _burning = true;
        } else if (_chargeProgressTime == 0) {
            
        }
    }

    private void OnEnable() {
        _enableCts?.Cancel();
        _enableCts?.Dispose();
        _enableCts = new CancellationTokenSource();
    }

    private void OnDisable() {
        _enableCts?.Cancel();
        _enableCts?.Dispose();
        _enableCts = null;
    }
}
