using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio;
using NAudio.Wave;
using NAudio.Dsp;
using System.Diagnostics;


namespace AudioInput
{
   
       

    public partial class MainWindow : Window
    {

        Stopwatch timer = new Stopwatch();
        Stopwatch milisegundos = new Stopwatch();


        Nota [] notaMorada = new Nota[1];
        int contadorNotaMorada = 0;

        Image [] morado = new Image[1];
        int contadorMorado = 0;

        WaveIn wavein;
        WaveFormat formato;

        bool poder = false;

        int puntaje = 0;
        int puntosPoder = 0;
        int cronometro = 78000;

        double mDBoton;
        double mIBoton;
        double canvasLeft;
        double canvasRight;

        double pasoNota = Math.Pow(2, 1.0 / 12.0);
        double frecuenciaLaBase = 110.0;
        
        public MainWindow()
        {
            milisegundos.Start();

            InitializeComponent();

            notaMorada[contadorNotaMorada] = new Nota();

            notaMorada[contadorNotaMorada].setMomento(210);
            notaMorada[contadorNotaMorada].setBoton(1);




            //contadorMorado += 1;

           


        }

        private void btnIniciar_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            



            wavein = new WaveIn();
            wavein.WaveFormat = new WaveFormat(44100, 16, 1);
            formato = wavein.WaveFormat;

            wavein.DataAvailable += OnDataAvailable;
            wavein.BufferMilliseconds = 500;

            wavein.StartRecording();



            aparecerNotaMorado(notaMorada[contadorNotaMorada]);

