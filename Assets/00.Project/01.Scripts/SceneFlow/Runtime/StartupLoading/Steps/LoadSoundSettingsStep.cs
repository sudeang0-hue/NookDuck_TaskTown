using System.Collections;
using UnityEngine;

namespace TaskTown.SceneFlow
{
    [CreateAssetMenu(fileName = "LoadSoundSettingsStep", menuName = "TaskTown/Scene Flow/Steps/Load Sound Settings")]
    public sealed class LoadSoundSettingsStep : StartupLoadStepSO
    {
        protected override IEnumerator ExecuteStep(StartupLoadStepContext context)
        {
            context.ReportProgress(0f);

            // 기존 저장소는 파일 없음/손상 상황에서 기본값으로 복구합니다.
            SoundSettingsData settingsData = SoundSettingsStore.Load();
            if (settingsData == null)
            {
                context.Recover("사운드 설정을 불러오지 못해 기본 설정을 사용합니다.");
            }
            else
            {
                context.Shared.Set(settingsData);
            }

            context.ReportProgress(1f);
            yield break;
        }
    }
}
