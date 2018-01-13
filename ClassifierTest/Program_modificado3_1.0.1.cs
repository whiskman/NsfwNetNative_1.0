/*
* Copyright © 2018 Jesse Nicholson
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using NsfwNET;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace ClassifierTest
{
    //internal unsafe 
    class Program
    {

       

        public static ImageClassifier GetClassifier()
        {
            string protoTxtPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy.prototxt");
            string mdlBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resnet_50_1by2_nsfw.caffemodel");   


            var classifier = new ImageClassifier(protoTxtPath, mdlBinPath);

            if(classifier.IsEmpty)
            {
                throw new Exception("Classifier shouldn't be empty. Are you model paths correct?");
            }

            return classifier;
        }

        private static void RunClassifier(ImageClassifier classifier, string ClassifierPath, 
                                          bool classifyPorn = true, bool verbose = false, bool copyWrongs = false, string wrongsPath = "", double cutoff = 0)
        {

            //cutoff = 2 --> usar default
            if (cutoff != 2) { classifier.Cutoff = cutoff; }


            Console.WriteLine("Classifier has cutoff of {0}.", classifier.Cutoff);

            //REVISAR  falla al encontrar directorios con acceso denegado (ej. SVI ó RECYCLE.BIN)
            // Y es lento para grandes tamaños, ver exploración recursiva y EnumerateFiles en su lugar
            //var allImgs = Directory.GetFiles(ClassifierPath, "*.jpg", SearchOption.AllDirectories).ToList();

            //var allImgs = Directory.GetFiles(ClassifierPath, "*.png", SearchOption.AllDirectories).ToList();


            //var allImgs = Directory.GetFiles(ClassifierPath, "*.jpg").ToList();
            var allImgs = Directory.GetFiles(ClassifierPath).ToList();
            var total = allImgs.Count;
            bool tPorn = classifyPorn;

            Console.WriteLine("{0} imágenes a clasificar.", total);


            int imgRight = 0, imgWrong = 0;

            var sw = new Stopwatch();
            sw.Start();

            foreach (var img in allImgs)
            {
                var imgData = File.ReadAllBytes(img);


                var outPath = Path.Combine(wrongsPath, Path.GetFileName(img)); 
                //var outPath = Path.Combine(@"D:\img_wrong", Path.GetFileName(img).Replace(".png", "_" + ctStr + score.ToString("F6") + ".png")); //Path.Combine(img, "wrong", Path.GetFileName(img));

                try
                {
                    if (classifier.ClassifyImage(imgData))
                    {
                        if (tPorn)
                        {
                            ++imgRight;
                        }
                        else
                        {
                            ++imgWrong;
                            if (copyWrongs) { File.Copy(img, outPath); }
                        }
                    }
                    else
                    {
                        // If you feel like inspecting false negatives.
                        //var outPath = Path.Combine(SOME PLACE FOR FALSE NEGATIVES, Path.GetFileName(img));
                        //File.Move(img, outPath);

                        //var outPath = Path.Combine(img, "wrong", Path.GetFileName(img));
                        //File.Copy(img, outPath);

                        if (tPorn)
                        {
                            ++imgWrong;
                            //if (verbose) Console.WriteLine("score: {0}", classifier.GetPositiveProbability(imgData));
                            if (copyWrongs) { File.Copy(img, outPath); }
                        }
                        else
                        {
                            ++imgRight;
                        }
                    }

                    if (verbose && (imgRight + imgWrong) % 100 == 0)
                    {
                        if (tPorn)
                        {
                            Console.WriteLine("Classified {0} of {1} pornographic images.", imgRight + imgWrong, total);
                        }
                        else
                        {
                            Console.WriteLine("Classified {0} of {1} non-pornographic images.", imgRight + imgWrong, total);
                        }

                    }

                    //Console.WriteLine("score: {0}", score);

                }
                catch (Exception e)
                {
                    // Extract some information from this exception, and then   
                    // throw it to the parent method.  
                    if (e.Source != null)
                        Console.WriteLine("Exception source: {0}", e.Source);
                    throw;
                }

            }



            sw.Stop();

            if (tPorn)
            {
                Console.WriteLine("Classified {0} pornographic images with an accuracy of {1}%.", imgRight + imgWrong, 100d * ((double)imgRight / (double)(imgRight + imgWrong)));
            }
            else
            {
                Console.WriteLine("Classified {0} non-pornographic images with an accuracy of {1}%.", imgRight + imgWrong, 100d * ((double)imgRight / (double)(imgRight + imgWrong)));
            }                      

            Console.WriteLine("Classifier took an average of {0} msec per image to classify.",  sw.ElapsedMilliseconds / (double)(total * 2));
        }

        [STAThread]
        private static void Main(string[] args)
        {
            var sw = new Stopwatch();
            bool copyWrongs = false, classifyPorn = true;
            string wrongsPath = "";


            Console.WriteLine("Ingrese el cutoff [0,001 - 1,0] (2 = default)");

            double co = 0;
            while (!double.TryParse(Console.ReadLine(), out co) || !(co > 0 && co <= 1 || co == 2))
            {
                // .. error with input
                Console.WriteLine("Entrada errónea"); 
            }
            


            Console.WriteLine("Seleccione carpeta origen de las imágenes...");

            FolderBrowserDialog fbDlg = new FolderBrowserDialog();
            string imgsPath = "";
            fbDlg.Description = "Seleccione carpeta origen de las imágenes";
            fbDlg.ShowNewFolderButton = false;

            if (fbDlg.ShowDialog() == DialogResult.OK)
            {
                imgsPath = fbDlg.SelectedPath;
            }
            else
            {
                Console.WriteLine("Debe seleccionar una ruta");
                Thread.Sleep(2000);
                Environment.Exit(0);
            }


            var kc = ConsoleKey.NoName;

            while ((kc != ConsoleKey.P) && (kc != ConsoleKey.N))
            {
                Console.WriteLine("Clasificar Pornográfico/No pornográfico? (P/N)");
                kc = Console.ReadKey().Key;
                Console.WriteLine("");
            }
            classifyPorn = (kc == ConsoleKey.P);

            kc = ConsoleKey.NoName;
            while ((kc != ConsoleKey.S) && (kc != ConsoleKey.N))
            {
                Console.WriteLine("Desea copiar archivos erroneos (falsos negativos) para inspección? (S/N)");
                kc = Console.ReadKey().Key;
                Console.WriteLine("");
            }
            
            copyWrongs = (kc == ConsoleKey.S);
            if (copyWrongs)
            {
                Console.WriteLine("Seleccione carpeta destino para copiar las imágenes...");
                fbDlg.Description = "Seleccione carpeta destino para copiar las imágenes";
                fbDlg.ShowNewFolderButton = true;
                if (fbDlg.ShowDialog() == DialogResult.OK)
                {
                    wrongsPath = fbDlg.SelectedPath;
                }
                else
                {
                    Console.WriteLine("Debe seleccionar una ruta");
                    Thread.Sleep(2000);
                    Environment.Exit(0);
                }
            }


            sw.Start();


            /*
                private static void RunClassifier(ImageClassifier classifier, string ClassifierPath, 
                                          bool classifyPorn = true, bool verbose = false, bool copyWrongs = false, string wrongsPath = "", double cutoff = 0)
            */


            var resnetClassifier = GetClassifier();                        
            RunClassifier(resnetClassifier, imgsPath, classifyPorn, true, copyWrongs, wrongsPath, co);

            sw.Stop();

            /*
            TimeSpan ts = sw.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
                */
            Console.WriteLine("Tiempo transcurrido: {0}", sw.Elapsed);
            Console.WriteLine("Presione cualquier tecla para terminar.");
            Console.ReadKey();




        }
    }
}