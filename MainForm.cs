using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Diagnostics;
using System.IO.Ports;
using System.IO;
using System.Text.RegularExpressions;
using Bots;
using System.Drawing.Imaging;

namespace AForge.WindowsForms
{
    delegate void FormUpdateDelegate();

    public partial class MainForm : Form
    {
        /// <summary>
        /// Класс, реализующий всю логику работы
        /// </summary>
        private Controller controller = null;
                
        /// <summary>
        /// Список устройств для снятия видео (веб-камер)
        /// </summary>
        private FilterInfoCollection videoDevicesList;
        
        /// <summary>
        /// Выбранное устройство для видео
        /// </summary>
        private IVideoSource videoSource;
        
        /// <summary>
        /// Таймер для измерения производительности (времени на обработку кадра)
        /// </summary>
        private Stopwatch sw = new Stopwatch();

        /// <summary>
        /// neuralNetwork
        /// </summary>
        public BaseNetwork neuralNetwork;

        int blobWidth = 25;
        int blobHeight = 25;

        /// <summary>
        /// Функция обновления формы, тут же происходит анализ текущего этапа, и при необходимости переключение на следующий
        /// Вызывается автоматически - это плохо, надо по делегатам вообще-то
        /// </summary>
        private void UpdateFormFields()
        {
            //  Проверяем, вызвана ли функция из потока главной формы. Если нет - вызов через Invoke
            //  для синхронизации, и выход
            if (statusLabel.InvokeRequired)
            {
                this.Invoke(new FormUpdateDelegate(UpdateFormFields));
                return;
            }

            sw.Stop();
            ticksLabel.Text = "Тики : " + sw.Elapsed.ToString();
            pictureButton.BackgroundImageLayout = ImageLayout.Zoom;
            originalImageBox.BackgroundImageLayout = ImageLayout.Zoom;
            processedImgBox.BackgroundImageLayout = ImageLayout.Zoom;
            originalImageBox.Image = controller.GetOriginalImage();
            processedImgBox.Image = controller.GetProcessedImage();

            var sample = GetStupidSample(SignType.Undefined, controller.processor.blobImage, true);
            var result = neuralNetwork.Predict(sample);
            pictureButton.Image = BaseImages[result];
            resultButton.Text = result.ToString();
            debugInfoButton.Text = String.Join("\n", sample.output.Select((value, i) => $"{((SignType) i)}: {value}"));

        }

        public MainForm()
        {
            InitializeComponent();
            // Список камер получаем
            videoDevicesList = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo videoDevice in videoDevicesList)
            {
                cmbVideoSource.Items.Add(videoDevice.Name);
            }
            if (cmbVideoSource.Items.Count > 0)
            {
                cmbVideoSource.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("А нет у вас камеры!", "Ошибочка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            controller = new Controller(new FormUpdateDelegate(UpdateFormFields));
            int[] structure = { blobHeight * blobWidth, 600, 200, 100, Enum.GetNames(typeof(SignType)).Length - 1};
            neuralNetwork = new AccordNet(structure); ///CHANGE
            //neuralNetwork = new NeuralNetworkI(structure, NeuralNetworkI.Arctan); 
            //neuralNetwork = new AccordNet(structure);

            //neuralNetwork.LoadFile($"data_{string.Join("_", structure)}.txt");

            Learn();

            //neuralNetwork.ToFile($"data_{string.Join("_", structure)}.txt");
            LoadTest();

            tglbotic = new TLGBot(this);
            tglbotic.Act();
        }

        private Bots.TLGBot tglbotic;
        Dictionary<SignType, Bitmap> BaseImages = new Dictionary<SignType, Bitmap>();

        private void LoadTest()
        {
            foreach (var file in Directory.GetFiles("test_images"))
                foreach (var t in Enum.GetValues(typeof(SignType)))
                    if (Regex.IsMatch(file, $".*{t.ToString().ToLower()}.*"))
                        BaseImages.Add((SignType)t, new Bitmap(file));
        }

        List<Bitmap> bitmaps = new List<Bitmap>();

        private Sample GetStupidSample(SignType type, Bitmap image, bool flag = false)
        {
            var scaleFilter =
                new AForge.Imaging.Filters.ResizeBilinear(blobWidth, blobHeight);

            Bitmap result = new Bitmap(blobWidth, blobHeight);//scaleFilter.Apply(image));scaleFilter.Apply(image);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.Black);
                var ratio = image.Width / (double)image.Height;
                if (ratio <= 1)
                {
                    g.DrawImage(image, new Rectangle(0, 0, (int)(blobWidth*ratio), blobHeight));
                }
                else
                {
                    g.DrawImage(image, new Rectangle(0, 0, blobWidth, (int)(blobHeight/ ratio))); }
            }
            var input = new double[blobHeight * blobWidth];
            for (int i = 0; i < result.Width; ++i)
                for (int j = 0; j < result.Height; ++j)
                    input[i * result.Height + j] = result.GetPixel(i, j).R / 256.0;

            if (flag)
                pictureButton.BackgroundImage = result;
            else
                bitmaps.Add(result);
            Sample sample = new Sample(input, Enum.GetNames(typeof(SignType)).Length - 1);
            sample.actualClass = type;
            return sample;
        }

