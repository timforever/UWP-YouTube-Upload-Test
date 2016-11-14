using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace YouTubeUploadTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void OnUploadBtnClicked(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.VideosLibrary};
            filePicker.FileTypeFilter.Add(".mpeg");
            filePicker.FileTypeFilter.Add(".mpg");
            filePicker.FileTypeFilter.Add(".mp4");
            filePicker.FileTypeFilter.Add(".avi");

            StorageFile movieFile = await filePicker.PickSingleFileAsync();

            UploadToYouTube(movieFile);
        }
        
        private async void UploadToYouTube(StorageFile file)
        {
            ProgressRing.Visibility = Visibility.Visible;
            ProgressRing.IsActive = true;

            var clientSecretFile = new Uri("ms-appx:///client_id.json");

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecretFile,
                // This OAuth 2.0 access scope allows an application to upload files to the
                // authenticated user's YouTube channel, but doesn't allow other types of access.
                new[] { YouTubeService.Scope.YoutubeUpload },
                "tim.eicher@gmail.com",
                CancellationToken.None
                );

            var youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Lineage Suncasts"
            });

            var video = new Video
            {
                Snippet = new VideoSnippet
                {
                    Title = $"{file.Name}",
                    Description = "A test recording made by the Suncasts app.",
                    //                    Tags = new string[] {"tag1", "tag2"},
                    CategoryId = "22"
                },
                Status = new VideoStatus { PrivacyStatus = "unlisted" }
            };
            // See https://developers.google.com/youtube/v3/docs/videoCategories/list
            // or "private" or "public"

            Stream fs = (await file.OpenReadAsync()).AsStreamForRead();
            VideosResource.InsertMediaUpload videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fs, "video/*");
            videosInsertRequest.ProgressChanged += OnVideoInsertProgressChanged;
            videosInsertRequest.ResponseReceived += OnVideosInsertRequestResponseReceived;

            videosInsertRequest.ChunkSize = videosInsertRequest.ChunkSize;
            await videosInsertRequest.UploadAsync();

            ProgressRing.IsActive = false;
            ProgressRing.Visibility = Visibility.Collapsed;
        }

        void OnVideoInsertProgressChanged(IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case UploadStatus.Uploading:
                    Debug.WriteLine("{0} bytes sent.", progress.BytesSent);
                    break;

                case UploadStatus.Failed:
                    Debug.WriteLine("An error prevented the upload from completing.\n{0}", progress.Exception);
                    break;
            }
        }

        void OnVideosInsertRequestResponseReceived(Video video)
        {
            Debug.WriteLine("Video id '{0}' was successfully uploaded.", video.Id);
        }
    }
}
