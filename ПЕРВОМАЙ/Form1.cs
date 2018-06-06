using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using NAudio.Wave;


namespace ПЕРВОМАЙ
{
    public partial class Form1 : Form
    {
        string pathplay;
        short[] AMPLITUDA;//массив для хранения точек аплитуд
        double[] BPEMA;//массив для хранения точек времени
        short[] Volt;//массив значений амплитуд сигнала Вольт
        short[] dB;//массив значений амплитуд сигнала дБ
        int KOL_TOCHEK = 0;//количество точек исследуемого сигнала
        short[] IZO;//массив для созданного сигнала
        int w = 0;
        WaveIn waveIn;// WaveIn - поток для записи
        WaveFileWriter writer;//Класс для записи в файл
        string outputFilename = "PERBIY.wav";//Имя файла для записи
        public Form1()
        {
            InitializeComponent();
            button2.Enabled = false;//выключаем кнопку преобразовать в Вольт
            button3.Enabled = false;//выключаем кнопку преобразовать в дБ
            button4.Enabled = false;//выключаем кнопку преобразовать в однополярное
        }
        #region ОТКРЫТИЕ ФАЙЛА
        [StructLayout(LayoutKind.Sequential)]//принудительное последовательное размещение членов в порядке их появления
        // Структура, описывающая заголовок WAV файла.
        internal class WavHeader
        {
            // WAV-формат начинается с RIFF-заголовка:

            // Содержит символы "RIFF" в ASCII кодировке
            // (0x52494646 в big-endian представлении)
            public UInt32 ChunkId;

            // 36 + subchunk2Size, или более точно:
            // 4 + (8 + subchunk1Size) + (8 + subchunk2Size)
            // Это оставшийся размер цепочки, начиная с этой позиции.
            // Иначе говоря, это размер файла - 8, то есть,
            // исключены поля chunkId и chunkSize.
            public UInt32 ChunkSize;

            // Содержит символы "WAVE"
            // (0x57415645 в big-endian представлении)
            public UInt32 Format;

            // Формат "WAVE" состоит из двух подцепочек: "fmt " и "data":
            // Подцепочка "fmt " описывает формат звуковых данных:

            // Содержит символы "fmt "
            // (0x666d7420 в big-endian представлении)
            public UInt32 Subchunk1Id;

            // 16 для формата PCM.
            // Это оставшийся размер подцепочки, начиная с этой позиции.
            public UInt32 Subchunk1Size;

            // Аудио формат, полный список можно получить здесь 
            // Для PCM = 1 (то есть, Линейное квантование).
            // Значения, отличающиеся от 1, обозначают некоторый формат сжатия.
            public UInt16 AudioFormat;

            // Количество каналов. Моно = 1, Стерео = 2 и т.д.
            public UInt16 NumChannels;

            // Частота дискретизации. 8000 Гц, 44100 Гц и т.д.
            public UInt32 SampleRate;

            // sampleRate * numChannels * bitsPerSample/8
            public UInt32 ByteRate;

            // numChannels * bitsPerSample/8
            // Количество байт для одного сэмпла, включая все каналы.
            public UInt16 BlockAlign;

            // Так называемая "глубиная" или точность звучания. 8 бит, 16 бит и т.д.
            public UInt16 BitsPerSample;

            // Подцепочка "data" содержит аудио-данные и их размер.

            // Содержит символы "data"
            // (0x64617461 в big-endian представлении)
            public UInt32 Subchunk2Id;

            // numSamples * numChannels * bitsPerSample/8
            // Количество байт в области данных.
            public UInt32 Subchunk2Size;

