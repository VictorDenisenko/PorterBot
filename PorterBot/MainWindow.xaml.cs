using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

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
        private int _maxConcurrentProcesses;
        private int k_NumberOfEvents = 0;
        IdentificationResults identificationResults = null;
        public string userRoot = "";
        public string fileFromAzure = "";

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _maxConcurrentProcesses = 4;

                if (Uri.IsWellFormedUriString(faceEndpoint, UriKind.Absolute))
                {
                    faceClient.Endpoint = faceEndpoint;
                }
                else
                {
                    faceDescriptionStatusBar.Text = faceEndpoint + " Invalid URI " +  MessageBoxButton.OK + " " +  MessageBoxImage.Error;
                    Environment.Exit(0);
                }

                //FileSystemWatcher fsw = new FileSystemWatcher("c:\\Users\\vic\\Pictures\\");
                //fsw.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                //    | NotifyFilters.FileName | NotifyFilters.DirectoryName;

                userRoot = System.Environment.GetEnvironmentVariable("USERPROFILE");
                fileFromAzure = "c:\\Users\\vic\\Pictures\\FromCamera\\CapturedPhoto.jpg";
                //System.Windows.Resources.StreamResourceInfo res = Application.GetResourceStream(new Uri("Users", UriKind.RelativeOrAbsolute));


                //FileSystemWatcher fsw = new FileSystemWatcher("c:\\Users\\vic\\Pictures\\");
                FileSystemWatcher fsw = new FileSystemWatcher(userRoot + "\\Pictures\\FromCamera\\");
                fsw.NotifyFilter = NotifyFilters.LastWrite;

                fsw.Changed += Fsw_Changed;
                
                //fsw.Changed += new FileSystemEventHandler(OnChanged);
                //fsw.Created += new FileSystemEventHandler(OnChanged);
                //fsw.Deleted += new FileSystemEventHandler(OnChanged);
                fsw.Error += new ErrorEventHandler(OnError);
                fsw.EnableRaisingEvents = true;
                identificationResults = new IdentificationResults();
            }
            catch (APIErrorException f)
            {
                faceDescriptionStatusBar.Text = f.Body.Error.Message;
            }
            catch (Exception e)
            {
                faceDescriptionStatusBar.Text = e.Message;
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

            faceDescriptionStatusBar.Text = "Detecting...";
            await FaceEmotion(filePath, bitmapSource);
        }


        private async Task FaceEmotion(string filePath, BitmapImage bitmapSource)
        {//Face Detect and Emotion finding
            faceList = await UploadAndDetectFaces(filePath);
            //faceDescriptionStatusBar.Text = "Detection Finished. Detected " + faceList.Count + " face(s)";

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

        private void FacePhoto_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
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
                faceDescriptionStatusBar.Text = f.Message;
                return new List<DetectedFace>();
            }
            catch (Exception e)
            {
                faceDescriptionStatusBar.Text = e.Message;
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

            identificationResults.Age = face.FaceAttributes.Age;
            identificationResults.Blur = Convert.ToString(face.FaceAttributes.Blur);
            identificationResults.Emotion = Convert.ToString(face.FaceAttributes.Emotion);
            identificationResults.Exposure = Convert.ToString(face.FaceAttributes.Exposure);
            identificationResults.FacialHair = Convert.ToString(face.FaceAttributes.FacialHair);
            identificationResults.Gender = Convert.ToString(face.FaceAttributes.Gender);
            identificationResults.Glasses = Convert.ToString(face.FaceAttributes.Glasses);
            identificationResults.Hair = Convert.ToString(face.FaceAttributes.Hair);
            identificationResults.HeadPose = Convert.ToDouble(face.FaceAttributes.HeadPose);
            identificationResults.Makeup = Convert.ToString(face.FaceAttributes.Makeup);
            identificationResults.Noise = Convert.ToString(face.FaceAttributes.Noise);
            identificationResults.Occlusion = Convert.ToString(face.FaceAttributes.Occlusion);
            identificationResults.Smile = Convert.ToDouble(face.FaceAttributes.Smile);

            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(IdentificationResults));

            

            using (FileStream fs = new FileStream(userRoot + "\\Pictures\\FromAzure\\DataFromAzure.json", FileMode.OpenOrCreate))
            {
                jsonFormatter.WriteObject(fs, identificationResults);
            }

            //using (FileStream fs = new FileStream(userRoot + "\\Pictures\\FromAzure\\DataFromAzure.json", FileMode.OpenOrCreate))
            //{
            //    IdentificationResults dataFromAzure = (IdentificationResults)jsonFormatter.ReadObject(fs);
            //}

            return sb.ToString();
        }

        private async void PictureFolder_ClickAsync(object sender, RoutedEventArgs e)
        {
            faceDescriptionStatusBar.Text = "";
            UnknownFace.IsEnabled = false;
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            string filePath = Properties.Settings.Default.filePath;//Переменная вида filePath должна быть сначала инициализирована в Properties/Settings проекта
            try
            {
                if (filePath == "")
                {
                    dialog.InitialDirectory = "C:\\Users";
                }
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Properties.Settings.Default.filePath = dialog.FileName;
                    Properties.Settings.Default.Save();
                }
                if (dialog.FileName == "")
                {
                    return;
                }
            }
            catch(Exception e1)
            {
                faceDescriptionStatusBar.Text = e1.Message;
                return;
            }

            bool groupExists = false;
            try
            {
                PersonGroup personGroup = await faceClient.PersonGroup.GetAsync(personGroupId);
                groupExists = true;
            }
            catch (APIErrorException ex)
            {
                groupExists = false;
                if (ex.Body.Error.Code != "PersonGroupNotFound")
                {
                    faceDescriptionStatusBar.Text = "Response: " + ex.Body.Error.Message;
                }
                else
                {
                    faceDescriptionStatusBar.Text = "Response: Group " + personGroupId + " did not exist previously.";
                }
            }

            if (groupExists)
            {
                await faceClient.PersonGroup.DeleteAsync(personGroupId);
                groupExists = false;
            }

            filePath = dialog.FileName;
            const int SuggestionCount = 15;

            if (groupExists == false)
            {
                try
                {
                    await faceClient.PersonGroup.CreateAsync(personGroupId, personGroupName);
                }
                catch (APIErrorException ex)
                {
                    faceDescriptionStatusBar.Text = "Response: "  + ex.Body.Error.Message;
                    return;
                }

                int processCount = 0;
                bool forceContinue = false;
                faceDescriptionStatusBar.Text = "Request: Preparing faces for identification, detecting faces in chosen folder.";
                int invalidImageCount = 0;
                int imageIndex = 0;
                foreach (var dir in Directory.EnumerateDirectories(filePath))
                {
                    var tasks = new List<Task>();
                    var tag = Path.GetFileName(dir);
                    Person person  =await faceClient.PersonGroupPerson.CreateAsync(personGroupId, tag);
                    imageIndex++;
                    var faces = new ObservableCollection<DetectedFace>();
                    string img;
                    // Enumerate images under the person folder, call detection
                    var imageList = new ConcurrentBag<string>(Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(".jpg") || s.ToLower().EndsWith(".png") || s.ToLower().EndsWith(".bmp") || s.ToLower().EndsWith(".gif")));

                    while (imageList.TryTake(out img))
                    {
                        tasks.Add(Task.Factory.StartNew(
                            async (obj) =>
                            {
                                var imgPath = obj as string;
                                using (var fStream = File.OpenRead(imgPath))
                                {
                                    try
                                    {
                                        // Update person faces on server side
                                        await faceClient.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId, fStream);
                                    }
                                    catch (APIErrorException ex)
                                    {
                                    // if operation conflict, retry.
                                    if (ex.Body.Error.Code.Equals("ConcurrentOperationConflict"))
                                        {
                                            imageList.Add(imgPath);
                                            return;
                                        }
                                    // if operation cause rate limit exceed, retry.
                                    else if (ex.Body.Error.Code.Equals("RateLimitExceeded"))
                                        {
                                            imageList.Add(imgPath);
                                            return;
                                        }
                                        else if (ex.Body.Error.Message.Contains("more than 1 face in the image."))
                                        {
                                            Interlocked.Increment(ref invalidImageCount);
                                        }
                                        // Here we simply ignore all detection failure in this sample
                                        // You may handle these exceptions by check the Error.Error.Code and Error.Message property for ClientException object
                                        //return null;//new Tuple<string, DetectedFace>(imgPath, faceList[0]);
                                    }
                                }
                            },
                            img).Unwrap().ContinueWith((detectTask) =>
                            {
                            // Update detected faces for rendering
                            //var detectionResult = detectTask?.Result;
                            //    if (detectionResult == null || detectionResult.Item2 == null)
                            //    {
                            //        return;
                            //    }

                                //this.Dispatcher.Invoke(
                                //    new Action<ObservableCollection<DetectedFace>, string, ClientContract.AddPersistedFaceResult>(UIHelper.UpdateFace),
                                //    faces,
                                //    detectionResult.Item1,
                                //    detectionResult.Item2);
                            }));
                        if (processCount >= SuggestionCount && !forceContinue)
                        {
                            //var continueProcess = MessageBox.Show("The images loaded have reached the recommended count, may take long time if proceed. Would you like to continue to load images?", "Warning");
                            var continueProcess = System.Windows.Forms.MessageBox.Show("The images loaded have reached the recommended count, may take long time if proceed. Would you like to continue to load images?", "Warning", System.Windows.Forms.MessageBoxButtons.YesNo);
                            if (continueProcess == System.Windows.Forms.DialogResult.Yes)
                            {
                                forceContinue = true;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (tasks.Count >= _maxConcurrentProcesses || imageList.IsEmpty)
                        {
                            await Task.WhenAll(tasks);
                            tasks.Clear();
                        }
                    }
                }
                if (invalidImageCount > 0)
                {
                    faceDescriptionStatusBar.Text = "Warning: more or less than one face is detected in images, can not add to face list. " + invalidImageCount;
                }
                faceDescriptionStatusBar.Text = "Response: Success. Total {0} faces are detected.";// + Persons.Sum(p => p.Faces.Count);

                try
                {
                    faceDescriptionStatusBar.Text = "Training group " + personGroupId;
                    await faceClient.PersonGroup.TrainAsync(personGroupId);
                    // Wait until train completed
                    TrainingStatus trainingStatus = null;
                    while (true)
                    {
                        trainingStatus = await faceClient.PersonGroup.GetTrainingStatusAsync(personGroupId);
                        if (trainingStatus.Status != TrainingStatusType.Running)
                        {
                            faceDescriptionStatusBar.Text = "Training is finished";
                            UnknownFace.IsEnabled = true;
                            break;
                        }
                        await Task.Delay(1000);
                    }
                }
                catch (APIErrorException ex)
                {
                    faceDescriptionStatusBar.Text = "Response: " + ex.Body.Error.Message;
                }
            }
            GC.Collect();
        }

        private void UnknownFace_Click(object sender, RoutedEventArgs e)
        {
            faceDescriptionStatusBar.Text = "";
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

            UnknownFaceChoosing(filePath);
        }


        private async void UnknownFaceChoosing(string filePath)
        {
            using (Stream s = File.OpenRead(filePath))
            {
                IList<FaceAttributeType> faceAttributes = new FaceAttributeType[]
               {
                    FaceAttributeType.Gender, FaceAttributeType.Age,
                    FaceAttributeType.Smile, FaceAttributeType.Emotion,
                    FaceAttributeType.Glasses, FaceAttributeType.Hair,
                    FaceAttributeType.FacialHair
               };

                IList<DetectedFace> faceList = null;
                try
                {
                    using (Stream imageFileStream = File.OpenRead(filePath))
                    {
                        faceList = await faceClient.Face.DetectWithStreamAsync(imageFileStream, true, false, faceAttributes);
                    }
                }
                catch (APIErrorException f)
                {
                    faceDescriptionStatusBar.Text = f.Body.Error.Message;
                }
                catch (Exception e2)
                {
                    faceDescriptionStatusBar.Text = e2.Message;
                }
             
                IList<Guid> faceIds = new Guid[faceList.Count]; 
                for (int i = 0; i< faceList.Count; i++)
                {
                    Guid defaultId = Guid.NewGuid();
                    faceIds[i] = faceList[i].FaceId.Value;
                    defaultId = faceList[i].FaceId.Value;
                }

                try
                {
                    var results = await faceClient.Face.IdentifyAsync(faceIds, personGroupId);
                    foreach (var identifyResult in results)
                    {
                        faceDescriptionStatusBar.Text = "Result of face: " + identifyResult.FaceId;
                        if (identifyResult.Candidates.Count == 0)
                        {
                            faceDescriptionStatusBar.Text = "No one identified";
                        }
                        else
                        {
                            // Get top 1 among all candidates returned
                            var candidateId = identifyResult.Candidates[0].PersonId;
                            var person = await faceClient.PersonGroupPerson.GetAsync(personGroupId, candidateId);
                            faceDescriptionStatusBar.Text = "Identified as " + person.Name;
                            identificationResults.Name = person.Name;
                        }
                    }
                }
                catch(APIErrorException e3)
                {
                    faceDescriptionStatusBar.Text = e3.Body.Error.Message;
                }
            }
        }

        private void AutomaticFace_Click(object sender, RoutedEventArgs e)
        {
            //string filePath = "c://Users//vic//Pictures//CapturedPhoto.jpg";
            fileFromAzure = userRoot + "\\Pictures\\FromCamera\\CapturedPhoto.jpg";
            Uri fileUri = new Uri(fileFromAzure);
            BitmapImage bitmapSource = new BitmapImage();
            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();
            FacePhoto.Source = bitmapSource;
            UnknownFaceChoosing(fileFromAzure);
        }


        private void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            k_NumberOfEvents++;
            if (k_NumberOfEvents == 1)
            {
                
                this.Dispatcher.Invoke(() =>
                {
                    AutomaticFace_Click(null, null);
                });
                fileFromAzure = e.FullPath;
            }
        }

        private static void OnError(object source, ErrorEventArgs e)
        {
            Console.WriteLine("The FileSystemWatcher has detected an error");
            if (e.GetException().GetType() == typeof(InternalBufferOverflowException))
            {
                Console.WriteLine(("The file system watcher experienced an internal buffer overflow: " + e.GetException().Message));
            }
        }
    }
}
