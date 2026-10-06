# AFM Reforge — Windows PC방 실행 가이드

## 1. 목적

이 패키지는 개발환경이 없는 Windows PC방에서 AFM Reforge의 현재 구현을 실행하고, 이후 실제 AFM Runtime Integration Gate를 검증하기 위한 **Windows x64 self-contained 배포본**이다.

이 패키지는 설치 프로그램이 아니다. ZIP을 압축 해제한 뒤 실행 파일을 직접 실행하는 방식이다.

현재 단계의 목표는 다음을 구분하는 것이다.

- `Publish 성공` = Windows x64 배포 산출물 생성 성공
- `실행 성공` = 프로세스가 시작되고 UI가 열림
- `Runtime 성공` = AFM Core/Receiver/packet capture가 실제로 초기화되고 Market Response가 관측됨

앞의 단계가 성공했다고 뒤의 단계까지 성공한 것으로 간주하지 않는다.

## 2. 패키지 구조

```text
AFM-Reforge-PC-Cafe/
├─ AFMReforge.App.exe
├─ ... published files ...
└─ README-PC-CAFE.md
```

`AFMReforge.App.exe`가 실행 파일이다.

## 3. PC방에서 실행하기

### Step 1 — ZIP 압축 해제

ZIP 전체를 원하는 로컬 폴더에 압축 해제한다.

예:

```text
C:\Users\Public\AFM-Reforge-PC-Cafe\
```

가능하면 OneDrive/네트워크 공유 폴더가 아닌 로컬 디스크에서 실행한다.

### Step 2 — 필요한 capture 환경 확인

AFM Data Client 계열의 Windows packet capture 경로는 Npcap 설치 여부에 의존한다. 원본 AFM Windows 클라이언트는 Npcap 설치 여부를 검사하고, 미설치 상태에서는 packet capture를 시작하지 않는다.

Npcap 공식 다운로드:

https://npcap.com/#download

설치가 필요한 경우 PC방 정책상 프로그램 설치가 허용되는지 먼저 확인한다. Npcap 설치에는 관리자 권한이 필요할 수 있다.

**중요:** AFM Reforge의 self-contained publish는 .NET 런타임을 패키지에 포함시키는 것이며, packet capture driver를 포함하거나 설치하는 기능이 아니다.

### Step 3 — 프로그램 실행

`AFMReforge.App.exe`를 실행한다.

현재 App은 ASP.NET Core 기반 UI를 호스팅하며 기본 주소는 다음과 같다.

```text
http://localhost:5180
```

브라우저에서 해당 주소를 열어 UI를 확인한다.

### Step 4 — Diagnostics 확인

`Diagnostics` 화면에서 실제 runtime 상태를 확인한다.

초기 상태가 `UNKNOWN`인 것은 정상이다. 실제 runtime 이벤트가 발생하기 전에는 `CONNECTED`, `READY`, `CAPTURED` 등의 상태를 임의로 표시하지 않는다.

확인할 항목:

- AFM Core
- Receiver
- Last ResponseKind
- Last ResponseTime
- Last ResponseRecordCount
- Last AdapterRecordCount
- Last ObservationId
- Last ObservationRecordCount
- Last PersistenceCount
- Last Error

## 4. 정상 실행의 판단 기준

### A. UI 실행만 확인

다음이 가능하면 UI 실행 자체는 성공이다.

- `AFMReforge.App.exe` 프로세스 시작
- `http://localhost:5180` 접속
- Overview/Observations/Diagnostics 등의 화면 표시

이것만으로 Runtime 성공으로 기록하지 않는다.

### B. Runtime Integration Gate 성공

실제 Albion 클라이언트 실행 상태에서 packet capture가 동작하고 다음 흐름이 확인되어야 한다.

```text
AFM Core
  ↓
Receiver
  ↓
Captured packet
  ↓
Market Response
  ↓
AFM Adapter
  ↓
MarketObservationInput
  ↓
MarketObservation
  ↓
Persistence
  ↓
Diagnostics / UI
```

특히 실제 `Market Response`가 들어와야 한다.

