using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AForge.WindowsForms
{
    public delegate void FormUpdater(double progress, double error, TimeSpan time);

    /// <summary>
    /// Базовый класс для реализации как самодельного персептрона, так и обёртки для ActivationNetwork из Accord.Net
    /// </summary>
    abstract public class BaseNetwork
    {
        //  Делегат для информирования о процессе обучения (периодически извещает форму о том, сколько процентов работы сделано)
        public FormUpdater updateDelegate = new FormUpdater( (_, __, ___)=> {});
        
        public abstract void ReInit(int[] structure, double initialLearningRate = 0.25);

        public abstract int Train(Sample sample, bool parallel = true);

        public abstract double TrainOnDataSet(SamplesSet samplesSet, int epochs_count, double acceptable_erorr, bool parallel = true);

        public abstract SignType Predict(Sample sample);

        public virtual void ToFile(string filename)
        {
            throw new NotImplementedException("Эта нейронка не умеет загружаться файл");
        }

        public virtual void LoadFile(string filename)
        {
            throw new NotImplementedException("Эта нейронка не умеет загружаться из файла");
        }
    }
}
