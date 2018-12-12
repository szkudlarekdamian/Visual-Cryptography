using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Drawing.Imaging;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using AForge.Imaging.Filters;

namespace Visual_Cryptography
{
    public partial class MainWindow : Window
    {
        String helpT = "Autor: Damian Szkudlarek 2018\n\n1. Opis algorytmu\n\tProgram wczytuje zdjęcie. Jeśli zdjęcie jest kolorowe, konwertuje je na skalę szarości. \n\tW następnym kroku zdjęcie jest poddane progowaniu binarnemu iteracyjnemu.\n\tW zależności od wybranej opcji, w miejsce jednego piksela obrazu wejściowego, w obrazie wyjściowym wstawione zostaną dwa lub cztery piksele.\n\tW przypadku obu opcji, kodowanie pikseli białych i czarnych losowane jest z równym prawdopodobieństwem z dwóch zbiorów.\n\tW przypadku odkodowania udziałów obraz zostaje poddany operacji morfologicznej - domknięciu, aby pozbyć się szumu.\n\n2. Przykład dla dwóch udziałów 1px:2px\n\n\t\t\t0 1\t\t 1 0\n\t\tc0 = [ {  \t0 1 \t}, {  \t 1 0 \t} ]\n\t\t\n\t\t\t1 0\t\t 0 1\n\t\tc1 = [ {  \t0 1 \t}, {  \t 1 0 \t} ]\n\t\t\n\tc0 to kodowanie piksela białego, c1 to kodowanie piksela czarnego\n\tJeśli pierwszy piksel obrazu wejściowego byłby biały, wylosowane zostałoby kodowanie pierwsze - pierwszy i drugi piksel udziałów byłyby kolejno koloru białego i \n\tczarnego. \n\tJeśli drugi piksel obrazu wejściowego byłby czarny, wylosowane zostałoby kodowanie drugie - pierwszy i drugi piksel pierwszego udziału byłyby kolejno koloru białego\n\t i czarnego, a drugiego udziału czarnego i białego.\n\tKolejnych pikseli też dotyczy ta zasada. W efekcie tego zabiegu szerokość udziałów jest dwa razy większa, niż obrazu wejściowego.\n\n3. Dane wejściowe i wyjściowe\n\tNa wejście akceptowane są pliki graficzne z rozszerzeniami *.bmp; *.png; *.jpg; *.gif; *.jpeg\n\tW takich samych formatach pliki wyjściowe mogą zostać zapisane.\n\tAutomatycznie generowane udziały zapisywane są jako bitmapy, aby nie uszkodzić zakodowanych informacji kompresją.\n\t\n4. Rozmiar danych\n\tNie ma ograniczeń rozmiaru danych, ale duże zdjęcia będą dłużej przechodziły przez proces kryptograficzny.\n\n5. Ograniczenia\n\tDozwolone rozszerzenia zostały opisane w punkcie 3.\n\tFunkcja Demo nie odzwierciedla poprawnie kolorów. Szary kolor w rzeczywistości jest czarny, a szum biało-czarny jest biały.\n\tRozwiązanie ma charakter poglądowy, w celu prawidłowego odzwierciedlenia obrazu wyjściowego, należy zapisać plik.\n\n6. Środowisko programistyczne i wykonawcze\n\tAplikacja WPF została napisana w języku C# z wykorzystaniem XAML.\n\tProgram skompilowano w środowisku Microsoft Visual Studio 2017.\n\tUżyto filtrów z biblioteki AForge";
        private String[] imageFileName;
        private Bitmap bitmapToBeMerged = null;

        private System.Windows.Controls.Image draggedImage;
        private System.Windows.Point mousePosition;

        private Color[,] whites = new Color[,] { { Color.White, Color.Black, Color.White, Color.Black }, { Color.Black, Color.White, Color.Black, Color.White } };
        private Color[,] blacks = new Color[,] { { Color.Black, Color.White, Color.White, Color.Black }, { Color.White, Color.Black, Color.Black, Color.White } };

