using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace BBJ.Work
{
    public sealed class WorkExecutionContext : IDisposable
    {
        private readonly CancellationTokenSource _cancelCts;
        private UniTaskCompletionSource _pauseGate;

        public CancellationToken Token    => _cancelCts.Token;
        public bool              IsPaused => _pauseGate != null;

        public WorkExecutionContext()
        {
            _cancelCts = new CancellationTokenSource();
        }

        internal void HardCancel() => _cancelCts.Cancel();

        public void Pause()
        {
            if (_pauseGate == null)
                _pauseGate = new UniTaskCompletionSource();
        }

        public void Resume()
        {
            var gate = _pauseGate;
            _pauseGate = null;
            gate?.TrySetResult();
        }

        public UniTask WaitIfPausedAsync(CancellationToken waitCancelToken = default)
        {
            if (_pauseGate == null) return UniTask.CompletedTask;
            return _pauseGate.Task.AttachExternalCancellation(waitCancelToken);
        }

        public void Dispose()
        {
            _pauseGate?.TrySetCanceled();
            _pauseGate = null;
            _cancelCts.Dispose();
        }
    }
}
