using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;

namespace AForge.WindowsForms
{



    /// <summary>
    /// Класс-диспетчер, управляющий всеми остальными и служащий для связи с формой
    /// </summary>
    class Controller
    {
        private FormUpdateDelegate formUpdateDelegate = null;

        /// <summa>
        /// Анализатор изображения - выполняет преобразования изображения с камеры и сопоставление с шаблонами
        /// </summary>
        public Processor processor = new Processor();
        
        /// <summary>
        /// Проверить, работает ли это
        /// </summary>
        /// <returns></returns>
        public Settings settings
        {
            get { return processor.settings; }
            set
            {
                processor.settings = value;
            }
        }

        private bool _imageProcessed = true;

        /// <summary>
        /// Готов ли процессор к обработке нового изображения
        /// </summary>
        public bool Ready { get { return _imageProcessed; } }

        /// <summary>
        /// Класс чтобы править ими всеми - и художником, и певцом, и мудрецом
        /// </summary>
        /// <param name="updater"></param>
        public Controller(FormUpdateDelegate updater)
        {
            formUpdateDelegate = updater;
        }
        
        /// <summary>
        /// Задаёт изображение для обработки
        /// </summary>
        /// <param name="image">Собственно изображение для обработки</param>
        /// <returns></returns>
        async public Task<bool> ProcessImage(Bitmap image)
        {
            if (!Ready) return false;
            _imageProcessed = false;

            bool processResult = await Task.Run(() => processor.ProcessImage(image));

            formUpdateDelegate();
            //  Этот блок сработает только по завершению обработки изображения
            //  Устанавливаем значение флажка о том, что мы закончили с обработкой изображения
            _imageProcessed = true;

            return true;
        }

        /// <summary>
        /// Получает обработанное изображение
        /// </summary>
        /// <returns></returns>
        public Bitmap GetOriginalImage()
        {
            return processor.original;
        }

        /// <summary>
        /// Получает обработанное изображение
        /// </summary>
        /// <returns></returns>
        public Bitmap GetProcessedImage()
        {
            return processor.processed;
        }


        /// <summary>
        /// Получает обработанное изображение
        /// </summary>
        /// <returns></returns>
        public Bitmap GetMyTestImage()
        {
            return processor.blobImage;
        }



    }
}
