# 더 나은 수분·포만감 HUD

[English](README.md) | [简体中文](README.zh-CN.md) | [Français](README.fr.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

**Escape from Duckov**용 HUD 모드: 수분·포만감 링 옆에 부족량과 **현재 / 최대** 값을 표시합니다.

> **v2.1.0** — DLL 하나, **게임 10개 언어**, **F10 단축키 변경 가능**, **선행 모드 불필요**.

## 주요 기능

- **단축키 변경 가능** — 기본은 F10으로 패널을 엽니다. 다른 모드와 겹치면 '설정 → HUD 수치'에서 바꿀 수 있습니다.
- **게임 10개 언어** — 게임 언어를 자동으로 따릅니다: 영어, 简体中文, 繁體中文, 日本語, 한국어, 프랑스어, 독일어, 스페인어, 포르투갈어, 러시아어.
- **선행 모드 불필요** — ModConfig / ModSetting / HarmonyLib 필요 없음.
- **게임 내 F10 패널** — 게임 일시정지, 마우스 사용 가능, 위치 / 글자 크기 / 형식 실시간 조정, 자동 저장.
- 값별 초기화, 낮은 값 경고, 퍼센트 / 소수, 설정 화면의 기본 탭.

## 설치

1. [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771)에서 구독.
2. 게임 **Mods** 목록에서 활성화.
3. 세이브를 불러오고 `F10`으로 패널을 엽니다.

## 빌드

```powershell
dotnet build src/BetterHydrationEnergyHUD/BetterHydrationEnergyHUD.csproj -c Release
powershell -ExecutionPolicy Bypass -File verify.ps1 -Dll src/BetterHydrationEnergyHUD/bin/Release/BetterHydrationEnergyHUD.dll
```

프로젝트는 게임의 관리 DLL을 참조하지만 포함하지 않습니다. API·테스트·릴리스 노트는 `docs/`를 참고하세요.

## 링크

- Steam Workshop: <https://steamcommunity.com/sharedfiles/filedetails/?id=3591875771>
- 릴리스 노트: [docs/RELEASE_NOTES.md](docs/RELEASE_NOTES.md)
- 테스트 보고서: [docs/TEST_REPORT.md](docs/TEST_REPORT.md)

## 제작

— Dorei Hime ♥
