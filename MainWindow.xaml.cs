using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace VideoGrid
{
    public partial class MainWindow : Window
    {
        private const int MAX_SLOTS = 20;
        private List<ChannelEntry> _channels = new();

        private bool _mainInit  = false;
        private bool _hist1Init = false;
        private bool _hist2Init = false;

        private int _mainIndex  = -1;
        private int _hist1Index = -1;
        private int _hist2Index = -1;

        // 하단 슬롯 20개 (WebView2 미리 생성)
        private readonly ThumbSlot[] _slots = new ThumbSlot[MAX_SLOTS];

        public MainWindow()
        {
            InitializeComponent();
            BuildThumbSlots();
            InitWebViews();
            History1Border.MouseLeftButtonUp += (s, e) => { if (_hist1Index >= 0) FocusChannel(_hist1Index); };
            History2Border.MouseLeftButtonUp += (s, e) => { if (_hist2Index >= 0) FocusChannel(_hist2Index); };
        }

        // ── 하단 슬롯 20개 미리 생성 ──
        private void BuildThumbSlots()
        {
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                int idx = i;
                var border = new Border
                {
                    Background      = new SolidColorBrush(Color.FromRgb(0x08, 0x08, 0x14)),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2A)),
                    BorderThickness = new Thickness(1,1,1,1),
                    CornerRadius    = new CornerRadius(6,6,6,6),
                    Margin          = new Thickness(2,2,2,2),
                    Cursor          = Cursors.Hand,
                    ClipToBounds    = true
                };

                var grid = new Grid();

                // 빈 슬롯 번호 표시
                var emptyLabel = new TextBlock
                {
                    Text = $"{i + 1}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x44)),
                    FontSize = 13, FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center
                };

                var wv = new WebView2 { Visibility = Visibility.Collapsed, IsHitTestVisible = false };

                // 채널 번호 배지
                var badge = new Border
                {
                    Background      = new SolidColorBrush(Color.FromArgb(0xCC, 0x00, 0x00, 0x10)),
                    CornerRadius    = new CornerRadius(4,4,4,4),
                    Padding         = new Thickness(4,2,4,2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment   = VerticalAlignment.Top,
                    Margin   = new Thickness(4,4,0,0),
                    Visibility = Visibility.Collapsed
                };
                var badgeText = new TextBlock
                {
                    Text       = $"{i + 1}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xFF)),
                    FontSize   = 9, FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Segoe UI")
                };
                badge.Child = badgeText;
                Panel.SetZIndex(badge, 10);

                // 활성 점
                var dot = new Border
                {
                    Width = 6, Height = 6,
                    CornerRadius = new CornerRadius(3,3,3,3),
                    Background   = new SolidColorBrush(Color.FromRgb(0x33, 0xFF, 0x99)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment   = VerticalAlignment.Top,
                    Margin     = new Thickness(0,4,4,0),
                    Visibility = Visibility.Collapsed
                };
                Panel.SetZIndex(dot, 10);

                grid.Children.Add(emptyLabel);
                grid.Children.Add(wv);
                grid.Children.Add(badge);
                grid.Children.Add(dot);
                border.Child = grid;

                border.MouseLeftButtonUp += (s, e) => { if (idx < _channels.Count) FocusChannel(idx); };
                border.MouseEnter += (s, e) => border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0xAA));
                border.MouseLeave += (s, e) =>
                {
                    if (_mainIndex != idx)
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2A));
                };

                _slots[i] = new ThumbSlot
                {
                    Border     = border,
                    WebView    = wv,
                    EmptyLabel = emptyLabel,
                    Badge      = badge,
                    Dot        = dot
                };

                ThumbnailGrid.Children.Add(border);
            }
        }

        // ── WebView2 초기화 ──
        private async void InitWebViews()
        {
            await MainWebView.EnsureCoreWebView2Async();
            _mainInit = true;
            // 메인만 fullscreen CSS 적용
            MainWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            MainWebView.CoreWebView2.Settings.IsScriptEnabled = true;
            MainWebView.CoreWebView2.Settings.UserAgent = MobileUA();
            MainWebView.CoreWebView2.DOMContentLoaded += async (s, e) =>
            {
                try { await MainWebView.CoreWebView2.ExecuteScriptAsync(FullscreenScript(muted: false)); } catch { }
            };

            await History1WebView.EnsureCoreWebView2Async();
            _hist1Init = true;
            ApplySettings(History1WebView, muted: true);

            await History2WebView.EnsureCoreWebView2Async();
            _hist2Init = true;
            ApplySettings(History2WebView, muted: true);
            // 썸네일 슬롯도 MuteOnly (InitSlotWebView에서 처리)
        }

        private void ApplySettings(WebView2 wv, bool muted)
        {
            wv.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            wv.CoreWebView2.Settings.IsScriptEnabled = true;
            wv.CoreWebView2.Settings.UserAgent = MobileUA();
            // 히스토리/썸네일: fullscreen CSS 없이 음소거 + 스크립트 오류 팝업만 제거
            wv.CoreWebView2.DOMContentLoaded += async (s, e) =>
            {
                try { await wv.CoreWebView2.ExecuteScriptAsync(MuteOnlyScript(muted)); } catch { }
            };
        }

        // 썸네일 슬롯 WebView2 초기화 (처음 URL 로드 시 한 번만)
        private async void InitSlotWebView(int slotIdx, string url)
        {
            var slot = _slots[slotIdx];
            var wv   = slot.WebView;

            if (!slot.Initialized)
            {
                await wv.EnsureCoreWebView2Async();
                slot.Initialized = true;

                wv.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                wv.CoreWebView2.Settings.IsScriptEnabled = true;
                wv.CoreWebView2.Settings.UserAgent = MobileUA();

                // 백그라운드 스로틀링 방지
                wv.CoreWebView2.Settings.IsStatusBarEnabled = false;

                wv.CoreWebView2.DOMContentLoaded += async (s, e) =>
                {
                    try { await wv.CoreWebView2.ExecuteScriptAsync(MuteOnlyScript(muted: true)); } catch { }
                };
            }

            wv.CoreWebView2.Navigate(url);
            wv.Visibility = Visibility.Visible;
            slot.EmptyLabel.Visibility = Visibility.Collapsed;
            slot.Badge.Visibility = Visibility.Visible;
            slot.Dot.Visibility   = Visibility.Visible;
        }

        // ── 채널 포커스 ──
        private async void FocusChannel(int index)
        {
            if (index < 0 || index >= _channels.Count) return;
            var entry = _channels[index];

            if (_mainIndex >= 0 && _mainIndex != index)
            {
                _hist2Index = _hist1Index;
                _hist1Index = _mainIndex;
                await UpdateHistoryView(History2WebView, History2Placeholder, History2Title, _hist2Init, _hist2Index);
                await UpdateHistoryView(History1WebView, History1Placeholder, History1Title, _hist1Init, _hist1Index);
            }

            _mainIndex = index;
            MainTitleText.Text = $"CH {index + 1}  ·  {ShortenUrl(entry.Url)}";

            if (!_mainInit) await MainWebView.EnsureCoreWebView2Async();
            MainWebView.CoreWebView2.Navigate(entry.Url);
            MainWebView.Visibility = Visibility.Visible;
            MainPlaceholder.Visibility = Visibility.Collapsed;

            // 슬롯 강조 업데이트
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                bool active = (i == index);
                _slots[i].Border.BorderBrush = new SolidColorBrush(
                    active ? Color.FromRgb(0x55, 0x55, 0xFF) : Color.FromRgb(0x1A, 0x1A, 0x2A));
            }

            // 사이드바 강조
            foreach (var ch in _channels)
                if (ch.SideLabel != null)
                    ch.SideLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x99));
            if (entry.SideLabel != null)
                entry.SideLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xFF));
        }

        private async System.Threading.Tasks.Task UpdateHistoryView(
            WebView2 wv, StackPanel placeholder, TextBlock title, bool initialized, int idx)
        {
            if (idx < 0 || idx >= _channels.Count)
            {
                wv.Visibility = Visibility.Collapsed;
                placeholder.Visibility = Visibility.Visible;
                title.Text = "";
                return;
            }
            var entry = _channels[idx];
            if (!initialized) await wv.EnsureCoreWebView2Async();
            wv.CoreWebView2.Navigate(entry.Url);
            wv.Visibility = Visibility.Visible;
            placeholder.Visibility = Visibility.Collapsed;
            title.Text = $"CH {idx + 1}";
        }

        // ── URL 추가 ──
        private void AddUrl_Click(object sender, RoutedEventArgs e) => AddUrlFromInput();
        private void UrlInputBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) AddUrlFromInput(); }
        private void UrlInputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!UrlInputBox.Text.StartsWith("http")) UrlInputBox.Text = "";
        }

        private void AddUrlFromInput()
        {
            string url = UrlInputBox.Text.Trim();
            if (string.IsNullOrEmpty(url) || !url.StartsWith("http"))
            { StatusText.Text = "⚠  유효한 URL을 입력해주세요."; return; }
            if (_channels.Count >= MAX_SLOTS)
            { StatusText.Text = "⚠  최대 20개 채널에 도달했습니다."; return; }

            string watchUrl = ToWatchUrl(url);
            int slotIdx = _channels.Count;
            var entry = new ChannelEntry { Url = watchUrl, Index = slotIdx };
            _channels.Add(entry);

            AddPlaylistItem(entry);
            InitSlotWebView(slotIdx, watchUrl);
            UpdateStatus();
            UrlInputBox.Text = "";

            if (_channels.Count == 1) FocusChannel(0);
        }

        // ── 사이드바 ──
        private void AddPlaylistItem(ChannelEntry entry)
        {
            var border = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(0x0C, 0x0C, 0x1A)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2A)),
                BorderThickness = new Thickness(1,1,1,1),
                CornerRadius    = new CornerRadius(6,6,6,6),
                Margin          = new Thickness(0,0,0,4),
                Padding         = new Thickness(10,7,10,7),
                Cursor          = Cursors.Hand
            };
            var inner = new StackPanel();
            var numLabel = new TextBlock
            {
                Text = $"CH {entry.Index + 1:D2}",
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x88)),
                FontSize = 9, FontFamily = new FontFamily("Segoe UI"), FontWeight = FontWeights.Bold
            };
            var urlLabel = new TextBlock
            {
                Text = ShortenUrl(entry.Url),
                Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x99)),
                FontSize = 10, FontFamily = new FontFamily("Consolas"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            entry.SideLabel = urlLabel;
            inner.Children.Add(numLabel);
            inner.Children.Add(urlLabel);
            border.Child = inner;

            int idx = entry.Index;
            border.MouseLeftButtonUp += (s, e) => FocusChannel(idx);
            border.MouseEnter += (s, e) => border.Background = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x25));
            border.MouseLeave += (s, e) => border.Background = new SolidColorBrush(Color.FromRgb(0x0C, 0x0C, 0x1A));

            PlaylistPanel.Children.Add(border);
            PlaylistCountText.Text = $"{_channels.Count} videos";
        }

        // ── 전체 재생/정지 ──
        private async void PlayAll_Click(object sender, RoutedEventArgs e)
        {
            if (_mainInit) try { await MainWebView.CoreWebView2.ExecuteScriptAsync("var v=document.querySelector('video');if(v)v.play();"); } catch { }
            for (int i = 0; i < _channels.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Initialized)
                    try { await slot.WebView.CoreWebView2.ExecuteScriptAsync("var v=document.querySelector('video');if(v){v.muted=true;v.play();}"); } catch { }
            }
            StatusText.Text = "▶  재생 중...";
        }

        private async void StopAll_Click(object sender, RoutedEventArgs e)
        {
            if (_mainInit) try { await MainWebView.CoreWebView2.ExecuteScriptAsync("var v=document.querySelector('video');if(v)v.pause();"); } catch { }
            for (int i = 0; i < _channels.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Initialized)
                    try { await slot.WebView.CoreWebView2.ExecuteScriptAsync("var v=document.querySelector('video');if(v)v.pause();"); } catch { }
            }
            StatusText.Text = "⏸  일시정지됨";
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show("모든 채널을 초기화하시겠습니까?", "NEXUS",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            _channels.Clear();
            _mainIndex = _hist1Index = _hist2Index = -1;

            if (_mainInit)  MainWebView.CoreWebView2.Navigate("about:blank");
            if (_hist1Init) History1WebView.CoreWebView2.Navigate("about:blank");
            if (_hist2Init) History2WebView.CoreWebView2.Navigate("about:blank");

            MainWebView.Visibility  = Visibility.Collapsed;
            MainPlaceholder.Visibility  = Visibility.Visible;
            History1WebView.Visibility  = Visibility.Collapsed;
            History1Placeholder.Visibility = Visibility.Visible;
            History2WebView.Visibility  = Visibility.Collapsed;
            History2Placeholder.Visibility = Visibility.Visible;
            History1Title.Text = ""; History2Title.Text = "";
            MainTitleText.Text = "";

            // 슬롯 초기화
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                var slot = _slots[i];
                if (slot.Initialized) slot.WebView.CoreWebView2.Navigate("about:blank");
                slot.WebView.Visibility    = Visibility.Collapsed;
                slot.EmptyLabel.Visibility = Visibility.Visible;
                slot.Badge.Visibility      = Visibility.Collapsed;
                slot.Dot.Visibility        = Visibility.Collapsed;
                slot.Border.BorderBrush    = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2A));
            }

            PlaylistPanel.Children.Clear();
            PlaylistCountText.Text = "0 videos";
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            CountText.Text  = $"{_channels.Count} / {MAX_SLOTS}";
            StatusText.Text = _channels.Count == 0
                ? "NEXUS VIDEO GRID · URL을 추가해 채널을 시작하세요"
                : $"NEXUS VIDEO GRID · {_channels.Count}개 채널 활성화됨";
        }

        // ── 히스토리/썸네일용: 음소거만 (fullscreen CSS 없음) ──
        private static string MuteOnlyScript(bool muted)
        {
            string muteJs = muted
                ? "var v=document.querySelector('video');if(v)v.muted=true;"
                : "var v=document.querySelector('video');if(v){v.muted=false;v.play().catch(function(){});}";
            return $@"(function(){{
                {muteJs}
                var btn=document.querySelector('.ytp-large-play-button')||
                        document.querySelector('button[aria-label=""Play""]')||
                        document.querySelector('button[aria-label=""재생""]');
                if(btn)btn.click();
            }})();";
        }

        // ── 메인 플레이어용: 화면 꽉 채우기 + 음소거 제어 ──
        private static string FullscreenScript(bool muted)
        {
            string muteJs = muted
                ? "var v=document.querySelector('video');if(v)v.muted=true;"
                : "var v=document.querySelector('video');if(v){v.muted=false;v.play().catch(function(){});}";

            return $@"(function(){{
                var s=document.getElementById('nx');
                if(!s){{
                    s=document.createElement('style');s.id='nx';
                    s.textContent=`
                        ytd-masthead,#masthead-container,#masthead,
                        tp-yt-app-drawer,#secondary,#secondary-inner,
                        #comments,#related,ytd-miniplayer,
                        #below,#info,#meta,ytd-watch-metadata,
                        #description,#subscribe-button,
                        .ytp-pause-overlay,.ytp-endscreen-content,
                        .ytp-chrome-top{{display:none!important;}}
                        html,body{{overflow:hidden!important;background:#000!important;}}
                        ytd-app{{--ytd-toolbar-height:0px!important;}}
                        #page-manager,ytd-watch-flexy{{margin-top:0!important;padding-top:0!important;}}
                        #primary,#primary-inner,#player-container,
                        #player-container-outer,#player-container-inner,
                        #movie_player,#player,.html5-video-container,video{{
                            width:100vw!important;height:100vh!important;
                            max-width:100vw!important;max-height:100vh!important;
                            position:fixed!important;top:0!important;left:0!important;
                            margin:0!important;padding:0!important;
                            z-index:9999!important;background:#000!important;}}
                        video{{object-fit:contain!important;}}
                    `;
                    document.head.appendChild(s);
                }}
                {muteJs}
                var btn=document.querySelector('.ytp-large-play-button')||
                        document.querySelector('button[aria-label=""Play""]')||
                        document.querySelector('button[aria-label=""재생""]');
                if(btn)btn.click();
            }})();";
        }

        private static string MobileUA() =>
            "Mozilla/5.0 (Linux; Android 12; Pixel 6) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36";

        private static string ToWatchUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                if (uri.Host.Contains("youtube.com") && uri.AbsolutePath == "/watch") return url;
                if (uri.Host == "youtu.be")
                    return $"https://www.youtube.com/watch?v={uri.AbsolutePath.TrimStart('/')}";
                if (uri.AbsolutePath.StartsWith("/embed/"))
                    return $"https://www.youtube.com/watch?v={uri.AbsolutePath.Replace("/embed/", "")}";
            }
            catch { }
            return url;
        }

        private static string ShortenUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
                string? v = q["v"];
                return v != null ? $"youtu.be/{v}" : uri.Host;
            }
            catch { return url; }
        }
    }

    public class ThumbSlot
    {
        public Border    Border     { get; set; } = null!;
        public WebView2  WebView    { get; set; } = null!;
        public TextBlock EmptyLabel { get; set; } = null!;
        public Border    Badge      { get; set; } = null!;
        public Border    Dot        { get; set; } = null!;
        public bool      Initialized { get; set; } = false;
    }

    public class ChannelEntry
    {
        public string    Url       { get; set; } = "";
        public int       Index     { get; set; }
        public TextBlock? SideLabel { get; set; }
    }
}
