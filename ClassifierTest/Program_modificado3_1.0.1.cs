﻿/*
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
                                          bool classifyPorn = true, bool verbose = false, bool copyRights = false, string rightsPath = "", double cutoff = 0)
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


                var outPath = Path.Combine(rightsPath, Path.GetFileName(img)); 
                //var outPath = Path.Combine(@"D:\img_wrong", Path.GetFileName(img).Replace(".png", "_" + ctStr + score.ToString("F6") + ".png")); //Path.Combine(img, "wrong", Path.GetFileName(img));

                try
                {
                    if (classifier.ClassifyImage(imgData))
                    {
                        if (tPorn)
                        {
                            ++imgRight;
                            if (copyRights) { File.Copy(img, outPath); }
                        }
                        else
                        {
                            ++imgWrong;
                            
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
                        }
                        else
                        {
                            ++imgRight;
                            //if (verbose) Console.WriteLine("score: {0}", classifier.GetPositiveProbability(imgData));
                            if (copyRights) { File.Copy(img, outPath); }
                        }
                    }

                    if (verbose && (imgRight + imgWrong) % 100 == 0)
                    {
                        if (tPorn)
                        {
                            Console.WriteLine("Clasificadas {0} de {1} imágenes buscando pornográficas.", imgRight + imgWrong, total);
                        }
                        else
                        {
                            Console.WriteLine("Clasificadas {0} de {1} imagenes buscando no pornográficas.", imgRight + imgWrong, total);
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
                Console.WriteLine("Clasificadas {0} imágenes, encontrando {1:0.00}% pornográficas.", imgRight + imgWrong, 100d * ((double)imgRight / (double)(imgRight + imgWrong)));
            }
            else
            {
                Console.WriteLine("Clasificadas {0} imágenes, encontrando {1:0.00}% no pornográficas.", imgRight + imgWrong, 100d * ((double)imgRight / (double)(imgRight + imgWrong)));
            }                      

            Console.WriteLine("El clasificador tardó un promedio de {0:0.00} mSeg por imagen.",  sw.ElapsedMilliseconds / (double)(total * 2));
        }

        [STAThread]
        private static void Main(string[] args)
        {
            var sw = new Stopwatch();
            bool copyRights = false, classifyPorn = true;
            string rightsPath = "";


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
                Console.WriteLine("Buscar Pornográfico/No pornográfico? (P/N)");
                kc = Console.ReadKey().Key;
                Console.WriteLine("");
            }
            classifyPorn = (kc == ConsoleKey.P);

            kc = ConsoleKey.NoName;
            while ((kc != ConsoleKey.S) && (kc != ConsoleKey.N))
            {
                Console.WriteLine("Desea copiar archivos encontrados para inspección? (S/N)");
                kc = Console.ReadKey().Key;
                Console.WriteLine("");
            }
            
            copyRights = (kc == ConsoleKey.S);
            if (copyRights)
            {
                Console.WriteLine("Seleccione carpeta destino para copiar las imágenes...");
                fbDlg.Description = "Seleccione carpeta destino para copiar las imágenes";
                fbDlg.ShowNewFolderButton = true;
                if (fbDlg.ShowDialog() == DialogResult.OK)
                {
                    rightsPath = fbDlg.SelectedPath;
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
                                          bool classifyPorn = true, bool verbose = false, bool copyRights = false, string rightsPath = "", double cutoff = 0)
            */


            var resnetClassifier = GetClassifier();                        
            RunClassifier(resnetClassifier, imgsPath, classifyPorn, true, copyRights, rightsPath, co);

            sw.Stop();

            
            TimeSpan ts = sw.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);

            //Console.WriteLine("Tiempo transcurrido: {0}", sw.Elapsed);
            Console.WriteLine("Tiempo transcurrido: " + elapsedTime);
            Console.WriteLine("Presione cualquier tecla para terminar.");
            Console.ReadKey();




        }
    }
}