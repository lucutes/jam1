using UnityEngine;
using UnityEngine.UI;

public class CustomCursorController : MonoBehaviour
{
    [Header("UI Cursor Image")] [SerializeField]
    private Image _cursorImage;

    [Header("Idle Animation Frames")] [SerializeField]
    private Sprite[] _idleFrames;

    [Header("Click Hold Animation Frames")] [SerializeField]
    private Sprite[] _clickFrames;

    [Header("Animation Settings")] [SerializeField]
    private float _frameRate = 0.1f;

    private int _frameIndex;

    private bool _isClicking;

    private float _timer;

    private void Start()
    {
        Cursor.visible = false;

        _cursorImage.rectTransform.pivot = new Vector2(0f, 1f); // Top-left
    }

    private void Update()
    {
        UpdateCursorPosition();
        UpdateClickState();
        AnimateCursor();
    }

    private void UpdateCursorPosition()
    {
        _cursorImage.transform.position = Input.mousePosition;
    }

    private void UpdateClickState()
    {
        _isClicking = Input.GetMouseButton(0);
    }

    private void AnimateCursor()
    {
        _timer += Time.unscaledDeltaTime;

        if (_timer < _frameRate) return;

        _timer = 0f;
        _frameIndex++;

        var currentFrames = _isClicking ? _clickFrames : _idleFrames;

        if (currentFrames == null || currentFrames.Length == 0) return;

        if (_frameIndex >= currentFrames.Length)
            _frameIndex = 0;

        _cursorImage.sprite = currentFrames[_frameIndex];
    }
}