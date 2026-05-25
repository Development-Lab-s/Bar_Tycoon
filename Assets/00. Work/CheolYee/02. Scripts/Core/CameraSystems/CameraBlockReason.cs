// CameraBlockReason.cs
// 우클릭/중클릭 드래그 + WASD/방향키 패닝
// 새 Input System 전용 (Unity 6.3 호환)

using System;

namespace _00._Work.CheolYee._02._Scripts.Core.CameraSystems
{
    [Flags]
    public enum CameraBlockReason
    {
        None = 0,
        NotMainScene = 1 << 0, // 메인 씬이 아닐 때 (스토리, 칵테일 씬 등)
        CameraMotion = 1 << 1  // CameraManager에 의해 카메라 강제 포커싱 연출 중일 때
    }
}