## 5. 권한 및 외부 의존성

### .NET Runtime

이 배포본은 `win-x64` self-contained publish를 목표로 한다. 따라서 별도의 .NET Runtime 설치를 전제로 하지 않는다.

단, 이것은 **publish 결과가 실제 self-contained로 생성된 경우에 한한다.** 배포본의 publish 로그/산출물을 확인하기 전에는 이를 실행 검증 결과로 간주하지 않는다.

### 관리자 권한

AFM Reforge 앱 자체가 항상 관리자 권한으로 실행되어야 한다고 현재 단계에서 확정하지 않는다.

다만 Npcap 설치나 Windows packet capture 환경 구성에는 관리자 권한이 필요할 수 있다. PC방에서는 설치 권한이 제한되어 있을 수 있으므로 이 부분은 실제 PC방에서 별도로 확인한다.

### Npcap / packet capture driver

Npcap은 self-contained .NET publish와 별개의 시스템 의존성이다.

즉:

```text
Self-contained publish
≠
Npcap 설치
≠
Packet capture 권한 확보
≠
실제 Market Response 수신
```

현재 패키지에는 Npcap 설치 프로그램을 임의로 포함하지 않는다.

## 6. PC방 테스트 순서

1. ZIP 압축 해제
2. Windows 보안/PC방 정책상 실행 가능 여부 확인
3. Npcap 설치 여부 확인
4. 필요한 경우 관리자 권한으로 Npcap 설치
5. `AFMReforge.App.exe` 실행
6. `http://localhost:5180` 접속
7. Diagnostics 화면 진입
8. Albion 실행/접속
9. 실제 Market 관련 행동을 수행
10. `Last ResponseKind` 및 record count 확인
11. Adapter record count 확인
12. Observation ID / record count 확인
13. Persistence count 확인
14. Error가 발생하면 내용을 보존

## 7. 기록해야 할 결과

### 성공

```text
[ ] Process starts
[ ] UI opens
[ ] AFM Core initialized
[ ] Receiver initialized
[ ] Packet capture works
[ ] Market Response observed
[ ] Adapter conversion observed
[ ] Observation created
[ ] Persistence completed
```

### 실패

다음 정보를 함께 기록한다.

- Windows 버전
- PC방 환경/정책
- Npcap 설치 여부
- 관리자 권한 여부
- 프로그램 시작 여부
- UI 접속 여부
- Diagnostics 화면의 마지막 상태
- Last Error
- 발생 시각
- 가능하면 관련 로그/스크린샷

## 8. 현재 검증 상태

| 항목 | 상태 |
|---|---|
| Windows x64 publish 설정 | CONFIGURED |
| Self-contained publish 실행 | UNVERIFIED until CI/package run |
| ZIP 생성 설정 | CONFIGURED |
| ZIP 실제 생성 | UNVERIFIED until CI/package run |
| .NET Runtime 별도 설치 필요성 | Self-contained 기준 NO; 실제 package 확인 필요 |
| Npcap 필요성 | AFM Windows capture 경로 기준 REQUIRED/DEPENDENCY |
| 관리자 권한 | 설치/환경에 따라 필요 가능; UNVERIFIED for this app host |
| 실제 PC방 실행 | UNVERIFIED |
| 실제 packet capture | UNVERIFIED |
| 실제 Market Response | UNVERIFIED |
| Runtime Integration Gate | NOT PASSED |

## 9. 범위 제한

현재 단계에서는 다음 기능을 추가하지 않는다.

- Snapshot
- Price History
- Trend
- Market State
- Market Price 추론
- 장기 분석 기능

실제 runtime에서 관측되는 response와 record 구조를 먼저 확인한 뒤 다음 개발 단계를 결정한다.

## 10. 문제 발생 시 원칙

Fake/Demo 데이터를 이용해 Runtime Gate를 통과한 것으로 처리하지 않는다.

실제 packet capture → response → adapter → observation → persistence 흐름이 확인되지 않았다면 상태는 `UNVERIFIED` 또는 `NOT PASSED`로 유지한다.
