# 설정 UI 수정 방법

- 설정 버튼은 진입 후 `MainGameScene`의 오른쪽 위에만 표시됩니다. 시작 화면 `SampleScene`과 `LoadingScene`에는 표시되지 않습니다.
- 항목: Master/BGM/SFX 음량, 해상도, 전체 화면/창 모드.
- `SettingsUI.prefab`을 더블 클릭하면 기본 사각형 Image, TMP 글자, Button, Slider를 직접 수정할 수 있습니다.
- `OpenSettingsButton`: 오른쪽 위 버튼의 위치/크기를 RectTransform에서 조정합니다.
- `SettingsModal`을 켜면 편집 중 패널이 보입니다. 플레이를 시작하면 자동으로 닫힌 상태가 됩니다.
- `SettingsPanel`: 창 배경. 내부의 이름별 오브젝트에서 색상, 글자, 위치를 바꿉니다.
- 음량은 즉시 저장됩니다. 해상도와 화면 모드는 `Apply display`로 적용합니다. 적용하지 않고 닫은 화면 변경은 버립니다.
- `Reset audio`는 음량 3종만 초기화합니다. 화면 모드는 유지합니다.
- 해상도/전체 화면 실제 변경은 독립 실행 빌드에서 확인합니다. Unity Game 뷰에서는 값만 저장합니다.
- 키 바인딩과 마우스 감도 UI 및 저장/적용 기능은 제거했습니다. 이전 저장 파일에 해당 값이 있어도 사용하지 않습니다.

## 오디오 연결

현재 메인 게임 씬은 배경만 있고 플레이어와 오디오 소스가 없습니다.

- 이후 BGM/SFX AudioSource에 `SettingsAudioChannel`을 추가하고 `Channel`을 선택하면 각 음량이 반영됩니다. `Base Volume`은 해당 소스의 기본 음량입니다. Master는 `AudioListener.volume`으로 전체에 적용됩니다.
- 설정 창을 열면 시간 배율과 Player 액션 입력을 잠시 정지하고, 닫으면 이전 상태와 마우스 잠금 상태를 복원합니다.

`Tools > Game Settings > Create or Rebuild Settings Prefab`은 기본 배치를 다시 생성하므로 직접 편집한 프리팹을 덮어씁니다. 일반 수정에는 사용할 필요가 없습니다.

검증: `SettingsVerification.Run`은 설정 동작을, `SceneFlowVerification.Run`은 기존 시작/로딩 씬 흐름을 Unity 배치 모드에서 확인합니다.
