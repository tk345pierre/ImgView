using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Windows.Controls.Image;
using System.Collections.Generic;

namespace ImgView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        class MyImage
        {
            public string FilePath { get; set; } = "";
            public BitmapImage BitmapImg { get; set; } = null;
            public Image ImageSrc { get; set; } = null;
            public void LoadBitmap()
            {
                if (ImageSrc.Source == null)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(FilePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    ImageSrc.Source = bitmap;
                }
            }

            public void Unload()
            {
                if (ImageSrc.Source != null)
                {
                    BitmapImg = null;
                    ImageSrc.Source = null;
                }
            }

            public void Clear()
            {
                FilePath = "";
                BitmapImg = null;
                ImageSrc = null;
            }
        }

        List<Image> imageControlList = null;
        List<TextBlock> dirTextBlockList = null;
        Dictionary<string, List<MyImage>> fileDictionary = null;
        private bool isRightButtonDown = false;
        private Point initialMousePosition;

        enum ViewMode
        {
            Max = 0,
            ImgSize = 1,
        }
        private ViewMode viewMode = ViewMode.Max;

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            if (imageControlList != null)
            {
                for (int i = 0; i < imageControlList.Count; i++)
                {
                    imageControlList[i] = null;
                }
            }
            if (dirTextBlockList != null)
            {
                for (int i = 0; i < dirTextBlockList.Count; i++)
                {
                    dirTextBlockList[i] = null;
                }
            }
            mainWrapPanel.Children.Clear();
            myListView.Items.Clear();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            imageControlList = new List<Image>();
            dirTextBlockList = new List<TextBlock>();

            string imageDirectory = dirPathTextBox.Text;
            fileDictionary = GetFilesRecursively(imageDirectory);
            LoadImagesFromDirectory(fileDictionary);
        }

        private void LoadImagesFromDirectory(Dictionary<string, List<MyImage>> filesDictionary)
        {
            foreach (var files in filesDictionary)
            {
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;

                TextBlock directoryName = new TextBlock();
                directoryName.Text = System.IO.Path.GetFileName(files.Key);
                directoryName.FontWeight = FontWeights.Bold;
                directoryName.Margin = new Thickness(0, 10, 0, 5);
                directoryName.HorizontalAlignment = HorizontalAlignment.Center;
                directoryName.Background = new SolidColorBrush(Colors.Yellow);

                dirTextBlockList.Add(directoryName);

                ItemsControl itemsControl = new ItemsControl();
                var factoryPanel = new FrameworkElementFactory(typeof(WrapPanel));
                factoryPanel.SetValue(Panel.IsItemsHostProperty, true);
                factoryPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
                var template = new ItemsPanelTemplate { VisualTree = factoryPanel };
                itemsControl.ItemsPanel = template;

                foreach (var myImage in files.Value)
                {
                    StackPanel itemstackPanel = new StackPanel();
                    itemstackPanel.Orientation = Orientation.Vertical;

                    Border border = new Border();
                    border.Background = new SolidColorBrush(Colors.Gray);

                    var selectItem = sizeCombobox.SelectedItem as ComboBoxItem;
                    var w = Int32.Parse(selectItem.Content as string);
                    Image image = new Image();
                    image.Width = w;
                    image.MaxHeight = w * 3;
                    image.MinWidth = 100;
                    image.MinHeight = 100;
                    image.Margin = new Thickness(5);
                    image.MouseRightButtonDown += Image_MouseRightButtonDown;
                    image.MouseRightButtonUp += Image_MouseRightButtonUp;
                    image.MouseLeftButtonUp += Image_MouseLeftButtonUp;

                    myImage.ImageSrc = image;
                    border.Child = image;

                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = TruncateString(System.IO.Path.GetFileName(myImage.FilePath), 20);
                    textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                    textBlock.Foreground = new SolidColorBrush(Colors.White);

                    itemstackPanel.Children.Add(border);
                    itemstackPanel.Children.Add(textBlock);

                    itemsControl.Items.Add(itemstackPanel);

                    imageControlList.Add(image);
                }

                stackPanel.Children.Add(directoryName);
                stackPanel.Children.Add(itemsControl);

                mainWrapPanel.Children.Add(stackPanel);
            }
        }

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 10)
            {
                List<MyImage> reducedList = fileDictionary.SelectMany(kvp => kvp.Value).ToList();
                for (int i = 0; i < reducedList.Count; i++)
                {
                    if (reducedList[i].FilePath == (mainImg.Source as BitmapImage).UriSource.LocalPath)
                    {
                        if (i == 0)
                        {
                            continue;
                        }
                        mainImg.Source = new BitmapImage(new Uri(reducedList[i - 1].FilePath, UriKind.Absolute));
                        break;
                    }
                }
            }
            else if (e.Delta < -10)
            {
                List<MyImage> reducedList = fileDictionary.SelectMany(kvp => kvp.Value).ToList();
                for (int i = 0; i < reducedList.Count; i++)
                {
                    if (reducedList[i].FilePath == (mainImg.Source as BitmapImage).UriSource.LocalPath)
                    {
                        if (reducedList.Count - 1 < i)
                        {
                            continue;
                        }
                        mainImg.Source = new BitmapImage(new Uri(reducedList[i + 1].FilePath, UriKind.Absolute));
                        break;
                    }
                }
            }
        }

        static Dictionary<string, List<MyImage>> GetFilesRecursively(string rootDirectory)
        {
            var filesDictionary = new Dictionary<string, List<MyImage>>();
            GetFiles(rootDirectory, filesDictionary);
            return filesDictionary;
        }
        static void GetFiles(string directory, Dictionary<string, List<MyImage>> filesDictionary)
        {
            try
            {
                var files = Directory.GetFiles(directory, "*.png");
                var directories = Directory.GetDirectories(directory);

                if (files.Length > 0)
                {
                    var list = new List<MyImage>();
                    foreach (var file in files)
                    {
                        list.Add(new MyImage() { FilePath = file });
                    }
                    filesDictionary[directory] = list;
                }

                foreach (var dir in directories)
                {
                    GetFiles(dir, filesDictionary);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing {directory}: {ex.Message}");
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox? comboBox = sender as ComboBox;
            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            var w = Int32.Parse(selectedItem.Content.ToString());
            imageControlList?.ForEach(item => { item.Width = w; item.MaxHeight = w * 3; });
        }

        private void Image_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            isRightButtonDown = true;
            initialMousePosition = e.GetPosition(null);
        }

        private void Image_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var clickedImage = sender as Image;
            if (clickedImage != null)
            {
                if (isRightButtonDown)
                {
                    isRightButtonDown = false;
                    Point currentMousePosition = e.GetPosition(null);
                    if (currentMousePosition.Y > initialMousePosition.Y)
                    {
                        myListView.Items.Add(GetImageAbsolutePath(clickedImage));

                        (clickedImage.Parent as Border).Background = new SolidColorBrush(Colors.Red);
                    }
                }
            }
        }

        private void MainImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            isRightButtonDown = true;
            initialMousePosition = e.GetPosition(null);
        }

        private void MainImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isRightButtonDown)
            {
                isRightButtonDown = false;
                Point currentMousePosition = e.GetPosition(null);
                if (currentMousePosition.Y > initialMousePosition.Y)
                {
                    mainImg.Stretch = viewMode == ViewMode.ImgSize ? Stretch.None : Stretch.Uniform;
                    viewMode = (ViewMode)((int)++viewMode % 2);
                }
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var clickedImage = sender as Image;
            if (clickedImage != null)
            {
                if (mainStackPanel.Visibility == Visibility.Collapsed)
                {
                    mainStackPanel.Visibility = Visibility.Visible;
                    mainWrapPanel.Visibility = Visibility.Collapsed;
                    menuStackPanel.Visibility = Visibility.Collapsed;

                    mainImg.Source = new BitmapImage(new Uri(GetImageAbsolutePath(clickedImage), UriKind.Absolute));
                }
                else
                {
                    mainStackPanel.Visibility = Visibility.Collapsed;
                    mainWrapPanel.Visibility = Visibility.Visible;
                    menuStackPanel.Visibility = Visibility.Visible;
                    mainImg.Source = null;
                }
            }
        }
        private string GetImageAbsolutePath(Image image)
        {
            if (image.Source is BitmapImage bitmapImage)
            {
                return bitmapImage.UriSource.LocalPath;
            }
            return null;
        }

        public static string TruncateString(string input, int maxLength)
        {
            if (input.Length <= maxLength)
            {
                return input;
            }
            return input.Substring(0, maxLength - 3) + "...";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var pathObj in myListView.Items)
            {
                var path = pathObj.ToString();
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            Init();
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            foreach (var dirT in dirTextBlockList)
            {
                var dirPath = dirPathTextBox.Text + "\\" + dirT.Text;
                if (IsElementVisible(mainScrollViewer, dirT, 1))
                {
                    foreach (var myImage in fileDictionary[dirPath])
                    {
                        myImage.LoadBitmap();
                    }
                }
                else
                {
                    foreach (var myImage in fileDictionary[dirPath])
                    {
                        myImage.Unload();
                    }
                }
            }
        }

        private bool IsElementVisible(FrameworkElement container, FrameworkElement element, int pageOffset)
        {
            if (!element.IsVisible)
                return false;

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            Rect viewport = new Rect(0, 0, container.ActualWidth, container.ActualHeight);

            double pageHeight = container.ActualHeight;
            Rect extendedViewport = new Rect(viewport.X, viewport.Y - pageHeight * pageOffset, viewport.Width, viewport.Height + pageHeight * 2 * pageOffset);

            return extendedViewport.IntersectsWith(bounds);
        }
    }
}