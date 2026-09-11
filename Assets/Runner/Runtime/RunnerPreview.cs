using UnityEngine;

namespace PixelRunner
{
    // Saved scene preview is replaced by the pooled runtime sections before the first physics step.
    [DefaultExecutionOrder(-300)]
    public sealed class RunnerPreview : MonoBehaviour
    {
        void Awake() => gameObject.SetActive(false);
    }
}
