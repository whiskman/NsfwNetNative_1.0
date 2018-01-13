using NsfwNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClassifierTest
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            string allImagesTesting = "";

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                allImagesTesting = fbd.SelectedPath;               
            } 

            string protoTxtPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deploy.prototxt");
            string mdlBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resnet_50_1by2_nsfw.caffemodel");

            var classifier = new ImageClassifier(protoTxtPath, mdlBinPath);

            Console.WriteLine("Loaded classifier.");
            Console.WriteLine("Classifier has cutoff of {0}.", classifier.Cutoff);

            /*
            string badImagesTesting = @"E:\img_malas";
            string goodImagesTesting = @"E:\img_buenas";
            string allImagesTesting = @"G:\Temp\$Temp\pics";
            */

            //string allImagesTesting = @"G:\Temp\$Temp\pics";


            //classifier.Cutoff = 0.15;

            //var badImgs = Directory.GetFiles(badImagesTesting).ToList();
            //var goodImgs = Directory.GetFiles(goodImagesTesting).ToList();

            //Esto lista todas las .jpg en la carpeta seleccionada y todas sus subcarpetas
            var allImgs = Directory.GetFiles(allImagesTesting, "*.jpg", SearchOption.AllDirectories).ToList();
            var total = allImgs.Count;

            //Creo subcarpeta para mover los falsos negativos
            Directory.CreateDirectory(Path.Combine(allImagesTesting, "wrong"));

            // Cut down our test base so it's not so huge.
            /*
            var min = Math.Min(badImgs.Count, goodImgs.Count);

            if(min > 1000)
            {
                min = 1000;
            }

            badImgs = badImgs.GetRange(0, min);
            goodImgs = goodImgs.GetRange(0, min);


            // Shuffle things up a bit.
            Random r = new Random();
            badImgs = badImgs.OrderBy(x => r.Next()).ToList();
            goodImgs = goodImgs.OrderBy(x => r.Next()).ToList();
            */

            int goodRight = 0, goodWrong = 0, badRight = 0, badWrong = 0;

            var sw = new Stopwatch();
            sw.Start();


            foreach(var img in allImgs)
            {
                var imgData = File.ReadAllBytes(img);

                if(classifier.ClassifyImage(imgData))
                {
                    ++badRight;
                }
                else
                {
                    // If you feel like inspecting false negatives.

                   var outPath = Path.Combine(allImagesTesting,"wrong", Path.GetFileName(img));
                    //File.Move(img, outPath);
                    File.Copy(img, outPath);
                    ++badWrong;
                }

                //Publica avance cada 10 imágenes procesadas
                if((badRight + badWrong) % 10 == 0)
                {
                    Console.WriteLine("Classified {0} of {1} bad images.", badRight + badWrong, total);
                }
            }


            /*

            foreach(var img in badImgs)
            {
                var imgData = File.ReadAllBytes(img);

                if(classifier.ClassifyImage(imgData))
                {
                    ++badRight;
                }
                else
                {
                    // If you feel like inspecting false negatives.
                    // var outPath = Path.Combine(SOME_BASE_PATH_FOR_FALSE_NEGATIVES, Path.GetFileName(img));
                    // File.Move(img, outPath);
                    var outPath = Path.Combine(badImagesTesting,"wrong", Path.GetFileName(img));
                    File.Move(img, outPath);
                    ++badWrong;
                }

                if((badRight + badWrong) % 10 == 0)
                {
                    Console.WriteLine("Classified {0} of {1} bad images.", badRight + badWrong, min);
                }
            }

            foreach(var img in goodImgs)
            {
                var imgData = File.ReadAllBytes(img);

                if(classifier.ClassifyImage(imgData))
                {
                    ++goodWrong;
                }
                else
                {
                    ++goodRight;
                }

                if((goodWrong + goodRight) % 10 == 0)
                {
                    Console.WriteLine("Classified {0} of {1} good images.", goodWrong + goodRight, min);
                }
            }

            */

            sw.Stop();

            Console.WriteLine("Classified pornographic images with an accuracy of {0}%.", 100d * ((double)badRight / (double)(badRight + badWrong)));

            Console.WriteLine("Classified non-pornographic images with an accuracy of {0}%.", 100d * ((double)goodRight / (double)(goodRight + goodWrong)));

            //Console.WriteLine("Took an average of {0} msec per image to classify.", sw.ElapsedMilliseconds / (double)(min * 2));
            Console.WriteLine("Took an average of {0} msec per image to classify.", sw.ElapsedMilliseconds / (double)(total));

            Console.ReadLine(); //Pausa para que no cierre consola
        }
    }
}
