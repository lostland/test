# 🎬 VideoGrid - 20채널 동시 동영상 재생기

YouTube 등 인터넷 동영상을 **최대 20개 동시에** 그리드로 재생하는 Windows 전용 WPF 앱입니다.

## 📸 주요 기능

- **4열 × 5행** 그리드로 최대 20개 동영상 동시 표시
- YouTube URL 자동 embed 변환 지원
- 전체 재생 / 전체 정지 일괄 제어
- 개별 채널 닫기 버튼
- 다크 테마 UI

## 🛠️ 빌드 방법

### 요구사항
- Windows 10/11
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) 이상

### 빌드 & 실행

```bash
# 프로젝트 폴더에서
dotnet build
dotnet run
```

### 배포용 exe 생성

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
`bin\Release\net10.0-windows\win-x64\publish\VideoGrid.exe` 파일이 생성됩니다.

## 📖 사용법

1. 상단 URL 입력창에 YouTube 주소를 붙여넣기
2. **➕ 추가** 버튼 클릭 (또는 Enter)
3. 최대 20개까지 반복
4. **▶ 전체 재생** 버튼으로 일괄 재생

## 🔗 지원 URL 형식

| 형식 | 예시 |
|------|------|
| YouTube 일반 | `https://www.youtube.com/watch?v=XXXXX` |
| YouTube 단축 | `https://youtu.be/XXXXX` |
| YouTube embed | `https://www.youtube.com/embed/XXXXX` |
| 기타 직접 URL | iframe 지원 사이트 |

## ⚠️ 참고사항

- YouTube는 일부 동영상에서 embed 재생을 제한할 수 있습니다 (동영상 업로더 설정에 따라)
- 20개 동시 재생 시 PC 사양에 따라 성능 차이가 있을 수 있습니다
- Internet Explorer 기반 WebBrowser 컨트롤을 사용합니다 (레거시 WPF 방식)

## 📁 프로젝트 구조

```
VideoGrid/
├── VideoGrid.csproj     # 프로젝트 파일
├── App.xaml             # 앱 리소스 & 스타일
├── App.xaml.cs
├── MainWindow.xaml      # 메인 UI (그리드 레이아웃)
├── MainWindow.xaml.cs   # 로직 (URL 파싱, 재생 제어)
└── README.md
```