            // Далее следуют непосредственно Wav данные.
        }  //в этом классе инициализируем переменные заголовка файла
        private static uint KPATHOCT(string chact, uint NZ = 0)
        {
            switch (chact) // с какой функцией работаем?
            {
                case "1": return NZ = 1;
                case "2": return NZ = 2;
                case "4": return NZ = 4;
                case "8": return NZ = 8;
                case "16": return NZ = 16;
                case "32": return NZ = 32;
                case "64": return NZ = 64;
                case "128": return NZ = 128;
                case "256": return NZ = 256;
                case "512": return NZ = 512;
                case "1024": return NZ = 1024;
            }
            return 0;
        }//во сколька раз сократить отображаемых точек      
        private void button1_Click(object sender, EventArgs e)
        {
            chart1.Series[0].Points.Clear();//очистить диаграмму
            if (openFileDialog1.ShowDialog() == DialogResult.OK) //необходимо проверить выбор файла
            {
                pathplay = openFileDialog1.FileName;
                using (var file = File.OpenRead(openFileDialog1.FileName))//создаеем файл, и считываем его
                {

                    var header = new WavHeader();
                    // Размер заголовка
                    var headerSize = Marshal.SizeOf(header);
                    var fileStream = File.OpenRead(openFileDialog1.FileName);
                    var buffer = new byte[headerSize];
                    fileStream.Read(buffer, 0, headerSize); // Чтобы не считывать каждое значение заголовка по отдельности,
                    var headerPtr = Marshal.AllocHGlobal(headerSize); // воспользуемся выделением unmanaged блока памяти
                    Marshal.Copy(buffer, 0, headerPtr, headerSize); // Копируем считанные байты из файла в выделенный блок памяти
                    Marshal.PtrToStructure(headerPtr, header);// Преобразовываем указатель на блок памяти к нашей структуре
                    var durationSeconds = 1.0 * header.ChunkSize / (header.BitsPerSample / 8.0) / header.NumChannels / header.SampleRate;
                    double количествоТочек = header.Subchunk2Size/2;
                   // KOL_TOCHEK = Convert.ToString(количествоТочек);
                    textBox1.Text = Convert.ToString(durationSeconds) + " сек ";//длительность в секундах
                    textBox2.Text = Convert.ToString(header.SampleRate) + " Гц ";//частота дискретизации
                    textBox3.Text = Convert.ToString(количествоТочек);//количество сэмплов
                    textBox4.Text = Convert.ToString(header.BitsPerSample) + " bit ";//разрядность
                    textBox5.Text = Convert.ToString(durationSeconds) + " сек ";//длительность в секундах
                    textBox6.Text = Convert.ToString(header.SampleRate) + " Гц ";//частота дискретизации
                    textBox7.Text = Convert.ToString(количествоТочек);//количество сэмплов
                    textBox8.Text = Convert.ToString(header.BitsPerSample) + " bit ";//разрядность
                    fileStream.Close();//закрываем использование потока 
                }
                using (var file = File.OpenRead(openFileDialog1.FileName))//создаеем файл, и считываем его
                {
                    KOL_TOCHEK = 0;
                    byte[] buffer = new byte[file.Length];
                    file.Read(buffer, 0, buffer.Length);
                    AMPLITUDA = new short [buffer.Length];
                    //BPEMA = new int[KOL_TOCHEK];
                    int j = 0; int y = 0;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (i > 44)//отсеиваем информационный заголовок
                        {
                            if (j < 1)//выбираем значения четных мест в массиве для преобразования из 8 bit byte в 16 bit short
                            {
                                KOL_TOCHEK++;
                                j++;
                                y++;
                                AMPLITUDA[y] = BitConverter.ToInt16(buffer, i - 1);
                                if (KPATHOCT(comboBox4.Text) < w)
                                     {
                                       chart1.Series[0].Points.AddXY(y, AMPLITUDA[y]);//уменьшаем значение амплитуды        
                                       w = 0;
                                     }
                                    else w++;
                             }
                            else
                                j = 0;
                        }
                    }
                }
                chart1.ChartAreas[0].AxisY.Title = "АМПЛИТУДА";//обозначение оси абсцисс
                chart1.ChartAreas[0].AxisX.Title = "НОМЕР ВЫБОРКИ";//обозначение оси ординат
              button2.Enabled = true;//активируем кнопку преобразование в Вольт
            }
        }//считываем заголовок WAV,преобразуем данные в точки значений амплитуд
        private void button2_Click(object sender, EventArgs e)
        {
            Volt = new short[AMPLITUDA.Length];//инициализируем массив значений Вольт
            chart1.Series[0].Points.Clear();//очистить диаграмму
            for (int i = 0; i < KOL_TOCHEK ; i++)
            {
                Volt[i] = Convert.ToInt16 (AMPLITUDA[i] / 64);//переводим значение амплитуды в мV
                if (KPATHOCT(comboBox4.Text) < w)
                {
                    chart1.Series[0].Points.AddXY(i, Volt[i]);      
                    w = 0;
                }
                else w++;  
            }
            chart1.ChartAreas[0].AxisY.Title = "АМПЛИТУДА mV";//обозначение оси абсцисс
            chart1.ChartAreas[0].AxisX.Title = "НОМЕР ВЫБОРКИ сэмплов";//обозначение оси абсцисс
           button3.Enabled = true;//активируем кнопку преобразование в дБ
        }//преобразуем значения амплитуд в вольты
        private void chart1_Click(object sender, EventArgs e)
        {
            chart1.ChartAreas[0].AxisX.TitleFont = new Font("Times New Roman", 20, FontStyle.Bold);//изменяем шастройки шрифта обозначения оси абсцис 
            chart1.ChartAreas[0].AxisY.TitleFont = new Font("Times New Roman", 20, FontStyle.Bold);//изменяем шастройки шрифта обозначения оси ординат
            chart1.ChartAreas[0].CursorX.IsUserEnabled = true;//реализуем возможность масштабирования
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;//включение возможности выбора интервала для масштабирования
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true; //включение масштабирования по оси Х
            chart1.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true; //включаем полосу прокрутки
            chart1.ChartAreas[0].CursorY.IsUserEnabled = true;//реализуем возможность масштабирования
            chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;//включение возможности выбора интервала для масштабирования
            chart1.ChartAreas[0].AxisY.ScaleView.Zoomable = true; //включение масштабирования по оси Y
            chart1.ChartAreas[0].AxisY.ScrollBar.IsPositionedInside = true; //включаем полосу прокрутки
        } //настройки графика
        private void button3_Click(object sender, EventArgs e)
        {
            chart1.Series[0].Points.Clear();//очистить диаграмму
            dB = new short[KOL_TOCHEK];//инициализируем длину массива
            for (int i = 0; i < KOL_TOCHEK; i++)//преобразуем значения типа short в амплитуду Вольт
            {
                dB[i] = Volt[i];
            }

            for (int i = 0; i < KOL_TOCHEK; i++)//преобразуем уровень Вольт в дБ
            {
                if (dB[i] != 0)
                {
                    if (dB[i] < 0)
                    {
                        dB[i] =Convert.ToInt16( dB[i] * -1);
                        dB[i] = Convert.ToInt16( 20 * Math.Log10(dB[i]));
                        dB[i] =Convert.ToInt16 (dB[i] * -1);
                    }
                    else
                        dB[i] = Convert.ToInt16(20 * Math.Log10(dB[i]));
                }
                if (KPATHOCT(comboBox4.Text) < w)
                {
                    chart1.Series[0].Points.AddXY(i, dB[i]);//уменьшаем значение амплитуды        
                    w = 0;
                }
                else w++;
                // chart1.Series[0].Points.AddXY(i, dB[i]);//выводи результат преобразований в виде графика  
            }
            chart1.ChartAreas[0].AxisY.Title = "АМПЛИТУДА дБ";//обозначение оси абсцисс
            button4.Enabled = true;//активируем кнопку преобразование в однополярное
        }//создаем массив Volt, представление в дБ
        private void button4_Click(object sender, EventArgs e)
        {
            
            chart1.Series[0].Points.Clear();//очищаем диаграмму
            for (int i = 0; i < KOL_TOCHEK; i++)
            {
                if (dB[i] < 0)
                { dB[i] = Convert.ToInt16(dB[i] * -1); }
                dB[i] = Convert.ToInt16 (dB[i] - 60);
                chart1.Series[0].Points.AddXY(i, dB[i]);       
            }
            //chart1.ChartAreas[0].AxisY.Title = "УРОВЕНЬ дБ однополярный";//обозначение оси абсцисс
        }//преобразование в одну полярность
        private void button9_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StreamWriter streamWriter = new StreamWriter(saveFileDialog1.FileName);
                for (int i = 0; i < chart1.Series[0].Points.Count; i++)
                streamWriter.WriteLine(chart1.Series[0].Points[i].YValues[0]);
                streamWriter.Close();
            }
        }//сохранение в массива точек значений амплитуд в файл.txt 
        private void button10_Click(object sender, EventArgs e)
        {
            chart1.Series[0].Points.Clear();//очищаем диаграмму
            if (openFileDialog1.ShowDialog() == DialogResult.OK) //необходимо проверить выбор файла
            {
                StreamReader StreamReader = new StreamReader(openFileDialog1.FileName);//создаем поток для чтения
                int X = 0; //добавляем координату на оси Х 
                while (!StreamReader.EndOfStream)
                {
                    int y = Convert.ToInt16(StreamReader.ReadLine());//считывание для преобразования всей строки файла
                    chart1.Series[0].Points.AddXY(X, y);
                    X++;
                }
                StreamReader.Close();  //очищение памяти от потока
            }
        }//отображение графика файл.txt
        private void button11_Click(object sender, EventArgs e)
        {
            
                playSound(pathplay);
           
        }
        //открыть WAV с указанного места
        #endregion 
        #region МОДЕЛИРОВАНИЕ
        private void playSound(string path)
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer();
            player.SoundLocation = path;
            player.Load();
            player.Play();
        }//функция воспроизведения  
        private static double Saw(int index, double frequency, string func)
        {
            switch (func) // с какой функцией работаем?
            {
                case "ПИЛА": return 2.0 * (index * frequency - Math.Floor(index * frequency)) - 1.0;//функция модель пилообразный, принимает два значения: смещение и частоту
                case "ТРЕУГОЛЬНИК": return 2.0 * Math.Abs(2.0 * (index * frequency - Math.Floor(index * frequency + 0.5))) - 1.0;//функция модель треугольный, принимает два значения: смещение и частоту
                case "МЕАНДР": if (Math.Sin(frequency * index) > 0) return 1; else return -1;//функция модель меандра, принимает два значения: смещение и частоту
                case "СИНУСОИДА": return Math.Sin(frequency * index);//функция модель синусоиды, принимает два значения: смещение и частоту
            }
            return 0;
        }//функции ФОРМ СИГНАЛОВ, принимает два значения: смещение и частоту
        public static void SaveWave(Stream stream, short[] data, int sampleRate)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            short frameSize = (short)(16 / 8); // Количество байт в блоке (16 бит делим на 8).
            writer.Write(0x46464952); // Заголовок "RIFF".
            writer.Write(36 + data.Length * frameSize); // Размер файла от данной точки.
            writer.Write(0x45564157); // Заголовок "WAVE".
            writer.Write(0x20746D66); // Заголовок "frm ".
            writer.Write(16); // Размер блока формата.
            writer.Write((short)1); // Формат 1 значит PCM.
            writer.Write((short)1); // Количество дорожек.
            writer.Write(sampleRate); // Частота дискретизации.
            writer.Write(sampleRate * frameSize); // Байтрейт (Как битрейт только в байтах).
            writer.Write(frameSize); // Количество байт в блоке.
            writer.Write((short)16); // разрядность.
            writer.Write(0x61746164); // Заголовок "DATA".
            writer.Write(data.Length * frameSize); // Размер данных в байтах.
            for (int index = 0; index < data.Length; index++)
            { // Начинаем записывать данные из нашего массива.
                foreach (byte element in BitConverter.GetBytes(data[index]))
                { // Разбиваем каждый элемент нашего массива на байты.
                    stream.WriteByte(element); // И записываем их в поток.
                }
            }
        }//Функция сохранения звука РСМ в поток WAV
        private static double HOTA(string chact, int NZ = 0)
        {
            switch (chact) // с какой функцией работаем?
            {
                case "125": return NZ = 125;
                case "250": return NZ = 250;
                case "500": return NZ = 500;
                case "1000": return NZ = 1000;
                case "2000": return NZ = 2000;
                case "4000": return NZ = 4000;
                case "8000": return NZ = 8000;
            }
            return 0;
        }//выбор частоты воспроизведения
        private static int DISKRET(string chact, int NZ = 0)
        {
            switch (chact) // с какой функцией работаем?
            {
                case "8 000 Гц": return NZ = 8000;
                case "11 025 Гц": return NZ = 11025;
                case "12 000 Гц": return NZ = 12000;
                case "16 000 Гц": return NZ = 16000;
                case "22 050 Гц": return NZ = 22050;
                case "24 000 Гц": return NZ = 24000;
                case "32 000 Гц": return NZ = 32000;
                case "44 100 Гц": return NZ = 44100;
                case "48 000 Гц": return NZ = 48000;
                case "96 000 Гц": return NZ = 96000;
                case "192 000 Гц": return NZ = 192000;
            }
            return 0;
        }//выбор частоты дискретизации 
        private void button5_Click(object sender, EventArgs e)
        {
            int sampleRate = DISKRET(comboBox1.Text); // наша частота дискретизации.
            short[] data = new short[sampleRate];  // Инициализируем массив 16 битных значений.
            IZO = new short[sampleRate]; // Инициализируем массив для графика
            double frequency = Math.PI * 2 * (short)HOTA(comboBox3.Text) / sampleRate; // Рассчитываем требующуюся частоту.
            int p = 0; short x = 0;//инициализируем переменную для выборки данных для графика
            for (int index = 0; index < sampleRate; index++)// Перебираем его.
            {
                data[index] = (short)(Saw(index, frequency, comboBox2.Text) * short.MaxValue); // Приводим уровень к амплитуде от 32767 до -32767.
                x = data[index];
                for (int j = 1; j < 2; j++)
                {
                    IZO[p] = x;
                    p++;
                }
            }
           Stream file = File.Create("tes.wav"); // Создаем новый файл и стыкуем его с потоком.
            SaveWave(file, data, sampleRate); // Записываем наши данные в поток.
            file.Close();
           chart1.Series[0].Points.Clear();//очистить диаграмму
            StreamWriter print = new StreamWriter("new_file.txt"); // перезапись в файл 
            //int del = 0;
            for (int i = 0; i < IZO.Length; i++)
            {
                /*if (del > 8)
                {
                    del = 0;*/
                    print.WriteLine(IZO[i]); // запись в файл массива  
                    chart1.Series[0].Points.AddXY(i, IZO[i]);//задаем формулу графика функции
               /* }
                else
                    del++;*/
            }
            print.Close();//закрываем поток
        }//моделирование сигнала
        private void button6_Click(object sender, EventArgs e)
        {
            playSound("tes.wav");
        }//кнопка воспроизвести
        #endregion
        #region ZAPIC ZBYKA
        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<WaveInEventArgs>(waveIn_DataAvailable), sender, e);
            }
            else
            {
                //Записываем данные из буфера в файл
                writer.WriteData(e.Buffer, 0, e.BytesRecorded);
            }
        } //Завершаем запись
        void StopRecording()
        {
            MessageBox.Show("Стоп запись");
            waveIn.StopRecording();
        }//Окончание записи
        private void waveIn_RecordingStopped(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(waveIn_RecordingStopped), sender, e);
            }
            else
            {
                waveIn.Dispose();
                waveIn = null;
                writer.Close();
                writer = null;
            }
        } //Начинаем запись
        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                MessageBox.Show("Старт записи");
                waveIn = new WaveIn();
                waveIn.DeviceNumber = 0;//Дефолтное устройство для записи (если оно имеется)
                waveIn.DataAvailable += waveIn_DataAvailable;//Прикрепляем к событию DataAvailable обработчик, возникающий при наличии записываемых данных
                waveIn.RecordingStopped += new EventHandler(waveIn_RecordingStopped);//Прикрепляем обработчик завершения записи
                waveIn.WaveFormat = new WaveFormat(16000, 1);//Формат wav-файла - принимает параметры - частоту дискретизации и количество каналов(здесь mono)
                writer = new WaveFileWriter(outputFilename, waveIn.WaveFormat);//Инициализируем объект WaveFileWriter
                waveIn.StartRecording();//Начало записи
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message); }
        }//старт записи
        private void button8_Click(object sender, EventArgs e)
        {
            if (waveIn != null)
            {
                StopRecording();
            }
        } //стоп запись
        #endregion



        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
