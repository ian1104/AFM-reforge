# AFM Reforge — PC방 Runtime Test Guide

## 1. 준비물

- Windows x64 PC
- Albion Online
- AFM Reforge Windows package

이 패키지는 .NET SDK, Visual Studio, Python, Node.js, npm/pnpm, Git이 설치된 개발환경을 전제로 하지 않는다.

### Windows 버전 주의

AFM Reforge는 현재 `net10.0`을 대상으로 한다. Self-contained publish이므로 별도의 .NET Runtime 설치는 필요하지 않지만, 실제 Windows OS 호환성은 별도 문제다. Microsoft의 현재 .NET 10 Windows 지원표를 확인하고 PC방 OS가 지원 대상인지 확인한다.

## 2. 설치

1. ZIP을 원하는 폴더에 압축 해제한다.
2. 설치 프로그램은 사용하지 않는다.
3. Program Files 같은 시스템 디렉터리에 설치하지 않는다.

## 3. 실행

권장:

`START-AFM-REFORGE.bat` 실행

Launcher는 AFMReforge.exe를 시작한 뒤 `http://localhost:5180/`을 기본 브라우저에서 연다.

직접 실행하려면 `AFMReforge.exe`를 실행한 뒤 브라우저에서 다음 주소를 연다.

`http://localhost:5180/`

## 4. Demo Mode

Demo Mode는 합성 데이터 검증용이다.

- `AFM_REFORGE_DEMO=1`: Demo Mode
- 기본값: Demo OFF

Demo DB와 Runtime DB는 서로 다른 파일을 사용한다.

기본 DB 위치:

`%LOCALAPPDATA%\AFMReforge\afm-reforge.db`

Demo DB:

`%LOCALAPPDATA%\AFMReforge\afm-reforge-demo.db`

실제 Runtime Integration Test에서는 `AFM_REFORGE_DEMO`를 설정하지 않는다.

`AFM_REFORGE_DB`를 지정하면 해당 경로를 사용한다.

## 5. 실제 Runtime Test

현재 저장소의 App은 Pre-Runtime UI 단계이며, 실제 Albion packet capture → AFMDataClientCore → Adapter 연결은 아직 Runtime Integration Gate에서 검증되지 않았다. 따라서 아래 절차는 패키지가 준비된 이후의 Gate 절차이며, 수행하지 않은 결과를 성공으로 기록하지 않는다.

### UI / persistence smoke test

1. 프로그램 실행
2. Diagnostics 확인
3. Demo OFF 상태 확인
4. UI가 정상적으로 열리는지 확인
5. DB가 생성되는지 확인
6. 종료
7. 재실행
8. 동일 DB가 재사용되는지 확인

### Runtime Integration Gate

1. AFM Reforge 실행
2. Diagnostics 확인
3. Albion Online 실행
4. Market 화면 접근
5. Diagnostics 변화 확인
6. Market Response 확인
7. Observation 확인
8. Records 확인
9. 동일 Market 재조회
10. 다른 Item 조회
11. 다른 City 조회
12. DB persistence 확인

## 6. 결과 기록

```text
AFM Core:
Receiver:

First Response:
Response Kind:
Record Count:

Second Response:
Response Kind:
Record Count:

Observation:
Observation ID:
Record Count:

Persistence:
Persisted Count:

ItemTypeId:
LocationId:
Quality:
Enchantment:

CapturedAt behavior:

AFM coexistence:

Errors:
```

## 7. Diagnostics 확인 항목

가능한 경우 다음 값을 기록한다.

- AFM Core
- Receiver
- Last Response Kind
- Last Response Time
- Last Response Record Count
- Last Adapter Record Count
- Last Observation ID
- Last Observation Record Count
- Last Persistence Count
- Last Error

## 8. Packet Capture / 권한

이 패키지는 self-contained .NET 애플리케이션이라는 사실만 보장한다. AFMDataClientCore의 실제 packet capture 경로가 요구하는 OS 권한, 네트워크 캡처 구성, native dependency 또는 driver가 있다면 그것은 별도의 Runtime 사전조건이다.

현재 AFMDataClientCore는 프로토콜/디코딩 중심의 managed library이며, README상 native packet capture와 플랫폼 권한은 host/client 책임으로 분리되어 있다. 따라서 PC방에서 실제 capture가 가능한지는 이 패키지 자체만으로 확정하지 않는다.

## 9. Verification Status

### CONFIRMED

- App target: `net10.0`
- App host: ASP.NET Core Minimal API
- Windows x64 self-contained publish profile is defined.
- Publish is intentionally not single-file and not trimmed.
- Runtime DB default is under `%LOCALAPPDATA%\AFMReforge\`.
- Demo DB and runtime DB use separate filenames.
- AFMDataClientCore is consumed as a Git submodule project reference.

### INFERRED

- A normal writable user directory should be suitable for the default SQLite path.
- The publish output is intended to run without a system-installed .NET runtime once a successful self-contained publish is produced.

### UNVERIFIED

- Actual Windows publish execution in this environment.
- Clean Windows PC execution.
- Actual Albion Online runtime capture.
- Packet capture permissions/driver requirements on a PC방 PC.
- Actual Market Response reception.
- Runtime Adapter → Observation → SQLite path.
- End-to-end Runtime Integration Gate.

If actual Albion Online runtime has not been executed, report:

`Runtime Integration: UNVERIFIED`