        public Sample GetSample(SignType type, Bitmap image, bool flag = false)
        {
            controller.processor.ProcessImage(image);
            Bitmap result = controller.processor.blobImage;
            Sample s = GetStupidSample(type, result, flag);
            if(type != SignType.Undefined)
                s.output[(int)type] = 1;
            return s;
        }

        private Sample GetSample(SignType type, string file)
        {
            Bitmap bmp = new Bitmap(file);
            //Bitmap bmp = new Bitmap(file);

            Console.WriteLine(bmp.PixelFormat);

            return GetSample(type, bmp);
        }

        public void Learn()
        {
            controller.processor.settings.differenceLim = 1.2f;
            SamplesSet samplesSet = new SamplesSet();
            foreach(var file in Directory.GetFiles("learn_images"))
            {
                foreach (var t in Enum.GetValues(typeof(SignType)))
                {
                    if (Regex.IsMatch(file, $".*{t.ToString().ToLower()}.*" ))
                        samplesSet.AddSample(GetSample((SignType)t, file));
                }
            }
            neuralNetwork.updateDelegate += (progress, error, time) => { Console.WriteLine($"progress {progress}, error {error}, time {time}"); };
            neuralNetwork.TrainOnDataSet(samplesSet, 50, 0.0);
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            //  Время засекаем
            sw.Restart();

            //  Отправляем изображение на обработку, и выводим оригинал (с раскраской) и разрезанные изображения
            if (controller.Ready)
            {
#               pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                controller.ProcessImage((Bitmap)eventArgs.Frame.Clone());
#               pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (videoSource == null)
            {
                var vcd = new VideoCaptureDevice(videoDevicesList[cmbVideoSource.SelectedIndex].MonikerString);
                vcd.VideoResolution = vcd.VideoCapabilities[resolutionsBox.SelectedIndex];
                Debug.WriteLine(vcd.VideoCapabilities[1].FrameSize.ToString());
                Debug.WriteLine(resolutionsBox.SelectedIndex);
                videoSource = vcd;
                videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                videoSource.Start();
                StartButton.Text = "Стоп";
                controlPanel.Enabled = true;
                cmbVideoSource.Enabled = false;
            }
            else
            {
                videoSource.SignalToStop();
                if (videoSource != null && videoSource.IsRunning && originalImageBox.Image != null)
                {
                    originalImageBox.Image.Dispose();
                }
                videoSource = null;
                StartButton.Text = "Старт";
                controlPanel.Enabled = false;
                cmbVideoSource.Enabled = true;
            }
        }

        private void tresholdTrackBar_ValueChanged(object sender, EventArgs e)
        {
            controller.settings.threshold = (byte)tresholdTrackBar.Value;
            controller.settings.differenceLim = (float)tresholdTrackBar.Value/tresholdTrackBar.Maximum;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
            }
        }

        private void cmbVideoSource_SelectionChangeCommitted(object sender, EventArgs e)
        {
            var vcd = new VideoCaptureDevice(videoDevicesList[cmbVideoSource.SelectedIndex].MonikerString);
            resolutionsBox.Items.Clear();
            for (int i = 0; i < vcd.VideoCapabilities.Length; i++)
                resolutionsBox.Items.Add(vcd.VideoCapabilities[i].FrameSize.ToString());
            resolutionsBox.SelectedIndex = 0;
        }

        int blobIndex = 0;
        private void blobButton_Click(object sender, EventArgs e)
        {
            blobButton.BackgroundImageLayout = ImageLayout.Zoom;
            ++blobIndex;
            if (blobIndex >= bitmaps.Count)
                blobIndex = 0;
            blobButton.BackgroundImage = bitmaps[blobIndex];
        }
    }
}
