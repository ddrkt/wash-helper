using System;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace AForge.WindowsForms
{
    internal class Settings
    {
        /// Желаемый размер изображения до обработки
        /// </summary>
        public Size orignalDesiredSize = new Size(500, 500);

        /// <summary>
        /// Порог при отсечении по цвету 
        /// </summary>
        public byte threshold = 120;

        public float differenceLim = 0.5f;

    }

    internal class Processor
    {
        /// <summary>
        /// Обработанное изображение
        /// </summary>
        public Bitmap processed;

        /// <summary>
        /// Оригинальное изображение после обработки
        /// </summary>
        public Bitmap original;

        /// <summary>
        /// Тестовое изображение что бы можно было вывести какую-нибудь фигню
        /// </summary>
        public Bitmap blobImage;

        /// <summary>
        /// Класс настроек
        /// </summary>
        public Settings settings = new Settings();

        public bool ProcessImage(Bitmap bitmap)
        {
            //var original = bitmap;
            
            Bitmap original = new Bitmap(bitmap.Width, bitmap.Height,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (Graphics gr = Graphics.FromImage(original))
            {
                gr.DrawImage(bitmap, new Rectangle(0, 0, original.Width, original.Height));
            }

            
            //  Теперь всю эту муть пилим в обработанное изображение
            Grayscale grayFilter = new Grayscale(0.2125, 0.7154, 0.0721);
            var uProcessed = grayFilter.Apply(UnmanagedImage.FromManagedImage(original));

            //  Масштабируем изображение до 500x500 - этого достаточно
            ResizeBilinear scaleFilter =
                new ResizeBilinear(settings.orignalDesiredSize.Width, settings.orignalDesiredSize.Height);
            //uProcessed = scaleFilter.Apply(uProcessed);
            //original = scaleFilter.Apply(original);

            //  Пороговый фильтр применяем. Величина порога берётся из настроек, и меняется на форме
            var threshldFilter = new Threshold();
            threshldFilter.ThresholdValue = (int) (settings.differenceLim * 100);
            threshldFilter.ApplyInPlace(uProcessed);
            var processed = uProcessed.ToManagedImage();


            Invert InvertFilter = new Invert();
            InvertFilter.ApplyInPlace(processed);
            // create an instance of blob counter algorithm
            BlobCounterBase bc = new BlobCounter();
            // set filtering options
            bc.FilterBlobs = true;
            bc.MinWidth = 5;
            bc.MinHeight = 5;
            // set ordering options
            bc.ObjectsOrder = ObjectsOrder.Size;
            Bitmap image = new Bitmap(processed.Width, processed.Height);
            // process binary image
            try
            {
                bc.ProcessImage(processed);
                Blob[] blobs = bc.GetObjectsInformation();
                // extract the biggest blob
                if (blobs.Length > 0)
                {
                    bc.ExtractBlobsImage(processed, blobs[0], false);
                    image = blobs[0].Image.ToManagedImage();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            this.original = original;
            this.processed = processed;
            blobImage = image;
            return true;
        }
    }
}