        private Color[,] whites4 = new Color[,] { { Color.White, Color.White, Color.Black, Color.Black, Color.White, Color.White, Color.Black, Color.Black }, { Color.Black, Color.Black, Color.White, Color.White, Color.Black, Color.Black, Color.White, Color.White }, { Color.White, Color.Black, Color.White, Color.Black, Color.White, Color.Black, Color.White, Color.Black }, { Color.Black, Color.White, Color.Black, Color.White, Color.Black, Color.White, Color.Black, Color.White }, { Color.White, Color.Black, Color.Black, Color.White, Color.White, Color.Black, Color.Black, Color.White }, { Color.Black, Color.White, Color.White, Color.Black, Color.Black, Color.White, Color.White, Color.Black } };
        private Color[,] blacks4 = new Color[,] { { Color.Black, Color.White, Color.White, Color.Black, Color.White, Color.Black, Color.Black, Color.White }, { Color.White, Color.Black, Color.Black, Color.White, Color.Black, Color.White, Color.White, Color.Black }, { Color.White, Color.White, Color.Black, Color.Black, Color.Black, Color.Black, Color.White, Color.White }, { Color.Black, Color.Black, Color.White, Color.White, Color.White, Color.White, Color.Black, Color.Black }, { Color.White, Color.Black, Color.White, Color.Black, Color.Black, Color.White, Color.Black, Color.White }, { Color.Black, Color.White, Color.Black, Color.White, Color.White, Color.Black, Color.White, Color.Black } };

        private Random random = new Random();

        public MainWindow()
        {
            InitializeComponent();
            help.Text = helpT;
        }
        private void showAlert(String text)
        {
            string caption = "Uwaga";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;
            MessageBox.Show(text, caption, button, icon);
        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private bool isWhite(float brightness)
        {
            if (brightness == 0)
                return false;
            else
                return true;
        }


        private Bitmap[] twoPixelsF(Bitmap b)
        {
            Bitmap bitmap = new Bitmap(b);
            Bitmap share1 = new Bitmap(bitmap.Width * 2, bitmap.Height);
            Bitmap share2 = new Bitmap(bitmap.Width * 2, bitmap.Height);
            for (int x = 0, z = 0; x < bitmap.Width; x++, z += 2)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    if (isWhite(bitmap.GetPixel(x, y).GetBrightness()))//biały
                    {
                        var index = random.Next(0, whites.GetLength(0));
                        share1.SetPixel(z, y, whites[index, 0]);
                        share1.SetPixel(z + 1, y, whites[index, 1]);
                        share2.SetPixel(z, y, whites[index, 2]);
                        share2.SetPixel(z + 1, y, whites[index, 3]);
                    }
                    else //czarny
                    {
                        var index = random.Next(0, blacks.GetLength(0));
                        share1.SetPixel(z, y, blacks[index, 0]);
                        share1.SetPixel(z + 1, y, blacks[index, 1]);
                        share2.SetPixel(z, y, blacks[index, 2]);
                        share2.SetPixel(z + 1, y, blacks[index, 3]);
                    }
                }
            }
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(BitmapToImageSource(share1)));
            using (var fileStream = new System.IO.FileStream(imageFileName[0] + "\\" + imageFileName[1] + "_share1." + imageFileName[2], System.IO.FileMode.Create))
            {
                encoder.Save(fileStream);
            }

            encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(BitmapToImageSource(share2)));
            using (var fileStream = new System.IO.FileStream(imageFileName[0] + "\\" + imageFileName[1] + "_share2." + imageFileName[2], System.IO.FileMode.Create))
            {
                encoder.Save(fileStream);
            }

            return new Bitmap[] { share1, share2 };

        }
        private Bitmap[] fourPixelsF(Bitmap b)
        {
            Bitmap bitmap = new Bitmap(b);
            Bitmap share1 = new Bitmap(bitmap.Width * 2, bitmap.Height * 2);
            Bitmap share2 = new Bitmap(bitmap.Width * 2, bitmap.Height * 2);
            for (int x = 0, z = 0; x < bitmap.Width; x++, z += 2)
            {
                for (int y = 0, k = 0; y < bitmap.Height; y++, k += 2)
                {
                    if (isWhite(bitmap.GetPixel(x, y).GetBrightness()))//biały
                    {
                        var index = random.Next(0, whites4.GetLength(0));
                        share1.SetPixel(z, k, whites4[index, 0]);
                        share1.SetPixel(z + 1, k, whites4[index, 1]);
                        share1.SetPixel(z, k + 1, whites4[index, 2]);
                        share1.SetPixel(z + 1, k + 1, whites4[index, 3]);
                        share2.SetPixel(z, k, whites4[index, 4]);
                        share2.SetPixel(z + 1, k, whites4[index, 5]);
                        share2.SetPixel(z, k + 1, whites4[index, 6]);
                        share2.SetPixel(z + 1, k + 1, whites4[index, 7]);
                    }
                    else //czarny
                    {
                        var index = random.Next(0, blacks4.GetLength(0));
                        share1.SetPixel(z, k, blacks4[index, 0]);
                        share1.SetPixel(z + 1, k, blacks4[index, 1]);
                        share1.SetPixel(z, k + 1, blacks4[index, 2]);
                        share1.SetPixel(z + 1, k + 1, blacks4[index, 3]);
                        share2.SetPixel(z, k, blacks4[index, 4]);
                        share2.SetPixel(z + 1, k, blacks4[index, 5]);
                        share2.SetPixel(z, k + 1, blacks4[index, 6]);
                        share2.SetPixel(z + 1, k + 1, blacks4[index, 7]);
                    }
                }
            }
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(BitmapToImageSource(share1)));
            using (var fileStream = new System.IO.FileStream(imageFileName[0] + "\\" + imageFileName[1] + "_share1" + imageFileName[2], System.IO.FileMode.Create))
            {
                encoder.Save(fileStream);
            }

            encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(BitmapToImageSource(share2)));
            using (var fileStream = new System.IO.FileStream(imageFileName[0] + "\\" + imageFileName[1] + "_share2" + imageFileName[2], System.IO.FileMode.Create))
            {
                encoder.Save(fileStream);
            }

            return new Bitmap[] { share1, share2 };
        }


        private void CanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var image = e.Source as System.Windows.Controls.Image;

            if (image != null && canvas.CaptureMouse())
            {
                mousePosition = e.GetPosition(canvas);
                draggedImage = image;
                draggedImage.Width = image.Width;
                draggedImage.Height = image.Height;
                //Panel.SetZIndex(draggedImage, 1); // in case of multiple images
            }
        }

        private void CanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (draggedImage != null)
            {
                canvas.ReleaseMouseCapture();
                Panel.SetZIndex(draggedImage, 0);
                draggedImage = null;
            }
        }

        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (draggedImage != null)
            {
                var position = e.GetPosition(canvas);
                var offset = position - mousePosition;
                Console.WriteLine("L:" + Canvas.GetLeft(draggedImage) + "T:" + Canvas.GetTop(draggedImage) + "R:" + (canvas.Height / draggedImage.ActualHeight) * draggedImage.ActualWidth);
                var left = Canvas.GetLeft(draggedImage);
                var top = Canvas.GetTop(draggedImage);
                mousePosition = position;
                if (left < 0)
                    left = 0;
                if (left > (canvas.Width - (canvas.Height / draggedImage.Height) * draggedImage.Width))
                    left = canvas.Width - (canvas.Height / draggedImage.Height) * draggedImage.Width;

                Canvas.SetLeft(draggedImage, left + offset.X);
            }
        }

        private void mergeFirst_Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Pliki graficzne|*.bmp; *.png; *.jpg; *.gif; *.jpeg"
            };
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                var bitmap = new Bitmap(dlg.FileName);
                var h = bitmap.Height;
                var w = bitmap.Width;

                if (h > 300 && w > 800) { h = 300; w = 800; }
                else if (h > 300 && w <= 800) { h = 300; }
                else if (h < 300 && w > 800) { w = 800; }

                System.Windows.Controls.Image image = null;
                if (canvas.Children.Count == 1)
                {
                    image = new System.Windows.Controls.Image { Source = BitmapToImageSource(bitmap), Height = h, Width = w, Opacity = 0.5 };
                    demo.Visibility = Visibility.Visible;
                }
                else if (canvas.Children.Count == 0)
                    image = new System.Windows.Controls.Image { Source = BitmapToImageSource(bitmap), Height = h, Width = w };
                else
                    return;
                mergeFirst.Source = image.Source;
                Canvas.SetLeft(image, 0);
                Canvas.SetTop(image, 0);
                canvas.Children.Add(image);

                if (bitmapToBeMerged == null)
                    bitmapToBeMerged = (Bitmap)bitmap.Clone();
                else
                {
                    bitmapToBeMerged = addBitmaps(bitmapToBeMerged, bitmap);
                    mergedShares.Source = BitmapToImageSource(bitmapToBeMerged);
                    saveMerged_Button.Visibility = Visibility.Visible;
                }

                bitmap.Dispose();
            }
        }

        private Bitmap addBitmaps(Bitmap bitmap1, Bitmap bitmap2)
        {
            Bitmap bitmap3 = new Bitmap(bitmap1.Width, bitmap1.Height);
            if (bitmap1.Width == bitmap2.Width && bitmap1.Height == bitmap2.Height)
            {
                for (int x = 0; x < bitmap1.Width; x++)
                {
                    for (int y = 0; y < bitmap1.Height; y++)
                    {
                        Color cb1 = bitmap1.GetPixel(x, y);
                        Color cb2 = bitmap2.GetPixel(x, y);


                        if (cb1.Name == "ff000000" || cb2.Name == "ff000000")
                        {
                            bitmap3.SetPixel(x, y, Color.Black);
                        }
                        else
                            bitmap3.SetPixel(x, y, Color.White);
                    }
                }

                bitmap3 = bitmap3.CloseMorphologyFilter(5, true, true, true);

                return bitmap3;
            }
            else
                showAlert("Udziały mają różne rozmiary!");
            return null;
        }

        private void mergeSecond_Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Pliki graficzne|*.bmp; *.png; *.jpg; *.gif; *.jpeg"
            };
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                var bitmap = new Bitmap(dlg.FileName);
                var h = bitmap.Height;
                var w = bitmap.Width;

                if (h > 300 && w > 800) { h = 300; w = 800; }
                else if (h > 300 && w <= 800) { h = 300; }
                else if (h < 300 && w > 800) { w = 800; }

                System.Windows.Controls.Image image = null;
                if (canvas.Children.Count == 1)
                {
                    image = new System.Windows.Controls.Image { Source = BitmapToImageSource(bitmap), Height = h, Width = w, Opacity = 0.5 };
                    demo.Visibility = Visibility.Visible;
                }
                else if (canvas.Children.Count == 0)
                {
                    image = new System.Windows.Controls.Image { Source = BitmapToImageSource(bitmap), Height = h, Width = w};
                }
                else
                    return;
                mergeSecond.Source = image.Source;
                Canvas.SetLeft(image, 0);
                Canvas.SetTop(image, 0);
                canvas.Children.Add(image);

                if (bitmapToBeMerged == null)
                    bitmapToBeMerged = (Bitmap)bitmap.Clone();
                else
                {
                    bitmapToBeMerged = addBitmaps(bitmapToBeMerged, bitmap);
                    mergedShares.Source = BitmapToImageSource(bitmapToBeMerged);
                    saveMerged_Button.Visibility = Visibility.Visible;
                }

                bitmap.Dispose();
            }
        }

        private void saveMerged_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                DefaultExt = ".bmp",
                Filter = "Pliki graficzne|*.bmp; *.png; *.jpg; *.gif; *.jpeg"
            };

            if (dialog.ShowDialog() == true)
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                var ext = Path.GetExtension(dialog.FileName);
                if (ext == ".png")
                    encoder = new PngBitmapEncoder();
                else if (ext == ".jpg" || ext== ".jpeg")
                    encoder = new JpegBitmapEncoder();
                else if (ext == ".gif")
                    encoder = new GifBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(BitmapToImageSource(bitmapToBeMerged)));
                using (var fileStream = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
            }
        }

        private void imageLoad_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Pliki graficzne|*.bmp; *.png; *.jpg; *.gif; *.jpeg"
            };
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                Bitmap b = new Bitmap(dlg.FileName);
                Grayscale filterG = new Grayscale(0.2125, 0.7154, 0.0721);
                Bitmap grayImage = b;
                if (b.PixelFormat != PixelFormat.Format8bppIndexed)
                {
                    grayImage = filterG.Apply(b);
                    IterativeThreshold filter = new IterativeThreshold(2, 128);
                    filter.ApplyInPlace(grayImage);
                }
                image.Source = BitmapToImageSource(grayImage);

                var path = Path.GetDirectoryName(dlg.FileName);
                var filename = Path.GetFileNameWithoutExtension(dlg.FileName);
                var extension = Path.GetExtension(dlg.FileName);
                imageFileName = new string[] { path, filename, extension };

                Bitmap[] shares = null;

                if (twoPixels.IsChecked == true)
                    shares = twoPixelsF(grayImage);
                else if (fourPixels.IsChecked == true)
                    shares = fourPixelsF(grayImage);
                else
                    showAlert("Nieoczekiwany błąd. Pole obok nie zostało zaznaczone.");
                showAlert("Udziały zostały automatycznie zapisane w folderze: "+imageFileName[0]);
                if (shares != null)
                {
                    firstShare.Source = BitmapToImageSource(shares[0]);
                    secondShare.Source = BitmapToImageSource(shares[1]);
                }
            }
        }

        private void twoPixels_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void fourPixels_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void mergeReset_Button_Click(object sender, RoutedEventArgs e)
        {
            mergeFirst.Source = null;
            mergeSecond.Source = null;
            mergedShares.Source = null;
            bitmapToBeMerged = null;
            canvas.Children.Clear();
            demo.Visibility = Visibility.Collapsed;
            saveMerged_Button.Visibility = Visibility.Hidden;
        }
    }


    public static class ExtBitmap
    {
        public static Bitmap CloseMorphologyFilter(this Bitmap sourceBitmap,
                                                           int matrixSize,
                                                           bool applyBlue = true,
                                                           bool applyGreen = true,
                                                           bool applyRed = true)
        {
            Bitmap resultBitmap = sourceBitmap.DilateAndErodeFilter(matrixSize,
                                                        MorphologyType.Dilation,
                                                applyBlue, applyGreen, applyRed);

            resultBitmap = resultBitmap.DilateAndErodeFilter(matrixSize,
                                                 MorphologyType.Erosion,
                                                applyBlue, applyGreen, applyRed);

            return resultBitmap;
        }

        public static Bitmap DilateAndErodeFilter(this Bitmap sourceBitmap,
                                                int matrixSize,
                                                MorphologyType morphType,
                                                bool applyBlue = true,
                                                bool applyGreen = true,
                                                bool applyRed = true)
        {
            BitmapData sourceData =
                       sourceBitmap.LockBits(new Rectangle(0, 0,
                       sourceBitmap.Width, sourceBitmap.Height),
                       ImageLockMode.ReadOnly,
                       PixelFormat.Format32bppArgb);

            byte[] pixelBuffer = new byte[sourceData.Stride *
                                          sourceData.Height];

            byte[] resultBuffer = new byte[sourceData.Stride *
                                           sourceData.Height];

            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0,
                                       pixelBuffer.Length);

            sourceBitmap.UnlockBits(sourceData);

            int filterOffset = (matrixSize - 1) / 2;
            int calcOffset = 0;

            int byteOffset = 0;

            byte blue = 0;
            byte green = 0;
            byte red = 0;

            byte morphResetValue = 0;

            if (morphType == MorphologyType.Erosion)
            {
                morphResetValue = 255;
            }

            for (int offsetY = filterOffset; offsetY <
                sourceBitmap.Height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX <
                    sourceBitmap.Width - filterOffset; offsetX++)
                {
                    byteOffset = offsetY *
                                 sourceData.Stride +
                                 offsetX * 4;

                    blue = morphResetValue;
                    green = morphResetValue;
                    red = morphResetValue;

                    if (morphType == MorphologyType.Dilation)
                    {
                        for (int filterY = -filterOffset;
                            filterY <= filterOffset; filterY++)
                        {
                            for (int filterX = -filterOffset;
                                filterX <= filterOffset; filterX++)
                            {
                                calcOffset = byteOffset +
                                             (filterX * 4) +
                                (filterY * sourceData.Stride);

                                if (pixelBuffer[calcOffset] > blue)
                                {
                                    blue = pixelBuffer[calcOffset];
                                }

                                if (pixelBuffer[calcOffset + 1] > green)
                                {
                                    green = pixelBuffer[calcOffset + 1];
                                }

                                if (pixelBuffer[calcOffset + 2] > red)
                                {
                                    red = pixelBuffer[calcOffset + 2];
                                }
                            }
                        }
                    }
                    else if (morphType == MorphologyType.Erosion)
                    {
                        for (int filterY = -filterOffset;
                            filterY <= filterOffset; filterY++)
                        {
                            for (int filterX = -filterOffset;
                                filterX <= filterOffset; filterX++)
                            {

                                calcOffset = byteOffset +
                                             (filterX * 4) +
                                (filterY * sourceData.Stride);

                                if (pixelBuffer[calcOffset] < blue)
                                {
                                    blue = pixelBuffer[calcOffset];
                                }

                                if (pixelBuffer[calcOffset + 1] < green)
                                {
                                    green = pixelBuffer[calcOffset + 1];
                                }

                                if (pixelBuffer[calcOffset + 2] < red)
                                {
                                    red = pixelBuffer[calcOffset + 2];
                                }
                            }
                        }
                    }

                    if (applyBlue == false)
                    {
                        blue = pixelBuffer[byteOffset];
                    }

                    if (applyGreen == false)
                    {
                        green = pixelBuffer[byteOffset + 1];
                    }

                    if (applyRed == false)
                    {
                        red = pixelBuffer[byteOffset + 2];
                    }

                    resultBuffer[byteOffset] = blue;
                    resultBuffer[byteOffset + 1] = green;
                    resultBuffer[byteOffset + 2] = red;
                    resultBuffer[byteOffset + 3] = 255;
                }
            }

            Bitmap resultBitmap = new Bitmap(sourceBitmap.Width,
                                             sourceBitmap.Height);

            BitmapData resultData =
                       resultBitmap.LockBits(new Rectangle(0, 0,
                       resultBitmap.Width, resultBitmap.Height),
                       ImageLockMode.WriteOnly,
                       PixelFormat.Format32bppArgb);

            Marshal.Copy(resultBuffer, 0, resultData.Scan0,
                                       resultBuffer.Length);

            resultBitmap.UnlockBits(resultData);

            return resultBitmap;
        }

        public enum MorphologyType
        {
            Dilation,
            Erosion
        }
    }
}