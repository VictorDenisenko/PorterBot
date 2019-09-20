using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PorterBot
{
    public partial class MainWindow : Window
    {
        private const string subscriptionKey = "62897d6b94e24c1d877f17b4ab17e066";
        private const string faceEndpoint = "https://vic.cognitiveservices.azure.com/";
        private readonly IFaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey), new System.Net.Http.DelegatingHandler[] { });
        private IList<DetectedFace> faceList;
        private string[] faceDescriptions;
        private double resizeFactor;
        private const string defaultStatusBarText = "";
        const string personGroupId = "rldastaffid";
        const string personGroupName = "rldastaff";


        public MainWindow()
        {
            try
            {
                InitializeComponent();

                if (Uri.IsWellFormedUriString(faceEndpoint, UriKind.Absolute))
                {
                    faceClient.Endpoint = faceEndpoint;
                }
                else
                {
                    MessageBox.Show(faceEndpoint,"Invalid URI", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }

                    string personGroupException = "";
                Task t = new Task(async () =>
                {
                    try
                    {
                        PersonGroup personGroup = await faceClient.PersonGroup.GetAsync(personGroupId);
                    }
                    catch (APIErrorException e)
                    {
                        personGroupException = e.Body.Error.Code;
                    }

                    if (personGroupException == "PersonGroupNotFound")
                    {
                        await faceClient.PersonGroup.CreateAsync(personGroupId, personGroupName);
                        var friend1 = await faceClient.PersonGroupPerson.CreateAsync(personGroupId, "vic");
                        var friend2 = await faceClient.PersonGroupPerson.CreateAsync(personGroupId, "dima");
                        var friend3 = await faceClient.PersonGroupPerson.CreateAsync(personGroupId, "ludmila");
                    }
                    else
                    {
                        await faceClient.PersonGroup.DeleteAsync(personGroupId);
                        //faceClient.PersonGroupPerson.AddFaceFromStreamAsync
                    }

                });
                t.Start();

            }
            catch (APIErrorException f)
            {

            }
            catch (Exception e)
            {
                string x = e.Message;
            }

        }



        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new Microsoft.Win32.OpenFileDialog();
            openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";
            bool? result = openDlg.ShowDialog(this);

            if (!(bool)result)
            {
                return;
            }

            string filePath = openDlg.FileName;

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            FacePhoto.Source = bitmapSource;

            Title = "Detecting...";
            faceList = await UploadAndDetectFaces(filePath);
            Title = String.Format("Detection Finished. {0} face(s) detected", faceList.Count);

            if (faceList.Count > 0)
            {
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource, new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;
                resizeFactor = (dpi == 0) ? 1 : 96 / dpi;
                faceDescriptions = new String[faceList.Count];

                for (int i = 0; i < faceList.Count; ++i)
                {
                    DetectedFace face = faceList[i];
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brushes.Red, 2),
                        new Rect(face.FaceRectangle.Left * resizeFactor, face.FaceRectangle.Top * resizeFactor, face.FaceRectangle.Width * resizeFactor, face.FaceRectangle.Height * resizeFactor)
                    );
                    faceDescriptions[i] = FaceDescription(face);
                }
                drawingContext.Close();
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap((int)(bitmapSource.PixelWidth * resizeFactor),(int)(bitmapSource.PixelHeight * resizeFactor),96,96,PixelFormats.Pbgra32);
                faceWithRectBitmap.Render(visual);
                FacePhoto.Source = faceWithRectBitmap;
                //faceDescriptionStatusBar.Text = defaultStatusBarText;
            }
        }

        private void FacePhoto_MouseMove(object sender, MouseEventArgs e)
        {
            if (faceList == null)
                return;
            Point mouseXY = e.GetPosition(FacePhoto);
            ImageSource imageSource = FacePhoto.Source;
            BitmapSource bitmapSource = (BitmapSource)imageSource;
            var scale = FacePhoto.ActualWidth / (bitmapSource.PixelWidth / resizeFactor);

            for (int i = 0; i < faceList.Count; ++i)
            {
                FaceRectangle fr = faceList[i].FaceRectangle;
                double left = fr.Left * scale;
                double top = fr.Top * scale;
                double width = fr.Width * scale;
                double height = fr.Height * scale;

                if (mouseXY.X >= left && mouseXY.X <= left + width &&
                    mouseXY.Y >= top && mouseXY.Y <= top + height)
                {
                    faceDescriptionStatusBar.Text = faceDescriptions[i];
                    
                    break;
                }
            }
        }

        private async Task<IList<DetectedFace>> UploadAndDetectFaces(string imageFilePath)
        {
            IList<FaceAttributeType> faceAttributes =
                new FaceAttributeType[]
                {
                    FaceAttributeType.Gender, FaceAttributeType.Age,
                    FaceAttributeType.Smile, FaceAttributeType.Emotion,
                    FaceAttributeType.Glasses, FaceAttributeType.Hair,
                    FaceAttributeType.FacialHair
                };

            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    IList<DetectedFace> faceList = await faceClient.Face.DetectWithStreamAsync(imageFileStream, true, false, faceAttributes);
                    return faceList;
                }
            }
            catch (APIErrorException f)
            {
                MessageBox.Show(f.Message);
                return new List<DetectedFace>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
                return new List<DetectedFace>();
            }
        }

        private string FaceDescription(DetectedFace face)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Face: ");
            sb.Append(face.FaceAttributes.Gender);
            sb.Append(", ");
            sb.Append(face.FaceAttributes.Age);
            sb.Append(", ");
            sb.Append(String.Format("smile {0:F1}%, ", face.FaceAttributes.Smile * 100));

            sb.Append("Emotion: ");
            Emotion emotionScores = face.FaceAttributes.Emotion;
            if (emotionScores.Anger >= 0.1f) sb.Append(String.Format("anger {0:F1}%, ", emotionScores.Anger * 100));
            if (emotionScores.Contempt >= 0.1f) sb.Append(String.Format("contempt {0:F1}%, ", emotionScores.Contempt * 100));
            if (emotionScores.Disgust >= 0.1f) sb.Append(String.Format("disgust {0:F1}%, ", emotionScores.Disgust * 100));
            if (emotionScores.Fear >= 0.1f) sb.Append(String.Format("fear {0:F1}%, ", emotionScores.Fear * 100));
            if (emotionScores.Happiness >= 0.1f) sb.Append(String.Format("happiness {0:F1}%, ", emotionScores.Happiness * 100));
            if (emotionScores.Neutral >= 0.1f) sb.Append(String.Format("neutral {0:F1}%, ", emotionScores.Neutral * 100));
            if (emotionScores.Sadness >= 0.1f) sb.Append(String.Format("sadness {0:F1}%, ", emotionScores.Sadness * 100));
            if (emotionScores.Surprise >= 0.1f) sb.Append(String.Format("surprise {0:F1}%, ", emotionScores.Surprise * 100));

            sb.Append(face.FaceAttributes.Glasses);
            sb.Append(", ");
            sb.Append("Hair: ");
            if (face.FaceAttributes.Hair.Bald >= 0.01f) sb.Append(String.Format("bald {0:F1}% ", face.FaceAttributes.Hair.Bald * 100));

            IList<HairColor> hairColors = face.FaceAttributes.Hair.HairColor;
            foreach (HairColor hairColor in hairColors)
            {
                if (hairColor.Confidence >= 0.1f)
                {
                    sb.Append(hairColor.Color.ToString());
                    sb.Append(String.Format(" {0:F1}% ", hairColor.Confidence * 100));
                }
            }

            return sb.ToString();
        }
    }
}
