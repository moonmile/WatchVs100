using WatchVs100.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Google.Apis.YouTube.v3;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using System.Net.Http;
using Windows.System;
using MyToolkit.Multimedia;
using Windows.UI.Xaml.Media.Animation;
using System.Diagnostics;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;

// 基本ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234237 を参照してください

namespace WatchVs100
{
    /// <summary>
    /// 多くのアプリケーションに共通の特性を指定する基本ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// これは厳密に型指定されたビュー モデルに変更できます。
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper は、ナビゲーションおよびプロセス継続時間管理を
        /// 支援するために、各ページで使用します。
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }


        public MainPage()
        {
            this.InitializeComponent();

            this.vm = new ViewModel();
            this.DataContext = this.vm;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }

        ViewModel vm;
        /// <summary>
        /// このページには、移動中に渡されるコンテンツを設定します。前のセッションからページを
        /// 再作成する場合は、保存状態も指定されます。
        /// </summary>
        /// <param name="sender">
        /// イベントのソース (通常、<see cref="NavigationHelper"/>)>
        /// </param>
        /// <param name="e">このページが最初に要求されたときに
        /// <see cref="Frame.Navigate(Type, Object)"/> に渡されたナビゲーション パラメーターと、
        /// 前のセッションでこのページによって保存された状態の辞書を提供する
        /// セッション。ページに初めてアクセスするとき、状態は null になります。</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // タイトル一覧を取得する
            this.vm.IsBusy = true;
            this.vm.Items = await getYoutube();
            this.vm.IsBusy = false;
            if (e.NavigationParameter != null )
            {
                string videoid = e.NavigationParameter as string;
                LoadState(videoid);
            }
        }
        public async void LoadState( string videoid )
        {
            if (videoid != "")
            {
                this.media.IsFullWindow = true;
                var url = await YouTube.GetVideoUriAsync(videoid, YouTubeQuality.Quality1080P);
                this.media.AutoPlay = true;
                this.media.Source = url.Uri;
                this.media.Play();
            }
        }

        async Task<VideoList> getYoutube()
        {
            // var url = "http://www.youtube.com/playlist?list=UUy2j4l0auN8DJaQ6itwVWFA";
            // var url = "http://www.youtube.com/watch?v=";
            // タイトル一覧を取得する
            var youtube = new YouTubeService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                ApiKey = "", // Google API key
                ApplicationName = "VS100Watcher"
            });

            var videos = new VideoList();

            var req = youtube.Search.List("snippet");
            req.ChannelId = "UCy2j4l0auN8DJaQ6itwVWFA"; // "VisualStudioJapan" のチャンネルID
            var res = new Google.Apis.YouTube.v3.Data.SearchListResponse(); 
            do
            {
                req.PageToken = res.NextPageToken;
                res = await req.ExecuteAsync();
                // Youtube のサムネールをリスト化する
                foreach (var it in res.Items)
                {
                    var vi = new Video() { Result = it };
                    // var cl = new HttpClient();
                    // var data = await cl.GetByteArrayAsync(vi.ThumUrl);
                    // var bmp = new BitmapImage();
                    // var mem = new MemoryStream(data);
                    // await bmp.SetSourceAsync( mem.AsRandomAccessStream() );
                    // vi.ThumImage = bmp;
                    videos.Add(vi);
                }
            } while (res.NextPageToken != null && res.NextPageToken != "");

            // 番号でソートする
            var lst = new VideoList();
            foreach ( var it in videos.OrderBy(x => {
                if ( x.Title.StartsWith("VS100") ) {
                    return x.Title.Replace( " ", "" ).Replace("-", "");
                } else {
                    return x.Title;
                }}))
            {
                lst.Add(it);
                Debug.WriteLine(it.Title);
            }
            return lst;
        }

        /// <summary>
        /// アプリケーションが中断される場合、またはページがナビゲーション キャッシュから破棄される場合、
        /// このページに関連付けられた状態を保存します。値は、
        /// <see cref="SuspensionManager.SessionState"/> のシリアル化の要件に準拠する必要があります。
        /// </summary>
        /// <param name="sender">イベントのソース (通常、<see cref="NavigationHelper"/>)</param>
        /// <param name="e">シリアル化可能な状態で作成される空のディクショナリを提供するイベント データ
        ///。</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // 選択状態を保持する
            MainPage.SelectItemIndex = vm.SelectedIndex;
        }
        static int SelectItemIndex = -1;

        #region NavigationHelper の登録

        /// このセクションに示したメソッドは、NavigationHelper がページの
        /// ナビゲーション メソッドに応答できるようにするためにのみ使用します。
        /// 
        /// ページ固有のロジックは、
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// および <see cref="GridCS.Common.NavigationHelper.SaveState"/> のイベント ハンドラーに配置する必要があります。
        /// LoadState メソッドでは、前のセッションで保存されたページの状態に加え、
        /// ナビゲーション パラメーターを使用できます。

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        /// <summary>
        /// <summary>
        /// アイテムをタップしたとき
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void itemGridView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as GridView).SelectedItem ;
            if (item == null ) return;

            vm.Current = item as Video;
            MainPage.SelectItemIndex = (sender as GridView).SelectedIndex;

            var uri = new Uri( vm.Current.Url );
            if (vm.GoIE == true)
            {
                await Launcher.LaunchUriAsync(uri);
            }
            else
            {
                try
                {
                    var url = await YouTube.GetVideoUriAsync(vm.Current.VidoId, YouTubeQuality.Quality1080P);
                    this.media.AutoPlay = true;
                    this.media.Source = url.Uri;
                    this.media.Play();
                }
                catch { }
            }
        }

        /// <summary>
        /// スタート画面にピン留め
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnButtonMakeTile(object sender, RoutedEventArgs e)
        {
            var item = this.itemGridView.SelectedItem as Video;
            if (item == null) return;
            await CreateSecondaryTileAsync(item);

        }
        private async System.Threading.Tasks.Task<bool> CreateSecondaryTileAsync(Video item)
        {
            string tileId = "VS100-" + item.VidoId; 
            var tile = new Windows.UI.StartScreen.SecondaryTile()
            {
                TileId = tileId,
                DisplayName = item.Title,
                Arguments = item.VidoId,
                RoamingEnabled = true,
            };
            tile.VisualElements.ForegroundText = Windows.UI.StartScreen.ForegroundText.Light;
            tile.VisualElements.ShowNameOnSquare150x150Logo = true;

            // アプリローカルに保存
            var cl = new HttpClient();
            var data = await cl.GetByteArrayAsync(new Uri(item.ThumUrl));

            var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var path = item.VidoId + ".jpg";

            try
            {
                var file = await folder.CreateFileAsync(path);
                using (var sw = await file.OpenStreamForWriteAsync())
                {
                    sw.Write(data, 0, data.Length);
                    sw.Flush();
                }
            }
            catch
            {
                // 同名のファイルがある場合は、そのまま使う
            }

            tile.VisualElements.Square150x150Logo = new Uri("ms-appdata:///local/" + path);
            tile.VisualElements.Square30x30Logo = new Uri("ms-appdata:///local/" + path);

            return await tile.RequestCreateForSelectionAsync(GetElementRect(this.btnMakeTile));
        }
        public static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            this.media.Play();
        }

        private void OnPause(object sender, RoutedEventArgs e)
        {
            this.media.Stop();

        }
        private void OnStop(object sender, RoutedEventArgs e)
        {
            this.media.Stop();
        }
        /// <summary>
        /// 元の大きさに戻す
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBackToWindow(object sender, RoutedEventArgs e)
        {
            this.media.IsFullWindow = false;
        }
    }

    public class ViewModel : BindableBase
    {
        private VideoList _Items;
        public VideoList Items
        {
            get { return _Items; }
            set { this.SetProperty(ref this._Items, value); }
        }

        private bool _GoIE;
        public bool GoIE
        {
            get { return _GoIE; }
            set { this.SetProperty(ref this._GoIE, value); }
        }
        private Video _Current;
        public Video Current
        {
            get { return _Current; }
            set { this.SetProperty(ref this._Current, value); }
        }
        private int _SelectedIndex;
        public int SelectedIndex
        {
            get { return _SelectedIndex; }
            set { this.SetProperty(ref this._SelectedIndex, value); }
        }

        private bool _IsBusy;
        public bool IsBusy
        {
            get { return _IsBusy; }
            set { this.SetProperty(ref this._IsBusy, value); }
        }
    }

    public class VideoList : ObservableCollection<Video> { }
    public class Video : BindableBase
    {
        /// <summary>
        /// 検索結果
        /// </summary>
        private Google.Apis.YouTube.v3.Data.SearchResult _Result;
        public Google.Apis.YouTube.v3.Data.SearchResult Result
        {
            get { return _Result; }
            set { this.SetProperty(ref this._Result, value); }
        }

        /// <summary>
        /// タイトル
        /// </summary>
        public string Title
        {
            get { return this.Result.Snippet.Title; }
        }
        public string Description
        {
            get { return this.Result.Snippet.Description; }
        }
        public string VidoId
        {
            get { return this.Result.Id.VideoId; }
        }
        public string Url
        {
            get { return "http://www.youtube.com/watch?v=" + this.VidoId;  }
        }
        

        /// <summary>
        /// サムネールのURL
        /// </summary>
        public string ThumUrl
        {
            get { return this.Result.Snippet.Thumbnails.High.Url; }
        }
        /// <summary>
        /// サムネールのBitmap
        /// </summary>
        private ImageSource _ThumImage;
        public ImageSource ThumImage
        {
            get { return _ThumImage; }
            set { this.SetProperty(ref this._ThumImage, value); }
        }
        /// <summary>
        /// サムネールの保存
        /// </summary>
        private string _IconUrl;
        public string IconUrl
        {
            get { return _IconUrl; }
            set { this.SetProperty(ref this._IconUrl, value); }
        }
    }

    public abstract class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
