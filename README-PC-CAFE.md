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

현재 App의 실제 기본 Host 설정은 다음과 같다.

```text
http://localhost:5180
```

`ASPNETCORE_URLS` 환경변수가 지정되어 있으면 해당 값이 우선한다.

브라우저에서 실제 사용 중인 주소를 열어 UI를 확인한다.

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

CI에서 `dotnet publish -c Release -r win-x64 --self-contained true`가 성공했고, 실제 publish 산출물에 `AFMReforge.App.runtimeconfig.json`, `hostfxr.dll`, `hostpolicy.dll`, `coreclr.dll` 및 Windows runtime 파일이 포함되어 있음을 확인했다.

따라서 **별도의 .NET Runtime 설치를 전제로 하지 않는다.** 이것은 self-contained 배포본 기준이며, 실제 PC방에서 실행되는 것까지 검증한 것은 아니다.

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

현재 PC방 ZIP에는 Npcap 설치 프로그램을 포함하지 않는다.

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

## 7. Runtime 결과 기록 양식

실제 PC방에서 다음 값을 기록한다.

```text
AFM Core:
Receiver:

First Response:
Response Kind:
Record Count:

Adapter Record Count:

Observation ID:
Observation Record Count:

Persistence Count:

ItemTypeId:
LocationId:
Quality:
Enchantment:
CapturedAt:

Second Query:

Different Item:

Different City:

AFM coexistence:

Errors:
```

예를 들어 Response Records / Adapter Records / Observation Records / Persistence Count가 모두 같은 값으로 관측되더라도, 그것만으로 시장 의미가 정확하다고 결론 내리지 않는다.

## 8. 현재 검증 상태

| 항목 | 상태 |
|---|---|
| Windows x64 publish 설정 | CONFIRMED |
| Self-contained publish 실행 | CONFIRMED |
| ZIP 생성 | CONFIRMED |
| ZIP artifact 생성 | CONFIRMED |
| ZIP 내부 구조 확인 | CONFIRMED |
| .NET Runtime 별도 설치 | Self-contained 산출물 기준 불필요 |
| Npcap 필요성 | AFM Windows capture 경로 기준 REQUIRED/DEPENDENCY |
| 관리자 권한 | 설치/환경에 따라 필요 가능; 앱 자체는 UNVERIFIED |
| localhost 기본 포트 | CONFIRMED: 5180 |
| 실제 PC방 실행 | UNVERIFIED |
| 실제 packet capture | UNVERIFIED |
| 실제 Market Response | UNVERIFIED |
| Runtime Integration Gate | NOT PASSED |

## 9. CI 배포 검증 결과

최종 배포 workflow가 다음 전체 pipeline을 통과해야 배포 성공으로 취급한다.

```text
Checkout                 PASS
.NET setup               PASS
Restore                  PASS
Build                    PASS
Test                     PASS
win-x64 self-contained   PASS
ZIP                      PASS
Artifact upload          PASS
```

검증된 테스트 프로젝트 결과:

```text
AFMReforge.Core.Tests             48 passed / 0 failed
AFMReforge.Adapter.AFM.Tests       7 passed / 0 failed
AFMReforge.Infrastructure.Tests  28 passed / 0 failed
Total                             83 passed / 0 failed
```

최종 workflow artifact에는 다음이 포함된다.

- `AFMReforge.App.exe`
- `AFMReforge.App.dll`
- `AFMReforge.App.deps.json`
- `AFMReforge.App.runtimeconfig.json`
- `AFMDataClient.Core.dll`
- `AFMReforge.Adapter.AFM.dll`
- `AFMReforge.Core.dll`
- `AFMReforge.Infrastructure.dll`
- `e_sqlite3.dll`
- .NET/ASP.NET Core runtime 파일
- `README-PC-CAFE.md`

PE 검사 결과 `AFMReforge.App.exe`는 Windows x64(PE32+) 실행 파일이다.

개발용 `.git`, `obj`, `Debug`, `node_modules` 항목은 publish ZIP에서 확인되지 않았다.

실제 artifact의 정확한 크기와 SHA256은 해당 GitHub Actions run의 artifact metadata를 기준으로 확인한다. README 자체에 이전 run의 hash를 고정하지 않는다.

## 10. Publish 설정

현재 publish 명령은 다음 조건으로 실행된다.

```text
Target Framework: net10.0
RID: win-x64
Configuration: Release
Self-contained: true
Single-file: false
Trimmed: false
```

Single-file과 Trimmed는 현재 publish 설정에서 활성화하지 않았다. 실제 산출물도 개별 DLL 및 runtime 파일을 포함하는 일반 self-contained 디렉터리 배포 형태다.

## 11. 개발환경 요구사항과 PC방 요구사항 구분

### 빌드/개발에만 필요한 것

- .NET 10 SDK: 개발/CI 빌드용
- Git: 소스 checkout용
- Node.js: 현재 PC방 실행에 필요하지 않음
- Visual Studio: 현재 PC방 실행에 필요하지 않음
- Python: 현재 PC방 실행에 필요하지 않음

### PC방 실행 시 별도 확인할 것

- Windows x64 환경
- AFMReforge.App.exe 실행 가능 여부
- 로컬 브라우저에서 localhost 접속 가능 여부
- Npcap / packet capture 환경
- Npcap 설치 권한 및 PC방 정책
- Albion Online 실행 환경

## 12. 범위 제한

현재 단계에서는 다음 기능을 추가하거나 확정하지 않는다.

- Snapshot
- Price History
- Trend
- Market State
- Market Price 추론
- Best Price
- Buy/Sell Judgment
- Arbitrage
- ROI
- Recommendation

또한 `Offers = Sell`, `Requests = Buy`라는 시장 의미를 임의로 확정하지 않는다.

실제 runtime에서 관측되는 response와 record 구조를 먼저 확인한 뒤 다음 개발 단계를 결정한다.

## 13. 문제 발생 시 원칙

Fake/Demo 데이터를 이용해 Runtime Gate를 통과한 것으로 처리하지 않는다.

실제 packet capture → response → adapter → observation → persistence 흐름이 확인되지 않았다면 상태는 `UNVERIFIED` 또는 `NOT PASSED`로 유지한다.
