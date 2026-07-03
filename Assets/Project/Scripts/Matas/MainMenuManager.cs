using System.Collections;
using UnityEngine;

namespace Project.Scripts.Matas
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject _menuObject;
        [SerializeField] private GameObject _mainCharacter;
        [SerializeField] private GameObject _tilesManager;
        [SerializeField] private GameObject _cursor;

        [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _distance = 5f;
        [SerializeField] private float _duration = 1f;

        private GameObject _mainCamera;
        private Vector3 _startPos;

        private void Start()
        {
            _mainCamera = Camera.main.gameObject;
            _startPos = _menuObject.transform.position;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                _mainCharacter.GetComponent<CharacterController2D>().enabled = true;
                _tilesManager.SetActive(true);
                _cursor.SetActive(false);
                SetupCamera();
                MoveMenuLeft();
            }
        }

        private void SetupCamera()
        {
            _mainCamera.GetComponent<Camera>().orthographic = true;
            _mainCamera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            _mainCamera.GetComponent<CameraFollowPlayer>().enabled = true;
        }

        private void MoveMenuLeft()
        {
            StopAllCoroutines();
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
                _menuObject.transform.position = Vector3.LerpUnclamped(_startPos, targetPos, curveValue);

                yield return null;
            }

            _menuObject.SetActive(false);
            _menuObject.transform.position = targetPos;
        }
    }
}