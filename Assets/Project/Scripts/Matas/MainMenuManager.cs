using System.Collections;
using UnityEngine;

namespace Project.Scripts.Matas
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject _mainCharacter;
        [SerializeField] private GameObject _tilesManager;
        [SerializeField] private GameObject _cursor;
        [SerializeField] private GameObject _menuUIOverlay;
        [SerializeField] private GameObject _creditsUIOverlay;

        [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _distance = 5f;
        [SerializeField] private float _duration = 1f;

        private GameObject _mainCamera;
        private Camera _mainCameraComponent;
        private Vector3 _startPos;

        private void Start()
        {
            _mainCamera = Camera.main.gameObject;
            _mainCameraComponent = Camera.main;
            _startPos = _menuUIOverlay.transform.position;
        }

        public void NewGame()
        {
            _mainCharacter.GetComponent<CharacterController2D>().enabled = true;
            _tilesManager.SetActive(true);
            _cursor.SetActive(false);
            SetupCamera();
            MoveMenuLeft();
        }

        public void ShowCredits()
        {
            _creditsUIOverlay.SetActive(!_creditsUIOverlay.activeSelf);
        }

        public void Quit()
        {
#if UNITY_WEBGL
            return;
#endif
            Application.Quit();
        }

        private void SetupCamera()
        {
            _mainCamera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            _mainCamera.GetComponent<CameraFollowPlayer>().enabled = true;
            StartCoroutine(ZoomOutRoutine());
        }

        private IEnumerator ZoomOutRoutine()
        {
            var time = 0f;
            var targetZoom = 7f;

            while (time < _duration)
            {
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / _duration);

                var curveValue = _moveCurve.Evaluate(t);
                _mainCameraComponent.orthographicSize = Mathf.LerpUnclamped(1f, targetZoom, curveValue);

                yield return null;
            }

            _mainCameraComponent.orthographicSize = targetZoom;
        }

        private void MoveMenuLeft()
        {
            StartCoroutine(MoveMenuLeftRoutine());
        }

        private IEnumerator MoveMenuLeftRoutine()
        {
            var time = 0f;
            var targetPos = _startPos + Vector3.left * _distance;

            while (time < _duration)
            {
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / _duration);

                var curveValue = _moveCurve.Evaluate(t);
                _menuUIOverlay.transform.position = Vector2.LerpUnclamped(_startPos, targetPos, curveValue);

                yield return null;
            }

            _menuUIOverlay.SetActive(false);
            _menuUIOverlay.transform.position = targetPos;
        }
    }
}