            Canvas.SetLeft(morado[contadorMorado], 80);
            Canvas.SetRight(morado[contadorMorado], 400);
            /*
            Canvas.SetLeft(imgTrack, 80);
            Canvas.SetRight(imgTrack, 90);

            

            canvasLeft = Canvas.GetLeft(imgTrack);
            canvasRight = Canvas.GetRight(imgTrack);

            lblMITrack.Text = Convert.ToString(canvasLeft);
            lblMDTrack.Text = Convert.ToString(canvasRight);
            */

        }
        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] buffer = e.Buffer;
            int bytesGrabados = e.BytesRecorded;

            double acumulador = 0;

            double nummuestras = bytesGrabados / 2;
            int exponente = 1;
            int numeroMuestrasComplejas = 0;
            int bitsMaximos = 0;

            do
            {
                bitsMaximos = (int)Math.Pow(2, exponente);
                exponente++;
            } while (bitsMaximos < nummuestras);

            exponente -= 2;
            numeroMuestrasComplejas = bitsMaximos / 2;

            Complex[] muestrasComplejas =
                new Complex[numeroMuestrasComplejas];

            for (int i = 0; i < bytesGrabados; i += 2)
            {
                short muestra = (short)(buffer[i + 1] << 8 | buffer[i]);

                float muestra32bits = (float)muestra / 32768.0f;

                acumulador += Math.Abs(muestra32bits);
                if (i / 2 < numeroMuestrasComplejas)
                {
                    muestrasComplejas[i / 2].X = muestra32bits;
                }
            }
            double promedio = acumulador / ((double)bytesGrabados / 2.0);

            if (promedio > 0)
            {
                FastFourierTransform.FFT(true, exponente, muestrasComplejas);
                float[] valoresAbsolutos =
                    new float[muestrasComplejas.Length];

                for (int i = 0; i < muestrasComplejas.Length; i++)
                {
                    valoresAbsolutos[i] = (float)
                        Math.Sqrt((muestrasComplejas[i].X * muestrasComplejas[i].X) +
                        (muestrasComplejas[i].Y * muestrasComplejas[i].Y));
                }

                int indiceMaximo =
                    valoresAbsolutos.ToList().IndexOf(
                        valoresAbsolutos.Max());

                float frecFundamental = (float)(indiceMaximo * wavein.WaveFormat.SampleRate) / (float)valoresAbsolutos.Length;

                lblFrecuencia.Text = frecFundamental.ToString("n2");

                
                moverBoton();
                detectarFrecuencia(frecFundamental);

                int octava = 0;
                int indiceTono = (int)Math.Round(Math.Log10(frecFundamental / frecuenciaLaBase) / Math.Log10(pasoNota));
                if (indiceTono < 0)
                {
                    do
                    {
                        indiceTono += 12;
                        octava--;
                    } while (indiceTono < 0);
                }
                else if (indiceTono > 11)
                {
                    do
                    {
                        octava++;
                        indiceTono -= 12;
                    } while (indiceTono > 11);
                }
                double frecTono = frecuenciaLaBase;
                for (int i = 0; i < Math.Abs(octava); i++)
                {
                    if (octava > 0)
                    {
                        frecTono *= 2.0;
                    }
                    else if (octava < 0)
                    {
                        frecTono /= 2.0;
                    }
                }

                for (int i = 0; i < indiceTono; i++)
                {
                    frecTono *= Math.Pow(2, 1.0 / 12.0);
                }

                double proxTono = frecTono * Math.Pow(2, 1.0 / 12.0);
                double antTono = frecTono / Math.Pow(2, 1.0 / 12.0);
                double rango = proxTono - antTono;
                double frecNormalizada = (frecFundamental - antTono) / rango;
            } else
            {
                lblFrecuencia.Text = "0";
            }   
        }

        void notas()
        {
            
        }

        void moverBoton()
        {

            mIBoton = Canvas.GetLeft(morado[contadorMorado]);
            lblMIBoton.Text = Convert.ToString(mIBoton);

            mDBoton = Canvas.GetRight(morado[contadorMorado]);
            lblMDBoton.Text = Convert.ToString(mDBoton);

            if (milisegundos.ElapsedMilliseconds >= 100)
            {
                mIBoton += 21;
                mDBoton += 21;
                milisegundos.Restart();
            }

            Canvas.SetLeft(morado[contadorMorado], mIBoton);
            Canvas.SetRight(morado[contadorMorado], mDBoton);
            




        }

        

        void detectarFrecuencia(float frecuenciaFundamental)
        {
            int boton = 0;

            if ((frecuenciaFundamental > 860 && frecuenciaFundamental < 900)
                || (frecuenciaFundamental > 760 && frecuenciaFundamental < 800))
            {
                boton = 1;
                txtBoton.Text = "VERDE";
            }
            if ((frecuenciaFundamental > 1026 && frecuenciaFundamental < 1066)
                || (frecuenciaFundamental > 967 && frecuenciaFundamental < 997))
            {
                boton = 2;
                txtBoton.Text = "ROJO";
            }
            if ((frecuenciaFundamental > 420 && frecuenciaFundamental < 460)
                || (frecuenciaFundamental > 1154 && frecuenciaFundamental < 1194))
            {
                boton = 3;
                txtBoton.Text = "AMARILLO";
            }
            if ((frecuenciaFundamental > 1459 && frecuenciaFundamental < 1499)
                || (frecuenciaFundamental > 503 && frecuenciaFundamental < 543))
            {
                boton = 4;
                txtBoton.Text = "AZUL";
            }
            if ((frecuenciaFundamental > 602 && frecuenciaFundamental < 642)
                 || (frecuenciaFundamental > 643 && frecuenciaFundamental < 677))
            {
                boton = 5;
                txtBoton.Text = "NARANJA";
            }
            if ((frecuenciaFundamental > 678 && frecuenciaFundamental < 718)
                || (frecuenciaFundamental > 763 && frecuenciaFundamental < 793))
            {
                boton = 6;
                txtBoton.Text = "ROSA";
            }
            if ((frecuenciaFundamental > 860 && frecuenciaFundamental < 900)
                 || (frecuenciaFundamental > 1026 && frecuenciaFundamental < 1066))
            {
                boton = 7;
                txtBoton.Text = "MORADO";
            }

            pulsarBoton(boton);
            
        }

        void pulsarBoton(int boton)
        {
            bool acierto = false;

            if (boton == 1)
            {
                if (mIBoton <= 634 && mDBoton >= 588)
                {
                    acierto = true;
                }
            }
            if (boton == 2 && mIBoton <= 634 && mDBoton >= 588)
            {
                acierto = true;
            }
            if (boton == 3 && mIBoton <= 634 && mDBoton >= 588)
            {
                acierto = true;
            }
            if (boton == 4 && mIBoton <= 634 && mDBoton >= 588)
            {
                acierto = true;
            }
            if (boton == 5 && mIBoton <= 634 && mDBoton >= 588)
            {
                acierto = true;
            }
            if (boton == 6 && mIBoton <= 634 && mDBoton >= 588)
            {
                acierto = true;
            }
            if (boton == 7)
            {
                poder = true;
            }

            if (acierto)
            {
                puntaje += 100;
                puntosPoder += 1;
            }

            if (poder && puntosPoder >= 10)
            {
                activarPoder();
            }

            acierto = false;
            lblPuntaje.Text = Convert.ToString(puntaje);
        }

        void activarPoder()
        {
            lblPoder.Text = "Activado";
            if (cronometro <= 0)
            {
                lblPoder.Text = "Desactivado";
                cronometro = 100;
                poder = false;
            }
            cronometro -= 1;
        }

        void aparecerNotaMorado(Nota nota)
        {
            //APARECER MORADO
            morado[contadorMorado] = new Image();

            morado[contadorMorado].Source = new BitmapImage(new Uri(@"graficos/Green.png", UriKind.RelativeOrAbsolute));
            morado[contadorMorado].Width = 50;
            morado[contadorMorado].Height = 50;
            Canvas.SetLeft(morado[contadorMorado], 7);
            Canvas.SetTop(morado[contadorMorado], 50);


            if (notaMorada[contadorNotaMorada].getBoton() == 1)
            {
                gridPrincipal.Children.Add(morado[contadorMorado]);
            }


        }


        

        


        private void btnFinalizar_Click(object sender, RoutedEventArgs e)
        {
            wavein.StopRecording();
        }
    }
}
